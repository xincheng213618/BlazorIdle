using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorIdle.Game.Config;

namespace BlazorIdle.Game
{
    /// <summary>
    /// 副本管理器 - 管理副本进度、波次切换和循环战斗
    /// Dungeon manager - manages dungeon progress, wave transitions and loop battles
    /// </summary>
    public class DungeonManager
    {
        private readonly DungeonDef _dungeonDef;
        private readonly IGameClock _clock;
        private readonly RngContext _rng;
        private readonly BattleTeam<Character> _playerTeam;
        private readonly IGameConfigService _gameConfig;

        // 副本状态
        private DungeonState _state = DungeonState.NotStarted;
        private int _currentWaveIndex = -1;
        private DungeonWave? _currentWave;
        private MultiBattleInstance? _currentBattle;
        private BattleTeam<Enemy>? _currentEnemyTeam;  // 当前敌人队伍引用

        // 波次计时器
        private int _nextActionAtMs = 0;

        // 副本统计
        private int _dungeonStartTimeMs = 0;
        private int _completionCount = 0;
        private int _totalKills = 0;
        private int _totalDeaths = 0;
        private readonly Dictionary<string, int> _totalLoot = new();

        // 自动循环设置
        private bool _autoRepeatEnabled = false;
        private int _autoRepeatDelayMs = 2000;

        // 事件
        public event Action<DungeonProgressEvent>? ProgressChanged;
        public event Action<DungeonWaveEvent>? WaveChanged;
        public event Action<DungeonCompleteEvent>? DungeonCompleted;
        public event Action<MultiCombatEvent>? CombatEventFired;
        public event Action<LootDropEvent>? LootDropped;
        public event Action<ExperienceGainEvent>? ExperienceGained;
        public event Action<DungeonStatsEvent>? StatsUpdated;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public DungeonManager(
            DungeonDef dungeonDef,
            IGameClock clock,
            RngContext rng,
            BattleTeam<Character> playerTeam,
            IGameConfigService gameConfig)
        {
            _dungeonDef = dungeonDef ?? throw new ArgumentNullException(nameof(dungeonDef));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _playerTeam = playerTeam ?? throw new ArgumentNullException(nameof(playerTeam));
            _gameConfig = gameConfig ?? throw new ArgumentNullException(nameof(gameConfig));
        }

        /// <summary>
        /// 当前副本状态
        /// Current dungeon state
        /// </summary>
        public DungeonState State => _state;

        /// <summary>
        /// 当前波次索引
        /// Current wave index
        /// </summary>
        public int CurrentWaveIndex => _currentWaveIndex;

        /// <summary>
        /// 总波次数
        /// Total wave count
        /// </summary>
        public int TotalWaves => _dungeonDef.Waves.Count;

        /// <summary>
        /// 是否启用自动循环
        /// Whether auto-repeat is enabled
        /// </summary>
        public bool IsAutoRepeatEnabled => _autoRepeatEnabled;

        /// <summary>
        /// 副本完成次数
        /// Dungeon completion count
        /// </summary>
        public int CompletionCount => _completionCount;

        // 新增：对 UI 暴露当前战斗实例（只读）
        public MultiBattleInstance? CurrentBattle => _currentBattle;
        /// <summary>
        /// 开始副本
        /// Start dungeon
        /// </summary>
        public void StartDungeon()
        {
            if (_state != DungeonState.NotStarted && _state != DungeonState.Completed)
            {
                return;
            }

            _currentWaveIndex = -1;
            _state = DungeonState.Preparing;
            _dungeonStartTimeMs = _clock.NowMs;
            _totalKills = 0;
            _totalDeaths = 0;
            _totalLoot.Clear();

            // 恢复玩家队伍
            _playerTeam.ReviveAll(true);

            FireProgressEvent();

            // 准备第一波
            PrepareNextWave();
        }

