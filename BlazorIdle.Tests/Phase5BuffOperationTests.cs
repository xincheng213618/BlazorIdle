using Xunit;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Buffs;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Tests for Phase 5: Buff operations in skills.
    /// </summary>
    public class Phase5BuffOperationTests
    {
        #region BuffOperation Tests

        [Fact]
        public void BuffOperation_ApplyToSelf_CreatesCorrectly()
        {
            var buffTemplate = new BuffInstance("test_buff", "owner_id", BuffKind.Buff, new List<BuffEffect>());
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.Self,
                BuffTemplate = buffTemplate,
                Reason = "test"
            };

            Assert.Equal(BuffOperationType.Apply, operation.Type);
            Assert.Equal(BuffTarget.Self, operation.Target);
            Assert.Same(buffTemplate, operation.BuffTemplate);
            Assert.Equal("test", operation.Reason);
        }

        [Fact]
        public void BuffOperation_RemoveBuff_CreatesCorrectly()
        {
            var operation = new BuffOperation
            {
                Type = BuffOperationType.Remove,
                Target = BuffTarget.Target,
                BuffIdToRemove = "buff_to_remove",
                Reason = "dispelled"
            };

            Assert.Equal(BuffOperationType.Remove, operation.Type);
            Assert.Equal(BuffTarget.Target, operation.Target);
            Assert.Equal("buff_to_remove", operation.BuffIdToRemove);
            Assert.Equal("dispelled", operation.Reason);
        }

        [Fact]
        public void BuffTarget_AllTargetTypesAvailable()
        {
            // Ensure all target types are accessible
            var targets = new[] 
            {
                BuffTarget.Self,
                BuffTarget.Target,
                BuffTarget.AllEnemies,
                BuffTarget.AllAllies,
                BuffTarget.RandomEnemy,
                BuffTarget.LowestHpAlly
            };

            Assert.Equal(6, targets.Length);
        }

        #endregion

        #region SkillCastResult Tests

        [Fact]
        public void SkillCastResult_BuffOperations_InitializesEmpty()
        {
            var result = new SkillCastResult();

            Assert.NotNull(result.BuffOperations);
            Assert.Empty(result.BuffOperations);
            Assert.Equal(0, result.InstantHeal);
        }

        [Fact]
        public void SkillCastResult_CanAddBuffOperations()
        {
            var result = new SkillCastResult
            {
                DamageDealt = 100,
                IsCrit = true
            };

            var buffOp = new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.Self
            };

            result.BuffOperations.Add(buffOp);

            Assert.Single(result.BuffOperations);
            Assert.Same(buffOp, result.BuffOperations[0]);
        }

        [Fact]
        public void SkillCastResult_InstantHeal_CanBeSet()
        {
            var result = new SkillCastResult
            {
                InstantHeal = 50
            };

            Assert.Equal(50, result.InstantHeal);
        }

        #endregion

        #region SkillDef Tests

        [Fact]
        public void SkillDef_DefaultValues_AreCorrect()
        {
            var skillDef = new SkillDef();

            Assert.Equal("", skillDef.Id);
            Assert.Equal(1.0, skillDef.DamageMultiplier);
            Assert.Equal(0, skillDef.InstantHeal);
            Assert.True(skillDef.AlwaysHits);
            Assert.True(skillDef.CanCrit);
            Assert.Empty(skillDef.OnCastBuffs);
            Assert.Empty(skillDef.OnHitBuffs);
            Assert.Empty(skillDef.OnCritBuffs);
            Assert.Empty(skillDef.ResourceCosts);
            Assert.Empty(skillDef.ResourceGains);
        }

        [Fact]
        public void SkillDef_CanConfigureBuffOperations()
        {
            var skillDef = new SkillDef
            {
                Id = "warrior_rage_strike"
            };

            var buffOp = new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.Self,
                BuffTemplate = new BuffInstance("rage_buff", "owner", BuffKind.Buff, new List<BuffEffect>())
            };

            skillDef.OnHitBuffs.Add(buffOp);

            Assert.Single(skillDef.OnHitBuffs);
            Assert.Same(buffOp, skillDef.OnHitBuffs[0]);
        }

        [Fact]
        public void SkillDef_CanConfigureResourceCosts()
        {
            var skillDef = new SkillDef
            {
                Id = "special_ability"
            };

            skillDef.ResourceCosts["rage"] = 5;
            skillDef.ResourceGains["energy"] = 10;

            Assert.Equal(5, skillDef.ResourceCosts["rage"]);
            Assert.Equal(10, skillDef.ResourceGains["energy"]);
        }

        [Fact]
        public void SkillDef_CanConfigureInstantHeal()
        {
            var skillDef = new SkillDef
            {
                Id = "healing_spell",
                InstantHeal = 100,
                DamageMultiplier = 0 // Healing spell doesn't deal damage
            };

            Assert.Equal(100, skillDef.InstantHeal);
            Assert.Equal(0, skillDef.DamageMultiplier);
        }

        [Fact]
        public void SkillDef_MultipleBuffOperationsOnDifferentTriggers()
        {
            var skillDef = new SkillDef
            {
                Id = "complex_skill"
            };

            skillDef.OnCastBuffs.Add(new BuffOperation { Type = BuffOperationType.Apply, Target = BuffTarget.Self });
            skillDef.OnHitBuffs.Add(new BuffOperation { Type = BuffOperationType.Apply, Target = BuffTarget.Target });
            skillDef.OnCritBuffs.Add(new BuffOperation { Type = BuffOperationType.Apply, Target = BuffTarget.Target });

            Assert.Single(skillDef.OnCastBuffs);
            Assert.Single(skillDef.OnHitBuffs);
            Assert.Single(skillDef.OnCritBuffs);
        }

        #endregion
    }
}
