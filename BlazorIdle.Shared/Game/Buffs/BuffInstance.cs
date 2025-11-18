using System;
using System.Collections.Generic;

namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Represents a runtime instance of a buff/debuff.
    /// </summary>
    public class BuffInstance
    {
        /// <summary>
        /// Unique identifier for this buff type (e.g., "warrior_rage_boost").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// ID of the entity that owns this buff.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Whether this is a beneficial buff or harmful debuff.
        /// </summary>
        public BuffKind Kind { get; set; }

        /// <summary>
        /// List of effects this buff applies.
        /// </summary>
        public List<BuffEffect> Effects { get; set; }

        /// <summary>
        /// How this buff behaves when reapplied.
        /// </summary>
        public BuffStackingPolicy StackingPolicy { get; set; }

        /// <summary>
        /// Current number of stacks (relevant for Stack policy).
        /// </summary>
        public int Stacks { get; set; }

        /// <summary>
        /// Maximum number of stacks allowed (0 = unlimited).
        /// </summary>
        public int MaxStacks { get; set; }

        /// <summary>
        /// Remaining duration in seconds (null = permanent).
        /// </summary>
        public double? RemainingDurationSec { get; set; }

        /// <summary>
        /// Interval between ticks for DoT/HoT effects (null = no ticking).
        /// </summary>
        public double? TickIntervalSec { get; set; }

        /// <summary>
        /// Internal accumulator for tick timing.
        /// </summary>
        public double TickAccumulator { get; set; }

        /// <summary>
        /// Source skill ID that applied this buff (optional, for tracking).
        /// </summary>
        public string? SourceSkillId { get; set; }

        /// <summary>
        /// Timestamp in milliseconds when this buff was applied.
        /// Used for determining application order when stacking multiple buffs.
        /// Method B: Time-order stacking approach.
        /// </summary>
        public long AppliedAtMs { get; set; }

        public BuffInstance()
        {
            Id = string.Empty;
            OwnerId = string.Empty;
            Kind = BuffKind.Buff;
            Effects = new List<BuffEffect>();
            StackingPolicy = BuffStackingPolicy.Refresh;
            Stacks = 1;
            MaxStacks = 0;
            TickAccumulator = 0;
        }

        public BuffInstance(
            string id,
            string ownerId,
            BuffKind kind,
            List<BuffEffect> effects,
            BuffStackingPolicy stackingPolicy = BuffStackingPolicy.Refresh,
            double? durationSec = null,
            double? tickIntervalSec = null,
            int maxStacks = 0,
            string? sourceSkillId = null,
            long appliedAtMs = 0)
        {
            // Validation
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Buff ID cannot be null or empty", nameof(id));
            if (string.IsNullOrEmpty(ownerId))
                throw new ArgumentException("Owner ID cannot be null or empty", nameof(ownerId));
            if (durationSec.HasValue && durationSec.Value < 0)
                throw new ArgumentException("Duration cannot be negative", nameof(durationSec));
            if (tickIntervalSec.HasValue && tickIntervalSec.Value <= 0)
                throw new ArgumentException("Tick interval must be positive", nameof(tickIntervalSec));
            if (maxStacks < 0)
                throw new ArgumentException("Max stacks cannot be negative", nameof(maxStacks));

            Id = id;
            OwnerId = ownerId;
            Kind = kind;
            Effects = effects ?? new List<BuffEffect>();
            StackingPolicy = stackingPolicy;
            Stacks = 1;
            MaxStacks = maxStacks;
            RemainingDurationSec = durationSec;
            TickIntervalSec = tickIntervalSec;
            TickAccumulator = 0;
            SourceSkillId = sourceSkillId;
            AppliedAtMs = appliedAtMs;
        }

        /// <summary>
        /// Updates the buff state over time.
        /// Handles duration decay and tick accumulation for DoT/HoT effects.
        /// Returns the number of ticks that occurred.
        /// </summary>
        public int Tick(double deltaTimeSec)
        {
            int ticksOccurred = 0;

            // Decrement duration
            if (RemainingDurationSec.HasValue)
            {
                RemainingDurationSec = Math.Max(0, RemainingDurationSec.Value - deltaTimeSec);
            }

            // Handle tick-based effects (DoT/HoT)
            if (TickIntervalSec.HasValue && TickIntervalSec.Value > 0)
            {
                TickAccumulator += deltaTimeSec;

                // Handle multiple ticks if deltaTime is large
                while (TickAccumulator >= TickIntervalSec.Value)
                {
                    TickAccumulator -= TickIntervalSec.Value;
                    ticksOccurred++;
                }
            }

            return ticksOccurred;
        }

        /// <summary>
        /// Checks if this buff has expired.
        /// </summary>
        public bool IsExpired()
        {
            return RemainingDurationSec.HasValue && RemainingDurationSec.Value <= 0;
        }

        /// <summary>
        /// Refreshes the duration of this buff (for Refresh stacking policy).
        /// </summary>
        public void RefreshDuration(double durationSec)
        {
            RemainingDurationSec = durationSec;
        }

        /// <summary>
        /// Adds a stack to this buff (for Stack stacking policy).
        /// Returns true if stack was added, false if max stacks reached.
        /// </summary>
        public bool AddStack()
        {
            if (MaxStacks > 0 && Stacks >= MaxStacks)
            {
                return false;
            }

            Stacks++;
            return true;
        }

        /// <summary>
        /// Checks if this buff has any DoT effects.
        /// </summary>
        public bool HasDamageOverTime()
        {
            return Effects.Exists(e => e.Type == BuffEffectType.DamageOverTime);
        }

        /// <summary>
        /// Checks if this buff has any HoT effects.
        /// </summary>
        public bool HasHealOverTime()
        {
            return Effects.Exists(e => e.Type == BuffEffectType.HealOverTime);
        }

        /// <summary>
        /// Gets the total DoT damage per tick (sum of all DoT effects).
        /// </summary>
        public int GetDamagePerTick()
        {
            int total = 0;
            foreach (var effect in Effects)
            {
                if (effect.Type == BuffEffectType.DamageOverTime)
                {
                    total += effect.AmountPerTick;
                }
            }
            return total * Stacks; // Multiply by stacks
        }

        /// <summary>
        /// Gets the total HoT healing per tick (sum of all HoT effects).
        /// </summary>
        public int GetHealPerTick()
        {
            int total = 0;
            foreach (var effect in Effects)
            {
                if (effect.Type == BuffEffectType.HealOverTime)
                {
                    total += effect.AmountPerTick;
                }
            }
            return total * Stacks; // Multiply by stacks
        }
    }
}
