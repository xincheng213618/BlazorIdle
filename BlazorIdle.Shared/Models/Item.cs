using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 物品类型枚举 - 定义游戏中的物品类型
    /// Item type enumeration - defines types of items in the game
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// 货币类物品（如金币）
        /// Currency items (like gold coins)
        /// </summary>
        Currency = 1,
        
        /// <summary>
        /// 装备类物品
        /// Equipment items
        /// </summary>
        Equipment = 2,
        
        /// <summary>
        /// 消耗品类物品
        /// Consumable items
        /// </summary>
        Consumable = 3
    }

    /// <summary>
    /// 物品定义 - 描述游戏中的物品模板
    /// Item definition - describes item templates in the game
    /// </summary>
    public class ItemDefinition
    {
        /// <summary>
        /// 物品唯一ID
        /// Unique item ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 物品名称
        /// Item name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 物品描述
        /// Item description
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 物品类型
        /// Item type
        /// </summary>
        [JsonPropertyName("type")]
        public ItemType Type { get; set; } = ItemType.Currency;

        /// <summary>
        /// 物品图标（可选）
        /// Item icon (optional)
        /// </summary>
        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        /// <summary>
        /// 是否可堆叠
        /// Whether the item is stackable
        /// </summary>
        [JsonPropertyName("stackable")]
        public bool Stackable { get; set; } = true;

        /// <summary>
        /// 最大堆叠数量（仅当可堆叠时有效）
        /// Maximum stack size (only valid when stackable)
        /// </summary>
        [JsonPropertyName("maxStack")]
        public int MaxStack { get; set; } = 999999;
    }

    /// <summary>
    /// 物品实例 - 角色拥有的具体物品
    /// Item instance - specific items owned by a character
    /// </summary>
    public class ItemInstance
    {
        /// <summary>
        /// 物品ID（引用ItemDefinition）
        /// Item ID (references ItemDefinition)
        /// </summary>
        [JsonPropertyName("itemId")]
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        /// 物品数量
        /// Item quantity
        /// </summary>
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; } = 0;

        /// <summary>
        /// 获得时间（UTC）
        /// Acquisition time (UTC)
        /// </summary>
        [JsonPropertyName("acquiredAt")]
        public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
    }
}
