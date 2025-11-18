namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Defines the types of effects that a buff can have.
    /// </summary>
    public enum BuffEffectType
    {
        /// <summary>
        /// Multiplier effect on stats (e.g., +15% damage).
        /// </summary>
        StatMultiplier,

        /// <summary>
        /// Additive effect on stats (absolute or percentage points, e.g., +5% crit chance).
        /// </summary>
        StatAdditive,

        /// <summary>
        /// Forces the next skill to critically hit.
        /// </summary>
        ForceCrit,

        /// <summary>
        /// Damage over time effect.
        /// </summary>
        DamageOverTime,

        /// <summary>
        /// Healing over time effect.
        /// </summary>
        HealOverTime,

        /// <summary>
        /// Reduction effect on stats (debuff, e.g., -10% attack speed).
        /// </summary>
        StatReduction
    }
}
