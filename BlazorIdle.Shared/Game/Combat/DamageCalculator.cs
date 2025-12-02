namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 伤害计算器 - 实现7层伤害计算管线
    /// Damage calculator - implements 7-layer damage calculation pipeline
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
        private readonly CombatConfigs _configs;

        /// <summary>
        /// 创建伤害计算器（使用配置容器）
        /// Create damage calculator (using config container)
        /// </summary>
        public DamageCalculator(CombatConfigs configs)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
        }

        /// <summary>
        /// 创建伤害计算器（兼容旧接口）
        /// Create damage calculator (legacy interface compatibility)
        /// </summary>
        [Obsolete("Use DamageCalculator(CombatConfigs) constructor instead")]
        public DamageCalculator(
            CombatCapsConfig caps,
            CritConfig critConfig,
            VarianceConfig varianceConfig,
            StanceConfig stanceConfig,
            ElementMatrix elementMatrix)
            : this(new CombatConfigs(caps, critConfig, varianceConfig, stanceConfig, elementMatrix))
        {
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
            double variance = _configs.Variance.GetVariance(ctx.Rng);
            result.AfterVariance = result.BaseDamage * variance;

            // 3. 主体层：AfterMain = AfterVariance × (1+Atk%) × (1+SpAtk%) × (1+Stance%)
            // Layer 3: AfterMain = AfterVariance × (1+Atk%) × (1+SpAtk%) × (1+Stance%)
            // Buff System Optimization: 不裁剪 AttackPercent 和 SpecialAttackPercent，因为 Buff 效果不受上限限制
            // Buff System Optimization: Don't clamp AttackPercent and SpecialAttackPercent, as Buff effects bypass caps
            double atkPct = stats.AttackPercent;  // 不裁剪，允许超过上限
            double spAtkPct = stats.SpecialAttackPercent;  // 不裁剪，允许超过上限
            double fortifyMaxPct = stats.FortifyMaxPercent;  // 不裁剪，允许超过上限
            double backwaterMaxPct = stats.BackwaterMaxPercent;  // 不裁剪，允许超过上限
            double stancePct = _configs.Stance.CalcStancePercent(ctx.AttackerHPRatio, fortifyMaxPct, backwaterMaxPct);
            result.StancePercent = stancePct;

            result.AfterMain = result.AfterVariance 
                * (1 + atkPct / 100.0) 
                * (1 + spAtkPct / 100.0) 
                * (1 + stancePct / 100.0);

            // 4. 暴击层：AfterCrit = AfterMain × (isCrit ? BaseMultiplier × (1+CritBonus%) : 1)
            // Layer 4: AfterCrit = AfterMain × (isCrit ? BaseMultiplier × (1+CritBonus%) : 1)
            // 暴击率仍然需要裁剪（不能超过100%），但暴击伤害加成不裁剪
            // Crit chance still needs clamping (can't exceed 100%), but crit damage bonus bypasses caps
            double critChance = _configs.Caps.ClampCritChancePct(stats.CritChancePercent);
            double critBonus = stats.CritDamageBonusPercent;  // 不裁剪，允许超过上限
            bool isCrit = ctx.Rng.NextDouble() * 100 < critChance;
            result.IsCrit = isCrit;

            if (isCrit)
            {
                result.CritMultiplier = _configs.Crit.BaseMultiplier * (1 + critBonus / 100.0);
                result.AfterCrit = result.AfterMain * result.CritMultiplier;
            }
            else
            {
                result.CritMultiplier = 1.0;
                result.AfterCrit = result.AfterMain;
            }

            // 5. 元素层：AfterElement = AfterCrit × ElementMult
            // Layer 5: AfterElement = AfterCrit × ElementMult
            result.ElementMultiplier = _configs.Elements.GetMultiplier(ctx.AttackerElement, ctx.DefenderElement);
            result.HasElementAdvantage = _configs.Elements.HasAdvantage(ctx.AttackerElement, ctx.DefenderElement);
            result.AfterElement = result.AfterCrit * result.ElementMultiplier;

            // 6. 追击层：AfterChase = AfterElement + AfterElement×Chase% + AfterElement×KenChase% + ChaseFlat
            // Layer 6: AfterChase = AfterElement + AfterElement×Chase% + AfterElement×KenChase% + ChaseFlat
            // 追击属性不裁剪，允许 Buff 超过上限
            // Chase stats bypass caps for Buff effects
            double chasePct = stats.ChasePercent;  // 不裁剪
            double kenChasePct = result.HasElementAdvantage ? stats.KenChasePercent : 0;  // 不裁剪
            int chaseFlat = stats.ChaseFlat;  // 不裁剪

            result.AfterChase = result.AfterElement 
                + result.AfterElement * (chasePct / 100.0)
                + result.AfterElement * (kenChasePct / 100.0)
                + chaseFlat;

            // 7. 减伤层：Final = AfterChase × (1-DR%)
            // Layer 7: Final = AfterChase × (1-DR%)
            double drPct = _configs.Caps.ClampDamageReductionPct(ctx.DefenderDRPct);
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
            double variance = (_configs.Variance.DefaultMin + _configs.Variance.DefaultMax) / 2.0;
            result.AfterVariance = result.BaseDamage * variance;

            // 3. 主体层
            // Buff System Optimization: 不裁剪，允许超过上限
            double atkPct = stats.AttackPercent;
            double spAtkPct = stats.SpecialAttackPercent;
            double fortifyMaxPct = stats.FortifyMaxPercent;
            double backwaterMaxPct = stats.BackwaterMaxPercent;
            double stancePct = _configs.Stance.CalcStancePercent(ctx.AttackerHPRatio, fortifyMaxPct, backwaterMaxPct);
            result.StancePercent = stancePct;

            result.AfterMain = result.AfterVariance 
                * (1 + atkPct / 100.0) 
                * (1 + spAtkPct / 100.0) 
                * (1 + stancePct / 100.0);

            // 4. 暴击层
            // 暴击伤害加成不裁剪
            double critBonus = stats.CritDamageBonusPercent;
            result.IsCrit = forceCrit;

            if (forceCrit)
            {
                result.CritMultiplier = _configs.Crit.BaseMultiplier * (1 + critBonus / 100.0);
                result.AfterCrit = result.AfterMain * result.CritMultiplier;
            }
            else
            {
                result.CritMultiplier = 1.0;
                result.AfterCrit = result.AfterMain;
            }

            // 5. 元素层
            result.ElementMultiplier = _configs.Elements.GetMultiplier(ctx.AttackerElement, ctx.DefenderElement);
            result.HasElementAdvantage = _configs.Elements.HasAdvantage(ctx.AttackerElement, ctx.DefenderElement);
            result.AfterElement = result.AfterCrit * result.ElementMultiplier;

            // 6. 追击层
            // 追击属性不裁剪
            double chasePct = stats.ChasePercent;
            double kenChasePct = result.HasElementAdvantage ? stats.KenChasePercent : 0;
            int chaseFlat = stats.ChaseFlat;

            result.AfterChase = result.AfterElement 
                + result.AfterElement * (chasePct / 100.0)
                + result.AfterElement * (kenChasePct / 100.0)
                + chaseFlat;

            // 7. 减伤层
            double drPct = _configs.Caps.ClampDamageReductionPct(ctx.DefenderDRPct);
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
            return new DamageCalculator(CombatConfigs.CreateDefault());
        }

        /// <summary>
        /// 从配置仓库创建伤害计算器
        /// Create damage calculator from config repository
        /// </summary>
        public static DamageCalculator CreateFromRepository()
        {
            return new DamageCalculator(CombatConfigs.LoadFromRepository());
        }
    }
}
