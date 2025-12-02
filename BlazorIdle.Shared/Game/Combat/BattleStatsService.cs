using BlazorIdle.Game.Items.Equipment;
using BlazorIdle.Game.Professions;

namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 战斗属性服务 - 提供战斗场景下的属性计算和期望伤害模拟
    /// Battle stats service - provides attribute calculation and expected damage simulation for battle scenarios
    /// </summary>
    public sealed class BattleStatsService
    {
        private readonly CharacterStatsCalculator _statsCalculator;
        private readonly DamageCalculator _damageCalculator;
        private readonly CombatCapsConfig _caps;

        /// <summary>
        /// 创建战斗属性服务
        /// Create battle stats service
        /// </summary>
        public BattleStatsService(
            CharacterStatsCalculator? statsCalculator = null,
            DamageCalculator? damageCalculator = null,
            CombatCapsConfig? caps = null)
        {
            _statsCalculator = statsCalculator ?? CharacterStatsCalculator.CreateDefault();
            _damageCalculator = damageCalculator ?? DamageCalculator.CreateFromRepository();
            _caps = caps ?? CombatConfigRepository.LoadCombatCaps();
        }

        /// <summary>
        /// 获取战斗属性包（包含最终属性、生命值、攻速、元素）
        /// Get battle stats package (includes final stats, max HP, attack rate, element)
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <param name="loadout">装备配置 / Equipment loadout (optional)</param>
        /// <param name="extraStats">额外加成（用于测试）/ Extra bonuses (for testing)</param>
        /// <param name="element">角色元素 / Character element (default neutral)</param>
        /// <returns>战斗属性包 / Battle stats package</returns>
        public BattleStatsPackage GetBattleStatsPackage(
            string professionId,
            EquipmentLoadout? loadout = null,
            ExtraCombatStats? extraStats = null,
            string? element = null)
        {
            // 计算基础属性
            var baseStats = _statsCalculator.CalculateFinalStats(professionId, loadout);
            var baseMaxHp = _statsCalculator.CalculateFinalMaxHp(professionId, loadout);
            var baseAttackRate = _statsCalculator.CalculateFinalAttackRate(professionId, loadout);

            // 应用额外加成（用于测试模式）
            var finalStats = ApplyExtraStats(baseStats, extraStats);
            var finalMaxHp = baseMaxHp + (extraStats?.ExtraMaxHp ?? 0);

            return new BattleStatsPackage
            {
                MaxHp = finalMaxHp,
                AttackRate = baseAttackRate,
                CombatStats = finalStats,
                Element = element ?? ElementIds.Neutral,
                ProfessionId = professionId
            };
        }

        /// <summary>
        /// 计算期望伤害（模拟不同血量条件）
        /// Calculate expected damage (simulate different HP conditions)
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <param name="loadout">装备配置 / Equipment loadout (optional)</param>
        /// <param name="hpRatio">血量比例 0-1 / HP ratio 0-1</param>
        /// <param name="defenderElement">防御者元素 / Defender element (default neutral)</param>
        /// <param name="defenderDRPct">防御者减伤% / Defender damage reduction %</param>
        /// <param name="skillCoef">技能系数 / Skill coefficient (default 1.0)</param>
        /// <param name="skillFlat">技能固定伤害 / Skill flat damage (default 0)</param>
        /// <param name="extraStats">额外加成 / Extra bonuses</param>
        /// <returns>期望伤害结果 / Expected damage result</returns>
        public ExpectedDamageResult CalculateExpectedDamage(
            string professionId,
            EquipmentLoadout? loadout = null,
            double hpRatio = 1.0,
            string? defenderElement = null,
            double defenderDRPct = 0,
            double skillCoef = 1.0,
            int skillFlat = 0,
            ExtraCombatStats? extraStats = null)
        {
            var package = GetBattleStatsPackage(professionId, loadout, extraStats);
            var combatStats = package.CombatStats;

            // 创建伤害上下文（非暴击）
            var ctx = new DamageContext
            {
                AttackFinal = combatStats.AttackFinal,
                AttackerStats = combatStats,
                AttackerHPRatio = hpRatio,
                AttackerElement = package.Element,
                DefenderElement = defenderElement ?? ElementIds.Neutral,
                DefenderDRPct = defenderDRPct,
                SkillCoef = skillCoef,
                SkillFlat = skillFlat,
                Rng = new Random(42) // 固定种子用于确定性计算
            };

            // 计算非暴击伤害
            var normalResult = _damageCalculator.CalculateDeterministic(ctx, forceCrit: false);

            // 计算暴击伤害
            var critResult = _damageCalculator.CalculateDeterministic(ctx, forceCrit: true);

            // 计算期望伤害 = 非暴击伤害 × (1-暴击率) + 暴击伤害 × 暴击率
            double critChance = Math.Min(combatStats.CritChancePercent, _caps.CritChancePct) / 100.0;
            double expectedDamage = normalResult.FinalDamage * (1 - critChance) + critResult.FinalDamage * critChance;

            return new ExpectedDamageResult
            {
                ExpectedDamage = expectedDamage,
                NormalDamage = normalResult.FinalDamage,
                CritDamage = critResult.FinalDamage,
                CritChance = critChance * 100,
                HpRatio = hpRatio,
                StancePercent = normalResult.StancePercent,
                HasElementAdvantage = normalResult.HasElementAdvantage,
                ElementMultiplier = normalResult.ElementMultiplier
            };
        }

        /// <summary>
        /// 计算不同血量下的期望伤害（用于态势模拟）
        /// Calculate expected damage at different HP levels (for stance simulation)
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <param name="loadout">装备配置 / Equipment loadout (optional)</param>
        /// <param name="extraStats">额外加成 / Extra bonuses</param>
        /// <returns>不同血量百分比下的期望伤害字典 / Expected damage at different HP percentages</returns>
        public Dictionary<int, ExpectedDamageResult> CalculateExpectedDamageAtHpLevels(
            string professionId,
            EquipmentLoadout? loadout = null,
            ExtraCombatStats? extraStats = null)
        {
            var hpLevels = new[] { 1, 25, 50, 75, 100 };
            var results = new Dictionary<int, ExpectedDamageResult>();

            foreach (var hpPercent in hpLevels)
            {
                double hpRatio = hpPercent / 100.0;
                results[hpPercent] = CalculateExpectedDamage(
                    professionId, loadout, hpRatio, extraStats: extraStats);
            }

            return results;
        }

        /// <summary>
        /// 应用额外属性加成
        /// Apply extra stat bonuses
        /// </summary>
        private CombatStats ApplyExtraStats(CombatStats baseStats, ExtraCombatStats? extra)
        {
            if (extra == null) return baseStats;

            return new CombatStats
            {
                AttackFinal = baseStats.AttackFinal + extra.ExtraAttackFinal,
                AttackPercent = baseStats.AttackPercent + extra.ExtraAttackPercent,
                CritChancePercent = baseStats.CritChancePercent + extra.ExtraCritChancePercent,
                CritDamageBonusPercent = baseStats.CritDamageBonusPercent + extra.ExtraCritDamageBonusPercent,
                FortifyMaxPercent = baseStats.FortifyMaxPercent + extra.ExtraFortifyMaxPercent,
                BackwaterMaxPercent = baseStats.BackwaterMaxPercent + extra.ExtraBackwaterMaxPercent,
                HastePercent = baseStats.HastePercent,
                HPPercent = baseStats.HPPercent,
                SpecialAttackPercent = baseStats.SpecialAttackPercent,
                ChasePercent = baseStats.ChasePercent,
                KenChasePercent = baseStats.KenChasePercent,
                ChaseFlat = baseStats.ChaseFlat,
                DamageReductionPercent = baseStats.DamageReductionPercent
            }.Clamp(_caps);
        }

        /// <summary>
        /// 创建默认服务实例
        /// Create default service instance
        /// </summary>
        public static BattleStatsService CreateDefault()
        {
            return new BattleStatsService();
        }
    }

    /// <summary>
    /// 战斗属性包 - 包含战斗所需的所有属性
    /// Battle stats package - contains all attributes needed for battle
    /// </summary>
    public sealed class BattleStatsPackage
    {
        /// <summary>
        /// 最大生命值
        /// Maximum HP
        /// </summary>
        public int MaxHp { get; set; }

        /// <summary>
        /// 攻击速度（次/秒）
        /// Attack rate (attacks per second)
        /// </summary>
        public double AttackRate { get; set; }

        /// <summary>
        /// 战斗属性
        /// Combat stats
        /// </summary>
        public CombatStats CombatStats { get; set; } = new();

        /// <summary>
        /// 元素属性
        /// Element attribute
        /// </summary>
        public string Element { get; set; } = ElementIds.Neutral;

        /// <summary>
        /// 职业ID
        /// Profession ID
        /// </summary>
        public string ProfessionId { get; set; } = "";
    }

    /// <summary>
    /// 额外战斗属性（用于测试模式下的手动加成）
    /// Extra combat stats (for manual bonuses in test mode)
    /// </summary>
    public sealed class ExtraCombatStats
    {
        public int ExtraAttackFinal { get; set; }
        public double ExtraAttackPercent { get; set; }
        public double ExtraCritChancePercent { get; set; }
        public double ExtraCritDamageBonusPercent { get; set; }
        public double ExtraFortifyMaxPercent { get; set; }
        public double ExtraBackwaterMaxPercent { get; set; }
        public int ExtraMaxHp { get; set; }
    }

    /// <summary>
    /// 期望伤害结果
    /// Expected damage result
    /// </summary>
    public sealed class ExpectedDamageResult
    {
        /// <summary>
        /// 期望伤害（考虑暴击概率的加权平均）
        /// Expected damage (weighted average considering crit chance)
        /// </summary>
        public double ExpectedDamage { get; set; }

        /// <summary>
        /// 非暴击伤害
        /// Normal (non-crit) damage
        /// </summary>
        public int NormalDamage { get; set; }

        /// <summary>
        /// 暴击伤害
        /// Critical hit damage
        /// </summary>
        public int CritDamage { get; set; }

        /// <summary>
        /// 暴击率百分比
        /// Critical chance percentage
        /// </summary>
        public double CritChance { get; set; }

        /// <summary>
        /// 血量比例
        /// HP ratio
        /// </summary>
        public double HpRatio { get; set; }

        /// <summary>
        /// 态势加成百分比
        /// Stance bonus percentage
        /// </summary>
        public double StancePercent { get; set; }

        /// <summary>
        /// 是否有元素克制
        /// Whether has element advantage
        /// </summary>
        public bool HasElementAdvantage { get; set; }

        /// <summary>
        /// 元素倍率
        /// Element multiplier
        /// </summary>
        public double ElementMultiplier { get; set; }
    }
}
