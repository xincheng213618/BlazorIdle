using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能解锁条件配置
    /// Skill unlock condition configuration
    /// </summary>
    public sealed class UnlockConfig
    {
        /// <summary>
        /// 最低等级要求
        /// Minimum level requirement
        /// </summary>
        public int MinLevel { get; set; }

        /// <summary>
        /// 账号标记要求（如成就、任务完成标记）
        /// Account flag requirements (e.g., achievements, quest completion flags)
        /// </summary>
        public List<string>? AccountFlags { get; set; }

        /// <summary>
        /// 职业等级要求 (Key: professionId, Value: minLevel)
        /// Profession level requirements
        /// </summary>
        public Dictionary<string, int>? RequiresProfessionLevel { get; set; }
    }
}
