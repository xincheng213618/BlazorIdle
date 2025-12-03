using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Combat;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 9: Tests for Buff UI snapshot methods
    /// </summary>
    public class Phase9BuffUITests
    {
        /// <summary>
        /// Test GetPlayerBuffs returns empty list for non-existent player
        /// </summary>
        [Fact]
        public void GetPlayerBuffs_NonExistentPlayer_ReturnsEmptyList()
        {
            // Arrange
            var battle = CreateTestBattle();
            battle.Start();
            
            // Act
            var buffs = battle.GetPlayerBuffs("non_existent_player");
            
            // Assert
            Assert.NotNull(buffs);
            Assert.Empty(buffs);
        }

        /// <summary>
        /// Test GetPlayerBuffs returns correct buff list
        /// </summary>
        [Fact]
        public void GetPlayerBuffs_WithActiveBuffs_ReturnsCorrectList()
        {
            // Arrange
            var battle = CreateTestBattle();
            battle.Start();
            
            var playerId = "player1";
            var owner = GetPlayerBuffOwner(battle, playerId);
            
            // Apply a buff directly
            var buff = new BuffInstance(
                id: "test_buff",
                ownerId: playerId,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.2) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0,
                tickIntervalSec: null,
                maxStacks: 0
            );
            
            owner.ApplyBuff(buff);
            
            // Act
            var buffs = battle.GetPlayerBuffs(playerId);
            
            // Assert
            Assert.NotNull(buffs);
            Assert.Single(buffs);
            Assert.Equal("test_buff", buffs[0].Id);
            Assert.Equal(BuffKind.Buff, buffs[0].Kind);
            Assert.Equal(10.0, buffs[0].RemainingDurationSec);
        }

        /// <summary>
        /// Test GetEnemyBuffs returns empty list for non-existent enemy
        /// </summary>
        [Fact]
        public void GetEnemyBuffs_NonExistentEnemy_ReturnsEmptyList()
        {
            // Arrange
            var battle = CreateTestBattle();
            battle.Start();
            
            // Act
            var buffs = battle.GetEnemyBuffs("non_existent_enemy");
            
            // Assert
            Assert.NotNull(buffs);
            Assert.Empty(buffs);
        }

        /// <summary>
        /// Test GetEnemyBuffs returns correct buff list
        /// </summary>
        [Fact]
        public void GetEnemyBuffs_WithActiveDebuffs_ReturnsCorrectList()
        {
            // Arrange
            var battle = CreateTestBattle();
            battle.Start();
            
            var enemyId = "enemy1"; // From CreateTestBattle
            var owner = GetEnemyBuffOwner(battle, enemyId);
            
            // Apply a debuff to enemy
            var debuff = new BuffInstance(
                id: "test_debuff",
                ownerId: enemyId,
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect> { BuffEffect.StatReduction("DamagePerAttack", 0.3) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 8.0,
                tickIntervalSec: null,
                maxStacks: 0
            );
            
            owner.ApplyBuff(debuff);
            
            // Act
            var buffs = battle.GetEnemyBuffs(enemyId);
            
            // Assert
            Assert.NotNull(buffs);
            Assert.Single(buffs);
            Assert.Equal("test_debuff", buffs[0].Id);
            Assert.Equal(BuffKind.Debuff, buffs[0].Kind);
            Assert.Equal(8.0, buffs[0].RemainingDurationSec);
        }

        /// <summary>
        /// Test GetPlayerBuffs returns multiple buffs correctly
        /// </summary>
        [Fact]
        public void GetPlayerBuffs_MultipleBuffs_ReturnsAllBuffs()
        {
            // Arrange
            var battle = CreateTestBattle();
            battle.Start();
            
            var playerId = "player1"; // From CreateTestBattle
            var owner = GetPlayerBuffOwner(battle, playerId);
            
            // Apply multiple buffs
            var buff1 = new BuffInstance(
                id: "buff_1",
                ownerId: playerId,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.2) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0,
                tickIntervalSec: null,
                maxStacks: 0
            );
            
            var buff2 = new BuffInstance(
                id: "buff_2",
                ownerId: playerId,
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.StatAdditive("CritChancePercent", 5.0) },
                stackingPolicy: BuffStackingPolicy.Stack,
                durationSec: 15.0,
                tickIntervalSec: null,
                maxStacks: 3
            );
            
            owner.ApplyBuff(buff1);
            owner.ApplyBuff(buff2);
            
            // Act
            var buffs = battle.GetPlayerBuffs(playerId);
            
            // Assert
            Assert.NotNull(buffs);
            Assert.Equal(2, buffs.Count);
            Assert.Contains(buffs, b => b.Id == "buff_1");
            Assert.Contains(buffs, b => b.Id == "buff_2");
        }

        /// <summary>
        /// Test GetEnemyBuffs returns DoT buff with correct tick interval
        /// </summary>
        [Fact]
        public void GetEnemyBuffs_WithDoT_ReturnsCorrectTickInterval()
        {
            // Arrange
            var battle = CreateTestBattle();
            battle.Start();
            
            var enemyId = "enemy1"; // From CreateTestBattle
            var owner = GetEnemyBuffOwner(battle, enemyId);
            
            // Apply DoT debuff
            var dot = new BuffInstance(
                id: "dot_debuff",
                ownerId: enemyId,
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect> { BuffEffect.DamageOverTime(5) },
                stackingPolicy: BuffStackingPolicy.Stack,
                durationSec: 12.0,
                tickIntervalSec: 2.0,
                maxStacks: 5
            );
            
            owner.ApplyBuff(dot);
            
            // Act
            var buffs = battle.GetEnemyBuffs(enemyId);
            
            // Assert
            Assert.NotNull(buffs);
            Assert.Single(buffs);
            var retrievedDot = buffs[0];
            Assert.Equal("dot_debuff", retrievedDot.Id);
            Assert.Equal(2.0, retrievedDot.TickIntervalSec);
            Assert.True(retrievedDot.HasDamageOverTime());
            Assert.Equal(5, retrievedDot.GetDamagePerTick());
        }

        // Helper methods
        
        private MultiBattleInstance CreateTestBattle()
        {
            var clock = new SimClock();
            var rng = new RngContext(12345);

            // 创建玩家队伍
            var playerTeam = new BattleTeam<Character>("team_player", "Player Team", TeamType.Player);
            var character = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                AttackRateAPS = 1.0,
                CritChancePercent = 0.0,
                HastePercent = 0.0,
                VariancePct = 0.0,
                SpecialIntervalSec = 10.0
            };
            
            // 初始化新伤害系统
            character.CombatStats = new CombatStats
            {
                AttackFinal = 50,
                CritDamageBonusPercent = 100  // 相当于原来的 2.0 倍暴击
            };
            
            playerTeam.AddMember("player1", character, maxHp: 1000, currentHp: 1000);

            // 创建敌人队伍
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
                SpecialIsAoe = false
            };

            return new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, config);
        }

        private CharacterBuffOwner GetPlayerBuffOwner(MultiBattleInstance battle, string playerId)
        {
            // Use reflection to access private field
            var field = typeof(MultiBattleInstance).GetField("_playerBuffOwners", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var owners = field?.GetValue(battle) as Dictionary<string, CharacterBuffOwner>;
            return owners?[playerId] ?? throw new InvalidOperationException($"Player {playerId} not found");
        }

        private EnemyBuffOwner GetEnemyBuffOwner(MultiBattleInstance battle, string enemyId)
        {
            // Use reflection to access private field
            var field = typeof(MultiBattleInstance).GetField("_enemyBuffOwners", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var owners = field?.GetValue(battle) as Dictionary<string, EnemyBuffOwner>;
            return owners?[enemyId] ?? throw new InvalidOperationException($"Enemy {enemyId} not found");
        }
    }
}
