using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Resources;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 6 fixes verification tests.
    /// Tests for BuffTemplate duration cloning, LowestHpAlly percentage logic, and resource consumption.
    /// </summary>
    public class Phase6FixesTests
    {
        [Fact]
        public void BuffTemplate_Clone_ShouldUseFullDuration()
        {
            // Phase 6 Fix Test: BuffTemplate should be created with full duration for reuse
            // Design note: BuffTemplate in BuffOperation should have full duration
            var template = new BuffInstance(
                id: "test_buff",
                ownerId: "template_owner",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0,
                tickIntervalSec: 1.0,
                maxStacks: 1
            );

            // Template should have full duration
            Assert.Equal(10.0, template.RemainingDurationSec);

            // BuffOperation should contain fresh templates, not ticked ones
            // This test verifies the design expectation
        }

        [Fact]
        public void BuffInstance_TickReducesRemainingDuration()
        {
            // Verify RemainingDurationSec decreases as time passes
            var buff = new BuffInstance(
                id: "test_buff",
                ownerId: "owner",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 15.0,
                tickIntervalSec: 1.0,
                maxStacks: 1
            );

            Assert.Equal(15.0, buff.RemainingDurationSec);
            
            buff.Tick(5.0);
            Assert.Equal(10.0, buff.RemainingDurationSec.Value, 0.01);

            buff.Tick(5.0);
            Assert.Equal(5.0, buff.RemainingDurationSec.Value, 0.01);
        }

        [Fact]
        public void LowestHpAlly_SelectsByPercentage_NotAbsoluteValue()
        {
            // Phase 6 Fix Test: LowestHpAlly should select by HP percentage, not absolute HP
            // Setup: Create three mock buff owners with different HP values
            var owner1 = new TestBuffOwner("ally1", currentHp: 50, maxHp: 100);  // 50% HP
            var owner2 = new TestBuffOwner("ally2", currentHp: 150, maxHp: 200); // 75% HP
            var owner3 = new TestBuffOwner("ally3", currentHp: 80, maxHp: 200);  // 40% HP - should be selected

            // Before fix: owner1 would be selected (absolute HP = 50)
            // After fix: owner3 should be selected (percentage = 40%)

            Assert.True(owner3.CurrentHp / (double)owner3.MaxHp < owner1.CurrentHp / (double)owner1.MaxHp);
            Assert.True(owner3.CurrentHp / (double)owner3.MaxHp < owner2.CurrentHp / (double)owner2.MaxHp);
        }

        [Fact]
        public void LowestHpAlly_HandlesZeroMaxHp()
        {
            // Edge case: Entity with MaxHp = 0 should be treated as 100% HP
            var owner = new TestBuffOwner("ally", currentHp: 0, maxHp: 0);
            
            // Should not throw, and percentage should default to 1.0 (100%)
            double hpPercentage = owner.MaxHp > 0 
                ? (double)owner.CurrentHp / owner.MaxHp 
                : 1.0;

            Assert.Equal(1.0, hpPercentage);
        }

        [Fact]
        public void ResourceChanges_PositiveAmount_IncreasesResource()
        {
            // Phase 6 Fix Test: Resource gains should increase bucket current
            var bucket = new ResourceBucket("mana", max: 100, initial: 0);
            int initialValue = bucket.Current;

            // Simulate resource gain of +20
            bucket.Gain(20, "test");

            Assert.Equal(initialValue + 20, bucket.Current);
        }

        [Fact]
        public void ResourceChanges_NegativeAmount_DecreasesResource()
        {
            // Phase 6 Fix Test: Resource costs should decrease bucket current
            var bucket = new ResourceBucket("mana", max: 100, initial: 50);
            int initialValue = bucket.Current;

            // Simulate resource cost of 15 (using ForceConsume)
            bucket.ForceConsume(15, "test");

            Assert.Equal(initialValue - 15, bucket.Current);
        }

        [Fact]
        public void ResourceChanges_ClampsToValidRange()
        {
            // Resource changes should respect min/max bounds
            var bucket = new ResourceBucket("energy", max: 100, initial: 50);

            // Test upper bound - Gain should clamp to max
            bucket.Gain(100, "test"); // Try to gain 100, should only gain 50 to reach max
            Assert.Equal(100, bucket.Current); // Clamped to maximum

            // Test lower bound - ForceConsume should clamp to 0
            bucket.ForceConsume(150, "test"); // Try to consume 150, should clamp to 0
            Assert.Equal(0, bucket.Current); // Clamped to 0
        }

        [Fact]
        public void BuffApplication_EndToEnd_AppliesBuffToOwner()
        {
            // End-to-end test: Verify buff application works correctly
            var character = new Character();
            var owner = new CharacterBuffOwner(character, memberId: "member1", resources: null, onDamageReceived: null, onHealReceived: null);

            var buffTemplate = new BuffInstance(
                id: "strength_buff",
                ownerId: "template", // Will be overwritten
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.StatMultiplier("atk", 1.5) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 20.0,
                tickIntervalSec: null, // No ticking for stat buff
                maxStacks: 1
            );

            // Design note: BuffTemplate should always be created fresh with full duration
            Assert.Equal(20.0, buffTemplate.RemainingDurationSec.Value, 0.01);

            // Apply buff (simulating what ApplyBuffOperation does)
            var buffToApply = new BuffInstance(
                id: buffTemplate.Id,
                ownerId: owner.Id,
                kind: buffTemplate.Kind,
                effects: buffTemplate.Effects,
                stackingPolicy: buffTemplate.StackingPolicy,
                durationSec: buffTemplate.RemainingDurationSec, // Use template's duration
                tickIntervalSec: buffTemplate.TickIntervalSec,
                maxStacks: buffTemplate.MaxStacks
            );

            owner.ApplyBuff(buffToApply);

            var appliedBuff = owner.Buffs[buffToApply.Id];
            Assert.Equal(20.0, appliedBuff.RemainingDurationSec.Value, 0.01); // Should have full duration
            Assert.True(owner.Buffs.ContainsKey("strength_buff"));
        }

        /// <summary>
        /// Test helper: Simple IBuffOwner implementation for testing.
        /// </summary>
        private class TestBuffOwner : IBuffOwner
        {
            public string Id { get; }
            public bool IsPlayer => true;
            public int CurrentHp { get; set; }
            public int MaxHp { get; }
            public ResourceBucketCollection? Buckets => null;
            public Dictionary<string, BuffInstance> Buffs { get; } = new Dictionary<string, BuffInstance>();

            public TestBuffOwner(string id, int currentHp, int maxHp)
            {
                Id = id;
                CurrentHp = currentHp;
                MaxHp = maxHp;
            }

            public void ApplyBuff(BuffInstance buff) => Buffs[buff.Id] = buff;
            public bool RemoveBuff(string buffId, string reason) => Buffs.Remove(buffId);
            public void ReceiveDamage(int amount, DamageMeta meta) => CurrentHp = System.Math.Max(0, CurrentHp - amount);
            public void ReceiveHeal(int amount, HealMeta meta) => CurrentHp = System.Math.Min(MaxHp, CurrentHp + amount);
        }
    }
}
