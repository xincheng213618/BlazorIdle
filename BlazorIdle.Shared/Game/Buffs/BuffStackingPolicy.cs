namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Defines how a buff behaves when reapplied.
    /// </summary>
    public enum BuffStackingPolicy
    {
        /// <summary>
        /// Refreshes the duration without stacking.
        /// </summary>
        Refresh,

        /// <summary>
        /// Adds a stack and may refresh duration.
        /// </summary>
        Stack,

        /// <summary>
        /// Ignores the new application if already exists.
        /// </summary>
        Ignore
    }
}
