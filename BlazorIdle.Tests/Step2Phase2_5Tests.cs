using BlazorIdle.Game.Skills;
using BlazorIdle.Shared.Models;
using System.Text.Json;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Unit tests for Step 2 Phase 2.5: Skill Learning & Equipment System
    /// </summary>
    public class Step2Phase2_5Tests
    {
        #region SkillLearningManager Tests (8 tests)

        [Fact]
        public void SkillLearningManager_CanLearnSkill_ReturnsTrue_WhenConditionsMet()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };

            // Act
            var result = manager.CanLearnSkill(characterData, "warrior_mortal_strike", characterLevel: 3, "warrior");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SkillLearningManager_CanLearnSkill_ReturnsFalse_WhenAlreadyLearned()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.LearnedSkills.Add("warrior_mortal_strike");

            // Act
            var result = manager.CanLearnSkill(characterData, "warrior_mortal_strike", characterLevel: 3, "warrior");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SkillLearningManager_CanLearnSkill_ReturnsFalse_WhenLevelTooLow()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };

            // Act - Try to learn level 10 skill with level 5 character
            var result = manager.CanLearnSkill(characterData, "warrior_desperate_strike", characterLevel: 5, "warrior");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SkillLearningManager_CanLearnSkill_ReturnsFalse_WhenWrongProfession()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };

            // Act - Try to learn mage skill as warrior
            var result = manager.CanLearnSkill(characterData, "mage_pyroblast", characterLevel: 10, "warrior");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SkillLearningManager_LearnSkill_AddsToLearnedSkills()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };

            // Act
            var result = manager.LearnSkill(characterData, "warrior_mortal_strike", characterLevel: 3, "warrior");

            // Assert
            Assert.True(result);
            Assert.Contains("warrior_mortal_strike", characterData.LearnedSkills);
        }

        [Fact]
        public void SkillLearningManager_GetLearnableSkills_ReturnsCorrectList()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };

            // Act
            var skills = manager.GetLearnableSkills(characterData, characterLevel: 10, "warrior");

            // Assert
            Assert.NotEmpty(skills);
            // Should not include fixed skills
            Assert.DoesNotContain(skills, s => s.Fixed);
            // Should only include warrior skills (or skills with no profession restriction)
            Assert.All(skills, s =>
            {
                if (s.AllowedProfessions != null && s.AllowedProfessions.Count > 0)
                    Assert.Contains("warrior", s.AllowedProfessions);
            });
        }

        [Fact]
        public void SkillLearningManager_GetLearnedSkills_ReturnsCorrectList()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.LearnedSkills.Add("warrior_mortal_strike");
            characterData.LearnedSkills.Add("warrior_rend");

            // Act
            var skills = manager.GetLearnedSkills(characterData);

            // Assert
            Assert.Equal(2, skills.Count);
            Assert.Contains(skills, s => s.Id == "warrior_mortal_strike");
            Assert.Contains(skills, s => s.Id == "warrior_rend");
        }

        [Fact]
        public void SkillLearningManager_GetLearnableSkills_ExcludesAlreadyLearned()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.LearnedSkills.Add("warrior_mortal_strike");

            // Act
            var skills = manager.GetLearnableSkills(characterData, characterLevel: 10, "warrior");

            // Assert
            Assert.DoesNotContain(skills, s => s.Id == "warrior_mortal_strike");
        }

        #endregion

        #region SkillEquipmentManager Tests (8 tests)

        [Fact]
        public void SkillEquipmentManager_InitializeFixedSkills_CreatesConfig()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillEquipmentManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };

            // Act
            manager.InitializeFixedSkills(characterData, "warrior");

            // Assert
            Assert.True(characterData.EquippedSkillsByProfession.ContainsKey("warrior"));
            var config = characterData.EquippedSkillsByProfession["warrior"];
            Assert.NotNull(config);
            Assert.Equal("warrior", config.ProfessionId);
        }

        [Fact]
        public void SkillEquipmentManager_InitializeFixedSkills_EquipsFixedSkills()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillEquipmentManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };

            // Act
            manager.InitializeFixedSkills(characterData, "warrior");

            // Assert
            var config = characterData.EquippedSkillsByProfession["warrior"];
            var equippedSkills = config.ActiveSlots.Values.Where(s => !string.IsNullOrEmpty(s)).ToList();
            Assert.NotEmpty(equippedSkills);
            // Verify at least one fixed skill is equipped
            var hasFixedSkill = equippedSkills.Any(skillId =>
            {
                var skill = repo.GetSkill(skillId!);
                return skill != null && skill.Fixed;
            });
            Assert.True(hasFixedSkill);
        }

        [Fact]
        public void SkillEquipmentManager_EquipSkill_SucceedsForLearnedSkill()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillEquipmentManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.LearnedSkills.Add("warrior_mortal_strike");
            manager.InitializeFixedSkills(characterData, "warrior");

            // Act
            var result = manager.EquipSkill(characterData, "warrior", "active_2", "warrior_mortal_strike");

            // Assert
            Assert.True(result);
            Assert.Equal("warrior_mortal_strike", characterData.EquippedSkillsByProfession["warrior"].ActiveSlots["active_2"]);
        }

        [Fact]
        public void SkillEquipmentManager_EquipSkill_FailsForUnlearnedSkill()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillEquipmentManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            manager.InitializeFixedSkills(characterData, "warrior");

            // Act - Try to equip unlearned skill
            var result = manager.EquipSkill(characterData, "warrior", "active_2", "warrior_mortal_strike");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SkillEquipmentManager_EquipSkill_FailsForWrongSlotType()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillEquipmentManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.LearnedSkills.Add("warrior_mortal_strike");
            manager.InitializeFixedSkills(characterData, "warrior");

            // Act - Try to equip active skill to passive slot
            var result = manager.EquipSkill(characterData, "warrior", "passive_1", "warrior_mortal_strike");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SkillEquipmentManager_UnequipSkill_SucceedsForNonFixedSkill()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillEquipmentManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.LearnedSkills.Add("warrior_mortal_strike");
            manager.InitializeFixedSkills(characterData, "warrior");
            manager.EquipSkill(characterData, "warrior", "active_2", "warrior_mortal_strike");

            // Act
            var result = manager.UnequipSkill(characterData, "warrior", "active_2");

            // Assert
            Assert.True(result);
            Assert.Null(characterData.EquippedSkillsByProfession["warrior"].ActiveSlots["active_2"]);
        }

        [Fact]
        public void SkillEquipmentManager_GetEquippedSkills_ReturnsCorrectList()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillEquipmentManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.LearnedSkills.Add("warrior_mortal_strike");
            manager.InitializeFixedSkills(characterData, "warrior");
            manager.EquipSkill(characterData, "warrior", "active_2", "warrior_mortal_strike");

            // Act
            var skills = manager.GetEquippedSkills(characterData, "warrior");

            // Assert
            Assert.NotEmpty(skills);
            Assert.Contains(skills, s => s.Id == "warrior_mortal_strike");
        }

        [Fact]
        public void SkillEquipmentManager_EquipSkill_PreventsDuplicateEquipment()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillEquipmentManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.LearnedSkills.Add("warrior_mortal_strike");
            manager.InitializeFixedSkills(characterData, "warrior");
            manager.EquipSkill(characterData, "warrior", "active_1", "warrior_mortal_strike");

            // Act - Try to equip same skill to another slot
            var result = manager.EquipSkill(characterData, "warrior", "active_2", "warrior_mortal_strike");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Persistence Tests (2 tests)

        [Fact]
        public void CharacterData_Serialization_IncludesLearnedSkills()
        {
            // Arrange
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.LearnedSkills.Add("warrior_mortal_strike");
            characterData.LearnedSkills.Add("warrior_rend");

            // Act
            var json = JsonSerializer.Serialize(characterData);
            var deserialized = JsonSerializer.Deserialize<CharacterData>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized!.LearnedSkills.Count);
            Assert.Contains("warrior_mortal_strike", deserialized.LearnedSkills);
            Assert.Contains("warrior_rend", deserialized.LearnedSkills);
        }

        [Fact]
        public void CharacterData_Serialization_IncludesEquippedSkillsByProfession()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillEquipmentManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            manager.InitializeFixedSkills(characterData, "warrior");

            // Act
            var json = JsonSerializer.Serialize(characterData);
            var deserialized = JsonSerializer.Deserialize<CharacterData>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.True(deserialized!.EquippedSkillsByProfession.ContainsKey("warrior"));
            var config = deserialized.EquippedSkillsByProfession["warrior"];
            Assert.NotNull(config);
            Assert.Equal("warrior", config.ProfessionId);
            Assert.Equal(3, config.ActiveSlots.Count);
        }

        #endregion
    }
}
