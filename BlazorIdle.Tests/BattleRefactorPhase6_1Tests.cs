using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Battle.Systems;
using BlazorIdle.Game.Battle.Execution;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Shared.Models;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 核心战斗系统重构 Phase 6.1 测试 - SkillExecutionCoordinator
    /// Core Battle System Refactoring Phase 6.1 Tests - SkillExecutionCoordinator
    /// </summary>
    public class BattleRefactorPhase6_1Tests
    {
        #region BuildPlayerContext Tests

        [Fact]
        public void BuildPlayerContext_SetsBasicProperties()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var character = CreateTestCharacter();
            character.Element = ElementIds.Fire;
            character.CombatStats = new CombatStats { AttackFinal = 50, CritChancePercent = 10 };

            var enemy = CreateTestEnemy();
            enemy.Element = ElementIds.Water;
            var playerTeam = new BattleTeam<Character>("player", "Players", TeamType.Player);
            var enemyTeam = new BattleTeam<Enemy>("enemy", "Enemies", TeamType.Enemy);
            var rng = new RngContext(12345);
            var damageCalculator = DamageCalculator.CreateDefault();

            // Act
            var ctx = coordinator.BuildPlayerContext(
                character, enemy, "enemy_1",
                playerTeam, enemyTeam, rng, clock,
                null, null, new Dictionary<string, EnemyBuffOwner>(),
                damageCalculator);

            // Assert
            Assert.Equal(character, ctx.Player);
            Assert.Equal(enemy, ctx.Enemy);
            Assert.Equal("enemy_1", ctx.CurrentTargetId);
            Assert.Equal(ElementIds.Fire, ctx.AttackerElement);
            Assert.Equal(ElementIds.Water, ctx.DefenderElement);
            Assert.Equal(1.0, ctx.AttackerHPRatio);
        }

        [Fact]
        public void BuildPlayerContext_CalculatesCorrectHPRatio()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var character = CreateTestCharacter();
            character.Hp = 50;
            character.MaxHp = 100;
            character.Element = ElementIds.Neutral;
            character.CombatStats = CombatStats.CreateDefault();

            var playerTeam = new BattleTeam<Character>("player", "Players", TeamType.Player);
            var enemyTeam = new BattleTeam<Enemy>("enemy", "Enemies", TeamType.Enemy);
            var rng = new RngContext(12345);
            var damageCalculator = DamageCalculator.CreateDefault();

            // Act
            var ctx = coordinator.BuildPlayerContext(
                character, null, null,
                playerTeam, enemyTeam, rng, clock,
                null, null, new Dictionary<string, EnemyBuffOwner>(),
                damageCalculator);

            // Assert
            Assert.Equal(0.5, ctx.AttackerHPRatio);
        }

        #endregion

        #region BuildMonsterContext Tests

        [Fact]
        public void BuildMonsterContext_SetsBasicProperties()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var enemy = CreateTestEnemy();
            enemy.MaxHp = 200;
            enemy.Hp = 200;
            enemy.BaseAttack = 30;
            enemy.Element = ElementIds.Earth;
            var character = CreateTestCharacter();
            character.Element = ElementIds.Fire;
            var playerTeam = new BattleTeam<Character>("player", "Players", TeamType.Player);
            var enemyTeam = new BattleTeam<Enemy>("enemy", "Enemies", TeamType.Enemy);
            var rng = new RngContext(12345);
            var damageCalculator = DamageCalculator.CreateDefault();

            // Act
            var ctx = coordinator.BuildMonsterContext(
                enemy, character, "player_1",
                playerTeam, enemyTeam, rng, clock,
                null, null, new Dictionary<string, EnemyBuffOwner>(),
                damageCalculator);

            // Assert
            Assert.Equal(enemy, ctx.Enemy);
            Assert.Equal(character, ctx.Player);
            Assert.Equal("player_1", ctx.CurrentTargetId);
            Assert.Equal(ElementIds.Earth, ctx.AttackerElement);
            Assert.Equal(ElementIds.Fire, ctx.DefenderElement);
            Assert.Equal(30, ctx.AttackerCombatStats.AttackFinal); // BaseAttack
        }

        [Fact]
        public void BuildMonsterContext_CalculatesCorrectHPRatio()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var enemy = CreateTestEnemy();
            enemy.MaxHp = 200;
            enemy.Hp = 100;
            var playerTeam = new BattleTeam<Character>("player", "Players", TeamType.Player);
            var enemyTeam = new BattleTeam<Enemy>("enemy", "Enemies", TeamType.Enemy);
            var rng = new RngContext(12345);
            var damageCalculator = DamageCalculator.CreateDefault();

            // Act
            var ctx = coordinator.BuildMonsterContext(
                enemy, null, null,
                playerTeam, enemyTeam, rng, clock,
                null, null, new Dictionary<string, EnemyBuffOwner>(),
                damageCalculator);

            // Assert
            Assert.Equal(0.5, ctx.AttackerHPRatio);
        }

        #endregion

        #region ResolveSkillTargets Tests

        [Fact]
        public void ResolveSkillTargets_ReturnsResultTargetIds_WhenNotEmpty()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult
            {
                TargetIds = new List<string> { "enemy_1", "enemy_2" }
            };

            // Act
            var targets = coordinator.ResolveSkillTargets(result, "default_enemy");

            // Assert
            Assert.Equal(2, targets.Count);
            Assert.Contains("enemy_1", targets);
            Assert.Contains("enemy_2", targets);
        }

        [Fact]
        public void ResolveSkillTargets_ReturnsDefaultTargetId_WhenResultEmpty()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult { TargetIds = null };

            // Act
            var targets = coordinator.ResolveSkillTargets(result, "default_enemy");

            // Assert
            Assert.Single(targets);
            Assert.Equal("default_enemy", targets[0]);
        }

        [Fact]
        public void ResolveSkillTargets_ReturnsEmptyList_WhenNoTargets()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult { TargetIds = null };

            // Act
            var targets = coordinator.ResolveSkillTargets(result, null);

            // Assert
            Assert.Empty(targets);
        }

        #endregion

        #region ProcessPlayerDamageInstances Tests

        [Fact]
        public void ProcessPlayerDamageInstances_FiresEventForImmediateDamage()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 100, ApplyAtSec = 0, IsCrit = true }
                },
                BundleId = "bundle_1"
            };

            string? receivedCasterId = null;
            string? receivedTargetId = null;
            int receivedDamage = 0;
            bool receivedIsCrit = false;

            coordinator.OnApplyDamageToEnemy += (casterId, targetId, damage, source, isAoe, isCrit, skillId, bundleId) =>
            {
                receivedCasterId = casterId;
                receivedTargetId = targetId;
                receivedDamage = damage;
                receivedIsCrit = isCrit;
            };

            // Act
            coordinator.ProcessPlayerDamageInstances(
                result, "player_1", new List<string> { "enemy_1" },
                EventSource.Attack, "test_skill", id => true);

            // Assert
            Assert.Equal("player_1", receivedCasterId);
            Assert.Equal("enemy_1", receivedTargetId);
            Assert.Equal(100, receivedDamage);
            Assert.True(receivedIsCrit);
        }

        [Fact]
        public void ProcessPlayerDamageInstances_EnqueuesDelayedDamage()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock(1000); // Set current time to 1 second
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 50, ApplyAtSec = 0.5 } // Delayed by 0.5s
                },
                BundleId = "bundle_1"
            };

            // Act
            coordinator.ProcessPlayerDamageInstances(
                result, "player_1", new List<string> { "enemy_1" },
                EventSource.Attack, "test_skill", id => true);

            // Assert
            Assert.Equal(1, queue.Count);
            var pending = queue.GetAll();
            Assert.Equal("enemy_1", pending[0].TargetId);
            Assert.True(pending[0].IsCasterPlayer);
        }

        [Fact]
        public void ProcessPlayerDamageInstances_AppliesAoeDamageReduction()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock, aoeDamageMultiplier: 0.5);

            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 100, ApplyAtSec = 0 }
                }
            };

            int receivedDamage = 0;
            coordinator.OnApplyDamageToEnemy += (casterId, targetId, damage, source, isAoe, isCrit, skillId, bundleId) =>
            {
                receivedDamage = damage;
            };

            // Act - AOE (multiple targets)
            coordinator.ProcessPlayerDamageInstances(
                result, "player_1", new List<string> { "enemy_1", "enemy_2" },
                EventSource.Attack, "test_skill", id => true);

            // Assert - Should be reduced by 50%
            Assert.Equal(50, receivedDamage);
        }

        [Fact]
        public void ProcessPlayerDamageInstances_SkipsDeadTargets()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 100, ApplyAtSec = 0 }
                }
            };

            int eventCount = 0;
            coordinator.OnApplyDamageToEnemy += (casterId, targetId, damage, source, isAoe, isCrit, skillId, bundleId) =>
            {
                eventCount++;
            };

            // Act - Target is dead
            coordinator.ProcessPlayerDamageInstances(
                result, "player_1", new List<string> { "enemy_1" },
                EventSource.Attack, "test_skill", id => false);

            // Assert
            Assert.Equal(0, eventCount);
        }

        #endregion

        #region ProcessMonsterDamageInstances Tests

        [Fact]
        public void ProcessMonsterDamageInstances_FiresEventForImmediateDamage()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 75, ApplyAtSec = 0 }
                },
                BundleId = "monster_bundle"
            };

            string? receivedTargetId = null;
            int receivedDamage = 0;

            coordinator.OnApplyDamageToPlayer += (casterId, targetId, damage, source, skillId, bundleId) =>
            {
                receivedTargetId = targetId;
                receivedDamage = damage;
            };

            // Act
            coordinator.ProcessMonsterDamageInstances(
                result, "monster_1", new List<string> { "player_1" },
                EventSource.EnemyAttack, "monster_attack", id => true);

            // Assert
            Assert.Equal("player_1", receivedTargetId);
            Assert.Equal(75, receivedDamage);
        }

        [Fact]
        public void ProcessMonsterDamageInstances_EnqueuesDelayedDamage_AsFalseIsCasterPlayer()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 50, ApplyAtSec = 1.0 }
                }
            };

            // Act
            coordinator.ProcessMonsterDamageInstances(
                result, "monster_1", new List<string> { "player_1" },
                EventSource.EnemyAttack, "monster_skill", id => true);

            // Assert
            Assert.Equal(1, queue.Count);
            var pending = queue.GetAll();
            Assert.False(pending[0].IsCasterPlayer);
        }

        #endregion

        #region ApplyPlayerLegacySingleDamage Tests

        [Fact]
        public void ApplyPlayerLegacySingleDamage_AppliesDamageToTargets()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult
            {
                DamageDealt = 80,
                IsCrit = false,
                BundleId = "legacy_bundle"
            };

            int totalDamage = 0;
            coordinator.OnApplyDamageToEnemy += (casterId, targetId, damage, source, isAoe, isCrit, skillId, bundleId) =>
            {
                totalDamage += damage;
            };

            // Act
            coordinator.ApplyPlayerLegacySingleDamage(
                result, "player_1", new List<string> { "enemy_1" },
                EventSource.Attack, "legacy_skill", id => true);

            // Assert
            Assert.Equal(80, totalDamage);
        }

        [Fact]
        public void ApplyPlayerLegacySingleDamage_AppliesAoeReduction()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock, aoeDamageMultiplier: 0.8);

            var result = new SkillCastResult { DamageDealt = 100, IsCrit = false };

            int damagePerTarget = 0;
            coordinator.OnApplyDamageToEnemy += (casterId, targetId, damage, source, isAoe, isCrit, skillId, bundleId) =>
            {
                damagePerTarget = damage;
            };

            // Act - Multiple targets
            coordinator.ApplyPlayerLegacySingleDamage(
                result, "player_1", new List<string> { "enemy_1", "enemy_2" },
                EventSource.Attack, "aoe_skill", id => true);

            // Assert - 80% of original
            Assert.Equal(80, damagePerTarget);
        }

        [Fact]
        public void ApplyPlayerLegacySingleDamage_StillAppliesHeal_WhenNoDamage()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult { DamageDealt = 0, InstantHeal = 50 };

            int healCallCount = 0;
            coordinator.OnApplyInstantHeal += (r, casterId, targetId, isCasterPlayer, skillId) =>
            {
                healCallCount++;
            };

            // Act
            coordinator.ApplyPlayerLegacySingleDamage(
                result, "player_1", new List<string> { "ally_1" },
                EventSource.Special, "heal_skill", id => true);

            // Assert
            Assert.Equal(1, healCallCount);
        }

        #endregion

        #region ApplyMonsterLegacySingleDamage Tests

        [Fact]
        public void ApplyMonsterLegacySingleDamage_AppliesDamageToPlayer()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult { DamageDealt = 60 };

            int receivedDamage = 0;
            coordinator.OnApplyDamageToPlayer += (casterId, targetId, damage, source, skillId, bundleId) =>
            {
                receivedDamage = damage;
            };

            // Act
            coordinator.ApplyMonsterLegacySingleDamage(
                result, "monster_1", new List<string> { "player_1" },
                EventSource.EnemyAttack, "monster_basic", id => true);

            // Assert
            Assert.Equal(60, receivedDamage);
        }

        #endregion

        #region HandleBackwardCompatibleResourceGain Tests

        [Fact]
        public void HandleBackwardCompatibleResourceGain_GeneratesResourceOnAttack()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var resources = new ResourceBucketCollection("rage", 100, 0);
            var defaultConfig = new ResourceConfig { GainPerAttack = 5, GainPerCritExtra = 3 };
            var result = new SkillCastResult { ResourceChanges = null, IsCrit = false };

            int gainedAmount = 0;
            string? gainedResourceId = null;
            coordinator.OnRecordResourceGain += (casterId, resId, amount, newValue, reason, skillId, bundleId) =>
            {
                gainedAmount = amount;
                gainedResourceId = resId;
            };

            // Act
            coordinator.HandleBackwardCompatibleResourceGain(
                "attack", result, "player_1", "basic_attack", "warrior",
                resources, null, defaultConfig);

            // Assert
            Assert.Equal(5, gainedAmount);
            Assert.Equal("rage", gainedResourceId);
            Assert.Equal(5, resources.GetBucket("rage").Current);
        }

        [Fact]
        public void HandleBackwardCompatibleResourceGain_AddsCritBonus()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var resources = new ResourceBucketCollection("rage", 100, 0);
            var defaultConfig = new ResourceConfig { GainPerAttack = 5, GainPerCritExtra = 3 };
            var result = new SkillCastResult { ResourceChanges = null, IsCrit = true };

            int totalGained = 0;
            coordinator.OnRecordResourceGain += (casterId, resId, amount, newValue, reason, skillId, bundleId) =>
            {
                totalGained += amount;
            };

            // Act
            coordinator.HandleBackwardCompatibleResourceGain(
                "attack", result, "player_1", "basic_attack", "warrior",
                resources, null, defaultConfig);

            // Assert - 5 (attack) + 3 (crit)
            Assert.Equal(8, totalGained);
            Assert.Equal(8, resources.GetBucket("rage").Current);
        }

        [Fact]
        public void HandleBackwardCompatibleResourceGain_UsesProfessionConfig_WhenAvailable()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var resources = new ResourceBucketCollection();
            resources.AddBucket("mana", 100, 50);

            var defaultConfig = new ResourceConfig { GainPerAttack = 5, GainPerCritExtra = 3 };
            var profConfigs = new Dictionary<string, ProfessionResourceConfig>
            {
                ["mage"] = new ProfessionResourceConfig { Id = "mana", GainPerAttack = 10, GainPerCritExtra = 5 }
            };
            var result = new SkillCastResult { ResourceChanges = null, IsCrit = false };

            string? gainedResourceId = null;
            int gainedAmount = 0;
            coordinator.OnRecordResourceGain += (casterId, resId, amount, newValue, reason, skillId, bundleId) =>
            {
                gainedResourceId = resId;
                gainedAmount = amount;
            };

            // Act
            coordinator.HandleBackwardCompatibleResourceGain(
                "attack", result, "player_1", "staff_attack", "mage",
                resources, profConfigs, defaultConfig);

            // Assert - Should use mage config
            Assert.Equal("mana", gainedResourceId);
            Assert.Equal(10, gainedAmount);
        }

        [Fact]
        public void HandleBackwardCompatibleResourceGain_SkipsNonAttackTracks()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var resources = new ResourceBucketCollection("rage", 100, 0);
            var defaultConfig = new ResourceConfig { GainPerAttack = 5 };
            var result = new SkillCastResult { ResourceChanges = null };

            bool eventFired = false;
            coordinator.OnRecordResourceGain += (casterId, resId, amount, newValue, reason, skillId, bundleId) =>
            {
                eventFired = true;
            };

            // Act - Source is "special", not "attack"
            coordinator.HandleBackwardCompatibleResourceGain(
                "special", result, "player_1", "special_skill", "warrior",
                resources, null, defaultConfig);

            // Assert
            Assert.False(eventFired);
        }

        [Fact]
        public void HandleBackwardCompatibleResourceGain_SkipsWhenResourceChangesDefined()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var resources = new ResourceBucketCollection("rage", 100, 0);
            var defaultConfig = new ResourceConfig { GainPerAttack = 5 };
            var result = new SkillCastResult 
            { 
                ResourceChanges = new Dictionary<string, int>
                {
                    ["rage"] = 10
                }
            };

            bool eventFired = false;
            coordinator.OnRecordResourceGain += (casterId, resId, amount, newValue, reason, skillId, bundleId) =>
            {
                eventFired = true;
            };

            // Act
            coordinator.HandleBackwardCompatibleResourceGain(
                "attack", result, "player_1", "skill_with_resource", "warrior",
                resources, null, defaultConfig);

            // Assert - Should skip because skill already defines resource changes
            Assert.False(eventFired);
        }

        #endregion

        #region HasDamageInstances Tests

        [Fact]
        public void HasDamageInstances_ReturnsTrue_WhenHasInstances()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 10 }
                }
            };

            // Act & Assert
            Assert.True(coordinator.HasDamageInstances(result));
        }

        [Fact]
        public void HasDamageInstances_ReturnsFalse_WhenNull()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult { DamageInstances = null };

            // Act & Assert
            Assert.False(coordinator.HasDamageInstances(result));
        }

        [Fact]
        public void HasDamageInstances_ReturnsFalse_WhenEmpty()
        {
            // Arrange
            var queue = new PendingDamageQueue();
            var clock = CreateTestClock();
            var coordinator = new SkillExecutionCoordinator(queue, clock);

            var result = new SkillCastResult { DamageInstances = new List<DamageInstance>() };

            // Act & Assert
            Assert.False(coordinator.HasDamageInstances(result));
        }

        #endregion

        #region Helper Methods

        private IGameClock CreateTestClock(int nowMs = 0)
        {
            return new TestClock { NowMs = nowMs };
        }

        private Character CreateTestCharacter()
        {
            return new Character
            {
                Hp = 100,
                MaxHp = 100,
                ActiveCombatProfessionId = "warrior"
            };
        }

        private Enemy CreateTestEnemy()
        {
            return new Enemy
            {
                Hp = 50,
                MaxHp = 50,
                BaseAttack = 10
            };
        }

        private class TestClock : IGameClock
        {
            public int NowMs { get; set; } = 0;
            public double NowSec => NowMs / 1000.0;
            public void Reset() => NowMs = 0;
            public void AdvanceBy(int ms) => NowMs += ms;
        }

        #endregion
    }
}