        /// <summary>
        /// 停止副本
        /// Stop dungeon
        /// </summary>
        public void StopDungeon()
        {
            if (_currentBattle != null && _currentBattle.IsRunning)
            {
                _currentBattle.Stop();
                UnsubscribeBattleEvents();
            }

            _state = DungeonState.Stopped;
            _autoRepeatEnabled = false;
            FireProgressEvent();
        }

        /// <summary>
        /// 启用自动循环
        /// Enable auto-repeat
        /// </summary>
        public void EnableAutoRepeat(int delayMs = 2000)
        {
            _autoRepeatEnabled = true;
            _autoRepeatDelayMs = Math.Max(0, delayMs);
        }

        /// <summary>
        /// 禁用自动循环
        /// Disable auto-repeat
        /// </summary>
        public void DisableAutoRepeat()
        {
            _autoRepeatEnabled = false;
        }

        /// <summary>
        /// 推进副本tick
        /// Advance dungeon tick
        /// </summary>
        public void AdvanceTick(int tickMs)
        {
            // 根据状态决定是否需要手动推进时钟
            // 战斗状态下，MultiBattleInstance 会自己管理时钟推进
            bool shouldAdvanceClock = _state switch
            {
                DungeonState.Fighting => false,  // 战斗时由 MultiBattleInstance 管理时钟
                DungeonState.NotStarted => false,
                DungeonState.Completed => false,
                DungeonState.Failed => false,
                DungeonState.Stopped => false,
                _ => true  // 其他状态需要手动推进
            };

            if (shouldAdvanceClock)
            {
                _clock.AdvanceBy(tickMs);
            }

            int now = _clock.NowMs;

            switch (_state)
            {
                case DungeonState.WaveStartDelay:
                    if (now >= _nextActionAtMs)
                    {
                        StartCurrentWave();
                    }
                    break;

                case DungeonState.Fighting:
                    if (_currentBattle != null)
                    {
                        // 战斗实例会自己推进时钟
                        _currentBattle.AdvanceTick(tickMs);
                        CheckWaveCompletion();
                    }
                    break;

                case DungeonState.WaveEndDelay:
                    if (now >= _nextActionAtMs)
                    {
                        PrepareNextWave();
                    }
                    break;

                case DungeonState.CompletionDelay:
                    if (now >= _nextActionAtMs)
                    {
                        if (_autoRepeatEnabled)
                        {
                            RestartDungeon();
                        }
                        else
                        {
                            _state = DungeonState.Completed;
                            FireProgressEvent();
                        }
                    }
                    break;

                case DungeonState.Preparing:
                    // 准备状态也需要推进时钟
                    break;
            }
        }

        /// <summary>
        /// 准备下一波
        /// Prepare next wave
        /// </summary>
        private void PrepareNextWave()
        {
            _currentWaveIndex++;

            if (_currentWaveIndex >= _dungeonDef.Waves.Count)
            {
                // 副本完成
                CompleteDungeon();
                return;
            }

            _currentWave = _dungeonDef.Waves[_currentWaveIndex];
            _state = DungeonState.WaveStartDelay;
            _nextActionAtMs = _clock.NowMs + (int)(_currentWave.WaveStartDelaySec * 1000);

            // 触发波次变更事件
            FireWaveChangedEvent(WaveChangeType.Preparing);
            FireProgressEvent();
        }

