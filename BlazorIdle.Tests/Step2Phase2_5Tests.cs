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

        #region Enhanced Validation Tests (6 tests) - Step 2 Phase 2.5+

        [Fact]
        public void SkillLearningManager_CanLearnSkill_ReturnsFalse_WhenMissingAccountFlag()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            
            // Create a skill with account flag requirement
            var skill = new SkillDef
            {
                Id = "test_skill_with_flag",
                Name = "Test Skill",
                Type = "active",
                SlotType = "active",
                Fixed = false,
                ReleaseType = "instant",
                AllowedProfessions = new System.Collections.Generic.List<string> { "warrior" },
                Unlock = new UnlockConfig
                {
                    MinLevel = 1,
                    AccountFlags = new System.Collections.Generic.List<string> { "achievement_test" }
                }
            };
            
            // Add skill to repository
            var skillsList = new System.Collections.Generic.List<SkillDef>
            {
                { skill }
            };
            repo.LoadFromJson(JsonSerializer.Serialize(skillsList));

            // Act - Character doesn't have the required account flag
            var result = manager.CanLearnSkill(characterData, "test_skill_with_flag", characterLevel: 10, "warrior");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SkillLearningManager_CanLearnSkill_ReturnsTrue_WhenHasAccountFlag()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.AccountFlags.Add("achievement_test");
            
            // Create a skill with account flag requirement
            var skill = new SkillDef
            {
                Id = "test_skill_with_flag",
                Name = "Test Skill",
                Type = "active",
                SlotType = "active",
                Fixed = false,
                ReleaseType = "instant",
                AllowedProfessions = new System.Collections.Generic.List<string> { "warrior" },
                Unlock = new UnlockConfig
                {
                    MinLevel = 1,
                    AccountFlags = new System.Collections.Generic.List<string> { "achievement_test" }
                }
            };
            
            // Add skill to repository
            var skillsList = new System.Collections.Generic.List<SkillDef>
            {
                { skill }
            };
            repo.LoadFromJson(JsonSerializer.Serialize(skillsList));

            // Act - Character has the required account flag
            var result = manager.CanLearnSkill(characterData, "test_skill_with_flag", characterLevel: 10, "warrior");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SkillLearningManager_CanLearnSkill_ReturnsFalse_WhenMissingMultipleAccountFlags()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.AccountFlags.Add("achievement_test1"); // Only has one of two required
            
            // Create a skill requiring multiple account flags
            var skill = new SkillDef
            {
                Id = "test_skill_multi_flag",
                Name = "Test Skill Multi",
                Type = "active",
                SlotType = "active",
                Fixed = false,
                ReleaseType = "instant",
                AllowedProfessions = new System.Collections.Generic.List<string> { "warrior" },
                Unlock = new UnlockConfig
                {
                    MinLevel = 1,
                    AccountFlags = new System.Collections.Generic.List<string> { "achievement_test1", "achievement_test2" }
                }
            };
            
            // Add skill to repository
            var skillsList = new System.Collections.Generic.List<SkillDef>
            {
                { skill }
            };
            repo.LoadFromJson(JsonSerializer.Serialize(skillsList));

            // Act - Character only has one of two required flags
            var result = manager.CanLearnSkill(characterData, "test_skill_multi_flag", characterLevel: 10, "warrior");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SkillLearningManager_CanLearnSkill_ReturnsFalse_WhenProfessionLevelTooLow()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            
            // Set up profession progress - warrior at level 3, mage at level 1
            characterData.Professions["warrior"] = new ProfessionProgress { Level = 3 };
            characterData.Professions["mage"] = new ProfessionProgress { Level = 1 };
            
            // Create a skill requiring warrior level 5
            var skill = new SkillDef
            {
                Id = "test_skill_prof_level",
                Name = "Test Skill Prof",
                Type = "active",
                SlotType = "active",
                Fixed = false,
                ReleaseType = "instant",
                AllowedProfessions = new System.Collections.Generic.List<string> { "warrior" },
                Unlock = new UnlockConfig
                {
                    MinLevel = 1,
                    RequiresProfessionLevel = new System.Collections.Generic.Dictionary<string, int>
                    {
                        { "warrior", 5 }
                    }
                }
            };
            
            // Add skill to repository
            var skillsList = new System.Collections.Generic.List<SkillDef>
            {
                { skill }
            };
            repo.LoadFromJson(JsonSerializer.Serialize(skillsList));

            // Act - Warrior level is too low (3 < 5)
            var result = manager.CanLearnSkill(characterData, "test_skill_prof_level", characterLevel: 10, "warrior");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SkillLearningManager_CanLearnSkill_ReturnsTrue_WhenProfessionLevelMet()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            
            // Set up profession progress - warrior at level 5
            characterData.Professions["warrior"] = new ProfessionProgress { Level = 5 };
            
            // Create a skill requiring warrior level 5
            var skill = new SkillDef
            {
                Id = "test_skill_prof_level",
                Name = "Test Skill Prof",
                Type = "active",
                SlotType = "active",
                Fixed = false,
                ReleaseType = "instant",
                AllowedProfessions = new System.Collections.Generic.List<string> { "warrior" },
                Unlock = new UnlockConfig
                {
                    MinLevel = 1,
                    RequiresProfessionLevel = new System.Collections.Generic.Dictionary<string, int>
                    {
                        { "warrior", 5 }
                    }
                }
            };
            
            // Add skill to repository
            var skillsList = new System.Collections.Generic.List<SkillDef>
            {
                { skill }
            };
            repo.LoadFromJson(JsonSerializer.Serialize(skillsList));

            // Act - Warrior level meets requirement (5 >= 5)
            var result = manager.CanLearnSkill(characterData, "test_skill_prof_level", characterLevel: 10, "warrior");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SkillLearningManager_CanLearnSkill_ReturnsFalse_WhenProfessionNotUnlocked()
        {
            // Arrange
            var repo = new SkillRepository();
            var manager = new SkillLearningManager(repo);
            var characterData = new CharacterData { ProfessionId = "warrior" };
            
            // Only warrior profession is unlocked, mage is not
            characterData.Professions["warrior"] = new ProfessionProgress { Level = 5 };
            // Note: mage is not in Professions dictionary
            
            // Create a skill requiring mage level 3
            var skill = new SkillDef
            {
                Id = "test_skill_mage_level",
                Name = "Test Skill Mage",
                Type = "active",
                SlotType = "active",
                Fixed = false,
                ReleaseType = "instant",
                AllowedProfessions = new System.Collections.Generic.List<string> { "warrior" },
                Unlock = new UnlockConfig
                {
                    MinLevel = 1,
                    RequiresProfessionLevel = new System.Collections.Generic.Dictionary<string, int>
                    {
                        { "mage", 3 }
                    }
                }
            };
            
            // Add skill to repository
            var skillsList = new System.Collections.Generic.List<SkillDef>
            {
                { skill }
            };
            repo.LoadFromJson(JsonSerializer.Serialize(skillsList));

            // Act - Mage profession is not unlocked (not in Professions dictionary)
            var result = manager.CanLearnSkill(characterData, "test_skill_mage_level", characterLevel: 10, "warrior");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CharacterData_Serialization_IncludesAccountFlags()
        {
            // Arrange
            var characterData = new CharacterData { ProfessionId = "warrior" };
            characterData.AccountFlags.Add("achievement_test1");
            characterData.AccountFlags.Add("achievement_test2");

            // Act
            var json = JsonSerializer.Serialize(characterData);
            var deserialized = JsonSerializer.Deserialize<CharacterData>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized!.AccountFlags.Count);
            Assert.Contains("achievement_test1", deserialized.AccountFlags);
            Assert.Contains("achievement_test2", deserialized.AccountFlags);
        }

        #endregion
    }
}
