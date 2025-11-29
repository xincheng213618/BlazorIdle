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
            entity.Property(e => e.MaxCharacterSlots).HasDefaultValue(1);
            entity.Property(e => e.UsedCharacterSlots).HasDefaultValue(0);

            // 配置账号级购买状态为 JSON 列 (Step 4 Phase 2)
            // Configure account-level purchase state as JSON column
            entity.Property(e => e.AccountPurchaseState)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<BlazorIdle.Game.Purchase.AccountPurchaseState>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new BlazorIdle.Game.Purchase.AccountPurchaseState()
                )
                .HasColumnType("TEXT");
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
            
            // 配置库存为 JSON 列 - 不创建单独的表
            // Configure inventory as JSON column - don't create separate table
            entity.OwnsOne(e => e.Inventory, inventory =>
            {
                inventory.ToJson(); // 存储为 JSON 列
                inventory.OwnsMany(i => i.Items); // Items 列表也存储在 JSON 中
            });

            // 配置职业进度为 JSON 列 - 存储 Dictionary<string, ProfessionProgress>
            // Configure profession progress as JSON column - store Dictionary<string, ProfessionProgress>
            entity.Property(e => e.Professions)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ProfessionProgress>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, ProfessionProgress>()
                )
                .HasColumnType("TEXT");

            // 配置激活的战斗职业ID
            // Configure active combat profession ID
            entity.Property(e => e.ActiveCombatProfessionId)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("warrior");

            // 配置已学习的技能为 JSON 列 (Step 2 Phase 2.5)
            // Configure learned skills as JSON column
            entity.Property(e => e.LearnedSkills)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new HashSet<string>()
                )
                .HasColumnType("TEXT");

            // 配置已装备的技能为 JSON 列 (Step 2 Phase 2.5)
            // Configure equipped skills as JSON column - store Dictionary<string, EquippedSkillsConfig>
            entity.Property(e => e.EquippedSkillsByProfession)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, EquippedSkillsConfig>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, EquippedSkillsConfig>()
                )
                .HasColumnType("TEXT");

            // 配置账号标记为 JSON 列 (Step 2 Phase 2.5+)
            // Configure account flags as JSON column
            entity.Property(e => e.AccountFlags)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new HashSet<string>()
                )
                .HasColumnType("TEXT");

            // 配置固定技能为 JSON 列 (Step 2 Phase 3+)
            // Configure fixed skills as JSON column - store Dictionary<string, ProfessionFixedSkills>
            entity.Property(e => e.FixedSkillsByProfession)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ProfessionFixedSkills>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, ProfessionFixedSkills>()
                )
                .HasColumnType("TEXT");

            // 配置购买状态为 JSON 列 (Step 4)
            // Configure purchase state as JSON column - store PurchaseState
            entity.Property(e => e.PurchaseState)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<BlazorIdle.Game.Purchase.PurchaseState>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new BlazorIdle.Game.Purchase.PurchaseState()
                )
                .HasColumnType("TEXT");

            // 配置消耗品装备为 JSON 列 (药水与食物系统)
            // Configure equipped consumables as JSON column - store ConsumableEquipmentConfig
            entity.Property(e => e.EquippedConsumables)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<ConsumableEquipmentConfig>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new ConsumableEquipmentConfig()
                )
                .HasColumnType("TEXT");

            // 配置按职业分组的消耗品装备为 JSON 列 (药水与食物系统)
            // Configure equipped consumables by profession as JSON column - store Dictionary<string, ConsumableEquipmentConfig>
            entity.Property(e => e.EquippedConsumablesByProfession)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ConsumableEquipmentConfig>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, ConsumableEquipmentConfig>()
                )
                .HasColumnType("TEXT");
        });
    }
}