        /// <summary>
        /// 开始当前波次
        /// Start current wave
        /// </summary>
        private void StartCurrentWave()
        {
            if (_currentWave == null) return;

            _state = DungeonState.Fighting;

            // 创建并保存敌人队伍引用
            _currentEnemyTeam = CreateEnemyTeam(_currentWave);

            // 创建战斗配置 - 优先使用 battleConfigId，其次使用嵌入的 battleConfig，最后使用默认值
            // Create battle config - prioritize battleConfigId, then embedded battleConfig, finally defaults
            MultiBattleConfig battleConfig;
            
            if (!string.IsNullOrEmpty(_dungeonDef.BattleConfigId))
            {
                // 从配置服务获取战斗配置
                var configDef = _gameConfig.GetBattleConfig(_dungeonDef.BattleConfigId);
                battleConfig = configDef?.ToMultiBattleConfig() ?? new MultiBattleConfig
                {
                    PlayerTargetStrategy = TargetStrategy.LowestHp,
                    EnemyTargetStrategy = TargetStrategy.Random,
                    SpecialIsAoe = true,
                    AoeDamageMultiplier = 0.75,
                    AllowPlayerRevive = _dungeonDef.AllowRevive,
                    AllowEnemyRespawn = false,
                    PlayerReviveCooldownMs = 5000
                };
            }
            else if (_dungeonDef.BattleConfig != null)
            {
                // 使用嵌入的配置（向后兼容）
                battleConfig = _dungeonDef.BattleConfig;
            }
            else
            {
                // 使用默认配置
                battleConfig = new MultiBattleConfig
                {
                    PlayerTargetStrategy = TargetStrategy.LowestHp,
                    EnemyTargetStrategy = TargetStrategy.Random,
                    SpecialIsAoe = true,
                    AoeDamageMultiplier = 0.75,
                    AllowPlayerRevive = _dungeonDef.AllowRevive,
                    AllowEnemyRespawn = false,
                    PlayerReviveCooldownMs = 5000
                };
            }

            // 副本特定的覆盖设置
            // Dungeon-specific overrides
            battleConfig.AllowPlayerRevive = _dungeonDef.AllowRevive;
            battleConfig.AllowEnemyRespawn = false; // 副本中敌人总是不复活 / Enemies never respawn in dungeons

            // 清理旧战斗
            if (_currentBattle != null)
            {
                UnsubscribeBattleEvents();
            }

            // 创建新战斗
            _currentBattle = new MultiBattleInstance(_clock, _rng, _playerTeam, _currentEnemyTeam, battleConfig);
            SubscribeBattleEvents();
            // 不重置玩家队伍状态，保持波次之间的血量
            // Don't reset player team state, preserve HP between waves
            _currentBattle.Start(resetPlayerTeam: false);

            // 触发事件
            FireWaveChangedEvent(WaveChangeType.Started);
            FireProgressEvent();
        }

        /// <summary>
        /// 创建敌人队伍
        /// Create enemy team
        /// </summary>
        private BattleTeam<Enemy> CreateEnemyTeam(DungeonWave wave)
        {
            var teamId = $"wave_{wave.WaveNumber}";
            var teamName = wave.Name ?? $"第 {wave.WaveNumber} 波";
            var team = new BattleTeam<Enemy>(teamId, teamName, TeamType.Enemy);

            int enemyCounter = 0;
            foreach (var monsterGroup in wave.MonsterGroups)
            {
                // 获取怪物定义
                var monsterDef = _gameConfig.GetMonster(monsterGroup.MonsterId);
                if (monsterDef == null)
                {
                    continue;
                }

                for (int i = 0; i < monsterGroup.Count; i++)
                {
                    enemyCounter++;

                    // 创建敌人实例
                    var enemy = new Enemy
                    {
                        MaxHp = (int)(monsterDef.MaxHp * monsterGroup.HpMultiplier),
                        Hp = (int)(monsterDef.MaxHp * monsterGroup.HpMultiplier),
                        AttackIntervalSec = monsterDef.AttackIntervalSec / monsterGroup.AttackSpeedMultiplier,
                        DamagePerHit = (int)(monsterDef.DamagePerHit * monsterGroup.DamageMultiplier),
                        VariancePct = monsterDef.VariancePct,
                        RespawnMs = 0, // 副本中不复活
                        LootDrops = monsterGroup.SpecialDrops ?? monsterDef.LootDrops
                    };

                    // 生成敌人ID - 使用更有意义的名称
                    string enemyId;
                    string displayName = monsterDef.Name ?? monsterGroup.MonsterId;

                    if (monsterGroup.IsBoss)
                    {
                        enemyId = $"Enemy_boss_{displayName}";
                    }
                    else if (monsterGroup.IsElite)
                    {
                        enemyId = $"Enemy_elite_{displayName}_{i + 1}";
                    }
                    else
                    {
                        enemyId = $"Enemy_{displayName}_{i + 1}";
                    }

                    team.AddMember(enemyId, enemy, enemy.MaxHp);
                }
            }

            return team;
        }

