using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 词条规则配置 - 从 Config/affixes/rules.json 加载
    /// Affix rules configuration - loaded from Config/affixes/rules.json
    /// </summary>
    public sealed class AffixRules
    {
        /// <summary>
        /// 互斥规则：Key为词条ID，Value为与其互斥的词条ID列表
        /// Mutual exclusion rules: Key is affix ID, Value is list of mutually exclusive affix IDs
        /// 例如：assault 与 vital_force 和 crit_damage_bonus 互斥
        /// Example: assault is mutually exclusive with vital_force and crit_damage_bonus
        /// </summary>
        [JsonPropertyName("mutuallyExclusive")]
        public Dictionary<string, List<string>> MutuallyExclusive { get; set; } = new();

        /// <summary>
        /// 唯一组限制：Key为组名，Value为该组最多可装备的词条数量（通常为1）
        /// Unique group limit: Key is group name, Value is max number of affixes in this group (usually 1)
        /// 例如：fortify_unique 组只能装备 1 个词条
        /// Example: fortify_unique group can only have 1 affix
        /// </summary>
        [JsonPropertyName("uniqueGroup")]
        public Dictionary<string, int> UniqueGroup { get; set; } = new();

        /// <summary>
        /// 装备数量限制：Key为词条ID，Value为全身最多可装备的带该词条的装备数量
        /// Equipment limit: Key is affix ID, Value is max number of equipment with this affix
        /// 例如：chase_pct 最多装备 8 件
        /// Example: chase_pct can be on at most 8 equipment pieces
        /// </summary>
        [JsonPropertyName("equipLimit")]
        public Dictionary<string, int> EquipLimit { get; set; } = new();

        /// <summary>
        /// 单件约束：各种单件装备内的词条限制
        /// Per-item constraint: various constraints within a single equipment piece
        /// </summary>
        [JsonPropertyName("perItemConstraint")]
        public Dictionary<string, int> PerItemConstraint { get; set; } = new();

        /// <summary>
        /// 检查两个词条是否互斥
        /// Check if two affixes are mutually exclusive
        /// </summary>
        public bool AreMutuallyExclusive(string affixId1, string affixId2)
        {
            // Check if affixId1 lists affixId2 as exclusive
            if (MutuallyExclusive.TryGetValue(affixId1, out var exclusive1))
            {
                if (exclusive1.Contains(affixId2, StringComparer.OrdinalIgnoreCase))
                    return true;
            }

            // Check if affixId2 lists affixId1 as exclusive
            if (MutuallyExclusive.TryGetValue(affixId2, out var exclusive2))
            {
                if (exclusive2.Contains(affixId1, StringComparer.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获取词条的装备数量限制（如果有）
        /// Get equipment limit for an affix (if any)
        /// </summary>
        public int? GetEquipLimit(string affixId)
        {
            return EquipLimit.TryGetValue(affixId, out var limit) ? limit : null;
        }

        /// <summary>
        /// 获取唯一组的数量限制
        /// Get unique group limit
        /// </summary>
        public int? GetUniqueGroupLimit(string groupName)
        {
            return UniqueGroup.TryGetValue(groupName, out var limit) ? limit : null;
        }

        /// <summary>
        /// 获取单件追击类型词条的数量限制
        /// Get per-item chase type limit
        /// </summary>
        public int MaxChaseTypePerItem => 
            PerItemConstraint.TryGetValue("maxChaseTypePerItem", out var limit) ? limit : 1;

        /// <summary>
        /// 创建空的规则配置
        /// Create empty rules configuration
        /// </summary>
        public static AffixRules CreateEmpty() => new();
    }
}
