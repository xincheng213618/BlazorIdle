using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Config
{
    /// <summary>
    /// 经验配置 - 定义等级经验曲线和经验增益规则
    /// Experience configuration - defines level experience curve and experience gain rules
    /// </summary>
    public class ExperienceConfig
    {
        /// <summary>
        /// 等级经验曲线 - 定义每级所需的经验值
        /// Level experience curve - defines experience required for each level
        /// </summary>
        [JsonPropertyName("levelCurve")]
        public List<LevelExperienceRequirement> LevelCurve { get; set; } = new List<LevelExperienceRequirement>();

        /// <summary>
        /// 经验增益系数 - 用于全局经验加成（1.0 = 100%）
        /// Experience gain multiplier - for global experience bonus (1.0 = 100%)
        /// </summary>
        [JsonPropertyName("experienceMultiplier")]
        public double ExperienceMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 职业最大等级 - 限制职业可达到的最高等级
        /// Max profession level - limits the maximum level a profession can reach
        /// </summary>
        [JsonPropertyName("maxProfessionLevel")]
        public int MaxProfessionLevel { get; set; } = 100;
    }

    /// <summary>
    /// 等级经验需求 - 定义特定等级需要的经验值
    /// Level experience requirement - defines experience needed for a specific level
    /// </summary>
    public class LevelExperienceRequirement
    {
        /// <summary>
        /// 等级
        /// Level
        /// </summary>
        [JsonPropertyName("level")]
        public int Level { get; set; }

        /// <summary>
        /// 到达该等级所需的累计经验值
        /// Total experience required to reach this level
        /// </summary>
        [JsonPropertyName("experienceRequired")]
        public long ExperienceRequired { get; set; }
    }
}
