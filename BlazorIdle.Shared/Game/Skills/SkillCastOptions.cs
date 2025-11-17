namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能施放选项 - 控制技能施放的各种参数
    /// Skill cast options - controls various parameters for skill casting
    /// </summary>
    public sealed class SkillCastOptions
    {
        /// <summary>
        /// 强制暴击（预留：由后续 Buff/脉冲触发）
        /// Force critical hit (reserved: triggered by future Buff/pulse)
        /// </summary>
        public bool ForceCrit { get; set; } = false;

        /// <summary>
        /// 来源轨道：标识技能来自哪个触发源
        /// Source track: identifies which trigger source the skill comes from
        /// Options: "attack" | "special" | "enemy_attack" | "manual"
        /// </summary>
        public string SourceTrack { get; set; } = "";

        /// <summary>
        /// Bundle ID：用于关联同时施放的多个技能
        /// Bundle ID: used to associate multiple skills cast simultaneously
        /// </summary>
        public string? BundleId { get; set; }

        /// <summary>
        /// 施法者ID：用于目标选择（Phase 3+）
        /// Caster ID: used for target selection (Phase 3+)
        /// </summary>
        public string? CasterId { get; set; }
    }
}
