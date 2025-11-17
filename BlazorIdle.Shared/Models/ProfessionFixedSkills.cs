using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 职业固定技能配置 - 存储在角色数据中
    /// Profession fixed skills - stored in character data
    /// 用于存储每个职业的普通攻击和特殊攻击技能ID
    /// Used to store normal attack and special attack skill IDs for each profession
    /// </summary>
    public class ProfessionFixedSkills
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
