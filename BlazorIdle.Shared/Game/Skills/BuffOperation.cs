using BlazorIdle.Game.Buffs;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// Represents a buff operation to be performed as a result of skill casting.
    /// Phase 5: Buff operation instruction from SkillResolver.
    /// </summary>
    public sealed class BuffOperation
    {
        /// <summary>
        /// Type of operation (Apply or Remove).
        /// </summary>
        public BuffOperationType Type { get; set; }

        /// <summary>
        /// Target of the buff operation.
        /// </summary>
        public BuffTarget Target { get; set; }

        /// <summary>
        /// The buff template to apply (for Apply operations).
        /// Contains buff configuration but OwnerId should be set by the applier.
        /// </summary>
        public BuffInstance? BuffTemplate { get; set; }

        /// <summary>
        /// Buff ID to remove (for Remove operations).
        /// </summary>
        public string? BuffIdToRemove { get; set; }

        /// <summary>
        /// Reason for the operation (for logging/debugging).
        /// </summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Type of buff operation.
    /// </summary>
    public enum BuffOperationType
    {
        /// <summary>
        /// Apply a buff/debuff to target(s).
        /// </summary>
        Apply,

        /// <summary>
        /// Remove a specific buff from target(s).
        /// </summary>
        Remove
    }

    /// <summary>
    /// Target specification for buff operations.
    /// </summary>
    public enum BuffTarget
    {
        /// <summary>
        /// Apply to the caster (self).
        /// </summary>
        Self,

        /// <summary>
        /// Apply to the primary target of the skill.
        /// </summary>
        Target,

        /// <summary>
        /// Apply to all enemies.
        /// </summary>
        AllEnemies,

        /// <summary>
        /// Apply to all allies.
        /// </summary>
        AllAllies,

        /// <summary>
        /// Apply to random enemy.
        /// </summary>
        RandomEnemy,

        /// <summary>
        /// Apply to lowest HP ally.
        /// </summary>
        LowestHpAlly
    }
}
