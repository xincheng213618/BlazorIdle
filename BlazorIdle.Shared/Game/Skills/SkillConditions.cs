using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能施放条件
    /// Skill casting conditions
    /// </summary>
    public sealed class SkillConditions
    {
        /// <summary>
        /// HP低于此百分比时才能使用（例：30 表示HP < 30%）
        /// Can only use when HP is below this percentage
        /// </summary>
        public double? HpBelowPct { get; set; }

        /// <summary>
        /// HP高于此百分比时才能使用（例：50 表示HP > 50%）
        /// Can only use when HP is above this percentage
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
        /// </summary>
        public Dictionary<string, int>? RequireResource { get; set; }

        /// <summary>
        /// Buff层数要求 (Key: buffId, Value: minStacks)
        /// Required buff stack count
        /// </summary>
        public Dictionary<string, int>? RequireBuffStacks { get; set; }
    }
}
