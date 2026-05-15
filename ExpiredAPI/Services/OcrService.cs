using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExpiredAPI.Models.DTOs;

namespace ExpiredAPI.Services;

public class OcrService : IOcrService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OcrService> _logger;
    private readonly string _apiKey;
    private readonly string _secretKey;

    public OcrService(HttpClient httpClient, IConfiguration configuration, ILogger<OcrService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["BaiduOcr:ApiKey"] ?? "";
        _secretKey = configuration["BaiduOcr:SecretKey"] ?? "";
    }

    public async Task<OcrResultResponse> RecognizeAsync(Stream imageStream)
    {
        try
        {
            // Step 1: 获取 Access Token
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new OcrResultResponse
                {
                    Success = false,
                    ErrorMessage = "百度OCR AccessToken 获取失败，请检查 ApiKey 和 SecretKey 配置"
                };
            }

            // Step 2: 调用百度 OCR 通用文字识别
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            var content = new StringContent(
                $"image={Uri.EscapeDataString(base64Image)}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            );

            var response = await _httpClient.PostAsync(
                $"https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic?access_token={accessToken}",
                content
            );

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("百度OCR响应: {Response}", responseBody);

            // Step 3: 解析返回结果
            using var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("error_code", out var errorCode))
            {
                return new OcrResultResponse
                {
                    Success = false,
                    ErrorMessage = $"百度OCR错误: {root.GetProperty("error_msg").GetString()}"
                };
            }

            // 提取所有识别到的文本行并拼接
            var texts = new List<string>();
            if (root.TryGetProperty("words_result", out var wordsResult))
            {
                foreach (var item in wordsResult.EnumerateArray())
                {
                    if (item.TryGetProperty("words", out var word))
                    {
                        texts.Add(word.GetString() ?? "");
                    }
                }
            }

            var rawText = string.Join("\n", texts);

            // Step 4: 从识别文本中提取保质期信息
            var result = ParseExpirationDate(rawText);

            return new OcrResultResponse
            {
                Success = result.success,
                RawText = rawText,
                FoodName = result.foodName,
                ExpirationDate = result.expirationDate,
                ErrorMessage = result.errorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR识别失败");
            return new OcrResultResponse
            {
                Success = false,
                ErrorMessage = $"OCR识别异常: {ex.Message}"
            };
        }
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var content = new StringContent(
                $"grant_type=client_credentials&client_id={_apiKey}&client_secret={_secretKey}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            );

            var response = await _httpClient.PostAsync(
                "https://aip.baidubce.com/oauth/2.0/token",
                content
            );

            var responseBody = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(responseBody);

            if (jsonDoc.RootElement.TryGetProperty("access_token", out var token))
            {
                return token.GetString();
            }

            _logger.LogWarning("获取百度AccessToken失败: {Response}", responseBody);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取百度AccessToken异常");
            return null;
        }
    }

    /// <summary>
    /// 从 OCR 文本中解析保质期和相关食品信息
    /// </summary>
    private static (bool success, string? foodName, DateTime? expirationDate, string? errorMessage) ParseExpirationDate(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return (false, null, null, "未识别到任何文字");
        }

        // 1. 尝试提取保质期日期 —— 匹配多种日期格式
        var datePatterns = new[]
        {
            // 保质期至：2025-12-31 / 保质期至：2025/12/31
            @"保质[期到至][：:]\s*(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})",
            @"保质[期到至][：:]?\s*(\d{4})年(\d{1,2})月(\d{1,2})日",
            // 到期：2025-12-31 / 有效期至：2025-12-31
            @"(?:到期|有效期[至到])[：:]\s*(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})",
            // 纯日期 2025-12-31
            @"(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})",
            // 2025年12月31日
            @"(\d{4})年(\d{1,2})月(\d{1,2})日",
        };

        DateTime? expirationDate = null;
        foreach (var pattern in datePatterns)
        {
            var match = Regex.Match(rawText, pattern);
            if (match.Success && match.Groups.Count >= 4)
            {
                if (int.TryParse(match.Groups[1].Value, out var year) &&
                    int.TryParse(match.Groups[2].Value, out var month) &&
                    int.TryParse(match.Groups[3].Value, out var day))
                {
                    // 简单有效性校验
                    if (year >= 2024 && year <= 2099 && month >= 1 && month <= 12 && day >= 1 && day <= 31)
                    {
                        expirationDate = new DateTime(year, month, day);
                        break;
                    }
                }
            }
        }

        // 2. 尝试提取食品名称 —— 取第一行非日期文字作为参考
        var lines = rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var foodName = lines.Length > 0
            ? lines[0].Trim().Trim('"', '“', '”', '《', '》').Truncate(50)
            : null;

        // 过滤掉明显不是食品名称的文本
        if (foodName != null && (foodName.Length > 30 || Regex.IsMatch(foodName, @"^\d") || foodName.Contains("生产") || foodName.Contains("保质")))
        {
            foodName = null;
        }

        // 3. 尝试提取"生产日期"来推算保质期到期日
        // 生产日期: 2025-01-01 + 保质期: 12个月
        var prodDateMatch = Regex.Match(rawText, @"生产日期[：:]\s*(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})");
        var shelfLifeMatch = Regex.Match(rawText, @"保质[期到至][：:]\s*(\d+)\s*(?:个月|月|天)");

        if (expirationDate == null && prodDateMatch.Success && shelfLifeMatch.Success)
        {
            if (int.TryParse(prodDateMatch.Groups[1].Value, out var py) &&
                int.TryParse(prodDateMatch.Groups[2].Value, out var pm) &&
                int.TryParse(prodDateMatch.Groups[3].Value, out var pd) &&
                int.TryParse(shelfLifeMatch.Groups[1].Value, out var duration))
            {
                var prodDate = new DateTime(py, pm, pd);
                if (shelfLifeMatch.Value.Contains("天"))
                {
                    expirationDate = prodDate.AddDays(duration);
                }
                else // 月
                {
                    expirationDate = prodDate.AddMonths(duration);
                }
            }
        }

        if (expirationDate == null && rawText.Contains("未识别"))
        {
            return (false, null, null, "图片中未识别到保质期信息，请尝试手动输入");
        }

        if (expirationDate == null)
        {
            return (false, foodName, null,
                $"已识别到文字，但未找到有效的保质期日期。识别到的文本：{rawText.Truncate(100)}");
        }

        return (true, foodName, expirationDate, null);
    }
}

public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
