using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 8: 施法技能集成测试
    /// Phase 8: Cast skill integration tests
    /// </summary>
    public class Step2Phase8Tests
    {
        #region 施法开始测试 (Cast Start Tests) - 3 个

        [Fact]
        public void StartCast_BasicFlow_ShouldSucceed()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";
            string skillId = "mage_pyroblast";
            double castTime = 2.0;

            // Act
            bool result = controller.StartCast(casterId, skillId, castTime);

            // Assert
            Assert.True(result);
            Assert.True(controller.IsCastingForCaster(casterId));
            var activeCast = controller.GetActiveCast(casterId);
            Assert.NotNull(activeCast);
            Assert.Equal(skillId, activeCast.SkillId);
            Assert.Equal(castTime, activeCast.CastTimeSec);
            Assert.Equal(0, activeCast.ElapsedSec);
        }

        [Fact]
        public void StartCast_WithHaste_ShouldReduceCastTime()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";
            string skillId = "mage_pyroblast";
            double baseCastTime = 2.0;
            double hastePercent = 25.0; // 25% haste

            // Act
            controller.StartCast(casterId, skillId, baseCastTime, hastePercent);

            // Assert
            var activeCast = controller.GetActiveCast(casterId);
            Assert.NotNull(activeCast);
            // 2.0 / (1 + 0.25) = 2.0 / 1.25 = 1.6
            Assert.Equal(1.6, activeCast.CastTimeSec);
        }

        [Fact]
        public void StartCast_MultipleCharacters_ShouldCastIndependently()
        {
            // Arrange
            var controller = new CastingController();
            string caster1 = "player1";
            string caster2 = "player2";
            string skill1 = "mage_pyroblast";
            string skill2 = "mage_frostbolt";

            // Act
            controller.StartCast(caster1, skill1, 2.0);
            controller.StartCast(caster2, skill2, 1.5);

            // Assert
            Assert.True(controller.IsCastingForCaster(caster1));
            Assert.True(controller.IsCastingForCaster(caster2));
            Assert.Equal(skill1, controller.GetActiveCast(caster1)?.SkillId);
            Assert.Equal(skill2, controller.GetActiveCast(caster2)?.SkillId);
        }

        #endregion

        #region 施法进度测试 (Cast Progress Tests) - 3 个

        [Fact]
        public void Tick_ShouldAdvanceCastProgress()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";
            string skillId = "mage_pyroblast";
            double castTime = 2.0;
            controller.StartCast(casterId, skillId, castTime);

            // Act
            controller.Tick(0.5); // 推进 0.5 秒

            // Assert
            var activeCast = controller.GetActiveCast(casterId);
            Assert.NotNull(activeCast);
            Assert.Equal(0.5, activeCast.ElapsedSec);
            Assert.Equal(0.25, controller.GetCastProgress(casterId)); // 0.5 / 2.0 = 0.25
            Assert.Equal(1.5, controller.GetRemainingCastTime(casterId)); // 2.0 - 0.5 = 1.5
        }

        [Fact]
        public void Tick_MultipleSteps_ShouldAccumulateProgress()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";
            controller.StartCast(casterId, "test_skill", 3.0);

            // Act
            controller.Tick(1.0);
            controller.Tick(0.5);
            controller.Tick(0.5);

            // Assert
            var activeCast = controller.GetActiveCast(casterId);
            Assert.NotNull(activeCast);
            Assert.Equal(2.0, activeCast.ElapsedSec);
            Assert.Equal(1.0, controller.GetRemainingCastTime(casterId));
        }

        [Fact]
        public void Tick_WithHaste_ShouldCompleteEarlier()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";
            double baseCastTime = 2.0;
            double hastePercent = 100.0; // 100% haste = 2x speed, so 1.0s cast time
            controller.StartCast(casterId, "test_skill", baseCastTime, hastePercent);

            bool completed = false;
            controller.OnCastComplete += (caster, skill) => completed = true;

            // Act
            controller.Tick(0.9);

            // Assert - 未完成
            Assert.False(completed);
            Assert.True(controller.IsCastingForCaster(casterId));

            // Act
            controller.Tick(0.2); // 总共 1.1 秒，应该完成

            // Assert - 已完成
            Assert.True(completed);
            Assert.False(controller.IsCastingForCaster(casterId));
        }

        #endregion

        #region 施法完成测试 (Cast Complete Tests) - 3 个

        [Fact]
        public void CastComplete_ShouldTriggerEvent()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";
            string skillId = "mage_pyroblast";
            controller.StartCast(casterId, skillId, 1.0);

            string? completedCaster = null;
            string? completedSkill = null;
            controller.OnCastComplete += (caster, skill) =>
            {
                completedCaster = caster;
                completedSkill = skill;
            };

            // Act
            controller.Tick(1.5); // 超过施法时间

            // Assert
            Assert.Equal(casterId, completedCaster);
            Assert.Equal(skillId, completedSkill);
            Assert.False(controller.IsCastingForCaster(casterId));
        }

        [Fact]
        public void CastComplete_ShouldClearActiveCast()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";
            controller.StartCast(casterId, "test_skill", 1.0);

            // Act
            controller.Tick(1.5);

            // Assert
            Assert.False(controller.IsCastingForCaster(casterId));
            Assert.Null(controller.GetActiveCast(casterId));
        }

        [Fact]
        public void CastComplete_MultipleCharacters_ShouldCompleteIndependently()
        {
            // Arrange
            var controller = new CastingController();
            string caster1 = "player1";
            string caster2 = "player2";
            controller.StartCast(caster1, "skill1", 1.0);
            controller.StartCast(caster2, "skill2", 2.0);

            int completedCount = 0;
            controller.OnCastComplete += (_, __) => completedCount++;

            // Act
            controller.Tick(1.5); // 第一个完成

            // Assert
            Assert.Equal(1, completedCount);
            Assert.False(controller.IsCastingForCaster(caster1));
            Assert.True(controller.IsCastingForCaster(caster2));

            // Act
            controller.Tick(0.6); // 第二个完成

            // Assert
            Assert.Equal(2, completedCount);
            Assert.False(controller.IsCastingForCaster(caster2));
        }

        #endregion

        #region 施法中断测试 (Cast Interrupt Tests) - 4 个

        [Fact]
        public void CancelCast_ShouldTriggerInterruptEvent()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";
            string skillId = "mage_pyroblast";
            controller.StartCast(casterId, skillId, 2.0);
            controller.Tick(0.5); // 施法进行到一半

            string? interruptedCaster = null;
            string? interruptedSkill = null;
            string? interruptReason = null;
            controller.OnCastInterrupt += (caster, skill, reason) =>
            {
                interruptedCaster = caster;
                interruptedSkill = skill;
                interruptReason = reason;
            };

            // Act
            bool result = controller.CancelCast(casterId, "target_died");

            // Assert
            Assert.True(result);
            Assert.Equal(casterId, interruptedCaster);
            Assert.Equal(skillId, interruptedSkill);
            Assert.Equal("target_died", interruptReason);
            Assert.False(controller.IsCastingForCaster(casterId));
        }

        [Fact]
        public void CancelCast_NotCasting_ShouldReturnFalse()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";

            // Act
            bool result = controller.CancelCast(casterId, "test");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void StartCast_WhileCasting_ShouldCancelPrevious()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";
            string skill1 = "mage_pyroblast";
            string skill2 = "mage_frostbolt";
            controller.StartCast(casterId, skill1, 2.0);

            string? interruptedSkill = null;
            controller.OnCastInterrupt += (_, skill, __) => interruptedSkill = skill;

            // Act
            controller.StartCast(casterId, skill2, 1.5);

            // Assert
            Assert.Equal(skill1, interruptedSkill);
            Assert.True(controller.IsCastingForCaster(casterId));
            Assert.Equal(skill2, controller.GetActiveCast(casterId)?.SkillId);
        }

        [Fact]
        public void CancelCast_ShouldClearActiveCast()
        {
            // Arrange
            var controller = new CastingController();
            string casterId = "player1";
            controller.StartCast(casterId, "test_skill", 2.0);

            // Act
            controller.CancelCast(casterId, "manual_cancel");

            // Assert
            Assert.False(controller.IsCastingForCaster(casterId));
            Assert.Null(controller.GetActiveCast(casterId));
        }

        #endregion

        #region Track 暂停测试 (Track Pause Tests) - 2 个

        [Fact]
        public void TrackState_Pause_ShouldPreventTriggers()
        {
            // Arrange
            var track = new TrackState(TrackType.Attack, 1000);
            track.Reset(0);

            // Act
            track.Pause();
            bool triggered = track.TryTrigger(1100); // 应该触发，但被暂停

            // Assert
            Assert.True(track.IsPaused);
            Assert.False(triggered);
        }

        [Fact]
        public void TrackState_ResumeAfterPause_ShouldAllowTriggers()
        {
            // Arrange
            var track = new TrackState(TrackType.Attack, 1000);
            track.Reset(0);
            track.Pause();

            // Act
            track.Resume();
            bool triggered = track.TryTrigger(1100);

            // Assert
            Assert.False(track.IsPaused);
            Assert.True(triggered);
        }

        #endregion
    }
}