        /// <summary>
        /// 检查波次是否完成
        /// Check if wave is completed
        /// </summary>
        private void CheckWaveCompletion()
        {
            if (_currentBattle == null || _currentWave == null) return;

            var snapshot = _currentBattle.GetSnapshot();

            // 检查失败条件
            if (snapshot.State == MultiBattleState.Defeat ||
                (snapshot.PlayerTeamAlive == 0 && !_dungeonDef.AllowRevive))
            {
                _state = DungeonState.Failed;
                _currentBattle.Stop();
                FireProgressEvent();
                FireDungeonCompleteEvent(false);

                if (_autoRepeatEnabled)
                {
                    _state = DungeonState.CompletionDelay;
                    _nextActionAtMs = _clock.NowMs + _autoRepeatDelayMs;
                }
                return;
            }

            // 检查胜利条件
            bool waveCompleted = false;
            switch (_currentWave.VictoryCondition)
            {
                case VictoryCondition.EliminateAll:
                    waveCompleted = snapshot.EnemyTeamAlive == 0;
                    break;

                case VictoryCondition.Survive:
                    // TODO: 实现生存时间检查
                    if (_currentWave.TimeLimitSec > 0)
                    {
                        var elapsedSec = snapshot.ElapsedMs / 1000.0;
                        waveCompleted = elapsedSec >= _currentWave.TimeLimitSec;
                    }
                    break;

                default:
                    waveCompleted = snapshot.EnemyTeamAlive == 0;
                    break;
            }

            if (waveCompleted)
            {
                _currentBattle.Stop();

                // 发放波次奖励
                if (_currentWave.WaveRewards != null)
                {
                    ProcessWaveRewards(_currentWave.WaveRewards);
                }

                // 进入波次结束延迟
                _state = DungeonState.WaveEndDelay;
                _nextActionAtMs = _clock.NowMs + (int)(_currentWave.WaveEndDelaySec * 1000);

                FireWaveChangedEvent(WaveChangeType.Completed);
                FireProgressEvent();
            }
        }

        /// <summary>
        /// 完成副本
        /// Complete dungeon
        /// </summary>
        private void CompleteDungeon()
        {
            _completionCount++;

            // 发放完成奖励
            ProcessCompletionRewards();

            // 触发完成事件
            FireDungeonCompleteEvent(true);

            if (_autoRepeatEnabled)
            {
                _state = DungeonState.CompletionDelay;
                _nextActionAtMs = _clock.NowMs + _autoRepeatDelayMs;
            }
            else
            {
                _state = DungeonState.Completed;
            }

            FireProgressEvent();
        }

        /// <summary>
        /// 重新开始副本
        /// Restart dungeon
        /// </summary>
        private void RestartDungeon()
        {
            _currentWaveIndex = -1;
            _state = DungeonState.Preparing;

            // 不重置时钟，记录新一轮的开始时间
            _dungeonStartTimeMs = _clock.NowMs;

            // 恢复玩家队伍
            _playerTeam.ReviveAll(true);

            FireProgressEvent();
            PrepareNextWave();
        }

