namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 装备计算器 - 计算装备配置的汇总属性
    /// Equipment calculator - calculates aggregated stats from equipment loadout
    /// </summary>
    public sealed class EquipmentCalculator
    {
        /// <summary>
        /// 计算装备配置的汇总属性
        /// Calculate aggregated stats from equipment loadout
        /// </summary>
        /// <param name="loadout">装备配置 / Equipment loadout</param>
        /// <returns>汇总属性 / Aggregated stats</returns>
        public AggregatedStats Calculate(EquipmentLoadout loadout)
        {
            var stats = AggregatedStats.CreateEmpty();
            var mainElement = loadout.MainElement;

            foreach (var item in loadout.EquippedItems)
            {
                // 添加基础攻击力和生命值
                stats.TotalBaseAttack += item.BaseAttack;
                stats.TotalBaseHp += item.BaseHp;

                // 添加词条效果
                foreach (var affix in item.Affixes)
                {
                    // 获取激活的效果（根据元素匹配情况）
                    var activeEffects = affix.GetActiveEffects(item.Element, mainElement);
                    stats.AddFromDictionary(activeEffects);
                }
            }

            return stats;
        }

        /// <summary>
        /// 计算单件装备的属性贡献
        /// Calculate stats contribution from a single equipment item
        /// </summary>
        /// <param name="item">装备 / Equipment item</param>
        /// <param name="mainElement">主元素 / Main element</param>
        /// <returns>汇总属性 / Aggregated stats</returns>
        public AggregatedStats CalculateSingle(EquipmentItem item, string mainElement)
        {
            var stats = AggregatedStats.CreateEmpty();

            // 添加基础攻击力和生命值
            stats.TotalBaseAttack += item.BaseAttack;
            stats.TotalBaseHp += item.BaseHp;

            // 添加词条效果
            foreach (var affix in item.Affixes)
            {
                var activeEffects = affix.GetActiveEffects(item.Element, mainElement);
                stats.AddFromDictionary(activeEffects);
            }

            return stats;
        }

        /// <summary>
        /// 计算装备配置的未激活属性（预览用）
        /// Calculate inactive stats from equipment loadout (for preview)
        /// </summary>
        /// <param name="loadout">装备配置 / Equipment loadout</param>
        /// <returns>未激活的汇总属性 / Inactive aggregated stats</returns>
        public AggregatedStats CalculateInactive(EquipmentLoadout loadout)
        {
            var stats = AggregatedStats.CreateEmpty();
            var mainElement = loadout.MainElement;

            foreach (var item in loadout.EquippedItems)
            {
                foreach (var affix in item.Affixes)
                {
                    // 获取未激活的效果
                    var inactiveEffects = affix.GetInactiveEffects(item.Element, mainElement);
                    stats.AddFromDictionary(inactiveEffects);
                }
            }

            return stats;
        }

        /// <summary>
        /// 比较两个装备配置的属性差异
        /// Compare stats difference between two loadouts
        /// </summary>
        public AggregatedStats CompareDifference(EquipmentLoadout current, EquipmentLoadout newLoadout)
        {
            var currentStats = Calculate(current);
            var newStats = Calculate(newLoadout);

            return new AggregatedStats
            {
                TotalBaseAttack = newStats.TotalBaseAttack - currentStats.TotalBaseAttack,
                TotalBaseHp = newStats.TotalBaseHp - currentStats.TotalBaseHp,
                AttackPercent = newStats.AttackPercent - currentStats.AttackPercent,
                SpecialAttackPercent = newStats.SpecialAttackPercent - currentStats.SpecialAttackPercent,
                HPPercent = newStats.HPPercent - currentStats.HPPercent,
                HastePercent = newStats.HastePercent - currentStats.HastePercent,
                CritChancePercent = newStats.CritChancePercent - currentStats.CritChancePercent,
                CritDamageBonusPercent = newStats.CritDamageBonusPercent - currentStats.CritDamageBonusPercent,
                ChasePercent = newStats.ChasePercent - currentStats.ChasePercent,
                ChaseFlat = newStats.ChaseFlat - currentStats.ChaseFlat,
                KenChasePercent = newStats.KenChasePercent - currentStats.KenChasePercent,
                DamageReductionPercent = newStats.DamageReductionPercent - currentStats.DamageReductionPercent,
                FortifyMaxPercent = newStats.FortifyMaxPercent - currentStats.FortifyMaxPercent,
                BackwaterMaxPercent = newStats.BackwaterMaxPercent - currentStats.BackwaterMaxPercent
            };
        }
    }
}
