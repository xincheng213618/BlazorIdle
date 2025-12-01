using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 词条实例 - 装备上的具体词条
    /// Affix instance - a concrete affix on equipment
    /// </summary>
    public sealed class AffixInstance
    {
        /// <summary>
        /// 词条定义ID（引用 AffixDef）
        /// Affix definition ID (references AffixDef)
        /// </summary>
        [JsonPropertyName("affixId")]
        public string AffixId { get; set; } = string.Empty;

        /// <summary>
        /// 当前词条等级（1-4，对应 Lv1-Lv4）
        /// Current affix level (1-4, corresponds to Lv1-Lv4)
        /// </summary>
        [JsonPropertyName("level")]
        public int Level { get; set; } = 1;

        /// <summary>
        /// 缓存的词条定义引用（运行时填充，不序列化）
        /// Cached affix definition reference (filled at runtime, not serialized)
        /// </summary>
        [JsonIgnore]
        public AffixDef? Definition { get; set; }

        /// <summary>
        /// 获取当前等级的效果
        /// Get effects for current level
        /// </summary>
        /// <returns>属性字典，Key为属性名，Value为属性值 / Attribute dictionary</returns>
        public Dictionary<string, double> GetEffects()
        {
            if (Definition == null)
                return new Dictionary<string, double>();

            return Definition.GetLevelEffects(Level);
        }

        /// <summary>
        /// 获取当前等级的效果（带元素过滤）
        /// Get effects for current level with element filtering
        /// </summary>
        /// <param name="equipmentElement">装备元素 / Equipment element</param>
        /// <param name="mainElement">主元素 / Main element</param>
        /// <returns>激活的属性字典 / Active attribute dictionary</returns>
        public Dictionary<string, double> GetActiveEffects(string equipmentElement, string mainElement)
        {
            if (Definition == null)
                return new Dictionary<string, double>();

            var allEffects = Definition.GetLevelEffects(Level);
            var activeEffects = new Dictionary<string, double>();
            bool elementMatch = string.Equals(equipmentElement, mainElement, StringComparison.OrdinalIgnoreCase);

            foreach (var kvp in allEffects)
            {
                var fieldScope = Definition.GetFieldScope(kvp.Key);
                
                // Global scope always active, Element scope requires element match
                if (fieldScope == AffixScope.Global || (fieldScope == AffixScope.Element && elementMatch))
                {
                    activeEffects[kvp.Key] = kvp.Value;
                }
            }

            return activeEffects;
        }

        /// <summary>
        /// 获取当前等级的未激活效果（用于预览）
        /// Get inactive effects for current level (for preview)
        /// </summary>
        /// <param name="equipmentElement">装备元素 / Equipment element</param>
        /// <param name="mainElement">主元素 / Main element</param>
        /// <returns>未激活的属性字典 / Inactive attribute dictionary</returns>
        public Dictionary<string, double> GetInactiveEffects(string equipmentElement, string mainElement)
        {
            if (Definition == null)
                return new Dictionary<string, double>();

            var allEffects = Definition.GetLevelEffects(Level);
            var inactiveEffects = new Dictionary<string, double>();
            bool elementMatch = string.Equals(equipmentElement, mainElement, StringComparison.OrdinalIgnoreCase);

            foreach (var kvp in allEffects)
            {
                var fieldScope = Definition.GetFieldScope(kvp.Key);
                
                // Element scope without match is inactive
                if (fieldScope == AffixScope.Element && !elementMatch)
                {
                    inactiveEffects[kvp.Key] = kvp.Value;
                }
            }

            return inactiveEffects;
        }

        /// <summary>
        /// 是否可以升级
        /// Whether can level up
        /// </summary>
        public bool CanLevelUp => Definition != null && Level < Definition.MaxLevel;

        /// <summary>
        /// 升级词条（强化时调用）
        /// Level up the affix (called during reinforcement)
        /// </summary>
        /// <returns>是否成功升级 / Whether level up was successful</returns>
        public bool LevelUp()
        {
            if (!CanLevelUp)
                return false;

            Level++;
            return true;
        }

        /// <summary>
        /// 获取词条显示名称
        /// Get affix display name
        /// </summary>
        [JsonIgnore]
        public string DisplayName => Definition?.Name ?? AffixId;

        /// <summary>
        /// 创建词条实例
        /// Create affix instance
        /// </summary>
        public static AffixInstance Create(string affixId, int level = 1, AffixDef? definition = null)
        {
            return new AffixInstance
            {
                AffixId = affixId,
                Level = Math.Max(1, level),
                Definition = definition
            };
        }

        /// <summary>
        /// 克隆词条实例
        /// Clone affix instance
        /// </summary>
        public AffixInstance Clone()
        {
            return new AffixInstance
            {
                AffixId = AffixId,
                Level = Level,
                Definition = Definition
            };
        }
    }
}
