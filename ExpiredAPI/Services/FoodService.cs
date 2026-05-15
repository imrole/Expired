using Microsoft.EntityFrameworkCore;
using ExpiredAPI.Data;
using ExpiredAPI.Models;

namespace ExpiredAPI.Services;

public class FoodService : IFoodService
{
    private readonly AppDbContext _db;

    public FoodService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<FoodItem>> GetUserFoodsAsync(int userId)
    {
        var foods = await _db.FoodItems
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.ExpirationDate)
            .ToListAsync();

        // 自动更新过期状态
        foreach (var food in foods)
        {
            var daysRemaining = (food.ExpirationDate.Date - DateTime.UtcNow.Date).Days;
            var newStatus = daysRemaining < 0 ? 2 : daysRemaining <= 3 ? 1 : 0;
            if (food.Status != newStatus)
            {
                food.Status = newStatus;
                food.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (foods.Any(f => f.Status != 0)) // 只在有状态变更时保存
        {
            await _db.SaveChangesAsync();
        }

        return foods;
    }

    public async Task<FoodItem?> GetFoodByIdAsync(int foodId, int userId)
    {
        return await _db.FoodItems
            .FirstOrDefaultAsync(f => f.Id == foodId && f.UserId == userId);
    }

    public async Task<FoodItem> AddFoodAsync(FoodItem foodItem)
    {
        // 计算初始状态
        var daysRemaining = (foodItem.ExpirationDate.Date - DateTime.UtcNow.Date).Days;
        foodItem.Status = daysRemaining < 0 ? 2 : daysRemaining <= 3 ? 1 : 0;

        _db.FoodItems.Add(foodItem);
        await _db.SaveChangesAsync();
        return foodItem;
    }

    public async Task<FoodItem?> UpdateFoodAsync(int foodId, int userId, Action<FoodItem> updateAction)
    {
        var food = await _db.FoodItems
            .FirstOrDefaultAsync(f => f.Id == foodId && f.UserId == userId);

        if (food == null) return null;

        updateAction(food);

        // 重新计算状态
        var daysRemaining = (food.ExpirationDate.Date - DateTime.UtcNow.Date).Days;
        food.Status = daysRemaining < 0 ? 2 : daysRemaining <= 3 ? 1 : 0;
        food.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return food;
    }

    public async Task<bool> DeleteFoodAsync(int foodId, int userId)
    {
        var food = await _db.FoodItems
            .FirstOrDefaultAsync(f => f.Id == foodId && f.UserId == userId);

        if (food == null) return false;

        _db.FoodItems.Remove(food);
        await _db.SaveChangesAsync();
        return true;
    }
}
