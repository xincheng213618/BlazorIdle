using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Professions
{
    /// <summary>
    /// 职业初始战斗属性 - 定义职业的初始战斗加成（叠加装备词条后使用）
    /// Profession initial combat stats - defines initial combat bonuses (used after stacking equipment affixes)
    /// </summary>
    public sealed class InitialCombatStats
    {
        #region 暴击属性 / Critical Hit Attributes

        /// <summary>
        /// 初始暴击率百分比
        /// Initial critical chance percentage
        /// </summary>
        [JsonPropertyName("critChancePercent")]
        public double CritChancePercent { get; set; }

        /// <summary>
        /// 初始暴击伤害加成百分比
        /// Initial critical damage bonus percentage
        /// </summary>
        [JsonPropertyName("critDamageBonusPercent")]
        public double CritDamageBonusPercent { get; set; }

        #endregion

        #region 速度属性 / Speed Attributes

        /// <summary>
        /// 初始急速百分比
        /// Initial haste percentage
        /// </summary>
        [JsonPropertyName("hastePercent")]
        public double HastePercent { get; set; }

        #endregion

        #region 伤害加成属性 / Damage Bonus Attributes

        /// <summary>
        /// 初始攻击力百分比加成
        /// Initial attack percentage bonus
        /// </summary>
        [JsonPropertyName("attackPercent")]
        public double AttackPercent { get; set; }

        /// <summary>
        /// 初始特攻百分比加成
        /// Initial special attack percentage bonus
        /// </summary>
        [JsonPropertyName("specialAttackPercent")]
        public double SpecialAttackPercent { get; set; }

        /// <summary>
        /// 初始生命百分比加成
        /// Initial HP percentage bonus
        /// </summary>
        [JsonPropertyName("hpPercent")]
        public double HPPercent { get; set; }

        #endregion

        #region 态势属性 / Stance Attributes

        /// <summary>
        /// 初始盛体态势上限百分比
        /// Initial fortify stance max percentage
        /// </summary>
        [JsonPropertyName("fortifyMaxPercent")]
        public double FortifyMaxPercent { get; set; }

        /// <summary>
        /// 初始背水态势上限百分比
        /// Initial backwater stance max percentage
        /// </summary>
        [JsonPropertyName("backwaterMaxPercent")]
        public double BackwaterMaxPercent { get; set; }

        #endregion

        #region 追击属性 / Chase Attributes

        /// <summary>
        /// 初始追击百分比
        /// Initial chase percentage
        /// </summary>
        [JsonPropertyName("chasePercent")]
        public double ChasePercent { get; set; }

        /// <summary>
        /// 初始克制追击百分比
        /// Initial ken chase percentage
        /// </summary>
        [JsonPropertyName("kenChasePercent")]
        public double KenChasePercent { get; set; }

        /// <summary>
        /// 初始固定追击伤害
        /// Initial flat chase damage
        /// </summary>
        [JsonPropertyName("chaseFlat")]
        public int ChaseFlat { get; set; }

        #endregion

        #region 防御属性 / Defense Attributes

        /// <summary>
        /// 初始减伤百分比
        /// Initial damage reduction percentage
        /// </summary>
        [JsonPropertyName("damageReductionPercent")]
        public double DamageReductionPercent { get; set; }

        #endregion

        /// <summary>
        /// 创建默认初始战斗属性（全部为0）
        /// Create default initial combat stats (all zeros)
        /// </summary>
        public static InitialCombatStats CreateDefault()
        {
            return new InitialCombatStats
            {
                CritChancePercent = 0,
                CritDamageBonusPercent = 0,
                HastePercent = 0,
                AttackPercent = 0,
                SpecialAttackPercent = 0,
                HPPercent = 0,
                FortifyMaxPercent = 0,
                BackwaterMaxPercent = 0,
                ChasePercent = 0,
                KenChasePercent = 0,
                ChaseFlat = 0,
                DamageReductionPercent = 0
            };
        }
    }
}
