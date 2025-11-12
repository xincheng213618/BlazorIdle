namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Event recorded when a buff/debuff is removed from an entity.
    /// </summary>
    public class BuffRemoveEvent : CombatEvent
    {
        /// <summary>
        /// ID of the entity that had the buff removed.
        /// </summary>
        public string OwnerId { get; set; } = "";

        /// <summary>
        /// ID of the buff that was removed.
        /// </summary>
        public string BuffId { get; set; } = "";

        /// <summary>
        /// Reason for removal (e.g., "expired", "dispelled", "manual").
        /// </summary>
        public string Reason { get; set; } = "";

        /// <summary>
        /// Optional: Bundle ID for grouping related events.
        /// </summary>
        public string? BundleId { get; set; }
    }
}
