namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Event recorded when an entity receives healing.
    /// </summary>
    public class HealEvent : CombatEvent
    {
        /// <summary>
        /// ID of the entity that received healing.
        /// </summary>
        public string OwnerId { get; set; } = "";

        /// <summary>
        /// Amount of healing applied.
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// HP of the entity after healing.
        /// </summary>
        public int ResultingHp { get; set; }

        /// <summary>
        /// Source of healing (e.g., "instant_heal_buff", "potion").
        /// </summary>
        public new string Source { get; set; } = "";

        /// <summary>
        /// Optional: Bundle ID for grouping related events.
        /// </summary>
        public string? BundleId { get; set; }
    }
}
