using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能定义（Step 2 Phase 1: 扩展配置基础设施）
    /// Skill definition (Step 2 Phase 1: Extended configuration infrastructure)
    /// </summary>
    public sealed class SkillDef
    {
        /// <summary>
        /// 技能ID
        /// Skill ID
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// 技能名称
        /// Skill name
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 技能描述
        /// Skill description
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 技能图标（emoji 或文字）- Phase 10.3: 预留接口用于未来图标支持
        /// Skill icon (emoji or text) - Phase 10.3: Reserved interface for future icon support
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 技能类型：active（主动）或 passive（被动）
        /// Skill type: active or passive
        /// </summary>
        public string Type { get; set; } = "active";

        /// <summary>
        /// 槽位类型：active（主动槽）或 passive（被动槽）
        /// Slot type: active or passive
        /// </summary>
        public string SlotType { get; set; } = "active";

        /// <summary>
        /// 是否为固定技能（自动装配）
        /// Whether this is a fixed skill (auto-equipped)
        /// </summary>
        public bool Fixed { get; set; }

        /// <summary>
        /// 释放类型：instant（瞬发）或 cast（施法）
        /// Release type: instant or cast
        /// </summary>
        public string ReleaseType { get; set; } = "instant";

        /// <summary>
        /// 施法时间（秒）- 仅对 cast 类型有效
        /// Cast time in seconds - only valid for cast type
        /// </summary>
        public double CastTimeSec { get; set; } = 0;

        /// <summary>
        /// 是否占用GCD窗口（窗口互斥标识）
        /// Whether this skill occupies GCD window (window exclusive flag)
        /// </summary>
        public bool IsGcd { get; set; } = true;

        /// <summary>
        /// 施法完成后是否允许共触发
        /// Whether to allow co-trigger after cast completion
        /// </summary>
        public bool AllowCoTriggerAfterCast { get; set; }

        /// <summary>
        /// 目标选择策略
        /// Target selection policy
        /// 注意：AOE 判定由 targetPolicy 决定，不需要单独的 IsAoe 属性
        /// Note: AOE is determined by targetPolicy, no separate IsAoe property needed
        /// </summary>
        public string TargetPolicy { get; set; } = "current_target";

        /// <summary>
        /// 冷却时间（秒）
        /// Cooldown time in seconds
        /// </summary>
        public double CooldownSec { get; set; }

        /// <summary>
        /// 允许使用此技能的职业列表
        /// List of professions allowed to use this skill
        /// </summary>
        public List<string>? AllowedProfessions { get; set; }

        /// <summary>
        /// 解锁条件
        /// Unlock conditions
        /// </summary>
        public UnlockConfig? Unlock { get; set; }

        /// <summary>
        /// 施放条件
        /// Casting conditions
        /// </summary>
        public SkillConditions? Conditions { get; set; }

        /// <summary>
        /// 触发器定义
        /// Trigger definitions
        /// </summary>
        public List<TriggerDef>? Triggers { get; set; }

        // Phase 5: Buff 操作支持
        // Phase 5: Buff operation support

        /// <summary>
        /// 技能伤害定义（单段伤害，向后兼容）
        /// Skill damage definition (single hit, backward compatible)
        /// </summary>
        public DamageDef? Damage { get; set; }

        /// <summary>
        /// 多段伤害定义列表（支持连击、弹幕等多段伤害技能）
        /// Multi-hit damage definition list (supports combos, barrages, etc.)
        /// 如果设置了 Hits，则优先使用 Hits，忽略 Damage
        /// If Hits is set, it takes priority over Damage
        /// </summary>
        public List<DamageHit>? Hits { get; set; }

        /// <summary>
        /// 是否为多段伤害技能
        /// Whether this is a multi-hit skill
        /// </summary>
        public bool IsMultiHit => Hits != null && Hits.Count > 0;

        /// <summary>
        /// 基础伤害倍率（1.0 = 正常伤害） - 保留以向后兼容
        /// Base damage multiplier (1.0 = normal damage) - kept for backward compatibility
        /// </summary>
        public double DamageMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 即时治疗量（0 = 无治疗）
        /// Instant heal amount (0 = no healing)
        /// </summary>
        public int InstantHeal { get; set; }

        /// <summary>
        /// 资源消耗列表（新格式）
        /// Resource costs list (new format)
        /// </summary>
        public List<ResourceCost>? Costs { get; set; }

        /// <summary>
        /// 资源获得列表（新格式）
        /// Resource gains list (new format)
        /// </summary>
        public List<ResourceGain>? Gains { get; set; }

        /// <summary>
        /// 施法时执行的 Buff 操作（无论是否命中）
        /// Buff operations to perform on cast (regardless of hit)
        /// </summary>
        public List<BuffOperation> OnCastBuffs { get; set; } = new();

        /// <summary>
        /// 命中时执行的 Buff 操作（仅命中时）
        /// Buff operations to perform on hit (only when skill hits)
        /// </summary>
        public List<BuffOperation> OnHitBuffs { get; set; } = new();

        /// <summary>
        /// 暴击时执行的 Buff 操作（仅暴击时）
        /// Buff operations to perform on crit (only on critical hits)
        /// </summary>
        public List<BuffOperation> OnCritBuffs { get; set; } = new();

        /// <summary>
        /// 资源消耗（Key = 资源ID，Value = 消耗量） - 保留以向后兼容
        /// Resource costs (Key = resource ID, Value = amount to consume) - kept for backward compatibility
        /// </summary>
        public Dictionary<string, int> ResourceCosts { get; set; } = new();

        /// <summary>
        /// 资源获得（Key = 资源ID，Value = 获得量） - 保留以向后兼容
        /// Resource gains (Key = resource ID, Value = amount to gain) - kept for backward compatibility
        /// </summary>
        public Dictionary<string, int> ResourceGains { get; set; } = new();

        /// <summary>
        /// 是否必定命中（无未命中几率）
        /// Whether this skill always hits (no miss chance)
        /// </summary>
        public bool AlwaysHits { get; set; } = true;

        /// <summary>
        /// 是否可以暴击
        /// Whether this skill can critically strike
        /// </summary>
        public bool CanCrit { get; set; } = true;

        /// <summary>
        /// 购买配置 - 定义技能的价格和限购（Step 4）
        /// Purchase configuration - defines skill price and limits
        /// </summary>
        public SkillPurchaseConfig? Purchase { get; set; }
    }
}
