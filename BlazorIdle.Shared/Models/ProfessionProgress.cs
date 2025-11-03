using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 职业进度数据 - 追踪单个职业的等级和经验
    /// Profession progress data - tracks level and experience for a single profession
    /// </summary>
    public class ProfessionProgress
    {
        /// <summary>
        /// 职业ID - 关联到职业定义
        /// Profession ID - links to profession definition
        /// </summary>
        [JsonPropertyName("professionId")]
        public string ProfessionId { get; set; } = string.Empty;

        /// <summary>
        /// 职业类型 - 战斗或非战斗
        /// Profession type - combat or non-combat
        /// </summary>
        [JsonPropertyName("type")]
        public ProfessionType Type { get; set; }

        /// <summary>
        /// 当前等级
        /// Current level
        /// </summary>
        [JsonPropertyName("level")]
        public int Level { get; set; } = 1;

        /// <summary>
        /// 当前经验值
        /// Current experience points
        /// </summary>
        [JsonPropertyName("experience")]
        public long Experience { get; set; } = 0;

        /// <summary>
        /// 升级所需经验值
        /// Experience required for next level
        /// </summary>
        [JsonPropertyName("experienceToNext")]
        public long ExperienceToNext { get; set; } = 100;
    }

    /// <summary>
    /// 职业类型枚举
    /// Profession type enumeration
    /// </summary>
    public enum ProfessionType
    {
        /// <summary>
        /// 战斗职业 - 用于战斗
        /// Combat profession - used in battle
        /// </summary>
        Combat = 0,

        /// <summary>
        /// 非战斗职业 - 用于采集和制作
        /// Non-combat profession - used for gathering and crafting
        /// </summary>
        NonCombat = 1
    }
}
