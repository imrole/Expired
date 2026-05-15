using ExpiredAPI.Models.DTOs;

namespace ExpiredAPI.Services;

public interface IOcrService
{
    Task<OcrResultResponse> RecognizeAsync(Stream imageStream);
}
