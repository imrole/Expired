using ExpiredAPI.Models;

namespace ExpiredAPI.Services;

public interface IFoodService
{
    Task<List<FoodItem>> GetUserFoodsAsync(int userId);
    Task<FoodItem?> GetFoodByIdAsync(int foodId, int userId);
    Task<FoodItem> AddFoodAsync(FoodItem foodItem);
    Task<FoodItem?> UpdateFoodAsync(int foodId, int userId, Action<FoodItem> updateAction);
    Task<bool> DeleteFoodAsync(int foodId, int userId);
}
