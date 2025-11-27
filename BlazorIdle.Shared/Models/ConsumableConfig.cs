using System;
using System.Text.Json.Serialization;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 消耗品类别枚举 - 类型安全的消耗品分类
    /// Consumable category enum - type-safe consumable classification
    /// </summary>
    public enum ConsumableCategory
    {
        /// <summary>
        /// 未知类别
        /// Unknown category
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 药水 - 主要提供持久/短期增益效果
        /// Potion - mainly provides long/short-term buff effects
        /// </summary>
        Potion = 1,

        /// <summary>
        /// 食物 - 主要提供瞬间/持续恢复效果
        /// Food - mainly provides instant/continuous recovery effects
        /// </summary>
        Food = 2
    }

    /// <summary>
    /// 消耗品配置 - 定义消耗品的战斗行为
    /// Consumable configuration - defines consumable battle behavior
    /// 
    /// 用于物品的 consumableConfig 字段，描述该物品作为消耗品的行为
    /// Used for item's consumableConfig field, describes item behavior as consumable
    /// </summary>
    public sealed class ConsumableConfig
    {
        /// <summary>
        /// 消耗品类别：potion（药水）或 food（食物）
        /// Consumable category: "potion" or "food"
        /// 
        /// - potion: 主要提供持久/短期增益效果
        /// - food: 主要提供瞬间/持续恢复效果
        /// 
        /// - potion: mainly provides long/short-term buff effects
        /// - food: mainly provides instant/continuous recovery effects
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; } = "food";

        /// <summary>
        /// 触发的技能ID（对应 consumableSkills.json 中的 id）
        /// Triggered skill ID (corresponds to id in consumableSkills.json)
        /// 
        /// 当消耗品被使用时，会执行这个技能
        /// When consumable is used, this skill will be executed
        /// </summary>
        [JsonPropertyName("skillId")]
        public string SkillId { get; set; } = string.Empty;

        /// <summary>
        /// 自动触发条件 - 复用现有的 SkillConditions
        /// Auto-trigger conditions - reuses existing SkillConditions
        /// 
        /// 例如：hpBelowPct = 50 表示HP低于50%时触发
        /// Example: hpBelowPct = 50 means trigger when HP below 50%
        /// </summary>
        [JsonPropertyName("triggerConditions")]
        public SkillConditions? TriggerConditions { get; set; }

        /// <summary>
        /// 使用冷却时间（秒）
        /// Usage cooldown time (seconds)
        /// 
        /// 冷却时间按物品ID追踪，不是按槽位
        /// Cooldown is tracked per item ID, not per slot
        /// </summary>
        [JsonPropertyName("cooldownSec")]
        public double CooldownSec { get; set; } = 30.0;

        /// <summary>
        /// 获取枚举类型的消耗品类别
        /// Get consumable category as enum type
        /// </summary>
        [JsonIgnore]
        public ConsumableCategory CategoryType => Category?.ToLowerInvariant() switch
        {
            "potion" => ConsumableCategory.Potion,
            "food" => ConsumableCategory.Food,
            _ => ConsumableCategory.Unknown
        };

        /// <summary>
        /// 检查是否为药水类
        /// Check if this is a potion
        /// </summary>
        [JsonIgnore]
        public bool IsPotion => CategoryType == ConsumableCategory.Potion;

        /// <summary>
        /// 检查是否为食物类
        /// Check if this is food
        /// </summary>
        [JsonIgnore]
        public bool IsFood => CategoryType == ConsumableCategory.Food;
    }
}
