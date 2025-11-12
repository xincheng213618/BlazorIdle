using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 1 单元测试：核心接口与数据结构
    /// Phase 1 unit tests: core interfaces and data structures
    /// </summary>
    public class SkillSystemPhase1Tests
    {
        [Fact]
        public void SkillCastOptions_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var options = new SkillCastOptions();

            // Assert
            Assert.False(options.ForceCrit);
            Assert.Equal("", options.SourceTrack);
            Assert.Null(options.BundleId);
        }

        [Fact]
        public void SkillCastOptions_CanSetProperties()
        {
            // Arrange & Act
            var options = new SkillCastOptions
            {
                ForceCrit = true,
                SourceTrack = "attack",
                BundleId = "bundle_123"
            };

            // Assert
            Assert.True(options.ForceCrit);
            Assert.Equal("attack", options.SourceTrack);
            Assert.Equal("bundle_123", options.BundleId);
        }

        [Fact]
        public void SkillCastResult_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var result = new SkillCastResult();

            // Assert
            Assert.Equal(0, result.DamageDealt);
            Assert.False(result.IsCrit);
            Assert.NotNull(result.ResourceChanges);
            Assert.Empty(result.ResourceChanges);
            Assert.NotNull(result.BuffOperations);
            Assert.Empty(result.BuffOperations);
        }

        [Fact]
        public void SkillCastResult_CanSetProperties()
        {
            // Arrange & Act
            var buffOp = new BuffOperation { Type = BuffOperationType.Apply, Target = BuffTarget.Self };
            var result = new SkillCastResult
            {
                DamageDealt = 100,
                IsCrit = true,
                ResourceChanges = new Dictionary<string, int> { ["rage"] = 10 },
                BuffOperations = new List<BuffOperation> { buffOp }
            };

            // Assert
            Assert.Equal(100, result.DamageDealt);
            Assert.True(result.IsCrit);
            Assert.Single(result.ResourceChanges);
            Assert.Equal(10, result.ResourceChanges["rage"]);
            Assert.Single(result.BuffOperations);
            Assert.Same(buffOp, result.BuffOperations[0]);
        }

        [Fact]
        public void BattleContext_CanBeCreatedWithPlayerOnly()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character { MaxHp = 100 };

            // Act
            var context = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };

            // Assert
            Assert.NotNull(context.Player);
            Assert.Null(context.Enemy);
            Assert.Null(context.PlayerTeam);
            Assert.Null(context.EnemyTeam);
            Assert.NotNull(context.Rng);
            Assert.NotNull(context.Clock);
        }

        [Fact]
        public void BattleContext_CanBeCreatedWithEnemyOnly()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var enemy = new Enemy { MaxHp = 200 };

            // Act
            var context = new BattleContext
            {
                Enemy = enemy,
                Rng = rng,
                Clock = clock
            };

            // Assert
            Assert.Null(context.Player);
            Assert.NotNull(context.Enemy);
            Assert.Null(context.PlayerTeam);
            Assert.Null(context.EnemyTeam);
            Assert.NotNull(context.Rng);
            Assert.NotNull(context.Clock);
        }

        [Fact]
        public void BattleContext_CanBeCreatedWithTeams()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var playerTeam = new BattleTeam<Character>("team1", "Players", TeamType.Player);
            var enemyTeam = new BattleTeam<Enemy>("team2", "Enemies", TeamType.Enemy);

            // Act
            var context = new BattleContext
            {
                PlayerTeam = playerTeam,
                EnemyTeam = enemyTeam,
                Rng = rng,
                Clock = clock
            };

            // Assert
            Assert.Null(context.Player);
            Assert.Null(context.Enemy);
            Assert.NotNull(context.PlayerTeam);
            Assert.NotNull(context.EnemyTeam);
            Assert.Equal("team1", context.PlayerTeam.TeamId);
            Assert.Equal("team2", context.EnemyTeam.TeamId);
        }

        [Fact]
        public void ISkillResolver_InterfaceExists()
        {
            // Arrange & Act
            var resolverType = typeof(ISkillResolver);

            // Assert
            Assert.True(resolverType.IsInterface);
            Assert.NotNull(resolverType.GetMethod("Cast"));
            Assert.NotNull(resolverType.GetMethod("CastBundle"));
        }
    }
}
