using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 施法控制器（占位实现）
    /// Casting controller (placeholder implementation)
    /// Step 0 中不启用施法功能，所有施法时间为 0
    /// Casting functionality is not enabled in Step 0, all cast times are 0
    /// </summary>
    public sealed class CastingController
    {
        /// <summary>
        /// 是否正在施法
        /// Whether currently casting
        /// </summary>
        public bool IsCasting { get; private set; } = false;

        /// <summary>
        /// 当前活跃的施法（如果有）
        /// Currently active cast (if any)
        /// </summary>
        public ActiveCast? ActiveCast { get; private set; }

        /// <summary>
        /// 推进施法控制器时间（占位实现）
        /// Advance casting controller time (placeholder implementation)
        /// </summary>
        /// <param name="dt">时间增量（秒）</param>
        public void Tick(double dt)
        {
            // 占位：空实现
            // Placeholder: empty implementation
            // 后续阶段会实现施法逻辑
            // Casting logic will be implemented in future phases
            
            // Step 0 中所有技能的施法时间都为 0，因此不需要实际的施法逻辑
            // In Step 0, all skills have 0 cast time, so no actual casting logic is needed
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
            // Step 0 中不会有施法，因此不会暂停任何轨道
            // In Step 0, there is no casting, so no tracks are paused
            return new List<string>();
        }
    }
}
