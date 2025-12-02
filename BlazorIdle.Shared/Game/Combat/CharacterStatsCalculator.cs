using BlazorIdle.Game.Items.Equipment;
using BlazorIdle.Game.Professions;

namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 角色属性计算器 - 整合职业基础属性、装备属性生成最终战斗属性
    /// Character stats calculator - combines profession base stats and equipment stats to generate final combat stats
    /// </summary>
    public sealed class CharacterStatsCalculator
    {
        private readonly CombatCapsConfig _caps;
        private readonly EquipmentCalculator _equipmentCalculator;
        private readonly ProfessionStatsRepository _professionRepository;

        /// <summary>
        /// 创建角色属性计算器
        /// Create character stats calculator
        /// </summary>
        /// <param name="caps">属性上限配置 / Combat caps config</param>
        /// <param name="equipmentCalculator">装备计算器 / Equipment calculator (optional)</param>
        /// <param name="professionRepository">职业仓库 / Profession repository (optional)</param>
        public CharacterStatsCalculator(
            CombatCapsConfig? caps = null,
            EquipmentCalculator? equipmentCalculator = null,
            ProfessionStatsRepository? professionRepository = null)
        {
            _caps = caps ?? CombatConfigRepository.LoadCombatCaps();
            _equipmentCalculator = equipmentCalculator ?? new EquipmentCalculator();
            _professionRepository = professionRepository ?? ProfessionStatsRepository.Shared;
        }

        /// <summary>
        /// 计算最终战斗属性
        /// Calculate final combat stats
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <param name="loadout">装备配置 / Equipment loadout (optional)</param>
        /// <returns>最终战斗属性（已裁剪）/ Final combat stats (capped)</returns>
        public CombatStats CalculateFinalStats(string professionId, EquipmentLoadout? loadout = null)
        {
            // 获取职业基础属性和初始战斗属性
            var profBaseStats = _professionRepository.GetBaseStats(professionId);
            var profCombatStats = _professionRepository.GetInitialCombatStats(professionId);

            // 计算装备属性
            AggregatedStats equipStats;
            if (loadout != null && loadout.EquippedItems.Any())
            {
                equipStats = _equipmentCalculator.Calculate(loadout);
            }
            else
            {
                equipStats = AggregatedStats.CreateEmpty();
            }

            // 合并属性
            var finalStats = new CombatStats
            {
                // 基础攻击力 = 职业基础 + 装备基础
                AttackFinal = profBaseStats.BaseAttack + (int)equipStats.TotalBaseAttack,

                // 百分比属性 = 职业初始 + 装备词条
                AttackPercent = profCombatStats.AttackPercent + equipStats.AttackPercent,
                SpecialAttackPercent = profCombatStats.SpecialAttackPercent + equipStats.SpecialAttackPercent,
                HPPercent = profCombatStats.HPPercent + equipStats.HPPercent,
                HastePercent = profCombatStats.HastePercent + equipStats.HastePercent,
                CritChancePercent = profCombatStats.CritChancePercent + equipStats.CritChancePercent,
                CritDamageBonusPercent = profCombatStats.CritDamageBonusPercent + equipStats.CritDamageBonusPercent,

                // 态势属性（装备提供）
                FortifyMaxPercent = profCombatStats.FortifyMaxPercent + equipStats.FortifyMaxPercent,
                BackwaterMaxPercent = profCombatStats.BackwaterMaxPercent + equipStats.BackwaterMaxPercent,

                // 追击属性（装备提供）
                ChasePercent = profCombatStats.ChasePercent + equipStats.ChasePercent,
                KenChasePercent = profCombatStats.KenChasePercent + equipStats.KenChasePercent,
                ChaseFlat = profCombatStats.ChaseFlat + (int)equipStats.ChaseFlat,

                // 减伤属性（装备提供）
                DamageReductionPercent = profCombatStats.DamageReductionPercent + equipStats.DamageReductionPercent
            };

            // 应用属性上限裁剪
            return finalStats.Clamp(_caps);
        }

        /// <summary>
        /// 计算最终生命值
        /// Calculate final max HP
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <param name="loadout">装备配置 / Equipment loadout (optional)</param>
        /// <returns>最终最大生命值 / Final max HP</returns>
        public int CalculateFinalMaxHp(string professionId, EquipmentLoadout? loadout = null)
        {
            // 获取职业基础属性
            var profBaseStats = _professionRepository.GetBaseStats(professionId);
            var profCombatStats = _professionRepository.GetInitialCombatStats(professionId);

            // 计算装备属性
            AggregatedStats equipStats;
            if (loadout != null && loadout.EquippedItems.Any())
            {
                equipStats = _equipmentCalculator.Calculate(loadout);
            }
            else
            {
                equipStats = AggregatedStats.CreateEmpty();
            }

            // 基础生命值 = 职业基础 + 装备基础
            int baseHp = profBaseStats.BaseHP + (int)equipStats.TotalBaseHp;

            // HP% 加成（裁剪后）
            double hpPercent = profCombatStats.HPPercent + equipStats.HPPercent;
            double clampedHpPercent = Math.Min(hpPercent, _caps.HpPercent);

            // 最终生命值 = 基础生命 × (1 + HP%/100)
            return (int)(baseHp * (1 + clampedHpPercent / 100.0));
        }

        /// <summary>
        /// 计算最终攻击速度
        /// Calculate final attack rate
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <param name="loadout">装备配置 / Equipment loadout (optional)</param>
        /// <returns>最终攻击速度 / Final attack rate APS</returns>
        public double CalculateFinalAttackRate(string professionId, EquipmentLoadout? loadout = null)
        {
            // 获取职业基础攻速
            var profBaseStats = _professionRepository.GetBaseStats(professionId);
            var profCombatStats = _professionRepository.GetInitialCombatStats(professionId);

            // 计算装备急速
            AggregatedStats equipStats;
            if (loadout != null && loadout.EquippedItems.Any())
            {
                equipStats = _equipmentCalculator.Calculate(loadout);
            }
            else
            {
                equipStats = AggregatedStats.CreateEmpty();
            }

            // 急速% = 职业初始 + 装备词条，裁剪后
            double hastePercent = profCombatStats.HastePercent + equipStats.HastePercent;
            double clampedHastePercent = Math.Min(hastePercent, _caps.HastePct);

            // 最终攻速 = 基础攻速 × (1 + 急速%/100)
            return profBaseStats.AttackRateAPS * (1 + clampedHastePercent / 100.0);
        }

        /// <summary>
        /// 获取职业基础属性
        /// Get profession base stats
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <returns>职业基础属性 / Profession base stats</returns>
        public ProfessionBaseStats GetProfessionBaseStats(string professionId)
        {
            return _professionRepository.GetBaseStats(professionId);
        }

        /// <summary>
        /// 获取职业初始战斗属性
        /// Get profession initial combat stats
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <returns>职业初始战斗属性 / Profession initial combat stats</returns>
        public InitialCombatStats GetProfessionInitialCombatStats(string professionId)
        {
            return _professionRepository.GetInitialCombatStats(professionId);
        }

        /// <summary>
        /// 创建默认计算器实例
        /// Create default calculator instance
        /// </summary>
        public static CharacterStatsCalculator CreateDefault()
        {
            return new CharacterStatsCalculator();
        }
    }
}
