using Xunit;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Shared.Models;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Step 2 Phase 9: AutoCastEngine 核心框架测试
    /// Step 2 Phase 9: AutoCastEngine core framework tests
    /// </summary>
    public class Step2Phase9Tests
    {
        #region Helper Methods

        private AutoCastEngine CreateEngine(SkillRepository? repo = null)
        {
            repo ??= new SkillRepository();
            var conditionChecker = new ConditionChecker();
            var cooldownManager = new CooldownManager();
            var resourceManager = new ResourceManager();

            return new AutoCastEngine(repo, conditionChecker, cooldownManager, resourceManager);
        }

        private CharacterData CreateTestCharacter(string professionId = "warrior")
        {
            var character = new CharacterData
            {
                Id = "char1",
                ProfessionId = professionId,
                ActiveCombatProfessionId = professionId,
                Name = "Test Character",
                MaxHp = 100,
                DamagePerAttack = 50,
                AttackRateAPS = 1.0,
                HastePercent = 0,
                SpecialIntervalSec = 5.0,
                SpecialDamage = 100,
                CritChancePercent = 5.0,
                CritMultiplier = 2.0,
                VariancePct = 10.0,
                ReviveSec = 5.0
            };

            return character;
        }

        private BattleContext CreateTestContext(CharacterData character, ResourceBucketCollection? buckets = null)
        {
            var player = new Character
            {
                MaxHp = character.MaxHp,
                Hp = character.MaxHp,
                AttackRateAPS = character.AttackRateAPS,
                DamagePerAttack = character.DamagePerAttack,
                HastePercent = character.HastePercent,
                SpecialIntervalSec = character.SpecialIntervalSec,
                SpecialDamage = character.SpecialDamage,
                CritChancePercent = character.CritChancePercent,
                CritMultiplier = character.CritMultiplier,
                VariancePct = character.VariancePct,
                ReviveMs = (int)(character.ReviveSec * 1000)
            };
            buckets ??= new ResourceBucketCollection();

            var buffOwner = new CharacterBuffOwner(player, character.Id, buckets);

            return new BattleContext
            {
                Player = player,
                PlayerBuffOwner = buffOwner,
                Rng = new RngContext(12345),
                Clock = new SimClock(),
                CurrentTargetId = "enemy1"
            };
        }

        /// <summary>
        /// Phase 9: Helper method to simulate old Tick behavior using new methods
        /// Tests can use this to check both cast and instant skills
        /// </summary>
        private string? SimulateTick(AutoCastEngine engine, CharacterData character, string professionId, BattleContext context)
        {
            // Try cast skills first
            var castSkill = engine.SelectCastSkill(character, professionId, context);
            if (castSkill != null)
                return castSkill.Id;

            // Try instant skills
            var instantSkills = engine.ExecuteWindow(character, professionId, context, gcdAlreadyUsed: false, "Test");
            if (instantSkills.Count > 0)
                return instantSkills[0].Id;

            return null;
        }

        #endregion

        #region Skill Selection Tests (5 tests)

        [Fact]
        public void AutoCastEngine_SelectSkill_ReturnsNullWhenNoSkillsEquipped()
        {
            // Arrange
            var engine = CreateEngine();
            var character = CreateTestCharacter();
            var context = CreateTestContext(character);

            // Act
            var result = SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void AutoCastEngine_SelectSkill_SelectsFirstAvailableSkill()
        {
            // Arrange
            var repo = new SkillRepository();
            var engine = CreateEngine(repo);
            var character = CreateTestCharacter();

            // Equip a skill
            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" }
                }
            };

            var buckets = new ResourceBucketCollection("rage", max: 100, initial: 10);
            var context = CreateTestContext(character, buckets);

            // Act
            var result = SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Equal("warrior_mortal_strike", result);
        }

        [Fact]
        public void AutoCastEngine_SelectSkill_SkipsSkillOnCooldown()
        {
            // Arrange
            var repo = new SkillRepository();
            var cooldownManager = new CooldownManager();
            var engine = new AutoCastEngine(repo, new ConditionChecker(), cooldownManager, new ResourceManager());
            var character = CreateTestCharacter();

            // Equip two skills
            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" },
                    { "active_2", "warrior_rend" }
                }
            };

            var buckets1 = new ResourceBucketCollection("rage", max: 100, initial: 20);
            var context = CreateTestContext(character, buckets1);

            // Put first skill on cooldown
            cooldownManager.StartCooldown("warrior_mortal_strike", 5.0);

            // Act
            var result = SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Equal("warrior_rend", result); // Should select second skill
        }

        [Fact]
        public void AutoCastEngine_SelectSkill_SkipsSkillWithInsufficientResources()
        {
            // Arrange
            var repo = new SkillRepository();
            var engine = CreateEngine(repo);
            var character = CreateTestCharacter();

            // Equip two skills
            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" }, // Costs 3 rage
                    { "active_2", "warrior_slam" } // Costs 0 rage, generates 2 rage
                }
            };

            // Setup resources (only 2 rage, not enough for first skill)
            var buckets2 = new ResourceBucketCollection("rage", max: 100, initial: 2);
            var context = CreateTestContext(character, buckets2);

            // Act
            var result = SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Equal("warrior_slam", result); // Should select second skill (first doesn't have enough rage)
        }

        [Fact]
        public void AutoCastEngine_SelectSkill_ReturnsNullWhenAllSkillsUnavailable()
        {
            // Arrange
            var repo = new SkillRepository();
            var cooldownManager = new CooldownManager();
            var engine = new AutoCastEngine(repo, new ConditionChecker(), cooldownManager, new ResourceManager());
            var character = CreateTestCharacter();

            // Equip a skill
            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" }
                }
            };

            // No resources
            var buckets3 = new ResourceBucketCollection("rage", max: 100, initial: 0);
            var context = CreateTestContext(character, buckets3);

            // Act
            var result = SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Execution Flow Tests (5 tests)

        [Fact]
        public void AutoCastEngine_SelectSkill_HandlesMultipleSkillsCorrectly()
        {
            // Arrange
            var repo = new SkillRepository();
            var engine = CreateEngine(repo);
            var character = CreateTestCharacter();

            // Equip three skills
            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" },
                    { "active_2", "warrior_rend" },
                    { "active_3", "warrior_slam" }
                }
            };

            // Setup resources
            var buckets4 = new ResourceBucketCollection("rage", max: 100, initial: 20);
            var context = CreateTestContext(character, buckets4);

            // Act
            var result = SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Equal("warrior_mortal_strike", result); // Should select first available
        }

        [Fact]
        public void AutoCastEngine_SelectSkill_RespectsSlotOrder()
        {
            // Arrange
            var repo = new SkillRepository();
            var engine = CreateEngine(repo);
            var character = CreateTestCharacter();

            // Equip skills in specific order
            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_slam" },     // Priority 1
                    { "active_2", "warrior_mortal_strike" }, // Priority 2
                    { "active_3", "warrior_rend" }      // Priority 3
                }
            };

            // Setup resources (enough for all)
            var buckets5 = new ResourceBucketCollection("rage", max: 100, initial: 20);
            var context = CreateTestContext(character, buckets5);

            // Act
            var result = SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Equal("warrior_slam", result); // Should select slot 1 first
        }

        [Fact]
        public void AutoCastEngine_SelectSkill_IncludesPassiveSkills()
        {
            // Arrange
            var repo = new SkillRepository();
            var engine = CreateEngine(repo);
            var character = CreateTestCharacter();

            // Equip passive skill only
            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                PassiveSlot = "warrior_special_pulse"
            };

            var context = CreateTestContext(character);

            // Act
            var result = SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Equal("warrior_special_pulse", result);
        }

        [Fact]
        public void AutoCastEngine_SelectSkill_HandlesNullContextGracefully()
        {
            // Arrange
            var engine = CreateEngine();
            var character = CreateTestCharacter();

            // Act
            var result = SimulateTick(engine, character, "warrior", null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void AutoCastEngine_SelectSkill_HandlesNullCharacterGracefully()
        {
            // Arrange
            var engine = CreateEngine();
            var context = CreateTestContext(CreateTestCharacter());

            // Act
            var result = SimulateTick(engine, null!, "warrior", context);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Event Recording Tests (3 tests)

        [Fact]
        public void AutoCastEngine_FiresSkillSelectionEvent()
        {
            // Arrange
            var repo = new SkillRepository();
            var engine = CreateEngine(repo);
            var character = CreateTestCharacter();

            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" }
                }
            };

            var buckets6 = new ResourceBucketCollection("rage", max: 100, initial: 10);
            var context = CreateTestContext(character, buckets6);

            SkillSelectionEvent? capturedEvent = null;
            engine.SkillSelectionDecision += (e) => capturedEvent = e;

            // Act
            SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.NotNull(capturedEvent);
            Assert.Equal("warrior_mortal_strike", capturedEvent!.SkillId);
            // Phase 9: Updated reason format (was "Available", now window-specific like "Test-GCD")
            Assert.Contains("-", capturedEvent.Reason); // Should contain window name format
        }

        [Fact]
        public void AutoCastEngine_FiresSkillCastAttemptEvent()
        {
            // Arrange
            var repo = new SkillRepository();
            var engine = CreateEngine(repo);
            var character = CreateTestCharacter();

            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" }
                }
            };

            var buckets7 = new ResourceBucketCollection("rage", max: 100, initial: 10);
            var context = CreateTestContext(character, buckets7);

            SkillCastAttemptEvent? capturedEvent = null;
            engine.SkillCastAttempt += (e) => capturedEvent = e;

            // Act
            SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.NotNull(capturedEvent);
            Assert.Equal("warrior_mortal_strike", capturedEvent!.SkillId);
        }

        [Fact]
        public void AutoCastEngine_FiresSkillFailureEvent()
        {
            // Arrange
            var repo = new SkillRepository();
            var cooldownManager = new CooldownManager();
            var engine = new AutoCastEngine(repo, new ConditionChecker(), cooldownManager, new ResourceManager());
            var character = CreateTestCharacter();

            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" }
                }
            };

            var buckets8 = new ResourceBucketCollection("rage", max: 100, initial: 10);
            var context = CreateTestContext(character, buckets8);

            // Put skill on cooldown
            cooldownManager.StartCooldown("warrior_mortal_strike", 5.0);

            var failureEvents = new List<SkillFailureEvent>();
            engine.SkillFailure += (e) => failureEvents.Add(e);

            // Act
            SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Single(failureEvents);
            Assert.Equal("warrior_mortal_strike", failureEvents[0].SkillId);
            Assert.Equal("Cooldown", failureEvents[0].Reason);
        }

        #endregion

        #region Integration Tests (2 tests)

        [Fact]
        public void AutoCastEngine_IntegrationTest_CompleteSkillSelectionFlow()
        {
            // Arrange
            var repo = new SkillRepository();
            var cooldownManager = new CooldownManager();
            var engine = new AutoCastEngine(repo, new ConditionChecker(), cooldownManager, new ResourceManager());
            var character = CreateTestCharacter();

            // Equip three skills
            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" }, // 3 rage
                    { "active_2", "warrior_rend" },          // 1 rage
                    { "active_3", "warrior_slam" }           // 0 rage
                }
            };

            var buckets9 = new ResourceBucketCollection("rage", max: 100, initial: 0); // Only enough for slam (no resources needed)
            var context = CreateTestContext(character, buckets9);

            // Track events
            var selections = new List<SkillSelectionEvent>();
            var attempts = new List<SkillCastAttemptEvent>();
            var failures = new List<SkillFailureEvent>();

            engine.SkillSelectionDecision += (e) => selections.Add(e);
            engine.SkillCastAttempt += (e) => attempts.Add(e);
            engine.SkillFailure += (e) => failures.Add(e);

            // Act
            var result = SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Equal("warrior_slam", result);
            Assert.Single(selections);
            Assert.Single(attempts);
            Assert.Equal(2, failures.Count); // Two skills failed due to insufficient resources
            Assert.Equal("warrior_mortal_strike", failures[0].SkillId);
            Assert.Equal("Resource", failures[0].Reason);
            Assert.Equal("warrior_rend", failures[1].SkillId);
            Assert.Equal("Resource", failures[1].Reason);
        }

        [Fact]
        public void AutoCastEngine_IntegrationTest_AllSkillsOnCooldown()
        {
            // Arrange
            var repo = new SkillRepository();
            var cooldownManager = new CooldownManager();
            var engine = new AutoCastEngine(repo, new ConditionChecker(), cooldownManager, new ResourceManager());
            var character = CreateTestCharacter();

            character.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" },
                    { "active_2", "warrior_rend" }
                }
            };

            var buckets10 = new ResourceBucketCollection("rage", max: 100, initial: 20);
            var context = CreateTestContext(character, buckets10);

            // Put all skills on cooldown
            cooldownManager.StartCooldown("warrior_mortal_strike", 5.0);
            cooldownManager.StartCooldown("warrior_rend", 5.0);

            var failures = new List<SkillFailureEvent>();
            engine.SkillFailure += (e) => failures.Add(e);

            // Act
            var result = SimulateTick(engine, character, "warrior", context);

            // Assert
            Assert.Null(result); // No skills available
            Assert.Equal(2, failures.Count);
            Assert.All(failures, f => Assert.Equal("Cooldown", f.Reason));
        }

        #endregion
    }
}
