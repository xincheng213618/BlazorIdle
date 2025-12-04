using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Battle.Systems;
using BlazorIdle.Game.Combat;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 核心战斗系统重构 Phase 6.2 测试 - DamageApplicationHelper
    /// Core Battle System Refactoring Phase 6.2 Tests - DamageApplicationHelper
    /// </summary>
    public class BattleRefactorPhase6_2Tests
    {
        #region CreatePlayerToEnemyEvent Tests

        [Fact]
        public void CreatePlayerToEnemyEvent_SetsCorrectActorTypes()
        {
            // Act
            var ev = DamageApplicationHelper.CreatePlayerToEnemyEvent(
                attackerId: "player_1",
                attackerName: "Hero",
                defenderId: "enemy_1",
                defenderName: "Goblin",
                source: EventSource.Attack,
                timeMs: 1000,
                damage: 50,
                isCrit: false,
                isAoe: false,
                isKill: false,
                rngIndexAfter: 0,
                defenderHpAfter: 100);

            // Assert
            Assert.Equal(ActorType.Player, ev.Attacker);
            Assert.Equal(ActorType.Enemy, ev.Defender);
        }

        [Fact]
        public void CreatePlayerToEnemyEvent_SetsAllProperties()
        {
            // Act
            var ev = DamageApplicationHelper.CreatePlayerToEnemyEvent(
                attackerId: "char_123",
                attackerName: "Warrior",
                defenderId: "mob_456",
                defenderName: "Dragon",
                source: EventSource.Skill,
                timeMs: 5000,
                damage: 999,
                isCrit: true,
                isAoe: true,
                isKill: true,
                rngIndexAfter: 42,
                defenderHpAfter: 0,
                skillId: "fireball",
                bundleId: "bundle_abc");

            // Assert
            Assert.Equal("char_123", ev.AttackerId);
            Assert.Equal("Warrior", ev.AttackerName);
            Assert.Equal("mob_456", ev.DefenderId);
            Assert.Equal("Dragon", ev.DefenderName);
            Assert.Equal(EventSource.Skill, ev.Source);
            Assert.Equal(5000, ev.TimeMs);
            Assert.Equal(999, ev.Damage);
            Assert.True(ev.Crit);
            Assert.True(ev.IsAoe);
            Assert.True(ev.IsKill);
            Assert.Equal(42, ev.RngIndexAfter);
            Assert.Equal(0, ev.DefenderHpAfter);
            Assert.Equal("fireball", ev.SkillId);
            Assert.Equal("bundle_abc", ev.BundleId);
        }

        [Fact]
        public void CreatePlayerToEnemyEvent_HandlesNullSkillAndBundleId()
        {
            // Act
            var ev = DamageApplicationHelper.CreatePlayerToEnemyEvent(
                attackerId: "player_1",
                attackerName: "Hero",
                defenderId: "enemy_1",
                defenderName: "Goblin",
                source: EventSource.Attack,
                timeMs: 1000,
                damage: 50,
                isCrit: false,
                isAoe: false,
                isKill: false,
                rngIndexAfter: 0,
                defenderHpAfter: 100);

            // Assert
            Assert.Null(ev.SkillId);
            Assert.Null(ev.BundleId);
        }

        [Fact]
        public void CreatePlayerToEnemyEvent_HandlesTriggerSource()
        {
            // Act
            var ev = DamageApplicationHelper.CreatePlayerToEnemyEvent(
                attackerId: "player_1",
                attackerName: "Hero",
                defenderId: "enemy_1",
                defenderName: "Goblin",
                source: EventSource.Trigger,
                timeMs: 1000,
                damage: 25,
                isCrit: false,
                isAoe: false,
                isKill: false,
                rngIndexAfter: 0,
                defenderHpAfter: 75);

            // Assert
            Assert.Equal(EventSource.Trigger, ev.Source);
        }

        #endregion

        #region CreateEnemyToPlayerEvent Tests

        [Fact]
        public void CreateEnemyToPlayerEvent_SetsCorrectActorTypes()
        {
            // Act
            var ev = DamageApplicationHelper.CreateEnemyToPlayerEvent(
                attackerId: "enemy_1",
                attackerName: "Orc",
                defenderId: "player_1",
                defenderName: "Hero",
                source: EventSource.EnemyAttack,
                timeMs: 2000,
                damage: 30,
                isKill: false,
                rngIndexAfter: 5,
                defenderHpAfter: 70);

            // Assert
            Assert.Equal(ActorType.Enemy, ev.Attacker);
            Assert.Equal(ActorType.Player, ev.Defender);
        }

        [Fact]
        public void CreateEnemyToPlayerEvent_SetsCritAndAoeToFalse()
        {
            // Act
            var ev = DamageApplicationHelper.CreateEnemyToPlayerEvent(
                attackerId: "enemy_1",
                attackerName: "Orc",
                defenderId: "player_1",
                defenderName: "Hero",
                source: EventSource.EnemyAttack,
                timeMs: 2000,
                damage: 30,
                isKill: false,
                rngIndexAfter: 5,
                defenderHpAfter: 70);

            // Assert - 怪物暂不支持暴击和AOE
            Assert.False(ev.Crit);
            Assert.False(ev.IsAoe);
        }

        [Fact]
        public void CreateEnemyToPlayerEvent_SetsAllProperties()
        {
            // Act
            var ev = DamageApplicationHelper.CreateEnemyToPlayerEvent(
                attackerId: "boss_001",
                attackerName: "Dark Lord",
                defenderId: "hero_001",
                defenderName: "Knight",
                source: EventSource.Skill,
                timeMs: 10000,
                damage: 500,
                isKill: true,
                rngIndexAfter: 100,
                defenderHpAfter: 0,
                skillId: "dark_blast",
                bundleId: "bundle_xyz");

            // Assert
            Assert.Equal("boss_001", ev.AttackerId);
            Assert.Equal("Dark Lord", ev.AttackerName);
            Assert.Equal("hero_001", ev.DefenderId);
            Assert.Equal("Knight", ev.DefenderName);
            Assert.Equal(EventSource.Skill, ev.Source);
            Assert.Equal(10000, ev.TimeMs);
            Assert.Equal(500, ev.Damage);
            Assert.True(ev.IsKill);
            Assert.Equal(100, ev.RngIndexAfter);
            Assert.Equal(0, ev.DefenderHpAfter);
            Assert.Equal("dark_blast", ev.SkillId);
            Assert.Equal("bundle_xyz", ev.BundleId);
        }

        [Fact]
        public void CreateEnemyToPlayerEvent_HandlesNullSkillAndBundleId()
        {
            // Act
            var ev = DamageApplicationHelper.CreateEnemyToPlayerEvent(
                attackerId: "enemy_1",
                attackerName: "Goblin",
                defenderId: "player_1",
                defenderName: "Hero",
                source: EventSource.EnemyAttack,
                timeMs: 1000,
                damage: 10,
                isKill: false,
                rngIndexAfter: 0,
                defenderHpAfter: 90);

            // Assert
            Assert.Null(ev.SkillId);
            Assert.Null(ev.BundleId);
        }

        #endregion

        #region CreateHealMeta Tests

        [Fact]
        public void CreateHealMeta_CreatesWithDefaultValues()
        {
            // Act
            var meta = DamageApplicationHelper.CreateHealMeta();

            // Assert
            Assert.NotNull(meta);
        }

        [Fact]
        public void CreateHealMeta_CreatesWithCustomValues()
        {
            // Act
            var meta = DamageApplicationHelper.CreateHealMeta("potion", "item_use");

            // Assert
            Assert.NotNull(meta);
        }

        #endregion

        #region ShouldProcessAttackTriggers Tests

        [Theory]
        [InlineData(EventSource.Attack, true)]
        [InlineData(EventSource.Skill, true)]
        [InlineData(EventSource.EnemyAttack, true)]
        [InlineData(EventSource.Cast, true)]
        [InlineData(EventSource.Trigger, false)]
        public void ShouldProcessAttackTriggers_ReturnsCorrectValue(EventSource source, bool expected)
        {
            // Act
            var result = DamageApplicationHelper.ShouldProcessAttackTriggers(source);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ShouldProcessAttackTriggers_ReturnsFalseForTrigger()
        {
            // Act
            var result = DamageApplicationHelper.ShouldProcessAttackTriggers(EventSource.Trigger);

            // Assert - 触发技能不应该再触发其他技能，避免无限递归
            Assert.False(result);
        }

        #endregion

        #region IsTargetPlayer Tests

        [Theory]
        [InlineData(true, false)]  // 玩家施法 -> 目标是敌人
        [InlineData(false, true)]  // 怪物施法 -> 目标是玩家
        public void IsTargetPlayer_ReturnsCorrectValue(bool isCasterPlayer, bool expected)
        {
            // Act
            var result = DamageApplicationHelper.IsTargetPlayer(isCasterPlayer);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
