namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能定义
    /// Skill definition
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

        // 预留其他字段：
        // Reserved for other fields:
        // - Cost (资源消耗)
        // - Cooldown (冷却时间)
        // - Effects (技能效果)
        // - Range (施法距离)
        // - TargetType (目标类型)
        // 等等
    }
}
