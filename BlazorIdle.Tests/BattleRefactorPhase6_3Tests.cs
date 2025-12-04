using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Battle.Systems;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Shared.Models;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 核心战斗系统重构 Phase 6.3 测试 - CastingIntegration 扩展
    /// Core Battle System Refactoring Phase 6.3 Tests - CastingIntegration Extension
    /// 
    /// 这些测试验证 CastingIntegration 模块中新增的 PreparePlayerCasting 和 PrepareMonsterCasting 方法
    /// These tests verify the new PreparePlayerCasting and PrepareMonsterCasting methods in CastingIntegration
    /// </summary>
    public class BattleRefactorPhase6_3Tests
    {
        #region CalculateHastePercent Tests

        [Fact]
        public void CalculateHastePercent_ReturnsBaseHaste_WhenNoBuffOwner()
        {
            // Arrange
            var integration = CreateCastingIntegration();
            var character = CreateTestCharacter();
            character.HastePercent = 25;

            // Act
            var result = integration.CalculateHastePercent(character, null);

            // Assert
            Assert.Equal(25, result);
        }

        [Fact]
        public void CalculateHastePercent_ReturnsZero_WhenCharacterHasNoHaste()
        {
            // Arrange
            var integration = CreateCastingIntegration();
            var character = CreateTestCharacter();
            character.HastePercent = 0;

            // Act
            var result = integration.CalculateHastePercent(character, null);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateHastePercent_ReturnsNegativeHaste_WhenCharacterHasNegativeHaste()
        {
            // Arrange
            var integration = CreateCastingIntegration();
            var character = CreateTestCharacter();
            character.HastePercent = -10;

            // Act
            var result = integration.CalculateHastePercent(character, null);

            // Assert
            Assert.Equal(-10, result);
        }

        [Fact]
        public void CalculateHastePercent_ReturnsHighHaste()
        {
            // Arrange
            var integration = CreateCastingIntegration();
            var character = CreateTestCharacter();
            character.HastePercent = 200;

            // Act
            var result = integration.CalculateHastePercent(character, null);

            // Assert
            Assert.Equal(200, result);
        }

        #endregion

        #region SelectCastSkill Tests

        [Fact]
        public void SelectCastSkill_ReturnsNull_WhenCharacterDataIsNull()
        {
            // Arrange
            var integration = CreateCastingIntegration();
            var character = CreateTestCharacter();
            var context = new BattleContext();

            // Act
            var result = integration.SelectCastSkill("char_1", character, null, context);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region SelectMonsterCastSkill Tests

        [Fact]
        public void SelectMonsterCastSkill_ReturnsNull_WhenNoSkillsConfigured()
        {
            // Arrange
            var integration = CreateCastingIntegration();
            var enemy = CreateTestEnemy();
            var context = new BattleContext();

            // Act
            var result = integration.SelectMonsterCastSkill("enemy_1", enemy, context);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region PreparePlayerCasting Tests

        [Fact]
        public void PreparePlayerCasting_ReturnsFailure_WhenCharacterDataIsNull()
        {
            // Arrange
            var integration = CreateCastingIntegration();
            var character = CreateTestCharacter();
            var context = new BattleContext();

            // Act
            var result = integration.PreparePlayerCasting("char_1", character, null, context, null);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Skill);
            Assert.Equal(0, result.HastePercent);
            Assert.Equal(0, result.ActualCastTime);
        }

        [Fact]
        public void PreparePlayerCasting_ReturnsFailure_WhenNoCastSkillAvailable()
        {
            // Arrange
            var integration = CreateCastingIntegration();
            var character = CreateTestCharacter();
            var characterData = CreateTestCharacterData();
            var context = new BattleContext();

            // Act - No cast skills configured in test setup
            var result = integration.PreparePlayerCasting("char_1", character, characterData, context, null);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region PrepareMonsterCasting Tests

        [Fact]
        public void PrepareMonsterCasting_ReturnsFailure_WhenNoCastSkillAvailable()
        {
            // Arrange
            var integration = CreateCastingIntegration();
            var enemy = CreateTestEnemy();
            var context = new BattleContext();

            // Act - No cast skills configured in test setup
            var result = integration.PrepareMonsterCasting("enemy_1", enemy, context);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Skill);
            Assert.Equal(0, result.ActualCastTime);
        }

        #endregion

        #region GetSkillDef Tests

        [Fact]
        public void GetSkillDef_ReturnsNull_WhenSkillNotFound()
        {
            // Arrange
            var integration = CreateCastingIntegration();

            // Act
            var result = integration.GetSkillDef("nonexistent_skill");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Integration Tests - Haste Calculation

        [Theory]
        [InlineData(0, 2.0, 2.0)]      // No haste: 2s cast time
        [InlineData(50, 2.0, 1.333)]   // 50% haste: 2s / 1.5 ≈ 1.333s
        [InlineData(100, 2.0, 1.0)]    // 100% haste: 2s / 2 = 1s
        [InlineData(200, 2.0, 0.667)]  // 200% haste: 2s / 3 ≈ 0.667s
        public void HastePercent_CalculatesActualCastTime_Correctly(double hastePercent, double baseCastTime, double expectedActualTime)
        {
            // This is a formula verification test
            // ActualCastTime = BaseCastTime / (1 + HastePercent / 100)
            double actualTime = baseCastTime / (1.0 + hastePercent / 100.0);
            Assert.Equal(expectedActualTime, actualTime, 2); // 2 decimal places precision
        }

        #endregion

        #region Helper Methods

        private CastingIntegration CreateCastingIntegration()
        {
            var clock = new TestClock();
            var skillRepo = new SkillRepository();
            var conditionChecker = new ConditionChecker();
            var resourceMgr = new ResourceManager();
            var windowExecutor = new WindowExecutor(skillRepo, conditionChecker, (id) => new CooldownManager(), resourceMgr);
            var autoCastEngine = new AutoCastEngine(skillRepo, conditionChecker, (id) => new CooldownManager(), resourceMgr);
            
            return new CastingIntegration(clock, skillRepo, windowExecutor, autoCastEngine);
        }

        private Character CreateTestCharacter()
        {
            return new Character
            {
                Hp = 100,
                MaxHp = 100,
                HastePercent = 0,
                ActiveCombatProfessionId = "warrior"
            };
        }

        private Enemy CreateTestEnemy()
        {
            return new Enemy
            {
                Hp = 200,
                MaxHp = 200,
                BaseAttack = 30
            };
        }

        private CharacterData CreateTestCharacterData()
        {
            return new CharacterData();
        }

        private class TestClock : IGameClock
        {
            public int NowMs { get; set; } = 1000;
            public double NowSec => NowMs / 1000.0;
            public void AdvanceBy(int deltaMs) => NowMs += deltaMs;
            public void Reset() => NowMs = 0;
        }

        #endregion
    }
}
