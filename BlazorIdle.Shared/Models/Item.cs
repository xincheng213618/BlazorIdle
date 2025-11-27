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
        Consumable = 3,
        
        /// <summary>
        /// 技能书类物品 - 使用后学习技能
        /// Skill book items - learn skill on use
        /// </summary>
        SkillBook = 4
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

        /// <summary>
        /// 元数据 - 用于存储特殊物品的额外信息（如技能书的技能ID）
        /// Metadata - stores additional info for special items (like skill ID for skill books)
        /// </summary>
        [JsonPropertyName("metadata")]
        public ItemMetadata? Metadata { get; set; }

        /// <summary>
        /// 消耗品配置 - 定义消耗品的战斗行为（药水与食物系统）
        /// Consumable configuration - defines consumable battle behavior
        /// 
        /// 仅当 Type == ItemType.Consumable 时有效
        /// Only valid when Type == ItemType.Consumable
        /// 
        /// 包含：
        /// - category: 消耗品类别 (potion/food)
        /// - skillId: 触发的技能ID
        /// - triggerConditions: 自动触发条件
        /// - cooldownSec: 使用冷却时间
        /// </summary>
        [JsonPropertyName("consumableConfig")]
        public ConsumableConfig? ConsumableConfig { get; set; }
    }

    /// <summary>
    /// 物品元数据 - 存储特殊物品的额外信息
    /// Item metadata - stores additional information for special items
    /// </summary>
    public class ItemMetadata
    {
        /// <summary>
        /// 技能ID - 用于技能书物品
        /// Skill ID - for skill book items
        /// </summary>
        [JsonPropertyName("skillId")]
        public string? SkillId { get; set; }

        /// <summary>
        /// 使用后设置的账户标记 - 用于解锁条件
        /// Account flag set after use - for unlock conditions
        /// </summary>
        [JsonPropertyName("accountFlag")]
        public string? AccountFlag { get; set; }

        /// <summary>
        /// 使用效果描述
        /// Usage effect description
        /// </summary>
        [JsonPropertyName("effectDescription")]
        public string? EffectDescription { get; set; }
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
