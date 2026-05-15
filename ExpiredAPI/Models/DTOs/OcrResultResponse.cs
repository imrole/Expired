namespace ExpiredAPI.Models.DTOs;

public class OcrResultResponse
{
    /// <summary>
    /// 是否识别成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 识别到的原始文本
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// 解析出的食品名称（如果有）
    /// </summary>
    public string? FoodName { get; set; }

    /// <summary>
    /// 解析出的保质期日期
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
