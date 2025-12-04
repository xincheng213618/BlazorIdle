using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Battle.Systems
{
    /// <summary>
    /// Buff 系统集成模块 - 从 MultiBattleInstance 提取的 Buff 相关逻辑
    /// Buff system integration module - Buff-related logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 处理 Buff 操作 (Apply/Remove)
    /// - 解析 Buff 目标
    /// - 处理 Buff Tick (DoT/HoT)
    /// - 触发 Buff 相关事件
    /// </summary>
    public class BuffIntegration
    {
        private readonly IGameClock _clock;
        private readonly RngContext _rng;
        private readonly Dictionary<string, CharacterBuffOwner> _playerBuffOwners;
        private readonly Dictionary<string, EnemyBuffOwner> _enemyBuffOwners;
        private readonly Func<IEnumerable<string>> _getAlivePlayerIds;
        private readonly Func<IEnumerable<string>> _getAliveEnemyIds;

        /// <summary>
        /// Buff 应用事件
        /// Buff apply event
        /// </summary>
        public event Action<string, string, BuffKind, double, int, string, string?, string?>? OnBuffApply;

        /// <summary>
        /// Buff 移除事件
        /// Buff remove event
        /// </summary>
        public event Action<string, string, string, string?>? OnBuffRemove;

        /// <summary>
        /// Buff Tick 事件 (DoT/HoT)
        /// Buff tick event (DoT/HoT)
        /// </summary>
        public event Action<string, string, BuffTickType, int, int, string?>? OnBuffTick;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public BuffIntegration(
            IGameClock clock,
            RngContext rng,
            Dictionary<string, CharacterBuffOwner> playerBuffOwners,
            Dictionary<string, EnemyBuffOwner> enemyBuffOwners,
            Func<IEnumerable<string>> getAlivePlayerIds,
            Func<IEnumerable<string>> getAliveEnemyIds)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _playerBuffOwners = playerBuffOwners ?? throw new ArgumentNullException(nameof(playerBuffOwners));
            _enemyBuffOwners = enemyBuffOwners ?? throw new ArgumentNullException(nameof(enemyBuffOwners));
            _getAlivePlayerIds = getAlivePlayerIds ?? throw new ArgumentNullException(nameof(getAlivePlayerIds));
            _getAliveEnemyIds = getAliveEnemyIds ?? throw new ArgumentNullException(nameof(getAliveEnemyIds));
        }

        /// <summary>
        /// 处理技能结果中的 Buff 操作
        /// Process buff operations from skill result
        /// </summary>
        public void ProcessBuffOperations(
            SkillCastResult result,
            string casterId,
            string? targetId,
            bool isCasterPlayer)
        {
            if (result.BuffOperations == null || result.BuffOperations.Count == 0)
                return;

            string? bundleId = result.BundleId;

            foreach (var operation in result.BuffOperations)
            {
                if (operation.Type == BuffOperationType.Apply)
                {
                    ApplyBuffOperation(operation, casterId, targetId, isCasterPlayer, bundleId);
                }
                else if (operation.Type == BuffOperationType.Remove)
                {
                    RemoveBuffOperation(operation, casterId, targetId, isCasterPlayer, bundleId);
                }
            }
        }

        /// <summary>
        /// 解析 Buff 目标
        /// Resolve buff targets
        /// </summary>
        public List<IBuffOwner> ResolveBuffTargets(
            BuffTarget targetType,
            string casterId,
            string? primaryTargetId,
            bool isCasterPlayer)
        {
            var targets = new List<IBuffOwner>();

            switch (targetType)
            {
                case BuffTarget.Self:
                    // 施法者自己 / Caster itself
                    if (isCasterPlayer)
                    {
                        if (_playerBuffOwners.TryGetValue(casterId, out var playerOwner))
                            targets.Add(playerOwner);
                    }
                    else
                    {
                        if (_enemyBuffOwners.TryGetValue(casterId, out var enemyOwner))
                            targets.Add(enemyOwner);
                    }
                    break;

                case BuffTarget.Target:
                    // 主要目标 / Primary target
                    if (!string.IsNullOrEmpty(primaryTargetId))
                    {
                        if (isCasterPlayer)
                        {
                            if (_enemyBuffOwners.TryGetValue(primaryTargetId, out var enemyOwner))
                                targets.Add(enemyOwner);
                        }
                        else
                        {
                            if (_playerBuffOwners.TryGetValue(primaryTargetId, out var playerOwner))
                                targets.Add(playerOwner);
                        }
                    }
                    break;

                case BuffTarget.AllEnemies:
                    // 所有敌人 / All enemies
                    if (isCasterPlayer)
                    {
                        foreach (var enemyId in _getAliveEnemyIds())
                        {
                            if (_enemyBuffOwners.TryGetValue(enemyId, out var enemyOwner))
                                targets.Add(enemyOwner);
                        }
                    }
                    else
                    {
                        foreach (var playerId in _getAlivePlayerIds())
                        {
                            if (_playerBuffOwners.TryGetValue(playerId, out var playerOwner))
                                targets.Add(playerOwner);
                        }
                    }
                    break;

                case BuffTarget.AllAllies:
                    // 所有队友（包括自己）/ All allies (including self)
                    if (isCasterPlayer)
                    {
                        foreach (var playerId in _getAlivePlayerIds())
                        {
                            if (_playerBuffOwners.TryGetValue(playerId, out var playerOwner))
                                targets.Add(playerOwner);
                        }
                    }
                    else
                    {
                        foreach (var enemyId in _getAliveEnemyIds())
                        {
                            if (_enemyBuffOwners.TryGetValue(enemyId, out var enemyOwner))
                                targets.Add(enemyOwner);
                        }
                    }
                    break;

                case BuffTarget.RandomEnemy:
                    // 随机敌人 / Random enemy
                    if (isCasterPlayer)
                    {
                        var aliveEnemies = _getAliveEnemyIds().ToList();
                        if (aliveEnemies.Count > 0)
                        {
                            var randomIndex = _rng.NextRange(0, aliveEnemies.Count - 1);
                            var randomId = aliveEnemies[randomIndex];
                            if (_enemyBuffOwners.TryGetValue(randomId, out var enemyOwner))
                                targets.Add(enemyOwner);
                        }
                    }
                    else
                    {
                        var alivePlayers = _getAlivePlayerIds().ToList();
                        if (alivePlayers.Count > 0)
                        {
                            var randomIndex = _rng.NextRange(0, alivePlayers.Count - 1);
                            var randomId = alivePlayers[randomIndex];
                            if (_playerBuffOwners.TryGetValue(randomId, out var playerOwner))
                                targets.Add(playerOwner);
                        }
                    }
                    break;

                case BuffTarget.LowestHpAlly:
                    // 血量最低的队友（按百分比）/ Lowest HP ally (by percentage)
                    if (isCasterPlayer)
                    {
                        IBuffOwner? lowestHpOwner = null;
                        double lowestHpPercentage = double.MaxValue;

                        foreach (var playerId in _getAlivePlayerIds())
                        {
                            if (_playerBuffOwners.TryGetValue(playerId, out var playerOwner))
                            {
                                double hpPercentage = playerOwner.MaxHp > 0
                                    ? (double)playerOwner.CurrentHp / playerOwner.MaxHp
                                    : 1.0;

                                if (hpPercentage < lowestHpPercentage)
                                {
                                    lowestHpPercentage = hpPercentage;
                                    lowestHpOwner = playerOwner;
                                }
                            }
                        }

                        if (lowestHpOwner != null)
                            targets.Add(lowestHpOwner);
                    }
                    else
                    {
                        IBuffOwner? lowestHpOwner = null;
                        double lowestHpPercentage = double.MaxValue;

                        foreach (var enemyId in _getAliveEnemyIds())
                        {
                            if (_enemyBuffOwners.TryGetValue(enemyId, out var enemyOwner))
                            {
                                double hpPercentage = enemyOwner.MaxHp > 0
                                    ? (double)enemyOwner.CurrentHp / enemyOwner.MaxHp
                                    : 1.0;

                                if (hpPercentage < lowestHpPercentage)
                                {
                                    lowestHpPercentage = hpPercentage;
                                    lowestHpOwner = enemyOwner;
                                }
                            }
                        }

                        if (lowestHpOwner != null)
                            targets.Add(lowestHpOwner);
                    }
                    break;
            }

            return targets;
        }

        /// <summary>
        /// 处理所有 Buff 的 Tick（DoT/HoT 和过期）
        /// Process all buff ticks (DoT/HoT and expiration)
        /// </summary>
        /// <param name="deltaTimeSec">时间增量（秒）/ Time delta in seconds</param>
        /// <param name="getPlayerMemberIsDead">检查玩家成员是否死亡的委托 / Delegate to check if player member is dead</param>
        /// <param name="getEnemyMemberIsDead">检查敌人成员是否死亡的委托 / Delegate to check if enemy member is dead</param>
        public void ProcessBuffTicks(
            double deltaTimeSec,
            Func<string, bool>? getPlayerMemberIsDead = null,
            Func<string, bool>? getEnemyMemberIsDead = null)
        {
            // 处理玩家 Buff / Process player buffs
            foreach (var kvp in _playerBuffOwners)
            {
                var charId = kvp.Key;
                var buffOwner = kvp.Value;

                // 检查成员是否死亡
                if (getPlayerMemberIsDead != null && getPlayerMemberIsDead(charId))
                    continue;

                ProcessEntityBuffs(buffOwner, deltaTimeSec);
            }

            // 处理敌人 Buff / Process enemy buffs
            foreach (var kvp in _enemyBuffOwners)
            {
                var enemyId = kvp.Key;
                var buffOwner = kvp.Value;

                // 检查成员是否死亡
                if (getEnemyMemberIsDead != null && getEnemyMemberIsDead(enemyId))
                    continue;

                ProcessEntityBuffs(buffOwner, deltaTimeSec);
            }
        }

        /// <summary>
        /// 处理单个实体的 Buff
        /// Process buffs for a single entity
        /// </summary>
        private void ProcessEntityBuffs(IBuffOwner buffOwner, double deltaTimeSec)
        {
            // 收集过期的 Buff / Collect expired buffs
            var expiredBuffs = new List<string>();

            foreach (var kvp in buffOwner.Buffs)
            {
                var buff = kvp.Value;

                // Tick buff（返回触发的 tick 次数）/ Tick buff (returns number of ticks triggered)
                int tickCount = buff.Tick(deltaTimeSec);

                // 处理 DoT/HoT / Process DoT/HoT
                if (tickCount > 0)
                {
                    if (buff.HasDamageOverTime())
                    {
                        int damagePerTick = buff.GetDamagePerTick();
                        int totalDamage = damagePerTick * tickCount;

                        var damageMeta = new DamageMeta("dot_tick", buff.Id);
                        buffOwner.ReceiveDamage(totalDamage, damageMeta);

                        // 触发 BuffTick 事件 (DoT) / Trigger BuffTick event (DoT)
                        OnBuffTick?.Invoke(
                            buffOwner.Id,
                            buff.Id,
                            BuffTickType.DamageOverTime,
                            totalDamage,
                            buffOwner.CurrentHp,
                            null);
                    }

                    if (buff.HasHealOverTime())
                    {
                        int healPerTick = buff.GetHealPerTick();
                        int totalHeal = healPerTick * tickCount;

                        var healMeta = new HealMeta("hot_tick", buff.Id);
                        buffOwner.ReceiveHeal(totalHeal, healMeta);

                        // 触发 BuffTick 事件 (HoT) / Trigger BuffTick event (HoT)
                        OnBuffTick?.Invoke(
                            buffOwner.Id,
                            buff.Id,
                            BuffTickType.HealOverTime,
                            totalHeal,
                            buffOwner.CurrentHp,
                            null);
                    }
                }

                // 检查是否过期 / Check if expired
                if (buff.IsExpired())
                {
                    expiredBuffs.Add(buff.Id);
                }
            }

            // 移除过期的 Buff / Remove expired buffs
            foreach (var buffId in expiredBuffs)
            {
                buffOwner.RemoveBuff(buffId, "expired");

                // 触发 BuffRemove 事件 (过期) / Trigger BuffRemove event (expired)
                OnBuffRemove?.Invoke(buffOwner.Id, buffId, "expired", null);
            }
        }

        /// <summary>
        /// 应用 Buff 操作
        /// Apply buff operation
        /// </summary>
        private void ApplyBuffOperation(
            BuffOperation operation,
            string casterId,
            string? targetId,
            bool isCasterPlayer,
            string? bundleId = null)
        {
            // 支持通过 BuffConfigId 引用配置化的 buff
            BuffInstance? templateToUse = null;
            BuffTarget targetToUse = operation.Target;

            if (!string.IsNullOrEmpty(operation.BuffConfigId))
            {
                // 从 BuffRepository 查找配置
                var buffConfig = BuffRepository.Instance.GetBuffById(operation.BuffConfigId);
                if (buffConfig == null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[BuffIntegration] ERROR: BuffConfig '{operation.BuffConfigId}' not found. " +
                        $"Caster: {casterId}, Target: {targetId}");
                    return;
                }

                templateToUse = buffConfig.ToBuffInstance("", operation.BuffTemplate?.SourceSkillId);
                targetToUse = operation.TargetOverride ?? buffConfig.DefaultTarget;
            }
            else if (operation.BuffTemplate != null)
            {
                templateToUse = operation.BuffTemplate;
            }
            else
            {
                return;
            }

            // 解析目标列表
            var targets = ResolveBuffTargets(targetToUse, casterId, targetId, isCasterPlayer);

            foreach (var target in targets)
            {
                // 克隆 buff 模板并设置 OwnerId
                var buffToApply = new BuffInstance(
                    id: templateToUse.Id,
                    ownerId: target.Id,
                    kind: templateToUse.Kind,
                    effects: new List<BuffEffect>(templateToUse.Effects),
                    stackingPolicy: templateToUse.StackingPolicy,
                    durationSec: templateToUse.RemainingDurationSec,
                    tickIntervalSec: templateToUse.TickIntervalSec,
                    maxStacks: templateToUse.MaxStacks,
                    appliedAtMs: _clock.NowMs
                );

                target.ApplyBuff(buffToApply);

                // 触发 BuffApply 事件
                string effectsSummary = string.Join(", ", buffToApply.Effects.Select(e =>
                    $"{e.Type}={e.Value}"));
                OnBuffApply?.Invoke(
                    target.Id,
                    buffToApply.Id,
                    buffToApply.Kind,
                    buffToApply.RemainingDurationSec ?? 0,
                    buffToApply.Stacks,
                    effectsSummary,
                    templateToUse.SourceSkillId,
                    bundleId);
            }
        }

        /// <summary>
        /// 移除 Buff 操作
        /// Remove buff operation
        /// </summary>
        private void RemoveBuffOperation(
            BuffOperation operation,
            string casterId,
            string? targetId,
            bool isCasterPlayer,
            string? bundleId = null)
        {
            if (string.IsNullOrEmpty(operation.BuffIdToRemove))
                return;

            var targets = ResolveBuffTargets(operation.Target, casterId, targetId, isCasterPlayer);

            foreach (var target in targets)
            {
                string reason = operation.Reason ?? "skill_effect";

                if (operation.StacksToRemove.HasValue && operation.StacksToRemove.Value > 0)
                {
                    target.ReduceBuffStacks(operation.BuffIdToRemove, operation.StacksToRemove.Value, reason);
                }
                else
                {
                    target.RemoveBuff(operation.BuffIdToRemove, reason);
                }

                OnBuffRemove?.Invoke(target.Id, operation.BuffIdToRemove, reason, bundleId);
            }
        }
    }
}
