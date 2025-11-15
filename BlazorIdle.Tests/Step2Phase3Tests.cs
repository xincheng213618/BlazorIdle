using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Unit tests for Step 2 Phase 3: Target Selection System
    /// </summary>
    public class Step2Phase3Tests
    {
        #region CurrentTarget Tests (3 tests)

        [Fact]
        public void TargetSelector_CurrentTarget_ReturnsTargetWhenProvided()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateBasicContext();
            string currentTargetId = "enemy_1";

            // Act
            var result = selector.ResolveTargets(TargetPolicy.CurrentTarget, context, currentTargetId: currentTargetId);

            // Assert
            Assert.Single(result);
            Assert.Equal("enemy_1", result[0]);
        }

        [Fact]
        public void TargetSelector_CurrentTarget_ReturnsEmptyWhenNoTarget()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateBasicContext();

            // Act
            var result = selector.ResolveTargets(TargetPolicy.CurrentTarget, context, currentTargetId: null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void TargetSelector_CurrentTarget_ReturnsEmptyWhenEmptyString()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateBasicContext();

            // Act
            var result = selector.ResolveTargets(TargetPolicy.CurrentTarget, context, currentTargetId: string.Empty);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region EnemiesAll Tests (3 tests)

        [Fact]
        public void TargetSelector_EnemiesAll_ReturnsAllAliveEnemies()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateContextWithEnemies(3);

            // Act
            var result = selector.ResolveTargets(TargetPolicy.EnemiesAll, context);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains("enemy_0", result);
            Assert.Contains("enemy_1", result);
            Assert.Contains("enemy_2", result);
        }

        [Fact]
        public void TargetSelector_EnemiesAll_ExcludesDeadEnemies()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateContextWithEnemies(3);
            
            // Kill enemy_1
            context.EnemyTeam!.DealDamage("enemy_1", 1000);

            // Act
            var result = selector.ResolveTargets(TargetPolicy.EnemiesAll, context);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("enemy_0", result);
            Assert.Contains("enemy_2", result);
            Assert.DoesNotContain("enemy_1", result);
        }

        [Fact]
        public void TargetSelector_EnemiesAll_ReturnsEmptyWhenNoEnemyTeam()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateBasicContext(); // No enemy team

            // Act
            var result = selector.ResolveTargets(TargetPolicy.EnemiesAll, context);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region AlliesLowestHpPct Tests (3 tests)

        [Fact]
        public void TargetSelector_AlliesLowestHpPct_ReturnsLowestHpPercentage()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateContextWithAllies(3);

            // Damage allies to different HP percentages
            // ally_0: 100/100 = 100%
            // ally_1: 50/100 = 50%
            // ally_2: 80/100 = 80%
            context.PlayerTeam!.DealDamage("ally_1", 50);
            context.PlayerTeam!.DealDamage("ally_2", 20);

            // Act
            var result = selector.ResolveTargets(TargetPolicy.AlliesLowestHpPct, context);

            // Assert
            Assert.Single(result);
            Assert.Equal("ally_1", result[0]); // 50% HP is lowest
        }

        [Fact]
        public void TargetSelector_AlliesLowestHpPct_UsesPercentageNotAbsolute()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = new BattleContext
            {
                PlayerTeam = new BattleTeam<Character>("player_team", "Players", TeamType.Player),
                Rng = new RngContext(12345),
                Clock = new SimClock()
            };

            // ally_0: 50/200 = 25% (lower percentage but higher absolute)
            // ally_1: 40/100 = 40% (higher percentage but lower absolute)
            context.PlayerTeam.AddMember("ally_0", new Character(), maxHp: 200, currentHp: 50);
            context.PlayerTeam.AddMember("ally_1", new Character(), maxHp: 100, currentHp: 40);

            // Act
            var result = selector.ResolveTargets(TargetPolicy.AlliesLowestHpPct, context);

            // Assert
            Assert.Single(result);
            Assert.Equal("ally_0", result[0]); // 25% is lower than 40%, even though 50 > 40 in absolute value
        }

        [Fact]
        public void TargetSelector_AlliesLowestHpPct_ReturnsEmptyWhenNoPlayerTeam()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateBasicContext(); // No player team

            // Act
            var result = selector.ResolveTargets(TargetPolicy.AlliesLowestHpPct, context);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Self Tests (2 tests)

        [Fact]
        public void TargetSelector_Self_ReturnsCasterWhenProvided()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateBasicContext();
            string casterId = "player_1";

            // Act
            var result = selector.ResolveTargets(TargetPolicy.Self, context, casterId: casterId);

            // Assert
            Assert.Single(result);
            Assert.Equal("player_1", result[0]);
        }

        [Fact]
        public void TargetSelector_Self_ReturnsEmptyWhenNoCaster()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateBasicContext();

            // Act
            var result = selector.ResolveTargets(TargetPolicy.Self, context, casterId: null);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region AlliesAll Tests (2 tests)

        [Fact]
        public void TargetSelector_AlliesAll_ReturnsAllAliveAllies()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateContextWithAllies(4);

            // Act
            var result = selector.ResolveTargets(TargetPolicy.AlliesAll, context);

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Contains("ally_0", result);
            Assert.Contains("ally_1", result);
            Assert.Contains("ally_2", result);
            Assert.Contains("ally_3", result);
        }

        [Fact]
        public void TargetSelector_AlliesAll_ExcludesDeadAllies()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateContextWithAllies(3);
            
            // Kill ally_1
            context.PlayerTeam!.DealDamage("ally_1", 1000);

            // Act
            var result = selector.ResolveTargets(TargetPolicy.AlliesAll, context);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains("ally_0", result);
            Assert.Contains("ally_2", result);
            Assert.DoesNotContain("ally_1", result);
        }

        #endregion

        #region Missing Target Handling Tests (2 tests)

        [Fact]
        public void TargetSelector_HandlesAllDeadEnemies()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateContextWithEnemies(2);

            // Kill all enemies
            context.EnemyTeam!.DealDamage("enemy_0", 1000);
            context.EnemyTeam!.DealDamage("enemy_1", 1000);

            // Act
            var result = selector.ResolveTargets(TargetPolicy.EnemiesAll, context);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void TargetSelector_HandlesAllDeadAllies()
        {
            // Arrange
            var selector = new TargetSelector();
            var context = CreateContextWithAllies(2);

            // Kill all allies
            context.PlayerTeam!.DealDamage("ally_0", 1000);
            context.PlayerTeam!.DealDamage("ally_1", 1000);

            // Act - Should handle both AlliesLowestHpPct and AlliesAll
            var resultLowest = selector.ResolveTargets(TargetPolicy.AlliesLowestHpPct, context);
            var resultAll = selector.ResolveTargets(TargetPolicy.AlliesAll, context);

            // Assert
            Assert.Empty(resultLowest);
            Assert.Empty(resultAll);
        }

        #endregion

        #region Helper Methods

        private BattleContext CreateBasicContext()
        {
            return new BattleContext
            {
                Rng = new RngContext(12345),
                Clock = new SimClock()
            };
        }

        private BattleContext CreateContextWithEnemies(int count)
        {
            var context = new BattleContext
            {
                EnemyTeam = new BattleTeam<Enemy>("enemy_team", "Enemies", TeamType.Enemy),
                Rng = new RngContext(12345),
                Clock = new SimClock()
            };

            for (int i = 0; i < count; i++)
            {
                context.EnemyTeam.AddMember($"enemy_{i}", new Enemy(), maxHp: 100);
            }

            return context;
        }

        private BattleContext CreateContextWithAllies(int count)
        {
            var context = new BattleContext
            {
                PlayerTeam = new BattleTeam<Character>("player_team", "Players", TeamType.Player),
                Rng = new RngContext(12345),
                Clock = new SimClock()
            };

            for (int i = 0; i < count; i++)
            {
                context.PlayerTeam.AddMember($"ally_{i}", new Character(), maxHp: 100);
            }

            return context;
        }

        #endregion
    }
}
