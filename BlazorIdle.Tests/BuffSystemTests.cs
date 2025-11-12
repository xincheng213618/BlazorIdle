using System;
using System.Collections.Generic;
using BlazorIdle.Game.Buffs;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Unit tests for the Buff System (Phase 3).
    /// </summary>
    public class BuffSystemTests
    {
        #region BuffEffect Tests

        [Fact]
        public void BuffEffect_StatMultiplier_CreatesCorrectly()
        {
            var effect = BuffEffect.StatMultiplier("DamagePerAttack", 0.15);

            Assert.Equal(BuffEffectType.StatMultiplier, effect.Type);
            Assert.Equal("DamagePerAttack", effect.Target);
            Assert.Equal(0.15, effect.Value);
        }

        [Fact]
        public void BuffEffect_StatAdditive_CreatesCorrectly()
        {
            var effect = BuffEffect.StatAdditive("CritChancePercent", 0.05);

            Assert.Equal(BuffEffectType.StatAdditive, effect.Type);
            Assert.Equal("CritChancePercent", effect.Target);
            Assert.Equal(0.05, effect.Value);
        }

        [Fact]
        public void BuffEffect_ForceCrit_CreatesCorrectly()
        {
            var effect = BuffEffect.ForceCrit();

            Assert.Equal(BuffEffectType.ForceCrit, effect.Type);
        }

        [Fact]
        public void BuffEffect_DamageOverTime_CreatesCorrectly()
        {
            var effect = BuffEffect.DamageOverTime(10);

            Assert.Equal(BuffEffectType.DamageOverTime, effect.Type);
            Assert.Equal(10, effect.AmountPerTick);
        }

        [Fact]
        public void BuffEffect_HealOverTime_CreatesCorrectly()
        {
            var effect = BuffEffect.HealOverTime(5);

            Assert.Equal(BuffEffectType.HealOverTime, effect.Type);
            Assert.Equal(5, effect.AmountPerTick);
        }

        [Fact]
        public void BuffEffect_InstantHeal_CreatesCorrectly()
        {
            var effect = BuffEffect.InstantHeal(50);

            Assert.Equal(BuffEffectType.InstantHeal, effect.Type);
            Assert.Equal(50, effect.Value);
        }

        [Fact]
        public void BuffEffect_StatReduction_CreatesCorrectly()
        {
            var effect = BuffEffect.StatReduction("HastePercent", 0.10);

            Assert.Equal(BuffEffectType.StatReduction, effect.Type);
            Assert.Equal("HastePercent", effect.Target);
            Assert.Equal(0.10, effect.Value);
        }

        #endregion

        #region BuffInstance Basic Tests

        [Fact]
        public void BuffInstance_Constructor_SetsPropertiesCorrectly()
        {
            var effects = new List<BuffEffect>
            {
                BuffEffect.StatMultiplier("DamagePerAttack", 0.15)
            };

            var buff = new BuffInstance(
                id: "warrior_rage",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: effects,
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 6.0,
                tickIntervalSec: null,
                maxStacks: 1,
                sourceSkillId: "special_pulse"
            );

            Assert.Equal("warrior_rage", buff.Id);
            Assert.Equal("player1", buff.OwnerId);
            Assert.Equal(BuffKind.Buff, buff.Kind);
            Assert.Single(buff.Effects);
            Assert.Equal(BuffStackingPolicy.Refresh, buff.StackingPolicy);
            Assert.Equal(1, buff.Stacks);
            Assert.Equal(1, buff.MaxStacks);
            Assert.Equal(6.0, buff.RemainingDurationSec);
            Assert.Null(buff.TickIntervalSec);
            Assert.Equal("special_pulse", buff.SourceSkillId);
        }

        [Fact]
        public void BuffInstance_DefaultConstructor_InitializesCorrectly()
        {
            var buff = new BuffInstance();

            Assert.NotNull(buff.Id);
            Assert.NotNull(buff.OwnerId);
            Assert.Equal(BuffKind.Buff, buff.Kind);
            Assert.Empty(buff.Effects);
            Assert.Equal(BuffStackingPolicy.Refresh, buff.StackingPolicy);
            Assert.Equal(1, buff.Stacks);
            Assert.Equal(0, buff.MaxStacks);
        }

        #endregion

        #region BuffInstance Duration Tests

        [Fact]
        public void BuffInstance_Tick_DecreasesDuration()
        {
            var buff = new BuffInstance(
                "test_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>(),
                durationSec: 5.0
            );

            buff.Tick(1.0);

            Assert.Equal(4.0, buff.RemainingDurationSec);
        }

        [Fact]
        public void BuffInstance_Tick_ClampsToZero()
        {
            var buff = new BuffInstance(
                "test_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>(),
                durationSec: 1.0
            );

            buff.Tick(2.0);

            Assert.Equal(0.0, buff.RemainingDurationSec);
        }

        [Fact]
        public void BuffInstance_IsExpired_ReturnsTrueWhenDurationZero()
        {
            var buff = new BuffInstance(
                "test_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>(),
                durationSec: 0.0
            );

            Assert.True(buff.IsExpired());
        }

        [Fact]
        public void BuffInstance_IsExpired_ReturnsFalseWhenDurationRemaining()
        {
            var buff = new BuffInstance(
                "test_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>(),
                durationSec: 5.0
            );

            Assert.False(buff.IsExpired());
        }

        [Fact]
        public void BuffInstance_IsExpired_ReturnsFalseWhenNoDuration()
        {
            var buff = new BuffInstance(
                "test_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>()
            );

            Assert.False(buff.IsExpired());
        }

        [Fact]
        public void BuffInstance_RefreshDuration_UpdatesDuration()
        {
            var buff = new BuffInstance(
                "test_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>(),
                durationSec: 2.0
            );

            buff.Tick(1.0);
            Assert.Equal(1.0, buff.RemainingDurationSec);

            buff.RefreshDuration(6.0);
            Assert.Equal(6.0, buff.RemainingDurationSec);
        }

        #endregion

        #region BuffInstance Tick Tests (DoT/HoT)

        [Fact]
        public void BuffInstance_Tick_AccumulatesTickTimer()
        {
            var buff = new BuffInstance(
                "test_dot",
                "enemy1",
                BuffKind.Debuff,
                new List<BuffEffect> { BuffEffect.DamageOverTime(10) },
                durationSec: 10.0,
                tickIntervalSec: 1.0
            );

            Assert.Equal(0.0, buff.TickAccumulator);

            buff.Tick(0.5);
            Assert.Equal(0.5, buff.TickAccumulator);

            buff.Tick(0.3);
            Assert.Equal(0.8, buff.TickAccumulator);
        }

        [Fact]
        public void BuffInstance_Tick_TriggersWhenIntervalReached()
        {
            var buff = new BuffInstance(
                "test_dot",
                "enemy1",
                BuffKind.Debuff,
                new List<BuffEffect> { BuffEffect.DamageOverTime(10) },
                durationSec: 10.0,
                tickIntervalSec: 1.0
            );

            bool tickOccurred = buff.Tick(0.5);
            Assert.False(tickOccurred);

            tickOccurred = buff.Tick(0.6);
            Assert.True(tickOccurred);
            Assert.Equal(0.1, buff.TickAccumulator, 2); // 0.5 + 0.6 - 1.0 = 0.1
        }

        [Fact]
        public void BuffInstance_Tick_PreservesFractionalProgress()
        {
            var buff = new BuffInstance(
                "test_hot",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect> { BuffEffect.HealOverTime(5) },
                durationSec: 10.0,
                tickIntervalSec: 1.5
            );

            buff.Tick(1.0);
            Assert.Equal(1.0, buff.TickAccumulator);

            buff.Tick(0.7);
            Assert.True(buff.TickAccumulator > 0); // Should have rolled over but preserve remainder
            Assert.Equal(0.2, buff.TickAccumulator, 2); // 1.0 + 0.7 - 1.5 = 0.2
        }

        #endregion

        #region BuffInstance Stacking Tests

        [Fact]
        public void BuffInstance_AddStack_IncreasesStacks()
        {
            var buff = new BuffInstance(
                "test_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Stack,
                maxStacks: 5
            );

            Assert.Equal(1, buff.Stacks);

            bool added = buff.AddStack();
            Assert.True(added);
            Assert.Equal(2, buff.Stacks);
        }

        [Fact]
        public void BuffInstance_AddStack_RespectsMaxStacks()
        {
            var buff = new BuffInstance(
                "test_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Stack,
                maxStacks: 2
            );

            buff.AddStack(); // Stack 2
            Assert.Equal(2, buff.Stacks);

            bool added = buff.AddStack(); // Try to add stack 3
            Assert.False(added);
            Assert.Equal(2, buff.Stacks); // Should remain at 2
        }

        [Fact]
        public void BuffInstance_AddStack_UnlimitedWhenMaxStacksZero()
        {
            var buff = new BuffInstance(
                "test_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Stack,
                maxStacks: 0
            );

            for (int i = 0; i < 100; i++)
            {
                bool added = buff.AddStack();
                Assert.True(added);
            }

            Assert.Equal(101, buff.Stacks); // 1 initial + 100 added
        }

        #endregion

        #region BuffInstance Helper Methods Tests

        [Fact]
        public void BuffInstance_HasDamageOverTime_ReturnsTrueWhenDoTPresent()
        {
            var buff = new BuffInstance(
                "test_dot",
                "enemy1",
                BuffKind.Debuff,
                new List<BuffEffect>
                {
                    BuffEffect.DamageOverTime(10),
                    BuffEffect.StatReduction("HastePercent", 0.1)
                }
            );

            Assert.True(buff.HasDamageOverTime());
        }

        [Fact]
        public void BuffInstance_HasDamageOverTime_ReturnsFalseWhenNoDoT()
        {
            var buff = new BuffInstance(
                "test_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.15)
                }
            );

            Assert.False(buff.HasDamageOverTime());
        }

        [Fact]
        public void BuffInstance_HasHealOverTime_ReturnsTrueWhenHoTPresent()
        {
            var buff = new BuffInstance(
                "test_hot",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>
                {
                    BuffEffect.HealOverTime(5)
                }
            );

            Assert.True(buff.HasHealOverTime());
        }

        [Fact]
        public void BuffInstance_HasInstantHeal_ReturnsTrueWhenPresent()
        {
            var buff = new BuffInstance(
                "test_heal",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>
                {
                    BuffEffect.InstantHeal(50)
                }
            );

            Assert.True(buff.HasInstantHeal());
        }

        [Fact]
        public void BuffInstance_GetDamagePerTick_SumsAllDoTEffects()
        {
            var buff = new BuffInstance(
                "test_dot",
                "enemy1",
                BuffKind.Debuff,
                new List<BuffEffect>
                {
                    BuffEffect.DamageOverTime(10),
                    BuffEffect.DamageOverTime(5),
                    BuffEffect.StatReduction("Armor", 0.2)
                }
            );

            Assert.Equal(15, buff.GetDamagePerTick());
        }

        [Fact]
        public void BuffInstance_GetDamagePerTick_MultipliesByStacks()
        {
            var buff = new BuffInstance(
                "test_dot",
                "enemy1",
                BuffKind.Debuff,
                new List<BuffEffect>
                {
                    BuffEffect.DamageOverTime(10)
                },
                stackingPolicy: BuffStackingPolicy.Stack,
                maxStacks: 5
            );

            buff.AddStack();
            buff.AddStack();
            // Now has 3 stacks

            Assert.Equal(30, buff.GetDamagePerTick()); // 10 * 3
        }

        [Fact]
        public void BuffInstance_GetHealPerTick_SumsAllHoTEffects()
        {
            var buff = new BuffInstance(
                "test_hot",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>
                {
                    BuffEffect.HealOverTime(5),
                    BuffEffect.HealOverTime(3)
                }
            );

            Assert.Equal(8, buff.GetHealPerTick());
        }

        [Fact]
        public void BuffInstance_GetHealPerTick_MultipliesByStacks()
        {
            var buff = new BuffInstance(
                "test_hot",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>
                {
                    BuffEffect.HealOverTime(5)
                },
                stackingPolicy: BuffStackingPolicy.Stack,
                maxStacks: 3
            );

            buff.AddStack();
            // Now has 2 stacks

            Assert.Equal(10, buff.GetHealPerTick()); // 5 * 2
        }

        [Fact]
        public void BuffInstance_GetInstantHealAmount_SumsAllInstantHeals()
        {
            var buff = new BuffInstance(
                "test_heal",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>
                {
                    BuffEffect.InstantHeal(50),
                    BuffEffect.InstantHeal(25)
                }
            );

            Assert.Equal(75, buff.GetInstantHealAmount());
        }

        #endregion

        #region Complex Scenarios

        [Fact]
        public void BuffInstance_ComplexBuff_WithMultipleEffects()
        {
            // Warrior rage buff: +15% damage, +10% haste, +5% crit, lasts 6 seconds
            var buff = new BuffInstance(
                "warrior_rage_boost",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.15),
                    BuffEffect.StatAdditive("HastePercent", 0.10),
                    BuffEffect.StatAdditive("CritChancePercent", 0.05)
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 6.0
            );

            Assert.Equal(3, buff.Effects.Count);
            Assert.Equal(6.0, buff.RemainingDurationSec);
            Assert.False(buff.HasDamageOverTime());
            Assert.False(buff.HasHealOverTime());
        }

        [Fact]
        public void BuffInstance_MageDoT_TicksMultipleTimes()
        {
            // Mage DoT: 6 damage per second, lasts 6 seconds
            var buff = new BuffInstance(
                "mage_dot",
                "enemy1",
                BuffKind.Debuff,
                new List<BuffEffect>
                {
                    BuffEffect.DamageOverTime(6)
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 6.0,
                tickIntervalSec: 1.0
            );

            int tickCount = 0;
            double totalTime = 0;

            while (totalTime < 6.0 && !buff.IsExpired())
            {
                bool ticked = buff.Tick(0.5);
                totalTime += 0.5;

                if (ticked)
                {
                    tickCount++;
                    Assert.Equal(6, buff.GetDamagePerTick());
                }
            }

            Assert.True(tickCount >= 5); // Should tick at least 5 times in 6 seconds
            Assert.True(buff.IsExpired());
        }

        [Fact]
        public void BuffInstance_PermanentBuff_NeverExpires()
        {
            var buff = new BuffInstance(
                "permanent_buff",
                "player1",
                BuffKind.Buff,
                new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.5)
                }
            );

            for (int i = 0; i < 100; i++)
            {
                buff.Tick(1.0);
                Assert.False(buff.IsExpired());
            }

            Assert.Null(buff.RemainingDurationSec);
        }

        #endregion
    }
}
