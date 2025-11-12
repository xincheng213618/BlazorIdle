namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Metadata for damage application.
    /// </summary>
    public class DamageMeta
    {
        /// <summary>
        /// Source of the damage (e.g., "dot_tick", "skill_cast", "buff_effect").
        /// </summary>
        public string Source { get; set; } = "";

        /// <summary>
        /// Optional: ID of the buff that caused this damage.
        /// </summary>
        public string? BuffId { get; set; }

        /// <summary>
        /// Optional: ID of the skill that caused this damage.
        /// </summary>
        public string? SkillId { get; set; }

        /// <summary>
        /// Whether this damage can critically hit.
        /// </summary>
        public bool CanCrit { get; set; }

        /// <summary>
        /// Optional: Bundle ID for grouping related events.
        /// </summary>
        public string? BundleId { get; set; }

        public DamageMeta()
        {
        }

        public DamageMeta(string source, string? buffId = null, string? skillId = null, bool canCrit = false, string? bundleId = null)
        {
            Source = source;
            BuffId = buffId;
            SkillId = skillId;
            CanCrit = canCrit;
            BundleId = bundleId;
        }
    }
}
