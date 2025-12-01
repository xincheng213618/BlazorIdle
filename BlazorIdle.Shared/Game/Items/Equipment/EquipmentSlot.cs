using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 装备槽 - 代表一个装备槽位
    /// Equipment slot - represents a single equipment slot
    /// </summary>
    public sealed class EquipmentSlot
    {
        /// <summary>
        /// 槽位索引 (0=主手, 1-9=副槽)
        /// Slot index (0=main hand, 1-9=sub slots)
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// 槽位中的装备实例ID
        /// Equipment instance ID in this slot
        /// </summary>
        [JsonPropertyName("equipmentInstanceId")]
        public string? EquipmentInstanceId { get; set; }

        /// <summary>
        /// 缓存的装备实例引用（运行时填充，不序列化）
        /// Cached equipment instance reference (filled at runtime, not serialized)
        /// </summary>
        [JsonIgnore]
        public EquipmentItem? Equipment { get; set; }

        /// <summary>
        /// 是否为主手槽位
        /// Whether this is the main hand slot
        /// </summary>
        [JsonIgnore]
        public bool IsMainHand => Index == 0;

        /// <summary>
        /// 槽位是否为空
        /// Whether the slot is empty
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(EquipmentInstanceId) && Equipment == null;

        /// <summary>
        /// 创建装备槽
        /// Create equipment slot
        /// </summary>
        public static EquipmentSlot Create(int index)
        {
            return new EquipmentSlot { Index = index };
        }

        /// <summary>
        /// 装备一件装备
        /// Equip an item
        /// </summary>
        public void Equip(EquipmentItem item)
        {
            EquipmentInstanceId = item.InstanceId;
            Equipment = item;
        }

        /// <summary>
        /// 卸下装备
        /// Unequip the item
        /// </summary>
        public EquipmentItem? Unequip()
        {
            var item = Equipment;
            EquipmentInstanceId = null;
            Equipment = null;
            return item;
        }
    }
}
