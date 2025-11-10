namespace BlazorIdle.Game.Tracks
{
    /// <summary>
    /// Track 进度策略 - 控制 Track 在何种条件下增长
    /// Track progress policy - controls under what conditions a track progresses
    /// </summary>
    public enum ProgressPolicy
    {
        /// <summary>
        /// Presence 模式：仅在"有敌人存活时"增长（当前默认行为）
        /// Presence mode: only progresses when "enemies are alive" (current default behavior)
        /// </summary>
        Presence,

        /// <summary>
        /// Encounter 模式：在遭遇期间持续增长（预留，即使无敌或 Attack 暂停）
        /// Encounter mode: progresses throughout encounter (reserved, even if no enemies or Attack suspended)
        /// 用于部分职业风格
        /// Used for certain profession styles
        /// </summary>
        Encounter
    }
}
