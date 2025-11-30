using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 暴击系统配置 - 从 Config/combat/crit.json 加载
    /// Critical hit system configuration - loaded from Config/combat/crit.json
    /// </summary>
    public sealed class CritConfig
    {
        /// <summary>
        /// 基础暴击倍率（暴击时的基础乘数）
        /// Base critical multiplier (base multiplier when critical hit)
        /// </summary>
        [JsonPropertyName("baseMultiplier")]
        public double BaseMultiplier { get; set; } = 1.2;

        /// <summary>
        /// 创建默认配置
        /// Create default configuration
        /// </summary>
        public static CritConfig CreateDefault() => new CritConfig { BaseMultiplier = 1.2 };
    }
}
