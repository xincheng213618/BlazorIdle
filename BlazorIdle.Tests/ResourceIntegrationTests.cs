using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Resources;
using System.Linq;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 2 集成测试：资源系统集成到战斗流程
    /// Phase 2 integration tests: Resource system integration into battle flow
    /// </summary>
    public class ResourceIntegrationTests
    {
        private MultiBattleInstance CreateTestBattle(int playerCount = 1, int enemyCount = 1, double critChance = 0.0)
        {
            var clock = new SimClock();
            var rng = new RngContext(12345);

            // 创建玩家队伍
            var playerTeam = new BattleTeam<Character>("team_player", "Player Team", TeamType.Player);
            for (int i = 0; i < playerCount; i++)
            {
                string memberId = $"player{i + 1}";
                var character = new Character
                {
                    MaxHp = 1000,
                    Hp = 1000,
                    DamagePerAttack = 50,
                    AttackRateAPS = 1.0,
                    CritChancePercent = critChance,
                    CritMultiplier = 2.0,
                    HastePercent = 0.0,
                    VariancePct = 0.0,
                    SpecialIntervalSec = 10.0,
                    SpecialDamage = 100
                };
                playerTeam.AddMember(memberId, character, maxHp: 1000, currentHp: 1000);
            }

            // 创建敌人队伍
            var enemyTeam = new BattleTeam<Enemy>("team_enemy", "Enemy Team", TeamType.Enemy);
            for (int i = 0; i < enemyCount; i++)
            {
                string memberId = $"enemy{i + 1}";
                var enemy = new Enemy
                {
                    MaxHp = 500,
                    Hp = 500,
                    DamagePerHit = 20,
                    AttackIntervalSec = 2.0,
                    VariancePct = 0.0
                };
                enemyTeam.AddMember(memberId, enemy, maxHp: 500, currentHp: 500);
            }

            var config = new MultiBattleConfig
            {
                PlayerTargetStrategy = TargetStrategy.Random,
                EnemyTargetStrategy = TargetStrategy.Random,
                SpecialIsAoe = false
            };

            return new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, config);
        }

        [Fact]
        public void MultiBattle_PlayerAttack_GainsRage()
        {
            // Arrange
            var battle = CreateTestBattle(playerCount: 1, enemyCount: 1);
            battle.Start();

            // Act - 执行多个 ticks 触发攻击
            for (int i = 0; i < 50; i++)
            {
                battle.AdvanceTick(100); // 100ms per tick, 攻击间隔 1秒
            }

            // Assert
            var resources = battle.GetResourceSnapshot();
            Assert.True(resources.ContainsKey("player1"));
            Assert.True(resources["player1"].ContainsKey("rage"));
            
            // 应该获得了一些 rage（至少 1，因为执行了足够的 ticks 触发攻击）
            Assert.InRange(resources["player1"]["rage"], 1, 10);
        }

        [Fact]
        public void MultiBattle_RageClampsAt10()
        {
            // Arrange
            var battle = CreateTestBattle(playerCount: 1, enemyCount: 1);
            battle.Start();

            // Act - 执行很多 ticks，确保触发足够多攻击
            for (int i = 0; i < 200; i++)
            {
                battle.AdvanceTick(100);
            }

            // Assert - rage 应该不超过 10
            var resources = battle.GetResourceSnapshot();
            Assert.True(resources.ContainsKey("player1"));
            Assert.InRange(resources["player1"]["rage"], 0, 10);
        }

        [Fact]
        public void MultiBattle_CritAttackGainsExtraRage()
        {
            // Arrange - 100% 暴击率
            var battle = CreateTestBattle(playerCount: 1, enemyCount: 1, critChance: 100.0);
            battle.Start();

            // Act - 执行足够的 ticks 触发至少一次攻击
            for (int i = 0; i < 15; i++)
            {
                battle.AdvanceTick(100); // 1.5秒，确保触发一次攻击
            }

            // Assert - 应该至少获得 2 点 rage（命中 +1，暴击 +1）
            var resources = battle.GetResourceSnapshot();
            Assert.True(resources.ContainsKey("player1"));
            Assert.True(resources["player1"].ContainsKey("rage"));
            Assert.InRange(resources["player1"]["rage"], 2, 10);
        }

        [Fact]
        public void MultiBattle_MultiplePlayersEachHaveOwnRage()
        {
            // Arrange
            var battle = CreateTestBattle(playerCount: 3, enemyCount: 2);
            battle.Start();

            // Act
            for (int i = 0; i < 50; i++)
            {
                battle.AdvanceTick(100);
            }

            // Assert - 每个玩家都应该有自己的 rage
            var resources = battle.GetResourceSnapshot();
            Assert.Equal(3, resources.Count);
            
            foreach (var playerId in new[] { "player1", "player2", "player3" })
            {
                Assert.True(resources.ContainsKey(playerId));
                Assert.True(resources[playerId].ContainsKey("rage"));
                Assert.InRange(resources[playerId]["rage"], 0, 10);
            }
        }

        [Fact]
        public void MultiBattle_NoCritAttack_GainsOneRagePerHit()
        {
            // Arrange - 0% 暴击率，确保不暴击
            var battle = CreateTestBattle(playerCount: 1, enemyCount: 1, critChance: 0.0);
            battle.Start();

            // 记录初始 rage
            var initialResources = battle.GetResourceSnapshot();
            int initialRage = initialResources["player1"]["rage"];

            // Act - 执行足够的 ticks 触发攻击
            for (int i = 0; i < 15; i++)
            {
                battle.AdvanceTick(100); // 1.5秒，至少触发一次攻击
            }

            // Assert - 应该获得了 rage（每次攻击 +1，无暴击额外奖励）
            var resources = battle.GetResourceSnapshot();
            int currentRage = resources["player1"]["rage"];
            int rageDelta = currentRage - initialRage;
            
            Assert.True(rageDelta >= 1, "Should gain at least 1 rage from attacks");
        }

        [Fact]
        public void MultiBattle_ResourceSnapshot_ReflectsCurrentState()
        {
            // Arrange
            var battle = CreateTestBattle(playerCount: 2, enemyCount: 1);
            battle.Start();

            // Act - 第一批 ticks
            for (int i = 0; i < 20; i++)
            {
                battle.AdvanceTick(100);
            }
            var snapshot1 = battle.GetResourceSnapshot();
            int rage1 = snapshot1["player1"]["rage"];

            // 第二批 ticks
            for (int i = 0; i < 20; i++)
            {
                battle.AdvanceTick(100);
            }
            var snapshot2 = battle.GetResourceSnapshot();
            int rage2 = snapshot2["player1"]["rage"];

            // Assert - rage 应该增加或达到上限
            Assert.True(rage2 >= rage1, "Rage should increase or stay at max");
            Assert.InRange(rage2, 0, 10);
        }
    }
}
