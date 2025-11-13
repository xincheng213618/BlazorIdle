using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Skills;
using System.Collections.Generic;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Tests for Phase 8: Buff stacking order implementation
    /// Method B: Time-ordered stacking with multiplicative accumulation
    /// </summary>
    public class Phase8BuffStackingOrderTests
    {
        private class TestClock : IGameClock
        {
            private int _currentTime = 0;
            
            public int NowMs => _currentTime;
            
            public void Advance(int ms)
            {
                _currentTime += ms;
            }
            
            public void AdvanceBy(int ms) => Advance(ms);
            public void Reset() => _currentTime = 0;
        }

        private class TestBuffOwner : IBuffOwner
        {
            private readonly Dictionary<string, BuffInstance> _buffs = new();
            
            public string Id { get; set; } = "test_owner";
            public bool IsPlayer => false;
            public int CurrentHp => 100;
            public int MaxHp => 100;
            public BlazorIdle.Game.Resources.ResourceBucketCollection? Buckets => null;
            public IReadOnlyDictionary<string, BuffInstance> Buffs => _buffs;
            
            public void ApplyBuff(BuffInstance buff)
            {
                _buffs[buff.Id] = buff;
            }
            
            public bool RemoveBuff(string buffId, string reason)
            {
                return _buffs.Remove(buffId);
            }
            
            public void ReceiveDamage(int amount, DamageMeta meta) { }
            public void ReceiveHeal(int amount, HealMeta meta) { }
        }

        [Fact]
        public void TimeOrderedStacking_MultiplierThenAdditive_CorrectOrder()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var skillRepo = new SkillRepository();
            var resolver = new SkillResolver(null, skillRepo);
            
            // Buff A (t=0): +50% damage multiplier
            clock.Advance(0);
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatMultiplier,
                        Target = "DamagePerAttack",
                        Value = 0.5 // +50%
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffA);
            
            // Buff B (t=100): +30 flat damage
            clock.Advance(100);
            var buffB = new BuffInstance(
                id: "buff_b",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatAdditive,
                        Target = "DamagePerAttack",
                        Value = 30
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffB);
            
            // Act - Use reflection to call private method
            var method = typeof(SkillResolver).GetMethod(
                "ApplyBuffEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var result = (int)method!.Invoke(resolver, new object[] { 100, "DamagePerAttack", owner })!;
            
            // Assert
            // Base: 100
            // Buff A (t=0) first: 100 * 1.5 = 150
            // Buff B (t=100) second: 150 + 30 = 180
            Assert.Equal(180, result);
        }

        [Fact]
        public void TimeOrderedStacking_AdditiveThenMultiplier_CorrectOrder()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var skillRepo = new SkillRepository();
            var resolver = new SkillResolver(null, skillRepo);
            
            // Buff A (t=0): +30 flat damage
            clock.Advance(0);
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatAdditive,
                        Target = "DamagePerAttack",
                        Value = 30
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffA);
            
            // Buff B (t=100): +50% damage multiplier
            clock.Advance(100);
            var buffB = new BuffInstance(
                id: "buff_b",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatMultiplier,
                        Target = "DamagePerAttack",
                        Value = 0.5 // +50%
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffB);
            
            // Act
            var method = typeof(SkillResolver).GetMethod(
                "ApplyBuffEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var result = (int)method!.Invoke(resolver, new object[] { 100, "DamagePerAttack", owner })!;
            
            // Assert
            // Base: 100
            // Buff A (t=0) first: 100 + 30 = 130
            // Buff B (t=100) second: 130 * 1.5 = 195
            Assert.Equal(195, result);
        }

        [Fact]
        public void TimeOrderedStacking_ThreeBuffs_CorrectOrder()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var skillRepo = new SkillRepository();
            var resolver = new SkillResolver(null, skillRepo);
            
            // Buff A (t=0): +50% multiplier
            clock.Advance(0);
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatMultiplier,
                        Target = "DamagePerAttack",
                        Value = 0.5
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffA);
            
            // Buff B (t=50): +30 flat
            clock.Advance(50);
            var buffB = new BuffInstance(
                id: "buff_b",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatAdditive,
                        Target = "DamagePerAttack",
                        Value = 30
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffB);
            
            // Buff C (t=100): +20% multiplier
            clock.Advance(50);
            var buffC = new BuffInstance(
                id: "buff_c",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatMultiplier,
                        Target = "DamagePerAttack",
                        Value = 0.2
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffC);
            
            // Act
            var method = typeof(SkillResolver).GetMethod(
                "ApplyBuffEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var result = (int)method!.Invoke(resolver, new object[] { 100, "DamagePerAttack", owner })!;
            
            // Assert
            // Base: 100
            // Buff A (t=0): 100 * 1.5 = 150
            // Buff B (t=50): 150 + 30 = 180
            // Buff C (t=100): 180 * 1.2 = 216
            Assert.Equal(216, result);
        }

        [Fact]
        public void MultiplicativeStacking_TwoMultipliers_PreventRunaway()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var skillRepo = new SkillRepository();
            var resolver = new SkillResolver(null, skillRepo);
            
            // Buff A (t=0): +50% multiplier
            clock.Advance(0);
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatMultiplier,
                        Target = "DamagePerAttack",
                        Value = 0.5
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffA);
            
            // Buff B (t=100): +30% multiplier
            clock.Advance(100);
            var buffB = new BuffInstance(
                id: "buff_b",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatMultiplier,
                        Target = "DamagePerAttack",
                        Value = 0.3
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffB);
            
            // Act
            var method = typeof(SkillResolver).GetMethod(
                "ApplyBuffEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var result = (int)method!.Invoke(resolver, new object[] { 100, "DamagePerAttack", owner })!;
            
            // Assert
            // Multiplicative: 100 * 1.5 * 1.3 = 195 (+95%)
            // If it were additive: 100 * 1.8 = 180 (+80%)
            // Multiplicative gives slightly higher but controlled scaling
            Assert.Equal(195, result);
        }

        [Fact]
        public void TimeOrderedStacking_SameTimestamp_StableOrder()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var skillRepo = new SkillRepository();
            var resolver = new SkillResolver(null, skillRepo);
            
            // Both buffs at t=0 - should be sorted by insertion order in dictionary
            // which is stable in .NET Core 3.0+
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatMultiplier,
                        Target = "DamagePerAttack",
                        Value = 0.5
                    }
                },
                appliedAtMs: 0
            );
            owner.ApplyBuff(buffA);
            
            var buffB = new BuffInstance(
                id: "buff_b",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatAdditive,
                        Target = "DamagePerAttack",
                        Value = 30
                    }
                },
                appliedAtMs: 0
            );
            owner.ApplyBuff(buffB);
            
            // Act
            var method = typeof(SkillResolver).GetMethod(
                "ApplyBuffEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var result = (int)method!.Invoke(resolver, new object[] { 100, "DamagePerAttack", owner })!;
            
            // Assert - Both are at t=0, order should be deterministic
            // Result should be consistent across multiple runs
            int result2 = (int)method!.Invoke(resolver, new object[] { 100, "DamagePerAttack", owner })!;
            Assert.Equal(result, result2);
        }

        [Fact]
        public void TimeOrderedStacking_CritMultiplier_DoubleValues()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var skillRepo = new SkillRepository();
            var resolver = new SkillResolver(null, skillRepo);
            
            // Buff A (t=0): +50% crit multiplier
            clock.Advance(0);
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatMultiplier,
                        Target = "CritMultiplier",
                        Value = 0.5
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffA);
            
            // Buff B (t=100): +0.3 flat crit multiplier
            clock.Advance(100);
            var buffB = new BuffInstance(
                id: "buff_b",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatAdditive,
                        Target = "CritMultiplier",
                        Value = 0.3
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffB);
            
            // Act
            var method = typeof(SkillResolver).GetMethod(
                "ApplyBuffEffectsToDouble",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var result = (double)method!.Invoke(resolver, new object[] { 2.0, "CritMultiplier", owner })!;
            
            // Assert
            // Base: 2.0
            // Buff A (t=0): 2.0 * 1.5 = 3.0
            // Buff B (t=100): 3.0 + 0.3 = 3.3
            Assert.Equal(3.3, result, 2);
        }

        [Fact]
        public void TimeOrderedStacking_Debuffs_CorrectReduction()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var skillRepo = new SkillRepository();
            var resolver = new SkillResolver(null, skillRepo);
            
            // Debuff A (t=0): -20% damage reduction
            clock.Advance(0);
            var debuffA = new BuffInstance(
                id: "debuff_a",
                ownerId: owner.Id,
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatReduction,
                        Target = "DamagePerAttack",
                        Value = 0.2 // -20%
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(debuffA);
            
            // Debuff B (t=100): -10% damage reduction
            clock.Advance(100);
            var debuffB = new BuffInstance(
                id: "debuff_b",
                ownerId: owner.Id,
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatReduction,
                        Target = "DamagePerAttack",
                        Value = 0.1 // -10%
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(debuffB);
            
            // Act
            var method = typeof(SkillResolver).GetMethod(
                "ApplyBuffEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var result = (int)method!.Invoke(resolver, new object[] { 100, "DamagePerAttack", owner })!;
            
            // Assert
            // Base: 100
            // Debuff A (t=0): 100 * 0.8 = 80 (20% reduction)
            // Debuff B (t=100): 80 * 0.9 = 72 (10% reduction)
            // Total: 28% reduction (multiplicative stacking)
            Assert.Equal(72, result);
        }

        [Fact]
        public void TimeOrderedStacking_MixedBuffsAndDebuffs_CorrectCalculation()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var skillRepo = new SkillRepository();
            var resolver = new SkillResolver(null, skillRepo);
            
            // Buff (t=0): +50% multiplier
            clock.Advance(0);
            var buff = new BuffInstance(
                id: "buff",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatMultiplier,
                        Target = "DamagePerAttack",
                        Value = 0.5
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buff);
            
            // Debuff (t=50): -20% reduction
            clock.Advance(50);
            var debuff = new BuffInstance(
                id: "debuff",
                ownerId: owner.Id,
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect>
                {
                    new BuffEffect
                    {
                        Type = BuffEffectType.StatReduction,
                        Target = "DamagePerAttack",
                        Value = 0.2
                    }
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(debuff);
            
            // Act
            var method = typeof(SkillResolver).GetMethod(
                "ApplyBuffEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var result = (int)method!.Invoke(resolver, new object[] { 100, "DamagePerAttack", owner })!;
            
            // Assert
            // Base: 100
            // Buff (t=0): 100 * 1.5 = 150
            // Debuff (t=50): 150 * 0.8 = 120
            Assert.Equal(120, result);
        }

        [Fact]
        public void TimeOrderedStacking_BuffInstanceHasAppliedAtMs()
        {
            // Arrange
            var clock = new TestClock();
            clock.Advance(12345);
            
            // Act
            var buff = new BuffInstance(
                id: "test_buff",
                ownerId: "test_owner",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>(),
                appliedAtMs: clock.NowMs
            );
            
            // Assert
            Assert.Equal(12345, buff.AppliedAtMs);
        }
    }
}
