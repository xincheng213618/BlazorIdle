using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Professions
{
    /// <summary>
    /// 职业资源配置 - 定义职业的资源系统
    /// Profession resource configuration - defines the resource system for a profession
    /// </summary>
    public sealed class ProfessionResource
    {
        /// <summary>
        /// 资源ID
        /// Resource ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        /// <summary>
        /// 资源名称
        /// Resource name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// 最大资源值
        /// Maximum resource value
        /// </summary>
        [JsonPropertyName("max")]
        public int Max { get; set; }

        /// <summary>
        /// 初始资源值
        /// Initial resource value
        /// </summary>
        [JsonPropertyName("initial")]
        public int Initial { get; set; }

        /// <summary>
        /// 每次攻击获得的资源
        /// Resource gained per attack
        /// </summary>
        [JsonPropertyName("gainPerAttack")]
        public int GainPerAttack { get; set; }

        /// <summary>
        /// 暴击额外获得的资源
        /// Extra resource gained on critical hit
        /// </summary>
        [JsonPropertyName("gainPerCritExtra")]
        public int GainPerCritExtra { get; set; }
    }

    /// <summary>
    /// 职业默认技能配置 - 定义职业的默认技能
    /// Profession default skills configuration - defines default skills for a profession
    /// </summary>
    public sealed class ProfessionDefaultSkills
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

    /// <summary>
    /// 职业完整配置 - 包含职业的所有配置信息
    /// Profession complete configuration - contains all configuration info for a profession
    /// </summary>
    public sealed class ProfessionConfig
    {
        /// <summary>
        /// 职业ID
        /// Profession ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        /// <summary>
        /// 职业名称
        /// Profession name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// 职业描述
        /// Profession description
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        /// <summary>
        /// 基础面板属性
        /// Base panel stats
        /// </summary>
        [JsonPropertyName("baseStats")]
        public ProfessionBaseStats BaseStats { get; set; } = new();

        /// <summary>
        /// 初始战斗属性
        /// Initial combat stats
        /// </summary>
        [JsonPropertyName("initialCombatStats")]
        public InitialCombatStats InitialCombatStats { get; set; } = new();

        /// <summary>
        /// 资源系统配置
        /// Resource system configuration
        /// </summary>
        [JsonPropertyName("resource")]
        public ProfessionResource Resource { get; set; } = new();

        /// <summary>
        /// 默认技能配置
        /// Default skills configuration
        /// </summary>
        [JsonPropertyName("defaultSkills")]
        public ProfessionDefaultSkills DefaultSkills { get; set; } = new();
    }
}
