namespace ExpiredAPI.Models.DTOs;

public class LoginRequest
{
    public string Code { get; set; } = string.Empty;
    public string? NickName { get; set; }
    public string? AvatarUrl { get; set; }
}
