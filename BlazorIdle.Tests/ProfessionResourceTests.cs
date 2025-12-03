using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Combat;
using BlazorIdle.Shared.Models;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 2.7 测试：职业特定资源系统
    /// Phase 2.7 tests: Profession-specific resource system
    /// 
    /// 旧系统清理：这些测试已更新为使用新的 CombatStats 系统
    /// Legacy cleanup: These tests have been updated to use the new CombatStats system
    /// </summary>
    public class ProfessionResourceTests
    {
        /// <summary>
        /// 创建用于测试的 Character（使用新伤害系统）
        /// Create Character for testing (using new damage system)
        /// </summary>
        private static Character CreateTestCharacter(
            string professionId = "warrior",
            int maxHp = 1000,
            int attackFinal = 50,
            double attackRate = 1.0,
            double critChance = 0.0)
        {
            return new Character
            {
                ActiveCombatProfessionId = professionId,
                MaxHp = maxHp,
                Hp = maxHp,
                AttackRateAPS = attackRate,
                CritChancePercent = critChance,
                HastePercent = 0.0,
                VariancePct = 0.0,
                SpecialIntervalSec = 10.0,
                CombatStats = new CombatStats
                {
                    AttackFinal = attackFinal,
                    CritChancePercent = critChance
                }
            };
        }

        [Fact]
        public void ResourceBucketCollection_CustomResourceId_CreatesCorrectly()
        {
            // Arrange & Act
            var collection = new ResourceBucketCollection("mana", 10, 5);

            // Assert
            Assert.True(collection.HasBucket("mana"));
            var bucket = collection.GetBucket("mana");
            Assert.Equal(5, bucket.Current);
            Assert.Equal(10, bucket.Max);
        }

        [Fact]
        public void ResourceBucketCollection_EnergyResource_StartsAtMax()
        {
            // Arrange & Act
            var collection = new ResourceBucketCollection("energy", 3, 3);

            // Assert
            var bucket = collection.GetBucket("energy");
            Assert.Equal(3, bucket.Current);
            Assert.Equal(3, bucket.Max);
        }

        [Fact]
        public void MultiBattle_WarriorResource_StartsAtZeroMax5()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);

            var warrior = CreateTestCharacter(professionId: "warrior", maxHp: 1000, attackFinal: 50, attackRate: 1.0);

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("warrior1", warrior, maxHp: 1000, currentHp: 1000);

            var enemyTeam = new BattleTeam<Enemy>("enemy_team", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy { MaxHp = 500, Hp = 500, DamagePerHit = 20, AttackIntervalSec = 2.0 };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 500, currentHp: 500);

            var professionConfigs = new Dictionary<string, ProfessionResourceConfig>
            {
                ["warrior"] = new ProfessionResourceConfig
                {
                    Id = "rage",
                    Name = "怒气",
                    Max = 5,
                    Initial = 0,
                    GainPerAttack = 1,
                    GainPerCritExtra = 1
                }
            };

            // Act
            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, null, null, professionConfigs);
            battle.Start();

            var resources = battle.GetResourceSnapshot();

            // Assert
            Assert.True(resources.ContainsKey("warrior1"));
            Assert.True(resources["warrior1"].ContainsKey("rage"));
            Assert.Equal(0, resources["warrior1"]["rage"]); // 初始值为0
        }

        [Fact]
        public void MultiBattle_MageResource_StartsAtHalfMax10()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);

            var mage = CreateTestCharacter(professionId: "mage", maxHp: 800, attackFinal: 40, attackRate: 1.0);

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("mage1", mage, maxHp: 800, currentHp: 800);

            var enemyTeam = new BattleTeam<Enemy>("enemy_team", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy { MaxHp = 500, Hp = 500, DamagePerHit = 20, AttackIntervalSec = 2.0 };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 500, currentHp: 500);

            var professionConfigs = new Dictionary<string, ProfessionResourceConfig>
            {
                ["mage"] = new ProfessionResourceConfig
                {
                    Id = "mana",
                    Name = "法力",
                    Max = 10,
                    Initial = 5,
                    GainPerAttack = 1,
                    GainPerCritExtra = 1
                }
            };

            // Act
            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, null, null, professionConfigs);
            battle.Start();

            var resources = battle.GetResourceSnapshot();

            // Assert
            Assert.True(resources.ContainsKey("mage1"));
            Assert.True(resources["mage1"].ContainsKey("mana"));
            Assert.Equal(5, resources["mage1"]["mana"]); // 初始值为5（一半）
        }

        [Fact]
        public void MultiBattle_RangerResource_StartsAtFullMax3()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);

            var ranger = CreateTestCharacter(professionId: "ranger", maxHp: 900, attackFinal: 45, attackRate: 1.5);

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("ranger1", ranger, maxHp: 900, currentHp: 900);

            var enemyTeam = new BattleTeam<Enemy>("enemy_team", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy { MaxHp = 500, Hp = 500, DamagePerHit = 20, AttackIntervalSec = 2.0 };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 500, currentHp: 500);

            var professionConfigs = new Dictionary<string, ProfessionResourceConfig>
            {
                ["ranger"] = new ProfessionResourceConfig
                {
                    Id = "energy",
                    Name = "能量",
                    Max = 3,
                    Initial = 3,
                    GainPerAttack = 1,
                    GainPerCritExtra = 1
                }
            };

            // Act
            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, null, null, professionConfigs);
            battle.Start();

            var resources = battle.GetResourceSnapshot();

            // Assert
            Assert.True(resources.ContainsKey("ranger1"));
            Assert.True(resources["ranger1"].ContainsKey("energy"));
            Assert.Equal(3, resources["ranger1"]["energy"]); // 初始值为3（满）
        }

        [Fact]
        public void MultiBattle_WarriorResource_ClampsAt5()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);

            var warrior = CreateTestCharacter(professionId: "warrior", maxHp: 1000, attackFinal: 50, attackRate: 2.0);

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("warrior1", warrior, maxHp: 1000, currentHp: 1000);

            var enemyTeam = new BattleTeam<Enemy>("enemy_team", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy { MaxHp = 1000, Hp = 1000, DamagePerHit = 20, AttackIntervalSec = 2.0 };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 1000, currentHp: 1000);

            var professionConfigs = new Dictionary<string, ProfessionResourceConfig>
            {
                ["warrior"] = new ProfessionResourceConfig
                {
                    Id = "rage",
                    Name = "怒气",
                    Max = 5,
                    Initial = 0,
                    GainPerAttack = 1,
                    GainPerCritExtra = 1
                }
            };

            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, null, null, professionConfigs);
            battle.Start();

            // Act - 让战士攻击多次
            for (int i = 0; i < 30; i++)
            {
                battle.AdvanceTick(100);
            }

            var resources = battle.GetResourceSnapshot();

            // Assert - 怒气应该 clamp 在 5
            Assert.Equal(5, resources["warrior1"]["rage"]);
        }

        [Fact]
        public void MultiBattle_MageResource_ClampsAt10()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);

            var mage = CreateTestCharacter(professionId: "mage", maxHp: 800, attackFinal: 40, attackRate: 2.0);

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("mage1", mage, maxHp: 800, currentHp: 800);

            var enemyTeam = new BattleTeam<Enemy>("enemy_team", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy { MaxHp = 1000, Hp = 1000, DamagePerHit = 20, AttackIntervalSec = 2.0 };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 1000, currentHp: 1000);

            var professionConfigs = new Dictionary<string, ProfessionResourceConfig>
            {
                ["mage"] = new ProfessionResourceConfig
                {
                    Id = "mana",
                    Name = "法力",
                    Max = 10,
                    Initial = 5,
                    GainPerAttack = 1,
                    GainPerCritExtra = 1
                }
            };

            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, null, null, professionConfigs);
            battle.Start();

            // Act - 让法师攻击多次（从5开始）
            for (int i = 0; i < 30; i++)
            {
                battle.AdvanceTick(100);
            }

            var resources = battle.GetResourceSnapshot();

            // Assert - 法力应该 clamp 在 10
            Assert.Equal(10, resources["mage1"]["mana"]);
        }

        [Fact]
        public void MultiBattle_MixedProfessions_EachUsesOwnResourceConfig()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);

            var warrior = CreateTestCharacter(professionId: "warrior", maxHp: 1000, attackFinal: 50, attackRate: 1.0);
            var mage = CreateTestCharacter(professionId: "mage", maxHp: 800, attackFinal: 40, attackRate: 1.0);

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("warrior1", warrior, maxHp: 1000, currentHp: 1000);
            playerTeam.AddMember("mage1", mage, maxHp: 800, currentHp: 800);

            var enemyTeam = new BattleTeam<Enemy>("enemy_team", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy { MaxHp = 1000, Hp = 1000, DamagePerHit = 20, AttackIntervalSec = 2.0 };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 1000, currentHp: 1000);

            var professionConfigs = new Dictionary<string, ProfessionResourceConfig>
            {
                ["warrior"] = new ProfessionResourceConfig
                {
                    Id = "rage",
                    Name = "怒气",
                    Max = 5,
                    Initial = 0,
                    GainPerAttack = 1,
                    GainPerCritExtra = 1
                },
                ["mage"] = new ProfessionResourceConfig
                {
                    Id = "mana",
                    Name = "法力",
                    Max = 10,
                    Initial = 5,
                    GainPerAttack = 1,
                    GainPerCritExtra = 1
                }
            };

            // Act
            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, null, null, professionConfigs);
            battle.Start();

            var resources = battle.GetResourceSnapshot();

            // Assert
            // 战士使用 rage，初始为 0，上限 5
            Assert.True(resources.ContainsKey("warrior1"));
            Assert.True(resources["warrior1"].ContainsKey("rage"));
            Assert.Equal(0, resources["warrior1"]["rage"]);

            // 法师使用 mana，初始为 5，上限 10
            Assert.True(resources.ContainsKey("mage1"));
            Assert.True(resources["mage1"].ContainsKey("mana"));
            Assert.Equal(5, resources["mage1"]["mana"]);
        }

        [Fact]
        public void MultiBattle_NoProfessionConfig_FallsBackToDefault()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);

            var character = CreateTestCharacter(professionId: "unknown", maxHp: 1000, attackFinal: 50, attackRate: 1.0);

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("player1", character, maxHp: 1000, currentHp: 1000);

            var enemyTeam = new BattleTeam<Enemy>("enemy_team", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy { MaxHp = 500, Hp = 500, DamagePerHit = 20, AttackIntervalSec = 2.0 };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 500, currentHp: 500);

            // Act - 不传入职业配置
            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam);
            battle.Start();

            var resources = battle.GetResourceSnapshot();

            // Assert - 应该使用默认的 rage 资源（max=10, initial=0）
            Assert.True(resources.ContainsKey("player1"));
            Assert.True(resources["player1"].ContainsKey("rage"));
            Assert.Equal(0, resources["player1"]["rage"]);
        }
    }
}
