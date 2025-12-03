using System;
using System.Collections.Generic;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Battle.Execution
{
    /// <summary>
    /// 伤害应用器 - 统一处理立即和延迟伤害的应用
    /// Damage Applier - unified handler for immediate and delayed damage application
    /// 
    /// 战斗重构 Phase 2: 从 MultiBattleInstance 提取伤害应用逻辑
    /// Battle Refactor Phase 2: Extract damage application logic from MultiBattleInstance
    /// 
    /// 注意：当前版本为独立组件，可被 MultiBattleInstance 选择性使用
    /// Note: Current version is a standalone component, can be optionally used by MultiBattleInstance
    /// </summary>
    public sealed class DamageApplier
    {
        private readonly PendingDamageQueue _pendingQueue;
        private readonly IGameClock _clock;
        
        /// <summary>
        /// 伤害应用事件（供外部订阅，记录事件等）
        /// Damage applied event (for external subscription, event recording, etc.)
        /// </summary>
        public event Action<DamageAppliedEventArgs>? DamageApplied;

        /// <summary>
        /// 创建伤害应用器
        /// Create damage applier
        /// </summary>
        /// <param name="pendingQueue">延迟伤害队列 / Pending damage queue</param>
        /// <param name="clock">游戏时钟 / Game clock</param>
        public DamageApplier(PendingDamageQueue pendingQueue, IGameClock clock)
        {
            _pendingQueue = pendingQueue ?? throw new ArgumentNullException(nameof(pendingQueue));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// 获取延迟伤害队列
        /// Get pending damage queue
        /// </summary>
        public PendingDamageQueue PendingQueue => _pendingQueue;

        /// <summary>
        /// 处理技能施放结果中的所有伤害实例
        /// Process all damage instances from skill cast result
        /// </summary>
        /// <param name="result">技能施放结果 / Skill cast result</param>
        /// <param name="applyImmediateCallback">立即伤害回调 / Immediate damage callback</param>
        public void ProcessDamageInstances(
            SkillCastResult result,
            Action<DamageInstance>? applyImmediateCallback = null)
        {
            if (result.DamageInstances == null || result.DamageInstances.Count == 0)
            {
                return;
            }

            double currentTimeSec = _clock.NowMs / 1000.0;

            foreach (var instance in result.DamageInstances)
            {
                if (instance.IsImmediate)
                {
                    // 立即伤害：直接应用
                    // Immediate damage: apply directly
                    applyImmediateCallback?.Invoke(instance);
                    RaiseDamageApplied(instance, isImmediate: true);
                }
                else
                {
                    // 延迟伤害：加入队列
                    // Delayed damage: enqueue
                    _pendingQueue.Enqueue(instance, currentTimeSec);
                }
            }
        }

        /// <summary>
        /// 处理所有到期的延迟伤害
        /// Process all pending damages that are ready
        /// </summary>
        /// <param name="applyCallback">伤害应用回调 / Damage apply callback</param>
        /// <returns>处理的伤害数量 / Number of damages processed</returns>
        public int ProcessPendingDamages(Action<DamageInstance>? applyCallback = null)
        {
            double currentTimeSec = _clock.NowMs / 1000.0;
            var readyDamages = _pendingQueue.DequeueReady(currentTimeSec);

            foreach (var instance in readyDamages)
            {
                applyCallback?.Invoke(instance);
                RaiseDamageApplied(instance, isImmediate: false);
            }

            return readyDamages.Count;
        }

        /// <summary>
        /// 目标死亡时清理相关的待应用伤害
        /// Clean up pending damages when target dies
        /// </summary>
        /// <param name="targetId">目标ID / Target ID</param>
        /// <returns>移除的伤害数量 / Number of damages removed</returns>
        public int OnTargetDeath(string targetId)
        {
            return _pendingQueue.RemoveByTarget(targetId);
        }

        /// <summary>
        /// 技能被打断时清理相关的待应用伤害
        /// Clean up pending damages when skill is interrupted
        /// </summary>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="bundleId">Bundle ID（可选）/ Bundle ID (optional)</param>
        /// <returns>移除的伤害数量 / Number of damages removed</returns>
        public int OnSkillInterrupted(string skillId, string? bundleId = null)
        {
            return _pendingQueue.RemoveBySkill(skillId, bundleId);
        }

        /// <summary>
        /// 战斗结束时清理所有待应用伤害
        /// Clear all pending damages when battle ends
        /// </summary>
        public void OnBattleEnd()
        {
            _pendingQueue.Clear();
        }

        /// <summary>
        /// 获取下一个延迟伤害的应用时间（毫秒）
        /// Get the next pending damage apply time (in milliseconds)
        /// </summary>
        /// <returns>下一个应用时间，如果没有则返回 null / Next apply time, or null if none</returns>
        public int? GetNextPendingTimeMs()
        {
            var nextTimeSec = _pendingQueue.PeekNextTime();
            return nextTimeSec.HasValue ? (int)(nextTimeSec.Value * 1000) : null;
        }

        /// <summary>
        /// 是否有待应用的延迟伤害
        /// Whether there are pending delayed damages
        /// </summary>
        public bool HasPendingDamages => !_pendingQueue.IsEmpty;

        /// <summary>
        /// 待应用的延迟伤害数量
        /// Number of pending delayed damages
        /// </summary>
        public int PendingCount => _pendingQueue.Count;

        private void RaiseDamageApplied(DamageInstance instance, bool isImmediate)
        {
            DamageApplied?.Invoke(new DamageAppliedEventArgs
            {
                Instance = instance,
                IsImmediate = isImmediate,
                AppliedAtMs = _clock.NowMs
            });
        }
    }

    /// <summary>
    /// 伤害应用事件参数
    /// Damage applied event arguments
    /// </summary>
    public sealed class DamageAppliedEventArgs : EventArgs
    {
        /// <summary>
        /// 伤害实例
        /// Damage instance
        /// </summary>
        public DamageInstance Instance { get; set; } = null!;

        /// <summary>
        /// 是否为立即伤害（false 表示延迟伤害）
        /// Whether this is immediate damage (false means delayed damage)
        /// </summary>
        public bool IsImmediate { get; set; }

        /// <summary>
        /// 应用时间（毫秒）
        /// Applied time (milliseconds)
        /// </summary>
        public int AppliedAtMs { get; set; }
    }
}
