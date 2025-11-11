namespace BlazorIdle.Game.Config
{
    /// <summary>
    /// 战斗配置
    /// Combat configuration
    /// </summary>
    public sealed class CombatConfig
    {
        /// <summary>
        /// 是否使用充能轨道（预留，后续阶段切换）
        /// Whether to use charge tracks (reserved, switch in future phases)
        /// </summary>
        public bool UseChargeTracks { get; set; } = false;

        /// <summary>
        /// 是否发送技能施放事件（便于调试）
        /// Whether to emit skill cast events (for debugging)
        /// </summary>
        public bool EmitCastEvents { get; set; } = true;

        /// <summary>
        /// 每 Tick 最多触发次数（防极端情况）
        /// Maximum triggers per tick (prevent extreme cases)
        /// </summary>
        public int MaxTriggersPerTick { get; set; } = 20;

        // 注：不再需要 UseLegacyPath 开关，直接实现新架构，有问题可使用 git revert
        // Note: No longer need UseLegacyPath switch, implement new architecture directly,
        // can use git revert if there are issues
    }
}
