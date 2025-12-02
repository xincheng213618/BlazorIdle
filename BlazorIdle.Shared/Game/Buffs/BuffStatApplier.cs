using System;
using System.Linq;
using BlazorIdle.Game.Combat;

namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Buff 效果应用器 - 将 Buff 效果应用到战斗属性
    /// Buff stat applier - applies buff effects to combat stats
    /// 
    /// 核心规则：Buff 效果不受属性上限裁剪
    /// Core rule: Buff effects are NOT subject to stat caps
    /// 
    /// 公式：最终属性 = 职业属性 + Clamp(装备属性) + Buff效果
    /// Formula: FinalStats = ProfessionStats + Clamp(EquipmentStats) + BuffEffects
    /// </summary>
    public static class BuffStatApplier
    {
        /// <summary>
        /// 应用 Buff 效果到战斗属性
        /// Buff 效果不受上限裁剪
        /// Apply buff effects to combat stats
        /// Buff effects are NOT subject to stat caps
        /// </summary>
        /// <param name="baseStats">基础属性（职业+装备裁剪后）/ Base stats (profession + clamped equipment)</param>
        /// <param name="buffOwner">Buff 所有者 / Buff owner</param>
        /// <returns>应用 Buff 后的属性 / Stats after buff application</returns>
        public static CombatStats ApplyBuffsToCombatStats(CombatStats baseStats, IBuffOwner? buffOwner)
        {
            if (buffOwner == null || !buffOwner.Buffs.Any())
                return CloneStats(baseStats);

            // 复制基础属性
            var modified = CloneStats(baseStats);

            // 按应用时间排序 Buff（Method B: Time-order stacking）
            var sortedBuffs = buffOwner.Buffs.Values
                .OrderBy(b => b.AppliedAtMs)
                .ToList();

            foreach (var buff in sortedBuffs)
            {
                foreach (var effect in buff.Effects)
                {
                    // 跳过非属性效果
                    if (effect.Type == BuffEffectType.ForceCrit ||
                        effect.Type == BuffEffectType.DamageOverTime ||
                        effect.Type == BuffEffectType.HealOverTime)
                        continue;

                    if (string.IsNullOrEmpty(effect.Target))
                        continue;

                    // 规范化属性名
                    var statName = BuffStatMapper.NormalizeStatName(effect.Target);

                    // 获取效果值（处理值转换）
                    double effectValue = effect.Value;
                    if (BuffStatMapper.NeedsValueConversion(effect.Target, out var converter) && converter != null)
                    {
                        effectValue = converter(effectValue);
                    }

                    // 应用效果到对应属性（不受上限裁剪）
                    ApplyEffectToStat(modified, statName, effect.Type, effectValue, buff.Stacks);
                }
            }

            return modified;
        }

        /// <summary>
        /// 应用单个效果到指定属性
        /// Apply single effect to specified stat
        /// </summary>
        private static void ApplyEffectToStat(
            CombatStats stats,
            string statName,
            BuffEffectType effectType,
            double value,
            int stacks)
        {
            // 根据属性名应用效果
            switch (statName)
            {
                case "AttackFinal":
                    stats.AttackFinal = ApplyEffectInt(stats.AttackFinal, effectType, value, stacks);
                    break;
                case "AttackPercent":
                    stats.AttackPercent = ApplyEffectDouble(stats.AttackPercent, effectType, value, stacks);
                    break;
                case "SpecialAttackPercent":
                    stats.SpecialAttackPercent = ApplyEffectDouble(stats.SpecialAttackPercent, effectType, value, stacks);
                    break;
                case "HPPercent":
                    stats.HPPercent = ApplyEffectDouble(stats.HPPercent, effectType, value, stacks);
                    break;
                case "HastePercent":
                    stats.HastePercent = ApplyEffectDouble(stats.HastePercent, effectType, value, stacks);
                    break;
                case "CritChancePercent":
                    stats.CritChancePercent = ApplyEffectDouble(stats.CritChancePercent, effectType, value, stacks);
                    break;
                case "CritDamageBonusPercent":
                    stats.CritDamageBonusPercent = ApplyEffectDouble(stats.CritDamageBonusPercent, effectType, value, stacks);
                    break;
                case "FortifyMaxPercent":
                    stats.FortifyMaxPercent = ApplyEffectDouble(stats.FortifyMaxPercent, effectType, value, stacks);
                    break;
                case "BackwaterMaxPercent":
                    stats.BackwaterMaxPercent = ApplyEffectDouble(stats.BackwaterMaxPercent, effectType, value, stacks);
                    break;
                case "ChasePercent":
                    stats.ChasePercent = ApplyEffectDouble(stats.ChasePercent, effectType, value, stacks);
                    break;
                case "KenChasePercent":
                    stats.KenChasePercent = ApplyEffectDouble(stats.KenChasePercent, effectType, value, stacks);
                    break;
                case "ChaseFlat":
                    stats.ChaseFlat = ApplyEffectInt(stats.ChaseFlat, effectType, value, stacks);
                    break;
                case "DamageReductionPercent":
                    stats.DamageReductionPercent = ApplyEffectDouble(stats.DamageReductionPercent, effectType, value, stacks);
                    break;
            }
        }

        /// <summary>
        /// 应用效果到整数属性
        /// Apply effect to integer stat
        /// </summary>
        private static int ApplyEffectInt(int baseValue, BuffEffectType type, double value, int stacks)
        {
            double result = baseValue;
            for (int i = 0; i < stacks; i++)
            {
                result = type switch
                {
                    BuffEffectType.StatMultiplier => result * (1 + value),
                    BuffEffectType.StatAdditive => result + value,
                    BuffEffectType.StatReduction => result * (1 - value),
                    _ => result
                };
            }
            return (int)Math.Floor(result);
        }

        /// <summary>
        /// 应用效果到浮点数属性
        /// Apply effect to double stat
        /// </summary>
        private static double ApplyEffectDouble(double baseValue, BuffEffectType type, double value, int stacks)
        {
            double result = baseValue;
            for (int i = 0; i < stacks; i++)
            {
                result = type switch
                {
                    BuffEffectType.StatMultiplier => result * (1 + value),
                    BuffEffectType.StatAdditive => result + value,
                    BuffEffectType.StatReduction => result * (1 - value),
                    _ => result
                };
            }
            return result;
        }

        /// <summary>
        /// 克隆战斗属性
        /// Clone combat stats
        /// </summary>
        private static CombatStats CloneStats(CombatStats source)
        {
            return new CombatStats
            {
                AttackFinal = source.AttackFinal,
                AttackPercent = source.AttackPercent,
                SpecialAttackPercent = source.SpecialAttackPercent,
                HPPercent = source.HPPercent,
                HastePercent = source.HastePercent,
                CritChancePercent = source.CritChancePercent,
                CritDamageBonusPercent = source.CritDamageBonusPercent,
                FortifyMaxPercent = source.FortifyMaxPercent,
                BackwaterMaxPercent = source.BackwaterMaxPercent,
                ChasePercent = source.ChasePercent,
                KenChasePercent = source.KenChasePercent,
                ChaseFlat = source.ChaseFlat,
                DamageReductionPercent = source.DamageReductionPercent
            };
        }

        /// <summary>
        /// 合并两个 CombatStats（加法叠加）
        /// Merge two CombatStats (additive stacking)
        /// </summary>
        /// <param name="stats1">第一个属性集 / First stats</param>
        /// <param name="stats2">第二个属性集 / Second stats</param>
        /// <returns>合并后的属性 / Merged stats</returns>
        public static CombatStats MergeStats(CombatStats stats1, CombatStats stats2)
        {
            return new CombatStats
            {
                AttackFinal = stats1.AttackFinal + stats2.AttackFinal,
                AttackPercent = stats1.AttackPercent + stats2.AttackPercent,
                SpecialAttackPercent = stats1.SpecialAttackPercent + stats2.SpecialAttackPercent,
                HPPercent = stats1.HPPercent + stats2.HPPercent,
                HastePercent = stats1.HastePercent + stats2.HastePercent,
                CritChancePercent = stats1.CritChancePercent + stats2.CritChancePercent,
                CritDamageBonusPercent = stats1.CritDamageBonusPercent + stats2.CritDamageBonusPercent,
                FortifyMaxPercent = stats1.FortifyMaxPercent + stats2.FortifyMaxPercent,
                BackwaterMaxPercent = stats1.BackwaterMaxPercent + stats2.BackwaterMaxPercent,
                ChasePercent = stats1.ChasePercent + stats2.ChasePercent,
                KenChasePercent = stats1.KenChasePercent + stats2.KenChasePercent,
                ChaseFlat = stats1.ChaseFlat + stats2.ChaseFlat,
                DamageReductionPercent = stats1.DamageReductionPercent + stats2.DamageReductionPercent
            };
        }
    }
}
