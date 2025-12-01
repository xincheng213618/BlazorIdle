using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 伤害浮动配置 - 从 Config/combat/variance.json 加载
    /// Damage variance configuration - loaded from Config/combat/variance.json
    /// </summary>
    public sealed class VarianceConfig
    {
        /// <summary>
        /// 最小浮动系数（默认 0.95）
        /// Minimum variance multiplier (default 0.95)
        /// </summary>
        [JsonPropertyName("defaultMin")]
        public double DefaultMin { get; set; } = 0.95;

        /// <summary>
        /// 最大浮动系数（默认 1.05）
        /// Maximum variance multiplier (default 1.05)
        /// </summary>
        [JsonPropertyName("defaultMax")]
        public double DefaultMax { get; set; } = 1.05;

        /// <summary>
        /// 创建默认配置
        /// Create default configuration
        /// </summary>
        public static VarianceConfig CreateDefault() => new VarianceConfig
        {
            DefaultMin = 0.95,
            DefaultMax = 1.05
        };

        /// <summary>
        /// 生成随机浮动系数
        /// Generate random variance multiplier
        /// </summary>
        /// <param name="rng">随机数生成器 / Random number generator</param>
        /// <returns>浮动系数 / Variance multiplier</returns>
        public double GetVariance(Random rng)
        {
            return DefaultMin + rng.NextDouble() * (DefaultMax - DefaultMin);
        }
    }
}
