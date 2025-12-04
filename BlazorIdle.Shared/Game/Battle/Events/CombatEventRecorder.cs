using System;
using System.Collections.Generic;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Config;

namespace BlazorIdle.Game.Battle.Events
{
    /// <summary>
    /// 战斗事件记录器 - 从 MultiBattleInstance 提取的事件记录逻辑
    /// Combat event recorder - Event recording logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 记录伤害事件
    /// - 记录 Buff 事件 (Apply/Remove/Tick)
    /// - 记录资源事件
    /// - 记录施法事件
    /// - 记录治疗事件
    /// - 统一事件格式和聚合逻辑
    /// </summary>
    public class CombatEventRecorder
    {
        private readonly IGameClock _clock;
        private readonly RngContext _rng;
        private readonly CombatConfig _combatConfig;
        private readonly SegmentAggregator _aggregator;
        private readonly List<CombatSegment> _segments;

        /// <summary>
        /// Buff 应用事件
        /// </summary>
        public event Action<BuffApplyEvent>? OnBuffApplied;

        /// <summary>
        /// Buff 移除事件
        /// </summary>
        public event Action<BuffRemoveEvent>? OnBuffRemoved;

        /// <summary>
        /// Buff Tick 事件
        /// </summary>
        public event Action<BuffTickEvent>? OnBuffTicked;

        /// <summary>
        /// 治疗事件
        /// </summary>
        public event Action<HealEvent>? OnHealed;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public CombatEventRecorder(
            IGameClock clock,
            RngContext rng,
            CombatConfig combatConfig,
            SegmentAggregator aggregator,
            List<CombatSegment> segments)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _combatConfig = combatConfig ?? throw new ArgumentNullException(nameof(combatConfig));
            _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
            _segments = segments ?? throw new ArgumentNullException(nameof(segments));
        }

        /// <summary>
        /// 记录资源变化事件
        /// Record resource change event
        /// </summary>
        public void RecordResourceGain(
            string actorId,
            string bucketId,
            int delta,
            int newValue,
            string reason,
            string? skillId = null,
            string? bundleId = null)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new ResourceGainEvent
                {
                    TimeMs = _clock.NowMs,
                    ActorId = actorId,
                    BucketId = bucketId,
                    Delta = delta,
                    NewValue = newValue,
                    Reason = reason,
                    SkillId = skillId,
                    BundleId = bundleId,
                    Attacker = ActorType.Player,
                    Defender = ActorType.Player,
                    Source = EventSource.Attack,
                    Damage = 0,
                    Crit = false,
                    RngIndexAfter = _rng.Index,
                    DefenderHpAfter = 0
                };

