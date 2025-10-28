using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 角色数据模型 - 用于持久化和客户端管理
    /// Character data model - for persistence and client management
    /// </summary>
    public class CharacterData
    {
        /// <summary>
        /// 角色唯一ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 角色名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 职业ID（引用职业模板）
        /// </summary>
        [JsonPropertyName("professionId")]
        public string ProfessionId { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 角色属性（从职业模板初始化，后续可成长）
        // Character stats (initialized from profession template, can grow later)

        [JsonPropertyName("maxHp")]
        public int MaxHp { get; set; }

        [JsonPropertyName("attackRateAPS")]
        public double AttackRateAPS { get; set; }

        [JsonPropertyName("damagePerAttack")]
        public int DamagePerAttack { get; set; }

        [JsonPropertyName("hastePercent")]
        public double HastePercent { get; set; }

        [JsonPropertyName("specialIntervalSec")]
        public double SpecialIntervalSec { get; set; }

        [JsonPropertyName("specialDamage")]
        public int SpecialDamage { get; set; }

        [JsonPropertyName("critChancePercent")]
        public double CritChancePercent { get; set; }

        [JsonPropertyName("critMultiplier")]
        public double CritMultiplier { get; set; }

        [JsonPropertyName("variancePct")]
        public double VariancePct { get; set; }

        [JsonPropertyName("reviveSec")]
        public double ReviveSec { get; set; }
    }
}
