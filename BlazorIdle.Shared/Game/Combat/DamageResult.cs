namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 伤害计算结果 - 包含最终伤害和各层中间值
    /// Damage calculation result - contains final damage and intermediate values for each layer
    /// </summary>
    public sealed class DamageResult
    {
        #region 最终结果 / Final Results

        /// <summary>
        /// 最终伤害（取整后）
        /// Final damage (rounded)
        /// </summary>
        public int FinalDamage { get; set; }

        /// <summary>
        /// 是否暴击
        /// Whether this was a critical hit
        /// </summary>
        public bool IsCrit { get; set; }

        /// <summary>
        /// 元素克制乘区
        /// Element advantage multiplier
        /// </summary>
        public double ElementMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 是否有元素克制优势
        /// Whether attacker has element advantage
        /// </summary>
        public bool HasElementAdvantage { get; set; }

        #endregion

        #region 各层中间值（调试用）/ Intermediate Values (for debugging)

        /// <summary>
        /// 基础伤害（AttackFinal × SkillCoef + SkillFlat）
        /// Base damage (AttackFinal × SkillCoef + SkillFlat)
        /// </summary>
        public double BaseDamage { get; set; }

        /// <summary>
        /// 浮动后伤害
        /// Damage after variance
        /// </summary>
        public double AfterVariance { get; set; }

        /// <summary>
        /// 主体层后伤害（含Atk%、SpAtk%、Stance%）
        /// Damage after main layer (includes Atk%, SpAtk%, Stance%)
        /// </summary>
        public double AfterMain { get; set; }

        /// <summary>
        /// 暴击层后伤害
        /// Damage after critical layer
        /// </summary>
        public double AfterCrit { get; set; }

        /// <summary>
        /// 元素层后伤害
        /// Damage after element layer
        /// </summary>
        public double AfterElement { get; set; }

        /// <summary>
        /// 追击层后伤害（含Chase%、KenChase%、ChaseFlat）
        /// Damage after chase layer (includes Chase%, KenChase%, ChaseFlat)
        /// </summary>
        public double AfterChase { get; set; }

        /// <summary>
        /// 减伤层后伤害（最终值）
        /// Damage after defense layer (final value)
        /// </summary>
        public double AfterDefense { get; set; }

        #endregion

        #region 态势信息 / Stance Information

        /// <summary>
        /// 态势加成百分比
        /// Stance bonus percentage
        /// </summary>
        public double StancePercent { get; set; }

        /// <summary>
        /// 暴击倍率（包含基础倍率和加成）
        /// Critical multiplier (includes base multiplier and bonus)
        /// </summary>
        public double CritMultiplier { get; set; } = 1.0;

        #endregion

        /// <summary>
        /// 创建空结果
        /// Create empty result
        /// </summary>
        public static DamageResult Empty() => new DamageResult { FinalDamage = 0 };
    }
}
