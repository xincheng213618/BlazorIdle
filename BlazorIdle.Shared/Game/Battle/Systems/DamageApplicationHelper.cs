using System;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Buffs;

namespace BlazorIdle.Game.Battle.Systems
{
    /// <summary>
    /// 伤害应用辅助类 - 从 MultiBattleInstance 提取的伤害和治疗应用逻辑
    /// Damage Application Helper - damage and healing application logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 创建战斗事件 (CreateCombatEvent)
    /// - 应用即时治疗 (ApplyInstantHeal)
    /// - 统一的伤害应用逻辑辅助
    /// 
    /// 设计原则:
    /// - 静态辅助方法减少重复代码
    /// - 玩家和怪物共享事件创建逻辑
    /// </summary>
    public static class DamageApplicationHelper
    {
        /// <summary>
        /// 创建玩家对敌人的战斗事件
        /// Create combat event for player attacking enemy
        /// </summary>
        /// <param name="attackerId">攻击者ID / Attacker ID</param>
        /// <param name="attackerName">攻击者名称 / Attacker name</param>
        /// <param name="defenderId">防御者ID / Defender ID</param>
        /// <param name="defenderName">防御者名称 / Defender name</param>
        /// <param name="source">事件来源 / Event source</param>
        /// <param name="timeMs">时间戳毫秒 / Timestamp in milliseconds</param>
        /// <param name="damage">伤害值 / Damage value</param>
        /// <param name="isCrit">是否暴击 / Whether is crit</param>
        /// <param name="isAoe">是否AOE / Whether is AOE</param>
        /// <param name="isKill">是否击杀 / Whether is kill</param>
        /// <param name="rngIndexAfter">RNG索引 / RNG index after</param>
        /// <param name="defenderHpAfter">防御者剩余HP / Defender HP after</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="bundleId">Bundle ID</param>
        /// <returns>战斗事件 / Combat event</returns>
        public static MultiCombatEvent CreatePlayerToEnemyEvent(
            string attackerId,
            string attackerName,
            string defenderId,
            string defenderName,
            EventSource source,
            int timeMs,
            int damage,
            bool isCrit,
            bool isAoe,
            bool isKill,
            int rngIndexAfter,
            int defenderHpAfter,
            string? skillId = null,
            string? bundleId = null)
        {
            return new MultiCombatEvent
            {
                Attacker = ActorType.Player,
                Defender = ActorType.Enemy,
                AttackerId = attackerId,
                AttackerName = attackerName,
                DefenderId = defenderId,
                DefenderName = defenderName,
                Source = source,
                TimeMs = timeMs,
                Damage = damage,
                Crit = isCrit,
                IsAoe = isAoe,
                IsKill = isKill,
                RngIndexAfter = rngIndexAfter,
                DefenderHpAfter = defenderHpAfter,
                SkillId = skillId,
                BundleId = bundleId
            };
        }

        /// <summary>
        /// 创建敌人对玩家的战斗事件
        /// Create combat event for enemy attacking player
        /// </summary>
        /// <param name="attackerId">攻击者ID / Attacker ID</param>
        /// <param name="attackerName">攻击者名称 / Attacker name</param>
        /// <param name="defenderId">防御者ID / Defender ID</param>
        /// <param name="defenderName">防御者名称 / Defender name</param>
        /// <param name="source">事件来源 / Event source</param>
        /// <param name="timeMs">时间戳毫秒 / Timestamp in milliseconds</param>
        /// <param name="damage">伤害值 / Damage value</param>
        /// <param name="isKill">是否击杀 / Whether is kill</param>
        /// <param name="rngIndexAfter">RNG索引 / RNG index after</param>
        /// <param name="defenderHpAfter">防御者剩余HP / Defender HP after</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="bundleId">Bundle ID</param>
        /// <returns>战斗事件 / Combat event</returns>
        public static MultiCombatEvent CreateEnemyToPlayerEvent(
            string attackerId,
            string attackerName,
            string defenderId,
            string defenderName,
            EventSource source,
            int timeMs,
            int damage,
            bool isKill,
            int rngIndexAfter,
            int defenderHpAfter,
            string? skillId = null,
            string? bundleId = null)
        {
            return new MultiCombatEvent
            {
                Attacker = ActorType.Enemy,
                Defender = ActorType.Player,
                AttackerId = attackerId,
                AttackerName = attackerName,
                DefenderId = defenderId,
                DefenderName = defenderName,
                Source = source,
                TimeMs = timeMs,
                Damage = damage,
                Crit = false, // 怪物暂不支持暴击 / Monster doesn't support crit yet
                IsAoe = false, // 怪物暂不支持AOE / Monster doesn't support AOE yet
                IsKill = isKill,
                RngIndexAfter = rngIndexAfter,
                DefenderHpAfter = defenderHpAfter,
                SkillId = skillId,
                BundleId = bundleId
            };
        }

        /// <summary>
        /// 应用即时治疗
        /// Apply instant heal
        /// </summary>
        /// <param name="healAmount">治疗量 / Heal amount</param>
        /// <param name="target">目标Buff所有者 / Target buff owner</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <returns>治疗元数据 / Heal metadata</returns>
        public static HealMeta ApplyInstantHeal(int healAmount, IBuffOwner target, string? skillId = null)
        {
            var healMeta = new HealMeta("instant_heal", buffId: null, skillId: skillId);
            target.ReceiveHeal(healAmount, healMeta);
            return healMeta;
        }

        /// <summary>
        /// 创建治疗元数据
        /// Create heal metadata
        /// </summary>
        /// <param name="source">治疗来源 / Heal source</param>
        /// <param name="reason">治疗原因 / Heal reason</param>
        /// <returns>治疗元数据 / Heal metadata</returns>
        public static HealMeta CreateHealMeta(string source = "instant_heal", string reason = "skill_cast")
        {
            return new HealMeta(source, reason);
        }

        /// <summary>
        /// 判断是否应该处理攻击触发器
        /// Determine if attack triggers should be processed
        /// </summary>
        /// <param name="source">事件来源 / Event source</param>
        /// <returns>是否应该处理 / Whether should process</returns>
        public static bool ShouldProcessAttackTriggers(EventSource source)
        {
            // 只在非触发技能造成伤害时才处理触发器，避免无限递归
            // Only process triggers for non-trigger skills to avoid infinite recursion
            return source != EventSource.Trigger;
        }

        /// <summary>
        /// 判断是否为玩家目标
        /// Determine if target is player
        /// </summary>
        /// <param name="isCasterPlayer">施法者是否为玩家 / Whether caster is player</param>
        /// <returns>目标是否为玩家 / Whether target is player</returns>
        public static bool IsTargetPlayer(bool isCasterPlayer)
        {
            // 玩家施法时目标为敌人，怪物施法时目标为玩家
            // When player casts, target is enemy; when monster casts, target is player
            return !isCasterPlayer;
        }
    }
}