        /// <summary>
        /// 处理波次奖励
        /// Process wave rewards
        /// </summary>
        private void ProcessWaveRewards(List<LootDrop> rewards)
        {
            foreach (var reward in rewards)
            {
                if (_rng.NextDouble() <= reward.DropChance)
                {
                    int quantity = reward.MinQuantity;
                    if (reward.MaxQuantity > reward.MinQuantity)
                    {
                        quantity = _rng.NextRange(reward.MinQuantity, reward.MaxQuantity);
                    }

                    if (quantity > 0)
                    {
                        RecordLoot(reward.ItemId, quantity);

                        var lootEvent = new LootDropEvent
                        {
                            TimeMs = _clock.NowMs,
                            ItemId = reward.ItemId,
                            Quantity = quantity,
                            MonsterId = $"wave_{_currentWaveIndex + 1}_reward"
                        };
                        LootDropped?.Invoke(lootEvent);
                    }
                }
            }
        }

        /// <summary>
        /// 处理完成奖励
        /// Process completion rewards
        /// </summary>
        private void ProcessCompletionRewards()
        {
            var rewards = _completionCount == 1 && _dungeonDef.FirstClearRewards != null
                ? _dungeonDef.FirstClearRewards
                : _dungeonDef.CompletionRewards;

            foreach (var reward in rewards)
            {
                if (_rng.NextDouble() <= reward.DropChance)
                {
                    int quantity = reward.MinQuantity;
                    if (reward.MaxQuantity > reward.MinQuantity)
                    {
                        quantity = _rng.NextRange(reward.MinQuantity, reward.MaxQuantity);
                    }

                    if (quantity > 0)
                    {
                        RecordLoot(reward.ItemId, quantity);

                        var lootEvent = new LootDropEvent
                        {
                            TimeMs = _clock.NowMs,
                            ItemId = reward.ItemId,
                            Quantity = quantity,
                            MonsterId = "dungeon_completion_reward"
                        };
                        LootDropped?.Invoke(lootEvent);
                    }
                }
            }
        }

        /// <summary>
        /// 记录掉落物
        /// Record loot
        /// </summary>
        private void RecordLoot(string itemId, int quantity)
        {
            if (_totalLoot.ContainsKey(itemId))
            {
                _totalLoot[itemId] += quantity;
            }
            else
            {
                _totalLoot[itemId] = quantity;
            }
        }

        /// <summary>
        /// 订阅战斗事件
        /// Subscribe to battle events
        /// </summary>
        private void SubscribeBattleEvents()
        {
            if (_currentBattle == null) return;

            _currentBattle.CombatEventFired += OnCombatEvent;
            _currentBattle.LootDropped += OnLootDropped;
            _currentBattle.ExperienceGained += OnExperienceGained;
            _currentBattle.TeamStatusChanged += OnTeamStatusChanged;
        }

        /// <summary>
        /// 取消订阅战斗事件
        /// Unsubscribe from battle events
        /// </summary>
        private void UnsubscribeBattleEvents()
        {
            if (_currentBattle == null) return;

            _currentBattle.CombatEventFired -= OnCombatEvent;
            _currentBattle.LootDropped -= OnLootDropped;
            _currentBattle.ExperienceGained -= OnExperienceGained;
            _currentBattle.TeamStatusChanged -= OnTeamStatusChanged;
        }

        /// <summary>
        /// 处理战斗事件
        /// Handle combat event
        /// </summary>
        private void OnCombatEvent(MultiCombatEvent ev)
        {
            // 转发事件
            CombatEventFired?.Invoke(ev);

            // 更新统计
            if (ev.IsKill)
            {
                if (ev.Attacker == ActorType.Player)
                {
                    _totalKills++;
                }
                else
                {
                    _totalDeaths++;
                }
                FireStatsEvent();
            }
        }

        /// <summary>
        /// 处理掉落事件
        /// Handle loot drop event
        /// </summary>
        private void OnLootDropped(LootDropEvent ev)
        {
            RecordLoot(ev.ItemId, ev.Quantity);
            LootDropped?.Invoke(ev);
            FireStatsEvent();
        }

        /// <summary>
        /// 处理经验获得事件
        /// Handle experience gain event
        /// </summary>
        private void OnExperienceGained(ExperienceGainEvent ev)
        {
            ExperienceGained?.Invoke(ev);
        }

