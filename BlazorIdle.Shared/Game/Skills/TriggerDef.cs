namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能触发器定义
    /// Skill trigger definition
    /// </summary>
    public sealed class TriggerDef
    {
        /// <summary>
        /// 触发时机
        /// When to trigger (OnAttackHit, OnAttackCrit, OnPostAttackWindow, OnPostCastWindow)
        /// </summary>
        public string When { get; set; } = "";

        /// <summary>
        /// 触发概率（0.0-1.0）
        /// Trigger probability (0.0-1.0)
        /// </summary>
        public double ProcChance { get; set; } = 1.0;

        /// <summary>
        /// 触发的技能ID
        /// Skill ID to trigger
        /// </summary>
        public string FireSkillId { get; set; } = "";

        /// <summary>
        /// 优先级（数值越大优先级越高）
        /// Priority (higher value = higher priority)
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 触发时的条件（可覆盖原技能条件）
        /// Conditions when triggering (can override original skill conditions)
        /// </summary>
        public SkillConditions? Conditions { get; set; }

        /// <summary>
        /// 是否忽略冷却和资源要求
        /// Whether to ignore cooldown and resource requirements
        /// </summary>
        public bool IgnoreRequirements { get; set; }
    }
}
