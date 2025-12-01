namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 伤害计算器 - 实现6层伤害计算管线
    /// Damage calculator - implements 6-layer damage calculation pipeline
    /// 
    /// 计算管线：
    /// Pipeline:
    /// 1. 基础层：Base = AttackFinal × SkillCoef + SkillFlat
    /// 2. 浮动层：AfterVariance = Base × Variance
    /// 3. 主体层：AfterMain = AfterVariance × (1+Atk%) × (1+SpAtk%) × (1+Stance%)
    /// 4. 暴击层：AfterCrit = AfterMain × (isCrit ? BaseMultiplier × (1+CritBonus%) : 1)
    /// 5. 元素层：AfterElement = AfterCrit × ElementMult
    /// 6. 追击层：AfterChase = AfterElement + AfterElement×Chase% + AfterElement×KenChase% + ChaseFlat
    /// 7. 减伤层：Final = AfterChase × (1-DR%)
    /// </summary>
    public sealed class DamageCalculator
    {
        private readonly CombatCapsConfig _caps;
        private readonly CritConfig _critConfig;
        private readonly VarianceConfig _varianceConfig;
        private readonly StanceConfig _stanceConfig;
        private readonly ElementMatrix _elementMatrix;

        /// <summary>
        /// 创建伤害计算器
        /// Create damage calculator
        /// </summary>
        public DamageCalculator(
            CombatCapsConfig caps,
            CritConfig critConfig,
            VarianceConfig varianceConfig,
            StanceConfig stanceConfig,
            ElementMatrix elementMatrix)
        {
            _caps = caps ?? throw new ArgumentNullException(nameof(caps));
            _critConfig = critConfig ?? throw new ArgumentNullException(nameof(critConfig));
            _varianceConfig = varianceConfig ?? throw new ArgumentNullException(nameof(varianceConfig));
            _stanceConfig = stanceConfig ?? throw new ArgumentNullException(nameof(stanceConfig));
            _elementMatrix = elementMatrix ?? throw new ArgumentNullException(nameof(elementMatrix));
        }

        /// <summary>
        /// 计算伤害
        /// Calculate damage
        /// </summary>
        /// <param name="ctx">伤害上下文 / Damage context</param>
        /// <returns>伤害结果 / Damage result</returns>
        public DamageResult Calculate(DamageContext ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var result = new DamageResult();
            var stats = ctx.AttackerStats ?? CombatStats.CreateDefault();

            // 1. 基础层：Base = AttackFinal × SkillCoef + SkillFlat
            // Layer 1: Base = AttackFinal × SkillCoef + SkillFlat
            result.BaseDamage = ctx.AttackFinal * ctx.SkillCoef + ctx.SkillFlat;

            // 2. 浮动层：AfterVariance = Base × Variance
            // Layer 2: AfterVariance = Base × Variance
            double variance = _varianceConfig.GetVariance(ctx.Rng);
            result.AfterVariance = result.BaseDamage * variance;

            // 3. 主体层：AfterMain = AfterVariance × (1+Atk%) × (1+SpAtk%) × (1+Stance%)
            // Layer 3: AfterMain = AfterVariance × (1+Atk%) × (1+SpAtk%) × (1+Stance%)
            double atkPct = _caps.ClampAttackPct(stats.AttackPercent);
            double spAtkPct = _caps.ClampSpecialAttackPct(stats.SpecialAttackPercent);
            double fortifyMaxPct = _caps.ClampFortifyMaxPct(stats.FortifyMaxPercent);
            double backwaterMaxPct = _caps.ClampBackwaterMaxPct(stats.BackwaterMaxPercent);
            double stancePct = _stanceConfig.CalcStancePercent(ctx.AttackerHPRatio, fortifyMaxPct, backwaterMaxPct);
            result.StancePercent = stancePct;

            result.AfterMain = result.AfterVariance 
                * (1 + atkPct / 100.0) 
                * (1 + spAtkPct / 100.0) 
                * (1 + stancePct / 100.0);

            // 4. 暴击层：AfterCrit = AfterMain × (isCrit ? BaseMultiplier × (1+CritBonus%) : 1)
            // Layer 4: AfterCrit = AfterMain × (isCrit ? BaseMultiplier × (1+CritBonus%) : 1)
            double critChance = _caps.ClampCritChancePct(stats.CritChancePercent);
            double critBonus = _caps.ClampCritDamageBonusPct(stats.CritDamageBonusPercent);
            bool isCrit = ctx.Rng.NextDouble() * 100 < critChance;
            result.IsCrit = isCrit;

            if (isCrit)
            {
                result.CritMultiplier = _critConfig.BaseMultiplier * (1 + critBonus / 100.0);
                result.AfterCrit = result.AfterMain * result.CritMultiplier;
            }
            else
            {
                result.CritMultiplier = 1.0;
                result.AfterCrit = result.AfterMain;
            }

            // 5. 元素层：AfterElement = AfterCrit × ElementMult
            // Layer 5: AfterElement = AfterCrit × ElementMult
            result.ElementMultiplier = _elementMatrix.GetMultiplier(ctx.AttackerElement, ctx.DefenderElement);
            result.HasElementAdvantage = _elementMatrix.HasAdvantage(ctx.AttackerElement, ctx.DefenderElement);
            result.AfterElement = result.AfterCrit * result.ElementMultiplier;

            // 6. 追击层：AfterChase = AfterElement + AfterElement×Chase% + AfterElement×KenChase% + ChaseFlat
            // Layer 6: AfterChase = AfterElement + AfterElement×Chase% + AfterElement×KenChase% + ChaseFlat
            double chasePct = _caps.ClampChasePct(stats.ChasePercent);
            double kenChasePct = result.HasElementAdvantage ? _caps.ClampKenChasePct(stats.KenChasePercent) : 0;
            int chaseFlat = _caps.ClampChaseFlat(stats.ChaseFlat);

            result.AfterChase = result.AfterElement 
                + result.AfterElement * (chasePct / 100.0)
                + result.AfterElement * (kenChasePct / 100.0)
                + chaseFlat;

            // 7. 减伤层：Final = AfterChase × (1-DR%)
            // Layer 7: Final = AfterChase × (1-DR%)
            double drPct = _caps.ClampDamageReductionPct(ctx.DefenderDRPct);
            result.AfterDefense = result.AfterChase * (1 - drPct / 100.0);

            // 最终取整（最小为1）
            // Final rounding (minimum 1)
            result.FinalDamage = Math.Max(1, (int)Math.Round(result.AfterDefense));

            return result;
        }

        /// <summary>
        /// 计算伤害（无暴击判定，用于期望值计算）
        /// Calculate damage without crit roll (for expected value calculation)
        /// </summary>
        /// <param name="ctx">伤害上下文 / Damage context</param>
        /// <param name="forceCrit">强制暴击 / Force critical hit</param>
        /// <returns>伤害结果 / Damage result</returns>
        public DamageResult CalculateDeterministic(DamageContext ctx, bool forceCrit = false)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            var result = new DamageResult();
            var stats = ctx.AttackerStats ?? CombatStats.CreateDefault();

            // 1. 基础层
            result.BaseDamage = ctx.AttackFinal * ctx.SkillCoef + ctx.SkillFlat;

            // 2. 浮动层（使用中间值）
            double variance = (_varianceConfig.DefaultMin + _varianceConfig.DefaultMax) / 2.0;
            result.AfterVariance = result.BaseDamage * variance;

            // 3. 主体层
            double atkPct = _caps.ClampAttackPct(stats.AttackPercent);
            double spAtkPct = _caps.ClampSpecialAttackPct(stats.SpecialAttackPercent);
            double fortifyMaxPct = _caps.ClampFortifyMaxPct(stats.FortifyMaxPercent);
            double backwaterMaxPct = _caps.ClampBackwaterMaxPct(stats.BackwaterMaxPercent);
            double stancePct = _stanceConfig.CalcStancePercent(ctx.AttackerHPRatio, fortifyMaxPct, backwaterMaxPct);
            result.StancePercent = stancePct;

            result.AfterMain = result.AfterVariance 
                * (1 + atkPct / 100.0) 
                * (1 + spAtkPct / 100.0) 
                * (1 + stancePct / 100.0);

            // 4. 暴击层
            double critBonus = _caps.ClampCritDamageBonusPct(stats.CritDamageBonusPercent);
            result.IsCrit = forceCrit;

            if (forceCrit)
            {
                result.CritMultiplier = _critConfig.BaseMultiplier * (1 + critBonus / 100.0);
                result.AfterCrit = result.AfterMain * result.CritMultiplier;
            }
            else
            {
                result.CritMultiplier = 1.0;
                result.AfterCrit = result.AfterMain;
            }

            // 5. 元素层
            result.ElementMultiplier = _elementMatrix.GetMultiplier(ctx.AttackerElement, ctx.DefenderElement);
            result.HasElementAdvantage = _elementMatrix.HasAdvantage(ctx.AttackerElement, ctx.DefenderElement);
            result.AfterElement = result.AfterCrit * result.ElementMultiplier;

            // 6. 追击层
            double chasePct = _caps.ClampChasePct(stats.ChasePercent);
            double kenChasePct = result.HasElementAdvantage ? _caps.ClampKenChasePct(stats.KenChasePercent) : 0;
            int chaseFlat = _caps.ClampChaseFlat(stats.ChaseFlat);

            result.AfterChase = result.AfterElement 
                + result.AfterElement * (chasePct / 100.0)
                + result.AfterElement * (kenChasePct / 100.0)
                + chaseFlat;

            // 7. 减伤层
            double drPct = _caps.ClampDamageReductionPct(ctx.DefenderDRPct);
            result.AfterDefense = result.AfterChase * (1 - drPct / 100.0);

            // 最终取整
            result.FinalDamage = Math.Max(1, (int)Math.Round(result.AfterDefense));

            return result;
        }

        /// <summary>
        /// 创建默认伤害计算器（使用所有默认配置）
        /// Create default damage calculator (using all default configurations)
        /// </summary>
        public static DamageCalculator CreateDefault()
        {
            return new DamageCalculator(
                CombatCapsConfig.CreateDefault(),
                CritConfig.CreateDefault(),
                VarianceConfig.CreateDefault(),
                StanceConfig.CreateDefault(),
                ElementMatrix.CreateDefault()
            );
        }

        /// <summary>
        /// 从配置仓库创建伤害计算器
        /// Create damage calculator from config repository
        /// </summary>
        public static DamageCalculator CreateFromRepository()
        {
            return new DamageCalculator(
                CombatConfigRepository.LoadCombatCaps(),
                CombatConfigRepository.LoadCritConfig(),
                CombatConfigRepository.LoadVarianceConfig(),
                CombatConfigRepository.LoadStanceConfig(),
                CombatConfigRepository.LoadElementMatrix()
            );
        }
    }
}
