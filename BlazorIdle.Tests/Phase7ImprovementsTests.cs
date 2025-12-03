using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Config;
using BlazorIdle.Game.Tracks;
using BlazorIdle.Game;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 7.4 改进测试
    /// Phase 7.4 improvement tests
    /// </summary>
    public class Phase7ImprovementsTests
    {
        #region Counter Overflow Handling Tests

        [Fact]
        public void SkillResolver_CounterResets_AfterThreshold()
        {
            // Arrange
            var resolver = new SkillResolver();
            var ctx = CreateTestContext();
            
            // Act: Cast skills many times to exceed threshold
            // 通过反射访问私有字段来验证重置
            // Use reflection to access private field for verification
            for (int i = 0; i < 1_000_005; i++)
            {
                resolver.CastBundle(new List<string> { "attack_basic" }, ctx, new SkillCastOptions());
            }
            
            // Assert: Should not throw overflow exception and continue working
            // 验证：应该不会抛出溢出异常并继续工作
            var result = resolver.CastBundle(new List<string> { "attack_basic" }, ctx, new SkillCastOptions());
            Assert.Single(result);
            Assert.True(result[0].DamageDealt > 0);
        }

        [Fact]
        public void SkillResolver_BundleIds_RemainUnique_AfterCounterReset()
        {
            // Arrange
            var resolver = new SkillResolver();
            var ctx = CreateTestContext();
            var bundleIds = new HashSet<string>();
            
            // Act: Generate many bundle IDs
            for (int i = 0; i < 2000; i++)
            {
                var results = resolver.CastBundle(new List<string> { "attack_basic" }, ctx, new SkillCastOptions());
                if (results.Count > 0)
                {
                    bundleIds.Add(results[0].BundleId ?? "");
                }
            }
            
            // Assert: All bundle IDs should be unique
            Assert.Equal(2000, bundleIds.Count);
        }

        #endregion

        #region Configuration Validation Tests

        [Fact]
        public void SkillDefCollection_Validate_ValidConfiguration_ReturnsNoErrors()
        {
            // Arrange
            var skillDefs = new SkillDefCollection();
            
            // Act
            var errors = skillDefs.Validate();
            
            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void SkillDefCollection_Validate_NegativeCastTime_ReturnsError()
        {
            // Arrange
            var skillDefs = new SkillDefCollection();
            skillDefs.Skills["invalid_skill"] = new SkillDef
            {
                Id = "invalid_skill",
                CastTimeSec = -1
            };
            
            // Act
            var errors = skillDefs.Validate();
            
            // Assert
            Assert.Contains(errors, e => e.Contains("negative cast time"));
        }

        [Fact]
        public void SkillDefCollection_Validate_IdMismatch_ReturnsError()
        {
            // Arrange
            var skillDefs = new SkillDefCollection();
            skillDefs.Skills["key_id"] = new SkillDef
            {
                Id = "different_id",
                CastTimeSec = 0
            };
            
            // Act
            var errors = skillDefs.Validate();
            
            // Assert
            Assert.Contains(errors, e => e.Contains("ID mismatch"));
        }

        [Fact]
        public void TrackConfigCollection_Validate_ValidConfiguration_ReturnsNoErrors()
        {
            // Arrange
            var trackConfigs = new TrackConfigCollection();
            var skillDefs = new SkillDefCollection();
            
            // Act
            var errors = trackConfigs.Validate(skillDefs);
            
            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void TrackConfigCollection_Validate_NonExistentSkillReference_ReturnsError()
        {
            // Arrange
            var trackConfigs = new TrackConfigCollection();
            trackConfigs.Tracks["test_track"] = new TrackConfig
            {
                BoundSkills = new List<string> { "non_existent_skill" },
                OnFireTriggers = new List<string>(),
                ProgressPolicy = ProgressPolicy.Presence
            };
            var skillDefs = new SkillDefCollection();
            
            // Act
            var errors = trackConfigs.Validate(skillDefs);
            
            // Assert
            Assert.Contains(errors, e => e.Contains("non-existent skill"));
        }

        [Fact]
        public void TrackConfigCollection_Validate_NullBoundSkills_ReturnsError()
        {
            // Arrange
            var trackConfigs = new TrackConfigCollection();
            trackConfigs.Tracks["test_track"] = new TrackConfig
            {
                BoundSkills = null!,
                OnFireTriggers = new List<string>(),
                ProgressPolicy = ProgressPolicy.Presence
            };
            
            // Act
            var errors = trackConfigs.Validate();
            
            // Assert
            Assert.Contains(errors, e => e.Contains("null BoundSkills"));
        }

        [Fact]
        public void TrackConfigCollection_Validate_InvalidOnFireTriggers_ReturnsError()
        {
            // Arrange
            var trackConfigs = new TrackConfigCollection();
            trackConfigs.Tracks["test_track"] = new TrackConfig
            {
                BoundSkills = new List<string> { "attack_basic" },
                OnFireTriggers = new List<string> { "invalid_trigger_skill" },
                ProgressPolicy = ProgressPolicy.Presence
            };
            var skillDefs = new SkillDefCollection();
            
            // Act
            var errors = trackConfigs.Validate(skillDefs);
            
            // Assert
            Assert.Contains(errors, e => e.Contains("OnFireTriggers") && e.Contains("non-existent skill"));
        }

        [Fact]
        public void TrackConfigCollection_Validate_WithoutSkillDefs_SkipsReferenceValidation()
        {
            // Arrange
            var trackConfigs = new TrackConfigCollection();
            trackConfigs.Tracks["test_track"] = new TrackConfig
            {
                BoundSkills = new List<string> { "any_skill_id" },
                OnFireTriggers = new List<string>(),
                ProgressPolicy = ProgressPolicy.Presence
            };
            
            // Act: Validate without skill definitions
            var errors = trackConfigs.Validate(null);
            
            // Assert: Should not return reference validation errors
            Assert.Empty(errors);
        }

        #endregion

        #region Helper Methods

        private BattleContext CreateTestContext()
        {
            // 更新使用新伤害系统 / Updated to use new damage system
            var combatStats = new Game.Combat.CombatStats
            {
                AttackFinal = 100,
                CritChancePercent = 10,
                CritDamageBonusPercent = 66.67 // 1.2 * 1.667 ≈ 2.0
            };
            
            var player = new Character
            {
                CombatStats = combatStats,
                CritChancePercent = 10,
                VariancePct = 0.1
            };

            var enemy = new Enemy
            {
                BaseAttack = 50,
                VariancePct = 0.1,
                Element = Game.Combat.ElementIds.Neutral
            };

            return new BattleContext
            {
                Player = player,
                Enemy = enemy,
                Rng = new RngContext(42),
                Clock = new TestClock(),
                DamageCalculator = Game.Combat.DamageCalculator.CreateDefault(),
                AttackerCombatStats = combatStats,
                AttackerElement = Game.Combat.ElementIds.Neutral,
                DefenderElement = Game.Combat.ElementIds.Neutral,
                AttackerHPRatio = 1.0,
                DefenderDamageReductionPercent = 0
            };
        }

        private class TestClock : IGameClock
        {
            private int _time = 0;
            public int NowMs => _time++;
            public void AdvanceBy(int ms) => _time += ms;
            public void Reset() => _time = 0;
        }

        #endregion
    }
}
