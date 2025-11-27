using System;
using System.Linq;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Shared.Models;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 消耗品系统 Phase 2 测试 - 配置文件
    /// Consumable system Phase 2 tests - Configuration files
    /// </summary>
    public class ConsumableSystemPhase2Tests
    {
        #region ConsumableSkills.json Tests

        [Fact]
        public void SkillRepository_LoadsConsumableSkills()
        {
            // Arrange
            var repository = new SkillRepository();

            // Act & Assert - Check that consumable skills were loaded
            Assert.NotNull(repository.GetSkill("consumable_strength_potion"));
            Assert.NotNull(repository.GetSkill("consumable_speed_potion"));
            Assert.NotNull(repository.GetSkill("consumable_health_potion"));
            Assert.NotNull(repository.GetSkill("consumable_apple"));
            Assert.NotNull(repository.GetSkill("consumable_bread"));
            Assert.NotNull(repository.GetSkill("consumable_cheese"));
            Assert.NotNull(repository.GetSkill("consumable_meat"));
            Assert.NotNull(repository.GetSkill("consumable_steak"));
        }

        [Fact]
        public void ConsumableSkill_StrengthPotion_HasCorrectProperties()
        {
            // Arrange
            var repository = new SkillRepository();

            // Act
            var skill = repository.GetSkill("consumable_strength_potion");

            // Assert
            Assert.NotNull(skill);
            Assert.Equal("consumable_strength_potion", skill.Id);
            Assert.Equal("consumable", skill.Type);
            Assert.Equal("self", skill.TargetPolicy?.ToLowerInvariant());
            Assert.NotNull(skill.OnCastBuffs);
            Assert.Contains(skill.OnCastBuffs, b => b.BuffConfigId == "consumable_strength_buff");
        }

        [Fact]
        public void ConsumableSkill_HealthPotion_HasInstantHeal()
        {
            // Arrange
            var repository = new SkillRepository();

            // Act
            var skill = repository.GetSkill("consumable_health_potion");

            // Assert
            Assert.NotNull(skill);
            Assert.Equal(50, skill.InstantHeal);
        }

        [Fact]
        public void ConsumableSkill_Apple_HasInstantHeal()
        {
            // Arrange
            var repository = new SkillRepository();

            // Act
            var skill = repository.GetSkill("consumable_apple");

            // Assert
            Assert.NotNull(skill);
            Assert.Equal(15, skill.InstantHeal);
        }

        [Fact]
        public void ConsumableSkill_Steak_HasHealAndBuff()
        {
            // Arrange
            var repository = new SkillRepository();

            // Act
            var skill = repository.GetSkill("consumable_steak");

            // Assert
            Assert.NotNull(skill);
            Assert.Equal(80, skill.InstantHeal);
            Assert.NotNull(skill.OnCastBuffs);
            Assert.Contains(skill.OnCastBuffs, b => b.BuffConfigId == "consumable_steak_buff");
        }

        #endregion

        #region Items.json ConsumableConfig Tests

        [Fact]
        public void ItemWithConsumableConfig_Potion_CorrectlyIdentified()
        {
            // Arrange - Test ConsumableConfig for potion
            var item = new ItemDefinition
            {
                Id = "test_potion",
                Name = "Test Potion",
                Type = ItemType.Consumable,
                ConsumableConfig = new ConsumableConfig
                {
                    Category = "potion",
                    SkillId = "consumable_strength_potion",
                    CooldownSec = 120.0
                }
            };

            // Assert
            Assert.NotNull(item.ConsumableConfig);
            Assert.Equal("potion", item.ConsumableConfig.Category);
            Assert.True(item.ConsumableConfig.IsPotion);
            Assert.False(item.ConsumableConfig.IsFood);
        }

        [Fact]
        public void ItemWithConsumableConfig_Food_CorrectlyIdentified()
        {
            // Arrange - Test ConsumableConfig for food
            var config = new ConsumableConfig
            {
                Category = "food",
                SkillId = "consumable_apple",
                CooldownSec = 15.0
            };

            // Assert
            Assert.True(config.IsFood);
            Assert.False(config.IsPotion);
        }

        [Fact]
        public void ConsumableConfig_TriggerConditions_CanBeSet()
        {
            // Arrange
            var config = new ConsumableConfig
            {
                Category = "food",
                SkillId = "consumable_health_potion",
                CooldownSec = 30.0,
                TriggerConditions = new SkillConditions
                {
                    HpBelowPct = 50.0
                }
            };

            // Assert
            Assert.NotNull(config.TriggerConditions);
            Assert.Equal(50.0, config.TriggerConditions.HpBelowPct);
        }

        #endregion

        #region Buffs.json Consumable Buff Tests

        [Fact]
        public void BuffRepository_LoadsConsumableBuffs()
        {
            // Arrange - Use singleton instance
            var repository = BuffRepository.Instance;

            // Act & Assert - Check consumable buffs were loaded
            Assert.NotNull(repository.GetBuffById("consumable_strength_buff"));
            Assert.NotNull(repository.GetBuffById("consumable_speed_buff"));
            Assert.NotNull(repository.GetBuffById("consumable_steak_buff"));
        }

        [Fact]
        public void ConsumableBuff_Strength_HasCorrectProperties()
        {
            // Arrange
            var repository = BuffRepository.Instance;

            // Act
            var buff = repository.GetBuffById("consumable_strength_buff");

            // Assert
            Assert.NotNull(buff);
            Assert.Equal("consumable_strength_buff", buff.Id);
            Assert.Equal(60.0, buff.DurationSec);
            Assert.NotEmpty(buff.Effects);
        }

        [Fact]
        public void ConsumableBuff_Speed_HasCorrectProperties()
        {
            // Arrange
            var repository = BuffRepository.Instance;

            // Act
            var buff = repository.GetBuffById("consumable_speed_buff");

            // Assert
            Assert.NotNull(buff);
            Assert.Equal("consumable_speed_buff", buff.Id);
            Assert.Equal(60.0, buff.DurationSec);
            Assert.NotEmpty(buff.Effects);
        }

        [Fact]
        public void ConsumableBuff_Steak_HasCorrectProperties()
        {
            // Arrange
            var repository = BuffRepository.Instance;

            // Act
            var buff = repository.GetBuffById("consumable_steak_buff");

            // Assert
            Assert.NotNull(buff);
            Assert.Equal("consumable_steak_buff", buff.Id);
            Assert.Equal(30.0, buff.DurationSec);
            Assert.NotEmpty(buff.Effects);
        }

        #endregion
    }
}
