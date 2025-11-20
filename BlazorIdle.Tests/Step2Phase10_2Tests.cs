using Xunit;
using BlazorIdle.Game.Skills;
using BlazorIdle.Shared.Models;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Step 2 Phase 10.2 单元测试：SkillEquipmentPanel 组件功能
    /// Unit tests for SkillEquipmentPanel component functionality
    /// </summary>
    public class Step2Phase10_2Tests
    {
        private readonly SkillRepository _skillRepository;
        private readonly SkillEquipmentManager _equipmentManager;
        private readonly SkillLearningManager _learningManager;

        public Step2Phase10_2Tests()
        {
            _skillRepository = new SkillRepository();
            _equipmentManager = new SkillEquipmentManager(_skillRepository);
            _learningManager = new SkillLearningManager(_skillRepository);
        }

        #region 固定技能测试 Fixed Skills Tests

        [Fact]
        public void WarriorFixedSkills_ExcludesAttackBasicAndSpecialPulse()
        {
            // Arrange
            var professionId = "warrior";

            // Act - 获取固定技能（排除attack_basic和special_pulse）
            var fixedSkills = _skillRepository.GetSkillsByProfession(professionId)
                .Where(s => s.Fixed && 
                           !s.Id.EndsWith("_attack_basic") && 
                           !s.Id.EndsWith("_special_pulse"))
                .ToList();

            // Assert - 战士应该有 warrior_slam
            Assert.NotEmpty(fixedSkills);
            Assert.Contains(fixedSkills, s => s.Id == "warrior_slam");
            
            // 不应该包含 attack_basic 和 special_pulse
            Assert.DoesNotContain(fixedSkills, s => s.Id.EndsWith("_attack_basic"));
            Assert.DoesNotContain(fixedSkills, s => s.Id.EndsWith("_special_pulse"));
        }

        [Fact]
        public void MageFixedSkills_ExcludesAttackBasicAndSpecialPulse()
        {
            // Arrange
            var professionId = "mage";

            // Act
            var fixedSkills = _skillRepository.GetSkillsByProfession(professionId)
                .Where(s => s.Fixed && 
                           s.AllowedProfessions != null &&
                           s.AllowedProfessions.Contains(professionId) &&
                           !s.Id.EndsWith("_attack_basic") && 
                           !s.Id.EndsWith("_special_pulse"))
                .ToList();

            // Assert - 法师应该没有其他固定技能（除了基础攻击和脉冲）
            // Mage should have no other fixed skills (besides basic attack and pulse)
            Assert.Empty(fixedSkills);
        }

        #endregion

        #region 技能装备测试 Skill Equipment Tests

        [Fact]
        public void EquipSkill_ToActiveSlot_Success()
        {
            // Arrange
            var character = CreateTestCharacter("warrior");
            var skillId = "warrior_mortal_strike";
            
            // 学习技能
            character.LearnedSkills.Add(skillId);
            
            // 初始化固定技能
            _equipmentManager.InitializeFixedSkills(character, "warrior");

            // Act - 装备到 active_1 槽位
            var result = _equipmentManager.EquipSkill(character, "warrior", "active_1", skillId);

            // Assert
            Assert.True(result);
            var config = character.EquippedSkillsByProfession["warrior"];
            Assert.Equal(skillId, config.ActiveSlots["active_1"]);
        }

        [Fact]
        public void EquipSkill_ToPassiveSlot_Success()
        {
            // Arrange
            var character = CreateTestCharacter("warrior");
            var skillId = "warrior_bloodlust"; // passive skill
            
            // 学习技能
            character.LearnedSkills.Add(skillId);
            
            // 初始化固定技能
            _equipmentManager.InitializeFixedSkills(character, "warrior");

            // Act - 装备到被动槽位
            var result = _equipmentManager.EquipSkill(character, "warrior", "passive_1", skillId);

            // Assert
            Assert.True(result);
            var config = character.EquippedSkillsByProfession["warrior"];
            Assert.Equal(skillId, config.PassiveSlot);
        }

        [Fact]
        public void EquipSkill_NotLearned_Fails()
        {
            // Arrange
            var character = CreateTestCharacter("warrior");
            var skillId = "warrior_mortal_strike";
            
            // 不学习技能（模拟未学习状态）
            // Don't learn the skill (simulate not learned state)
            
            // 初始化固定技能
            _equipmentManager.InitializeFixedSkills(character, "warrior");

            // Act - 尝试装备未学习的技能
            var result = _equipmentManager.EquipSkill(character, "warrior", "active_1", skillId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void EquipSkill_WrongSlotType_Fails()
        {
            // Arrange
            var character = CreateTestCharacter("warrior");
            var skillId = "warrior_mortal_strike"; // 主动技能
            
            // 学习技能
            character.LearnedSkills.Add(skillId);
            
            // 初始化固定技能
            _equipmentManager.InitializeFixedSkills(character, "warrior");

            // Act - 尝试装备主动技能到被动槽位
            var result = _equipmentManager.EquipSkill(character, "warrior", "passive_1", skillId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void EquipSkill_DifferentProfession_Fails()
        {
            // Arrange
            var character = CreateTestCharacter("mage");
            var skillId = "warrior_mortal_strike"; // 战士技能
            
            // 学习技能（虽然通常不应该跨职业学习）
            character.LearnedSkills.Add(skillId);
            
            // 初始化固定技能
            _equipmentManager.InitializeFixedSkills(character, "mage");

            // Act - 尝试装备战士技能到法师
            var result = _equipmentManager.EquipSkill(character, "mage", "active_1", skillId);

            // Assert - 应该失败，因为职业限制
            Assert.False(result);
        }

        #endregion

        #region 技能卸载测试 Skill Unequip Tests

        [Fact]
        public void UnequipSkill_FromActiveSlot_Success()
        {
            // Arrange
            var character = CreateTestCharacter("warrior");
            var skillId = "warrior_mortal_strike";
            
            // 学习并装备技能
            character.LearnedSkills.Add(skillId);
            _equipmentManager.InitializeFixedSkills(character, "warrior");
            _equipmentManager.EquipSkill(character, "warrior", "active_1", skillId);

            // Act - 卸载技能
            var result = _equipmentManager.UnequipSkill(character, "warrior", "active_1");

            // Assert
            Assert.True(result);
            var config = character.EquippedSkillsByProfession["warrior"];
            Assert.Null(config.ActiveSlots["active_1"]);
        }

        [Fact]
        public void UnequipSkill_FixedSkill_Fails()
        {
            // Arrange
            var character = CreateTestCharacter("warrior");
            
            // 初始化固定技能（会自动装备 warrior_slam）
            _equipmentManager.InitializeFixedSkills(character, "warrior");
            
            // 找到固定技能所在的槽位
            var config = character.EquippedSkillsByProfession["warrior"];
            string? fixedSkillSlot = null;
            foreach (var (slotId, skillId) in config.ActiveSlots)
            {
                if (skillId == "warrior_slam")
                {
                    fixedSkillSlot = slotId;
                    break;
                }
            }
            
            Assert.NotNull(fixedSkillSlot); // 确保找到了固定技能槽位

            // Act - 尝试卸载固定技能
            var result = _equipmentManager.UnequipSkill(character, "warrior", fixedSkillSlot);

            // Assert - 应该失败
            Assert.False(result);
            
            // 验证固定技能仍然存在
            Assert.Equal("warrior_slam", config.ActiveSlots[fixedSkillSlot]);
        }

        #endregion

        #region 职业配置初始化测试 Profession Config Initialization Tests

        [Fact]
        public void InitializeFixedSkills_Warrior_AutoEquipsWarriorSlam()
        {
            // Arrange
            var character = CreateTestCharacter("warrior");

            // Act - 初始化战士固定技能
            _equipmentManager.InitializeFixedSkills(character, "warrior");

            // Assert - 应该自动装备 warrior_slam
            Assert.True(character.EquippedSkillsByProfession.ContainsKey("warrior"));
            var config = character.EquippedSkillsByProfession["warrior"];
            
            var hasWarriorSlam = config.ActiveSlots.Values.Contains("warrior_slam");
            Assert.True(hasWarriorSlam, "warrior_slam should be auto-equipped");
        }

        [Fact]
        public void InitializeFixedSkills_Mage_AutoEquipsBasicAndPulse()
        {
            // Arrange
            var character = CreateTestCharacter("mage");

            // Act - 初始化法师固定技能
            _equipmentManager.InitializeFixedSkills(character, "mage");

            // Assert - 法师应该自动装备 mage_attack_basic 或 mage_special_pulse
            Assert.True(character.EquippedSkillsByProfession.ContainsKey("mage"));
            var config = character.EquippedSkillsByProfession["mage"];
            
            // 被动槽位应该被装备（attack_basic 和 special_pulse 都是 passive）
            Assert.False(string.IsNullOrEmpty(config.PassiveSlot));
            Assert.True(config.PassiveSlot == "mage_attack_basic" || config.PassiveSlot == "mage_special_pulse");
            
            // 主动槽位应该为空（法师没有其他固定技能）
            Assert.All(config.ActiveSlots.Values, value => Assert.True(string.IsNullOrEmpty(value)));
        }

        #endregion

        #region 辅助方法 Helper Methods

        private CharacterData CreateTestCharacter(string professionId)
        {
            var character = new CharacterData
            {
                Id = "test-char-" + professionId,
                Name = $"Test {professionId}",
                ProfessionId = professionId,
                ActiveCombatProfessionId = professionId,
                LearnedSkills = new HashSet<string>(),
                EquippedSkillsByProfession = new Dictionary<string, EquippedSkillsConfig>()
            };

            // 添加职业进度
            character.Professions[professionId] = new ProfessionProgress
            {
                Level = 10,
                Experience = 0
            };

            return character;
        }

        #endregion
    }
}
