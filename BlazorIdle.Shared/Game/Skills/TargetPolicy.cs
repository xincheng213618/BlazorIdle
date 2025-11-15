namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 目标选择策略
    /// Target selection policy
    /// </summary>
    public enum TargetPolicy
    {
        /// <summary>
        /// 当前目标（普攻目标）
        /// Current target (basic attack target)
        /// </summary>
        CurrentTarget,

        /// <summary>
        /// 所有敌人
        /// All enemies
        /// </summary>
        EnemiesAll,

        /// <summary>
        /// HP百分比最低的友方
        /// Ally with lowest HP percentage
        /// </summary>
        AlliesLowestHpPct,

        /// <summary>
        /// 自己
        /// Self
        /// </summary>
        Self,

        /// <summary>
        /// 所有友方
        /// All allies
        /// </summary>
        AlliesAll
    }
}
