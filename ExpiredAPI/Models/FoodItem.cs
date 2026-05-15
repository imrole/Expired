using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpiredAPI.Models;

public class FoodItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Category { get; set; }

    [Required]
    public DateTime ExpirationDate { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public int Quantity { get; set; } = 1;

    [MaxLength(20)]
    public string Unit { get; set; } = "个";

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// 0=正常 1=即将过期 2=已过期
    /// </summary>
    public int Status { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
