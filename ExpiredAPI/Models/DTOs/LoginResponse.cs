namespace ExpiredAPI.Models.DTOs;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string? NickName { get; set; }
    public string? AvatarUrl { get; set; }
}
