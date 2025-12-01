using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 装备配置 - 包含主手和9个副槽的装备配置
    /// Equipment loadout - contains main hand and 9 sub slots
    /// </summary>
    public sealed class EquipmentLoadout
    {
        /// <summary>
        /// 配置名称
        /// Loadout name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "默认配置";

        /// <summary>
        /// 所有槽位列表（索引0=主手，1-9=副槽）
        /// All slots list (index 0=main hand, 1-9=sub slots)
        /// </summary>
        [JsonPropertyName("slots")]
        public List<EquipmentSlot> Slots { get; set; } = new();

        /// <summary>
        /// 总槽位数
        /// Total slot count
        /// </summary>
        public const int TotalSlots = 10;

        /// <summary>
        /// 获取主手槽位
        /// Get main hand slot
        /// </summary>
        [JsonIgnore]
        public EquipmentSlot MainHand => Slots.Count > 0 ? Slots[0] : throw new InvalidOperationException("Loadout not initialized");

        /// <summary>
        /// 获取副槽列表
        /// Get sub slots list
        /// </summary>
        [JsonIgnore]
        public IEnumerable<EquipmentSlot> SubSlots => Slots.Skip(1);

        /// <summary>
        /// 获取主元素（由主手装备决定）
        /// Get main element (determined by main hand equipment)
        /// </summary>
        [JsonIgnore]
        public string MainElement => MainHand.Equipment?.Element ?? "neutral";

        /// <summary>
        /// 获取所有已装备的装备
        /// Get all equipped items
        /// </summary>
        [JsonIgnore]
        public IEnumerable<EquipmentItem> EquippedItems =>
            Slots.Where(s => s.Equipment != null).Select(s => s.Equipment!);

        /// <summary>
        /// 创建默认配置（10个空槽位）
        /// Create default loadout (10 empty slots)
        /// </summary>
        public static EquipmentLoadout CreateDefault(string name = "默认配置")
        {
            var loadout = new EquipmentLoadout { Name = name };
            for (int i = 0; i < TotalSlots; i++)
            {
                loadout.Slots.Add(EquipmentSlot.Create(i));
            }
            return loadout;
        }

        /// <summary>
        /// 获取指定索引的槽位
        /// Get slot by index
        /// </summary>
        public EquipmentSlot? GetSlot(int index)
        {
            if (index < 0 || index >= Slots.Count)
                return null;
            return Slots[index];
        }

        /// <summary>
        /// 装备一件装备到指定槽位
        /// Equip item to specific slot
        /// </summary>
        public bool Equip(int slotIndex, EquipmentItem item)
        {
            var slot = GetSlot(slotIndex);
            if (slot == null)
                return false;

            // 主手槽位只能装备武器
            if (slotIndex == 0)
            {
                // 所有装备都是武器，可以直接装备
            }

            slot.Equip(item);
            return true;
        }

        /// <summary>
        /// 卸下指定槽位的装备
        /// Unequip from specific slot
        /// </summary>
        public EquipmentItem? Unequip(int slotIndex)
        {
            var slot = GetSlot(slotIndex);
            return slot?.Unequip();
        }

        /// <summary>
        /// 交换两个槽位的装备
        /// Swap equipment between two slots
        /// </summary>
        public bool Swap(int slotIndex1, int slotIndex2)
        {
            var slot1 = GetSlot(slotIndex1);
            var slot2 = GetSlot(slotIndex2);

            if (slot1 == null || slot2 == null)
                return false;

            var temp = slot1.Equipment;
            var tempId = slot1.EquipmentInstanceId;

            slot1.Equipment = slot2.Equipment;
            slot1.EquipmentInstanceId = slot2.EquipmentInstanceId;

            slot2.Equipment = temp;
            slot2.EquipmentInstanceId = tempId;

            return true;
        }

        /// <summary>
        /// 获取所有同元素的装备数量
        /// Get count of equipment with same element as main hand
        /// </summary>
        public int GetSameElementCount()
        {
            var mainElement = MainElement;
            return EquippedItems.Count(e => 
                string.Equals(e.Element, mainElement, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取所有不同元素的装备数量
        /// Get count of equipment with different element from main hand
        /// </summary>
        public int GetDifferentElementCount()
        {
            var mainElement = MainElement;
            return EquippedItems.Count(e => 
                !string.Equals(e.Element, mainElement, StringComparison.OrdinalIgnoreCase));
        }
    }
}
