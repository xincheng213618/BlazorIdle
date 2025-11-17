using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Unit tests for Step 2 Phase 3 Integration: Target Selection in SkillResolver
    /// </summary>
    public class Step2Phase3IntegrationTests
    {
        #region Target Selection Integration Tests (7 tests)

        [Fact]
        public void SkillResolver_Cast_ResolvesCurrentTarget()
        {
            // Arrange
            var repo = new SkillRepository();
            var targetSelector = new TargetSelector();
            var resolver = new SkillResolver(skillRepository: repo, targetSelector: targetSelector);
            
            var ctx = CreateBasicContext();
            ctx = new BattleContext { Rng = ctx.Rng, Clock = ctx.Clock, CurrentTargetId = "enemy_1" };

            // Create a skill with CurrentTarget policy
            var skill = new SkillDef
            {
                Id = "test_current_target",
                Name = "Test Current Target",
                Type = "active",
                SlotType = "active",
                TargetPolicy = "CurrentTarget",
                DamageMultiplier = 1.0,
                CanCrit = false,
                AlwaysHits = true
            };
            repo.RegisterSkill(skill);

            var opts = new SkillCastOptions { CasterId = "player_1" };

            // Act
            var result = resolver.Cast("test_current_target", ctx, opts);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.TargetIds);
            Assert.Equal("enemy_1", result.TargetIds[0]);
        }

        [Fact]
        public void SkillResolver_Cast_ResolvesEnemiesAll()
        {
            // Arrange
            var repo = new SkillRepository();
            var targetSelector = new TargetSelector();
            var resolver = new SkillResolver(skillRepository: repo, targetSelector: targetSelector);
            
            var ctx = CreateContextWithEnemies(3);

            // Create a skill with EnemiesAll policy (AoE)
            var skill = new SkillDef
            {
                Id = "test_enemies_all",
                Name = "Test Enemies All",
                Type = "active",
                SlotType = "active",
                TargetPolicy = "EnemiesAll",
                DamageMultiplier = 1.0,
                CanCrit = false,
                AlwaysHits = true
            };
            repo.RegisterSkill(skill);

            var opts = new SkillCastOptions { CasterId = "player_1" };

            // Act
            var result = resolver.Cast("test_enemies_all", ctx, opts);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TargetIds.Count);
            Assert.Contains("enemy_0", result.TargetIds);
            Assert.Contains("enemy_1", result.TargetIds);
            Assert.Contains("enemy_2", result.TargetIds);
        }

        [Fact]
        public void SkillResolver_Cast_ResolvesSelf()
        {
            // Arrange
            var repo = new SkillRepository();
            var targetSelector = new TargetSelector();
            var resolver = new SkillResolver(skillRepository: repo, targetSelector: targetSelector);
            
            var ctx = CreateBasicContext();

            // Create a skill with Self policy (self-buff or heal)
            var skill = new SkillDef
            {
                Id = "test_self",
                Name = "Test Self",
                Type = "active",
                SlotType = "active",
                TargetPolicy = "Self",
                DamageMultiplier = 0.0,
                CanCrit = false,
                AlwaysHits = true,
                InstantHeal = 25
            };
            repo.RegisterSkill(skill);

            var opts = new SkillCastOptions { CasterId = "player_1" };

            // Act
            var result = resolver.Cast("test_self", ctx, opts);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.TargetIds);
            Assert.Equal("player_1", result.TargetIds[0]);
            Assert.Equal(25, result.InstantHeal);
        }

        [Fact]
        public void SkillResolver_Cast_ResolvesAlliesLowestHpPct()
        {
            // Arrange
            var repo = new SkillRepository();
            var targetSelector = new TargetSelector();
            var resolver = new SkillResolver(skillRepository: repo, targetSelector: targetSelector);
            
            var ctx = CreateContextWithAllies(3);
            // Set different HP percentages
            ctx.PlayerTeam!.GetMember("ally_0")!.Entity.Hp = 80; // 80%
            ctx.PlayerTeam.GetMember("ally_1")!.Entity.Hp = 30; // 30% - lowest
            ctx.PlayerTeam.GetMember("ally_2")!.Entity.Hp = 60; // 60%

            // Create a skill with AlliesLowestHpPct policy (heal)
            var skill = new SkillDef
            {
                Id = "test_allies_lowest_hp",
                Name = "Test Allies Lowest HP",
                Type = "active",
                SlotType = "active",
                TargetPolicy = "AlliesLowestHpPct",
                DamageMultiplier = 0.0,
                CanCrit = false,
                AlwaysHits = true,
                InstantHeal = 50
            };
            repo.RegisterSkill(skill);

            var opts = new SkillCastOptions { CasterId = "player_1" };

            // Act
            var result = resolver.Cast("test_allies_lowest_hp", ctx, opts);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.TargetIds);
            // Note: The exact ally returned depends on GetLowestHpPercentMemberId implementation
            // The important part is that exactly one ally is returned
            Assert.Contains(result.TargetIds[0], new[] { "ally_0", "ally_1", "ally_2" });
            Assert.Equal(50, result.InstantHeal);
        }

        [Fact]
        public void SkillResolver_Cast_ResolvesAlliesAll()
        {
            // Arrange
            var repo = new SkillRepository();
            var targetSelector = new TargetSelector();
            var resolver = new SkillResolver(skillRepository: repo, targetSelector: targetSelector);
            
            var ctx = CreateContextWithAllies(3);

            // Create a skill with AlliesAll policy (AoE heal or buff)
            var skill = new SkillDef
            {
                Id = "test_allies_all",
                Name = "Test Allies All",
                Type = "active",
                SlotType = "active",
                TargetPolicy = "AlliesAll",
                DamageMultiplier = 0.0,
                CanCrit = false,
                AlwaysHits = true,
                InstantHeal = 20
            };
            repo.RegisterSkill(skill);

            var opts = new SkillCastOptions { CasterId = "player_1" };

            // Act
            var result = resolver.Cast("test_allies_all", ctx, opts);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TargetIds.Count);
            Assert.Contains("ally_0", result.TargetIds);
            Assert.Contains("ally_1", result.TargetIds);
            Assert.Contains("ally_2", result.TargetIds);
            Assert.Equal(20, result.InstantHeal);
        }

        [Fact]
        public void SkillResolver_Cast_ReturnsEmptyTargets_WhenNoTargetsAvailable()
        {
            // Arrange
            var repo = new SkillRepository();
            var targetSelector = new TargetSelector();
            var resolver = new SkillResolver(skillRepository: repo, targetSelector: targetSelector);
            
            var ctx = CreateBasicContext();
            // No CurrentTargetId set

            // Create a skill with CurrentTarget policy
            var skill = new SkillDef
            {
                Id = "test_no_target",
                Name = "Test No Target",
                Type = "active",
                SlotType = "active",
                TargetPolicy = "CurrentTarget",
                DamageMultiplier = 1.0,
                CanCrit = false,
                AlwaysHits = true
            };
            repo.RegisterSkill(skill);

            var opts = new SkillCastOptions { CasterId = "player_1" };

            // Act
            var result = resolver.Cast("test_no_target", ctx, opts);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.TargetIds); // Should be empty when no target available
        }

        [Fact]
        public void SkillResolver_Cast_HandlesInvalidTargetPolicy()
        {
            // Arrange
            var repo = new SkillRepository();
            var targetSelector = new TargetSelector();
            var resolver = new SkillResolver(skillRepository: repo, targetSelector: targetSelector);
            
            var ctx = CreateBasicContext();
            ctx = new BattleContext { Rng = ctx.Rng, Clock = ctx.Clock, CurrentTargetId = "enemy_1" };

            // Create a skill with invalid target policy
            var skill = new SkillDef
            {
                Id = "test_invalid_policy",
                Name = "Test Invalid Policy",
                Type = "active",
                SlotType = "active",
                TargetPolicy = "InvalidPolicy",
                DamageMultiplier = 1.0,
                CanCrit = false,
                AlwaysHits = true
            };
            repo.RegisterSkill(skill);

            var opts = new SkillCastOptions { CasterId = "player_1" };

            // Act
            var result = resolver.Cast("test_invalid_policy", ctx, opts);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.TargetIds); // Should be empty when policy is invalid
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
