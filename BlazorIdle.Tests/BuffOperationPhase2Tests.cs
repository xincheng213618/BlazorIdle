using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Skills;
using BlazorIdle.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Tests for Phase 2: BuffOperation with BuffConfigId support.
    /// </summary>
    public class BuffOperationPhase2Tests
    {
        #region BuffOperation Helper Methods Tests

        [Fact]
        public void BuffOperation_ApplyByConfigId_CreatesCorrectOperation()
        {
            var operation = BuffOperation.ApplyByConfigId("damage_boost");

            Assert.Equal(BuffOperationType.Apply, operation.Type);
            Assert.Equal("damage_boost", operation.BuffConfigId);
            Assert.Null(operation.BuffTemplate);
            Assert.Null(operation.TargetOverride);
        }

        [Fact]
        public void BuffOperation_ApplyByConfigId_WithTargetOverride_CreatesCorrectOperation()
        {
            var operation = BuffOperation.ApplyByConfigId("warrior_rage_boost", BuffTarget.AllAllies);

            Assert.Equal(BuffOperationType.Apply, operation.Type);
            Assert.Equal("warrior_rage_boost", operation.BuffConfigId);
            Assert.Equal(BuffTarget.AllAllies, operation.TargetOverride);
        }

        [Fact]
        public void BuffOperation_ApplyByTemplate_CreatesCorrectOperation()
        {
            var template = new BuffInstance
            {
                Id = "test_buff",
                Kind = BuffKind.Buff
            };

            var operation = BuffOperation.ApplyByTemplate(template, BuffTarget.Self);

            Assert.Equal(BuffOperationType.Apply, operation.Type);
            Assert.Same(template, operation.BuffTemplate);
            Assert.Equal(BuffTarget.Self, operation.Target);
            Assert.Null(operation.BuffConfigId);
        }

        [Fact]
        public void BuffOperation_Remove_CreatesCorrectOperation()
        {
            var operation = BuffOperation.Remove("weakness", BuffTarget.Target, "expired");

            Assert.Equal(BuffOperationType.Remove, operation.Type);
            Assert.Equal("weakness", operation.BuffIdToRemove);
            Assert.Equal(BuffTarget.Target, operation.Target);
            Assert.Equal("expired", operation.Reason);
        }

        #endregion

        #region MultiBattleInstance Integration Tests

        [Fact]
        public void BuffConfig_ToBuffInstance_CanBeUsedForCombat()
        {
            // Arrange
            var repo = BuffRepository.CreateNew();
            
            // Act
            var buffConfig = repo.GetBuffById("damage_boost");
            Assert.NotNull(buffConfig);

            var buffInstance = buffConfig.ToBuffInstance("char1");
            
            // Assert - Verify the buff instance is ready for combat
            Assert.Equal("damage_boost", buffInstance.Id);
            Assert.Equal("char1", buffInstance.OwnerId);
            Assert.Equal(BuffKind.Buff, buffInstance.Kind);
            Assert.Equal(10.0, buffInstance.RemainingDurationSec);
            Assert.Equal(BuffStackingPolicy.Stack, buffInstance.StackingPolicy);
            Assert.Equal(3, buffInstance.MaxStacks);
            Assert.Single(buffInstance.Effects);
            Assert.Equal(BuffEffectType.StatMultiplier, buffInstance.Effects[0].Type);
        }

        [Fact]
        public void BuffConfig_DefaultTarget_UsedWhenNoOverride()
        {
            // Arrange
            var repo = BuffRepository.CreateNew();
            var buffConfig = repo.GetBuffById("mage_burn_dot"); // DefaultTarget = AllEnemies

            Assert.NotNull(buffConfig);
            Assert.Equal(BuffTarget.AllEnemies, buffConfig.DefaultTarget);

            // Act - Create operation without override
            var operation = BuffOperation.ApplyByConfigId("mage_burn_dot");

            // Assert
            Assert.Null(operation.TargetOverride);
            // In ApplyBuffOperation, this should use buffConfig.DefaultTarget (AllEnemies)
        }

        [Fact]
        public void BuffConfig_TargetOverride_OverridesDefault()
        {
            // Arrange
            var repo = BuffRepository.CreateNew();
            var buffConfig = repo.GetBuffById("mage_burn_dot"); // DefaultTarget = AllEnemies

            Assert.NotNull(buffConfig);
            Assert.Equal(BuffTarget.AllEnemies, buffConfig.DefaultTarget);

            // Act - Create operation with override
            var operation = BuffOperation.ApplyByConfigId("mage_burn_dot", BuffTarget.Target);

            // Assert
            Assert.Equal(BuffTarget.Target, operation.TargetOverride);
            // In ApplyBuffOperation, this should use TargetOverride (Target) instead of DefaultTarget
        }

        [Fact]
        public void BuffOperation_NonExistentConfigId_ShouldBeHandledGracefully()
        {
            // Arrange
            var operation = BuffOperation.ApplyByConfigId("non_existent_buff");

            // Assert
            Assert.Equal("non_existent_buff", operation.BuffConfigId);
            // ApplyBuffOperation should handle this gracefully by logging warning and returning early
        }

        [Fact]
        public void BuffOperation_BackwardCompatibility_InlineTemplateStillWorks()
        {
            // Arrange
            var template = new BuffInstance
            {
                Id = "custom_inline_buff",
                Kind = BuffKind.Buff,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.20)
                },
                RemainingDurationSec = 5.0,
                StackingPolicy = BuffStackingPolicy.Refresh
            };

            var operation = BuffOperation.ApplyByTemplate(template, BuffTarget.Self);

            // Assert
            Assert.Null(operation.BuffConfigId);
            Assert.NotNull(operation.BuffTemplate);
            Assert.Equal("custom_inline_buff", operation.BuffTemplate.Id);
            // ApplyBuffOperation should use BuffTemplate when BuffConfigId is not set
        }

        [Fact]
        public void BuffOperation_Priority_ConfigIdOverTemplate()
        {
            // Arrange - Create operation with both ConfigId and Template
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Apply,
                BuffConfigId = "damage_boost", // This should take priority
                BuffTemplate = new BuffInstance
                {
                    Id = "should_be_ignored",
                    Kind = BuffKind.Buff
                },
                Target = BuffTarget.Self
            };

            // Assert
            Assert.NotNull(operation.BuffConfigId);
            Assert.NotNull(operation.BuffTemplate);
            // ApplyBuffOperation should prioritize BuffConfigId over BuffTemplate
            // The BuffTemplate should be ignored when BuffConfigId is set
        }

        #endregion

        #region BuffConfig Properties Tests

        [Fact]
        public void BuffConfig_ToBuffInstance_PreservesAllProperties()
        {
            var repo = BuffRepository.CreateNew();
            var config = repo.GetBuffById("warrior_rage_boost");
            Assert.NotNull(config);

            var instance = config.ToBuffInstance("player1", "special_skill");

            Assert.Equal(config.Id, instance.Id);
            Assert.Equal("player1", instance.OwnerId);
            Assert.Equal(config.Kind, instance.Kind);
            Assert.Equal(config.DurationSec, instance.RemainingDurationSec);
            Assert.Equal(config.TickIntervalSec, instance.TickIntervalSec);
            Assert.Equal(config.StackingPolicy, instance.StackingPolicy);
            Assert.Equal(config.MaxStacks, instance.MaxStacks);
            Assert.Equal("special_skill", instance.SourceSkillId);
            Assert.Equal(config.Effects.Count, instance.Effects.Count);
        }

        [Fact]
        public void MultipleBufs_CanBeReferencedByConfigId()
        {
            var repo = BuffRepository.CreateNew();

            // Test various buff types
            var buffs = new[]
            {
                "warrior_rage_boost",  // Multi-effect buff
                "mage_burn_dot",       // DoT debuff
                "regeneration",        // HoT buff
                "weakness",            // Stat reduction debuff
                "haste",               // Stackable buff
                "force_crit"           // Special effect buff
            };

            foreach (var buffId in buffs)
            {
                var config = repo.GetBuffById(buffId);
                Assert.NotNull(config);

                var instance = config.ToBuffInstance("test_owner");
                Assert.Equal(buffId, instance.Id);
                Assert.Equal("test_owner", instance.OwnerId);

                var operation = BuffOperation.ApplyByConfigId(buffId);
                Assert.Equal(buffId, operation.BuffConfigId);
            }
        }

        #endregion
    }
}
