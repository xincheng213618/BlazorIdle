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
            string? sourceSkillId = null)
        {
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
        }

        /// <summary>
        /// Updates the buff state over time.
        /// Handles duration decay and tick accumulation for DoT/HoT effects.
        /// Returns true if a tick occurred.
        /// </summary>
        public bool Tick(double deltaTimeSec)
        {
            bool tickOccurred = false;

            // Decrement duration
            if (RemainingDurationSec.HasValue)
            {
                RemainingDurationSec = Math.Max(0, RemainingDurationSec.Value - deltaTimeSec);
            }

            // Handle tick-based effects (DoT/HoT)
            if (TickIntervalSec.HasValue && TickIntervalSec.Value > 0)
            {
                TickAccumulator += deltaTimeSec;

                if (TickAccumulator >= TickIntervalSec.Value)
                {
                    TickAccumulator -= TickIntervalSec.Value;
                    tickOccurred = true;
                }
            }

            return tickOccurred;
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
        /// Checks if this buff has any instant heal effects.
        /// </summary>
        public bool HasInstantHeal()
        {
            return Effects.Exists(e => e.Type == BuffEffectType.InstantHeal);
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

        /// <summary>
        /// Gets the total instant heal amount (sum of all InstantHeal effects).
        /// </summary>
        public int GetInstantHealAmount()
        {
            int total = 0;
            foreach (var effect in Effects)
            {
                if (effect.Type == BuffEffectType.InstantHeal)
                {
                    total += (int)effect.Value;
                }
            }
            return total;
        }
    }
}
