using System;
using System.Collections.Generic;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Tracks;

namespace BlazorIdle.Game.Battle.State
{
    /// <summary>
    /// 战斗状态管理器 - 从 MultiBattleInstance 提取的状态管理逻辑
    /// Battle state manager - State management logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 检查战斗结束条件 (CheckBattleState)
    /// - 处理冷却结束 (HandleCooldownEnd)
    /// - 触发队伍状态事件 (FireTeamStatusEvent)
    /// - 管理 MultiBattleState 状态转换
    /// </summary>
    public class BattleStateManager
    {
        private readonly IGameClock _clock;
        private readonly BattleTeam<Character> _playerTeam;
        private readonly BattleTeam<Enemy> _enemyTeam;
        private readonly MultiBattleConfig _config;
        private readonly Dictionary<string, CharacterBuffOwner> _playerBuffOwners;
        private readonly Dictionary<string, EnemyBuffOwner> _enemyBuffOwners;
        private readonly Dictionary<string, ResourceBucketCollection> _playerResources;
        private readonly Dictionary<string, Shared.Models.ProfessionResourceConfig>? _professionResourceConfigs;
        private readonly Dictionary<string, CharacterTracks> _characterTracks;
        private readonly Dictionary<string, EnemyTrack> _enemyTracks;

        private MultiBattleState _state = MultiBattleState.NotStarted;
        private int _resumeAtMs = 0;

        /// <summary>
        /// 队伍状态变化事件
        /// Team status changed event
        /// </summary>
        public event Action<TeamStatusEvent>? OnTeamStatusChanged;

        /// <summary>
        /// 战斗停止请求事件
        /// Battle stop request event
        /// </summary>
        public event Action? OnStopRequested;

        /// <summary>
        /// 当前战斗状态
        /// Current battle state
        /// </summary>
        public MultiBattleState State => _state;

        /// <summary>
        /// 恢复时间（毫秒）
        /// Resume time (milliseconds)
        /// </summary>
        public int ResumeAtMs => _resumeAtMs;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public BattleStateManager(
            IGameClock clock,
            BattleTeam<Character> playerTeam,
            BattleTeam<Enemy> enemyTeam,
            MultiBattleConfig config,
            Dictionary<string, CharacterBuffOwner> playerBuffOwners,
            Dictionary<string, EnemyBuffOwner> enemyBuffOwners,
            Dictionary<string, ResourceBucketCollection> playerResources,
            Dictionary<string, Shared.Models.ProfessionResourceConfig>? professionResourceConfigs,
            Dictionary<string, CharacterTracks> characterTracks,
            Dictionary<string, EnemyTrack> enemyTracks)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _playerTeam = playerTeam ?? throw new ArgumentNullException(nameof(playerTeam));
            _enemyTeam = enemyTeam ?? throw new ArgumentNullException(nameof(enemyTeam));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _playerBuffOwners = playerBuffOwners ?? throw new ArgumentNullException(nameof(playerBuffOwners));
            _enemyBuffOwners = enemyBuffOwners ?? throw new ArgumentNullException(nameof(enemyBuffOwners));
            _playerResources = playerResources ?? throw new ArgumentNullException(nameof(playerResources));
            _professionResourceConfigs = professionResourceConfigs;
            _characterTracks = characterTracks ?? throw new ArgumentNullException(nameof(characterTracks));
            _enemyTracks = enemyTracks ?? throw new ArgumentNullException(nameof(enemyTracks));
        }

        /// <summary>
        /// 设置战斗状态
        /// Set battle state
        /// </summary>
        public void SetState(MultiBattleState state)
        {
            _state = state;
        }

        /// <summary>
        /// 设置恢复时间
        /// Set resume time
        /// </summary>
        public void SetResumeAtMs(int resumeAtMs)
        {
            _resumeAtMs = resumeAtMs;
        }

        /// <summary>
        /// 检查战斗状态
        /// Check battle state
        /// </summary>
        public void CheckBattleState(int now)
        {
            if (_playerTeam.IsAllDead)
            {
                if (_config.AllowPlayerRevive)
                {
                    _state = MultiBattleState.PlayerTeamDeadCooldown;
                    _resumeAtMs = now + _config.PlayerReviveCooldownMs;
                }
                else
                {
                    _state = MultiBattleState.Defeat;
                    OnStopRequested?.Invoke();
                }
                FireTeamStatusEvent();
            }
            else if (_enemyTeam.IsAllDead)
            {
                if (_config.AllowEnemyRespawn)
                {
                    _state = MultiBattleState.EnemyTeamDeadCooldown;
                    _resumeAtMs = now + _config.EnemyRespawnCooldownMs;
                }
                else
                {
                    _state = MultiBattleState.Victory;
                    OnStopRequested?.Invoke();
                }
                FireTeamStatusEvent();
            }
        }

