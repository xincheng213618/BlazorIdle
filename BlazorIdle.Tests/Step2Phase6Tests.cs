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
    /// Step 2 Phase 6: Window-GCD 机制测试
    /// Step 2 Phase 6: Window-GCD mechanism tests
    /// </summary>
    public class Step2Phase6Tests
    {
        #region Helper Methods

        private WindowExecutor CreateExecutor(SkillRepository? repo = null)
        {
            repo ??= new SkillRepository();
            var conditionChecker = new ConditionChecker();
            var cooldownManager = new CooldownManager();
            var resourceManager = new ResourceManager();

            return new WindowExecutor(repo, conditionChecker, cooldownManager, resourceManager);
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
                PlayerResources = buckets,
                Rng = new RngContext(12345),
                Clock = new SimClock(),
                CurrentTargetId = "enemy1"
            };
        }

        private void EquipSkill(CharacterData character, string professionId, string slotId, string skillId)
        {
            if (!character.EquippedSkillsByProfession.TryGetValue(professionId, out var config))
            {
                config = new EquippedSkillsConfig
                {
                    ProfessionId = professionId,
                    ActiveSlots = new Dictionary<string, string?>()
                };
                character.EquippedSkillsByProfession[professionId] = config;
            }

            if (slotId.StartsWith("active_"))
            {
                config.ActiveSlots[slotId] = skillId;
            }
            else if (slotId == "passive_1")
            {
                config.PassiveSlot = skillId;
            }
        }

        #endregion

        #region PreAttack Window Tests (4 tests)

        [Fact]
        public void PreAttackWindow_SelectsCastSkill_WhenAvailable()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("mana", 10, 10); // 有足够的法力
            var context = CreateTestContext(character, buckets);

            // 装备一个施法技能
            EquipSkill(character, "warrior", "active_1", "mage_pyroblast"); // cast skill

            // Act
            var results = executor.ExecuteWindow(WindowType.PreAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert
            Assert.Single(results);
            Assert.Equal("mage_pyroblast", results[0].Id);
        }

        [Fact]
        public void PreAttackWindow_IgnoresInstantSkills()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var context = CreateTestContext(character);

            // 装备瞬发技能（不应该在 PreAttack 窗口触发）
            EquipSkill(character, "warrior", "active_1", "warrior_mortal_strike"); // instant skill

            // Act
            var results = executor.ExecuteWindow(WindowType.PreAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void PreAttackWindow_RespectsResourceRequirements()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("mana", 10, 0); // 没有法力
            var context = CreateTestContext(character, buckets);

            // 装备需要资源的施法技能（mage_pyroblast 需要 8 法力）
            EquipSkill(character, "warrior", "active_1", "mage_pyroblast");

            // Act
            var results = executor.ExecuteWindow(WindowType.PreAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert - 应该没有技能可用（资源不足）
            Assert.Empty(results);
        }

        [Fact]
        public void PreAttackWindow_ReturnsCastSkillWithSufficientResources()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("mana", 10, 10); // 有足够的法力
            var context = CreateTestContext(character, buckets);

            // 装备需要资源的施法技能
            EquipSkill(character, "warrior", "active_1", "mage_pyroblast");

            // Act
            var results = executor.ExecuteWindow(WindowType.PreAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert
            Assert.Single(results);
            Assert.Equal("mage_pyroblast", results[0].Id);
        }

        #endregion

        #region PostAttack Window Tests (6 tests)

        [Fact]
        public void PostAttackWindow_ExecutesInstantSkills()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("rage", 10, 10); // 充足的资源
            var context = CreateTestContext(character, buckets);

            // 装备瞬发技能
            EquipSkill(character, "warrior", "active_1", "warrior_mortal_strike"); // instant, GCD

            // Act
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert
            Assert.Single(results);
            Assert.Equal("warrior_mortal_strike", results[0].Id);
        }

        [Fact]
        public void PostAttackWindow_IgnoresCastSkills()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var context = CreateTestContext(character);

            // 装备施法技能（不应该在 PostAttack 窗口触发）
            EquipSkill(character, "warrior", "active_1", "mage_pyroblast");

            // Act
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void PostAttackWindow_GcdSkill_BlockedWhenGcdUsed()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("rage", 10, 10);
            var context = CreateTestContext(character, buckets);

            // 装备 GCD 技能
            EquipSkill(character, "warrior", "active_1", "warrior_mortal_strike"); // isGcd=true

            // Act - GCD 已被占用（例如普攻占用）
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: true);

            // Assert - GCD 技能不应该触发
            Assert.Empty(results);
        }

        [Fact]
        public void PostAttackWindow_NonGcdSkill_TriggersEvenWhenGcdUsed()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var context = CreateTestContext(character);

            // 装备非 GCD 技能
            EquipSkill(character, "warrior", "active_1", "warrior_thunderclap"); // isGcd=false

            // Act - GCD 已被占用
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: true);

            // Assert - 非 GCD 技能可以触发
            Assert.Single(results);
            Assert.Equal("warrior_thunderclap", results[0].Id);
        }

        [Fact]
        public void PostAttackWindow_MultipleNonGcdSkills_AllTrigger()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var context = CreateTestContext(character);

            // 装备多个非 GCD 技能
            EquipSkill(character, "warrior", "active_1", "warrior_thunderclap"); // isGcd=false
            EquipSkill(character, "warrior", "active_2", "warrior_battle_shout"); // isGcd=false

            // Act
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert - 两个非 GCD 技能都应该触发
            Assert.Equal(2, results.Count);
            Assert.Contains(results, s => s.Id == "warrior_thunderclap");
            Assert.Contains(results, s => s.Id == "warrior_battle_shout");
        }

        [Fact]
        public void PostAttackWindow_MixedSkills_OnlyOneGcdTriggers()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("rage", 10, 10);
            var context = CreateTestContext(character, buckets);

            // 装备混合技能：1个 GCD + 2个非 GCD
            EquipSkill(character, "warrior", "active_1", "warrior_mortal_strike"); // isGcd=true, slot 1 优先
            EquipSkill(character, "warrior", "active_2", "warrior_thunderclap"); // isGcd=false
            EquipSkill(character, "warrior", "active_3", "warrior_slam"); // isGcd=true

            // Act
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert - 应该触发：1个 GCD (slot 1) + 1个非 GCD
            Assert.Equal(2, results.Count);
            Assert.Contains(results, s => s.Id == "warrior_mortal_strike"); // GCD 技能（优先级高）
            Assert.Contains(results, s => s.Id == "warrior_thunderclap"); // 非 GCD 技能
            Assert.DoesNotContain(results, s => s.Id == "warrior_slam"); // 第二个 GCD 技能应被阻止
        }

        #endregion

        #region PostCast Window Tests (4 tests)

        [Fact]
        public void PostCastWindow_OnlyTriggersSkillsWithAllowCoTrigger()
        {
            // Arrange
            var repo = new SkillRepository();
            var executor = CreateExecutor(repo);
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("rage", 10, 10);
            var context = CreateTestContext(character, buckets);

            // 创建测试技能：一个允许 CoTrigger，一个不允许
            var skill1 = new SkillDef
            {
                Id = "test_cotrigger_yes",
                ReleaseType = "instant",
                IsGcd = false,
                AllowCoTriggerAfterCast = true
            };
            var skill2 = new SkillDef
            {
                Id = "test_cotrigger_no",
                ReleaseType = "instant",
                IsGcd = false,
                AllowCoTriggerAfterCast = false
            };

            repo.RegisterSkill(skill1);
            repo.RegisterSkill(skill2);

            EquipSkill(character, "warrior", "active_1", "test_cotrigger_yes");
            EquipSkill(character, "warrior", "active_2", "test_cotrigger_no");

            // Act
            var results = executor.ExecuteWindow(WindowType.PostCast, character, "warrior", context, gcdAlreadyUsed: true);

            // Assert - 只有 AllowCoTriggerAfterCast=true 的技能触发
            Assert.Single(results);
            Assert.Equal("test_cotrigger_yes", results[0].Id);
        }

        [Fact]
        public void PostCastWindow_RespectsGcdRules()
        {
            // Arrange
            var repo = new SkillRepository();
            var executor = CreateExecutor(repo);
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("rage", 10, 10);
            var context = CreateTestContext(character, buckets);

            // 创建 GCD 技能，允许 CoTrigger
            var skill = new SkillDef
            {
                Id = "test_gcd_cotrigger",
                ReleaseType = "instant",
                IsGcd = true,
                AllowCoTriggerAfterCast = true
            };

            repo.RegisterSkill(skill);
            EquipSkill(character, "warrior", "active_1", "test_gcd_cotrigger");

            // Act - GCD 已被施法技能占用
            var results = executor.ExecuteWindow(WindowType.PostCast, character, "warrior", context, gcdAlreadyUsed: true);

            // Assert - GCD 技能不应该触发（即使 AllowCoTriggerAfterCast=true）
            Assert.Empty(results);
        }

        [Fact]
        public void PostCastWindow_MultipleNonGcdCoTriggerSkills_AllTrigger()
        {
            // Arrange
            var repo = new SkillRepository();
            var executor = CreateExecutor(repo);
            var character = CreateTestCharacter();
            var context = CreateTestContext(character);

            // 创建多个非 GCD 技能，都允许 CoTrigger
            var skill1 = new SkillDef
            {
                Id = "test_cotrigger_1",
                ReleaseType = "instant",
                IsGcd = false,
                AllowCoTriggerAfterCast = true
            };
            var skill2 = new SkillDef
            {
                Id = "test_cotrigger_2",
                ReleaseType = "instant",
                IsGcd = false,
                AllowCoTriggerAfterCast = true
            };

            repo.RegisterSkill(skill1);
            repo.RegisterSkill(skill2);

            EquipSkill(character, "warrior", "active_1", "test_cotrigger_1");
            EquipSkill(character, "warrior", "active_2", "test_cotrigger_2");

            // Act
            var results = executor.ExecuteWindow(WindowType.PostCast, character, "warrior", context, gcdAlreadyUsed: true);

            // Assert - 两个技能都应该触发
            Assert.Equal(2, results.Count);
            Assert.Contains(results, s => s.Id == "test_cotrigger_1");
            Assert.Contains(results, s => s.Id == "test_cotrigger_2");
        }

        [Fact]
        public void PostCastWindow_IgnoresCastSkills()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var context = CreateTestContext(character);

            // 装备施法技能
            EquipSkill(character, "warrior", "active_1", "mage_pyroblast");

            // Act
            var results = executor.ExecuteWindow(WindowType.PostCast, character, "warrior", context, gcdAlreadyUsed: true);

            // Assert - 施法技能不应该在 PostCast 窗口触发
            Assert.Empty(results);
        }

        #endregion

        #region GCD Exclusion Tests (4 tests)

        [Fact]
        public void GcdExclusion_OnlyFirstGcdSkillTriggersInWindow()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("rage", 10, 10);
            var context = CreateTestContext(character, buckets);

            // 装备3个 GCD 技能
            EquipSkill(character, "warrior", "active_1", "warrior_mortal_strike"); // GCD
            EquipSkill(character, "warrior", "active_2", "warrior_slam"); // GCD
            EquipSkill(character, "warrior", "passive_1", "warrior_rend"); // GCD

            // Act
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert - 只有第一个 GCD 技能触发
            Assert.Single(results);
            Assert.Equal("warrior_mortal_strike", results[0].Id);
        }

        [Fact]
        public void GcdExclusion_NonGcdSkillsNotBlockedByGcd()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("rage", 10, 10);
            var context = CreateTestContext(character, buckets);

            // 装备：GCD 技能 + 非 GCD 技能
            EquipSkill(character, "warrior", "active_1", "warrior_mortal_strike"); // GCD
            EquipSkill(character, "warrior", "active_2", "warrior_thunderclap"); // 非 GCD

            // Act
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert - 两个技能都应该触发
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void GcdExclusion_GcdUsedByPreviousAction_BlocksAllGcdSkills()
        {
            // Arrange
            var executor = CreateExecutor();
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("rage", 10, 10);
            var context = CreateTestContext(character, buckets);

            // 装备 GCD 技能
            EquipSkill(character, "warrior", "active_1", "warrior_mortal_strike");
            EquipSkill(character, "warrior", "active_2", "warrior_slam");

            // Act - GCD 已被占用（例如普攻）
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: true);

            // Assert - 所有 GCD 技能都应被阻止
            Assert.Empty(results);
        }

        [Fact]
        public void GcdExclusion_GcdUsedByPreviousAction_AllowsNonGcdSkills()
        {
            // Arrange
            var repo = new SkillRepository();
            var executor = CreateExecutor(repo);
            var character = CreateTestCharacter();
            var context = CreateTestContext(character);

            // 创建测试用的非 GCD 技能
            var skill1 = new SkillDef
            {
                Id = "test_nongcd_1",
                ReleaseType = "instant",
                IsGcd = false
            };
            var skill2 = new SkillDef
            {
                Id = "test_nongcd_2",
                ReleaseType = "instant",
                IsGcd = false
            };
            
            repo.RegisterSkill(skill1);
            repo.RegisterSkill(skill2);

            // 装备非 GCD 技能
            EquipSkill(character, "warrior", "active_1", "test_nongcd_1");
            EquipSkill(character, "warrior", "active_2", "test_nongcd_2");

            // Act - GCD 已被占用
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: true);

            // Assert - 非 GCD 技能不受影响
            Assert.Equal(2, results.Count);
        }

        #endregion

        #region Non-GCD Co-Trigger Tests (2 tests)

        [Fact]
        public void NonGcdCoTrigger_MultipleNonGcdSkills_AllTriggerSimultaneously()
        {
            // Arrange
            var repo = new SkillRepository();
            var executor = CreateExecutor(repo);
            var character = CreateTestCharacter();
            var context = CreateTestContext(character);

            // 创建3个非 GCD 技能
            var skill1 = new SkillDef
            {
                Id = "test_nongcd_a",
                ReleaseType = "instant",
                IsGcd = false
            };
            var skill2 = new SkillDef
            {
                Id = "test_nongcd_b",
                ReleaseType = "instant",
                IsGcd = false
            };
            var skill3 = new SkillDef
            {
                Id = "test_nongcd_c",
                Type = "passive",
                ReleaseType = "instant",
                IsGcd = false
            };
            
            repo.RegisterSkill(skill1);
            repo.RegisterSkill(skill2);
            repo.RegisterSkill(skill3);

            // 装备3个非 GCD 技能
            EquipSkill(character, "warrior", "active_1", "test_nongcd_a");
            EquipSkill(character, "warrior", "active_2", "test_nongcd_b");
            EquipSkill(character, "warrior", "passive_1", "test_nongcd_c");

            // Act
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert - 所有3个非 GCD 技能都应触发
            Assert.Equal(3, results.Count);
            Assert.Contains(results, s => s.Id == "test_nongcd_a");
            Assert.Contains(results, s => s.Id == "test_nongcd_b");
            Assert.Contains(results, s => s.Id == "test_nongcd_c");
        }

        [Fact]
        public void NonGcdCoTrigger_WithOneGcdSkill_GcdPlusAllNonGcd()
        {
            // Arrange
            var repo = new SkillRepository();
            var executor = CreateExecutor(repo);
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("rage", 10, 10);
            var context = CreateTestContext(character, buckets);

            // 创建测试技能：1 GCD + 2 非 GCD
            var gcdSkill = new SkillDef
            {
                Id = "test_gcd_skill",
                ReleaseType = "instant",
                IsGcd = true
            };
            var nonGcdSkill1 = new SkillDef
            {
                Id = "test_nongcd_x",
                ReleaseType = "instant",
                IsGcd = false
            };
            var nonGcdSkill2 = new SkillDef
            {
                Id = "test_nongcd_y",
                ReleaseType = "instant",
                IsGcd = false
            };
            
            repo.RegisterSkill(gcdSkill);
            repo.RegisterSkill(nonGcdSkill1);
            repo.RegisterSkill(nonGcdSkill2);

            // 装备：1 GCD + 2 非 GCD
            EquipSkill(character, "warrior", "active_1", "test_gcd_skill"); // GCD
            EquipSkill(character, "warrior", "active_2", "test_nongcd_x"); // 非 GCD
            EquipSkill(character, "warrior", "active_3", "test_nongcd_y"); // 非 GCD

            // Act
            var results = executor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert - 应该触发：1 GCD + 2 非 GCD = 3 个技能
            Assert.Equal(3, results.Count);
            Assert.Contains(results, s => s.Id == "test_gcd_skill");
            Assert.Contains(results, s => s.Id == "test_nongcd_x");
            Assert.Contains(results, s => s.Id == "test_nongcd_y");
        }

        #endregion

        #region GCD Slot Selection Priority Tests (2 tests)

        [Fact]
        public void GcdSlotSelection_FirstGcdUnavailable_SelectsSecondGcd()
        {
            // Arrange - 测试用户提出的场景：slot 1 GCD 不可用，slot 2 GCD 可用
            var repo = new SkillRepository();
            var executor = CreateExecutor(repo);
            var character = CreateTestCharacter();
            var cooldownManager = new CooldownManager();
            var resourceManager = new ResourceManager();
            var conditionChecker = new ConditionChecker();
            var buckets = new ResourceBucketCollection("rage", 10, 0); // 0 怒气
            var context = CreateTestContext(character, buckets);

            // 创建两个 GCD 技能：slot 1 需要资源（不可用），slot 2 不需要资源（可用）
            var gcdSkill1 = new SkillDef
            {
                Id = "test_gcd_1_needs_resource",
                ReleaseType = "instant",
                IsGcd = true,
                Costs = new List<ResourceCost>
                {
                    new ResourceCost { BucketId = "rage", Amount = 5 } // 需要5怒气
                }
            };
            var gcdSkill2 = new SkillDef
            {
                Id = "test_gcd_2_no_cost",
                ReleaseType = "instant",
                IsGcd = true
                // 不需要资源
            };

            repo.RegisterSkill(gcdSkill1);
            repo.RegisterSkill(gcdSkill2);

            // 装备顺序：slot 1 (不可用), slot 2 (可用)
            EquipSkill(character, "warrior", "active_1", "test_gcd_1_needs_resource");
            EquipSkill(character, "warrior", "active_2", "test_gcd_2_no_cost");

            // 创建新的 executor 使用实际的 managers
            var testExecutor = new WindowExecutor(repo, conditionChecker, cooldownManager, resourceManager);

            // Act
            var results = testExecutor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert - 应该选择 slot 2 的 GCD 技能（slot 1 因资源不足被跳过）
            Assert.Single(results);
            Assert.Equal("test_gcd_2_no_cost", results[0].Id);
        }

        [Fact]
        public void GcdSlotSelection_FirstGcdOnCooldown_SelectsSecondGcd()
        {
            // Arrange - 测试 slot 1 GCD 冷却中，slot 2 GCD 可用
            var repo = new SkillRepository();
            var cooldownManager = new CooldownManager();
            var resourceManager = new ResourceManager();
            var conditionChecker = new ConditionChecker();
            var character = CreateTestCharacter();
            var context = CreateTestContext(character);

            // 创建两个 GCD 技能
            var gcdSkill1 = new SkillDef
            {
                Id = "test_gcd_1_on_cd",
                ReleaseType = "instant",
                IsGcd = true,
                CooldownSec = 10.0
            };
            var gcdSkill2 = new SkillDef
            {
                Id = "test_gcd_2_ready",
                ReleaseType = "instant",
                IsGcd = true
            };

            repo.RegisterSkill(gcdSkill1);
            repo.RegisterSkill(gcdSkill2);

            // 装备两个技能
            EquipSkill(character, "warrior", "active_1", "test_gcd_1_on_cd");
            EquipSkill(character, "warrior", "active_2", "test_gcd_2_ready");

            // 让 slot 1 进入冷却
            cooldownManager.StartCooldown("test_gcd_1_on_cd", 10.0);

            // 创建 executor
            var testExecutor = new WindowExecutor(repo, conditionChecker, cooldownManager, resourceManager);

            // Act
            var results = testExecutor.ExecuteWindow(WindowType.PostAttack, character, "warrior", context, gcdAlreadyUsed: false);

            // Assert - 应该选择 slot 2 的 GCD 技能（slot 1 在冷却中被跳过）
            Assert.Single(results);
            Assert.Equal("test_gcd_2_ready", results[0].Id);
        }

        #endregion

        #region PostCast Window Integration Test (1 test)

        [Fact]
        public void PostCastWindow_TriggersAfterCastSkill_WithAllowCoTriggerAfterCast()
        {
            // Arrange - 测试施法技能后触发 PostCast 窗口
            var repo = new SkillRepository();
            var executor = CreateExecutor(repo);
            var character = CreateTestCharacter();
            var buckets = new ResourceBucketCollection("mana", 10, 10);
            var context = CreateTestContext(character, buckets);

            // 创建施法技能和 PostCast 瞬发技能
            var castSkill = new SkillDef
            {
                Id = "test_cast_skill",
                ReleaseType = "cast",
                IsGcd = true  // 施法技能通常是 GCD
            };
            var postCastSkill = new SkillDef
            {
                Id = "test_postcast_instant",
                ReleaseType = "instant",
                IsGcd = false,
                AllowCoTriggerAfterCast = true  // 允许在施法后触发
            };

            repo.RegisterSkill(castSkill);
            repo.RegisterSkill(postCastSkill);

            EquipSkill(character, "warrior", "active_1", "test_cast_skill");
            EquipSkill(character, "warrior", "active_2", "test_postcast_instant");

            // Act - 模拟施法后的 PostCast 窗口
            // 施法技能是 GCD，所以 gcdAlreadyUsed=true
            var results = executor.ExecuteWindow(WindowType.PostCast, character, "warrior", context, gcdAlreadyUsed: true);

            // Assert - PostCast 窗口应该触发 AllowCoTriggerAfterCast=true 的技能
            Assert.Single(results);
            Assert.Equal("test_postcast_instant", results[0].Id);
        }

        #endregion
    }
}
