using System;
using System.Collections.Generic;
using BlazorIdle.Game.Resources;

namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Wrapper class that implements IBuffOwner for Character entities.
    /// Manages buffs/debuffs on player characters.
    /// </summary>
    public class CharacterBuffOwner : IBuffOwner
    {
        private readonly Character _character;
        private readonly string _memberId;
        private readonly ResourceBucketCollection? _resources;
        private readonly Dictionary<string, BuffInstance> _buffs;
        private readonly Action<int, DamageMeta>? _onDamageReceived;
        private readonly Action<int, HealMeta>? _onHealReceived;

        public CharacterBuffOwner(
            Character character,
            string memberId,
            ResourceBucketCollection? resources = null,
            Action<int, DamageMeta>? onDamageReceived = null,
            Action<int, HealMeta>? onHealReceived = null)
        {
            _character = character ?? throw new ArgumentNullException(nameof(character));
            _memberId = memberId ?? throw new ArgumentNullException(nameof(memberId));
            _resources = resources;
            _buffs = new Dictionary<string, BuffInstance>();
            _onDamageReceived = onDamageReceived;
            _onHealReceived = onHealReceived;
        }

        public string Id => _memberId;
        public bool IsPlayer => true;
        public int CurrentHp => _character.Hp;
        public int MaxHp => _character.MaxHp;
        public ResourceBucketCollection? Buckets => _resources;
        
        /// <summary>
        /// Phase 7.11: Return read-only view to prevent external modification.
        /// </summary>
        public IReadOnlyDictionary<string, BuffInstance> Buffs => _buffs;

        public void ApplyBuff(BuffInstance buff)
        {
            if (buff == null) throw new ArgumentNullException(nameof(buff));
            
            // Validate OwnerId matches
            if (buff.OwnerId != Id)
            {
                throw new ArgumentException($"Buff OwnerId '{buff.OwnerId}' does not match BuffOwner Id '{Id}'", nameof(buff));
            }
            
            if (_buffs.TryGetValue(buff.Id, out var existing))
            {
                // Handle stacking policy
                switch (existing.StackingPolicy)
                {
                    case BuffStackingPolicy.Refresh:
                        // Refresh duration using the existing buff's initial duration
                        if (existing.RemainingDurationSec.HasValue && buff.RemainingDurationSec.HasValue)
                        {
                            existing.RefreshDuration(buff.RemainingDurationSec.Value);
                        }
                        break;
                    
                    case BuffStackingPolicy.Stack:
                        existing.AddStack();
                        // Also refresh duration when stacking
                        if (existing.RemainingDurationSec.HasValue && buff.RemainingDurationSec.HasValue)
                        {
                            existing.RefreshDuration(buff.RemainingDurationSec.Value);
                        }
                        break;
                    
                    case BuffStackingPolicy.Ignore:
                        // Do nothing - ignore new application
                        break;
                }
            }
            else
            {
                _buffs[buff.Id] = buff;
            }
        }

        public bool RemoveBuff(string buffId, string reason)
        {
            return _buffs.Remove(buffId);
        }

        public bool ReduceBuffStacks(string buffId, int stacksToRemove, string reason)
        {
            if (!_buffs.TryGetValue(buffId, out var buff))
                return false;

            if (stacksToRemove <= 0)
                return false;

            // Reduce stacks
            buff.Stacks = Math.Max(0, buff.Stacks - stacksToRemove);

            // If stacks reach 0 or below, remove the buff entirely
            if (buff.Stacks <= 0)
            {
                _buffs.Remove(buffId);
            }

            return true;
        }

        public void ReceiveDamage(int amount, DamageMeta meta)
        {
            if (amount < 0) throw new ArgumentException("Damage amount cannot be negative", nameof(amount));
            
            _character.Hp = Math.Max(0, _character.Hp - amount);
            _onDamageReceived?.Invoke(amount, meta);
        }

        public void ReceiveHeal(int amount, HealMeta meta)
        {
            if (amount < 0) throw new ArgumentException("Heal amount cannot be negative", nameof(amount));
            
            int actualHealAmount = Math.Min(amount, _character.MaxHp - _character.Hp);
            _character.Hp = Math.Min(_character.MaxHp, _character.Hp + actualHealAmount);
            _onHealReceived?.Invoke(actualHealAmount, meta);
        }

        /// <summary>
        /// Clears all buffs on this entity (e.g., on death).
        /// </summary>
        public void ClearAllBuffs()
        {
            _buffs.Clear();
        }

        /// <summary>
        /// Gets the underlying character entity.
        /// </summary>
        public Character Character => _character;
    }
}
