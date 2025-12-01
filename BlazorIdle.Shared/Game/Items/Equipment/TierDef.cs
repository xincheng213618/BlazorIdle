using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 装备层级定义 - 从 Config/equipment/tiers.json 加载
    /// Equipment tier definition - loaded from Config/equipment/tiers.json
    /// </summary>
    public sealed class TierDef
    {
        /// <summary>
        /// 层级编号 (1-4)
        /// Tier number (1-4)
        /// </summary>
        [JsonPropertyName("tier")]
        public int Tier { get; set; } = 1;

        /// <summary>
        /// 层级名称
        /// Tier name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 该层级的基础攻击力
        /// Base attack for this tier
        /// </summary>
        [JsonPropertyName("baseAttack")]
        public double BaseAttack { get; set; } = 100;

        /// <summary>
        /// 该层级的基础生命值
        /// Base HP for this tier
        /// </summary>
        [JsonPropertyName("baseHp")]
        public double BaseHp { get; set; } = 50;
    }

    /// <summary>
    /// 层级配置容器
    /// Tier configuration container
    /// </summary>
    public sealed class TierConfig
    {
        [JsonPropertyName("tiers")]
        public List<TierDef> Tiers { get; set; } = new();

        /// <summary>
        /// 获取指定层级的定义
        /// Get tier definition by tier number
        /// </summary>
        public TierDef? GetTier(int tier) => Tiers.FirstOrDefault(t => t.Tier == tier);
    }
}
