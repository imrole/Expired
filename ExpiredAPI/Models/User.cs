using System.ComponentModel.DataAnnotations;

namespace ExpiredAPI.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string OpenId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? NickName { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FoodItem> FoodItems { get; set; } = new List<FoodItem>();
}
