using Xunit;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Buffs;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Step 2 Phase 5: 资源消耗与冷却系统测试
    /// Step 2 Phase 5: Resource consumption and cooldown system tests
    /// </summary>
    public class Step2Phase5Tests
    {
        #region CooldownManager Tests (4 tests)

        [Fact]
        public void CooldownManager_IsReady_ReturnsTrueWhenNoCooldown()
        {
            // Arrange
            var manager = new CooldownManager();

            // Act
            var result = manager.IsReady("test_skill");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CooldownManager_StartCooldown_MakesSkillNotReady()
        {
            // Arrange
            var manager = new CooldownManager();

            // Act
            manager.StartCooldown("test_skill", 5.0);

            // Assert
            Assert.False(manager.IsReady("test_skill"));
            Assert.Equal(5.0, manager.GetRemainingCooldown("test_skill"));
        }

        [Fact]
        public void CooldownManager_TickCooldowns_ReducesCooldownTime()
        {
            // Arrange
            var manager = new CooldownManager();
            manager.StartCooldown("test_skill", 5.0);

            // Act
            manager.TickCooldowns(2.0);

            // Assert
            Assert.False(manager.IsReady("test_skill"));
            Assert.Equal(3.0, manager.GetRemainingCooldown("test_skill"));
        }

        [Fact]
        public void CooldownManager_TickCooldowns_MakesSkillReadyWhenExpired()
        {
            // Arrange
            var manager = new CooldownManager();
            manager.StartCooldown("test_skill", 5.0);

            // Act
            manager.TickCooldowns(5.0);

            // Assert
            Assert.True(manager.IsReady("test_skill"));
            Assert.Equal(0, manager.GetRemainingCooldown("test_skill"));
        }

        #endregion

        #region ResourceManager - Cost Tests (4 tests)

        [Fact]
        public void ResourceManager_CheckResourceCost_ReturnsTrueWhenSufficientResources()
        {
            // Arrange
            var manager = new ResourceManager();
            var buckets = CreateResourceBuckets(rage: 10);
            var player = CreateTestCharacter();
            var buffOwner = new CharacterBuffOwner(player, "player_1", buckets);
            var skill = new SkillDef
            {
                Id = "test_skill",
                Costs = new List<ResourceCost>
                {
                    new ResourceCost { BucketId = "rage", Amount = 5 }
                }
            };
            var context = new BattleContext { Player = player, PlayerBuffOwner = buffOwner };

            // Act
            var result = manager.CheckResourceCost(skill, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ResourceManager_CheckResourceCost_ReturnsFalseWhenInsufficientResources()
        {
            // Arrange
            var manager = new ResourceManager();
            var buckets = CreateResourceBuckets(rage: 3);
            var player = CreateTestCharacter();
            var buffOwner = new CharacterBuffOwner(player, "player_1", buckets);
            var skill = new SkillDef
            {
                Id = "test_skill",
                Costs = new List<ResourceCost>
                {
                    new ResourceCost { BucketId = "rage", Amount = 5 }
                }
            };
            var context = new BattleContext { Player = player, PlayerBuffOwner = buffOwner };

            // Act
            var result = manager.CheckResourceCost(skill, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ResourceManager_ConsumeResourceCost_ReducesResourceAmount()
        {
            // Arrange
            var manager = new ResourceManager();
            var buckets = CreateResourceBuckets(rage: 10);
            var player = CreateTestCharacter();
            var buffOwner = new CharacterBuffOwner(player, "player_1", buckets);
            var skill = new SkillDef
            {
                Id = "test_skill",
                Costs = new List<ResourceCost>
                {
                    new ResourceCost { BucketId = "rage", Amount = 5 }
                }
            };
            var context = new BattleContext { Player = player, PlayerBuffOwner = buffOwner };

            // Act
            var result = manager.ConsumeResourceCost(skill, context);

            // Assert
            Assert.True(result);
            var rageBucket = buffOwner.Buckets?.GetBucket("rage");
            Assert.NotNull(rageBucket);
            Assert.Equal(5, rageBucket.Current);
        }

        [Fact]
        public void ResourceManager_ConsumeResourceCost_ReturnsFalseWhenInsufficientResources()
        {
            // Arrange
            var manager = new ResourceManager();
            var buckets = CreateResourceBuckets(rage: 3);
            var player = CreateTestCharacter();
            var buffOwner = new CharacterBuffOwner(player, "player_1", buckets);
            var skill = new SkillDef
            {
                Id = "test_skill",
                Costs = new List<ResourceCost>
                {
                    new ResourceCost { BucketId = "rage", Amount = 5 }
                }
            };
            var context = new BattleContext { Player = player, PlayerBuffOwner = buffOwner };
            var initialRage = buffOwner.Buckets?.GetBucket("rage")?.Current ?? 0;

            // Act
            var result = manager.ConsumeResourceCost(skill, context);

            // Assert
            Assert.False(result);
            var rageBucket = buffOwner.Buckets?.GetBucket("rage");
            Assert.NotNull(rageBucket);
            Assert.Equal(initialRage, rageBucket.Current); // Resource unchanged
        }

        #endregion

        #region ResourceManager - Gain Tests (3 tests)

        [Fact]
        public void ResourceManager_ApplyResourceGains_IncreasesResourceAmount()
        {
            // Arrange
            var manager = new ResourceManager();
            var buckets = CreateResourceBuckets(rage: 5);
            var player = CreateTestCharacter();
            var buffOwner = new CharacterBuffOwner(player, "player_1", buckets);
            var skill = new SkillDef
            {
                Id = "test_skill",
                Gains = new List<ResourceGain>
                {
                    new ResourceGain { BucketId = "rage", Amount = 3 }
                }
            };
            var context = new BattleContext { Player = player, PlayerBuffOwner = buffOwner };

            // Act
            manager.ApplyResourceGains(skill, context);

            // Assert
            var rageBucket = buffOwner.Buckets?.GetBucket("rage");
            Assert.NotNull(rageBucket);
            Assert.Equal(8, rageBucket.Current);
        }

        [Fact]
        public void ResourceManager_ApplyResourceGains_CapsAtMaxResource()
        {
            // Arrange
            var manager = new ResourceManager();
            var buckets = CreateResourceBuckets(rage: 18); // Max is 20
            var player = CreateTestCharacter();
            var buffOwner = new CharacterBuffOwner(player, "player_1", buckets);
            var skill = new SkillDef
            {
                Id = "test_skill",
                Gains = new List<ResourceGain>
                {
                    new ResourceGain { BucketId = "rage", Amount = 5 }
                }
            };
            var context = new BattleContext { Player = player, PlayerBuffOwner = buffOwner };

            // Act
            manager.ApplyResourceGains(skill, context);

            // Assert
            var rageBucket = buffOwner.Buckets?.GetBucket("rage");
            Assert.NotNull(rageBucket);
            Assert.Equal(20, rageBucket.Current); // Capped at max
        }

        [Fact]
        public void ResourceManager_ApplyResourceGains_SupportsMultipleResources()
        {
            // Arrange
            var manager = new ResourceManager();
            var buckets = CreateResourceBucketsWithMana(rage: 5, mana: 10);
            var player = CreateTestCharacter();
            var buffOwner = new CharacterBuffOwner(player, "player_1", buckets);
            var skill = new SkillDef
            {
                Id = "test_skill",
                Gains = new List<ResourceGain>
                {
                    new ResourceGain { BucketId = "rage", Amount = 2 },
                    new ResourceGain { BucketId = "mana", Amount = 5 }
                }
            };
            var context = new BattleContext { Player = player, PlayerBuffOwner = buffOwner };

            // Act
            manager.ApplyResourceGains(skill, context);

            // Assert
            var rageBucket = buffOwner.Buckets?.GetBucket("rage");
            var manaBucket = buffOwner.Buckets?.GetBucket("mana");
            Assert.NotNull(rageBucket);
            Assert.NotNull(manaBucket);
            Assert.Equal(7, rageBucket.Current);
            Assert.Equal(15, manaBucket.Current);
        }

        #endregion

        #region InstantHeal Fix Tests (4 tests)

        [Fact]
        public void SkillDef_InstantHeal_PreservedAsSkillProperty()
        {
            // Arrange & Act
            var skill = new SkillDef
            {
                Id = "heal_skill",
                InstantHeal = 50
            };

            // Assert
            Assert.Equal(50, skill.InstantHeal);
        }

        [Fact]
        public void BuffEffectType_DoesNotContainInstantHeal()
        {
            // Arrange
            var enumValues = System.Enum.GetValues(typeof(BuffEffectType));
            var enumNames = System.Enum.GetNames(typeof(BuffEffectType));

            // Assert
            Assert.DoesNotContain("InstantHeal", enumNames);
        }

        [Fact]
        public void BuffInstance_DoesNotHaveInstantHealMethod()
        {
            // Arrange
            var buffInstance = new BuffInstance(
                id: "test_buff",
                ownerId: "player_1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>()
            );

            // Act & Assert
            var type = buffInstance.GetType();
            var hasInstantHealMethod = type.GetMethod("HasInstantHeal") != null;
            var getInstantHealAmountMethod = type.GetMethod("GetInstantHealAmount") != null;

            Assert.False(hasInstantHealMethod, "BuffInstance should not have HasInstantHeal method");
            Assert.False(getInstantHealAmountMethod, "BuffInstance should not have GetInstantHealAmount method");
        }

        [Fact]
        public void BuffEffect_DoesNotHaveInstantHealFactory()
        {
            // Arrange & Act
            var type = typeof(BuffEffect);
            var instantHealMethod = type.GetMethod("InstantHeal", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            // Assert
            Assert.Null(instantHealMethod);
        }

        #endregion

        #region Helper Methods

        private Character CreateTestCharacter()
        {
            return new Character
            {
                MaxHp = 100,
                Hp = 100,
                DamagePerAttack = 10,
                AttackRateAPS = 2.0,
                CritChancePercent = 5.0,
                CritMultiplier = 2.0
            };
        }

        private ResourceBucketCollection CreateResourceBuckets(int rage = 0)
        {
            // Use non-default constructor to avoid creating default rage bucket
            var collection = new ResourceBucketCollection("rage", max: 20, initial: rage);
            return collection;
        }

        private ResourceBucketCollection CreateResourceBucketsWithMana(int rage = 0, int mana = 0)
        {
            var collection = new ResourceBucketCollection("rage", max: 20, initial: rage);
            collection.AddBucket("mana", max: 100, initial: mana);
            return collection;
        }

        #endregion
    }
}
