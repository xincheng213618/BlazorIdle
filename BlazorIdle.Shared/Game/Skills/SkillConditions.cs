using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能施放条件
    /// Skill casting conditions
    /// 
    /// 注意：所有定义的条件必须同时满足（AND 逻辑），任意一个条件不满足则技能无法施放。
    /// Note: All defined conditions must be satisfied simultaneously (AND logic). 
    /// If any condition is not met, the skill cannot be cast.
    /// </summary>
    public sealed class SkillConditions
    {
        /// <summary>
        /// HP低于此百分比时才能使用（例：30 表示HP < 30%）
        /// Can only use when HP is below this percentage
        /// 
        /// 建议值范围：0.0 - 100.0
        /// Recommended range: 0.0 - 100.0
        /// </summary>
        public double? HpBelowPct { get; set; }

        /// <summary>
        /// HP高于此百分比时才能使用（例：50 表示HP > 50%）
        /// Can only use when HP is above this percentage
        /// 
        /// 建议值范围：0.0 - 100.0
        /// Recommended range: 0.0 - 100.0
        /// </summary>
        public double? HpAbovePct { get; set; }

        /// <summary>
        /// 必须拥有的Buff ID
        /// Required buff ID
        /// </summary>
        public string? RequireBuffId { get; set; }

        /// <summary>
        /// 禁止拥有的Buff ID
        /// Forbidden buff ID
        /// </summary>
        public string? ForbidBuffId { get; set; }

        /// <summary>
        /// 资源要求 (Key: bucketId, Value: minAmount)
        /// Resource requirements
        /// 
        /// 建议：minAmount 应为正整数（>= 0）
        /// Recommendation: minAmount should be a non-negative integer (>= 0)
        /// </summary>
        public Dictionary<string, int>? RequireResource { get; set; }

        /// <summary>
        /// Buff层数要求 (Key: buffId, Value: minStacks)
        /// Required buff stack count
        /// 
        /// 建议：minStacks 应为正整数（> 0）
        /// Recommendation: minStacks should be a positive integer (> 0)
        /// </summary>
        public Dictionary<string, int>? RequireBuffStacks { get; set; }
    }
}
