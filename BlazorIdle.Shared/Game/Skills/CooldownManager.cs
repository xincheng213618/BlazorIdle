using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 管理技能冷却时间（Phase 5: 资源消耗与冷却）
    /// Manages skill cooldowns (Phase 5: Resource consumption and cooldowns)
    /// </summary>
    public sealed class CooldownManager
    {
        private readonly Dictionary<string, double> _cooldowns = new();

        /// <summary>
        /// 检查技能是否已准备就绪（不在冷却中）
        /// Check if a skill is ready (not on cooldown)
        /// </summary>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <returns>如果技能可用返回 true，否则返回 false / True if skill is ready, false otherwise</returns>
        public bool IsReady(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return false;

            if (!_cooldowns.TryGetValue(skillId, out var remaining))
                return true; // 没有冷却记录，技能可用 / No cooldown record, skill is ready

            return remaining <= 0;
        }

        /// <summary>
        /// 开始技能冷却
        /// Start skill cooldown
        /// </summary>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="duration">冷却时间（秒）/ Cooldown duration in seconds</param>
        public void StartCooldown(string skillId, double duration)
        {
            if (string.IsNullOrEmpty(skillId))
                return;

            if (duration <= 0)
            {
                // 如果冷却时间为 0 或负数，移除冷却记录
                // If cooldown is 0 or negative, remove cooldown record
                _cooldowns.Remove(skillId);
                return;
            }

            _cooldowns[skillId] = duration;
        }

        /// <summary>
        /// 更新所有技能的冷却时间
        /// Tick all skill cooldowns
        /// </summary>
        /// <param name="deltaTime">时间增量（秒）/ Time delta in seconds</param>
        public void TickCooldowns(double deltaTime)
        {
            if (deltaTime <= 0)
                return;

            // 使用列表收集需要移除的键，避免在迭代过程中修改字典
            // Collect keys to remove to avoid modifying dictionary during iteration
            var keysToRemove = new List<string>();

            foreach (var kvp in _cooldowns)
            {
                var remaining = kvp.Value - deltaTime;
                
                if (remaining <= 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
                else
                {
                    _cooldowns[kvp.Key] = remaining;
                }
            }

            // 移除已完成冷却的技能
            // Remove skills that finished cooling down
            foreach (var key in keysToRemove)
            {
                _cooldowns.Remove(key);
            }
        }

        /// <summary>
        /// 获取技能剩余冷却时间
        /// Get remaining cooldown time for a skill
        /// </summary>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <returns>剩余冷却时间（秒），如果没有冷却返回 0 / Remaining cooldown in seconds, 0 if not on cooldown</returns>
        public double GetRemainingCooldown(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return 0;

            if (_cooldowns.TryGetValue(skillId, out var remaining))
                return remaining > 0 ? remaining : 0;

            return 0;
        }

        /// <summary>
        /// 重置所有冷却
        /// Reset all cooldowns
        /// </summary>
        public void ResetAll()
        {
            _cooldowns.Clear();
        }

        /// <summary>
        /// 重置指定技能的冷却
        /// Reset cooldown for a specific skill
        /// </summary>
        /// <param name="skillId">技能ID / Skill ID</param>
        public void ResetCooldown(string skillId)
        {
            if (!string.IsNullOrEmpty(skillId))
            {
                _cooldowns.Remove(skillId);
            }
        }
    }
}
