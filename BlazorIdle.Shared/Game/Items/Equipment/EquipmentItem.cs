using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 装备实例 - 玩家持有的具体装备
    /// Equipment instance - a concrete equipment owned by player
    /// </summary>
    public sealed class EquipmentItem
    {
        /// <summary>
        /// 装备唯一实例ID
        /// Unique instance ID
        /// </summary>
        [JsonPropertyName("instanceId")]
        public string InstanceId { get; set; } = string.Empty;

        /// <summary>
        /// 装备模板ID（引用 EquipmentTemplate）
        /// Equipment template ID (references EquipmentTemplate)
        /// </summary>
        [JsonPropertyName("templateId")]
        public string TemplateId { get; set; } = string.Empty;

        /// <summary>
        /// 装备品质 (white/green/blue/purple/orange)
        /// Equipment quality
        /// </summary>
        [JsonPropertyName("quality")]
        public string Quality { get; set; } = "white";

        /// <summary>
        /// 装备上的词条实例列表
        /// Affix instances on this equipment
        /// </summary>
        [JsonPropertyName("affixes")]
        public List<AffixInstance> Affixes { get; set; } = new();

        /// <summary>
        /// 强化等级 (0-N)
        /// Reinforcement level (0-N)
        /// </summary>
        [JsonPropertyName("reinforceLevel")]
        public int ReinforceLevel { get; set; } = 0;

        /// <summary>
        /// 缓存的模板引用（运行时填充，不序列化）
        /// Cached template reference (filled at runtime, not serialized)
        /// </summary>
        [JsonIgnore]
        public EquipmentTemplate? Template { get; set; }

        /// <summary>
        /// 缓存的品质定义引用（运行时填充，不序列化）
        /// Cached quality definition reference (filled at runtime, not serialized)
        /// </summary>
        [JsonIgnore]
        public QualityDef? QualityDef { get; set; }

        /// <summary>
        /// 缓存的层级定义引用（运行时填充，不序列化）
        /// Cached tier definition reference (filled at runtime, not serialized)
        /// </summary>
        [JsonIgnore]
        public TierDef? TierDef { get; set; }

        /// <summary>
        /// 获取装备显示名称
        /// Get equipment display name
        /// </summary>
        [JsonIgnore]
        public string DisplayName => Template?.Name ?? TemplateId;

        /// <summary>
        /// 获取装备元素
        /// Get equipment element
        /// </summary>
        [JsonIgnore]
        public string Element => Template?.Element ?? "neutral";

        /// <summary>
        /// 获取武器类型
        /// Get weapon type
        /// </summary>
        [JsonIgnore]
        public string WeaponType => Template?.WeaponType ?? "sword";

        /// <summary>
        /// 获取装备层级
        /// Get equipment tier
        /// </summary>
        [JsonIgnore]
        public int Tier => Template?.Tier ?? 1;

        /// <summary>
        /// 获取品质对应的词条数量
        /// Get affix count for quality
        /// </summary>
        [JsonIgnore]
        public int AffixCount => QualityDef?.AffixCount ?? GetDefaultAffixCount(Quality);

        /// <summary>
        /// 获取基础攻击力（从层级配置）
        /// Get base attack from tier config
        /// </summary>
        [JsonIgnore]
        public double BaseAttack => TierDef?.BaseAttack ?? GetDefaultBaseAttack(Tier);

        /// <summary>
        /// 获取基础生命值（从层级配置）
        /// Get base HP from tier config
        /// </summary>
        [JsonIgnore]
        public double BaseHp => TierDef?.BaseHp ?? GetDefaultBaseHp(Tier);

        /// <summary>
        /// 获取品质颜色
        /// Get quality color
        /// </summary>
        [JsonIgnore]
        public string QualityColor => QualityDef?.Color ?? GetDefaultQualityColor(Quality);

        /// <summary>
        /// 根据品质获取默认词条数
        /// Get default affix count by quality
        /// </summary>
        private static int GetDefaultAffixCount(string quality) => quality.ToLowerInvariant() switch
        {
            "white" => 0,
            "green" => 1,
            "blue" => 2,
            "purple" => 3,
            "orange" => 4,
            _ => 0
        };

        /// <summary>
        /// 根据层级获取默认基础攻击力
        /// Get default base attack by tier
        /// </summary>
        private static double GetDefaultBaseAttack(int tier) => tier switch
        {
            1 => 100,
            2 => 200,
            3 => 320,
            4 => 450,
            _ => 100
        };

        /// <summary>
        /// 根据层级获取默认基础生命值
        /// Get default base HP by tier
        /// </summary>
        private static double GetDefaultBaseHp(int tier) => tier switch
        {
            1 => 50,
            2 => 100,
            3 => 160,
            4 => 225,
            _ => 50
        };

        /// <summary>
        /// 根据品质获取默认颜色
        /// Get default color by quality
        /// </summary>
        private static string GetDefaultQualityColor(string quality) => quality.ToLowerInvariant() switch
        {
            "white" => "#FFFFFF",
            "green" => "#00FF00",
            "blue" => "#0088FF",
            "purple" => "#AA00FF",
            "orange" => "#FF8800",
            _ => "#FFFFFF"
        };

        /// <summary>
        /// 创建装备实例
        /// Create equipment instance
        /// </summary>
        public static EquipmentItem Create(
            string templateId,
            string quality,
            EquipmentTemplate? template = null,
            QualityDef? qualityDef = null,
            TierDef? tierDef = null)
        {
            return new EquipmentItem
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                TemplateId = templateId,
                Quality = quality,
                Template = template,
                QualityDef = qualityDef,
                TierDef = tierDef,
                Affixes = new List<AffixInstance>()
            };
        }

        /// <summary>
        /// 克隆装备实例
        /// Clone equipment instance
        /// </summary>
        public EquipmentItem Clone()
        {
            return new EquipmentItem
            {
                InstanceId = Guid.NewGuid().ToString("N"), // 新实例ID
                TemplateId = TemplateId,
                Quality = Quality,
                Affixes = Affixes.Select(a => a.Clone()).ToList(),
                ReinforceLevel = ReinforceLevel,
                Template = Template,
                QualityDef = QualityDef,
                TierDef = TierDef
            };
        }
    }
}
