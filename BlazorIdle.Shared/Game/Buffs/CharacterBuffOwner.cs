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
        private readonly ResourceBucketCollection? _resources;
        private readonly Dictionary<string, BuffInstance> _buffs;
        private readonly Action<int, DamageMeta>? _onDamageReceived;
        private readonly Action<int, HealMeta>? _onHealReceived;

        public CharacterBuffOwner(
            Character character,
            ResourceBucketCollection? resources = null,
            Action<int, DamageMeta>? onDamageReceived = null,
            Action<int, HealMeta>? onHealReceived = null)
        {
            _character = character ?? throw new ArgumentNullException(nameof(character));
            _resources = resources;
            _buffs = new Dictionary<string, BuffInstance>();
            _onDamageReceived = onDamageReceived;
            _onHealReceived = onHealReceived;
        }

        public string Id => $"character_{_character.ActiveCombatProfessionId}";
        public bool IsPlayer => true;
        public int CurrentHp => _character.Hp;
        public int MaxHp => _character.MaxHp;
        public ResourceBucketCollection? Buckets => _resources;
        public Dictionary<string, BuffInstance> Buffs => _buffs;

        public void ApplyBuff(BuffInstance buff)
        {
            if (buff == null) throw new ArgumentNullException(nameof(buff));
            
            if (_buffs.TryGetValue(buff.Id, out var existing))
            {
                // Handle stacking policy
                switch (buff.StackingPolicy)
                {
                    case BuffStackingPolicy.Refresh:
                        if (buff.RemainingDurationSec.HasValue)
                        {
                            existing.RefreshDuration(buff.RemainingDurationSec.Value);
                        }
                        break;
                    
                    case BuffStackingPolicy.Stack:
                        existing.AddStack();
                        if (buff.RemainingDurationSec.HasValue)
                        {
                            existing.RefreshDuration(buff.RemainingDurationSec.Value);
                        }
                        break;
                    
                    case BuffStackingPolicy.Ignore:
                        // Do nothing
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

        public void ReceiveDamage(int amount, DamageMeta meta)
        {
            if (amount < 0) throw new ArgumentException("Damage amount cannot be negative", nameof(amount));
            
            _character.Hp = Math.Max(0, _character.Hp - amount);
            _onDamageReceived?.Invoke(amount, meta);
        }

        public void ReceiveHeal(int amount, HealMeta meta)
        {
            if (amount < 0) throw new ArgumentException("Heal amount cannot be negative", nameof(amount));
            
            int healedAmount = Math.Min(amount, _character.MaxHp - _character.Hp);
            _character.Hp = Math.Min(_character.MaxHp, _character.Hp + amount);
            _onHealReceived?.Invoke(healedAmount, meta);
        }

        /// <summary>
        /// Gets the underlying character entity.
        /// </summary>
        public Character Character => _character;
    }
}
