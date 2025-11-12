using Xunit;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Buffs;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 6: Tests for buff operation types and target resolution.
    /// Integration with MultiBattleInstance is tested through existing integration tests.
    /// </summary>
    public class Phase6BuffIntegrationTests
    {
        [Fact]
        public void BuffTarget_SelfEnum_HasCorrectValue()
        {
            // Test that BuffTarget enum values are correctly defined
            Assert.Equal(0, (int)BuffTarget.Self);
            Assert.Equal(1, (int)BuffTarget.Target);
            Assert.Equal(2, (int)BuffTarget.AllEnemies);
            Assert.Equal(3, (int)BuffTarget.AllAllies);
            Assert.Equal(4, (int)BuffTarget.RandomEnemy);
            Assert.Equal(5, (int)BuffTarget.LowestHpAlly);
        }

        [Fact]
        public void BuffOperation_WithSelfTarget_CreatesCorrectly()
        {
            // Arrange
            var buffTemplate = new BuffInstance(
                "test_buff",
                "temp_owner",
                BuffKind.Buff,
                new List<BuffEffect>());

            // Act
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.Self,
                BuffTemplate = buffTemplate
            };

            // Assert
            Assert.Equal(BuffOperationType.Apply, operation.Type);
            Assert.Equal(BuffTarget.Self, operation.Target);
            Assert.NotNull(operation.BuffTemplate);
        }

        [Fact]
        public void BuffOperation_WithTargetTarget_CreatesCorrectly()
        {
            // Arrange
            var buffTemplate = new BuffInstance(
                "debuff",
                "temp_owner",
                BuffKind.Debuff,
                new List<BuffEffect>());

            // Act
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.Target,
                BuffTemplate = buffTemplate
            };

            // Assert
            Assert.Equal(BuffTarget.Target, operation.Target);
        }

        [Fact]
        public void BuffOperation_WithAllEnemiesTarget_CreatesCorrectly()
        {
            // Arrange & Act
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.AllEnemies,
                BuffTemplate = new BuffInstance("aoe_debuff", "temp", BuffKind.Debuff, new List<BuffEffect>())
            };

            // Assert
            Assert.Equal(BuffTarget.AllEnemies, operation.Target);
        }

        [Fact]
        public void BuffOperation_WithAllAlliesTarget_CreatesCorrectly()
        {
            // Arrange & Act
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.AllAllies,
                BuffTemplate = new BuffInstance("party_buff", "temp", BuffKind.Buff, new List<BuffEffect>())
            };

            // Assert
            Assert.Equal(BuffTarget.AllAllies, operation.Target);
        }

        [Fact]
        public void BuffOperation_WithRandomEnemyTarget_CreatesCorrectly()
        {
            // Arrange & Act
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.RandomEnemy,
                BuffTemplate = new BuffInstance("random_debuff", "temp", BuffKind.Debuff, new List<BuffEffect>())
            };

            // Assert
            Assert.Equal(BuffTarget.RandomEnemy, operation.Target);
        }

        [Fact]
        public void BuffOperation_WithLowestHpAllyTarget_CreatesCorrectly()
        {
            // Arrange & Act
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.LowestHpAlly,
                BuffTemplate = new BuffInstance("heal_buff", "temp", BuffKind.Buff, new List<BuffEffect>())
            };

            // Assert
            Assert.Equal(BuffTarget.LowestHpAlly, operation.Target);
        }

        [Fact]
        public void BuffOperation_RemoveType_WithBuffId_CreatesCorrectly()
        {
            // Arrange & Act
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Remove,
                Target = BuffTarget.Target,
                BuffIdToRemove = "buff_to_dispel",
                Reason = "skill_dispel"
            };

            // Assert
            Assert.Equal(BuffOperationType.Remove, operation.Type);
            Assert.Equal("buff_to_dispel", operation.BuffIdToRemove);
            Assert.Equal("skill_dispel", operation.Reason);
        }

        [Fact]
        public void BuffOperation_WithInstantHeal_InSkillDef_CreatesCorrectly()
        {
            // Arrange & Act
            var skillDef = new SkillDef
            {
                Id = "heal_skill",
                InstantHeal = 100
            };

            // Assert
            Assert.Equal(100, skillDef.InstantHeal);
        }

        [Fact]
        public void SkillDef_WithMultipleBuffOperations_CreatesCorrectly()
        {
            // Arrange
            var buff1 = new BuffInstance("buff1", "temp", BuffKind.Buff, new List<BuffEffect>());
            var buff2 = new BuffInstance("buff2", "temp", BuffKind.Buff, new List<BuffEffect>());

            // Act
            var skillDef = new SkillDef
            {
                Id = "multi_buff_skill",
                OnCastBuffs = new List<BuffOperation>
                {
                    new BuffOperation { Type = BuffOperationType.Apply, Target = BuffTarget.Self, BuffTemplate = buff1 },
                    new BuffOperation { Type = BuffOperationType.Apply, Target = BuffTarget.Target, BuffTemplate = buff2 }
                }
            };

            // Assert
            Assert.Equal(2, skillDef.OnCastBuffs.Count);
            Assert.Equal(BuffTarget.Self, skillDef.OnCastBuffs[0].Target);
            Assert.Equal(BuffTarget.Target, skillDef.OnCastBuffs[1].Target);
        }

        [Fact]
        public void SkillDef_WithOnHitBuffs_CreatesCorrectly()
        {
            // Arrange
            var debuff = new BuffInstance("on_hit_debuff", "temp", BuffKind.Debuff, new List<BuffEffect>());

            // Act
            var skillDef = new SkillDef
            {
                Id = "attack_with_debuff",
                OnHitBuffs = new List<BuffOperation>
                {
                    new BuffOperation { Type = BuffOperationType.Apply, Target = BuffTarget.Target, BuffTemplate = debuff }
                }
            };

            // Assert
            Assert.Single(skillDef.OnHitBuffs);
            Assert.Equal(BuffOperationType.Apply, skillDef.OnHitBuffs[0].Type);
        }

        [Fact]
        public void SkillDef_WithOnCritBuffs_CreatesCorrectly()
        {
            // Arrange
            var critBuff = new BuffInstance("on_crit_buff", "temp", BuffKind.Buff, new List<BuffEffect>());

            // Act
            var skillDef = new SkillDef
            {
                Id = "crit_boost_skill",
                OnCritBuffs = new List<BuffOperation>
                {
                    new BuffOperation { Type = BuffOperationType.Apply, Target = BuffTarget.Self, BuffTemplate = critBuff }
                }
            };

            // Assert
            Assert.Single(skillDef.OnCritBuffs);
        }

        [Fact]
        public void BuffOperation_NullBuffTemplate_ForRemoveOperation_IsValid()
        {
            // Arrange & Act
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Remove,
                Target = BuffTarget.AllEnemies,
                BuffIdToRemove = "poison",
                BuffTemplate = null // Null is ok for Remove operations
            };

            // Assert
            Assert.Equal(BuffOperationType.Remove, operation.Type);
            Assert.Null(operation.BuffTemplate);
            Assert.Equal("poison", operation.BuffIdToRemove);
        }

        [Fact]
        public void BuffOperationType_Apply_HasCorrectValue()
        {
            Assert.Equal(0, (int)BuffOperationType.Apply);
        }

        [Fact]
        public void BuffOperationType_Remove_HasCorrectValue()
        {
            Assert.Equal(1, (int)BuffOperationType.Remove);
        }
    }
}
