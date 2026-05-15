using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ExpiredAPI.Data;
using ExpiredAPI.Models;
using ExpiredAPI.Models.DTOs;

namespace ExpiredAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext db, IConfiguration configuration, HttpClient httpClient, ILogger<AuthController> logger)
    {
        _db = db;
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// 微信登录（使用 wx.login 获取的 code，通过微信服务器换取 openid）
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Code))
        {
            return BadRequest(new { message = "code 不能为空" });
        }

        var appId = _configuration["WeChat:AppId"];
        var appSecret = _configuration["WeChat:AppSecret"];

        if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appSecret))
        {
            return BadRequest(new { message = "服务器未配置微信小程序 AppId/AppSecret" });
        }

        // 调用微信接口获取 openid
        string openId;
        try
        {
            var wxResponse = await _httpClient.GetStringAsync(
                $"https://api.weixin.qq.com/sns/jscode2session?appid={appId}&secret={appSecret}&js_code={request.Code}&grant_type=authorization_code");

            using var jsonDoc = System.Text.Json.JsonDocument.Parse(wxResponse);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("errcode", out var errCode) && errCode.GetInt32() != 0)
            {
                var errMsg = root.TryGetProperty("errmsg", out var msg) ? msg.GetString() : "未知错误";
                _logger.LogWarning("微信登录失败: {Error}", wxResponse);
                return BadRequest(new { message = $"微信登录失败: {errMsg}" });
            }

            openId = root.GetProperty("openid").GetString()!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用微信接口失败");
            return StatusCode(500, new { message = "微信登录服务调用失败" });
        }

        // 查找或创建用户
        var user = await _db.Users.FirstOrDefaultAsync(u => u.OpenId == openId);
        if (user == null)
        {
            user = new User
            {
                OpenId = openId,
                NickName = request.NickName,
                AvatarUrl = request.AvatarUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        else
        {
            // 更新用户信息
            if (request.NickName != null) user.NickName = request.NickName;
            if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // 生成 JWT Token
        var token = GenerateJwtToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            NickName = user.NickName,
            AvatarUrl = user.AvatarUrl
        });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSecret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret 未配置");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.OpenId),
        };

        var token = new JwtSecurityToken(
            issuer: "ExpiredAPI",
            audience: "ExpiredMiniProgram",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
