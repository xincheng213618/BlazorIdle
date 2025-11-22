using BlazorIdle.Game.Skills;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Step3 Phase 1: 定期技能检查系统 - 基础测试
    /// Step3 Phase 1: Periodic Skill Check System - Basic Tests
    /// 
    /// 测试定期技能检查的核心功能：
    /// - 条件扩展（数量、队友HP、Buff时间）
    /// </summary>
    public class Step3Phase1Tests
    {
        /// <summary>
        /// 测试：SkillConditions 应该包含新的敌人数量条件字段
        /// Test: SkillConditions should contain new enemy count condition fields
        /// </summary>
        [Fact]
        public void SkillConditions_ShouldHave_EnemyCountConditions()
        {
            // Arrange & Act
            var conditions = new SkillConditions
            {
                EnemyCountAbove = 3,
                EnemyCountBelow = 10
            };

            // Assert
            Assert.Equal(3, conditions.EnemyCountAbove);
            Assert.Equal(10, conditions.EnemyCountBelow);
        }

        /// <summary>
        /// 测试：SkillConditions 应该包含新的队友数量条件字段
        /// Test: SkillConditions should contain new ally count condition fields
        /// </summary>
        [Fact]
        public void SkillConditions_ShouldHave_AllyCountConditions()
        {
            // Arrange & Act
            var conditions = new SkillConditions
            {
                AllyCountAbove = 2,
                AllyCountBelow = 5
            };

            // Assert
            Assert.Equal(2, conditions.AllyCountAbove);
            Assert.Equal(5, conditions.AllyCountBelow);
        }

        /// <summary>
        /// 测试：SkillConditions 应该包含队友HP条件字段
        /// Test: SkillConditions should contain ally HP condition field
        /// </summary>
        [Fact]
        public void SkillConditions_ShouldHave_AllyHpCondition()
        {
            // Arrange & Act
            var conditions = new SkillConditions
            {
                AllyHpBelowPct = 30.0
            };

            // Assert
            Assert.Equal(30.0, conditions.AllyHpBelowPct);
        }

        /// <summary>
        /// 测试：SkillConditions 应该包含Buff时间条件字段（用于光环续期）
        /// Test: SkillConditions should contain buff time condition fields (for aura refresh)
        /// </summary>
        [Fact]
        public void SkillConditions_ShouldHave_BuffTimeConditions()
        {
            // Arrange & Act
            var conditions = new SkillConditions
            {
                BuffTimeRemainingSec = 5.0,
                BuffTimeCheckId = "test_buff"
            };

            // Assert
            Assert.Equal(5.0, conditions.BuffTimeRemainingSec);
            Assert.Equal("test_buff", conditions.BuffTimeCheckId);
        }

        /// <summary>
        /// 测试：SkillConditions 所有新字段可以同时设置
        /// Test: All new SkillConditions fields can be set together
        /// </summary>
        [Fact]
        public void SkillConditions_AllNewFields_CanBeSetTogether()
        {
            // Arrange & Act
            var conditions = new SkillConditions
            {
                // Existing conditions
                HpBelowPct = 50.0,
                HpAbovePct = 20.0,
                RequireBuffId = "existing_buff",
                
                // New Step3 conditions
                EnemyCountAbove = 3,
                EnemyCountBelow = 10,
                AllyCountAbove = 2,
                AllyCountBelow = 5,
                AllyHpBelowPct = 30.0,
                BuffTimeRemainingSec = 5.0,
                BuffTimeCheckId = "test_buff"
            };

            // Assert - verify existing fields still work
            Assert.Equal(50.0, conditions.HpBelowPct);
            Assert.Equal(20.0, conditions.HpAbovePct);
            Assert.Equal("existing_buff", conditions.RequireBuffId);
            
            // Assert - verify new Step3 fields
            Assert.Equal(3, conditions.EnemyCountAbove);
            Assert.Equal(10, conditions.EnemyCountBelow);
            Assert.Equal(2, conditions.AllyCountAbove);
            Assert.Equal(5, conditions.AllyCountBelow);
            Assert.Equal(30.0, conditions.AllyHpBelowPct);
            Assert.Equal(5.0, conditions.BuffTimeRemainingSec);
            Assert.Equal("test_buff", conditions.BuffTimeCheckId);
        }
    }
}
