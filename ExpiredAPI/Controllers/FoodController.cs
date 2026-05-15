using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExpiredAPI.Models;
using ExpiredAPI.Models.DTOs;
using ExpiredAPI.Services;

namespace ExpiredAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoodController : ControllerBase
{
    private readonly IFoodService _foodService;

    public FoodController(IFoodService foodService)
    {
        _foodService = foodService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }

    /// <summary>
    /// 获取用户的所有食品
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FoodItemResponse>>> GetAll()
    {
        var userId = GetUserId();
        var foods = await _foodService.GetUserFoodsAsync(userId);
        var result = foods.Select(FoodItemResponse.FromEntity).ToList();
        return Ok(result);
    }

    /// <summary>
    /// 获取单个食品详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FoodItemResponse>> GetById(int id)
    {
        var userId = GetUserId();
        var food = await _foodService.GetFoodByIdAsync(id, userId);

        if (food == null)
            return NotFound(new { message = "食品不存在" });

        return Ok(FoodItemResponse.FromEntity(food));
    }

    /// <summary>
    /// 新增食品
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FoodItemResponse>> Add([FromBody] AddFoodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "食品名称不能为空" });

        var userId = GetUserId();
        var foodItem = new FoodItem
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Category = request.Category?.Trim(),
            ExpirationDate = request.ExpirationDate,
            PurchaseDate = request.PurchaseDate,
            Quantity = request.Quantity,
            Unit = request.Unit,
            Notes = request.Notes?.Trim()
        };

        var created = await _foodService.AddFoodAsync(foodItem);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, FoodItemResponse.FromEntity(created));
    }

    /// <summary>
    /// 更新食品信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<FoodItemResponse>> Update(int id, [FromBody] UpdateFoodRequest request)
    {
        var userId = GetUserId();

        var updated = await _foodService.UpdateFoodAsync(id, userId, food =>
        {
            if (request.Name != null) food.Name = request.Name.Trim();
            if (request.Category != null) food.Category = request.Category.Trim();
            if (request.ExpirationDate.HasValue) food.ExpirationDate = request.ExpirationDate.Value;
            if (request.PurchaseDate.HasValue) food.PurchaseDate = request.PurchaseDate;
            if (request.Quantity.HasValue) food.Quantity = request.Quantity.Value;
            if (request.Unit != null) food.Unit = request.Unit;
            if (request.Notes != null) food.Notes = request.Notes.Trim();
        });

        if (updated == null)
            return NotFound(new { message = "食品不存在" });

        return Ok(FoodItemResponse.FromEntity(updated));
    }

    /// <summary>
    /// 删除食品
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var deleted = await _foodService.DeleteFoodAsync(id, userId);

        if (!deleted)
            return NotFound(new { message = "食品不存在" });

        return NoContent();
    }

    /// <summary>
    /// 获取食品统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetStats()
    {
        var userId = GetUserId();
        var foods = await _foodService.GetUserFoodsAsync(userId);

        return Ok(new
        {
            Total = foods.Count,
            Normal = foods.Count(f => f.Status == 0),
            ExpiringSoon = foods.Count(f => f.Status == 1),
            Expired = foods.Count(f => f.Status == 2)
        });
    }
}
