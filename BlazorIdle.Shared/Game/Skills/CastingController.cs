using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 施法控制器 - Phase 8: 完整实现
    /// Casting controller - Phase 8: Full implementation
    /// 支持多角色同时施法、急速加成、中断处理
    /// Supports multi-character casting, haste bonus, and interrupt handling
    /// </summary>
    public sealed class CastingController
    {
        /// <summary>
        /// 活跃的施法字典 (casterId -> ActiveCast)
        /// Active casts dictionary (casterId -> ActiveCast)
        /// </summary>
        private readonly Dictionary<string, ActiveCast> _activeCasts = new();

        /// <summary>
        /// 施法完成事件委托
        /// Cast complete event delegate
        /// </summary>
        public event Action<string, string>? OnCastComplete; // (casterId, skillId)

        /// <summary>
        /// 施法中断事件委托
        /// Cast interrupt event delegate
        /// </summary>
        public event Action<string, string, string>? OnCastInterrupt; // (casterId, skillId, reason)

        /// <summary>
        /// 是否有任何角色正在施法
        /// Whether any character is currently casting
        /// </summary>
        public bool IsCasting => _activeCasts.Count > 0;

        /// <summary>
        /// 当前活跃的施法（向后兼容）- 返回第一个施法
        /// Currently active cast (backward compatible) - returns first cast
        /// </summary>
        [Obsolete("Use IsCastingForCaster(casterId) and GetActiveCast(casterId) instead")]
        public ActiveCast? ActiveCast => _activeCasts.Values.FirstOrDefault();

        /// <summary>
        /// 检查指定施法者是否正在施法
        /// Check if specified caster is currently casting
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <returns>是否正在施法 / Whether casting</returns>
        public bool IsCastingForCaster(string casterId)
        {
            return _activeCasts.ContainsKey(casterId);
        }

        /// <summary>
        /// 获取指定施法者的施法状态
        /// Get cast status for specified caster
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <returns>施法状态，如果不存在返回 null / Cast status, null if not exists</returns>
        public ActiveCast? GetActiveCast(string casterId)
        {
            return _activeCasts.GetValueOrDefault(casterId);
        }

        /// <summary>
        /// 开始施法
        /// Start casting
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="baseCastTimeSec">基础施法时间（秒）/ Base cast time in seconds</param>
        /// <param name="hastePercent">急速百分比 / Haste percentage</param>
        /// <param name="pauseAttackTrack">是否暂停攻击轨道 / Whether to pause attack track</param>
        /// <returns>是否成功开始施法 / Whether cast started successfully</returns>
        public bool StartCast(string casterId, string skillId, double baseCastTimeSec, double hastePercent = 0, bool pauseAttackTrack = true)
        {
            // 如果已经在施法，取消之前的施法
            // If already casting, cancel previous cast
            if (_activeCasts.ContainsKey(casterId))
            {
                CancelCast(casterId, "new_cast_started");
            }

            // 应用急速加成：castTime = baseTime / (1 + haste%)
            // Apply haste bonus: castTime = baseTime / (1 + haste%)
            double actualCastTime = baseCastTimeSec / (1.0 + hastePercent / 100.0);

            // 创建施法状态
            // Create cast status
            var cast = new ActiveCast
            {
                SkillId = skillId,
                CastTimeSec = actualCastTime,
                ElapsedSec = 0,
                PauseAttackTrack = pauseAttackTrack,
                PauseSpecialTrack = false // 默认不暂停特殊轨道 / Don't pause special track by default
            };

            _activeCasts[casterId] = cast;
            return true;
        }

        /// <summary>
        /// 推进施法控制器时间
        /// Advance casting controller time
        /// </summary>
        /// <param name="dt">时间增量（秒）/ Time delta in seconds</param>
        public void Tick(double dt)
        {
            if (dt <= 0) return;

            // 收集完成的施法
            // Collect completed casts
            var completedCasts = new List<(string casterId, string skillId)>();

            foreach (var kvp in _activeCasts.ToList())
            {
                var casterId = kvp.Key;
                var cast = kvp.Value;

                // 推进施法进度
                // Advance cast progress
                cast.ElapsedSec += dt;

                // 检查是否完成
                // Check if completed
                if (cast.ElapsedSec >= cast.CastTimeSec)
                {
                    completedCasts.Add((casterId, cast.SkillId));
                }
            }

            // 处理完成的施法
            // Handle completed casts
            foreach (var (casterId, skillId) in completedCasts)
            {
                CompleteCast(casterId, skillId);
            }
        }

        /// <summary>
        /// 完成施法（内部方法）
        /// Complete cast (internal method)
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        private void CompleteCast(string casterId, string skillId)
        {
            if (_activeCasts.Remove(casterId))
            {
                OnCastComplete?.Invoke(casterId, skillId);
            }
        }

        /// <summary>
        /// 取消施法
        /// Cancel casting
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="reason">中断原因 / Interrupt reason</param>
        /// <returns>是否成功取消 / Whether canceled successfully</returns>
        public bool CancelCast(string casterId, string reason)
        {
            if (_activeCasts.TryGetValue(casterId, out var cast))
            {
                _activeCasts.Remove(casterId);
                OnCastInterrupt?.Invoke(casterId, cast.SkillId, reason);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取施法进度（0-1之间）
        /// Get cast progress (between 0-1)
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <returns>施法进度，如果不存在返回 0 / Cast progress, 0 if not exists</returns>
        public double GetCastProgress(string casterId)
        {
            if (_activeCasts.TryGetValue(casterId, out var cast) && cast.CastTimeSec > 0)
            {
                return Math.Min(cast.ElapsedSec / cast.CastTimeSec, 1.0);
            }
            return 0;
        }

        /// <summary>
        /// 获取剩余施法时间（秒）
        /// Get remaining cast time in seconds
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <returns>剩余时间，如果不存在返回 0 / Remaining time, 0 if not exists</returns>
        public double GetRemainingCastTime(string casterId)
        {
            if (_activeCasts.TryGetValue(casterId, out var cast))
            {
                return Math.Max(cast.CastTimeSec - cast.ElapsedSec, 0);
            }
            return 0;
        }

        /// <summary>
        /// 获取当前被暂停的轨道列表
        /// Get list of currently paused tracks
        /// </summary>
        /// <returns>被暂停轨道的施法者ID列表 / List of caster IDs with paused tracks</returns>
        public List<string> GetPausedTracks()
        {
            return _activeCasts
                .Where(kvp => kvp.Value.PauseAttackTrack)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// 清除所有施法
        /// Clear all casts
        /// </summary>
        public void Clear()
        {
            _activeCasts.Clear();
        }
    }
}
