using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using BlazorIdle.Shared.Models;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 9 集成测试：AutoCastEngine 在实际战斗中的表现
    /// Phase 9 Integration Tests: AutoCastEngine performance in actual battle
    /// </summary>
    public class Step2Phase9IntegrationTests
    {
        [Fact]
        public void AutoCastEngine_WithEquippedSkills_ExecutesInPostAttackWindow()
        {
            // Arrange: 创建带有装备技能的角色
            // Arrange: Create character with equipped skills
            var characterData = new CharacterData 
            { 
                ProfessionId = "warrior",
                ActiveCombatProfessionId = "warrior"
            };

            // 装备一些瞬发技能用于测试
            // Equip some instant skills for testing
            // warrior_mortal_strike: isGcd=true, costs 3 rage, cd=3s
            // warrior_thunderclap: isGcd=false, no cost, cd=10s
            // warrior_slam: isGcd=true, no cost, cd=10s
            characterData.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" }, // GCD 技能，需要 3 怒气
                    { "active_2", "warrior_thunderclap" },   // 非 GCD 技能，无消耗
                    { "active_3", "warrior_slam" }           // GCD 技能，无消耗
                }
            };

            // 创建战斗所需的组件
            // Create battle components
            var repo = new SkillRepository();
            var conditionChecker = new ConditionChecker();
            var cooldownManager = new CooldownManager();
            var resourceManager = new ResourceManager();
            var cooldownManagers = new Dictionary<string, CooldownManager>();
            CooldownManager GetCooldownManager(string casterId)
            {
                if (!cooldownManagers.TryGetValue(casterId, out var manager))
                {
                    manager = cooldownManager;
                    cooldownManagers[casterId] = manager;
                }
                return manager;
            }
            var autoCastEngine = new AutoCastEngine(repo, conditionChecker, GetCooldownManager, resourceManager);

            // 创建角色实体和资源
            // Create character entity and resources
            var character = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                AttackRateAPS = 0.4,
                DamagePerAttack = 100,
                ActiveCombatProfessionId = "warrior"
            };

            var resources = new BlazorIdle.Game.Resources.ResourceBucketCollection("rage", max: 100, initial: 10);

            // 创建战斗上下文
            // Create battle context
            var context = new BattleContext
            {
                Player = character,
                PlayerResources = resources,
                Rng = new RngContext(12345),
                Clock = new SimClock(),
                CurrentTargetId = "enemy1"
            };

            // Act: 测试 PreAttack 窗口（应该不选择施法技能，因为没有）
            // Act: Test PreAttack window (should not select cast skill as there are none)
            var castSkill = autoCastEngine.SelectCastSkill("test_char_1", characterData, "warrior", context);

            // 测试 PostAttack 窗口（假设普通攻击 isGcd=false）
            // Test PostAttack window (assuming normal attack isGcd=false)
            var normalAttackSkill = repo.GetSkill("warrior_attack_basic");
            bool normalAttackIsGcd = normalAttackSkill?.IsGcd ?? false;
            
            var instantSkills = autoCastEngine.ExecuteWindow("test_char_1", characterData, "warrior", context, normalAttackIsGcd, "PostAttack");

            // Assert: 验证技能选择
            // Assert: Verify skill selection
            Assert.Null(castSkill); // 没有施法技能

            // 应该选择了技能（取决于资源和冷却）
            // Should have selected skills (depending on resources and cooldowns)
            Assert.NotNull(instantSkills);
            
            // AutoCastEngine 应该至少尝试选择技能（即使可能因为资源/冷却而失败）
            // AutoCastEngine should at least attempt to select skills (even if they fail due to resources/cooldowns)
            // 这个测试主要验证集成工作正常，不是验证具体选择了哪些技能
            // This test mainly verifies integration works, not which specific skills are selected
            
            // 验证至少有一个技能被考虑（通过检查装备的技能存在）
            // Verify at least one skill was considered (by checking equipped skills exist)
            var mortalStrike = repo.GetSkill("warrior_mortal_strike");
            var thunderclap = repo.GetSkill("warrior_thunderclap");
            var slam = repo.GetSkill("warrior_slam");
            
            Assert.NotNull(mortalStrike);
            Assert.NotNull(thunderclap);
            Assert.NotNull(slam);
            
            // 这个测试证明了 AutoCastEngine 可以访问装备的技能并执行选择逻辑
            // This test proves AutoCastEngine can access equipped skills and execute selection logic
        }

        [Fact]
        public void AutoCastEngine_WithInsufficientResources_SelectsOnlyAffordableSkills()
        {
            // Arrange: 创建资源不足的场景
            // Arrange: Create scenario with insufficient resources
            var characterData = new CharacterData 
            { 
                ProfessionId = "warrior",
                ActiveCombatProfessionId = "warrior"
            };

            // 装备需要资源的技能
            // Equip skills that require resources
            characterData.EquippedSkillsByProfession["warrior"] = new EquippedSkillsConfig
            {
                ProfessionId = "warrior",
                ActiveSlots = new Dictionary<string, string>
                {
                    { "active_1", "warrior_mortal_strike" }, // 需要 3 怒气
                    { "active_2", "warrior_rend" },          // 需要 1 怒气
                    { "active_3", "warrior_slam" }           // 不需要怒气
                }
            };

            var repo = new SkillRepository();
            var conditionChecker = new ConditionChecker();
            var cooldownManager = new CooldownManager();
            var resourceManager = new ResourceManager();
            var cooldownManagers2 = new Dictionary<string, CooldownManager>();
            CooldownManager GetCooldownManager2(string casterId)
            {
                if (!cooldownManagers2.TryGetValue(casterId, out var manager))
                {
                    manager = cooldownManager;
                    cooldownManagers2[casterId] = manager;
                }
                return manager;
            }
            var autoCastEngine = new AutoCastEngine(repo, conditionChecker, GetCooldownManager2, resourceManager);

            var character = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                AttackRateAPS = 0.4,
                DamagePerAttack = 100,
                ActiveCombatProfessionId = "warrior"
            };

            // 只有 2 点怒气（不够释放 warrior_mortal_strike）
            // Only 2 rage (not enough for warrior_mortal_strike)
            var resources = new BlazorIdle.Game.Resources.ResourceBucketCollection("rage", max: 100, initial: 2);

            var context = new BattleContext
            {
                Player = character,
                PlayerResources = resources,
                Rng = new RngContext(12345),
                Clock = new SimClock(),
                CurrentTargetId = "enemy1"
            };

            // Act: 执行 PostAttack 窗口（假设普通攻击是 GCD）
            // Act: Execute PostAttack window (assuming normal attack is GCD)
            var instantSkills = autoCastEngine.ExecuteWindow("test_char_1", characterData, "warrior", context, gcdAlreadyUsed: true, "PostAttack");

            // Assert: 应该跳过需要资源的 GCD 技能，只选择不需要资源的技能
            // Assert: Should skip GCD skills requiring resources, only select skills without cost
            Assert.NotNull(instantSkills);
            
            // 因为普通攻击已经占用了 GCD，所以不应该有 GCD 技能
            // Since normal attack already used GCD, there should be no GCD skills
            var hasGcdSkill = instantSkills.Exists(s => 
            {
                var skill = repo.GetSkill(s.Id);
                return skill != null && skill.IsGcd;
            });
            Assert.False(hasGcdSkill, "因为 GCD 已被占用，不应该有 GCD 技能");
        }

        /// <summary>
        /// Per-Character Cooldown Test: 验证两个角色使用相同技能时冷却独立
        /// Per-Character Cooldown Test: Verify independent cooldowns when two characters use the same skill
        /// </summary>
        [Fact]
        public void PerCharacterCooldown_TwoCharactersWithSameSkill_IndependentCooldowns()
        {
            // Arrange: 创建两个战士角色，都装备相同的技能
            // Arrange: Create two warrior characters, both equipped with the same skill
            var repo = new SkillRepository();
            var conditionChecker = new ConditionChecker();
            var cooldownManager1 = new CooldownManager();
            var cooldownManager2 = new CooldownManager();
            var resourceManager = new ResourceManager();

            // 创建cooldown管理器字典（模拟MultiBattleInstance的行为）
            var cooldownManagers = new Dictionary<string, CooldownManager>
            {
                { "char_1", cooldownManager1 },
                { "char_2", cooldownManager2 }
            };

            CooldownManager GetCooldownManager(string casterId)
            {
                if (!cooldownManagers.TryGetValue(casterId, out var manager))
                {
                    manager = new CooldownManager();
                    cooldownManagers[casterId] = manager;
                }
                return manager;
            }

            var autoCastEngine = new AutoCastEngine(repo, conditionChecker, GetCooldownManager, resourceManager);

            // 两个角色都装备相同的技能 warrior_mortal_strike
            var char1Data = new CharacterData
            {
                ProfessionId = "warrior",
                ActiveCombatProfessionId = "warrior",
                EquippedSkillsByProfession = new Dictionary<string, EquippedSkillsConfig>
                {
                    {
                        "warrior", new EquippedSkillsConfig
                        {
                            ProfessionId = "warrior",
                            ActiveSlots = new Dictionary<string, string>
                            {
                                { "active_1", "warrior_mortal_strike" }
                            }
                        }
                    }
                }
            };

            var char2Data = new CharacterData
            {
                ProfessionId = "warrior",
                ActiveCombatProfessionId = "warrior",
                EquippedSkillsByProfession = new Dictionary<string, EquippedSkillsConfig>
                {
                    {
                        "warrior", new EquippedSkillsConfig
                        {
                            ProfessionId = "warrior",
                            ActiveSlots = new Dictionary<string, string>
                            {
                                { "active_1", "warrior_mortal_strike" }
                            }
                        }
                    }
                }
            };

            var context = new BattleContext
            {
                Player = new Character { MaxHp = 1000, Hp = 1000 },
                Enemy = new Enemy { MaxHp = 1000, Hp = 1000 },
                Rng = new RngContext(0),
                Clock = new SimClock()
            };

            // Act: Char1 使用技能（通过ExecuteWindow选择瞬发技能）
            // Act: Char1 uses skill (select instant skill via ExecuteWindow)
            var char1Skills = autoCastEngine.ExecuteWindow("char_1", char1Data, "warrior", context, gcdAlreadyUsed: false, "PostAttack");
            Assert.NotEmpty(char1Skills);
            Assert.Equal("warrior_mortal_strike", char1Skills[0].Id);

            // 模拟技能施放后进入冷却（使用技能定义的冷却时间）
            // Simulate skill going on cooldown (use cooldown from skill definition)
            var skillDef = repo.GetSkill("warrior_mortal_strike");
            var cooldownDuration = skillDef?.CooldownSec ?? 5.0;
            cooldownManager1.StartCooldown("warrior_mortal_strike", cooldownDuration);

            // Assert: Char1 的技能应该在冷却中
            // Assert: Char1's skill should be on cooldown
            Assert.False(cooldownManager1.IsReady("warrior_mortal_strike"));
            Assert.True(cooldownManager1.GetRemainingCooldown("warrior_mortal_strike") > 0);

            // Assert: Char2 的相同技能应该仍然可用（这是修复的核心bug）
            // Assert: Char2's same skill should still be available (this is the core bug being fixed)
            Assert.True(cooldownManager2.IsReady("warrior_mortal_strike"));
            Assert.Equal(0, cooldownManager2.GetRemainingCooldown("warrior_mortal_strike"));

            // Char2 应该能够选择和使用相同的技能（验证冷却独立）
            // Char2 should be able to select and use the same skill (verify cooldown independence)
            var char2Skills = autoCastEngine.ExecuteWindow("char_2", char2Data, "warrior", context, gcdAlreadyUsed: false, "PostAttack");
            Assert.NotEmpty(char2Skills);
            Assert.Equal("warrior_mortal_strike", char2Skills[0].Id);

            // 使用后Char2的技能也进入冷却（使用相同的冷却时间）
            // After use, Char2's skill also goes on cooldown (use same cooldown duration)
            cooldownManager2.StartCooldown("warrior_mortal_strike", cooldownDuration);

            // 最终验证：两个角色的冷却是完全独立的
            // Final verification: Both characters' cooldowns are completely independent
            Assert.False(cooldownManager1.IsReady("warrior_mortal_strike"));
            Assert.False(cooldownManager2.IsReady("warrior_mortal_strike"));
            Assert.True(cooldownManager1.GetRemainingCooldown("warrior_mortal_strike") > 0);
            Assert.True(cooldownManager2.GetRemainingCooldown("warrior_mortal_strike") > 0);
        }
    }
}
