namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Metadata for healing application.
    /// </summary>
    public class HealMeta
    {
        /// <summary>
        /// Source of the healing (e.g., "hot_tick", "instant_heal", "potion").
        /// </summary>
        public string Source { get; set; } = "";

        /// <summary>
        /// Optional: ID of the buff that caused this healing.
        /// </summary>
        public string? BuffId { get; set; }

        /// <summary>
        /// Optional: ID of the skill that caused this healing.
        /// </summary>
        public string? SkillId { get; set; }

        /// <summary>
        /// Optional: Bundle ID for grouping related events.
        /// </summary>
        public string? BundleId { get; set; }

        public HealMeta()
        {
        }

        public HealMeta(string source, string? buffId = null, string? skillId = null, string? bundleId = null)
        {
            Source = source;
            BuffId = buffId;
            SkillId = skillId;
            BundleId = bundleId;
        }
    }
}
