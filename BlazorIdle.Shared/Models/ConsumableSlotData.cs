using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 消耗品槽位数据 - 存储单个槽位的装备信息
    /// Consumable slot data - stores equipment info for a single slot
    /// 
    /// 设计说明：
    /// - 槽位只保存物品ID引用（引用模式）
    /// - 每次战斗使用时，实时从背包（Inventory）扣除物品
    /// - 配置界面显示的数量是背包中该物品的实时库存
    /// 
    /// Design notes:
    /// - Slot only stores item ID reference (reference mode)
    /// - Each battle use deducts from Inventory in real-time
    /// - Config UI shows real-time stock from inventory
    /// </summary>
    public sealed class ConsumableSlotData
    {
        /// <summary>
        /// 装备的物品ID（对应 items.json 中的 id）
        /// Equipped item ID (corresponds to id in items.json)
        /// 
        /// 为null或空表示槽位未装备
        /// Null or empty means slot is not equipped
        /// </summary>
        [JsonPropertyName("itemId")]
        public string? ItemId { get; set; }

        /// <summary>
        /// 关联的技能ID（对应 consumableSkills.json 中的 id）
        /// Associated skill ID (corresponds to id in consumableSkills.json)
        /// 
        /// 从物品的 consumableConfig.skillId 自动获取
        /// Automatically derived from item's consumableConfig.skillId
        /// </summary>
        [JsonPropertyName("skillId")]
        public string? SkillId { get; set; }

        /// <summary>
        /// 检查槽位是否已装备物品
        /// Check if the slot has an item equipped
        /// </summary>
        [JsonIgnore]
        public bool IsEquipped => !string.IsNullOrEmpty(ItemId);

        /// <summary>
        /// 创建一个空槽位
        /// Create an empty slot
        /// </summary>
        public static ConsumableSlotData Empty() => new ConsumableSlotData();

        /// <summary>
        /// 创建一个已装备的槽位
        /// Create an equipped slot
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="skillId">技能ID</param>
        public static ConsumableSlotData Create(string itemId, string skillId)
        {
            return new ConsumableSlotData
            {
                ItemId = itemId,
                SkillId = skillId
            };
        }

        /// <summary>
        /// 清空槽位
        /// Clear the slot
        /// </summary>
        public void Clear()
        {
            ItemId = null;
            SkillId = null;
        }
    }
}
