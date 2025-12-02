using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Professions
{
    /// <summary>
    /// 职业基础面板属性 - 定义职业的基础战斗数值
    /// Profession base panel stats - defines base combat values for a profession
    /// </summary>
    public sealed class ProfessionBaseStats
    {
        /// <summary>
        /// 基础攻击力
        /// Base attack power
        /// </summary>
        [JsonPropertyName("baseAttack")]
        public int BaseAttack { get; set; }

        /// <summary>
        /// 基础生命值
        /// Base HP
        /// </summary>
        [JsonPropertyName("baseHP")]
        public int BaseHP { get; set; }

        /// <summary>
        /// 攻击速度（次/秒）
        /// Attack rate (attacks per second)
        /// </summary>
        [JsonPropertyName("attackRateAPS")]
        public double AttackRateAPS { get; set; }

        /// <summary>
        /// 伤害浮动比例
        /// Damage variance percentage
        /// </summary>
        [JsonPropertyName("variance")]
        public double Variance { get; set; }

        /// <summary>
        /// 复活时间（秒）
        /// Revive time (seconds)
        /// </summary>
        [JsonPropertyName("reviveSec")]
        public double ReviveSec { get; set; }

        /// <summary>
        /// 特殊技能间隔（秒）
        /// Special attack interval (seconds)
        /// </summary>
        [JsonPropertyName("specialIntervalSec")]
        public double SpecialIntervalSec { get; set; }

        /// <summary>
        /// 创建默认基础属性
        /// Create default base stats
        /// </summary>
        public static ProfessionBaseStats CreateDefault()
        {
            return new ProfessionBaseStats
            {
                BaseAttack = 100,
                BaseHP = 500,
                AttackRateAPS = 0.4,
                Variance = 0.05,
                ReviveSec = 5.0,
                SpecialIntervalSec = 5.0
            };
        }
    }
}
