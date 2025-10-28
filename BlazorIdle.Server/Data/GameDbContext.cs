using Microsoft.EntityFrameworkCore;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Server.Data;

/// <summary>
/// 游戏数据库上下文 - 管理用户和角色数据
/// Game database context - manages user and character data
/// </summary>
public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// 用户表
    /// User table
    /// </summary>
    public DbSet<User> Users { get; set; }
    
    /// <summary>
    /// 角色表
    /// Character table
    /// </summary>
    public DbSet<CharacterData> Characters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 配置用户实体
        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.MaxCharacterSlots).HasDefaultValue(3);
            entity.Property(e => e.UsedCharacterSlots).HasDefaultValue(0);
        });
        
        // 配置角色实体
        // Configure Character entity
        modelBuilder.Entity<CharacterData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ProfessionId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.Name });
        });
    }
}
