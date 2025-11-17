using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Skills;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Unit tests for Step 2 Phase 4: Skill Condition System
    /// 技能条件判定系统单元测试
    /// </summary>
    public class Step2Phase4Tests
    {
        #region HP Condition Tests (6 tests)

        [Fact]
        public void ConditionChecker_HpBelowPct_PassesWhenBelowThreshold()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 60 }; // 30% HP
            var buffOwner = CreateBuffOwner(player);
            
            var skill = new SkillDef
            {
                Id = "test_skill",
                Name = "Test Skill",
                Conditions = new SkillConditions
                {
                    HpBelowPct = 50.0 // HP must be below 50%
                }
            };
            
            var context = new BattleContext
            {
                Player = player,
                PlayerBuffOwner = buffOwner,
                Rng = new RngContext(12345),
                Clock = new SimClock()
            };

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.True(result); // 30% < 50%, should pass
        }

        [Fact]
        public void ConditionChecker_HpBelowPct_FailsWhenAboveThreshold()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 120 }; // 60% HP
            var buffOwner = CreateBuffOwner(player);
            
            var skill = new SkillDef
            {
                Id = "test_skill",
                Name = "Test Skill",
                Conditions = new SkillConditions
                {
                    HpBelowPct = 50.0 // HP must be below 50%
                }
            };
            
            var context = new BattleContext
            {
                Player = player,
                PlayerBuffOwner = buffOwner,
                Rng = new RngContext(12345),
                Clock = new SimClock()
            };

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.False(result); // 60% >= 50%, should fail
        }

        [Fact]
        public void ConditionChecker_HpAbovePct_PassesWhenAboveThreshold()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 160 }; // 80% HP
            var buffOwner = CreateBuffOwner(player);
            var skill = CreateSkillWithConditions(new SkillConditions { HpAbovePct = 50.0 });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.True(result); // 80% > 50%, should pass
        }

        [Fact]
        public void ConditionChecker_HpAbovePct_FailsWhenBelowThreshold()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 60 }; // 30% HP
            var buffOwner = CreateBuffOwner(player);
            var skill = CreateSkillWithConditions(new SkillConditions { HpAbovePct = 50.0 });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.False(result); // 30% <= 50%, should fail
        }

        [Fact]
        public void ConditionChecker_HpBothConditions_PassesWhenInRange()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 120 }; // 60% HP
            var buffOwner = CreateBuffOwner(player);
            var skill = CreateSkillWithConditions(new SkillConditions 
            {
                HpAbovePct = 50.0,  // HP must be above 50%
                HpBelowPct = 80.0   // HP must be below 80%
            });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.True(result); // 50% < 60% < 80%, should pass
        }

        [Fact]
        public void ConditionChecker_HpBothConditions_FailsWhenOutOfRange()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 180 }; // 90% HP
            var buffOwner = CreateBuffOwner(player);
            var skill = CreateSkillWithConditions(new SkillConditions 
            {
                HpAbovePct = 50.0,  // HP must be above 50%
                HpBelowPct = 80.0   // HP must be below 80%
            });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.False(result); // 90% >= 80%, should fail
        }

        #endregion

        #region Buff Condition Tests (6 tests)

        [Fact]
        public void ConditionChecker_RequireBuffId_PassesWhenBuffExists()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwner(player);
            
            // Apply required buff
            var buff = CreateTestBuff("test_buff", buffOwner);
            buffOwner.ApplyBuff(buff);
            
            var skill = CreateSkillWithConditions(new SkillConditions { RequireBuffId = "test_buff" });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ConditionChecker_RequireBuffId_FailsWhenBuffNotExists()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwner(player);
            var skill = CreateSkillWithConditions(new SkillConditions { RequireBuffId = "test_buff" });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.False(result); // Buff doesn't exist, should fail
        }

        [Fact]
        public void ConditionChecker_ForbidBuffId_PassesWhenBuffNotExists()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwner(player);
            var skill = CreateSkillWithConditions(new SkillConditions { ForbidBuffId = "forbidden_buff" });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.True(result); // Forbidden buff doesn't exist, should pass
        }

        [Fact]
        public void ConditionChecker_ForbidBuffId_FailsWhenBuffExists()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwner(player);
            
            // Apply forbidden buff
            var buff = CreateTestBuff("forbidden_buff", buffOwner);
            buffOwner.ApplyBuff(buff);
            
            var skill = CreateSkillWithConditions(new SkillConditions { ForbidBuffId = "forbidden_buff" });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.False(result); // Forbidden buff exists, should fail
        }

        [Fact]
        public void ConditionChecker_RequireAndForbidBuff_PassesWhenConditionsMet()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwner(player);
            
            // Apply required buff but not forbidden buff
            var buff = CreateTestBuff("required_buff", buffOwner);
            buffOwner.ApplyBuff(buff);
            
            var skill = CreateSkillWithConditions(new SkillConditions 
            { 
                RequireBuffId = "required_buff",
                ForbidBuffId = "forbidden_buff"
            });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.True(result); // Has required, doesn't have forbidden, should pass
        }

        [Fact]
        public void ConditionChecker_RequireAndForbidBuff_FailsWhenBothExist()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwner(player);
            
            // Apply both buffs
            buffOwner.ApplyBuff(CreateTestBuff("required_buff", buffOwner));
            buffOwner.ApplyBuff(CreateTestBuff("forbidden_buff", buffOwner));
            
            var skill = CreateSkillWithConditions(new SkillConditions 
            { 
                RequireBuffId = "required_buff",
                ForbidBuffId = "forbidden_buff"
            });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.False(result); // Has forbidden buff, should fail
        }

        #endregion

        #region Resource Condition Tests (4 tests)

        [Fact]
        public void ConditionChecker_RequireResource_PassesWhenResourceSufficient()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwnerWithResources(player);
            
            // Set rage to 5
            buffOwner.Buckets!.GetBucket("rage")!.Gain(5, "test");
            
            var skill = CreateSkillWithConditions(new SkillConditions
            {
                RequireResource = new Dictionary<string, int> { { "rage", 3 } }
            });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.True(result); // 5 >= 3, should pass
        }

        [Fact]
        public void ConditionChecker_RequireResource_FailsWhenResourceInsufficient()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwnerWithResources(player);
            
            // Set rage to 2
            buffOwner.Buckets!.GetBucket("rage")!.Gain(2, "test");
            
            var skill = CreateSkillWithConditions(new SkillConditions
            {
                RequireResource = new Dictionary<string, int> { { "rage", 5 } }
            });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.False(result); // 2 < 5, should fail
        }

        [Fact]
        public void ConditionChecker_RequireMultipleResources_PassesWhenAllSufficient()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwnerWithResources(player);
            
            // Set resources
            buffOwner.Buckets!.GetBucket("rage")!.Gain(5, "test");
            buffOwner.Buckets!.GetBucket("mana")!.Gain(8, "test");
            
            var skill = CreateSkillWithConditions(new SkillConditions
            {
                RequireResource = new Dictionary<string, int>
                {
                    { "rage", 3 },
                    { "mana", 5 }
                }
            });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.True(result); // Both resources sufficient, should pass
        }

        [Fact]
        public void ConditionChecker_RequireMultipleResources_FailsWhenOnInsufficient()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwnerWithResources(player);
            
            // Set resources - rage sufficient, mana insufficient
            buffOwner.Buckets!.GetBucket("rage")!.Gain(5, "test");
            buffOwner.Buckets!.GetBucket("mana")!.Gain(2, "test");
            
            var skill = CreateSkillWithConditions(new SkillConditions
            {
                RequireResource = new Dictionary<string, int>
                {
                    { "rage", 3 },
                    { "mana", 5 }
                }
            });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.False(result); // Mana insufficient, should fail
        }

        #endregion

        #region Combined Condition Tests (2 tests)

        [Fact]
        public void ConditionChecker_CombinedConditions_PassesWhenAllMet()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 80 }; // 40% HP
            var buffOwner = CreateBuffOwnerWithResources(player);
            
            // Apply required buff
            buffOwner.ApplyBuff(CreateTestBuff("enrage", buffOwner));
            
            // Set rage to 5
            buffOwner.Buckets!.GetBucket("rage")!.Gain(5, "test");
            
            var skill = CreateSkillWithConditions(new SkillConditions
            {
                HpBelowPct = 50.0,  // HP < 50%
                RequireBuffId = "enrage",
                RequireResource = new Dictionary<string, int> { { "rage", 3 } }
            });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.True(result); // All conditions met, should pass
        }

        [Fact]
        public void ConditionChecker_CombinedConditions_FailsWhenOneNotMet()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 80 }; // 40% HP
            var buffOwner = CreateBuffOwnerWithResources(player);
            
            // Apply required buff - meets condition
            buffOwner.ApplyBuff(CreateTestBuff("enrage", buffOwner));
            
            // Set rage to 2 - DOES NOT meet condition
            buffOwner.Buckets!.GetBucket("rage")!.Gain(2, "test");
            
            var skill = CreateSkillWithConditions(new SkillConditions
            {
                HpBelowPct = 50.0,  // HP < 50%
                RequireBuffId = "enrage",
                RequireResource = new Dictionary<string, int> { { "rage", 5 } }
            });
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.False(result); // Resource condition not met, should fail
        }

        #endregion

        #region No Conditions Test

        [Fact]
        public void ConditionChecker_NoConditions_AlwaysPasses()
        {
            // Arrange
            var checker = new ConditionChecker();
            var player = new Character { MaxHp = 200, Hp = 200 };
            var buffOwner = CreateBuffOwner(player);
            var skill = new SkillDef { Id = "test", Name = "Test" }; // No conditions
            var context = CreateContext(player, buffOwner);

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: true);

            // Assert
            Assert.True(result); // No conditions, should always pass
        }

        #endregion

        #region Monster/Enemy Condition Tests (2 tests)

        [Fact]
        public void ConditionChecker_Monster_HpCondition_WorksCorrectly()
        {
            // Arrange
            var checker = new ConditionChecker();
            var enemy = new Enemy { MaxHp = 300, Hp = 90, MonsterId = "enemy_1" }; // 30% HP
            var enemyBuffOwner = CreateEnemyBuffOwner(enemy);
            
            var skill = CreateSkillWithConditions(new SkillConditions { HpBelowPct = 50.0 });
            
            var context = new BattleContext
            {
                Enemy = enemy,
                EnemyBuffOwners = new Dictionary<string, EnemyBuffOwner>
                {
                    { "enemy_1", enemyBuffOwner }
                },
                Rng = new RngContext(12345),
                Clock = new SimClock()
            };

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: false);

            // Assert
            Assert.True(result); // 30% < 50%, should pass
        }

        [Fact]
        public void ConditionChecker_Monster_BuffCondition_WorksCorrectly()
        {
            // Arrange
            var checker = new ConditionChecker();
            var enemy = new Enemy { MaxHp = 300, Hp = 300, MonsterId = "enemy_1" };
            var enemyBuffOwner = CreateEnemyBuffOwner(enemy);
            
            // Apply buff to enemy
            var buff = CreateTestBuff("monster_enrage", enemyBuffOwner);
            enemyBuffOwner.ApplyBuff(buff);
            
            var skill = CreateSkillWithConditions(new SkillConditions { RequireBuffId = "monster_enrage" });
            
            var context = new BattleContext
            {
                Enemy = enemy,
                EnemyBuffOwners = new Dictionary<string, EnemyBuffOwner>
                {
                    { "enemy_1", enemyBuffOwner }
                },
                Rng = new RngContext(12345),
                Clock = new SimClock()
            };

            // Act
            var result = checker.CheckConditions(skill, context, isCasterPlayer: false);

            // Assert
            Assert.True(result); // Has required buff, should pass
        }

        #endregion

        #region Helper Methods

        private BattleContext CreateContext(Character player, CharacterBuffOwner buffOwner)
        {
            return new BattleContext
            {
                Player = player,
                PlayerBuffOwner = buffOwner,
                Rng = new RngContext(12345),
                Clock = new SimClock()
            };
        }

        private SkillDef CreateSkillWithConditions(SkillConditions conditions)
        {
            return new SkillDef
            {
                Id = "test_skill",
                Name = "Test Skill",
                Type = "active",
                SlotType = "active",
                Conditions = conditions
            };
        }

        private CharacterBuffOwner CreateBuffOwner(Character character)
        {
            return new CharacterBuffOwner(character, "test_char_id");
        }

        private CharacterBuffOwner CreateBuffOwnerWithResources(Character character)
        {
            var resources = new ResourceBucketCollection();
            resources.AddBucket("mana", max: 10);
            resources.AddBucket("energy", max: 10);
            
            return new CharacterBuffOwner(character, "test_char_id", resources);
        }

        private EnemyBuffOwner CreateEnemyBuffOwner(Enemy enemy)
        {
            return new EnemyBuffOwner(enemy, enemy.MonsterId!);
        }

        private BuffInstance CreateTestBuff(string buffId, IBuffOwner owner)
        {
            return new BuffInstance(
                id: buffId,
                ownerId: owner.Id,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>()
            );
        }

        #endregion
    }
}
