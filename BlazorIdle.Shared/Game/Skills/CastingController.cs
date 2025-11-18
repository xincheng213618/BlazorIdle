using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 施法控制器 - Phase 9: 支持施法完成通知
    /// Casting controller - Phase 9: Support cast completion notification
    /// </summary>
    public sealed class CastingController
    {
        private readonly Dictionary<string, CastState> _activeCasts = new();
        private readonly List<CastCompletionEvent> _completedCasts = new();

        /// <summary>
        /// 开始施法
        /// Start casting
        /// </summary>
        public void StartCast(string casterId, string skillId, double castTimeMs)
        {
            _activeCasts[casterId] = new CastState
            {
                CasterId = casterId,
                SkillId = skillId,
                RemainingMs = castTimeMs,
                TotalMs = castTimeMs
            };
        }

        /// <summary>
        /// 推进施法控制器时间
        /// Advance casting controller time
        /// </summary>
        /// <param name="dt">时间增量（秒）</param>
        public void Tick(double dt)
        {
            _completedCasts.Clear();
            double dtMs = dt * 1000.0;

            var castersToRemove = new List<string>();

            foreach (var kvp in _activeCasts)
            {
                var state = kvp.Value;
                state.RemainingMs -= dtMs;

                if (state.RemainingMs <= 0)
                {
                    // 施法完成
                    _completedCasts.Add(new CastCompletionEvent
                    {
                        CasterId = state.CasterId,
                        SkillId = state.SkillId
                    });
                    castersToRemove.Add(kvp.Key);
                }
            }

            foreach (var casterId in castersToRemove)
            {
                _activeCasts.Remove(casterId);
            }
        }

        /// <summary>
        /// 获取并清空本帧完成的施法列表
        /// Get and clear completed casts for this frame
        /// </summary>
        public List<CastCompletionEvent> CollectCompletedCasts()
        {
            var result = new List<CastCompletionEvent>(_completedCasts);
            _completedCasts.Clear();
            return result;
        }

        /// <summary>
        /// 检查指定角色是否正在施法
        /// Check if specified character is casting
        /// </summary>
        public bool IsCasting(string casterId)
        {
            return _activeCasts.ContainsKey(casterId);
        }

        /// <summary>
        /// 获取当前被暂停的轨道列表（占位实现）
        /// Get list of currently paused tracks (placeholder implementation)
        /// </summary>
        /// <returns>被暂停的轨道ID列表</returns>
        public List<string> GetPausedTracks()
        {
            // 占位：始终返回空列表
            // Placeholder: always return empty list
            // 后续阶段会实现轨道暂停逻辑
            // Track pausing logic will be implemented in future phases
            return new List<string>();
        }

        private class CastState
        {
            public string CasterId { get; set; } = "";
            public string SkillId { get; set; } = "";
            public double RemainingMs { get; set; }
            public double TotalMs { get; set; }
        }
    }

    /// <summary>
    /// 施法完成事件
    /// Cast completion event
    /// </summary>
    public class CastCompletionEvent
    {
        public string CasterId { get; set; } = "";
        public string SkillId { get; set; } = "";
    }
}
