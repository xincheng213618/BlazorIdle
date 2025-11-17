using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 职业属性配置 - 定义职业的属性成长和基线
    /// </summary>
    public class ProfessionAttributeConfig
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("displayMainStat")]
        public string DisplayMainStat { get; set; } = string.Empty;

        [JsonPropertyName("mainStat")]
        public AttributeGrowthConfig MainStat { get; set; } = new();

        [JsonPropertyName("stamina")]
        public AttributeGrowthConfig Stamina { get; set; } = new();

        [JsonPropertyName("haste")]
        public AttributeGrowthConfig Haste { get; set; } = new();

        [JsonPropertyName("crit")]
        public AttributeGrowthConfig Crit { get; set; } = new();

        [JsonPropertyName("baseline")]
        public ProfessionBaseline Baseline { get; set; } = new();

        [JsonPropertyName("weights")]
        public AttributeWeights Weights { get; set; } = new();

        /// <summary>
        /// Phase 2.7: 职业资源配置 - 定义职业的资源类型、上限和初始值
        /// Phase 2.7: Profession resource configuration - defines resource type, max and initial value
        /// </summary>
        [JsonPropertyName("resource")]
        public ProfessionResourceConfig? Resource { get; set; }

        /// <summary>
        /// Phase 3+: 默认固定技能配置 - 用于角色创建时初始化
        /// Phase 3+: Default fixed skills - used for character creation initialization
        /// </summary>
        [JsonPropertyName("defaultFixedSkills")]
        public ProfessionDefaultFixedSkills? DefaultFixedSkills { get; set; }
    }

    /// <summary>
    /// 属性成长配置（用于主属性、耐力、急速、暴击等）
    /// </summary>
    public class AttributeGrowthConfig
    {
        [JsonPropertyName("base")]
        public double Base { get; set; }

        [JsonPropertyName("perLevel")]
        public double PerLevel { get; set; }
    }

    /// <summary>
    /// 职业基线属性
    /// </summary>
    public class ProfessionBaseline
    {
        [JsonPropertyName("baseDamage")]
        public int BaseDamage { get; set; }

        [JsonPropertyName("baseSpecialDamage")]
        public int BaseSpecialDamage { get; set; }

        [JsonPropertyName("baseAPS")]
        public double BaseAPS { get; set; }

        [JsonPropertyName("baseHp")]
        public int BaseHp { get; set; }

        [JsonPropertyName("baseSpecialIntervalSec")]
        public double BaseSpecialIntervalSec { get; set; }

        [JsonPropertyName("variance")]
        public double Variance { get; set; }

        [JsonPropertyName("reviveSec")]
        public double ReviveSec { get; set; }

        [JsonPropertyName("critMultiplier")]
        public double CritMultiplier { get; set; } = 2.0;
    }

    /// <summary>
    /// 属性权重配置 - 定义各属性的转换系数
    /// </summary>
    public class AttributeWeights
    {
        [JsonPropertyName("damagePerAttackPerMain")]
        public double DamagePerAttackPerMain { get; set; }

        [JsonPropertyName("specialDamagePerMain")]
        public double SpecialDamagePerMain { get; set; }

        [JsonPropertyName("maxHpPerStamina")]
        public double MaxHpPerStamina { get; set; }

        [JsonPropertyName("hasteRatingToPercent")]
        public double HasteRatingToPercent { get; set; }

        [JsonPropertyName("critRatingToPercent")]
        public double CritRatingToPercent { get; set; }

        [JsonPropertyName("critCap")]
        public double CritCap { get; set; } = 0.6;

        [JsonPropertyName("hasteCap")]
        public double HasteCap { get; set; } = 0.4;
    }

    /// <summary>
    /// Phase 2.7: 职业资源配置
    /// Phase 2.7: Profession resource configuration
    /// </summary>
    public class ProfessionResourceConfig
    {
        /// <summary>
        /// 资源ID（如 "rage", "mana", "energy"）
        /// Resource ID (e.g., "rage", "mana", "energy")
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "rage";

        /// <summary>
        /// 资源显示名称
        /// Resource display name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "怒气";

        /// <summary>
        /// 资源最大值
        /// Resource maximum value
        /// </summary>
        [JsonPropertyName("max")]
        public int Max { get; set; } = 10;

        /// <summary>
        /// 资源初始值
        /// Resource initial value
        /// </summary>
        [JsonPropertyName("initial")]
        public int Initial { get; set; } = 0;

        /// <summary>
        /// 每次攻击命中获得的资源量
        /// Resource gained per attack hit
        /// </summary>
        [JsonPropertyName("gainPerAttack")]
        public int GainPerAttack { get; set; } = 1;

        /// <summary>
        /// 暴击时额外获得的资源量
        /// Extra resource gained on critical hit
        /// </summary>
        [JsonPropertyName("gainPerCritExtra")]
        public int GainPerCritExtra { get; set; } = 1;
    }

    /// <summary>
    /// Phase 3+: 默认固定技能配置 - 用于角色创建时初始化
    /// Phase 3+: Default fixed skills configuration - used for character creation initialization
    /// </summary>
    public class ProfessionDefaultFixedSkills
    {
        /// <summary>
        /// 普通攻击技能ID
        /// Normal attack skill ID
        /// </summary>
        [JsonPropertyName("normalAttack")]
        public string NormalAttack { get; set; } = "";
        
        /// <summary>
        /// 特殊攻击技能ID
        /// Special attack skill ID
        /// </summary>
        [JsonPropertyName("specialAttack")]
        public string SpecialAttack { get; set; } = "";
    }
}
