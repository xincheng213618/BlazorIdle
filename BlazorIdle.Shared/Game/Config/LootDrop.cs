using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Config
{
    /// <summary>
    /// 掉落物配置 - 定义怪物死亡时掉落的物品
    /// Loot drop configuration - defines items dropped when monsters die
    /// </summary>
    public class LootDrop
    {
        /// <summary>
        /// 物品ID
        /// Item ID
        /// </summary>
        [JsonPropertyName("itemId")]
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        /// 最小掉落数量
        /// Minimum drop quantity
        /// </summary>
        [JsonPropertyName("minQuantity")]
        public int MinQuantity { get; set; } = 1;

        /// <summary>
        /// 最大掉落数量
        /// Maximum drop quantity
        /// </summary>
        [JsonPropertyName("maxQuantity")]
        public int MaxQuantity { get; set; } = 1;

        /// <summary>
        /// 掉落概率（0-1之间，1表示100%掉落）
        /// Drop chance (between 0-1, 1 means 100% drop)
        /// </summary>
        [JsonPropertyName("dropChance")]
        public double DropChance { get; set; } = 1.0;
    }
}
