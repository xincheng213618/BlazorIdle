using System.Collections.Generic;

namespace BlazorIdle.Game.Tracks
{
    /// <summary>
    /// Track 配置 - 定义 Track 的行为和绑定的技能
    /// Track configuration - defines track behavior and bound skills
    /// </summary>
    public sealed class TrackConfig
    {
        /// <summary>
        /// 绑定的技能列表（Track 触发时施放的技能）
        /// Bound skills (skills cast when track triggers)
        /// </summary>
        public List<string> BoundSkills { get; set; } = new();

        /// <summary>
        /// 触发时附加的瞬发技能（预留：on-fire triggers）
        /// Instant skills triggered on fire (reserved: on-fire triggers)
        /// </summary>
        public List<string> OnFireTriggers { get; set; } = new();

        /// <summary>
        /// 进度策略
        /// Progress policy
        /// </summary>
        public ProgressPolicy ProgressPolicy { get; set; } = ProgressPolicy.Presence;
    }
}
