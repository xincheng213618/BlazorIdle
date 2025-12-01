using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 主手槽位配置
    /// Main hand slot configuration
    /// </summary>
    public sealed class MainHandSlotConfig
    {
        [JsonPropertyName("index")]
        public int Index { get; set; } = 0;

        [JsonPropertyName("name")]
        public string Name { get; set; } = "主手";

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("allowedItemTypes")]
        public List<string> AllowedItemTypes { get; set; } = new() { "weapon" };
    }

    /// <summary>
    /// 副槽位配置
    /// Sub slot configuration
    /// </summary>
    public sealed class SubSlotConfig
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 装备槽位配置模型 - 从 Config/equipment/slots.json 加载
    /// Equipment slot configuration model - loaded from Config/equipment/slots.json
    /// </summary>
    public sealed class SlotConfig
    {
        /// <summary>
        /// 主手槽位配置
        /// Main hand slot configuration
        /// </summary>
        [JsonPropertyName("mainHand")]
        public MainHandSlotConfig MainHand { get; set; } = new();

        /// <summary>
        /// 副槽位配置列表
        /// Sub slot configuration list
        /// </summary>
        [JsonPropertyName("subSlots")]
        public List<SubSlotConfig> SubSlots { get; set; } = new();

        /// <summary>
        /// 总槽位数
        /// Total slot count
        /// </summary>
        [JsonPropertyName("totalSlots")]
        public int TotalSlots { get; set; } = 10;

        /// <summary>
        /// 获取槽位名称
        /// Get slot name by index
        /// </summary>
        public string GetSlotName(int index)
        {
            if (index == 0)
                return MainHand.Name;

            var subSlot = SubSlots.FirstOrDefault(s => s.Index == index);
            return subSlot?.Name ?? $"槽位{index}";
        }

        /// <summary>
        /// 判断是否为主手槽位
        /// Check if index is main hand slot
        /// </summary>
        public bool IsMainHandSlot(int index) => index == MainHand.Index;
    }
}
