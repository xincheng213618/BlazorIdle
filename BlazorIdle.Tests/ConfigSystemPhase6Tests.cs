using Xunit;
using BlazorIdle.Game.Config;
using BlazorIdle.Game.Tracks;
using System.Linq;
using System.Text.Json;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 6 单元测试：配置系统
    /// Phase 6 unit tests: Configuration system
    /// </summary>
    public class ConfigSystemPhase6Tests
    {
        [Fact]
        public void CombatConfig_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var config = new CombatConfig();

            // Assert
            Assert.False(config.UseChargeTracks); // Step 0 中不使用充能轨道
            Assert.True(config.EmitCastEvents);   // 默认发送事件便于调试
            Assert.Equal(20, config.MaxTriggersPerTick); // 防极端情况
        }

        [Fact]
        public void CombatConfig_CanSetProperties()
        {
            // Arrange & Act
            var config = new CombatConfig
            {
                UseChargeTracks = true,
                EmitCastEvents = false,
                MaxTriggersPerTick = 30
            };

            // Assert
            Assert.True(config.UseChargeTracks);
            Assert.False(config.EmitCastEvents);
            Assert.Equal(30, config.MaxTriggersPerTick);
        }

        [Fact]
        public void CombatConfig_CanSerializeToJson()
        {
            // Arrange
            var config = new CombatConfig
            {
                UseChargeTracks = false,
                EmitCastEvents = true,
                MaxTriggersPerTick = 20
            };

            // Act
            var json = JsonSerializer.Serialize(config);
            var deserialized = JsonSerializer.Deserialize<CombatConfig>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(config.UseChargeTracks, deserialized.UseChargeTracks);
            Assert.Equal(config.EmitCastEvents, deserialized.EmitCastEvents);
            Assert.Equal(config.MaxTriggersPerTick, deserialized.MaxTriggersPerTick);
        }

        [Fact]
        public void TrackConfigCollection_HasCorrectTracks()
        {
            // Arrange & Act
            var collection = new TrackConfigCollection();

            // Assert
            Assert.Equal(3, collection.Tracks.Count);
            Assert.True(collection.Tracks.ContainsKey("attack"));
            Assert.True(collection.Tracks.ContainsKey("special"));
            Assert.True(collection.Tracks.ContainsKey("enemy_attack"));
        }

        [Fact]
        public void TrackConfigCollection_AttackTrack_HasCorrectConfig()
        {
            // Arrange & Act
            var collection = new TrackConfigCollection();
            var attackConfig = collection.Tracks["attack"];

            // Assert
            Assert.Single(attackConfig.BoundSkills);
            Assert.Equal("attack_basic", attackConfig.BoundSkills[0]);
            Assert.Empty(attackConfig.OnFireTriggers);
            Assert.Equal(ProgressPolicy.Presence, attackConfig.ProgressPolicy);
        }

        [Fact]
        public void TrackConfigCollection_SpecialTrack_HasCorrectConfig()
        {
            // Arrange & Act
            var collection = new TrackConfigCollection();
            var specialConfig = collection.Tracks["special"];

            // Assert
            Assert.Single(specialConfig.BoundSkills);
            Assert.Equal("special_pulse", specialConfig.BoundSkills[0]);
            Assert.Empty(specialConfig.OnFireTriggers);
            Assert.Equal(ProgressPolicy.Presence, specialConfig.ProgressPolicy);
        }

        [Fact]
        public void TrackConfigCollection_EnemyAttackTrack_HasCorrectConfig()
        {
            // Arrange & Act
            var collection = new TrackConfigCollection();
            var enemyConfig = collection.Tracks["enemy_attack"];

            // Assert
            Assert.Single(enemyConfig.BoundSkills);
            Assert.Equal("enemy_attack_basic", enemyConfig.BoundSkills[0]);
            Assert.Empty(enemyConfig.OnFireTriggers);
            Assert.Equal(ProgressPolicy.Presence, enemyConfig.ProgressPolicy);
        }

        [Fact]
        public void TrackConfigCollection_CanSerializeToJson()
        {
            // Arrange
            var collection = new TrackConfigCollection();

            // Act
            var json = JsonSerializer.Serialize(collection);
            var deserialized = JsonSerializer.Deserialize<TrackConfigCollection>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Tracks.Count);
            Assert.True(deserialized.Tracks.ContainsKey("attack"));
            Assert.True(deserialized.Tracks.ContainsKey("special"));
            Assert.True(deserialized.Tracks.ContainsKey("enemy_attack"));
        }

        [Fact]
        public void SkillDefCollection_HasCorrectSkills()
        {
            // Arrange & Act
            var collection = new SkillDefCollection();

            // Assert
            Assert.Equal(3, collection.Skills.Count);
            Assert.True(collection.Skills.ContainsKey("attack_basic"));
            Assert.True(collection.Skills.ContainsKey("special_pulse"));
            Assert.True(collection.Skills.ContainsKey("enemy_attack_basic"));
        }

        [Fact]
        public void SkillDefCollection_AttackBasic_HasCorrectDef()
        {
            // Arrange & Act
            var collection = new SkillDefCollection();
            var attackBasic = collection.Skills["attack_basic"];

            // Assert
            Assert.Equal("attack_basic", attackBasic.Id);
            Assert.Equal(0, attackBasic.CastTimeSec); // Step 0 中为 0
            // Note: AOE is determined by targetPolicy, not IsAoe property
        }

        [Fact]
        public void SkillDefCollection_SpecialPulse_HasCorrectDef()
        {
            // Arrange & Act
            var collection = new SkillDefCollection();
            var specialPulse = collection.Skills["special_pulse"];

            // Assert
            Assert.Equal("special_pulse", specialPulse.Id);
            Assert.Equal(0, specialPulse.CastTimeSec); // Step 0 中为 0
            // Note: AOE is determined by targetPolicy, not IsAoe property
        }

        [Fact]
        public void SkillDefCollection_EnemyAttackBasic_HasCorrectDef()
        {
            // Arrange & Act
            var collection = new SkillDefCollection();
            var enemyAttack = collection.Skills["enemy_attack_basic"];

            // Assert
            Assert.Equal("enemy_attack_basic", enemyAttack.Id);
            Assert.Equal(0, enemyAttack.CastTimeSec); // Step 0 中为 0
            // Note: AOE is determined by targetPolicy, not IsAoe property
        }

        [Fact]
        public void SkillDefCollection_CanSerializeToJson()
        {
            // Arrange
            var collection = new SkillDefCollection();

            // Act
            var json = JsonSerializer.Serialize(collection);
            var deserialized = JsonSerializer.Deserialize<SkillDefCollection>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Skills.Count);
            Assert.True(deserialized.Skills.ContainsKey("attack_basic"));
            Assert.True(deserialized.Skills.ContainsKey("special_pulse"));
            Assert.True(deserialized.Skills.ContainsKey("enemy_attack_basic"));
        }

        [Fact]
        public void SkillDefCollection_AllSkills_HaveZeroCastTime()
        {
            // Arrange & Act
            var collection = new SkillDefCollection();

            // Assert - Step 0 中所有技能的施法时间都应该为 0
            Assert.All(collection.Skills.Values, skill => 
                Assert.Equal(0, skill.CastTimeSec)
            );
        }
    }
}