        /// <summary>
        /// 处理冷却结束
        /// Handle cooldown end
        /// </summary>
        public void HandleCooldownEnd(int now)
        {
            if (_state == MultiBattleState.PlayerTeamDeadCooldown)
            {
                // 复活玩家队伍
                _playerTeam.ReviveAll(_config.ReviveWithFullHp);

                // 清除玩家所有 buff 和重置资源
                foreach (var kvp in _playerBuffOwners)
                {
                    var charId = kvp.Key;
                    var buffOwner = kvp.Value;

                    // 清除所有 buff
                    buffOwner.ClearAllBuffs();

                    // 重置资源
                    if (_playerResources.TryGetValue(charId, out var resources))
                    {
                        // 获取资源配置以重置到初始值
                        if (_professionResourceConfigs != null &&
                            buffOwner.Character.ActiveCombatProfessionId != null &&
                            _professionResourceConfigs.TryGetValue(buffOwner.Character.ActiveCombatProfessionId, out var profConfig))
                        {
                            // 重置到职业初始资源值
                            var bucket = resources.GetBucket(profConfig.Id);
                            bucket.Reset(profConfig.Initial);
                        }
                        else
                        {
                            // 后备：重置所有资源到 0
                            foreach (var bucket in resources.GetAll().Values)
                            {
                                bucket.Reset(0);
                            }
                        }
                    }
                }

                // 对称重置：玩家与敌人轨道全部重置到 now，避免冷却期间积压触发
                foreach (var tracks in _characterTracks.Values)
                {
                    tracks.Reset(now);
                }
                foreach (var track in _enemyTracks.Values)
                {
                    track.Reset(now);
                }

                _state = MultiBattleState.Fighting;
            }
            else if (_state == MultiBattleState.EnemyTeamDeadCooldown)
            {
                // 刷新敌人队伍
                _enemyTeam.ReviveAll(true);

                // 清除敌人所有 buff
                foreach (var buffOwner in _enemyBuffOwners.Values)
                {
                    buffOwner.ClearAllBuffs();
                }

                // 对称重置：敌人与玩家轨道全部重置到 now，避免冷却期间积压触发
                foreach (var track in _enemyTracks.Values)
                {
                    track.Reset(now);
                }
                foreach (var tracks in _characterTracks.Values)
                {
                    tracks.Reset(now);
                }

                _state = MultiBattleState.Fighting;
            }

            FireTeamStatusEvent();
        }

        /// <summary>
        /// 触发队伍状态事件
        /// Fire team status event
        /// </summary>
        public void FireTeamStatusEvent()
        {
            var playerStatus = new TeamStatus
            {
                TeamId = _playerTeam.TeamId,
                TeamName = _playerTeam.TeamName,
                AliveCount = _playerTeam.AliveCount,
                TotalCount = _playerTeam.TotalCount
            };

            foreach (var member in _playerTeam.Members)
            {
                playerStatus.MemberHp[member.Id] = member.CurrentHp;
                playerStatus.MemberMaxHp[member.Id] = member.MaxHp;
            }

            var enemyStatus = new TeamStatus
            {
                TeamId = _enemyTeam.TeamId,
                TeamName = _enemyTeam.TeamName,
                AliveCount = _enemyTeam.AliveCount,
                TotalCount = _enemyTeam.TotalCount
            };

            foreach (var member in _enemyTeam.Members)
            {
                enemyStatus.MemberHp[member.Id] = member.CurrentHp;
                enemyStatus.MemberMaxHp[member.Id] = member.MaxHp;
            }

            var statusEvent = new TeamStatusEvent
            {
                TimeMs = _clock.NowMs,
                PlayerTeamStatus = playerStatus,
                EnemyTeamStatus = enemyStatus,
                BattleState = _state
            };

            OnTeamStatusChanged?.Invoke(statusEvent);
        }

        /// <summary>
        /// 检查是否处于冷却状态且冷却已结束
        /// Check if in cooldown state and cooldown has ended
        /// </summary>
        public bool IsCooldownEnded(int now)
        {
            return (_state == MultiBattleState.PlayerTeamDeadCooldown || 
                    _state == MultiBattleState.EnemyTeamDeadCooldown) && 
                   now >= _resumeAtMs;
        }

        /// <summary>
        /// 检查战斗是否正在进行
        /// Check if battle is in progress
        /// </summary>
        public bool IsFighting => _state == MultiBattleState.Fighting;

        /// <summary>
        /// 检查战斗是否已结束
        /// Check if battle has ended
        /// </summary>
        public bool IsEnded => _state == MultiBattleState.Victory || _state == MultiBattleState.Defeat;

        /// <summary>
        /// 检查是否处于冷却状态
        /// Check if in cooldown state
        /// </summary>
        public bool IsInCooldown => _state == MultiBattleState.PlayerTeamDeadCooldown || 
                                    _state == MultiBattleState.EnemyTeamDeadCooldown;
    }
}
