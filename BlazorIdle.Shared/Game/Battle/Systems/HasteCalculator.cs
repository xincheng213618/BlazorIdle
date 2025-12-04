using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Tracks;

namespace BlazorIdle.Game.Battle.Systems
{
    /// <summary>
    /// 急速计算辅助类 - 从 MultiBattleInstance 提取的急速计算逻辑
    /// Haste calculator helper - Haste calculation logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 计算角色急速百分比（包含 Buff 效果）
    /// - 更新攻击轨道的急速倍率
    /// 
    /// Phase 8 优化：
    /// - 统一玩家/怪物急速计算为泛型方法
    /// - 消除代码重复
    /// </summary>
    public static class HasteCalculator
    {
        /// <summary>
        /// 通用急速计算方法 - 适用于任何拥有 Buff 的实体
        /// Generic haste calculation method - works for any entity with buffs
        /// </summary>
        /// <typeparam name="TBuffOwner">Buff 拥有者类型 / Buff owner type</typeparam>
        /// <param name="baseHastePercent">基础急速百分比 / Base haste percent</param>
        /// <param name="buffOwner">Buff 拥有者（可为 null）/ Buff owner (may be null)</param>
        /// <returns>修改后的急速百分比 / Modified haste percent</returns>
        public static double CalculateHastePercentGeneric<TBuffOwner>(double baseHastePercent, TBuffOwner? buffOwner) 
            where TBuffOwner : class, IBuffOwner
        {
            if (buffOwner == null)
                return baseHastePercent;

            double modifiedHaste = baseHastePercent;

            // 按应用时间排序 Buff（与 SkillResolver 一致）
            // Sort buffs by application time (consistent with SkillResolver)
            var sortedBuffs = buffOwner.Buffs.Values
                .OrderBy(b => b.AppliedAtMs)
                .ToList();

            foreach (var buff in sortedBuffs)
            {
                foreach (var effect in buff.Effects)
                {
                    // 只处理影响急速的效果
                    // Only process effects targeting haste
                    if (effect.Target != "HastePercent")
                        continue;

                    switch (effect.Type)
                    {
                        case BuffEffectType.StatMultiplier:
                            modifiedHaste *= (1.0 + effect.Value);
                            break;
                        case BuffEffectType.StatAdditive:
                            modifiedHaste += effect.Value;
                            break;
                        case BuffEffectType.StatReduction:
                            modifiedHaste *= (1.0 - effect.Value);
                            break;
                    }
                }
            }

            return modifiedHaste;
        }

        /// <summary>
        /// 计算包含 Buff 效果的急速百分比（玩家角色）
        /// Calculate haste percent including buff effects (player character)
        /// </summary>
        /// <param name="baseHastePercent">基础急速百分比 / Base haste percent</param>
        /// <param name="buffOwner">Buff 拥有者（可为 null）/ Buff owner (may be null)</param>
        /// <returns>修改后的急速百分比 / Modified haste percent</returns>
        public static double CalculateHastePercent(double baseHastePercent, CharacterBuffOwner? buffOwner)
        {
            return CalculateHastePercentGeneric(baseHastePercent, buffOwner);
        }

        /// <summary>
        /// 计算急速倍率（用于攻击轨道）
        /// Calculate haste multiplier (for attack track)
        /// </summary>
        /// <param name="hastePercent">急速百分比 / Haste percent</param>
        /// <returns>急速倍率 / Haste multiplier</returns>
        public static double CalculateHasteMultiplier(double hastePercent)
        {
            return 1.0 + hastePercent / 100.0;
        }

        /// <summary>
        /// 更新角色的急速（包含 Buff 效果）
        /// Update character haste (including buff effects)
        /// </summary>
        /// <param name="character">角色 / Character</param>
        /// <param name="buffOwner">Buff 拥有者（可为 null）/ Buff owner (may be null)</param>
        /// <param name="tracks">角色轨道 / Character tracks</param>
        public static void UpdateCharacterHaste(Character character, CharacterBuffOwner? buffOwner, CharacterTracks tracks)
        {
            // 获取基础急速
            // Get base haste
            double baseHastePercent = character.HastePercent;

            // 计算包含 Buff 效果的急速
            // Calculate haste including buff effects
            double modifiedHaste = CalculateHastePercent(baseHastePercent, buffOwner);

            // 更新攻击轨道的急速倍率
            // Update attack track haste multiplier
            tracks.AttackTrack.SetHaste(CalculateHasteMultiplier(modifiedHaste));
        }

        /// <summary>
        /// 计算怪物急速百分比（包含 Buff 效果）
        /// Calculate monster haste percent including buff effects
        /// </summary>
        /// <param name="baseHastePercent">基础急速百分比 / Base haste percent</param>
        /// <param name="buffOwner">Buff 拥有者（可为 null）/ Buff owner (may be null)</param>
        /// <returns>修改后的急速百分比 / Modified haste percent</returns>
        public static double CalculateMonsterHastePercent(double baseHastePercent, EnemyBuffOwner? buffOwner)
        {
            return CalculateHastePercentGeneric(baseHastePercent, buffOwner);
        }

    }
}
