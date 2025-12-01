using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 战斗属性模型 - 汇总装备词条后的战斗属性
    /// Combat stats model - aggregated combat stats from equipment affixes
    /// </summary>
    public sealed class CombatStats
    {
        #region 基础属性 / Base Attributes

        /// <summary>
        /// 最终攻击力（基础面板值）
        /// Final attack power (base panel value)
        /// </summary>
        [JsonPropertyName("attackFinal")]
        public int AttackFinal { get; set; }

        /// <summary>
        /// 攻击力百分比加成
        /// Attack power percentage bonus
        /// </summary>
        [JsonPropertyName("attackPercent")]
        public double AttackPercent { get; set; }

        /// <summary>
        /// 特攻百分比加成
        /// Special attack percentage bonus
        /// </summary>
        [JsonPropertyName("specialAttackPercent")]
        public double SpecialAttackPercent { get; set; }

        /// <summary>
        /// 生命百分比加成（仅影响面板，不入伤害乘区）
        /// HP percentage bonus (only affects panel, not damage multiplier)
        /// </summary>
        [JsonPropertyName("hpPercent")]
        public double HPPercent { get; set; }

        #endregion

        #region 暴击属性 / Critical Hit Attributes

        /// <summary>
        /// 暴击率百分比
        /// Critical chance percentage
        /// </summary>
        [JsonPropertyName("critChancePercent")]
        public double CritChancePercent { get; set; }

        /// <summary>
        /// 暴击伤害加成百分比（叠加到基础 1.2 倍率）
        /// Critical damage bonus percentage (adds to base 1.2 multiplier)
        /// </summary>
        [JsonPropertyName("critDamageBonusPercent")]
        public double CritDamageBonusPercent { get; set; }

        #endregion

        #region 态势属性 / Stance Attributes

        /// <summary>
        /// 盛体态势上限百分比（HP >= 75% 时生效）
        /// Fortify stance max percentage (active when HP >= 75%)
        /// </summary>
        [JsonPropertyName("fortifyMaxPercent")]
        public double FortifyMaxPercent { get; set; }

        /// <summary>
        /// 背水态势上限百分比（HP <= 50% 时生效）
        /// Backwater stance max percentage (active when HP <= 50%)
        /// </summary>
        [JsonPropertyName("backwaterMaxPercent")]
        public double BackwaterMaxPercent { get; set; }

        #endregion

        #region 追击属性 / Chase Attributes

        /// <summary>
        /// 追击百分比（尾部加法）
        /// Chase percentage (tail additive)
        /// </summary>
        [JsonPropertyName("chasePercent")]
        public double ChasePercent { get; set; }

        /// <summary>
        /// 克制追击百分比（仅在元素克制时生效）
        /// Ken chase percentage (only active when element advantage)
        /// </summary>
        [JsonPropertyName("kenChasePercent")]
        public double KenChasePercent { get; set; }

        /// <summary>
        /// 固定追击伤害
        /// Flat chase damage
        /// </summary>
        [JsonPropertyName("chaseFlat")]
        public int ChaseFlat { get; set; }

        #endregion

        #region 防御属性 / Defense Attributes

        /// <summary>
        /// 减伤百分比（最终伤害 × (1 - 减伤%)）
        /// Damage reduction percentage (FinalDamage × (1 - DamageReduction%))
        /// </summary>
        [JsonPropertyName("damageReductionPercent")]
        public double DamageReductionPercent { get; set; }

        #endregion

        /// <summary>
        /// 创建默认战斗属性
        /// Create default combat stats
        /// </summary>
        public static CombatStats CreateDefault()
        {
            return new CombatStats
            {
                AttackFinal = 100,
                AttackPercent = 0,
                SpecialAttackPercent = 0,
                HPPercent = 0,
                CritChancePercent = 0,
                CritDamageBonusPercent = 0,
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
