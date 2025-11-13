namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Event recorded when a buff ticks (DoT/HoT).
    /// </summary>
    public class BuffTickEvent : CombatEvent
    {
        /// <summary>
        /// ID of the entity that owns the buff.
        /// </summary>
        public string OwnerId { get; set; } = "";

        /// <summary>
        /// ID of the buff that ticked.
        /// </summary>
        public string BuffId { get; set; } = "";

        /// <summary>
        /// Type of tick (DoT or HoT).
        /// </summary>
        public BuffTickType TickType { get; set; }

        /// <summary>
        /// Amount of damage or healing applied.
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// HP of the entity after the tick.
        /// </summary>
        public int ResultingHp { get; set; }

        /// <summary>
        /// Optional: Bundle ID for grouping related events.
        /// </summary>
        public string? BundleId { get; set; }
    }

    /// <summary>
    /// Type of buff tick.
    /// </summary>
    public enum BuffTickType
    {
        /// <summary>
        /// Damage over time.
        /// </summary>
        DamageOverTime,

        /// <summary>
        /// Healing over time.
        /// </summary>
        HealOverTime
    }
}
