using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpiredAPI.Services;

namespace ExpiredAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OcrController : ControllerBase
{
    private readonly IOcrService _ocrService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<OcrController> _logger;

    public OcrController(IOcrService ocrService, IWebHostEnvironment env, ILogger<OcrController> logger)
    {
        _ocrService = ocrService;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// 上传图片进行 OCR 识别，识别保质期日期
    /// </summary>
    [HttpPost("recognize")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<ActionResult> Recognize(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, errorMessage = "请上传图片文件" });
        }

        // 验证文件类型
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(new { success = false, errorMessage = "仅支持 JPEG、PNG、WebP 格式图片" });
        }

        // 保存图片（可选，用于后续查看）
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("OCR图片已保存: {Path}", filePath);

        // 调用 OCR 服务
        await using var imageStream = System.IO.File.OpenRead(filePath);
        var result = await _ocrService.RecognizeAsync(imageStream);

        return Ok(result);
    }
}
