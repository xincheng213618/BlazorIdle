using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能定义（Phase 5: 扩展支持 Buff 操作）
    /// Skill definition (Phase 5: Extended with Buff operation support)
    /// </summary>
    public sealed class SkillDef
    {
        /// <summary>
        /// 技能ID
        /// Skill ID
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// 施法时间（秒）- 预留，Step 0 中为 0
        /// Cast time in seconds - reserved, 0 in Step 0
        /// </summary>
        public double CastTimeSec { get; set; } = 0;

        /// <summary>
        /// 是否为 AOE 技能（预留）
        /// Whether this is an AOE skill (reserved)
        /// </summary>
        public bool IsAoe { get; set; } = false;

        // Phase 5: Buff 操作支持
        // Phase 5: Buff operation support

        /// <summary>
        /// 基础伤害倍率（1.0 = 正常伤害）
        /// Base damage multiplier (1.0 = normal damage)
        /// </summary>
        public double DamageMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 即时治疗量（0 = 无治疗）
        /// Instant heal amount (0 = no healing)
        /// </summary>
        public int InstantHeal { get; set; }

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
        /// 资源消耗（Key = 资源ID，Value = 消耗量）
        /// Resource costs (Key = resource ID, Value = amount to consume)
        /// </summary>
        public Dictionary<string, int> ResourceCosts { get; set; } = new();

        /// <summary>
        /// 资源获得（Key = 资源ID，Value = 获得量）
        /// Resource gains (Key = resource ID, Value = amount to gain)
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
    }
}
