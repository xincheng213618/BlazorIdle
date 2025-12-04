using System;
using System.Collections.Generic;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Battle.Execution;
using BlazorIdle.Game.Resources;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Battle.Systems
{
    /// <summary>
    /// 技能执行协调器 - 从 MultiBattleInstance.ExecuteSkill 提取的核心逻辑
    /// Skill Execution Coordinator - Core logic extracted from MultiBattleInstance.ExecuteSkill
    /// 
    /// 职责:
    /// - 构建战斗上下文 (BuildPlayerContext, BuildMonsterContext)
    /// - 处理多段伤害实例 (ProcessDamageInstances)
    /// - 处理传统单段伤害 (ApplyLegacySingleDamage)
    /// - 处理向后兼容的资源获得 (HandleBackwardCompatibleResourceGain)
    /// 
    /// 设计原则:
    /// - 玩家和怪物执行路径共享核心逻辑
    /// - 通过委托回调应用实际伤害（避免依赖具体的团队实现）
    /// </summary>
    public class SkillExecutionCoordinator
    {
        /// <summary>
        /// 默认资源ID（用于向后兼容的资源获得）
        /// Default resource ID (used for backward compatible resource gain)
        /// </summary>
        public const string DefaultResourceId = "rage";
        
        private readonly PendingDamageQueue _pendingDamageQueue;
        private readonly IGameClock _clock;
        private readonly double _aoeDamageMultiplier;

        /// <summary>
        /// 立即伤害应用请求事件（玩家对敌人）
        /// Immediate damage application request event (player to enemy)
        /// </summary>
        /// <remarks>
        /// 参数: (casterId, targetId, damage, eventSource, isAoe, isCrit, skillId, bundleId)
        /// </remarks>
        public event Action<string, string, int, EventSource, bool, bool, string?, string?>? OnApplyDamageToEnemy;

        /// <summary>
        /// 立即伤害应用请求事件（怪物对玩家）
        /// Immediate damage application request event (monster to player)
        /// </summary>
        /// <remarks>
        /// 参数: (casterId, targetId, damage, eventSource, skillId, bundleId)
        /// </remarks>
        public event Action<string, string, int, EventSource, string?, string?>? OnApplyDamageToPlayer;

        /// <summary>
        /// 即时治疗请求事件
        /// Instant heal request event
        /// </summary>
        /// <remarks>
        /// 参数: (result, casterId, targetId, isCasterPlayer, skillId)
        /// </remarks>
        public event Action<SkillCastResult, string, string, bool, string?>? OnApplyInstantHeal;

        /// <summary>
        /// 资源获得记录请求事件
        /// Resource gain record request event
        /// </summary>
        /// <remarks>
        /// 参数: (casterId, resourceId, amount, newValue, reason, skillId, bundleId)
        /// </remarks>
        public event Action<string, string, int, int, string, string?, string?>? OnRecordResourceGain;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="pendingDamageQueue">延迟伤害队列 / Pending damage queue</param>
        /// <param name="clock">游戏时钟 / Game clock</param>
        /// <param name="aoeDamageMultiplier">AOE伤害倍率 / AOE damage multiplier</param>
        public SkillExecutionCoordinator(
            PendingDamageQueue pendingDamageQueue,
            IGameClock clock,
            double aoeDamageMultiplier = 0.8)
        {
            _pendingDamageQueue = pendingDamageQueue ?? throw new ArgumentNullException(nameof(pendingDamageQueue));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _aoeDamageMultiplier = aoeDamageMultiplier;
        }

        /// <summary>
        /// 构建玩家战斗上下文
        /// Build player battle context
        /// </summary>
        /// <param name="character">玩家角色 / Player character</param>
        /// <param name="defaultTarget">默认敌人目标 / Default enemy target</param>
        /// <param name="defaultTargetId">默认目标ID / Default target ID</param>
        /// <param name="playerTeam">玩家队伍 / Player team</param>
        /// <param name="enemyTeam">敌人队伍 / Enemy team</param>
        /// <param name="rng">随机数上下文 / RNG context</param>
        /// <param name="clock">游戏时钟 / Game clock</param>
        /// <param name="playerResources">玩家资源 / Player resources</param>
        /// <param name="playerBuffOwner">玩家Buff所有者 / Player buff owner</param>
        /// <param name="enemyBuffOwners">敌人Buff所有者字典 / Enemy buff owners dictionary</param>
        /// <param name="damageCalculator">伤害计算器 / Damage calculator</param>
        /// <returns>战斗上下文 / Battle context</returns>
        public BattleContext BuildPlayerContext(
            Character character,
            Enemy? defaultTarget,
            string? defaultTargetId,
            BattleTeam<Character> playerTeam,
            BattleTeam<Enemy> enemyTeam,
            RngContext rng,
            IGameClock clock,
            ResourceBucketCollection? playerResources,
            Buffs.CharacterBuffOwner? playerBuffOwner,
            Dictionary<string, Buffs.EnemyBuffOwner> enemyBuffOwners,
            DamageCalculator damageCalculator)
        {
            // 计算攻击者战斗属性和HP比例
            // Calculate attacker combat stats and HP ratio
            var attackerCombatStats = character.CombatStats ?? CombatStats.CreateDefault();
            double attackerHPRatio = character.MaxHp > 0 ? (double)character.Hp / character.MaxHp : 1.0;
            double defenderDRPct = defaultTarget?.DamageReductionPercent ?? 0;

            return new BattleContext
            {
                Player = character,
                Enemy = defaultTarget,
                PlayerTeam = playerTeam,
                EnemyTeam = enemyTeam,
                Rng = rng,
                Clock = clock,
                PlayerResources = playerResources,
                PlayerBuffOwner = playerBuffOwner,
                EnemyBuffOwners = enemyBuffOwners,
                CurrentTargetId = defaultTargetId,
                DamageCalculator = damageCalculator,
                AttackerCombatStats = attackerCombatStats,
                AttackerElement = character.Element,
                DefenderElement = defaultTarget?.Element ?? ElementIds.Neutral,
                AttackerHPRatio = attackerHPRatio,
                DefenderDamageReductionPercent = defenderDRPct
            };
        }

        /// <summary>
        /// 构建怪物战斗上下文
        /// Build monster battle context
        /// </summary>
        /// <param name="enemy">怪物 / Enemy</param>
        /// <param name="defaultTarget">默认玩家目标 / Default player target</param>
        /// <param name="defaultTargetId">默认目标ID / Default target ID</param>
        /// <param name="playerTeam">玩家队伍 / Player team</param>
        /// <param name="enemyTeam">敌人队伍 / Enemy team</param>
        /// <param name="rng">随机数上下文 / RNG context</param>
        /// <param name="clock">游戏时钟 / Game clock</param>
        /// <param name="playerResources">目标玩家资源 / Target player resources</param>
        /// <param name="playerBuffOwner">目标玩家Buff所有者 / Target player buff owner</param>
        /// <param name="enemyBuffOwners">敌人Buff所有者字典 / Enemy buff owners dictionary</param>
        /// <param name="damageCalculator">伤害计算器 / Damage calculator</param>
        /// <returns>战斗上下文 / Battle context</returns>
        public BattleContext BuildMonsterContext(
            Enemy enemy,
            Character? defaultTarget,
            string? defaultTargetId,
            BattleTeam<Character> playerTeam,
            BattleTeam<Enemy> enemyTeam,
            RngContext rng,
            IGameClock clock,
            ResourceBucketCollection? playerResources,
            Buffs.CharacterBuffOwner? playerBuffOwner,
            Dictionary<string, Buffs.EnemyBuffOwner> enemyBuffOwners,
            DamageCalculator damageCalculator)
        {
            // 怪物使用 BaseAttack 作为攻击力
            // Monster uses BaseAttack as attack power
            var monsterCombatStats = new CombatStats { AttackFinal = (int)enemy.BaseAttack };
            double attackerHPRatio = enemy.MaxHp > 0 ? (double)enemy.Hp / enemy.MaxHp : 1.0;
            // 防御者（玩家）的减伤百分比 - 玩家暂时没有减伤属性，默认为0
            // Defender (player) damage reduction percentage - players don't have DR yet, default to 0
            double defenderDRPct = 0;

            return new BattleContext
            {
                Enemy = enemy,
                Player = defaultTarget,
                PlayerTeam = playerTeam,
                EnemyTeam = enemyTeam,
                Rng = rng,
                Clock = clock,
                PlayerResources = playerResources,
                PlayerBuffOwner = playerBuffOwner,
                EnemyBuffOwners = enemyBuffOwners,
                CurrentTargetId = defaultTargetId,
                DamageCalculator = damageCalculator,
                AttackerCombatStats = monsterCombatStats,
                AttackerElement = enemy.Element,
                DefenderElement = defaultTarget?.Element ?? ElementIds.Neutral,
                AttackerHPRatio = attackerHPRatio,
                DefenderDamageReductionPercent = defenderDRPct
            };
        }

        /// <summary>
        /// 解析技能目标
        /// Resolve skill targets
        /// </summary>
        /// <param name="result">技能施放结果 / Skill cast result</param>
        /// <param name="defaultTargetId">默认目标ID / Default target ID</param>
        /// <returns>目标ID列表 / List of target IDs</returns>
        public List<string> ResolveSkillTargets(SkillCastResult result, string? defaultTargetId)
        {
            if (result.TargetIds?.Count > 0)
            {
                return result.TargetIds;
            }
            
            return defaultTargetId != null 
                ? new List<string> { defaultTargetId } 
                : new List<string>();
        }

        /// <summary>
        /// 处理多段伤害实例（玩家对敌人）
        /// Process multi-hit damage instances (player to enemy)
        /// </summary>
        /// <param name="result">技能施放结果 / Skill cast result</param>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="targetIds">目标ID列表 / Target ID list</param>
        /// <param name="eventSource">事件来源 / Event source</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="isTargetAlive">检查目标是否存活的委托 / Delegate to check if target is alive</param>
        public void ProcessPlayerDamageInstances(
            SkillCastResult result,
            string casterId,
            List<string> targetIds,
            EventSource eventSource,
            string skillId,
            Func<string, bool> isTargetAlive)
        {
            if (result.DamageInstances == null || result.DamageInstances.Count == 0)
                return;

            bool isAoe = targetIds.Count > 1;
            double currentTimeSec = _clock.NowMs / 1000.0;

            foreach (var targetId in targetIds)
            {
                if (!isTargetAlive(targetId))
                    continue;

                foreach (var instance in result.DamageInstances)
                {
                    // 克隆实例并设置目标
                    // Clone instance and set target
                    var targetedInstance = instance.Clone();
                    targetedInstance.TargetId = targetId;
                    targetedInstance.CasterId = casterId;
                    targetedInstance.IsCasterPlayer = true;
                    targetedInstance.EventSource = eventSource;

                    // AOE 伤害减免
                    // AOE damage reduction
                    targetedInstance.Damage = ApplyAoeDamageReduction(targetedInstance.Damage, isAoe);

                    if (targetedInstance.IsImmediate)
                    {
                        // 立即伤害 - 通过事件回调
                        // Immediate damage - via event callback
                        if (targetedInstance.Damage > 0)
                        {
                            OnApplyDamageToEnemy?.Invoke(
                                casterId, targetId, targetedInstance.Damage, eventSource,
                                isAoe, targetedInstance.IsCrit, skillId, result.BundleId);
                        }
                    }
                    else
                    {
                        // 延迟伤害加入队列
                        // Delayed damage enqueue
                        _pendingDamageQueue.Enqueue(targetedInstance, currentTimeSec);
                    }
                }

                // 即时治疗应用到每个目标
                // Instant heal applies to each target
                if (result.InstantHeal > 0)
                {
                    OnApplyInstantHeal?.Invoke(result, casterId, targetId, true, skillId);
                }
            }
        }

        /// <summary>
        /// 处理多段伤害实例（怪物对玩家）
        /// Process multi-hit damage instances (monster to player)
        /// </summary>
        /// <param name="result">技能施放结果 / Skill cast result</param>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="targetIds">目标ID列表 / Target ID list</param>
        /// <param name="eventSource">事件来源 / Event source</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="isTargetAlive">检查目标是否存活的委托 / Delegate to check if target is alive</param>
        public void ProcessMonsterDamageInstances(
            SkillCastResult result,
            string casterId,
            List<string> targetIds,
            EventSource eventSource,
            string skillId,
            Func<string, bool> isTargetAlive)
        {
            if (result.DamageInstances == null || result.DamageInstances.Count == 0)
                return;

            bool isAoe = targetIds.Count > 1;
            double currentTimeSec = _clock.NowMs / 1000.0;

            foreach (var targetId in targetIds)
            {
                if (!isTargetAlive(targetId))
                    continue;

                foreach (var instance in result.DamageInstances)
                {
                    // 克隆实例并设置目标
                    // Clone instance and set target
                    var targetedInstance = instance.Clone();
                    targetedInstance.TargetId = targetId;
                    targetedInstance.CasterId = casterId;
                    targetedInstance.IsCasterPlayer = false;
                    targetedInstance.EventSource = eventSource;

                    // AOE 伤害减免
                    // AOE damage reduction
                    targetedInstance.Damage = ApplyAoeDamageReduction(targetedInstance.Damage, isAoe);

                    if (targetedInstance.IsImmediate)
                    {
                        // 立即伤害 - 通过事件回调
                        // Immediate damage - via event callback
                        if (targetedInstance.Damage > 0)
                        {
                            OnApplyDamageToPlayer?.Invoke(
                                casterId, targetId, targetedInstance.Damage, eventSource,
                                skillId, result.BundleId);
                        }
                    }
                    else
                    {
                        // 延迟伤害加入队列
                        // Delayed damage enqueue
                        _pendingDamageQueue.Enqueue(targetedInstance, currentTimeSec);
                    }
                }

                // 即时治疗应用到每个目标
                // Instant heal applies to each target
                if (result.InstantHeal > 0)
                {
                    OnApplyInstantHeal?.Invoke(result, casterId, targetId, false, skillId);
                }
            }
        }

        /// <summary>
        /// 处理传统单段伤害（向后兼容）- 玩家对敌人
        /// Process legacy single-hit damage (backward compatible) - player to enemy
        /// </summary>
        /// <param name="result">技能施放结果 / Skill cast result</param>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="targetIds">目标ID列表 / Target ID list</param>
        /// <param name="eventSource">事件来源 / Event source</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="isTargetAlive">检查目标是否存活的委托 / Delegate to check if target is alive</param>
        public void ApplyPlayerLegacySingleDamage(
            SkillCastResult result,
            string casterId,
            List<string> targetIds,
            EventSource eventSource,
            string skillId,
            Func<string, bool> isTargetAlive)
        {
            if (result.DamageDealt <= 0)
            {
                // 只处理即时治疗
                // Only process instant heal
                foreach (var targetId in targetIds)
                {
                    if (result.InstantHeal > 0)
                    {
                        OnApplyInstantHeal?.Invoke(result, casterId, targetId, true, skillId);
                    }
                }
                return;
            }

            bool isAoe = targetIds.Count > 1;
            int damagePerTarget = ApplyAoeDamageReduction(result.DamageDealt, isAoe);

            foreach (var targetId in targetIds)
            {
                if (!isTargetAlive(targetId))
                    continue;

                if (damagePerTarget > 0)
                {
                    OnApplyDamageToEnemy?.Invoke(
                        casterId, targetId, damagePerTarget, eventSource,
                        isAoe, result.IsCrit, skillId, result.BundleId);
                }

                // 即时治疗应用到每个目标
                // Instant heal applies to each target
                if (result.InstantHeal > 0)
                {
                    OnApplyInstantHeal?.Invoke(result, casterId, targetId, true, skillId);
                }
            }
        }

        /// <summary>
        /// 处理传统单段伤害（向后兼容）- 怪物对玩家
        /// Process legacy single-hit damage (backward compatible) - monster to player
        /// </summary>
        /// <param name="result">技能施放结果 / Skill cast result</param>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="targetIds">目标ID列表 / Target ID list</param>
        /// <param name="eventSource">事件来源 / Event source</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="isTargetAlive">检查目标是否存活的委托 / Delegate to check if target is alive</param>
        public void ApplyMonsterLegacySingleDamage(
            SkillCastResult result,
            string casterId,
            List<string> targetIds,
            EventSource eventSource,
            string skillId,
            Func<string, bool> isTargetAlive)
        {
            if (result.DamageDealt <= 0)
            {
                // 只处理即时治疗
                // Only process instant heal
                foreach (var targetId in targetIds)
                {
                    if (result.InstantHeal > 0)
                    {
                        OnApplyInstantHeal?.Invoke(result, casterId, targetId, false, skillId);
                    }
                }
                return;
            }

            bool isAoe = targetIds.Count > 1;
            int damagePerTarget = ApplyAoeDamageReduction(result.DamageDealt, isAoe);

            foreach (var targetId in targetIds)
            {
                if (!isTargetAlive(targetId))
                    continue;

                if (damagePerTarget > 0)
                {
                    OnApplyDamageToPlayer?.Invoke(
                        casterId, targetId, damagePerTarget, eventSource,
                        skillId, result.BundleId);
                }

                // 即时治疗应用到每个目标
                // Instant heal applies to each target
                if (result.InstantHeal > 0)
                {
                    OnApplyInstantHeal?.Invoke(result, casterId, targetId, false, skillId);
                }
            }
        }

        /// <summary>
        /// 处理向后兼容的资源获得（仅普通攻击）
        /// Handle backward compatible resource gain (normal attack only)
        /// </summary>
        /// <param name="sourceTrack">技能来源轨道 / Skill source track</param>
        /// <param name="result">技能施放结果 / Skill cast result</param>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <param name="resources">玩家资源集合 / Player resource collection</param>
        /// <param name="professionResourceConfigs">职业资源配置 / Profession resource configs</param>
        /// <param name="defaultResourceConfig">默认资源配置 / Default resource config</param>
        public void HandleBackwardCompatibleResourceGain(
            string sourceTrack,
            SkillCastResult result,
            string casterId,
            string skillId,
            string professionId,
            ResourceBucketCollection resources,
            Dictionary<string, ProfessionResourceConfig>? professionResourceConfigs,
            ResourceConfig defaultResourceConfig)
        {
            // 仅普通攻击且技能没有定义资源获得
            // Only normal attack and skill doesn't define resource gains
            if (sourceTrack != "attack" || 
                (result.ResourceChanges != null && result.ResourceChanges.Count > 0))
            {
                return;
            }

            // 获取职业资源配置
            // Get profession resource config
            string resourceId = DefaultResourceId;
            int gainPerAttack = defaultResourceConfig.GainPerAttack;
            int gainPerCritExtra = defaultResourceConfig.GainPerCritExtra;

            if (professionResourceConfigs != null &&
                professionResourceConfigs.TryGetValue(professionId, out var profConfig))
            {
                resourceId = profConfig.Id;
                gainPerAttack = profConfig.GainPerAttack;
                gainPerCritExtra = profConfig.GainPerCritExtra;
            }

            if (!resources.HasBucket(resourceId))
                return;

            var bucket = resources.GetBucket(resourceId);

            // 命中产生资源
            // Attack hit generates resource
            int gained = bucket.Gain(gainPerAttack, "attack_hit");
            if (gained > 0)
            {
                OnRecordResourceGain?.Invoke(casterId, resourceId, gained, bucket.Current, "attack_hit", 
                    skillId, result.BundleId);
            }

            // 暴击额外产生资源
            // Crit generates extra resource
            if (result.IsCrit)
            {
                int critGain = bucket.Gain(gainPerCritExtra, "crit_bonus");
                if (critGain > 0)
                {
                    OnRecordResourceGain?.Invoke(casterId, resourceId, critGain, bucket.Current, "crit_bonus",
                        skillId, result.BundleId);
                }
            }
        }

        /// <summary>
        /// 检查是否有多段伤害实例
        /// Check if there are multi-hit damage instances
        /// </summary>
        public bool HasDamageInstances(SkillCastResult result)
        {
            return result.DamageInstances != null && result.DamageInstances.Count > 0;
        }

        /// <summary>
        /// 计算AOE伤害减免后的伤害值
        /// Calculate damage value after AOE damage reduction
        /// </summary>
        /// <param name="baseDamage">基础伤害 / Base damage</param>
        /// <param name="isAoe">是否为AOE / Whether is AOE</param>
        /// <returns>减免后的伤害 / Damage after reduction</returns>
        private int ApplyAoeDamageReduction(int baseDamage, bool isAoe)
        {
            return isAoe ? (int)(baseDamage * _aoeDamageMultiplier) : baseDamage;
        }
    }
}
