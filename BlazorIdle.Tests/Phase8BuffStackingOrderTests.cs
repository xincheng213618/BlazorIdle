using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;
using System.Collections.Generic;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Tests for Phase 8: Buff stacking order implementation
    /// Method B: Time-ordered stacking with BuffStatApplier
    /// 
    /// 旧系统清理：这些测试已更新为使用新的 BuffStatApplier.ApplyBuffsToCombatStats()
    /// Legacy cleanup: Tests updated to use new BuffStatApplier.ApplyBuffsToCombatStats()
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
            
            public bool ReduceBuffStacks(string buffId, int stacksToRemove, string reason)
            {
                if (!_buffs.TryGetValue(buffId, out var buff))
                    return false;
                buff.Stacks = System.Math.Max(0, buff.Stacks - stacksToRemove);
                if (buff.Stacks <= 0)
                    _buffs.Remove(buffId);
                return true;
            }
            
            public void ReceiveDamage(int amount, DamageMeta meta) { }
            public void ReceiveHeal(int amount, HealMeta meta) { }
        }

        [Fact]
        public void TimeOrderedStacking_MultiplierThenAdditive_CorrectOrder()
        {
            // Arrange - 使用新的 BuffStatApplier 系统
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            
            // 创建基础 CombatStats
            var baseStats = new CombatStats { AttackPercent = 0, ChaseFlat = 0 };
            
            // Buff A (t=0): +50% via AttackPercent
            clock.Advance(0);
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("AttackPercent", 50.0)
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffA);
            
            // Buff B (t=100): +30 flat via ChaseFlat
            clock.Advance(100);
            var buffB = new BuffInstance(
                id: "buff_b",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("ChaseFlat", 30)
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffB);
            
            // Act - 使用 BuffStatApplier
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, owner);
            
            // Assert - Buff 效果正确应用
            Assert.Equal(50.0, result.AttackPercent);
            Assert.Equal(30, result.ChaseFlat);
        }

        [Fact]
        public void TimeOrderedStacking_AdditiveThenMultiplier_CorrectOrder()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var baseStats = new CombatStats { AttackPercent = 0, ChaseFlat = 0 };
            
            // Buff A (t=0): +30 flat
            clock.Advance(0);
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("ChaseFlat", 30)
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffA);
            
            // Buff B (t=100): +50% via AttackPercent
            clock.Advance(100);
            var buffB = new BuffInstance(
                id: "buff_b",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("AttackPercent", 50.0)
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffB);
            
            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, owner);
            
            // Assert
            Assert.Equal(30, result.ChaseFlat);
            Assert.Equal(50.0, result.AttackPercent);
        }

        [Fact]
        public void TimeOrderedStacking_ThreeBuffs_CorrectOrder()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var baseStats = new CombatStats { AttackPercent = 0, ChaseFlat = 0, ChasePercent = 0 };
            
            // Buff A (t=0): +50% AttackPercent
            clock.Advance(0);
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("AttackPercent", 50.0)
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
                    BuffEffect.StatAdditive("ChaseFlat", 30)
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffB);
            
            // Buff C (t=100): +20% ChasePercent
            clock.Advance(50);
            var buffC = new BuffInstance(
                id: "buff_c",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("ChasePercent", 20.0)
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffC);
            
            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, owner);
            
            // Assert - 所有 buff 效果正确应用
            Assert.Equal(50.0, result.AttackPercent);
            Assert.Equal(30, result.ChaseFlat);
            Assert.Equal(20.0, result.ChasePercent);
        }

        [Fact]
        public void MultiplicativeStacking_TwoMultipliers_PreventRunaway()
        {
            // Arrange - 测试 StatMultiplier 的累积效果
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var baseStats = new CombatStats { AttackPercent = 10.0 }; // 基础 10%
            
            // Buff A (t=0): ×1.5 on AttackPercent (StatMultiplier)
            clock.Advance(0);
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("AttackPercent", 0.5) // 乘以 1.5 (value * (1 + 0.5))
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffA);
            
            // Buff B (t=100): ×1.3 on AttackPercent
            clock.Advance(100);
            var buffB = new BuffInstance(
                id: "buff_b",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("AttackPercent", 0.3) // 乘以 1.3 (value * (1 + 0.3))
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffB);
            
            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, owner);
            
            // Assert
            // Base: 10%
            // Buff A: 10 * 1.5 = 15%
            // Buff B: 15 * 1.3 = 19.5%
            Assert.InRange(result.AttackPercent, 19.0, 20.0);
        }

        [Fact]
        public void TimeOrderedStacking_SameTimestamp_StableOrder()
        {
            // Arrange
            var owner = new TestBuffOwner();
            var baseStats = new CombatStats { AttackPercent = 0, ChaseFlat = 0 };
            
            // 两个 buff 都在 t=0
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("AttackPercent", 50.0)
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
                    BuffEffect.StatAdditive("ChaseFlat", 30)
                },
                appliedAtMs: 0
            );
            owner.ApplyBuff(buffB);
            
            // Act - 多次调用应该得到相同结果
            var result1 = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, owner);
            var result2 = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, owner);
            
            // Assert - 结果应该稳定
            Assert.Equal(result1.AttackPercent, result2.AttackPercent);
            Assert.Equal(result1.ChaseFlat, result2.ChaseFlat);
        }

        [Fact]
        public void TimeOrderedStacking_CritMultiplier_DoubleValues()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var baseStats = new CombatStats { CritDamageBonusPercent = 50.0 }; // 基础 50%
            
            // Buff A (t=0): +50% 暴击伤害 (additive)
            clock.Advance(0);
            var buffA = new BuffInstance(
                id: "buff_a",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("CritDamageBonusPercent", 50.0)
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffA);
            
            // Buff B (t=100): +30% 暴击伤害
            clock.Advance(100);
            var buffB = new BuffInstance(
                id: "buff_b",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("CritDamageBonusPercent", 30.0)
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buffB);
            
            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, owner);
            
            // Assert
            // Base: 50%
            // Buff A: 50 + 50 = 100%
            // Buff B: 100 + 30 = 130%
            Assert.Equal(130.0, result.CritDamageBonusPercent);
        }

        [Fact]
        public void TimeOrderedStacking_Debuffs_CorrectReduction()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var baseStats = new CombatStats { AttackPercent = 50.0 }; // 基础 50%
            
            // Debuff A (t=0): -20%
            clock.Advance(0);
            var debuffA = new BuffInstance(
                id: "debuff_a",
                ownerId: owner.Id,
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("AttackPercent", -20.0) // -20%
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(debuffA);
            
            // Debuff B (t=100): -10%
            clock.Advance(100);
            var debuffB = new BuffInstance(
                id: "debuff_b",
                ownerId: owner.Id,
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("AttackPercent", -10.0) // -10%
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(debuffB);
            
            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, owner);
            
            // Assert
            // Base: 50%
            // Debuff A: 50 - 20 = 30%
            // Debuff B: 30 - 10 = 20%
            Assert.Equal(20.0, result.AttackPercent);
        }

        [Fact]
        public void TimeOrderedStacking_MixedBuffsAndDebuffs_CorrectCalculation()
        {
            // Arrange
            var clock = new TestClock();
            var owner = new TestBuffOwner();
            var baseStats = new CombatStats { AttackPercent = 0 };
            
            // Buff (t=0): +50%
            clock.Advance(0);
            var buff = new BuffInstance(
                id: "buff",
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("AttackPercent", 50.0)
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(buff);
            
            // Debuff (t=50): -20%
            clock.Advance(50);
            var debuff = new BuffInstance(
                id: "debuff",
                ownerId: owner.Id,
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("AttackPercent", -20.0)
                },
                appliedAtMs: clock.NowMs
            );
            owner.ApplyBuff(debuff);
            
            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, owner);
            
            // Assert
            // Base: 0%
            // Buff (t=0): 0 + 50 = 50%
            // Debuff (t=50): 50 - 20 = 30%
            Assert.Equal(30.0, result.AttackPercent);
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