                var flushed = _aggregator.AddEvent(evt);
                if (flushed != null) _segments.Add(flushed);
            }
        }

        /// <summary>
        /// 记录 Buff 应用事件
        /// Record buff apply event
        /// </summary>
        public void RecordBuffApply(
            string ownerId,
            string buffId,
            BuffKind kind,
            double? durationSec,
            int stacks,
            string effectsSummary,
            string? sourceSkillId = null,
            string? bundleId = null)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new BuffApplyEvent
                {
                    TimeMs = _clock.NowMs,
                    OwnerId = ownerId,
                    BuffId = buffId,
                    Kind = kind,
                    DurationSec = durationSec,
                    Stacks = stacks,
                    EffectsSummary = effectsSummary,
                    SourceSkillId = sourceSkillId,
                    BundleId = bundleId,
                    Attacker = ActorType.Player,
                    Defender = ActorType.Player,
                    Source = EventSource.Attack,
                    Damage = 0,
                    Crit = false,
                    RngIndexAfter = _rng.Index,
                    DefenderHpAfter = 0
                };

                var flushed = _aggregator.AddEvent(evt);
                if (flushed != null) _segments.Add(flushed);

                // 触发 Buff 应用事件
                OnBuffApplied?.Invoke(evt);
            }
        }

        /// <summary>
        /// 记录 Buff 移除事件
        /// Record buff remove event
        /// </summary>
        public void RecordBuffRemove(
            string ownerId,
            string buffId,
            string reason,
            string? bundleId = null)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new BuffRemoveEvent
                {
                    TimeMs = _clock.NowMs,
                    OwnerId = ownerId,
                    BuffId = buffId,
                    Reason = reason,
                    BundleId = bundleId,
                    Attacker = ActorType.Player,
                    Defender = ActorType.Player,
                    Source = EventSource.Attack,
                    Damage = 0,
                    Crit = false,
                    RngIndexAfter = _rng.Index,
                    DefenderHpAfter = 0
                };

                var flushed = _aggregator.AddEvent(evt);
                if (flushed != null) _segments.Add(flushed);

                // 触发 Buff 移除事件
                OnBuffRemoved?.Invoke(evt);
            }
        }

        /// <summary>
        /// 记录 Buff Tick 事件
        /// Record buff tick event
        /// </summary>
        public void RecordBuffTick(
            string ownerId,
            string buffId,
            BuffTickType tickType,
            int amount,
            int resultingHp,
            string? bundleId = null)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new BuffTickEvent
                {
                    TimeMs = _clock.NowMs,
                    OwnerId = ownerId,
                    BuffId = buffId,
                    TickType = tickType,
                    Amount = amount,
                    ResultingHp = resultingHp,
                    BundleId = bundleId,
                    Attacker = ActorType.Player,
                    Defender = ActorType.Player,
                    Source = EventSource.Attack,
                    Damage = 0,
                    Crit = false,
                    RngIndexAfter = _rng.Index,
                    DefenderHpAfter = 0
                };

                var flushed = _aggregator.AddEvent(evt);
                if (flushed != null) _segments.Add(flushed);

                // 触发 Buff Tick 事件
                OnBuffTicked?.Invoke(evt);
            }
        }

        /// <summary>
        /// 记录治疗事件
        /// Record heal event
        /// </summary>
        public void RecordHeal(
            string ownerId,
            int amount,
            int resultingHp,
            string healSource,
            string? bundleId = null)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new HealEvent
                {
                    TimeMs = _clock.NowMs,
                    OwnerId = ownerId,
                    Amount = amount,
                    ResultingHp = resultingHp,
                    BundleId = bundleId,
                    Source = healSource,
                    Attacker = ActorType.Player,
                    Defender = ActorType.Player,
                    Damage = 0,
                    Crit = false,
                    RngIndexAfter = _rng.Index,
                    DefenderHpAfter = 0
                };

                var flushed = _aggregator.AddEvent(evt);
                if (flushed != null) _segments.Add(flushed);

                // 触发治疗事件
                OnHealed?.Invoke(evt);
            }
        }

        // ===============================
        // 施法事件 / Casting Events
        // ===============================
        // 注意：施法事件不通过 SegmentAggregator 聚合，而是直接触发回调
        // 这是因为施法事件（CastStartEvent, CastCompleteEvent, CastInterruptEvent）
        // 不继承自 CombatEvent，无法添加到聚合器中
        // Note: Cast events are fired directly instead of through SegmentAggregator
        // because cast events (CastStartEvent, CastCompleteEvent, CastInterruptEvent)
        // don't inherit from CombatEvent and cannot be added to the aggregator

        /// <summary>
        /// 施法开始事件
        /// </summary>
        public event Action<CastStartEvent>? OnCastStarted;

        /// <summary>
        /// 施法完成事件
        /// </summary>
        public event Action<CastCompleteEvent>? OnCastCompleted;

        /// <summary>
        /// 施法中断事件
        /// </summary>
        public event Action<CastInterruptEvent>? OnCastInterrupted;

        /// <summary>
        /// 记录施法开始事件
        /// Record cast start event
        /// </summary>
        public void RecordCastStart(
            string casterId,
            string skillId,
            double castTimeSec,
            bool pauseAttackTrack,
            bool isPlayer = true)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new CastStartEvent
                {
                    TimeMs = _clock.NowMs,
                    CasterId = casterId,
                    SkillId = skillId,
                    CastTimeSec = castTimeSec,
                    PauseAttackTrack = pauseAttackTrack
                };

                // Cast events are fired directly, not through aggregator
                OnCastStarted?.Invoke(evt);
            }
        }

        /// <summary>
        /// 记录施法完成事件
        /// Record cast complete event
        /// </summary>
        public void RecordCastComplete(
            string casterId,
            string skillId,
            double actualCastTimeSec)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new CastCompleteEvent
                {
                    TimeMs = _clock.NowMs,
                    CasterId = casterId,
                    SkillId = skillId,
                    ActualCastTimeSec = actualCastTimeSec
                };

                // Cast events are fired directly, not through aggregator
                OnCastCompleted?.Invoke(evt);
            }
        }

        /// <summary>
        /// 记录施法中断事件
        /// Record cast interrupt event
        /// </summary>
        public void RecordCastInterrupt(
            string casterId,
            string skillId,
            string reason,
            double elapsedSec)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new CastInterruptEvent
                {
                    TimeMs = _clock.NowMs,
                    CasterId = casterId,
                    SkillId = skillId,
                    Reason = reason,
                    ElapsedSec = elapsedSec
                };

                // Cast events are fired directly, not through aggregator
                OnCastInterrupted?.Invoke(evt);
            }
        }
    }
}
