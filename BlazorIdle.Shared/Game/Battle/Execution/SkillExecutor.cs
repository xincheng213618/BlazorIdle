using System;
using System.Collections.Generic;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Battle.Execution
{
    /// <summary>
    /// 技能执行器 - 统一处理技能执行流程
    /// Skill Executor - unified handler for skill execution flow
    /// 
    /// 战斗重构 Phase 2: 从 MultiBattleInstance 提取技能执行逻辑
    /// Battle Refactor Phase 2: Extract skill execution logic from MultiBattleInstance
    /// 
    /// 职责：
    /// - 协调 SkillResolver 计算伤害
    /// - 协调 DamageApplier 应用伤害
    /// - 处理 Buff 操作和资源变化的分发
    /// - 处理攻击触发器的分发
    /// 
    /// Responsibilities:
    /// - Coordinate SkillResolver to calculate damage
    /// - Coordinate DamageApplier to apply damage
    /// - Distribute Buff operations and resource changes
    /// - Distribute attack trigger processing
    /// </summary>
    public sealed class SkillExecutor
    {
        private readonly ISkillResolver _skillResolver;
        private readonly DamageApplier _damageApplier;
        private readonly IGameClock _clock;

        /// <summary>
        /// 技能执行完成事件
        /// Skill execution completed event
        /// </summary>
        public event Action<SkillExecutionResult>? SkillExecuted;

        /// <summary>
        /// Buff 操作请求事件（由外部订阅处理）
        /// Buff operation request event (handled by external subscriber)
        /// </summary>
        public event Action<BuffOperationRequest>? BuffOperationRequested;

        /// <summary>
        /// 资源变化请求事件（由外部订阅处理）
        /// Resource change request event (handled by external subscriber)
        /// </summary>
        public event Action<ResourceChangeRequest>? ResourceChangeRequested;

        /// <summary>
        /// 攻击触发器请求事件（由外部订阅处理）
        /// Attack trigger request event (handled by external subscriber)
        /// </summary>
        public event Action<AttackTriggerRequest>? AttackTriggerRequested;

        /// <summary>
        /// 创建技能执行器
        /// Create skill executor
        /// </summary>
        /// <param name="skillResolver">技能解析器 / Skill resolver</param>
        /// <param name="damageApplier">伤害应用器 / Damage applier</param>
        /// <param name="clock">游戏时钟 / Game clock</param>
        public SkillExecutor(
            ISkillResolver skillResolver,
            DamageApplier damageApplier,
            IGameClock clock)
        {
            _skillResolver = skillResolver ?? throw new ArgumentNullException(nameof(skillResolver));
            _damageApplier = damageApplier ?? throw new ArgumentNullException(nameof(damageApplier));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// 获取伤害应用器
        /// Get damage applier
        /// </summary>
        public DamageApplier DamageApplier => _damageApplier;

        /// <summary>
        /// 执行技能
        /// Execute skill
        /// </summary>
        /// <param name="request">执行请求 / Execution request</param>
        /// <returns>执行结果 / Execution result</returns>
        public SkillExecutionResult Execute(SkillExecutionRequest request)
        {
            // 1. 使用 SkillResolver 计算技能效果
            // 1. Calculate skill effects using SkillResolver
            var opts = new SkillCastOptions
            {
                SourceTrack = request.SourceTrack,
                CasterId = request.CasterId,
                BundleId = request.BundleId,
                ForceCrit = request.ForceCrit
            };

            var castResult = _skillResolver.Cast(request.SkillId, request.Context, opts);

            // 2. 处理伤害实例（立即 + 延迟）
            // 2. Process damage instances (immediate + delayed)
            var immediateDamages = new List<DamageInstance>();
            var delayedDamages = new List<DamageInstance>();

            if (castResult.HasMultiHit && castResult.DamageInstances != null)
            {
                _damageApplier.ProcessDamageInstances(castResult, instance =>
                {
                    immediateDamages.Add(instance);
                    // 通知外部应用伤害
                    // Notify external to apply damage
                    request.OnImmediateDamage?.Invoke(instance);
                });

                // 记录本次技能产生的延迟伤害
                // Record delayed damages from this skill cast
                // 注：这里获取的是刚入队的伤害实例，不包含之前已在队列中的其他技能伤害
                // Note: This captures the just-enqueued damages, not including other skills' pending damages
                if (castResult.DamageInstances != null)
                {
                    foreach (var instance in castResult.DamageInstances)
                    {
                        if (instance.IsPending)
                        {
                            delayedDamages.Add(instance);
                        }
                    }
                }
            }
            else if (castResult.DamageDealt > 0)
            {
                // 旧式单段伤害，创建一个立即伤害实例
                // Legacy single-hit damage, create an immediate damage instance
                var singleInstance = new DamageInstance
                {
                    HitIndex = 0,
                    Damage = castResult.DamageDealt,
                    IsCrit = castResult.IsCrit,
                    SkillId = request.SkillId,
                    CasterId = request.CasterId,
                    IsCasterPlayer = request.IsCasterPlayer,
                    BundleId = castResult.BundleId,
                    HasElementAdvantage = castResult.HasElementAdvantage,
                    ElementMultiplier = castResult.ElementMultiplier,
                    DetailedResult = castResult.DetailedDamageResult,
                    ApplyAtSec = 0,
                    TargetId = request.PrimaryTargetId ?? ""
                };
                immediateDamages.Add(singleInstance);
                request.OnImmediateDamage?.Invoke(singleInstance);
            }

            // 3. 请求处理 Buff 操作
            // 3. Request Buff operation processing
            if (castResult.BuffOperations.Count > 0)
            {
                BuffOperationRequested?.Invoke(new BuffOperationRequest
                {
                    CasterId = request.CasterId,
                    PrimaryTargetId = request.PrimaryTargetId,
                    IsCasterPlayer = request.IsCasterPlayer,
                    SkillId = request.SkillId,
                    BundleId = castResult.BundleId,
                    Operations = castResult.BuffOperations,
                    CastResult = castResult
                });
            }

            // 4. 请求处理资源变化
            // 4. Request resource change processing
            if (castResult.ResourceChanges.Count > 0)
            {
                ResourceChangeRequested?.Invoke(new ResourceChangeRequest
                {
                    CasterId = request.CasterId,
                    IsCasterPlayer = request.IsCasterPlayer,
                    SkillId = request.SkillId,
                    BundleId = castResult.BundleId,
                    Changes = castResult.ResourceChanges
                });
            }

            // 5. 请求处理攻击触发器
            // 5. Request attack trigger processing
            if (request.ProcessTriggers)
            {
                AttackTriggerRequested?.Invoke(new AttackTriggerRequest
                {
                    CasterId = request.CasterId,
                    SkillId = request.SkillId,
                    IsCrit = castResult.IsCrit,
                    IsCasterPlayer = request.IsCasterPlayer
                });
            }

            // 6. 构建执行结果
            // 6. Build execution result
            var result = new SkillExecutionResult
            {
                Success = true,
                SkillId = request.SkillId,
                CasterId = request.CasterId,
                CastResult = castResult,
                ImmediateDamages = immediateDamages,
                DelayedDamages = delayedDamages,
                ExecutedAtMs = _clock.NowMs
            };

            // 7. 触发执行完成事件
            // 7. Raise execution completed event
            SkillExecuted?.Invoke(result);

            return result;
        }

        /// <summary>
        /// 处理待应用的延迟伤害
        /// Process pending delayed damages
        /// </summary>
        /// <param name="applyCallback">伤害应用回调 / Damage apply callback</param>
        /// <returns>处理的伤害数量 / Number of damages processed</returns>
        public int ProcessPendingDamages(Action<DamageInstance>? applyCallback = null)
        {
            return _damageApplier.ProcessPendingDamages(applyCallback);
        }
    }

    /// <summary>
    /// 技能执行请求
    /// Skill execution request
    /// </summary>
    public sealed class SkillExecutionRequest
    {
        /// <summary>
        /// 施法者ID
        /// Caster ID
        /// </summary>
        public string CasterId { get; set; } = "";

        /// <summary>
        /// 技能ID
        /// Skill ID
        /// </summary>
        public string SkillId { get; set; } = "";

        /// <summary>
        /// 战斗上下文
        /// Battle context
        /// </summary>
        public BattleContext Context { get; set; } = null!;

        /// <summary>
        /// 来源轨道
        /// Source track
        /// </summary>
        public string SourceTrack { get; set; } = "";

        /// <summary>
        /// 施法者是否为玩家
        /// Whether caster is player
        /// </summary>
        public bool IsCasterPlayer { get; set; } = true;

        /// <summary>
        /// 主要目标ID（用于 Buff 等）
        /// Primary target ID (for Buff, etc.)
        /// </summary>
        public string? PrimaryTargetId { get; set; }

        /// <summary>
        /// Bundle ID（可选）
        /// Bundle ID (optional)
        /// </summary>
        public string? BundleId { get; set; }

        /// <summary>
        /// 是否强制暴击
        /// Whether to force crit
        /// </summary>
        public bool ForceCrit { get; set; }

        /// <summary>
        /// 是否处理触发器
        /// Whether to process triggers
        /// </summary>
        public bool ProcessTriggers { get; set; } = true;

        /// <summary>
        /// 立即伤害回调
        /// Immediate damage callback
        /// </summary>
        public Action<DamageInstance>? OnImmediateDamage { get; set; }
    }

    /// <summary>
    /// 技能执行结果
    /// Skill execution result
    /// </summary>
    public sealed class SkillExecutionResult
    {
        /// <summary>
        /// 是否成功执行
        /// Whether execution was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 技能ID
        /// Skill ID
        /// </summary>
        public string SkillId { get; set; } = "";

        /// <summary>
        /// 施法者ID
        /// Caster ID
        /// </summary>
        public string CasterId { get; set; } = "";

        /// <summary>
        /// 技能施放结果
        /// Skill cast result
        /// </summary>
        public SkillCastResult CastResult { get; set; } = null!;

        /// <summary>
        /// 立即应用的伤害列表
        /// List of immediately applied damages
        /// </summary>
        public List<DamageInstance> ImmediateDamages { get; set; } = new();

        /// <summary>
        /// 延迟应用的伤害列表
        /// List of delayed damages
        /// </summary>
        public List<DamageInstance> DelayedDamages { get; set; } = new();

        /// <summary>
        /// 执行时间（毫秒）
        /// Execution time (milliseconds)
        /// </summary>
        public int ExecutedAtMs { get; set; }
    }

    /// <summary>
    /// Buff 操作请求
    /// Buff operation request
    /// </summary>
    public sealed class BuffOperationRequest
    {
        public string CasterId { get; set; } = "";
        public string? PrimaryTargetId { get; set; }
        public bool IsCasterPlayer { get; set; }
        public string SkillId { get; set; } = "";
        public string? BundleId { get; set; }
        public List<BuffOperation> Operations { get; set; } = new();
        public SkillCastResult CastResult { get; set; } = null!;
    }

    /// <summary>
    /// 资源变化请求
    /// Resource change request
    /// </summary>
    public sealed class ResourceChangeRequest
    {
        public string CasterId { get; set; } = "";
        public bool IsCasterPlayer { get; set; }
        public string SkillId { get; set; } = "";
        public string? BundleId { get; set; }
        public Dictionary<string, int> Changes { get; set; } = new();
    }

    /// <summary>
    /// 攻击触发器请求
    /// Attack trigger request
    /// </summary>
    public sealed class AttackTriggerRequest
    {
        public string CasterId { get; set; } = "";
        public string? SkillId { get; set; }
        public bool IsCrit { get; set; }
        public bool IsCasterPlayer { get; set; }
    }
}
