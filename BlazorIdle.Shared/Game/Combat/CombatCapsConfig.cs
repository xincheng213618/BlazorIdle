using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 战斗属性上限配置 - 从 Config/combat/caps.json 加载
    /// Combat attribute caps configuration - loaded from Config/combat/caps.json
    /// </summary>
    public sealed class CombatCapsConfig
    {
        /// <summary>
        /// 攻击力百分比上限
        /// Attack percentage cap
        /// </summary>
        [JsonPropertyName("attackPct")]
        public double AttackPct { get; set; } = 100.0;

        /// <summary>
        /// 特攻百分比上限
        /// Special attack percentage cap
        /// </summary>
        [JsonPropertyName("specialAttackPct")]
        public double SpecialAttackPct { get; set; } = 80.0;

        /// <summary>
        /// 生命百分比上限
        /// HP percentage cap
        /// </summary>
        [JsonPropertyName("hpPercent")]
        public double HpPercent { get; set; } = 100.0;

        /// <summary>
        /// 暴击率百分比上限
        /// Critical chance percentage cap
        /// </summary>
        [JsonPropertyName("critChancePct")]
        public double CritChancePct { get; set; } = 80.0;

        /// <summary>
        /// 暴击伤害加成百分比上限
        /// Critical damage bonus percentage cap
        /// </summary>
        [JsonPropertyName("critDamageBonusPct")]
        public double CritDamageBonusPct { get; set; } = 50.0;

        /// <summary>
        /// 盛体态势上限百分比上限
        /// Fortify max percentage cap
        /// </summary>
        [JsonPropertyName("fortifyMaxPct")]
        public double FortifyMaxPct { get; set; } = 20.0;

        /// <summary>
        /// 背水态势上限百分比上限
        /// Backwater max percentage cap
        /// </summary>
        [JsonPropertyName("backwaterMaxPct")]
        public double BackwaterMaxPct { get; set; } = 20.0;

        /// <summary>
        /// 追击百分比上限
        /// Chase percentage cap
        /// </summary>
        [JsonPropertyName("chasePct")]
        public double ChasePct { get; set; } = 30.0;

        /// <summary>
        /// 克制追击百分比上限
        /// Ken chase percentage cap
        /// </summary>
        [JsonPropertyName("kenChasePct")]
        public double KenChasePct { get; set; } = 20.0;

        /// <summary>
        /// 减伤百分比上限
        /// Damage reduction percentage cap
        /// </summary>
        [JsonPropertyName("damageReductionPct")]
        public double DamageReductionPct { get; set; } = 90.0;

        /// <summary>
        /// 固定追击伤害上限
        /// Flat chase damage cap
        /// </summary>
        [JsonPropertyName("chaseFlatCap")]
        public int ChaseFlatCap { get; set; } = 9999;

        #region Clamp Methods

        /// <summary>
        /// 裁剪攻击力百分比到上限
        /// Clamp attack percentage to cap
        /// </summary>
        public double ClampAttackPct(double value) => Math.Clamp(value, 0, AttackPct);

        /// <summary>
        /// 裁剪特攻百分比到上限
        /// Clamp special attack percentage to cap
        /// </summary>
        public double ClampSpecialAttackPct(double value) => Math.Clamp(value, 0, SpecialAttackPct);

        /// <summary>
        /// 裁剪生命百分比到上限
        /// Clamp HP percentage to cap
        /// </summary>
        public double ClampHpPercent(double value) => Math.Clamp(value, 0, HpPercent);

        /// <summary>
        /// 裁剪暴击率百分比到上限
        /// Clamp critical chance percentage to cap
        /// </summary>
        public double ClampCritChancePct(double value) => Math.Clamp(value, 0, CritChancePct);

        /// <summary>
        /// 裁剪暴击伤害加成百分比到上限
        /// Clamp critical damage bonus percentage to cap
        /// </summary>
        public double ClampCritDamageBonusPct(double value) => Math.Clamp(value, 0, CritDamageBonusPct);

        /// <summary>
        /// 裁剪盛体态势上限百分比到上限
        /// Clamp fortify max percentage to cap
        /// </summary>
        public double ClampFortifyMaxPct(double value) => Math.Clamp(value, 0, FortifyMaxPct);

        /// <summary>
        /// 裁剪背水态势上限百分比到上限
        /// Clamp backwater max percentage to cap
        /// </summary>
        public double ClampBackwaterMaxPct(double value) => Math.Clamp(value, 0, BackwaterMaxPct);

        /// <summary>
        /// 裁剪追击百分比到上限
        /// Clamp chase percentage to cap
        /// </summary>
        public double ClampChasePct(double value) => Math.Clamp(value, 0, ChasePct);

        /// <summary>
        /// 裁剪克制追击百分比到上限
        /// Clamp ken chase percentage to cap
        /// </summary>
        public double ClampKenChasePct(double value) => Math.Clamp(value, 0, KenChasePct);

        /// <summary>
        /// 裁剪减伤百分比到上限
        /// Clamp damage reduction percentage to cap
        /// </summary>
        public double ClampDamageReductionPct(double value) => Math.Clamp(value, 0, DamageReductionPct);

        /// <summary>
        /// 裁剪固定追击伤害到上限
        /// Clamp flat chase damage to cap
        /// </summary>
        public int ClampChaseFlat(int value) => Math.Clamp(value, 0, ChaseFlatCap);

        /// <summary>
        /// 应用所有上限裁剪到战斗属性
        /// Apply all caps to combat stats
        /// </summary>
        public CombatStats ApplyCaps(CombatStats stats)
        {
            return new CombatStats
            {
                AttackFinal = stats.AttackFinal,
                AttackPercent = ClampAttackPct(stats.AttackPercent),
                SpecialAttackPercent = ClampSpecialAttackPct(stats.SpecialAttackPercent),
                HPPercent = ClampHpPercent(stats.HPPercent),
                CritChancePercent = ClampCritChancePct(stats.CritChancePercent),
                CritDamageBonusPercent = ClampCritDamageBonusPct(stats.CritDamageBonusPercent),
                FortifyMaxPercent = ClampFortifyMaxPct(stats.FortifyMaxPercent),
                BackwaterMaxPercent = ClampBackwaterMaxPct(stats.BackwaterMaxPercent),
                ChasePercent = ClampChasePct(stats.ChasePercent),
                KenChasePercent = ClampKenChasePct(stats.KenChasePercent),
                ChaseFlat = ClampChaseFlat(stats.ChaseFlat),
                Armor = stats.Armor,
                DamageReductionPercent = ClampDamageReductionPct(stats.DamageReductionPercent)
            };
        }

        #endregion

        /// <summary>
        /// 创建默认配置
        /// Create default configuration
        /// </summary>
        public static CombatCapsConfig CreateDefault() => new CombatCapsConfig();
    }
}
