namespace ExpiredAPI.Models.DTOs;

public class AddFoodRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public int Quantity { get; set; } = 1;
    public string Unit { get; set; } = "个";
    public string? Notes { get; set; }
}

public class UpdateFoodRequest
{
    public string? Name { get; set; }
    public string? Category { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public int? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }
}
