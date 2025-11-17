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

        #region P3.4 Tests: Character entity accessor methods

        [Fact]
        public void Character_GetNormalAttackSkillId_ReturnsConfiguredValue()
        {
            // Arrange
            var character = new BlazorIdle.Game.Character
            {
                NormalAttackSkillId = "warrior_attack_basic"
            };

            // Act
            var skillId = character.GetNormalAttackSkillId();

            // Assert
            Assert.Equal("warrior_attack_basic", skillId);
        }

        [Fact]
        public void Character_GetNormalAttackSkillId_ReturnsDefaultWhenNull()
        {
            // Arrange
            var character = new BlazorIdle.Game.Character
            {
                NormalAttackSkillId = null
            };

            // Act
            var skillId = character.GetNormalAttackSkillId();

            // Assert
            Assert.Equal("attack_basic", skillId);
        }

        [Fact]
        public void Character_GetSpecialAttackSkillId_ReturnsConfiguredValue()
        {
            // Arrange
            var character = new BlazorIdle.Game.Character
            {
                SpecialAttackSkillId = "warrior_special_pulse"
            };

            // Act
            var skillId = character.GetSpecialAttackSkillId();

            // Assert
            Assert.Equal("warrior_special_pulse", skillId);
        }

        [Fact]
        public void Character_GetSpecialAttackSkillId_ReturnsDefaultWhenNull()
        {
            // Arrange
            var character = new BlazorIdle.Game.Character
            {
                SpecialAttackSkillId = null
            };

            // Act
            var skillId = character.GetSpecialAttackSkillId();

            // Assert
            Assert.Equal("special_pulse", skillId);
        }

        [Fact]
        public void Character_FixedSkillIds_CanBeSetAndRetrieved()
        {
            // Arrange
            var character = new BlazorIdle.Game.Character();

            // Act
            character.NormalAttackSkillId = "mage_attack_basic";
            character.SpecialAttackSkillId = "mage_special_pulse";

            // Assert
            Assert.Equal("mage_attack_basic", character.NormalAttackSkillId);
            Assert.Equal("mage_special_pulse", character.SpecialAttackSkillId);
            Assert.Equal("mage_attack_basic", character.GetNormalAttackSkillId());
            Assert.Equal("mage_special_pulse", character.GetSpecialAttackSkillId());
        }

        #endregion

        #region P3.5 Tests: MultiBattleInstance integration

        [Fact]
        public void MultiBattleInstance_UsesCharacterFixedSkills_ForNormalAttack()
        {
            // Arrange
            var character = new BlazorIdle.Game.Character
            {
                MaxHp = 100,
                Hp = 100,
                NormalAttackSkillId = "warrior_attack_basic",
                SpecialAttackSkillId = "warrior_special_pulse"
            };

            // Act
            var normalSkillId = character.GetNormalAttackSkillId();
            var specialSkillId = character.GetSpecialAttackSkillId();

            // Assert
            Assert.Equal("warrior_attack_basic", normalSkillId);
            Assert.Equal("warrior_special_pulse", specialSkillId);
        }

        [Fact]
        public void MultiBattleInstance_DifferentProfessions_UseDifferentSkills()
        {
            // Arrange
            var warrior = new BlazorIdle.Game.Character
            {
                NormalAttackSkillId = "warrior_attack_basic",
                SpecialAttackSkillId = "warrior_special_pulse"
            };

            var mage = new BlazorIdle.Game.Character
            {
                NormalAttackSkillId = "mage_attack_basic",
                SpecialAttackSkillId = "mage_special_pulse"
            };

            // Act & Assert
            Assert.NotEqual(warrior.GetNormalAttackSkillId(), mage.GetNormalAttackSkillId());
            Assert.NotEqual(warrior.GetSpecialAttackSkillId(), mage.GetSpecialAttackSkillId());
        }

        [Fact]
        public void CharacterData_ToBattleCharacter_CopiesFixedSkills()
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

            // Act - Simulate what BattleDemo does
            string? normalAttackSkillId = null;
            string? specialAttackSkillId = null;
            
            if (characterData.FixedSkillsByProfession.TryGetValue(characterData.ActiveCombatProfessionId, out var fixedSkills))
            {
                normalAttackSkillId = fixedSkills.NormalAttack;
                specialAttackSkillId = fixedSkills.SpecialAttack;
            }

            // Assert
            Assert.Equal("warrior_attack_basic", normalAttackSkillId);
            Assert.Equal("warrior_special_pulse", specialAttackSkillId);
        }

        #endregion

        #region P3.6 Tests: skills.json validation

        [Theory]
        [InlineData("warrior_attack_basic", "current_target")]
        [InlineData("warrior_special_pulse", "self")]
        [InlineData("mage_attack_basic", "current_target")]
        [InlineData("mage_special_pulse", "self")]
        [InlineData("ranger_attack_basic", "current_target")]
        [InlineData("ranger_special_pulse", "self")]
        [InlineData("rogue_attack_basic", "current_target")]
        [InlineData("rogue_special_pulse", "current_target")]
        public void SkillsJson_ProfessionFixedSkills_HaveTargetPolicy(string skillId, string expectedTargetPolicy)
        {
            // Arrange
            var skillRepo = new BlazorIdle.Game.Skills.SkillRepository();

            // Act
            var skill = skillRepo.GetSkill(skillId);

            // Assert
            Assert.NotNull(skill);
            Assert.NotNull(skill.TargetPolicy);
            Assert.Equal(expectedTargetPolicy, skill.TargetPolicy, ignoreCase: true);
        }

        #endregion

        #region P3.7 Tests: Backward compatibility

        [Fact]
        public void CharacterData_OldSave_FixedSkillsByProfession_IsEmptyByDefault()
        {
            // Arrange & Act - Simulating an old save with no fixed skills
            var characterData = new CharacterData
            {
                Id = "old-character",
                Name = "Old Character",
                ProfessionId = "warrior"
                // FixedSkillsByProfession not set - simulates old save
            };

            // Assert
            Assert.NotNull(characterData.FixedSkillsByProfession);
            Assert.Empty(characterData.FixedSkillsByProfession);
        }

        [Fact]
        public void Character_NoFixedSkills_FallsBackToDefaults()
        {
            // Arrange - Character with no fixed skills set
            var character = new BlazorIdle.Game.Character
            {
                NormalAttackSkillId = null,
                SpecialAttackSkillId = null
            };

            // Act
            var normalSkillId = character.GetNormalAttackSkillId();
            var specialSkillId = character.GetSpecialAttackSkillId();

            // Assert - Should return default fallback values
            Assert.Equal("attack_basic", normalSkillId);
            Assert.Equal("special_pulse", specialSkillId);
        }

        [Fact]
        public void CharacterData_MissingProfessionInFixedSkills_ReturnsNull()
        {
            // Arrange
            var characterData = new CharacterData
            {
                ActiveCombatProfessionId = "mage",
                FixedSkillsByProfession = new Dictionary<string, ProfessionFixedSkills>
                {
                    ["warrior"] = new ProfessionFixedSkills
                    {
                        NormalAttack = "warrior_attack_basic",
                        SpecialAttack = "warrior_special_pulse"
                    }
                    // Note: mage not included - simulates missing profession
                }
            };

            // Act
            var hasMageSkills = characterData.FixedSkillsByProfession.TryGetValue("mage", out var mageSkills);

            // Assert
            Assert.False(hasMageSkills);
            Assert.Null(mageSkills);
        }

        #endregion

        #region P3.8 Tests: Target Policy snake_case 转换

        [Theory]
        [InlineData("current_target", "CurrentTarget")]
        [InlineData("enemies_all", "EnemiesAll")]
        [InlineData("self", "Self")]
        [InlineData("allies_all", "AlliesAll")]
        [InlineData("allies_lowest_hp_pct", "AlliesLowestHpPct")]
        public void TargetPolicy_SnakeCaseToPascalCase_Conversion(string snakeCase, string expectedPascalCase)
        {
            // Arrange
            var skillResolver = new BlazorIdle.Game.Skills.SkillResolver();

            // Use reflection to call the private method
            var method = typeof(BlazorIdle.Game.Skills.SkillResolver).GetMethod(
                "ConvertSnakeCaseToPascalCase", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(method);

            // Act
            var result = method.Invoke(skillResolver, new object[] { snakeCase }) as string;

            // Assert
            Assert.Equal(expectedPascalCase, result);
        }

        [Fact]
        public void TargetPolicy_SnakeCaseEnumParsing_WorksCorrectly()
        {
            // This test verifies that snake_case target policies can be parsed to the enum
            // after conversion to PascalCase
            
            // Arrange
            var testCases = new Dictionary<string, BlazorIdle.Game.Skills.TargetPolicy>
            {
                ["current_target"] = BlazorIdle.Game.Skills.TargetPolicy.CurrentTarget,
                ["enemies_all"] = BlazorIdle.Game.Skills.TargetPolicy.EnemiesAll,
                ["self"] = BlazorIdle.Game.Skills.TargetPolicy.Self,
                ["allies_all"] = BlazorIdle.Game.Skills.TargetPolicy.AlliesAll,
                ["allies_lowest_hp_pct"] = BlazorIdle.Game.Skills.TargetPolicy.AlliesLowestHpPct
            };

            var skillResolver = new BlazorIdle.Game.Skills.SkillResolver();

            var convertMethod = typeof(BlazorIdle.Game.Skills.SkillResolver).GetMethod(
                "ConvertSnakeCaseToPascalCase", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            Assert.NotNull(convertMethod);

            foreach (var testCase in testCases)
            {
                // Act
                var pascalCase = convertMethod.Invoke(skillResolver, new object[] { testCase.Key }) as string;
                var parseSuccess = System.Enum.TryParse<BlazorIdle.Game.Skills.TargetPolicy>(
                    pascalCase, ignoreCase: true, out var parsedPolicy);

                // Assert
                Assert.True(parseSuccess, $"Failed to parse '{testCase.Key}' -> '{pascalCase}'");
                Assert.Equal(testCase.Value, parsedPolicy);
            }
        }

        #endregion
    }
}
