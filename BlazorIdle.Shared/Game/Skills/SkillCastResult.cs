using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能施放结果 - 记录技能施放产生的所有效果
    /// Skill cast result - records all effects produced by skill casting
    /// </summary>
    public sealed class SkillCastResult
    {
        /// <summary>
        /// 造成的伤害值
        /// Damage dealt
        /// </summary>
        public int DamageDealt { get; set; }

        /// <summary>
        /// 是否暴击
        /// Whether it was a critical hit
        /// </summary>
        public bool IsCrit { get; set; }

        /// <summary>
        /// Bundle ID：记录本次技能施放所属的 bundle
        /// Bundle ID: records which bundle this skill cast belongs to
        /// </summary>
        public string? BundleId { get; set; }

        /// <summary>
        /// 资源变化（Phase 5：现已实现）
        /// Resource changes (Phase 5: now implemented)
        /// Key: 资源类型（如 "rage", "mana"）
        /// Value: 变化量（正数为增加，负数为消耗）
        /// </summary>
        public Dictionary<string, int> ResourceChanges { get; set; } = new();

        /// <summary>
        /// Buff 操作列表（Phase 5）
        /// List of buff operations to perform
        /// </summary>
        public List<BuffOperation> BuffOperations { get; set; } = new();

        /// <summary>
        /// 即时治疗量（Phase 5）
        /// Instant heal amount (if skill provides instant healing)
        /// </summary>
        public int InstantHeal { get; set; }

        /// <summary>
        /// 目标ID列表（Phase 3+ Integration）
        /// List of target IDs resolved by target selector
        /// </summary>
        public List<string> TargetIds { get; set; } = new();
    }
}
