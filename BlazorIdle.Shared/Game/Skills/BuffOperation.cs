using BlazorIdle.Game.Buffs;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// Represents a buff operation to be performed as a result of skill casting.
    /// Phase 5: Buff operation instruction from SkillResolver.
    /// Phase 2 (Buff Config): Added BuffConfigId for referencing centralized buff configurations.
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
        /// Phase 2: Buff configuration ID to apply (for Apply operations).
        /// If set, this takes priority over BuffTemplate and the buff will be looked up from BuffRepository.
        /// Allows centralized buff management instead of inline definitions.
        /// </summary>
        public string? BuffConfigId { get; set; }

        /// <summary>
        /// The buff template to apply (for Apply operations).
        /// Contains buff configuration but OwnerId should be set by the applier.
        /// This is used when BuffConfigId is not set (backward compatibility).
        /// </summary>
        public BuffInstance? BuffTemplate { get; set; }

        /// <summary>
        /// Optional target override. If set, overrides the DefaultTarget from BuffConfig.
        /// Useful when a skill wants to apply a buff to a different target than the buff's default.
        /// </summary>
        public BuffTarget? TargetOverride { get; set; }

        /// <summary>
        /// Buff ID to remove (for Remove operations).
        /// </summary>
        public string? BuffIdToRemove { get; set; }

        /// <summary>
        /// Phase 7: Number of stacks to remove (for Remove operations on stackable buffs).
        /// If null or 0, removes all stacks (entire buff).
        /// If > 0, reduces stack count by this amount.
        /// </summary>
        public int? StacksToRemove { get; set; }

        /// <summary>
        /// Reason for the operation (for logging/debugging).
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Helper: Creates a BuffOperation that applies a buff by config ID.
        /// </summary>
        public static BuffOperation ApplyByConfigId(string buffConfigId, BuffTarget? targetOverride = null)
        {
            return new BuffOperation
            {
                Type = BuffOperationType.Apply,
                BuffConfigId = buffConfigId,
                TargetOverride = targetOverride
            };
        }

        /// <summary>
        /// Helper: Creates a BuffOperation that applies a buff using inline template (backward compatibility).
        /// </summary>
        public static BuffOperation ApplyByTemplate(BuffInstance template, BuffTarget target)
        {
            return new BuffOperation
            {
                Type = BuffOperationType.Apply,
                BuffTemplate = template,
                Target = target
            };
        }

        /// <summary>
        /// Helper: Creates a BuffOperation that removes a buff.
        /// </summary>
        public static BuffOperation Remove(string buffId, BuffTarget target, string? reason = null)
        {
            return new BuffOperation
            {
                Type = BuffOperationType.Remove,
                BuffIdToRemove = buffId,
                Target = target,
                Reason = reason
            };
        }
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
