using System;
using System.Collections.Generic;
using BlazorIdle.Game.Resources;

namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Wrapper class that implements IBuffOwner for Enemy entities.
    /// Manages buffs/debuffs on enemies.
    /// </summary>
    public class EnemyBuffOwner : IBuffOwner
    {
        private readonly Enemy _enemy;
        private readonly string _enemyId;
        private readonly Dictionary<string, BuffInstance> _buffs;
        private readonly Action<int, DamageMeta>? _onDamageReceived;
        private readonly Action<int, HealMeta>? _onHealReceived;

        public EnemyBuffOwner(
            Enemy enemy,
            string enemyId,
            Action<int, DamageMeta>? onDamageReceived = null,
            Action<int, HealMeta>? onHealReceived = null)
        {
            _enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            _enemyId = enemyId ?? throw new ArgumentNullException(nameof(enemyId));
            _buffs = new Dictionary<string, BuffInstance>();
            _onDamageReceived = onDamageReceived;
            _onHealReceived = onHealReceived;
        }

        public string Id => _enemyId;
        public bool IsPlayer => false;
        public int CurrentHp => _enemy.Hp;
        public int MaxHp => _enemy.MaxHp;
        public ResourceBucketCollection? Buckets => null; // Enemies don't use resources
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
            
            _enemy.Hp = Math.Max(0, _enemy.Hp - amount);
            _onDamageReceived?.Invoke(amount, meta);
        }

        public void ReceiveHeal(int amount, HealMeta meta)
        {
            if (amount < 0) throw new ArgumentException("Heal amount cannot be negative", nameof(amount));
            
            int healedAmount = Math.Min(amount, _enemy.MaxHp - _enemy.Hp);
            _enemy.Hp = Math.Min(_enemy.MaxHp, _enemy.Hp + amount);
            _onHealReceived?.Invoke(healedAmount, meta);
        }

        /// <summary>
        /// Gets the underlying enemy entity.
        /// </summary>
        public Enemy Enemy => _enemy;
    }
}
