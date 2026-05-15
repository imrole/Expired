using Microsoft.EntityFrameworkCore;
using ExpiredAPI.Models;

namespace ExpiredAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<FoodItem> FoodItems => Set<FoodItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User 配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.OpenId).IsUnique();
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(u => u.UpdatedAt).HasDefaultValueSql("GETDATE()");
        });

        // FoodItem 配置
        modelBuilder.Entity<FoodItem>(entity =>
        {
            entity.HasIndex(f => f.UserId);
            entity.HasIndex(f => f.ExpirationDate);
            entity.Property(f => f.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(f => f.UpdatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(f => f.Quantity).HasDefaultValue(1);
            entity.Property(f => f.Unit).HasDefaultValue("个");
            entity.Property(f => f.Status).HasDefaultValue(0);

            entity.HasOne(f => f.User)
                  .WithMany(u => u.FoodItems)
                  .HasForeignKey(f => f.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
