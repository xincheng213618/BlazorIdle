using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Skills;
using System.Linq;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 3 Integration Tests: Verify buff configuration migration from inline to config-based.
    /// Tests that SkillRepository buffs use BuffConfigId and work correctly in combat.
    /// </summary>
    public class BuffMigrationPhase3Tests
    {
        [Fact]
        public void BuffRepository_ContainsAllMigratedBuffs()
        {
            // Arrange
            var repo = BuffRepository.Instance;

            // Act & Assert - Verify all Phase 3 migrated buffs exist
            Assert.True(repo.HasBuff("warrior_power_boost"));
            Assert.True(repo.HasBuff("instant_heal"));
            Assert.True(repo.HasBuff("regeneration_hot"));
            Assert.True(repo.HasBuff("burning"));
            Assert.True(repo.HasBuff("weakened"));

            // Also verify original buffs still exist
            Assert.True(repo.HasBuff("warrior_rage_boost"));
            Assert.True(repo.HasBuff("damage_boost"));
            Assert.True(repo.HasBuff("regeneration"));
        }

        [Fact]
        public void BuffRepository_MigratedBuffs_HaveCorrectProperties()
        {
            // Arrange
            var repo = BuffRepository.Instance;

            // Act - Get migrated buffs
            var warriorPowerBoost = repo.GetBuffById("warrior_power_boost");
            var instantHeal = repo.GetBuffById("instant_heal");
            var regenHot = repo.GetBuffById("regeneration_hot");
            var burning = repo.GetBuffById("burning");
            var weakened = repo.GetBuffById("weakened");

            // Assert - Verify warrior_power_boost
            Assert.NotNull(warriorPowerBoost);
            Assert.Equal(BuffKind.Buff, warriorPowerBoost.Kind);
            Assert.Equal(6.0, warriorPowerBoost.DurationSec);
            Assert.Equal(BuffStackingPolicy.Refresh, warriorPowerBoost.StackingPolicy);
            Assert.Equal(3, warriorPowerBoost.Effects.Count);

            // Assert - Verify instant_heal
            Assert.NotNull(instantHeal);
            Assert.Equal(BuffKind.Buff, instantHeal.Kind);
            Assert.Equal(0.1, instantHeal.DurationSec);
            Assert.Single(instantHeal.Effects);
            Assert.Equal(BuffEffectType.InstantHeal, instantHeal.Effects[0].Type);

            // Assert - Verify regeneration_hot
            Assert.NotNull(regenHot);
            Assert.Equal(BuffKind.Buff, regenHot.Kind);
            Assert.Equal(8.0, regenHot.DurationSec);
            Assert.Equal(2.0, regenHot.TickIntervalSec);
            Assert.Equal(BuffStackingPolicy.Stack, regenHot.StackingPolicy);
            Assert.Equal(3, regenHot.MaxStacks);

            // Assert - Verify burning
            Assert.NotNull(burning);
            Assert.Equal(BuffKind.Debuff, burning.Kind);
            Assert.Equal(10.0, burning.DurationSec);
            Assert.Equal(2.0, burning.TickIntervalSec);
            Assert.Equal(BuffStackingPolicy.Stack, burning.StackingPolicy);
            Assert.Equal(5, burning.MaxStacks);
            Assert.Equal(BuffTarget.AllEnemies, burning.DefaultTarget);

            // Assert - Verify weakened
            Assert.NotNull(weakened);
            Assert.Equal(BuffKind.Debuff, weakened.Kind);
            Assert.Equal(8.0, weakened.DurationSec);
            Assert.Equal(BuffStackingPolicy.Refresh, weakened.StackingPolicy);
            Assert.Equal(BuffTarget.AllEnemies, weakened.DefaultTarget);
            Assert.Single(weakened.Effects);
            Assert.Equal(BuffEffectType.StatReduction, weakened.Effects[0].Type);
        }

        [Fact]
        public void SkillRepository_SpecialPulse_UsesBuffConfigId()
        {
            // Arrange
            var skillRepo = new SkillRepository();

            // Act
            var specialPulse = skillRepo.GetSkill(SkillIds.SpecialPulse);

            // Assert
            Assert.NotNull(specialPulse);
            Assert.NotNull(specialPulse.OnCastBuffs);
            Assert.Equal(5, specialPulse.OnCastBuffs.Count);

            // Verify all buff operations use BuffConfigId (not inline BuffTemplate)
            foreach (var buffOp in specialPulse.OnCastBuffs)
            {
                Assert.NotNull(buffOp.BuffConfigId);
                Assert.Null(buffOp.BuffTemplate); // Should not use inline templates
            }

            // Verify specific buff IDs
            Assert.Contains(specialPulse.OnCastBuffs, op => op.BuffConfigId == "warrior_power_boost");
            Assert.Contains(specialPulse.OnCastBuffs, op => op.BuffConfigId == "instant_heal");
            Assert.Contains(specialPulse.OnCastBuffs, op => op.BuffConfigId == "regeneration_hot");
            Assert.Contains(specialPulse.OnCastBuffs, op => op.BuffConfigId == "burning");
            Assert.Contains(specialPulse.OnCastBuffs, op => op.BuffConfigId == "weakened");
        }

        [Fact]
        public void Integration_SpecialPulse_AppliesBuffsFromConfig()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);

            var playerTeam = new BattleTeam<Character>("team_player", "Player Team", TeamType.Player);
            var character = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 50,
                AttackRateAPS = 1.0,
                CritChancePercent = 0.0,
                CritMultiplier = 2.0,
                HastePercent = 0.0,
                VariancePct = 0.0,
                SpecialIntervalSec = 1.0,
                SpecialDamage = 100
            };
            playerTeam.AddMember("player1", character, maxHp: 1000, currentHp: 1000);

            var enemyTeam = new BattleTeam<Enemy>("team_enemy", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy
            {
                MaxHp = 500,
                Hp = 500,
                DamagePerHit = 20,
                AttackIntervalSec = 2.0,
                VariancePct = 0.0
            };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 500, currentHp: 500);

            var config = new MultiBattleConfig
            {
                PlayerTargetStrategy = TargetStrategy.Random,
                EnemyTargetStrategy = TargetStrategy.Random,
                SpecialIsAoe = true
            };

            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, config);
            battle.Start();

            // Act - advance enough to trigger special attack
            for (int i = 0; i < 15; i++)
            {
                battle.AdvanceTick(100);
            }

            // Assert - verify player buffs
            var playerBuffs = battle.GetPlayerBuffs("player1");
            Assert.NotNull(playerBuffs);
            Assert.NotEmpty(playerBuffs);

            // Should have warrior_power_boost
            Assert.Contains(playerBuffs, b => b.Id == "warrior_power_boost");

            // Should have regeneration_hot
            Assert.Contains(playerBuffs, b => b.Id == "regeneration_hot");

            // instant_heal might have expired (0.1s duration)

            // Assert - verify enemy debuffs
            var enemyBuffs = battle.GetEnemyBuffs("enemy1");
            Assert.NotNull(enemyBuffs);
            Assert.NotEmpty(enemyBuffs);

            // Should have burning DoT
            Assert.Contains(enemyBuffs, b => b.Id == "burning");

            // Should have weakened debuff
            Assert.Contains(enemyBuffs, b => b.Id == "weakened");
        }

        [Fact]
        public void Integration_BuffFromConfig_DoTWorks()
        {
            // Arrange - Create a simple battle with config-based buff
            var clock = new SimClock();
            var rng = new RngContext(12345);

            var playerTeam = new BattleTeam<Character>("team_player", "Player Team", TeamType.Player);
            var character = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 50,
                AttackRateAPS = 1.0,
                CritChancePercent = 0.0,
                CritMultiplier = 2.0,
                HastePercent = 0.0,
                VariancePct = 0.0,
                SpecialIntervalSec = 1.0,
                SpecialDamage = 100
            };
            playerTeam.AddMember("player1", character, maxHp: 1000, currentHp: 1000);

            var enemyTeam = new BattleTeam<Enemy>("team_enemy", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy
            {
                MaxHp = 500,
                Hp = 500,
                DamagePerHit = 20,
                AttackIntervalSec = 10.0, // Very slow so enemy doesn't interfere
                VariancePct = 0.0
            };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 500, currentHp: 500);

            var config = new MultiBattleConfig
            {
                PlayerTargetStrategy = TargetStrategy.Random,
                EnemyTargetStrategy = TargetStrategy.Random,
                SpecialIsAoe = true
            };

            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, config);
            battle.Start();

            // Get initial HP
            var initialEnemyHp = enemyTeam.GetMember("enemy1")!.CurrentHp;

            // Act - trigger special and advance time for buffs to tick
            for (int i = 0; i < 15; i++)
            {
                battle.AdvanceTick(100);
            }

            // Advance more time for DoT to tick (burning has 2s tick interval)
            for (int i = 0; i < 30; i++)
            {
                battle.AdvanceTick(100);
            }

            // Assert - Enemy should be damaged by burning DoT
            var finalEnemyHp = enemyTeam.GetMember("enemy1")!.CurrentHp;
            Assert.True(finalEnemyHp < initialEnemyHp, "Enemy HP should decrease due to DoT from burning buff");
            
            // Verify burning debuff exists on enemy
            var enemyBuffs = battle.GetEnemyBuffs("enemy1");
            Assert.Contains(enemyBuffs, b => b.Id == "burning");
        }

        [Fact]
        public void BuffConfig_ToBuffInstance_PreservesAllConfiguredProperties()
        {
            // Arrange
            var repo = BuffRepository.Instance;
            var config = repo.GetBuffById("warrior_power_boost");
            Assert.NotNull(config);

            // Act
            var instance = config.ToBuffInstance("test_owner", "test_skill");

            // Assert - All properties preserved
            Assert.Equal("warrior_power_boost", instance.Id);
            Assert.Equal("test_owner", instance.OwnerId);
            Assert.Equal("test_skill", instance.SourceSkillId);
            Assert.Equal(BuffKind.Buff, instance.Kind);
            Assert.Equal(6.0, instance.RemainingDurationSec);
            Assert.Equal(BuffStackingPolicy.Refresh, instance.StackingPolicy);
            Assert.Equal(1, instance.MaxStacks);
            Assert.Equal(1, instance.Stacks);
            Assert.Null(instance.TickIntervalSec);
            Assert.Equal(3, instance.Effects.Count);
        }

        [Fact]
        public void BuffCount_AfterMigration_IsCorrect()
        {
            // Arrange
            var repo = BuffRepository.Instance;

            // Act
            var allBuffs = repo.GetAllBuffs();

            // Assert - Should have at least 15 buffs (10 original + 5 migrated)
            Assert.True(allBuffs.Count >= 15, $"Expected at least 15 buffs, got {allBuffs.Count}");

            // Count by kind
            var buffsCount = repo.GetBuffsByKind(BuffKind.Buff).Count;
            var debuffsCount = repo.GetBuffsByKind(BuffKind.Debuff).Count;

            Assert.True(buffsCount >= 10, $"Expected at least 10 buffs, got {buffsCount}");
            Assert.True(debuffsCount >= 5, $"Expected at least 5 debuffs, got {debuffsCount}");
        }
    }
}
