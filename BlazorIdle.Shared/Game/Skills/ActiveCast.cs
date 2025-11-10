namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 活跃的施法信息（预留）
    /// Active cast information (reserved)
    /// </summary>
    public sealed class ActiveCast
    {
        /// <summary>
        /// 正在施放的技能ID
        /// Skill ID being cast
        /// </summary>
        public string SkillId { get; set; } = "";

        /// <summary>
        /// 施法时间（秒）
        /// Cast time in seconds
        /// </summary>
        public double CastTimeSec { get; set; } = 0;

        /// <summary>
        /// 是否暂停攻击轨道
        /// Whether to pause attack track
        /// </summary>
        public bool PauseAttackTrack { get; set; } = false;

        /// <summary>
        /// 是否暂停特殊技能轨道
        /// Whether to pause special track
        /// </summary>
        public bool PauseSpecialTrack { get; set; } = false;

        /// <summary>
        /// 已经过的时间（秒）
        /// Elapsed time in seconds
        /// </summary>
        public double ElapsedSec { get; set; } = 0;
    }
}
