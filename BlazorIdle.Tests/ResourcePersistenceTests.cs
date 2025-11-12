using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Config;
using BlazorIdle.Shared.Models;
using System.Linq;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 2.6 测试：资源在副本波次之间的持久化
    /// Phase 2.6 tests: Resource persistence between dungeon waves
    /// </summary>
    public class ResourcePersistenceTests
    {
        [Fact]
        public void DungeonWaves_ResourcesPersistBetweenWaves()
        {
            // Arrange - 创建包含多波次的副本配置
            var clock = new SimClock();
            var rng = new RngContext(12345);
            
            var character = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                AttackRateAPS = 2.0,
                CritChancePercent = 0.0,
                CritMultiplier = 2.0,
                HastePercent = 0.0,
                VariancePct = 0.0,
                SpecialIntervalSec = 10.0,
                SpecialDamage = 200
            };

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("player1", character, maxHp: 1000, currentHp: 1000);

            var dungeonDef = new DungeonDef
            {
                Id = "test_dungeon",
                Name = "Test Dungeon",
                Waves = new System.Collections.Generic.List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Name = "Wave 1",
                        MonsterGroups = new System.Collections.Generic.List<MonsterGroup>
                        {
                            new MonsterGroup
                            {
                                MonsterId = "test_monster",
                                Count = 1,
                                HpMultiplier = 1.0,
                                DamageMultiplier = 1.0,
                                AttackSpeedMultiplier = 1.0
                            }
                        },
                        WaveStartDelaySec = 0.1
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Name = "Wave 2",
                        MonsterGroups = new System.Collections.Generic.List<MonsterGroup>
                        {
                            new MonsterGroup
                            {
                                MonsterId = "test_monster",
                                Count = 1,
                                HpMultiplier = 1.0,
                                DamageMultiplier = 1.0,
                                AttackSpeedMultiplier = 1.0
                            }
                        },
                        WaveStartDelaySec = 0.1
                    }
                },
                AllowRevive = true
            };

            var mockConfig = new MockGameConfigService();
            var dungeonManager = new DungeonManager(dungeonDef, clock, rng, playerTeam, mockConfig);

            // Act - 开始副本并推进到第一波
            dungeonManager.StartDungeon();
            
            // 推进战斗，让玩家攻击积累 rage
            for (int i = 0; i < 20; i++)
            {
                dungeonManager.AdvanceTick(100);
            }

            // 获取第一波结束时的资源状态
            int rageAfterWave1 = 0;
            if (dungeonManager.CurrentBattle != null)
            {
                var resources = dungeonManager.CurrentBattle.GetResourceSnapshot();
                if (resources.ContainsKey("player1") && resources["player1"].ContainsKey("rage"))
                {
                    rageAfterWave1 = resources["player1"]["rage"];
                }
            }

            // 继续推进直到第一波完成并进入第二波
            for (int i = 0; i < 100; i++)
            {
                dungeonManager.AdvanceTick(100);
                if (dungeonManager.CurrentWaveIndex >= 1)
                {
                    break;
                }
            }

            // 获取第二波开始时的资源状态
            int rageAtWave2Start = 0;
            if (dungeonManager.CurrentBattle != null)
            {
                var resources = dungeonManager.CurrentBattle.GetResourceSnapshot();
                if (resources.ContainsKey("player1") && resources["player1"].ContainsKey("rage"))
                {
                    rageAtWave2Start = resources["player1"]["rage"];
                }
            }

            // Assert - 第二波的 rage 应该保持第一波的值（或继续积累）
            Assert.True(rageAfterWave1 > 0, "Player should have gained rage in wave 1");
            Assert.True(rageAtWave2Start >= rageAfterWave1, "Rage should be preserved or increased in wave 2");
        }

        [Fact]
        public void MultiBattle_WithPreservedResources_RestoresCorrectly()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            
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
                SpecialIntervalSec = 10.0,
                SpecialDamage = 100
            };

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("player1", character, maxHp: 1000, currentHp: 1000);

            var enemyTeam = new BattleTeam<Enemy>("enemy_team", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy
            {
                MaxHp = 500,
                Hp = 500,
                DamagePerHit = 20,
                AttackIntervalSec = 2.0,
                VariancePct = 0.0
            };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 500, currentHp: 500);

            // 创建第一个战斗并积累资源
            var battle1 = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam);
            battle1.Start();
            
            for (int i = 0; i < 30; i++)
            {
                battle1.AdvanceTick(100);
            }

            var resourcesAfterBattle1 = battle1.GetResourceSnapshot();
            int rage1 = resourcesAfterBattle1["player1"]["rage"];

            // 保存资源快照
            var preservedResources = new System.Collections.Generic.Dictionary<string, ResourceBucketCollection>();
            foreach (var (playerId, resources) in resourcesAfterBattle1)
            {
                var collection = new ResourceBucketCollection();
                foreach (var (bucketId, value) in resources)
                {
                    var bucket = collection.GetBucket(bucketId);
                    bucket.Reset(value);
                }
                preservedResources[playerId] = collection;
            }

            // 创建新的敌人队伍（模拟新波次）
            var enemyTeam2 = new BattleTeam<Enemy>("enemy_team2", "Enemy Team 2", TeamType.Enemy);
            var enemy2 = new Enemy
            {
                MaxHp = 600,
                Hp = 600,
                DamagePerHit = 25,
                AttackIntervalSec = 2.0,
                VariancePct = 0.0
            };
            enemyTeam2.AddMember("enemy2", enemy2, maxHp: 600, currentHp: 600);

            // Act - 创建第二个战斗，传入保留的资源
            var battle2 = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam2, null, preservedResources);
            battle2.Start(resetPlayerTeam: false);

            var resourcesAtBattle2Start = battle2.GetResourceSnapshot();
            int rage2 = resourcesAtBattle2Start["player1"]["rage"];

            // Assert - 资源应该被保留
            Assert.Equal(rage1, rage2);
        }

        [Fact]
        public void MultiBattle_WithoutPreservedResources_StartsAtZero()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            
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
                SpecialIntervalSec = 10.0,
                SpecialDamage = 100
            };

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("player1", character, maxHp: 1000, currentHp: 1000);

            var enemyTeam = new BattleTeam<Enemy>("enemy_team", "Enemy Team", TeamType.Enemy);
            var enemy = new Enemy
            {
                MaxHp = 500,
                Hp = 500,
                DamagePerHit = 20,
                AttackIntervalSec = 2.0,
                VariancePct = 0.0
            };
            enemyTeam.AddMember("enemy1", enemy, maxHp: 500, currentHp: 500);

            // Act - 创建战斗不传入保留资源
            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, null, null);
            battle.Start();

            var resources = battle.GetResourceSnapshot();
            int rage = resources["player1"]["rage"];

            // Assert - 资源应该从 0 开始
            Assert.Equal(0, rage);
        }

        [Fact]
        public void DungeonWaves_ProfessionSpecificResourceMax_PreservedCorrectly()
        {
            // Phase 2.7.2: 测试职业特定资源上限在波次间正确保持
            // Phase 2.7.2: Test profession-specific resource max is preserved correctly between waves
            
            // Arrange - 创建战士角色（max rage = 5）
            var clock = new SimClock();
            var rng = new RngContext(12345);
            
            var warrior = new Character
            {
                ActiveCombatProfessionId = "warrior",
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                AttackRateAPS = 2.0,
                CritChancePercent = 0.0,
                CritMultiplier = 2.0,
                HastePercent = 0.0,
                VariancePct = 0.0,
                SpecialIntervalSec = 10.0,
                SpecialDamage = 200
            };

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("warrior1", warrior, maxHp: 1000, currentHp: 1000);

            var dungeonDef = new DungeonDef
            {
                Id = "test_dungeon",
                Name = "Test Dungeon",
                Waves = new System.Collections.Generic.List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Name = "Wave 1",
                        MonsterGroups = new System.Collections.Generic.List<MonsterGroup>
                        {
                            new MonsterGroup
                            {
                                MonsterId = "test_monster",
                                Count = 1,
                                HpMultiplier = 1.0,
                                DamageMultiplier = 1.0,
                                AttackSpeedMultiplier = 1.0
                            }
                        },
                        WaveStartDelaySec = 0.1
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Name = "Wave 2",
                        MonsterGroups = new System.Collections.Generic.List<MonsterGroup>
                        {
                            new MonsterGroup
                            {
                                MonsterId = "test_monster",
                                Count = 1,
                                HpMultiplier = 1.0,
                                DamageMultiplier = 1.0,
                                AttackSpeedMultiplier = 1.0
                            }
                        },
                        WaveStartDelaySec = 0.1
                    }
                },
                AllowRevive = true
            };

            // 配置战士职业资源（rage, max=5, initial=0）
            var professionConfigs = new System.Collections.Generic.Dictionary<string, ProfessionResourceConfig>
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

            var mockConfig = new MockGameConfigService();
            var dungeonManager = new DungeonManager(dungeonDef, clock, rng, playerTeam, mockConfig, professionConfigs);

            // Act - 开始副本并积累资源到上限
            dungeonManager.StartDungeon();
            
            // 推进战斗，让战士攻击积累满 rage（5点）
            for (int i = 0; i < 30; i++)
            {
                dungeonManager.AdvanceTick(100);
            }

            // 获取第一波的资源
            int rageWave1 = 0;
            if (dungeonManager.CurrentBattle != null)
            {
                var resources = dungeonManager.CurrentBattle.GetResourceSnapshot();
                if (resources.ContainsKey("warrior1") && resources["warrior1"].ContainsKey("rage"))
                {
                    rageWave1 = resources["warrior1"]["rage"];
                }
            }

            // 继续推进到第二波
            for (int i = 0; i < 100; i++)
            {
                dungeonManager.AdvanceTick(100);
                if (dungeonManager.CurrentWaveIndex >= 1)
                {
                    break;
                }
            }

            // 再多攻击几次，测试上限是否仍然是 5
            for (int i = 0; i < 10; i++)
            {
                dungeonManager.AdvanceTick(100);
            }

            // 获取第二波的资源
            int rageWave2 = 0;
            if (dungeonManager.CurrentBattle != null)
            {
                var resources = dungeonManager.CurrentBattle.GetResourceSnapshot();
                if (resources.ContainsKey("warrior1") && resources["warrior1"].ContainsKey("rage"))
                {
                    rageWave2 = resources["warrior1"]["rage"];
                }
            }

            // Assert - 战士的 rage 应该 clamp 到 5（不是默认的 10）
            Assert.True(rageWave1 > 0, "Warrior should have gained rage in wave 1");
            Assert.True(rageWave1 <= 5, $"Warrior rage should be clamped to 5, but was {rageWave1}");
            Assert.True(rageWave2 <= 5, $"Warrior rage should still be clamped to 5 in wave 2, but was {rageWave2}");
        }
    }

    // Mock GameConfigService for testing
    internal class MockGameConfigService : IGameConfigService
    {
        public System.Collections.Generic.IReadOnlyList<ProfessionDef> Professions => 
            new System.Collections.Generic.List<ProfessionDef>();
        
        public System.Collections.Generic.IReadOnlyList<MonsterDef> Monsters => 
            new System.Collections.Generic.List<MonsterDef>
            {
                new MonsterDef
                {
                    Id = "test_monster",
                    Name = "Test Monster",
                    MaxHp = 100,
                    DamagePerHit = 10,
                    AttackIntervalSec = 2.0,
                    VariancePct = 0.0
                }
            };
        
        public System.Collections.Generic.IReadOnlyList<Shared.Models.ItemDefinition> Items => 
            new System.Collections.Generic.List<Shared.Models.ItemDefinition>();
        
        public System.Collections.Generic.IReadOnlyList<BattleScenarioDef> BattleScenarios => 
            new System.Collections.Generic.List<BattleScenarioDef>();
        
        public System.Collections.Generic.IReadOnlyList<DungeonDef> Dungeons => 
            new System.Collections.Generic.List<DungeonDef>();
        
        public System.Collections.Generic.IReadOnlyList<BattleConfigDef> BattleConfigs => 
            new System.Collections.Generic.List<BattleConfigDef>();
        
        public System.Collections.Generic.IReadOnlyDictionary<string, ProfessionAttributeConfig> ProfessionAttributes => 
            new System.Collections.Generic.Dictionary<string, ProfessionAttributeConfig>();
        
        public int MaxProfessionLevel => 100;
        
        public string Version => "test";
        
        public bool IsLoaded => true;

        public System.Threading.Tasks.Task EnsureLoadedAsync(System.Threading.CancellationToken ct = default) => 
            System.Threading.Tasks.Task.CompletedTask;
        
        public MonsterDef? GetMonster(string id) => Monsters.FirstOrDefault(m => m.Id == id);
        
        public ProfessionDef? GetProfession(string id) => null;
        
        public Shared.Models.ItemDefinition? GetItem(string id) => null;
        
        public BattleScenarioDef? GetBattleScenario(string id) => null;
        
        public DungeonDef? GetDungeon(string id) => null;
        
        public BattleConfigDef? GetBattleConfig(string id) => null;
    }
    
    public class DungeonResourceResetTests
    {
        [Fact]
        public void DungeonRestart_ResourcesResetToInitialValues()
        {
            // Phase 2.7.2: 测试副本重启时资源重置为初始值
            // Phase 2.7.2: Test resources reset to initial values when dungeon restarts
            
            // Arrange - 创建战士角色（max rage = 5, initial = 0）
            var clock = new SimClock();
            var rng = new RngContext(12345);
            
            var warrior = new Character
            {
                ActiveCombatProfessionId = "warrior",
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                AttackRateAPS = 2.0,
                CritChancePercent = 0.0,
                CritMultiplier = 2.0,
                HastePercent = 0.0,
                VariancePct = 0.0,
                SpecialIntervalSec = 10.0,
                SpecialDamage = 200
            };

            var playerTeam = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            playerTeam.AddMember("warrior1", warrior, maxHp: 1000, currentHp: 1000);

            var dungeonDef = new DungeonDef
            {
                Id = "test_dungeon",
                Name = "Test Dungeon",
                AllowRevive = true,
                Waves = new System.Collections.Generic.List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Name = "Wave 1",
                        MonsterGroups = new System.Collections.Generic.List<MonsterGroup>
                        {
                            new MonsterGroup
                            {
                                MonsterId = "test_monster",
                                Count = 1,
                                HpMultiplier = 1.0,
                                DamageMultiplier = 1.0,
                                AttackSpeedMultiplier = 1.0
                            }
                        }
                    }
                }
            };

            var profConfig = new ProfessionResourceConfig
            {
                Id = "rage",
                Name = "怒气",
                Max = 5,
                Initial = 0,
                GainPerAttack = 1,
                GainPerCritExtra = 1
            };

            var professionResourceConfigs = new System.Collections.Generic.Dictionary<string, ProfessionResourceConfig>
            {
                { "warrior", profConfig }
            };

            var mockGameConfig = new MockGameConfigService();
            var dungeonManager = new DungeonManager(dungeonDef, clock, rng, playerTeam, mockGameConfig, professionResourceConfigs);
            
            dungeonManager.EnableAutoRepeat(100); // 短延迟以便快速测试

            // Act - 启动副本，打一波，让资源积累
            dungeonManager.StartDungeon();
            
            // 推进到战斗开始
            for (int i = 0; i < 5; i++)
            {
                dungeonManager.AdvanceTick(100);
            }
            
            // 推进战斗让资源积累
            for (int i = 0; i < 30; i++)
            {
                dungeonManager.AdvanceTick(100);
            }
            
            // 检查资源已经积累
            var resourcesAfterWave1 = dungeonManager.CurrentBattle?.GetResourceSnapshot();
            Assert.NotNull(resourcesAfterWave1);
            int rageAfterWave1 = resourcesAfterWave1["warrior1"]["rage"];
            Assert.True(rageAfterWave1 > 0, "Resources should have accumulated during first run");
            
            // 快速完成副本（让敌人死亡）
            for (int i = 0; i < 1000; i++)
            {
                dungeonManager.AdvanceTick(100);
                if (dungeonManager.State == DungeonState.Completed || dungeonManager.State == DungeonState.CompletionDelay)
                {
                    break;
                }
            }
            
            // 等待自动重启
            for (int i = 0; i < 50; i++)
            {
                dungeonManager.AdvanceTick(100);
                if (dungeonManager.State == DungeonState.Fighting || dungeonManager.State == DungeonState.Preparing)
                {
                    break;
                }
            }
            
            // Assert - 重启后资源应该重置为初始值 0
            Assert.True(dungeonManager.State == DungeonState.Fighting || dungeonManager.State == DungeonState.Preparing || dungeonManager.State == DungeonState.WaveStartDelay);
            
            // 推进到战斗开始
            for (int i = 0; i < 10; i++)
            {
                dungeonManager.AdvanceTick(100);
                if (dungeonManager.CurrentBattle != null)
                {
                    break;
                }
            }
            
            var resourcesAfterRestart = dungeonManager.CurrentBattle?.GetResourceSnapshot();
            Assert.NotNull(resourcesAfterRestart);
            int rageAfterRestart = resourcesAfterRestart["warrior1"]["rage"];
            
            // 资源应该重置为初始值 0（战士的初始值）
            Assert.Equal(0, rageAfterRestart);
        }
    }
}
