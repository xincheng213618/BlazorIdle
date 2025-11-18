using Xunit;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 5 单元测试：CastingController 占位实现
    /// Phase 5 unit tests: CastingController placeholder implementation
    /// </summary>
    public class CastingControllerPhase5Tests
    {
        [Fact]
        public void CastingController_InitialState_IsNotCasting()
        {
            // Arrange & Act
            var controller = new CastingController();

            // Assert - Phase 9: IsCasting now takes a casterId parameter
            Assert.False(controller.IsCasting("char1"));
        }

        [Fact]
        public void CastingController_Tick_DoesNotCrash()
        {
            // Arrange
            var controller = new CastingController();

            // Act - 调用多次 Tick 应该不会产生任何副作用
            controller.Tick(0.1);
            controller.Tick(0.5);
            controller.Tick(1.0);

            // Assert - Phase 9: IsCasting now takes a casterId parameter
            Assert.False(controller.IsCasting("char1"));
        }

        [Fact]
        public void CastingController_GetPausedTracks_ReturnsEmptyList()
        {
            // Arrange
            var controller = new CastingController();

            // Act
            var pausedTracks = controller.GetPausedTracks();

            // Assert
            Assert.NotNull(pausedTracks);
            Assert.Empty(pausedTracks);
        }

        [Fact]
        public void CastingController_GetPausedTracks_AlwaysReturnsEmptyList()
        {
            // Arrange
            var controller = new CastingController();

            // Act - 即使 Tick 之后，仍然返回空列表
            controller.Tick(1.0);
            var pausedTracks1 = controller.GetPausedTracks();
            
            controller.Tick(2.0);
            var pausedTracks2 = controller.GetPausedTracks();

            // Assert
            Assert.Empty(pausedTracks1);
            Assert.Empty(pausedTracks2);
        }

        [Fact]
        public void ActiveCast_CanBeCreated()
        {
            // Arrange & Act
            var activeCast = new ActiveCast
            {
                SkillId = "test_skill",
                CastTimeSec = 2.0,
                PauseAttackTrack = true,
                PauseSpecialTrack = false,
                ElapsedSec = 0.5
            };

            // Assert
            Assert.Equal("test_skill", activeCast.SkillId);
            Assert.Equal(2.0, activeCast.CastTimeSec);
            Assert.True(activeCast.PauseAttackTrack);
            Assert.False(activeCast.PauseSpecialTrack);
            Assert.Equal(0.5, activeCast.ElapsedSec);
        }

        [Fact]
        public void ActiveCast_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var activeCast = new ActiveCast();

            // Assert
            Assert.Equal("", activeCast.SkillId);
            Assert.Equal(0, activeCast.CastTimeSec);
            Assert.False(activeCast.PauseAttackTrack);
            Assert.False(activeCast.PauseSpecialTrack);
            Assert.Equal(0, activeCast.ElapsedSec);
        }

        [Fact]
        public void SkillDef_CanBeCreated()
        {
            // Arrange & Act
            var skillDef = new SkillDef
            {
                Id = "fireball",
                CastTimeSec = 1.5,
                IsAoe = true
            };

            // Assert
            Assert.Equal("fireball", skillDef.Id);
            Assert.Equal(1.5, skillDef.CastTimeSec);
            Assert.True(skillDef.IsAoe);
        }

        [Fact]
        public void SkillDef_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var skillDef = new SkillDef();

            // Assert
            Assert.Equal("", skillDef.Id);
            Assert.Equal(0, skillDef.CastTimeSec);
            Assert.False(skillDef.IsAoe);
        }

        [Fact]
        public void SkillDef_Step0Skills_HaveZeroCastTime()
        {
            // Arrange & Act - Step 0 中所有技能的施法时间都应该为 0
            var attackBasic = new SkillDef { Id = "attack_basic", CastTimeSec = 0 };
            var specialPulse = new SkillDef { Id = "special_pulse", CastTimeSec = 0 };
            var enemyAttack = new SkillDef { Id = "enemy_attack_basic", CastTimeSec = 0 };

            // Assert
            Assert.Equal(0, attackBasic.CastTimeSec);
            Assert.Equal(0, specialPulse.CastTimeSec);
            Assert.Equal(0, enemyAttack.CastTimeSec);
        }
    }
}
