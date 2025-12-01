using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 装备品质定义模型 - 从 Config/equipment/quality.json 加载
    /// Equipment quality definition model - loaded from Config/equipment/quality.json
    /// </summary>
    public sealed class QualityDef
    {
        /// <summary>
        /// 品质唯一标识符 (white/green/blue/purple/orange)
        /// Quality unique identifier
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 品质显示名称
        /// Quality display name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 品质颜色（十六进制）
        /// Quality color (hex)
        /// </summary>
        [JsonPropertyName("color")]
        public string Color { get; set; } = "#FFFFFF";

        /// <summary>
        /// 该品质可拥有的词条数量
        /// Number of affixes for this quality
        /// </summary>
        [JsonPropertyName("affixCount")]
        public int AffixCount { get; set; } = 1;

        /// <summary>
        /// 品质权重（用于排序，数值越高越稀有）
        /// Quality weight (for sorting, higher = rarer)
        /// </summary>
        [JsonIgnore]
        public int Weight => Id.ToLowerInvariant() switch
        {
            "white" => 1,
            "green" => 2,
            "blue" => 3,
            "purple" => 4,
            "orange" => 5,
            _ => 0
        };
    }
}
