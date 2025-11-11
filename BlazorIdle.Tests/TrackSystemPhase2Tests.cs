using Xunit;
using BlazorIdle.Game.Tracks;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 2 单元测试：Track 抽象接口
    /// Phase 2 unit tests: Track abstract interface
    /// </summary>
    public class TrackSystemPhase2Tests
    {
        [Fact]
        public void ProgressPolicy_HasCorrectValues()
        {
            // Act & Assert
            Assert.Equal(0, (int)ProgressPolicy.Presence);
            Assert.Equal(1, (int)ProgressPolicy.Encounter);
        }

        [Fact]
        public void TrackConfig_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var config = new TrackConfig();

            // Assert
            Assert.NotNull(config.BoundSkills);
            Assert.Empty(config.BoundSkills);
            Assert.NotNull(config.OnFireTriggers);
            Assert.Empty(config.OnFireTriggers);
            Assert.Equal(ProgressPolicy.Presence, config.ProgressPolicy);
        }

        [Fact]
        public void TrackConfig_CanSetProperties()
        {
            // Arrange & Act
            var config = new TrackConfig
            {
                BoundSkills = new List<string> { "attack_basic", "skill_1" },
                OnFireTriggers = new List<string> { "trigger_1" },
                ProgressPolicy = ProgressPolicy.Encounter
            };

            // Assert
            Assert.Equal(2, config.BoundSkills.Count);
            Assert.Equal("attack_basic", config.BoundSkills[0]);
            Assert.Equal("skill_1", config.BoundSkills[1]);
            Assert.Single(config.OnFireTriggers);
            Assert.Equal("trigger_1", config.OnFireTriggers[0]);
            Assert.Equal(ProgressPolicy.Encounter, config.ProgressPolicy);
        }

        [Fact]
        public void TrackConfig_AttackTrack_Configuration()
        {
            // Arrange & Act - 模拟 Attack Track 配置
            var attackConfig = new TrackConfig
            {
                BoundSkills = new List<string> { "attack_basic" },
                OnFireTriggers = new List<string>(),
                ProgressPolicy = ProgressPolicy.Presence
            };

            // Assert
            Assert.Single(attackConfig.BoundSkills);
            Assert.Equal("attack_basic", attackConfig.BoundSkills[0]);
            Assert.Empty(attackConfig.OnFireTriggers);
            Assert.Equal(ProgressPolicy.Presence, attackConfig.ProgressPolicy);
        }

        [Fact]
        public void TrackConfig_SpecialTrack_Configuration()
        {
            // Arrange & Act - 模拟 Special Track 配置
            var specialConfig = new TrackConfig
            {
                BoundSkills = new List<string> { "special_pulse" },
                OnFireTriggers = new List<string>(),
                ProgressPolicy = ProgressPolicy.Presence
            };

            // Assert
            Assert.Single(specialConfig.BoundSkills);
            Assert.Equal("special_pulse", specialConfig.BoundSkills[0]);
            Assert.Empty(specialConfig.OnFireTriggers);
            Assert.Equal(ProgressPolicy.Presence, specialConfig.ProgressPolicy);
        }

        [Fact]
        public void TrackConfig_EnemyAttackTrack_Configuration()
        {
            // Arrange & Act - 模拟 Enemy Attack Track 配置
            var enemyConfig = new TrackConfig
            {
                BoundSkills = new List<string> { "enemy_attack_basic" },
                OnFireTriggers = new List<string>(),
                ProgressPolicy = ProgressPolicy.Presence
            };

            // Assert
            Assert.Single(enemyConfig.BoundSkills);
            Assert.Equal("enemy_attack_basic", enemyConfig.BoundSkills[0]);
            Assert.Empty(enemyConfig.OnFireTriggers);
            Assert.Equal(ProgressPolicy.Presence, enemyConfig.ProgressPolicy);
        }


    }
}
