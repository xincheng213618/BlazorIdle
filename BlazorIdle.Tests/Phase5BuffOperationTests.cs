using Xunit;
using BlazorIdle.Game;
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
        #region SkillRepository Tests

        [Fact]
        public void SkillRepository_InitializesWithDefaultSkills()
        {
            var repo = new SkillRepository();

            var attackBasic = repo.GetSkill(SkillIds.AttackBasic);
            var specialPulse = repo.GetSkill(SkillIds.SpecialPulse);
            var enemyAttack = repo.GetSkill(SkillIds.EnemyAttackBasic);

            Assert.NotNull(attackBasic);
            Assert.NotNull(specialPulse);
            Assert.NotNull(enemyAttack);
        }

        [Fact]
        public void SkillRepository_CanRegisterCustomSkill()
        {
            var repo = new SkillRepository();
            var customSkill = new SkillDef
            {
                Id = "custom_skill",
                DamageMultiplier = 2.0
            };

            repo.RegisterSkill(customSkill);

            var retrieved = repo.GetSkill("custom_skill");
            Assert.NotNull(retrieved);
            Assert.Equal(2.0, retrieved.DamageMultiplier);
        }

        #endregion

        #region SkillResolver Integration Tests

        [Fact]
        public void SkillResolver_ReturnsBuffOperationsFromSkillDef()
        {
            var repo = new SkillRepository();
            var testSkill = new SkillDef
            {
                Id = "test_skill"
            };
            testSkill.OnCastBuffs.Add(new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.Self
            });
            repo.RegisterSkill(testSkill);

            var resolver = new SkillResolver(null, repo);
            var ctx = CreateTestContext();
            var result = resolver.Cast("test_skill", ctx);

            Assert.Single(result.BuffOperations);
            Assert.Equal(BuffOperationType.Apply, result.BuffOperations[0].Type);
        }

        [Fact]
        public void SkillResolver_AppliesDamageMultiplier()
        {
            var repo = new SkillRepository();
            var testSkill = new SkillDef
            {
                Id = SkillIds.AttackBasic,
                // 新系统使用 Damage.CoefAtk 代替 DamageMultiplier
                // New system uses Damage.CoefAtk instead of DamageMultiplier
                Damage = new DamageDef { CoefAtk = 2.0 }
            };
            repo.RegisterSkill(testSkill);

            var resolver = new SkillResolver(null, repo);
            var ctx = CreateTestContext();
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // 新系统: AttackFinal=10, 技能系数 2.0
            // New system: AttackFinal=10, skill coefficient 2.0
            // 预期伤害 ~20 (10 * 2.0 * variance)
            Assert.True(result.DamageDealt >= 15, $"Expected damage >= 15, got {result.DamageDealt}");
        }

        [Fact]
        public void SkillResolver_ReturnsInstantHeal()
        {
            var repo = new SkillRepository();
            var healSkill = new SkillDef
            {
                Id = "heal_spell",
                InstantHeal = 50,
                DamageMultiplier = 0
            };
            repo.RegisterSkill(healSkill);

            var resolver = new SkillResolver(null, repo);
            var ctx = CreateTestContext();
            var result = resolver.Cast("heal_spell", ctx);

            Assert.Equal(50, result.InstantHeal);
        }

        [Fact]
        public void SkillResolver_OnCritBuffsAppliedOnlyOnCrit()
        {
            var repo = new SkillRepository();
            var critSkill = new SkillDef
            {
                Id = "crit_skill"
            };
            critSkill.OnCritBuffs.Add(new BuffOperation
            {
                Type = BuffOperationType.Apply,
                Target = BuffTarget.Target
            });
            repo.RegisterSkill(critSkill);

            var resolver = new SkillResolver(null, repo);
            
            // Force crit - 创建新的上下文
            var ctxCrit = CreateTestContext();
            var opts = new SkillCastOptions { ForceCrit = true };
            var resultCrit = resolver.Cast("crit_skill", ctxCrit, opts);
            Assert.Single(resultCrit.BuffOperations);

            // No crit - 创建新的上下文，使用不同的 CombatStats
            var noCritStats = new Game.Combat.CombatStats
            {
                AttackFinal = 10,
                CritChancePercent = 0, // 确保不暴击
                CritDamageBonusPercent = 66.67
            };
            var playerNoCrit = new Character 
            { 
                CombatStats = noCritStats,
                CritChancePercent = 0,
                VariancePct = 0
            };
            var ctxNoCrit = new BattleContext
            {
                Player = playerNoCrit,
                Enemy = new Enemy { BaseAttack = 5, Element = Game.Combat.ElementIds.Neutral },
                Rng = new RngContext(12345),
                Clock = new TestGameClock(),
                DamageCalculator = Game.Combat.DamageCalculator.CreateDefault(),
                AttackerCombatStats = noCritStats,
                AttackerElement = Game.Combat.ElementIds.Neutral,
                DefenderElement = Game.Combat.ElementIds.Neutral,
                AttackerHPRatio = 1.0,
                DefenderDamageReductionPercent = 0
            };
            var resultNoCrit = resolver.Cast("crit_skill", ctxNoCrit);
            Assert.Empty(resultNoCrit.BuffOperations);
        }

        private BattleContext CreateTestContext()
        {
            // 更新使用新伤害系统 / Updated to use new damage system
            var combatStats = new Game.Combat.CombatStats
            {
                AttackFinal = 10,
                CritChancePercent = 0,
                CritDamageBonusPercent = 66.67 // ~2.0x crit
            };
            
            var player = new Character 
            { 
                CombatStats = combatStats,
                CritChancePercent = 0,
                VariancePct = 0
            };
            
            var enemy = new Enemy
            {
                BaseAttack = 5,
                Element = Game.Combat.ElementIds.Neutral
            };
            
            return new BattleContext
            {
                Player = player,
                Enemy = enemy,
                Rng = new RngContext(12345),
                Clock = new TestGameClock(),
                DamageCalculator = Game.Combat.DamageCalculator.CreateDefault(),
                AttackerCombatStats = combatStats,
                AttackerElement = Game.Combat.ElementIds.Neutral,
                DefenderElement = Game.Combat.ElementIds.Neutral,
                AttackerHPRatio = 1.0,
                DefenderDamageReductionPercent = 0
            };
        }

        [Fact]
        public void SkillResolver_ReturnsResourceCosts()
        {
            var repo = new SkillRepository();
            var skillWithCost = new SkillDef
            {
                Id = "execute",
                DamageMultiplier = 2.0,
                ResourceCosts = new Dictionary<string, int> { { "rage", 3 } }
            };
            repo.RegisterSkill(skillWithCost);

            var resolver = new SkillResolver(null, repo);
            var ctx = CreateTestContext();
            var result = resolver.Cast("execute", ctx);

            Assert.True(result.ResourceChanges.ContainsKey("rage"));
            Assert.Equal(-3, result.ResourceChanges["rage"]); // Negative means cost
        }

        [Fact]
        public void SkillResolver_ReturnsResourceGains()
        {
            var repo = new SkillRepository();
            var skillWithGain = new SkillDef
            {
                Id = "charge",
                ResourceGains = new Dictionary<string, int> { { "rage", 5 } }
            };
            repo.RegisterSkill(skillWithGain);

            var resolver = new SkillResolver(null, repo);
            var ctx = CreateTestContext();
            var result = resolver.Cast("charge", ctx);

            Assert.True(result.ResourceChanges.ContainsKey("rage"));
            Assert.Equal(5, result.ResourceChanges["rage"]);
        }

        [Fact]
        public void SkillResolver_CombinesResourceCostsAndGains()
        {
            var repo = new SkillRepository();
            var skill = new SkillDef
            {
                Id = "rampage",
                ResourceCosts = new Dictionary<string, int> { { "rage", 10 } },
                ResourceGains = new Dictionary<string, int> { { "rage", 3 } } // Refunds some
            };
            repo.RegisterSkill(skill);

            var resolver = new SkillResolver(null, repo);
            var ctx = CreateTestContext();
            var result = resolver.Cast("rampage", ctx);

            Assert.True(result.ResourceChanges.ContainsKey("rage"));
            Assert.Equal(-7, result.ResourceChanges["rage"]); // Net cost: -10 + 3 = -7
        }

        [Fact]
        public void SkillResolver_MultipleBuffOperations()
        {
            var repo = new SkillRepository();
            var complexSkill = new SkillDef
            {
                Id = "complex_skill"
            };
            complexSkill.OnCastBuffs.Add(new BuffOperation { Type = BuffOperationType.Apply, Target = BuffTarget.Self });
            complexSkill.OnHitBuffs.Add(new BuffOperation { Type = BuffOperationType.Apply, Target = BuffTarget.Target });
            complexSkill.OnHitBuffs.Add(new BuffOperation { Type = BuffOperationType.Remove, Target = BuffTarget.Target, BuffIdToRemove = "old_buff" });
            repo.RegisterSkill(complexSkill);

            var resolver = new SkillResolver(null, repo);
            var ctx = CreateTestContext();
            var result = resolver.Cast("complex_skill", ctx);

            // OnCast: 1, OnHit: 2 = 3 total
            Assert.Equal(3, result.BuffOperations.Count);
        }

        private class TestGameClock : IGameClock
        {
            public int NowMs => 0;
            public void Reset() { }
            public void AdvanceBy(int ms) { }
        }

        #endregion
    }
}