        /// <summary>
        /// 处理队伍状态变更
        /// Handle team status change
        /// </summary>
        private void OnTeamStatusChanged(TeamStatusEvent ev)
        {
            // 可以在这里处理特殊逻辑
        }

        /// <summary>
        /// 触发进度事件
        /// Fire progress event
        /// </summary>
        private void FireProgressEvent()
        {
            var progressEvent = new DungeonProgressEvent
            {
                DungeonId = _dungeonDef.Id,
                DungeonName = _dungeonDef.Name,
                CurrentWave = _currentWaveIndex + 1,
                TotalWaves = _dungeonDef.Waves.Count,
                State = _state,
                ElapsedMs = _clock.NowMs - _dungeonStartTimeMs,
                CompletionCount = _completionCount,
                AutoRepeatEnabled = _autoRepeatEnabled
            };
            ProgressChanged?.Invoke(progressEvent);
        }

        /// <summary>
        /// 触发波次变更事件
        /// Fire wave changed event
        /// </summary>
        private void FireWaveChangedEvent(WaveChangeType changeType)
        {
            if (_currentWave == null) return;

            var waveEvent = new DungeonWaveEvent
            {
                WaveNumber = _currentWave.WaveNumber,
                WaveName = _currentWave.Name ?? $"第 {_currentWave.WaveNumber} 波",
                WaveType = _currentWave.WaveType,
                ChangeType = changeType,
                TimeMs = _clock.NowMs
            };
            WaveChanged?.Invoke(waveEvent);
        }

        /// <summary>
        /// 触发副本完成事件
        /// Fire dungeon complete event
        /// </summary>
        private void FireDungeonCompleteEvent(bool success)
        {
            var completeEvent = new DungeonCompleteEvent
            {
                DungeonId = _dungeonDef.Id,
                DungeonName = _dungeonDef.Name,
                Success = success,
                CompletionTimeMs = _clock.NowMs - _dungeonStartTimeMs,
                CompletionCount = _completionCount,
                TotalKills = _totalKills,
                TotalDeaths = _totalDeaths,
                TotalLoot = new Dictionary<string, int>(_totalLoot)
            };
            DungeonCompleted?.Invoke(completeEvent);
        }

        /// <summary>
        /// 触发统计更新事件
        /// Fire stats update event
        /// </summary>
        private void FireStatsEvent()
        {
            var statsEvent = new DungeonStatsEvent
            {
                TotalKills = _totalKills,
                TotalDeaths = _totalDeaths,
                TotalLoot = new Dictionary<string, int>(_totalLoot),
                CompletionCount = _completionCount,
                ElapsedMs = _clock.NowMs - _dungeonStartTimeMs
            };
            StatsUpdated?.Invoke(statsEvent);
        }

        /// <summary>
        /// 获取副本快照
        /// Get dungeon snapshot
        /// </summary>
        public DungeonSnapshot GetSnapshot()
        {
            var battleSnapshot = _currentBattle?.GetSnapshot();
            int displayWaveIndex = Math.Min(_currentWaveIndex, _dungeonDef.Waves.Count - 1);

            return new DungeonSnapshot
            {
                DungeonId = _dungeonDef.Id,
                DungeonName = _dungeonDef.Name,
                State = _state,
                CurrentWaveIndex = displayWaveIndex,
                TotalWaves = _dungeonDef.Waves.Count,
                ElapsedMs = _clock.NowMs - _dungeonStartTimeMs,  // 计算相对于本轮开始的时间
                CompletionCount = _completionCount,
                TotalKills = _totalKills,
                TotalDeaths = _totalDeaths,
                TotalLoot = new Dictionary<string, int>(_totalLoot),
                AutoRepeatEnabled = _autoRepeatEnabled,
                BattleSnapshot = battleSnapshot,
                CurrentEnemyTeam = _currentEnemyTeam
            };
        }
    }
}