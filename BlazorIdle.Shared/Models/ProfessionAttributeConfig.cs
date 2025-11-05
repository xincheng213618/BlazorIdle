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
}
