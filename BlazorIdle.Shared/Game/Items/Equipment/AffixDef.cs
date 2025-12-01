using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 词条定义模型 - 从 Config/affixes/definitions.json 加载
    /// Affix definition model - loaded from Config/affixes/definitions.json
    /// </summary>
    public sealed class AffixDef
    {
        /// <summary>
        /// 词条唯一标识符
        /// Affix unique identifier
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 词条显示名称
        /// Affix display name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 词条作用域（global/element/hybrid）
        /// Affix scope (global/element/hybrid)
        /// </summary>
        [JsonPropertyName("scope")]
        public string ScopeString { get; set; } = "global";

        /// <summary>
        /// 获取解析后的作用域枚举
        /// Get parsed scope enum
        /// </summary>
        [JsonIgnore]
        public AffixScope Scope => ScopeString.ToLowerInvariant() switch
        {
            "element" => AffixScope.Element,
            "hybrid" => AffixScope.Hybrid,
            _ => AffixScope.Global
        };

        /// <summary>
        /// 混合作用域时，各字段的独立作用域定义
        /// Per-field scope definitions for hybrid scope
        /// Key: field name (e.g., "AttackPercent"), Value: scope ("global" or "element")
        /// </summary>
        [JsonPropertyName("perFieldScope")]
        public Dictionary<string, string>? PerFieldScope { get; set; }

        /// <summary>
        /// 获取指定字段的作用域
        /// Get scope for a specific field
        /// </summary>
        public AffixScope GetFieldScope(string fieldName)
        {
            if (Scope != AffixScope.Hybrid)
                return Scope;

            if (PerFieldScope != null && PerFieldScope.TryGetValue(fieldName, out var scopeStr))
            {
                return scopeStr.ToLowerInvariant() switch
                {
                    "element" => AffixScope.Element,
                    _ => AffixScope.Global
                };
            }

            return AffixScope.Global;
        }

        /// <summary>
        /// 词条等级数值列表（Lv1-Lv4，索引0对应Lv1）
        /// Affix level values list (Lv1-Lv4, index 0 = Lv1)
        /// 每个等级是一个字典，Key为属性名，Value为属性值
        /// Each level is a dictionary, Key is attribute name, Value is attribute value
        /// </summary>
        [JsonPropertyName("levels")]
        public List<Dictionary<string, double>> Levels { get; set; } = new();

        /// <summary>
        /// 词条标签（用于分类和筛选）
        /// Affix tags (for categorization and filtering)
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 唯一组标识（同组只能装备一个）
        /// Unique group identifier (only one per group can be equipped)
        /// </summary>
        [JsonPropertyName("uniqueGroup")]
        public string? UniqueGroup { get; set; }

        /// <summary>
        /// 装备数量限制（全身最多装备几件带该词条的装备）
        /// Equipment limit (max number of equipment with this affix)
        /// </summary>
        [JsonPropertyName("equipLimit")]
        public int? EquipLimit { get; set; }

        /// <summary>
        /// 获取指定等级的效果
        /// Get effects for a specific level
        /// </summary>
        /// <param name="level">等级（1-4）/ Level (1-4)</param>
        /// <returns>该等级的属性字典 / Attribute dictionary for this level</returns>
        /// <remarks>
        /// Returns empty dictionary if:
        /// - Levels list is empty (warning: affix has no level data)
        /// - Level is out of range (clamped to valid range)
        /// </remarks>
        public Dictionary<string, double> GetLevelEffects(int level)
        {
            if (Levels.Count == 0)
            {
                // Note: In production, consider using ILogger for proper logging
                System.Diagnostics.Debug.WriteLine($"[Warning] Affix '{Id}' has no level data defined.");
                return new Dictionary<string, double>();
            }

            int index = Math.Clamp(level - 1, 0, Levels.Count - 1);
            return Levels[index];
        }

        /// <summary>
        /// 检查词条是否有效（包含等级数据）
        /// Check if affix is valid (has level data)
        /// </summary>
        [JsonIgnore]
        public bool IsValid => !string.IsNullOrEmpty(Id) && Levels.Count > 0;

        /// <summary>
        /// 获取最大等级
        /// Get maximum level
        /// </summary>
        [JsonIgnore]
        public int MaxLevel => Levels.Count;

        /// <summary>
        /// 检查是否有指定标签
        /// Check if has a specific tag
        /// </summary>
        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }
}
