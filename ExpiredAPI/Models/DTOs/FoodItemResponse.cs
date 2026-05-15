namespace ExpiredAPI.Models.DTOs;

public class FoodItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = "个";
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
    public int Status { get; set; }
    public int DaysRemaining { get; set; }
    public DateTime CreatedAt { get; set; }

    public static FoodItemResponse FromEntity(FoodItem item)
    {
        var daysRemaining = (item.ExpirationDate.Date - DateTime.UtcNow.Date).Days;

        return new FoodItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            ExpirationDate = item.ExpirationDate,
            PurchaseDate = item.PurchaseDate,
            Quantity = item.Quantity,
            Unit = item.Unit,
            ImageUrl = item.ImageUrl,
            Notes = item.Notes,
            Status = item.Status,
            DaysRemaining = daysRemaining,
            CreatedAt = item.CreatedAt
        };
    }
}
