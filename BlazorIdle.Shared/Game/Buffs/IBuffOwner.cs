using System.Collections.Generic;
using BlazorIdle.Game.Resources;

namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Interface for entities that can own and be affected by buffs/debuffs.
    /// Both players and enemies should implement this interface.
    /// </summary>
    public interface IBuffOwner
    {
        /// <summary>
        /// Unique identifier for this entity.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Whether this is a player entity.
        /// </summary>
        bool IsPlayer { get; }

        /// <summary>
        /// Current hit points.
        /// </summary>
        int CurrentHp { get; }

        /// <summary>
        /// Maximum hit points.
        /// </summary>
        int MaxHp { get; }

        /// <summary>
        /// Resource buckets (e.g., rage, mana, energy).
        /// May be null for enemies that don't use resources.
        /// </summary>
        ResourceBucketCollection? Buckets { get; }

        /// <summary>
        /// Active buffs/debuffs on this entity.
        /// Key = buff ID.
        /// </summary>
        Dictionary<string, BuffInstance> Buffs { get; }

        /// <summary>
        /// Applies a buff/debuff to this entity.
        /// Handles stacking policy (Refresh/Stack/Ignore).
        /// </summary>
        void ApplyBuff(BuffInstance buff);

        /// <summary>
        /// Removes a buff/debuff from this entity.
        /// </summary>
        /// <param name="buffId">ID of the buff to remove.</param>
        /// <param name="reason">Reason for removal (e.g., "expired", "dispelled", "manual").</param>
        /// <returns>True if buff was removed, false if it didn't exist.</returns>
        bool RemoveBuff(string buffId, string reason);

        /// <summary>
        /// Applies damage to this entity.
        /// </summary>
        /// <param name="amount">Amount of damage.</param>
        /// <param name="meta">Metadata about the damage source.</param>
        void ReceiveDamage(int amount, DamageMeta meta);

        /// <summary>
        /// Applies healing to this entity.
        /// </summary>
        /// <param name="amount">Amount of healing.</param>
        /// <param name="meta">Metadata about the healing source.</param>
        void ReceiveHeal(int amount, HealMeta meta);
    }
}
