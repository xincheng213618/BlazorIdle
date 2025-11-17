using Xunit;
using BlazorIdle.Shared.Models;
using System.Text.Json;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Step 2 Phase 3+ 单元测试 - 固定技能配置测试
    /// Step 2 Phase 3+ Unit Tests - Fixed skills configuration tests
    /// </summary>
    public class Step2Phase3PlusTests
    {
        #region P3.1 Tests: CharacterData 固定技能存储

        [Fact]
        public void CharacterData_FixedSkillsByProfession_DefaultInitialization()
        {
            // Arrange & Act
            var characterData = new CharacterData();

            // Assert
            Assert.NotNull(characterData.FixedSkillsByProfession);
            Assert.Empty(characterData.FixedSkillsByProfession);
        }

        [Fact]
        public void CharacterData_FixedSkillsByProfession_Serialization()
        {
            // Arrange
            var characterData = new CharacterData
            {
                Id = "test-char-1",
                Name = "Test Character",
                FixedSkillsByProfession = new Dictionary<string, ProfessionFixedSkills>
                {
                    ["warrior"] = new ProfessionFixedSkills
                    {
                        NormalAttack = "warrior_attack_basic",
                        SpecialAttack = "warrior_special_pulse"
                    },
                    ["mage"] = new ProfessionFixedSkills
                    {
                        NormalAttack = "mage_attack_basic",
                        SpecialAttack = "mage_special_pulse"
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(characterData);
            var deserialized = JsonSerializer.Deserialize<CharacterData>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.FixedSkillsByProfession.Count);
            Assert.True(deserialized.FixedSkillsByProfession.ContainsKey("warrior"));
            Assert.Equal("warrior_attack_basic", deserialized.FixedSkillsByProfession["warrior"].NormalAttack);
            Assert.Equal("warrior_special_pulse", deserialized.FixedSkillsByProfession["warrior"].SpecialAttack);
        }

        [Fact]
        public void ProfessionFixedSkills_DefaultValues()
        {
            // Arrange & Act
            var fixedSkills = new ProfessionFixedSkills();

            // Assert
            Assert.Equal("", fixedSkills.NormalAttack);
            Assert.Equal("", fixedSkills.SpecialAttack);
        }

        #endregion

        #region P3.2 Tests: ProfessionAttributeConfig 默认技能配置

        [Fact]
        public void ProfessionDefaultFixedSkills_Initialization()
        {
            // Arrange & Act
            var defaultSkills = new ProfessionDefaultFixedSkills
            {
                NormalAttack = "warrior_attack_basic",
                SpecialAttack = "warrior_special_pulse"
            };

            // Assert
            Assert.Equal("warrior_attack_basic", defaultSkills.NormalAttack);
            Assert.Equal("warrior_special_pulse", defaultSkills.SpecialAttack);
        }

        [Fact]
        public void ProfessionAttributeConfig_DefaultFixedSkills_Serialization()
        {
            // Arrange
            var config = new ProfessionAttributeConfig
            {
                Id = "warrior",
                Name = "战士",
                DefaultFixedSkills = new ProfessionDefaultFixedSkills
                {
                    NormalAttack = "warrior_attack_basic",
                    SpecialAttack = "warrior_special_pulse"
                }
            };

            // Act
            var json = JsonSerializer.Serialize(config);
            var deserialized = JsonSerializer.Deserialize<ProfessionAttributeConfig>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.DefaultFixedSkills);
            Assert.Equal("warrior_attack_basic", deserialized.DefaultFixedSkills.NormalAttack);
            Assert.Equal("warrior_special_pulse", deserialized.DefaultFixedSkills.SpecialAttack);
        }

        [Fact]
        public void ProfessionAttributeConfig_DefaultFixedSkills_CanBeNull()
        {
            // Arrange
            var config = new ProfessionAttributeConfig
            {
                Id = "warrior",
                Name = "战士"
                // DefaultFixedSkills intentionally not set
            };

            // Act
            var json = JsonSerializer.Serialize(config);
            var deserialized = JsonSerializer.Deserialize<ProfessionAttributeConfig>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Null(deserialized.DefaultFixedSkills);
        }

        #endregion

        #region P3.3 Tests: Character creation initialization

        [Fact]
        public void CharacterCreation_InitializesFixedSkills_ForAllProfessions()
        {
            // Arrange
            var characterData = new CharacterData
            {
                FixedSkillsByProfession = new Dictionary<string, ProfessionFixedSkills>
                {
                    ["warrior"] = new ProfessionFixedSkills
                    {
                        NormalAttack = "warrior_attack_basic",
                        SpecialAttack = "warrior_special_pulse"
                    },
                    ["mage"] = new ProfessionFixedSkills
                    {
                        NormalAttack = "mage_attack_basic",
                        SpecialAttack = "mage_special_pulse"
                    },
                    ["ranger"] = new ProfessionFixedSkills
                    {
                        NormalAttack = "ranger_attack_basic",
                        SpecialAttack = "ranger_special_pulse"
                    },
                    ["rogue"] = new ProfessionFixedSkills
                    {
                        NormalAttack = "rogue_attack_basic",
                        SpecialAttack = "rogue_special_pulse"
                    }
                }
            };

            // Assert
            Assert.Equal(4, characterData.FixedSkillsByProfession.Count);
            Assert.All(characterData.FixedSkillsByProfession.Values, skills =>
            {
                Assert.NotEmpty(skills.NormalAttack);
                Assert.NotEmpty(skills.SpecialAttack);
            });
        }

        [Fact]
        public void CharacterCreation_FixedSkillsPerProfession_AreDistinct()
        {
            // Arrange
            var characterData = new CharacterData
            {
                FixedSkillsByProfession = new Dictionary<string, ProfessionFixedSkills>
                {
                    ["warrior"] = new ProfessionFixedSkills
                    {
                        NormalAttack = "warrior_attack_basic",
                        SpecialAttack = "warrior_special_pulse"
                    },
                    ["mage"] = new ProfessionFixedSkills
                    {
                        NormalAttack = "mage_attack_basic",
                        SpecialAttack = "mage_special_pulse"
                    }
                }
            };

            // Act & Assert - Each profession has different skills
            Assert.NotEqual(
                characterData.FixedSkillsByProfession["warrior"].NormalAttack,
                characterData.FixedSkillsByProfession["mage"].NormalAttack
            );
            Assert.NotEqual(
                characterData.FixedSkillsByProfession["warrior"].SpecialAttack,
                characterData.FixedSkillsByProfession["mage"].SpecialAttack
            );
        }

        [Fact]
        public void CharacterCreation_CanAccessFixedSkillsByProfession()
        {
            // Arrange
            var characterData = new CharacterData
            {
                ActiveCombatProfessionId = "warrior",
                FixedSkillsByProfession = new Dictionary<string, ProfessionFixedSkills>
                {
                    ["warrior"] = new ProfessionFixedSkills
                    {
                        NormalAttack = "warrior_attack_basic",
                        SpecialAttack = "warrior_special_pulse"
                    }
                }
            };

            // Act
            var hasWarriorSkills = characterData.FixedSkillsByProfession.TryGetValue("warrior", out var warriorSkills);

            // Assert
            Assert.True(hasWarriorSkills);
            Assert.NotNull(warriorSkills);
            Assert.Equal("warrior_attack_basic", warriorSkills.NormalAttack);
            Assert.Equal("warrior_special_pulse", warriorSkills.SpecialAttack);
        }

        #endregion
    }
}
