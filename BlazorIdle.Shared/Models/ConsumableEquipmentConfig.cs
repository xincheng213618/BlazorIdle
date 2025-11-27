using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 消耗品装备配置 - 管理角色的药水和食物槽位
    /// Consumable equipment configuration - manages player's potion and food slots
    /// 
    /// 设计说明：
    /// - 每个角色独立配置
    /// - 2个药水槽位 + 2个食物槽位
    /// - 槽位只保存物品ID引用，实时从背包扣除
    /// - 同一物品不能装备在多个槽位
    /// 
    /// Design notes:
    /// - Each character has independent configuration
    /// - 2 potion slots + 2 food slots
    /// - Slots store item ID reference, deduct from inventory in real-time
    /// - Same item cannot be equipped in multiple slots
    /// </summary>
    public sealed class ConsumableEquipmentConfig
    {
        /// <summary>
        /// 药水槽位ID常量数组 - 便于维护和扩展
        /// Potion slot ID constants - for easier maintenance and extension
        /// </summary>
        public static readonly string[] PotionSlotIds = { "potion_1", "potion_2" };

        /// <summary>
        /// 食物槽位ID常量数组 - 便于维护和扩展
        /// Food slot ID constants - for easier maintenance and extension
        /// </summary>
        public static readonly string[] FoodSlotIds = { "food_1", "food_2" };

        /// <summary>
        /// 药水槽位 (2个槽位: potion_1, potion_2)
        /// Potion slots (2 slots)
        /// 
        /// 药水主要提供持久/短期增益效果
        /// Potions mainly provide long/short-term buff effects
        /// </summary>
        [JsonPropertyName("potionSlots")]
        public Dictionary<string, ConsumableSlotData> PotionSlots { get; set; } = 
            PotionSlotIds.ToDictionary(id => id, _ => new ConsumableSlotData());

        /// <summary>
        /// 食物槽位 (2个槽位: food_1, food_2)
        /// Food slots (2 slots)
        /// 
        /// 食物主要提供瞬间/持续恢复效果
        /// Food mainly provides instant/continuous recovery effects
        /// </summary>
        [JsonPropertyName("foodSlots")]
        public Dictionary<string, ConsumableSlotData> FoodSlots { get; set; } = 
            FoodSlotIds.ToDictionary(id => id, _ => new ConsumableSlotData());

        /// <summary>
        /// 获取指定槽位的数据
        /// Get slot data by slot ID
        /// </summary>
        /// <param name="slotId">槽位ID (potion_1, potion_2, food_1, food_2)</param>
        /// <returns>槽位数据，如果不存在返回null</returns>
        public ConsumableSlotData? GetSlot(string slotId)
        {
            if (PotionSlots.TryGetValue(slotId, out var potionSlot))
                return potionSlot;
            if (FoodSlots.TryGetValue(slotId, out var foodSlot))
                return foodSlot;
            return null;
        }

        /// <summary>
        /// 检查物品是否已装备在任何槽位
        /// Check if an item is already equipped in any slot
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns>如果已装备返回true</returns>
        public bool IsItemEquipped(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            foreach (var slot in PotionSlots.Values)
            {
                if (slot.ItemId == itemId)
                    return true;
            }

            foreach (var slot in FoodSlots.Values)
            {
                if (slot.ItemId == itemId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获取物品装备的槽位ID
        /// Get the slot ID where an item is equipped
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns>槽位ID，如果未装备返回null</returns>
        public string? GetEquippedSlotId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;

            foreach (var kvp in PotionSlots)
            {
                if (kvp.Value.ItemId == itemId)
                    return kvp.Key;
            }

            foreach (var kvp in FoodSlots)
            {
                if (kvp.Value.ItemId == itemId)
                    return kvp.Key;
            }

            return null;
        }

        /// <summary>
        /// 获取所有已装备的物品ID列表
        /// Get list of all equipped item IDs
        /// </summary>
        /// <returns>已装备的物品ID列表</returns>
        public List<string> GetAllEquippedItemIds()
        {
            var result = new List<string>();

            foreach (var slot in PotionSlots.Values)
            {
                if (!string.IsNullOrEmpty(slot.ItemId))
                    result.Add(slot.ItemId);
            }

            foreach (var slot in FoodSlots.Values)
            {
                if (!string.IsNullOrEmpty(slot.ItemId))
                    result.Add(slot.ItemId);
            }

            return result;
        }
    }
}
