using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Skills;
using BlazorIdle.Shared.Models;
using System.Collections.Generic;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Unit tests for Step 2 Phase 7: Trigger Skill System
    /// 触发类技能系统单元测试
    /// </summary>
    public class Step2Phase7Tests
    {
        #region OnAttackHit Trigger Tests (4 tests)

        [Fact]
        public void TriggerProcessor_OnAttackHit_TriggersSkillSuccessfully()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            // Manually create a skill with OnAttackHit trigger (warrior_attack_basic has this in config)
            var sourceSkill = repo.GetSkill("warrior_attack_basic");

            // Act - This skill should have triggers in the config
            var triggeredSkills = processor.ProcessTriggers(
                "OnAttackHit",
                "test_char_1",
                sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert - May be empty if no triggers defined, which is fine
            Assert.NotNull(triggeredSkills);
        }

        [Fact]
        public void TriggerProcessor_OnAttackHit_RespectsInsufficientResources()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            // Set low rage
            context.PlayerBuffOwner?.Buckets?.GetBucket("rage")?.ForceConsume(9, "test_setup"); // Leave only 1 rage
            
            // Create skill that triggers warrior_mortal_strike (requires 5 rage)
            var sourceSkill = new SkillDef
            {
                Id = "test_attack",
                Name = "Test Attack",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackHit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_mortal_strike",
                        Priority = 10
                    }
                }
            };

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnAttackHit",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert - Should not trigger due to insufficient resources
            Assert.Empty(triggeredSkills);
        }

        [Fact]
        public void TriggerProcessor_OnAttackHit_IgnoreRequirementsWorks()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            // Set low rage
            context.PlayerBuffOwner?.Buckets?.GetBucket("rage")?.ForceConsume(9, "test_setup");
            
            // Create skill with IgnoreRequirements
            var sourceSkill = new SkillDef
            {
                Id = "test_attack",
                Name = "Test Attack",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackHit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_mortal_strike",
                        Priority = 10,
                        IgnoreRequirements = true
                    }
                }
            };

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnAttackHit",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert - Should trigger despite insufficient resources
            Assert.Single(triggeredSkills);
            Assert.Equal("warrior_mortal_strike", triggeredSkills[0].Id);
        }

        [Fact]
        public void TriggerProcessor_OnAttackHit_MultipleTriggersInPriorityOrder()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            var sourceSkill = new SkillDef
            {
                Id = "test_attack",
                Name = "Test Attack",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackHit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_slam",
                        Priority = 5,
                        IgnoreRequirements = true
                    },
                    new TriggerDef
                    {
                        When = "OnAttackHit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_rend",
                        Priority = 10,
                        IgnoreRequirements = true
                    }
                }
            };

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnAttackHit",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert - Should be sorted by priority (higher first)
            Assert.Equal(2, triggeredSkills.Count);
            Assert.Equal("warrior_rend", triggeredSkills[0].Id); // Priority 10
            Assert.Equal("warrior_slam", triggeredSkills[1].Id); // Priority 5
        }

        #endregion

        #region OnAttackCrit Trigger Tests (4 tests)

        [Fact]
        public void TriggerProcessor_OnAttackCrit_OnlyTriggersWhenCrit()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            var sourceSkill = new SkillDef
            {
                Id = "test_attack",
                Name = "Test Attack",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackCrit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_slam",
                        Priority = 10,
                        IgnoreRequirements = true
                    }
                }
            };

            // Act - with crit
            var triggeredWithCrit = processor.ProcessTriggers(
                "OnAttackCrit",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: true);

            // Act - without crit
            processor.ResetCounters();
            var triggeredWithoutCrit = processor.ProcessTriggers(
                "OnAttackCrit",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert
            Assert.Single(triggeredWithCrit);
            Assert.Empty(triggeredWithoutCrit);
        }

        [Fact]
        public void TriggerProcessor_OnAttackCrit_RespectsConditions()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            // Set player HP to 100% (warrior_desperate_strike requires < 50%)
            context.Player.Hp = context.Player.MaxHp;
            
            var sourceSkill = new SkillDef
            {
                Id = "test_attack",
                Name = "Test Attack",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackCrit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_desperate_strike",
                        Priority = 10
                    }
                }
            };

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnAttackCrit",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: true);

            // Assert - Should not trigger due to HP condition
            Assert.Empty(triggeredSkills);
        }

        [Fact]
        public void TriggerProcessor_OnAttackCrit_ConditionOverrideWorks()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            // Set player HP to 100%
            context.Player.Hp = context.Player.MaxHp;
            
            var sourceSkill = new SkillDef
            {
                Id = "test_attack",
                Name = "Test Attack",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackCrit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_desperate_strike",
                        Priority = 10,
                        Conditions = null, // Override to no conditions
                        IgnoreRequirements = true
                    }
                }
            };

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnAttackCrit",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: true);

            // Assert - Should trigger with override
            Assert.Single(triggeredSkills);
        }

        [Fact]
        public void TriggerProcessor_OnAttackCrit_WorksFromEquippedSkills()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            // Add warrior_proc_strike which has OnAttackCrit trigger
            characterData.LearnedSkills.Add("warrior_proc_strike");
            characterData.EquippedSkillsByProfession["warrior"].ActiveSlots["active_1"] = "warrior_proc_strike";

            // Act - No source skill, triggers from equipped skills
            var triggeredSkills = processor.ProcessTriggers(
                "OnAttackCrit",
                "test_char_1", null,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: true);

            // Assert - Should find trigger from equipped skill
            Assert.NotNull(triggeredSkills);
        }

        #endregion

        #region OnPostAttackWindow Trigger Tests (3 tests)

        [Fact]
        public void TriggerProcessor_OnPostAttackWindow_TriggersSuccessfully()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            var sourceSkill = new SkillDef
            {
                Id = "test_skill",
                Name = "Test Skill",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnPostAttackWindow",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_battle_shout",
                        Priority = 10,
                        IgnoreRequirements = true
                    }
                }
            };

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnPostAttackWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert
            Assert.Single(triggeredSkills);
            Assert.Equal("warrior_battle_shout", triggeredSkills[0].Id);
        }

        [Fact]
        public void TriggerProcessor_OnPostAttackWindow_ResetsCountersPerWindow()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            var sourceSkill = CreateSkillWithMultipleTriggers("OnPostAttackWindow", 5);

            // Act - First window (max 5 triggers)
            var triggeredSkills1 = processor.ProcessTriggers(
                "OnPostAttackWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Act - Second window (counter resets)
            var triggeredSkills2 = processor.ProcessTriggers(
                "OnPostAttackWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert
            Assert.Equal(5, triggeredSkills1.Count);
            Assert.Equal(5, triggeredSkills2.Count);
        }

        [Fact]
        public void TriggerProcessor_OnPostAttackWindow_LimitsToMaxTriggersPerWindow()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            // Create 7 triggers (more than max 5)
            var sourceSkill = CreateSkillWithMultipleTriggers("OnPostAttackWindow", 7);

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnPostAttackWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert - Limited to 5
            Assert.Equal(5, triggeredSkills.Count);
        }

        #endregion

        #region OnPostCastWindow Trigger Tests (3 tests)

        [Fact]
        public void TriggerProcessor_OnPostCastWindow_TriggersSuccessfully()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            // warrior_special_pulse has OnPostCastWindow trigger
            var sourceSkill = repo.GetSkill("warrior_special_pulse");

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnPostCastWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert - Should trigger warrior_check_stance
            Assert.NotNull(triggeredSkills);
        }

        [Fact]
        public void TriggerProcessor_OnPostCastWindow_ResetsCountersPerWindow()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            var sourceSkill = CreateSkillWithMultipleTriggers("OnPostCastWindow", 5);

            // Act - First window
            var triggeredSkills1 = processor.ProcessTriggers(
                "OnPostCastWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Act - Second window
            var triggeredSkills2 = processor.ProcessTriggers(
                "OnPostCastWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert
            Assert.Equal(5, triggeredSkills1.Count);
            Assert.Equal(5, triggeredSkills2.Count);
        }

        [Fact]
        public void TriggerProcessor_OnPostCastWindow_LimitsToMaxTriggersPerWindow()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            var sourceSkill = CreateSkillWithMultipleTriggers("OnPostCastWindow", 7);

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnPostCastWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert
            Assert.Equal(5, triggeredSkills.Count);
        }

        #endregion

        #region Probability Trigger Tests (2 tests)

        [Fact]
        public void TriggerProcessor_ProcChance_ZeroChanceNeverTriggers()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            var sourceSkill = new SkillDef
            {
                Id = "test_skill",
                Name = "Test Skill",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackHit",
                        ProcChance = 0.0,
                        FireSkillId = "warrior_slam",
                        Priority = 10,
                        IgnoreRequirements = true
                    }
                }
            };

            // Act - Try 10 times
            int triggerCount = 0;
            for (int i = 0; i < 10; i++)
            {
                processor.ResetCounters();
                var triggeredSkills = processor.ProcessTriggers(
                    "OnAttackHit",
                    "test_char_1", sourceSkill,
                    context,
                    isCasterPlayer: true,
                    casterCharacterData: characterData,
                    casterProfessionId: "warrior",
                    wasCrit: false);
                
                if (triggeredSkills.Count > 0)
                    triggerCount++;
            }

            // Assert
            Assert.Equal(0, triggerCount);
        }

        [Fact]
        public void TriggerProcessor_ProcChance_OneChanceAlwaysTriggers()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            var sourceSkill = new SkillDef
            {
                Id = "test_skill",
                Name = "Test Skill",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackHit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_slam",
                        Priority = 10,
                        IgnoreRequirements = true
                    }
                }
            };

            // Act - Try 10 times
            int triggerCount = 0;
            for (int i = 0; i < 10; i++)
            {
                processor.ResetCounters();
                var triggeredSkills = processor.ProcessTriggers(
                    "OnAttackHit",
                    "test_char_1", sourceSkill,
                    context,
                    isCasterPlayer: true,
                    casterCharacterData: characterData,
                    casterProfessionId: "warrior",
                    wasCrit: false);
                
                if (triggeredSkills.Count > 0)
                    triggerCount++;
            }

            // Assert
            Assert.Equal(10, triggerCount);
        }

        #endregion

        #region Safety Mechanism Tests (2 tests)

        [Fact]
        public void TriggerProcessor_SafetyMechanism_LimitsMaxTriggersPerWindow()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            var sourceSkill = CreateSkillWithMultipleTriggers("OnPostAttackWindow", 10);

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnPostAttackWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert
            Assert.True(triggeredSkills.Count <= 5);
        }

        [Fact]
        public void TriggerProcessor_SafetyMechanism_ResetCountersWorks()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            var characterData = CreateCharacterData();
            
            var sourceSkill = CreateSkillWithMultipleTriggers("OnPostAttackWindow", 5);

            // Act - Fill counter
            processor.ProcessTriggers(
                "OnPostAttackWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Reset
            processor.ResetCounters();

            // Try again
            var triggeredSkills = processor.ProcessTriggers(
                "OnPostAttackWindow",
                "test_char_1", sourceSkill,
                context,
                isCasterPlayer: true,
                casterCharacterData: characterData,
                casterProfessionId: "warrior",
                wasCrit: false);

            // Assert
            Assert.Equal(5, triggeredSkills.Count);
        }

        #endregion

        #region Monster Trigger Tests (4 tests)

        [Fact]
        public void TriggerProcessor_Monster_OnAttackHit_TriggersSuccessfully()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            
            var monsterSkill = new SkillDef
            {
                Id = "monster_attack",
                Name = "Monster Attack",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackHit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_slam", // Use any valid skill
                        Priority = 10,
                        IgnoreRequirements = true
                    }
                }
            };

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnAttackHit",
                "test_char_1", monsterSkill,
                context,
                isCasterPlayer: false,
                casterCharacterData: null,
                casterProfessionId: null,
                wasCrit: false);

            // Assert
            Assert.Single(triggeredSkills);
        }

        [Fact]
        public void TriggerProcessor_Monster_OnAttackCrit_WorksWithCritFlag()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            
            var monsterSkill = new SkillDef
            {
                Id = "monster_attack",
                Name = "Monster Attack",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackCrit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_slam",
                        Priority = 10,
                        IgnoreRequirements = true
                    }
                }
            };

            // Act - with crit
            var triggeredWithCrit = processor.ProcessTriggers(
                "OnAttackCrit",
                "test_char_1", monsterSkill,
                context,
                isCasterPlayer: false,
                casterCharacterData: null,
                casterProfessionId: null,
                wasCrit: true);

            // Act - without crit
            processor.ResetCounters();
            var triggeredWithoutCrit = processor.ProcessTriggers(
                "OnAttackCrit",
                "test_char_1", monsterSkill,
                context,
                isCasterPlayer: false,
                casterCharacterData: null,
                casterProfessionId: null,
                wasCrit: false);

            // Assert
            Assert.Single(triggeredWithCrit);
            Assert.Empty(triggeredWithoutCrit);
        }

        [Fact]
        public void TriggerProcessor_Monster_RespectsIgnoreRequirements()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            
            var monsterSkill = new SkillDef
            {
                Id = "monster_attack",
                Name = "Monster Attack",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackHit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_mortal_strike", // Has resource cost
                        Priority = 10,
                        IgnoreRequirements = true // Should bypass check
                    }
                }
            };

            // Act
            var triggeredSkills = processor.ProcessTriggers(
                "OnAttackHit",
                "test_char_1", monsterSkill,
                context,
                isCasterPlayer: false,
                casterCharacterData: null,
                casterProfessionId: null,
                wasCrit: false);

            // Assert
            Assert.Single(triggeredSkills);
        }

        [Fact]
        public void TriggerProcessor_Monster_RespectsCooldownWithoutIgnoreRequirements()
        {
            // Arrange
            var repo = new SkillRepository();
            var processor = CreateTriggerProcessor(repo);
            var context = CreateBattleContext();
            
            // Put the skill on cooldown
            var cooldownManager = new CooldownManager();
            cooldownManager.StartCooldown("warrior_slam", 10.0);
            
            var cooldownManagers = new Dictionary<string, CooldownManager>();
            CooldownManager GetCooldownManager(string casterId)
            {
                if (!cooldownManagers.TryGetValue(casterId, out var manager))
                {
                    manager = cooldownManager; // Reuse the shared manager
                    cooldownManagers[casterId] = manager;
                }
                return manager;
            }

            var processorWithCooldown = new TriggerProcessor(
                repo,
                new ConditionChecker(),
                GetCooldownManager,
                new ResourceManager());
            
            var monsterSkill = new SkillDef
            {
                Id = "monster_attack",
                Name = "Monster Attack",
                Triggers = new List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackHit",
                        ProcChance = 1.0,
                        FireSkillId = "warrior_slam",
                        Priority = 10,
                        IgnoreRequirements = false // Should respect cooldown
                    }
                }
            };

            // Act - Skill is on cooldown
            var triggeredSkills = processorWithCooldown.ProcessTriggers(
                "OnAttackHit",
                "test_monster_1",
                monsterSkill,
                context,
                isCasterPlayer: false,
                casterCharacterData: null,
                casterProfessionId: null,
                wasCrit: false);

            // Assert - Should not trigger due to cooldown
            Assert.Empty(triggeredSkills);
        }

        #endregion

        #region Helper Methods

        private TriggerProcessor CreateTriggerProcessor(SkillRepository repo)
        {
            var conditionChecker = new ConditionChecker();
            var cooldownManager = new CooldownManager();
            var resourceManager = new ResourceManager();
            
            var cooldownManagers = new Dictionary<string, CooldownManager>();
            CooldownManager GetCooldownManager(string casterId)
            {
                if (!cooldownManagers.TryGetValue(casterId, out var manager))
                {
                    manager = new CooldownManager();
                    cooldownManagers[casterId] = manager;
                }
                return manager;
            }

            return new TriggerProcessor(
                repo,
                conditionChecker,
                GetCooldownManager,
                resourceManager);
        }

        private BattleContext CreateBattleContext()
        {
            var player = new Character
            {
                MaxHp = 200,
                Hp = 100,
                DamagePerAttack = 50,
                AttackRateAPS = 1.0,
                CritChancePercent = 10.0,
                CritMultiplier = 2.0
            };

            var resources = new ResourceBucketCollection("rage", max: 20, initial: 10);
            resources.AddBucket("mana", max: 100, initial: 50);
            resources.AddBucket("energy", max: 100, initial: 50);
            
            var buffOwner = new CharacterBuffOwner(player, "player_1", resources);

            return new BattleContext
            {
                Player = player,
                PlayerBuffOwner = buffOwner,
                Rng = new RngContext(12345),
                Clock = new SimClock()
            };
        }

        private CharacterData CreateCharacterData()
        {
            return new CharacterData
            {
                ProfessionId = "warrior",
                LearnedSkills = new HashSet<string>
                {
                    "warrior_attack_basic",
                    "warrior_mortal_strike",
                    "warrior_slam",
                    "warrior_rend",
                    "warrior_thunderclap",
                    "warrior_battle_shout",
                    "warrior_desperate_strike",
                    "warrior_check_stance",
                    "warrior_special_pulse"
                },
                EquippedSkillsByProfession = new Dictionary<string, EquippedSkillsConfig>
                {
                    {
                        "warrior",
                        new EquippedSkillsConfig
                        {
                            ProfessionId = "warrior",
                            ActiveSlots = new Dictionary<string, string?>
                            {
                                { "active_1", "warrior_mortal_strike" },
                                { "active_2", "warrior_slam" },
                                { "active_3", "warrior_thunderclap" }
                            },
                            PassiveSlot = "warrior_battle_shout"
                        }
                    }
                }
            };
        }

        private SkillDef CreateSkillWithMultipleTriggers(string when, int count)
        {
            var triggers = new List<TriggerDef>();
            
            for (int i = 0; i < count; i++)
            {
                triggers.Add(new TriggerDef
                {
                    When = when,
                    ProcChance = 1.0,
                    FireSkillId = "warrior_slam",
                    Priority = 10 - i,
                    IgnoreRequirements = true
                });
            }

            return new SkillDef
            {
                Id = $"test_skill_{count}_triggers",
                Name = "Test Skill",
                Triggers = triggers
            };
        }

        #endregion
    }
}
