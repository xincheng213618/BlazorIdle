using BlazorIdle.Game.Buffs;
using System.Collections.Generic;

namespace BlazorIdle.Models
{
    /// <summary>
    /// Configuration for a buff/debuff template.
    /// Defines all properties of a buff that can be instantiated during combat.
    /// </summary>
    public class BuffConfig
    {
        /// <summary>
        /// Unique identifier for this buff (e.g., "warrior_rage_boost", "mage_burn_dot").
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Display name for UI (e.g., "狂暴", "燃烧").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the buff's effects.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Icon for UI display (emoji or icon class).
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Whether this is a beneficial buff or harmful debuff.
        /// </summary>
        public BuffKind Kind { get; set; }

        /// <summary>
        /// List of effects this buff applies.
        /// </summary>
        public List<BuffEffect> Effects { get; set; } = new List<BuffEffect>();

        /// <summary>
        /// Duration in seconds (null = permanent).
        /// </summary>
        public double? DurationSec { get; set; }

        /// <summary>
        /// Interval between ticks for DoT/HoT effects (null = no ticking).
        /// </summary>
        public double? TickIntervalSec { get; set; }

        /// <summary>
        /// How this buff behaves when reapplied.
        /// </summary>
        public BuffStackingPolicy StackingPolicy { get; set; } = BuffStackingPolicy.Refresh;

        /// <summary>
        /// Maximum number of stacks allowed (0 = unlimited).
        /// </summary>
        public int MaxStacks { get; set; } = 0;

        /// <summary>
        /// Default target type when this buff is applied via a skill.
        /// Can be overridden by BuffOperation.
        /// </summary>
        public Game.Skills.BuffTarget DefaultTarget { get; set; } = Game.Skills.BuffTarget.Self;

        /// <summary>
        /// Converts this configuration to a BuffInstance for runtime use.
        /// </summary>
        /// <param name="ownerId">ID of the entity that owns this buff.</param>
        /// <param name="sourceSkillId">Optional source skill ID.</param>
        /// <returns>A new BuffInstance ready to be applied.</returns>
        public BuffInstance ToBuffInstance(string ownerId, string? sourceSkillId = null)
        {
            return new BuffInstance
            {
                Id = Id,
                OwnerId = ownerId,
                Kind = Kind,
                Effects = new List<BuffEffect>(Effects), // Clone the effects list
                StackingPolicy = StackingPolicy,
                Stacks = 1,
                MaxStacks = MaxStacks,
                RemainingDurationSec = DurationSec,
                TickIntervalSec = TickIntervalSec,
                TickAccumulator = 0,
                SourceSkillId = sourceSkillId
            };
        }
    }
}
