namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Event recorded when a buff/debuff is applied to an entity.
    /// </summary>
    public class BuffApplyEvent : CombatEvent
    {
        /// <summary>
        /// ID of the entity that received the buff.
        /// </summary>
        public string OwnerId { get; set; } = "";

        /// <summary>
        /// ID of the buff that was applied.
        /// </summary>
        public string BuffId { get; set; } = "";

        /// <summary>
        /// Whether this is a buff or debuff.
        /// </summary>
        public BuffKind Kind { get; set; }

        /// <summary>
        /// Duration of the buff in seconds (null = permanent).
        /// </summary>
        public double? DurationSec { get; set; }

        /// <summary>
        /// Number of stacks applied (usually 1 on initial application).
        /// </summary>
        public int Stacks { get; set; }

        /// <summary>
        /// Summary of effects for UI display.
        /// </summary>
        public string EffectsSummary { get; set; } = "";

        /// <summary>
        /// Optional: Source skill ID that applied this buff.
        /// </summary>
        public string? SourceSkillId { get; set; }

        /// <summary>
        /// Optional: Bundle ID for grouping related events.
        /// </summary>
        public string? BundleId { get; set; }
    }
}
