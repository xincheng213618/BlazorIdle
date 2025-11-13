namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Represents an effect that a buff can have.
    /// </summary>
    public class BuffEffect
    {
        /// <summary>
        /// The type of effect.
        /// </summary>
        public BuffEffectType Type { get; set; }

        /// <summary>
        /// Target stat for stat-based effects (e.g., "DamagePerAttack", "HastePercent", "CritChancePercent").
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// Value for the effect.
        /// For StatMultiplier: multiplier value (e.g., 0.15 for +15%)
        /// For StatAdditive: additive value (e.g., 0.05 for +5%)
        /// For DoT/HoT: amount per tick
        /// For InstantHeal: heal amount
        /// For StatReduction: reduction value
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Amount per tick for DoT/HoT effects.
        /// </summary>
        public int AmountPerTick { get; set; }

        public BuffEffect()
        {
        }

        public BuffEffect(BuffEffectType type, string? target = null, double value = 0, int amountPerTick = 0)
        {
            Type = type;
            Target = target;
            Value = value;
            AmountPerTick = amountPerTick;
        }

        /// <summary>
        /// Creates a stat multiplier effect.
        /// </summary>
        public static BuffEffect StatMultiplier(string target, double value)
        {
            return new BuffEffect(BuffEffectType.StatMultiplier, target, value);
        }

        /// <summary>
        /// Creates a stat additive effect.
        /// </summary>
        public static BuffEffect StatAdditive(string target, double value)
        {
            return new BuffEffect(BuffEffectType.StatAdditive, target, value);
        }

        /// <summary>
        /// Creates a force crit effect.
        /// </summary>
        public static BuffEffect ForceCrit()
        {
            return new BuffEffect(BuffEffectType.ForceCrit);
        }

        /// <summary>
        /// Creates a damage over time effect.
        /// </summary>
        public static BuffEffect DamageOverTime(int amountPerTick)
        {
            return new BuffEffect(BuffEffectType.DamageOverTime, amountPerTick: amountPerTick);
        }

        /// <summary>
        /// Creates a heal over time effect.
        /// </summary>
        public static BuffEffect HealOverTime(int amountPerTick)
        {
            return new BuffEffect(BuffEffectType.HealOverTime, amountPerTick: amountPerTick);
        }

        /// <summary>
        /// Creates an instant heal effect.
        /// </summary>
        public static BuffEffect InstantHeal(int amount)
        {
            return new BuffEffect(BuffEffectType.InstantHeal, value: amount);
        }

        /// <summary>
        /// Creates a stat reduction effect (debuff).
        /// </summary>
        public static BuffEffect StatReduction(string target, double value)
        {
            return new BuffEffect(BuffEffectType.StatReduction, target, value);
        }
    }
}
