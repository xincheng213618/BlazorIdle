using System.Text.Json.Serialization;
using BlazorIdle.Game.Skills;

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
    /// - 支持自定义触发条件，覆盖物品默认配置
    /// 
    /// Design notes:
    /// - Slot only stores item ID reference (reference mode)
    /// - Each battle use deducts from Inventory in real-time
    /// - Config UI shows real-time stock from inventory
    /// - Supports custom trigger conditions that override item defaults
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
        /// 是否使用自定义触发条件
        /// Whether to use custom trigger conditions
        /// 
        /// true: 使用 CustomTriggerConditions（即使为 null 也不使用默认条件）
        /// false: 使用物品默认的 triggerConditions
        /// 
        /// true: Use CustomTriggerConditions (even if null, won't use default)
        /// false: Use item's default triggerConditions
        /// </summary>
        [JsonPropertyName("useCustomTrigger")]
        public bool UseCustomTrigger { get; set; }

        /// <summary>
        /// 自定义触发条件 - 覆盖物品默认配置
        /// Custom trigger conditions - overrides item's default config
        /// 
        /// 当 UseCustomTrigger 为 true 时使用此条件
        /// When UseCustomTrigger is true, this condition is used
        /// 
        /// 支持的条件包括：
        /// - HpBelowPct: 生命值低于百分比
        /// - HpAbovePct: 生命值高于百分比
        /// - RequireBuffId: 需要拥有某个Buff
        /// - ForbidBuffId: 禁止拥有某个Buff
        /// - 等等...
        /// 
        /// Supported conditions include:
        /// - HpBelowPct: HP below percentage
        /// - HpAbovePct: HP above percentage  
        /// - RequireBuffId: Requires a buff
        /// - ForbidBuffId: Forbids a buff
        /// - etc...
        /// </summary>
        [JsonPropertyName("customTriggerConditions")]
        public SkillConditions? CustomTriggerConditions { get; set; }

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
                SkillId = skillId,
                UseCustomTrigger = false,
                CustomTriggerConditions = null
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
            UseCustomTrigger = false;
            CustomTriggerConditions = null;
        }

        /// <summary>
        /// 获取有效的触发条件（自定义或默认）
        /// Get effective trigger conditions (custom or default)
        /// </summary>
        /// <param name="defaultConditions">物品默认条件</param>
        /// <returns>应使用的触发条件</returns>
        public SkillConditions? GetEffectiveTriggerConditions(SkillConditions? defaultConditions)
        {
            return UseCustomTrigger ? CustomTriggerConditions : defaultConditions;
        }
    }
}
