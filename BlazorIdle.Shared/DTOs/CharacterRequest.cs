namespace BlazorIdle.Shared.DTOs;

/// <summary>
/// 创建角色请求DTO
/// Create character request DTO
/// </summary>
public class CreateCharacterRequest
{
    /// <summary>
    /// 角色名称
    /// Character name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 职业ID
    /// Profession ID
    /// </summary>
    public string ProfessionId { get; set; } = string.Empty;
}

/// <summary>
/// 更新角色数据请求DTO - 用于心跳保存和手动更新
/// Update character data request DTO - for heartbeat save and manual updates
/// </summary>
public class UpdateCharacterRequest
{
    /// <summary>
    /// 角色名称（可选，通常不允许修改）
    /// Character name (optional, usually not allowed to modify)
    /// </summary>
    public string? Name { get; set; }

    // 角色属性 - 可以随游戏进程变化
    // Character stats - can change during gameplay

    /// <summary>
    /// 最大生命值
    /// Maximum HP
    /// </summary>
    public int? MaxHp { get; set; }

    /// <summary>
    /// 攻击速率（每秒攻击次数）
    /// Attack rate (attacks per second)
    /// </summary>
    public double? AttackRateAPS { get; set; }

    /// <summary>
    /// 每次攻击伤害
    /// Damage per attack
    /// </summary>
    public int? DamagePerAttack { get; set; }

    /// <summary>
    /// 急速百分比
    /// Haste percentage
    /// </summary>
    public double? HastePercent { get; set; }

    /// <summary>
    /// 特殊技能冷却时间（秒）
    /// Special skill cooldown (seconds)
    /// </summary>
    public double? SpecialIntervalSec { get; set; }

    /// <summary>
    /// 特殊技能伤害
    /// Special skill damage
    /// </summary>
    public int? SpecialDamage { get; set; }

    /// <summary>
    /// 暴击率百分比
    /// Critical chance percentage
    /// </summary>
    public double? CritChancePercent { get; set; }

    /// <summary>
    /// 暴击伤害倍率
    /// Critical damage multiplier
    /// </summary>
    public double? CritMultiplier { get; set; }

    /// <summary>
    /// 伤害浮动百分比
    /// Damage variance percentage
    /// </summary>
    public double? VariancePct { get; set; }

    /// <summary>
    /// 复活时间（秒）
    /// Revive time (seconds)
    /// </summary>
    public double? ReviveSec { get; set; }

    /// <summary>
    /// 角色库存数据（JSON序列化）
    /// Character inventory data (JSON serialized)
    /// </summary>
    public BlazorIdle.Shared.Models.Inventory? Inventory { get; set; }

    /// <summary>
    /// 职业进度数据 - 所有职业的等级和经验
    /// Profession progress data - levels and experience for all professions
    /// </summary>
    public Dictionary<string, BlazorIdle.Shared.Models.ProfessionProgress>? Professions { get; set; }

    /// <summary>
    /// 当前激活的战斗职业ID
    /// Currently active combat profession ID
    /// </summary>
    public string? ActiveCombatProfessionId { get; set; }
}

/// <summary>
/// 角色列表响应DTO
/// Character list response DTO
/// </summary>
public class CharacterListResponse
{
    /// <summary>
    /// 角色列表
    /// List of characters
    /// </summary>
    public List<BlazorIdle.Shared.Models.CharacterData> Characters { get; set; } = new();
    
    /// <summary>
    /// 最大角色槽位数
    /// Maximum character slots
    /// </summary>
    public int MaxSlots { get; set; }
    
    /// <summary>
    /// 已使用的角色槽位数
    /// Used character slots
    /// </summary>
    public int UsedSlots { get; set; }
    
    /// <summary>
    /// 是否可以创建更多角色
    /// Whether more characters can be created
    /// </summary>
    public bool CanCreateMore => UsedSlots < MaxSlots;
}

/// <summary>
/// 角色操作响应DTO
/// Character operation response DTO
/// </summary>
public class CharacterResponse
{
    /// <summary>
    /// 操作是否成功
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 消息（成功提示或错误信息）
    /// Message (success or error message)
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 角色数据（创建或获取成功时返回）
    /// Character data (returned on successful creation or retrieval)
    /// </summary>
    public BlazorIdle.Shared.Models.CharacterData? Character { get; set; }
}

/// <summary>
/// 切换战斗职业请求DTO
/// Switch combat profession request DTO
/// </summary>
public class SwitchProfessionRequest
{
    /// <summary>
    /// 要切换到的职业ID
    /// Profession ID to switch to
    /// </summary>
    public string ProfessionId { get; set; } = string.Empty;
}
