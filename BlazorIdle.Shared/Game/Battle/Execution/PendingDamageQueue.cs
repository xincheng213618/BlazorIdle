using System.Collections.Generic;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Battle.Execution
{
    /// <summary>
    /// 延迟伤害队列 - 管理待应用的延迟伤害实例
    /// Pending Damage Queue - manages delayed damage instances waiting to be applied
    /// 
    /// 使用优先队列按 ApplyAtSec 排序，确保伤害按时间顺序应用
    /// Uses priority queue sorted by ApplyAtSec to ensure damage is applied in order
    /// </summary>
    public sealed class PendingDamageQueue
    {
        // 使用 SortedList 按 ApplyAtSec 排序
        // 键为 (ApplyAtSec, 唯一ID) 以支持相同时间的多个伤害
        private readonly SortedList<(double time, int id), DamageInstance> _queue;
        private int _nextId = 0;
        
        /// <summary>
        /// 队列中的伤害实例数量
        /// Number of damage instances in the queue
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// 队列是否为空
        /// Whether the queue is empty
        /// </summary>
        public bool IsEmpty => _queue.Count == 0;

        /// <summary>
        /// 创建新的延迟伤害队列
        /// Create a new pending damage queue
        /// </summary>
        public PendingDamageQueue()
        {
            _queue = new SortedList<(double time, int id), DamageInstance>();
        }

        /// <summary>
        /// 将伤害实例加入延迟队列
        /// Enqueue a damage instance to the pending queue
        /// </summary>
        /// <param name="instance">伤害实例 / Damage instance</param>
        /// <param name="baseTimeSec">基准时间（秒）/ Base time in seconds</param>
        public void Enqueue(DamageInstance instance, double baseTimeSec = 0)
        {
            // 计算绝对应用时间
            double absoluteTime = baseTimeSec + instance.ApplyAtSec;
            
            // 创建唯一键（时间 + 唯一ID）
            var key = (absoluteTime, _nextId++);
            
            // 创建新实例，更新 ApplyAtSec 为绝对时间
            var queuedInstance = instance.Clone();
            queuedInstance.ApplyAtSec = absoluteTime;
            
            _queue.Add(key, queuedInstance);
        }

        /// <summary>
        /// 取出所有已到时间的伤害实例
        /// Dequeue all damage instances that are ready to be applied
        /// </summary>
        /// <param name="currentTimeSec">当前时间（秒）/ Current time in seconds</param>
        /// <returns>已到时间的伤害实例列表 / List of ready damage instances</returns>
        public List<DamageInstance> DequeueReady(double currentTimeSec)
        {
            var ready = new List<DamageInstance>();
            
            while (_queue.Count > 0)
            {
                var firstKey = _queue.Keys[0];
                if (firstKey.time <= currentTimeSec)
                {
                    ready.Add(_queue.Values[0]);
                    _queue.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
            
            return ready;
        }

        /// <summary>
        /// 查看下一个伤害的应用时间（不移除）
        /// Peek at the next damage application time (without removing)
        /// </summary>
        /// <returns>下一个应用时间，如果队列为空则返回 null / Next application time, or null if queue is empty</returns>
        public double? PeekNextTime()
        {
            if (_queue.Count == 0)
                return null;
            
            return _queue.Keys[0].time;
        }

        /// <summary>
        /// 清空队列
        /// Clear the queue
        /// </summary>
        public void Clear()
        {
            _queue.Clear();
            _nextId = 0;
        }

        /// <summary>
        /// 移除指定目标的所有伤害（用于目标死亡时）
        /// Remove all damages for a specific target (used when target dies)
        /// </summary>
        /// <param name="targetId">目标ID / Target ID</param>
        /// <returns>移除的伤害数量 / Number of damages removed</returns>
        public int RemoveByTarget(string targetId)
        {
            var keysToRemove = new List<(double time, int id)>();
            
            foreach (var kvp in _queue)
            {
                if (kvp.Value.TargetId == targetId)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _queue.Remove(key);
            }
            
            return keysToRemove.Count;
        }

        /// <summary>
        /// 移除指定技能的所有伤害（用于技能被打断时）
        /// Remove all damages for a specific skill (used when skill is interrupted)
        /// </summary>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <param name="bundleId">Bundle ID（可选）/ Bundle ID (optional)</param>
        /// <returns>移除的伤害数量 / Number of damages removed</returns>
        public int RemoveBySkill(string skillId, string? bundleId = null)
        {
            var keysToRemove = new List<(double time, int id)>();
            
            foreach (var kvp in _queue)
            {
                bool skillMatch = kvp.Value.SkillId == skillId;
                bool bundleMatch = bundleId == null || kvp.Value.BundleId == bundleId;
                
                if (skillMatch && bundleMatch)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _queue.Remove(key);
            }
            
            return keysToRemove.Count;
        }

        /// <summary>
        /// 获取队列中所有伤害实例（用于调试/显示）
        /// Get all damage instances in the queue (for debugging/display)
        /// </summary>
        /// <returns>所有伤害实例的只读列表 / Read-only list of all damage instances</returns>
        public IReadOnlyList<DamageInstance> GetAll()
        {
            var list = new List<DamageInstance>(_queue.Count);
            foreach (var kvp in _queue)
            {
                list.Add(kvp.Value);
            }
            return list;
        }
    }
}
