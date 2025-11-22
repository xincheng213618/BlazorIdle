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

        // ============================================================
        // Step3: 新增条件类型 - New Condition Types
        // ============================================================

        /// <summary>
        /// 敌人数量必须大于等于此值
        /// Enemy count must be greater than or equal to this value
        /// 
        /// Step3 Phase 3: 支持基于敌人数量的条件触发
        /// Step3 Phase 3: Support condition triggers based on enemy count
        /// </summary>
        public int? EnemyCountAbove { get; set; }

        /// <summary>
        /// 敌人数量必须小于等于此值
        /// Enemy count must be less than or equal to this value
        /// 
        /// Step3 Phase 3: 支持基于敌人数量的条件触发
        /// Step3 Phase 3: Support condition triggers based on enemy count
        /// </summary>
        public int? EnemyCountBelow { get; set; }

        /// <summary>
        /// 队友数量必须大于等于此值
        /// Ally count must be greater than or equal to this value
        /// 
        /// Step3 Phase 3: 支持基于队友数量的条件触发
        /// Step3 Phase 3: Support condition triggers based on ally count
        /// </summary>
        public int? AllyCountAbove { get; set; }

        /// <summary>
        /// 队友数量必须小于等于此值
        /// Ally count must be less than or equal to this value
        /// 
        /// Step3 Phase 3: 支持基于队友数量的条件触发
        /// Step3 Phase 3: Support condition triggers based on ally count
        /// </summary>
        public int? AllyCountBelow { get; set; }

        /// <summary>
        /// 任意队友的HP低于此百分比时满足条件
        /// Condition is met when any ally's HP is below this percentage
        /// 
        /// Step3 Phase 3: 支持基于队友HP的条件触发
        /// Step3 Phase 3: Support condition triggers based on ally HP
        /// 
        /// 建议值范围：0.0 - 100.0
        /// Recommended range: 0.0 - 100.0
        /// </summary>
        public double? AllyHpBelowPct { get; set; }

        /// <summary>
        /// Buff剩余时间必须小于此秒数（用于光环续期）
        /// Buff remaining time must be less than this value in seconds (for aura refresh)
        /// 
        /// Step3 Phase 4: 支持基于Buff时间的条件触发（光环自动续期）
        /// Step3 Phase 4: Support condition triggers based on buff time (aura auto-refresh)
        /// 
        /// 配合 BuffTimeCheckId 使用
        /// Used together with BuffTimeCheckId
        /// </summary>
        public double? BuffTimeRemainingSec { get; set; }

        /// <summary>
        /// 要检查剩余时间的Buff ID
        /// Buff ID to check remaining time
        /// 
        /// Step3 Phase 4: 支持基于Buff时间的条件触发（光环自动续期）
        /// Step3 Phase 4: Support condition triggers based on buff time (aura auto-refresh)
        /// 
        /// 配合 BuffTimeRemainingSec 使用
        /// Used together with BuffTimeRemainingSec
        /// </summary>
        public string? BuffTimeCheckId { get; set; }
    }
}
