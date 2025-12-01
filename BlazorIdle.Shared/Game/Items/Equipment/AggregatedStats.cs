namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 汇总属性 - 从所有装备词条计算出的最终属性加成
    /// Aggregated stats - final stat bonuses calculated from all equipment affixes
    /// </summary>
    public sealed class AggregatedStats
    {
        /// <summary>
        /// 基础攻击力总和
        /// Total base attack
        /// </summary>
        public double TotalBaseAttack { get; set; } = 0;

        /// <summary>
        /// 基础生命值总和
        /// Total base HP
        /// </summary>
        public double TotalBaseHp { get; set; } = 0;

        /// <summary>
        /// 攻击力加成百分比（Element scope, 仅同元素生效）
        /// Attack percent bonus (Element scope)
        /// </summary>
        public double AttackPercent { get; set; } = 0;

        /// <summary>
        /// 特攻加成百分比（Element scope）
        /// Special attack percent bonus (Element scope)
        /// </summary>
        public double SpecialAttackPercent { get; set; } = 0;

        /// <summary>
        /// 生命加成百分比（Global scope）
        /// HP percent bonus (Global scope)
        /// </summary>
        public double HPPercent { get; set; } = 0;

        /// <summary>
        /// 急速加成百分比（Global scope）
        /// Haste percent bonus (Global scope)
        /// </summary>
        public double HastePercent { get; set; } = 0;

        /// <summary>
        /// 暴击率加成百分比（Global scope）
        /// Crit chance percent bonus (Global scope)
        /// </summary>
        public double CritChancePercent { get; set; } = 0;

        /// <summary>
        /// 暴击伤害加成百分比（Global scope）
        /// Crit damage percent bonus (Global scope)
        /// </summary>
        public double CritDamageBonusPercent { get; set; } = 0;

        /// <summary>
        /// 追击百分比（Element scope）
        /// Chase percent (Element scope)
        /// </summary>
        public double ChasePercent { get; set; } = 0;

        /// <summary>
        /// 追击固定值（Global scope）
        /// Chase flat value (Global scope)
        /// </summary>
        public double ChaseFlat { get; set; } = 0;

        /// <summary>
        /// 克制追击百分比（Element scope）
        /// Ken chase percent (Element scope)
        /// </summary>
        public double KenChasePercent { get; set; } = 0;

        /// <summary>
        /// 伤害减免百分比（Global scope）
        /// Damage reduction percent (Global scope)
        /// </summary>
        public double DamageReductionPercent { get; set; } = 0;

        /// <summary>
        /// 盛体增幅上限百分比（Global scope, Unique）
        /// Fortify max percent (Global scope, Unique)
        /// </summary>
        public double FortifyMaxPercent { get; set; } = 0;

        /// <summary>
        /// 背水意志上限百分比（Global scope, Unique）
        /// Backwater max percent (Global scope, Unique)
        /// </summary>
        public double BackwaterMaxPercent { get; set; } = 0;

        /// <summary>
        /// 从属性字典添加属性
        /// Add stats from attribute dictionary
        /// </summary>
        public void AddFromDictionary(Dictionary<string, double> effects)
        {
            foreach (var kvp in effects)
            {
                AddStat(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// 添加单个属性
        /// Add single stat
        /// </summary>
        public void AddStat(string statName, double value)
        {
            switch (statName)
            {
                case "AttackPercent":
                    AttackPercent += value;
                    break;
                case "SpecialAttackPercent":
                    SpecialAttackPercent += value;
                    break;
                case "HPPercent":
                    HPPercent += value;
                    break;
                case "HastePercent":
                    HastePercent += value;
                    break;
                case "CritChancePercent":
                    CritChancePercent += value;
                    break;
                case "CritDamageBonusPercent":
                    CritDamageBonusPercent += value;
                    break;
                case "ChasePercent":
                    ChasePercent += value;
                    break;
                case "ChaseFlat":
                    ChaseFlat += value;
                    break;
                case "KenChasePercent":
                    KenChasePercent += value;
                    break;
                case "DamageReductionPercent":
                    DamageReductionPercent += value;
                    break;
                case "FortifyMaxPercent":
                    // Unique: 只取最高值
                    FortifyMaxPercent = Math.Max(FortifyMaxPercent, value);
                    break;
                case "BackwaterMaxPercent":
                    // Unique: 只取最高值
                    BackwaterMaxPercent = Math.Max(BackwaterMaxPercent, value);
                    break;
            }
        }

        /// <summary>
        /// 创建空的汇总属性
        /// Create empty aggregated stats
        /// </summary>
        public static AggregatedStats CreateEmpty() => new AggregatedStats();

        /// <summary>
        /// 合并另一个汇总属性
        /// Merge with another aggregated stats
        /// </summary>
        public void MergeWith(AggregatedStats other)
        {
            TotalBaseAttack += other.TotalBaseAttack;
            TotalBaseHp += other.TotalBaseHp;
            AttackPercent += other.AttackPercent;
            SpecialAttackPercent += other.SpecialAttackPercent;
            HPPercent += other.HPPercent;
            HastePercent += other.HastePercent;
            CritChancePercent += other.CritChancePercent;
            CritDamageBonusPercent += other.CritDamageBonusPercent;
            ChasePercent += other.ChasePercent;
            ChaseFlat += other.ChaseFlat;
            KenChasePercent += other.KenChasePercent;
            DamageReductionPercent += other.DamageReductionPercent;
            // Unique stats: take max
            FortifyMaxPercent = Math.Max(FortifyMaxPercent, other.FortifyMaxPercent);
            BackwaterMaxPercent = Math.Max(BackwaterMaxPercent, other.BackwaterMaxPercent);
        }
    }
}
