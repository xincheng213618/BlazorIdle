using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Game
{
    /// <summary>
    /// 多单位战斗实例 - 支持多角色对多怪物的战斗
    /// Multi-unit battle instance - supports multiple characters vs multiple monsters
    /// </summary>
    public sealed class MultiBattleInstance
    {
        private readonly IGameClock _clock;
        private readonly RngContext _rng;
        private readonly BattleTeam<Character> _playerTeam;
        private readonly BattleTeam<Enemy> _enemyTeam;

        // 每个角色的战斗轨道
        private readonly Dictionary<string, CharacterTracks> _characterTracks = new();
        // 每个怪物的攻击轨道
        private readonly Dictionary<string, EnemyTrack> _enemyTracks = new();
        /// <summary>
        /// 获取玩家队伍引用（只读）
        /// Get player team reference (read-only)
        /// </summary>
        public BattleTeam<Character> PlayerTeam => _playerTeam;

        /// <summary>
        /// 获取敌人队伍引用（只读）
        /// Get enemy team reference (read-only)
        /// </summary>
        public BattleTeam<Enemy> EnemyTeam => _enemyTeam;

        // 战斗配置
        private readonly MultiBattleConfig _config;

        // 战斗状态
        private bool _running;
        private MultiBattleState _state = MultiBattleState.NotStarted;
        private int _resumeAtMs = 0;

        // 战斗统计
        private readonly Dictionary<string, int> _damageDealtByCharacter = new();
        private readonly Dictionary<string, int> _damageTakenByCharacter = new();
        private readonly Dictionary<string, int> _damageDealtByEnemy = new();
        private readonly Dictionary<string, int> _healingDone = new();
        private int _totalPlayerDamage = 0;
        private int _totalEnemyDamage = 0;

        // 战斗段落聚合器
        private readonly List<CombatSegment> _segments = new();
        private SegmentAggregator _aggregator;

        // RNG追踪
        private readonly int _rngSeed;
        private int _rngIndexStart;
        private int _rngIndexEnd;

        // 事件
        public event Action<MultiCombatEvent>? CombatEventFired;
        public event Action<LootDropEvent>? LootDropped;
        public event Action<ExperienceGainEvent>? ExperienceGained;
        public event Action<TeamStatusEvent>? TeamStatusChanged;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public MultiBattleInstance(
            IGameClock clock,
            RngContext rng,
            BattleTeam<Character> playerTeam,
            BattleTeam<Enemy> enemyTeam,
            MultiBattleConfig? config = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _playerTeam = playerTeam ?? throw new ArgumentNullException(nameof(playerTeam));
            _enemyTeam = enemyTeam ?? throw new ArgumentNullException(nameof(enemyTeam));
            _config = config ?? new MultiBattleConfig();

            _rngSeed = rng.Seed;
            _aggregator = new SegmentAggregator(new SegmentAggregatorOptions
            {
                MaxEvents = 20,
                MaxDurationMs = 3000
            });

            InitializeTracks();
        }

        /// <summary>
        /// 初始化所有战斗轨道
        /// Initialize all battle tracks
        /// </summary>
        private void InitializeTracks()
        {
            // 为每个角色初始化战斗轨道
            foreach (var member in _playerTeam.Members)
            {
                var character = member.Entity;
                var tracks = new CharacterTracks(member.Id, character);
                _characterTracks[member.Id] = tracks;

                // 初始化统计
                _damageDealtByCharacter[member.Id] = 0;
                _damageTakenByCharacter[member.Id] = 0;
            }

            // 为每个怪物初始化攻击轨道
            foreach (var member in _enemyTeam.Members)
            {
                var enemy = member.Entity;
                var track = new EnemyTrack(member.Id, enemy);
                _enemyTracks[member.Id] = track;

                // 初始化统计
                _damageDealtByEnemy[member.Id] = 0;
            }
        }

        /// <summary>
        /// 开始战斗
        /// Start battle
        /// </summary>
        /// <param name="resetPlayerTeam">是否重置玩家队伍状态（默认true）/ Whether to reset player team state (default true)</param>
        public void Start(bool resetPlayerTeam = true)
        {
            if (_running) return;

            _running = true;
            _clock.Reset();
            _state = MultiBattleState.Fighting;
            _resumeAtMs = 0;

            // 重置队伍状态
            if (resetPlayerTeam)
            {
                _playerTeam.Reset();
            }
            _enemyTeam.Reset();

            // 重置所有轨道
            var now = _clock.NowMs;
            foreach (var tracks in _characterTracks.Values)
            {
                tracks.Reset(now);
            }
            foreach (var track in _enemyTracks.Values)
            {
                track.Reset(now);
            }

            // 重置统计
            foreach (var key in _damageDealtByCharacter.Keys.ToList())
            {
                _damageDealtByCharacter[key] = 0;
            }
            foreach (var key in _damageTakenByCharacter.Keys.ToList())
            {
                _damageTakenByCharacter[key] = 0;
            }
            foreach (var key in _damageDealtByEnemy.Keys.ToList())
            {
                _damageDealtByEnemy[key] = 0;
            }
            _healingDone.Clear();
            _totalPlayerDamage = 0;
            _totalEnemyDamage = 0;

            // 重置段落聚合器
            _segments.Clear();
            _aggregator.Reset();
            _rngIndexStart = _rng.Index;

            // 触发初始状态事件
            FireTeamStatusEvent();
        }

        /// <summary>
        /// 停止战斗
        /// Stop battle
        /// </summary>
        public void Stop()
        {
            if (!_running) return;

            _running = false;
            var seg = _aggregator.ForceFlush(_clock.NowMs, _rng.Index);
            if (seg != null) _segments.Add(seg);
            _rngIndexEnd = _rng.Index;
        }

        /// <summary>
        /// 战斗是否正在运行
        /// Whether battle is running
        /// </summary>
        public bool IsRunning => _running;

        /// <summary>
        /// 推进战斗时钟
        /// Advance battle tick
        /// </summary>
        public void AdvanceTick(int tickMs)
        {
            if (!_running) return;

            _clock.AdvanceBy(tickMs);
            int now = _clock.NowMs;

            // 处理冷却状态
            if (_state == MultiBattleState.PlayerTeamDeadCooldown ||
                _state == MultiBattleState.EnemyTeamDeadCooldown)
            {
                if (now >= _resumeAtMs)
                {
                    HandleCooldownEnd(now);
                }

                var segByTime = _aggregator.Tick(now, _rng.Index);
                if (segByTime != null) _segments.Add(segByTime);
                return;
            }

            // 检查战斗结束条件
            if (_state == MultiBattleState.Fighting)
            {
                // 处理角色行动
                ProcessCharacterActions(now);

                // 处理怪物行动
                ProcessEnemyActions(now);

                // 检查战斗状态
                CheckBattleState(now);
            }

            // 处理段落聚合
            var seg = _aggregator.Tick(now, _rng.Index);
            if (seg != null) _segments.Add(seg);
        }

        /// <summary>
        /// 处理角色行动
        /// Process character actions
        /// </summary>
        private void ProcessCharacterActions(int now)
        {
            var aliveCharIds = _playerTeam.GetAliveMemberIds();

            foreach (var charId in aliveCharIds)
            {
                if (!_characterTracks.TryGetValue(charId, out var tracks))
                    continue;

                if (!tracks.IsEnabled)
                    continue;

                var character = tracks.Character;

                // 处理普通攻击
                var atkCount = tracks.AttackTrack.CollectTriggers(now);
                for (int i = 0; i < atkCount; i++)
                {
                    ProcessCharacterAttack(charId, character);
                }

                // 处理特殊技能
                var spCount = tracks.SpecialTrack.CollectTriggers(now);
                for (int i = 0; i < spCount; i++)
                {
                    ProcessCharacterSpecial(charId, character);
                }
            }
        }

        /// <summary>
        /// 处理角色普通攻击
        /// Process character normal attack
        /// </summary>
        private void ProcessCharacterAttack(string charId, Character character)
        {
            var targetId = SelectEnemyTarget(_config.PlayerTargetStrategy);
            if (targetId == null) return;

            var member = _playerTeam.GetMember(charId);
            var target = _enemyTeam.GetMember(targetId);
            if (member == null || target == null) return;

            // 计算伤害
            int damage = PlayerRollDamage(character.DamagePerAttack, character, true);

            // 应用伤害
            ApplyDamageToEnemy(charId, member, targetId, target, damage, EventSource.Attack);
        }

        /// <summary>
        /// 处理角色特殊技能
        /// Process character special skill
        /// </summary>
        private void ProcessCharacterSpecial(string charId, Character character)
        {
            var member = _playerTeam.GetMember(charId);
            if (member == null) return;

            if (_config.SpecialIsAoe)
            {
                // AOE技能 - 打击所有存活敌人
                var aliveEnemies = _enemyTeam.GetAliveMemberIds();
                int baseDamage = character.SpecialDamage;

                foreach (var enemyId in aliveEnemies)
                {
                    var target = _enemyTeam.GetMember(enemyId);
                    if (target == null) continue;

                    // AOE伤害可以有衰减
                    int damage = PlayerRollDamage(
                        (int)(baseDamage * _config.AoeDamageMultiplier),
                        character,
                        true
                    );

                    ApplyDamageToEnemy(charId, member, enemyId, target, damage, EventSource.Special, true);
                }
            }
            else
            {
                // 单体技能
                var targetId = SelectEnemyTarget(_config.PlayerTargetStrategy);
                if (targetId == null) return;

                var target = _enemyTeam.GetMember(targetId);
                if (target == null) return;

                int damage = PlayerRollDamage(character.SpecialDamage, character, true);
                ApplyDamageToEnemy(charId, member, targetId, target, damage, EventSource.Special);
            }
        }

        /// <summary>
        /// 处理怪物行动
        /// Process enemy actions
        /// </summary>
        private void ProcessEnemyActions(int now)
        {
            var aliveEnemyIds = _enemyTeam.GetAliveMemberIds();

            foreach (var enemyId in aliveEnemyIds)
            {
                if (!_enemyTracks.TryGetValue(enemyId, out var track))
                    continue;

                if (!track.IsEnabled)
                    continue;

                var enemy = track.Enemy;

                var count = track.AttackTrack.CollectTriggers(now);
                for (int i = 0; i < count; i++)
                {
                    ProcessEnemyAttack(enemyId, enemy);
                }
            }
        }

        /// <summary>
        /// 处理怪物攻击
        /// Process enemy attack
        /// </summary>
        private void ProcessEnemyAttack(string enemyId, Enemy enemy)
        {
            var targetId = SelectPlayerTarget(_config.EnemyTargetStrategy);
            if (targetId == null) return;

            var member = _enemyTeam.GetMember(enemyId);
            var target = _playerTeam.GetMember(targetId);
            if (member == null || target == null) return;

            int damage = EnemyRollDamage(enemy.DamagePerHit, enemy);
            ApplyDamageToPlayer(enemyId, member, targetId, target, damage);
        }

        /// <summary>
        /// 应用伤害到敌人
        /// Apply damage to enemy
        /// </summary>
        private void ApplyDamageToEnemy(
            string attackerId,
            BattleMember<Character> attacker,
            string defenderId,
            BattleMember<Enemy> defender,
            int damage,
            EventSource source,
            bool isAoe = false)
        {
            // 应用伤害
            int actualDamage = defender.TakeDamage(damage);
            bool isKill = defender.IsDead;

            // 更新统计
            attacker.RecordDamageDealt(actualDamage, isKill);
            _damageDealtByCharacter[attackerId] += actualDamage;
            _totalPlayerDamage += actualDamage;

            // 创建事件
            var ev = new MultiCombatEvent
            {
                Attacker = ActorType.Player,
                Defender = ActorType.Enemy,
                AttackerId = attackerId,
                AttackerName = GetCharacterName(attackerId),
                DefenderId = defenderId,
                DefenderName = GetEnemyName(defenderId),
                Source = source,
                TimeMs = _clock.NowMs,
                Damage = actualDamage,
                Crit = false, // TODO: 实现暴击判定
                IsAoe = isAoe,
                IsKill = isKill,
                RngIndexAfter = _rng.Index,
                DefenderHpAfter = defender.CurrentHp
            };

            // 聚合事件
            var flushed = _aggregator.AddEvent(ev);
            if (flushed != null) _segments.Add(flushed);

            // 触发事件
            CombatEventFired?.Invoke(ev);

            // 如果击杀，处理掉落物和经验
            if (isKill)
            {
                ProcessLootDrops(defenderId, defender.Entity);
                ProcessExperienceGain(defenderId, defender.Entity, attackerId);
            }
        }

        /// <summary>
        /// 应用伤害到玩家
        /// Apply damage to player
        /// </summary>
        private void ApplyDamageToPlayer(
            string attackerId,
            BattleMember<Enemy> attacker,
            string defenderId,
            BattleMember<Character> defender,
            int damage)
        {
            // 应用伤害
            int actualDamage = defender.TakeDamage(damage);
            bool isKill = defender.IsDead;

            // 更新统计
            attacker.RecordDamageDealt(actualDamage, isKill);
            _damageDealtByEnemy[attackerId] += actualDamage;
            _damageTakenByCharacter[defenderId] += actualDamage;
            _totalEnemyDamage += actualDamage;

            // 创建事件
            var ev = new MultiCombatEvent
            {
                Attacker = ActorType.Enemy,
                Defender = ActorType.Player,
                AttackerId = attackerId,
                AttackerName = GetEnemyName(attackerId),
                DefenderId = defenderId,
                DefenderName = GetCharacterName(defenderId),
                Source = EventSource.EnemyAttack,
                TimeMs = _clock.NowMs,
                Damage = actualDamage,
                Crit = false,
                IsAoe = false,
                IsKill = isKill,
                RngIndexAfter = _rng.Index,
                DefenderHpAfter = defender.CurrentHp
            };

            // 聚合事件
            var flushed = _aggregator.AddEvent(ev);
            if (flushed != null) _segments.Add(flushed);

            // 触发事件
            CombatEventFired?.Invoke(ev);
        }

        /// <summary>
        /// 选择敌人目标
        /// Select enemy target
        /// </summary>
        private string? SelectEnemyTarget(TargetStrategy strategy)
        {
            switch (strategy)
            {
                case TargetStrategy.Random:
                    return _enemyTeam.GetRandomAliveMemberId(_rng);

                case TargetStrategy.LowestHp:
                    return _enemyTeam.GetLowestHpMemberId();

                case TargetStrategy.LowestHpPercent:
                    return _enemyTeam.GetLowestHpPercentMemberId();

                case TargetStrategy.HighestHp:
                    var aliveEnemies = _enemyTeam.GetAliveMembers();
                    var highest = aliveEnemies.OrderByDescending(m => m.CurrentHp).FirstOrDefault();
                    return highest?.Id;

                default:
                    return _enemyTeam.GetRandomAliveMemberId(_rng);
            }
        }

        /// <summary>
        /// 选择玩家目标
        /// Select player target
        /// </summary>
        private string? SelectPlayerTarget(TargetStrategy strategy)
        {
            switch (strategy)
            {
                case TargetStrategy.Random:
                    return _playerTeam.GetRandomAliveMemberId(_rng);

                case TargetStrategy.LowestHp:
                    return _playerTeam.GetLowestHpMemberId();

                case TargetStrategy.LowestHpPercent:
                    return _playerTeam.GetLowestHpPercentMemberId();

                case TargetStrategy.HighestHp:
                    var alivePlayers = _playerTeam.GetAliveMembers();
                    var highest = alivePlayers.OrderByDescending(m => m.CurrentHp).FirstOrDefault();
                    return highest?.Id;

                default:
                    return _playerTeam.GetRandomAliveMemberId(_rng);
            }
        }

        /// <summary>
        /// 检查战斗状态
        /// Check battle state
        /// </summary>
        private void CheckBattleState(int now)
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
                    Stop();
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
                    Stop();
                }
                FireTeamStatusEvent();
            }
        }

        /// <summary>
        /// 处理冷却结束
        /// Handle cooldown end
        /// </summary>
        private void HandleCooldownEnd(int now)
        {
            if (_state == MultiBattleState.PlayerTeamDeadCooldown)
            {
                // 复活玩家队伍
                _playerTeam.ReviveAll(_config.ReviveWithFullHp);

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
        /// 处理掉落物
        /// Process loot drops
        /// </summary>
        private void ProcessLootDrops(string enemyId, Enemy enemy)
        {
            if (enemy.LootDrops == null || enemy.LootDrops.Count == 0)
                return;

            foreach (var drop in enemy.LootDrops)
            {
                if (_rng.NextDouble() > drop.DropChance)
                    continue;

                int quantity = drop.MinQuantity;
                if (drop.MaxQuantity > drop.MinQuantity)
                {
                    quantity = _rng.NextRange(drop.MinQuantity, drop.MaxQuantity);
                }

                if (quantity > 0)
                {
                    var lootEvent = new LootDropEvent
                    {
                        TimeMs = _clock.NowMs,
                        ItemId = drop.ItemId,
                        Quantity = quantity,
                        MonsterId = enemyId
                    };
                    LootDropped?.Invoke(lootEvent);
                }
            }
        }

        /// <summary>
        /// 处理经验获得 - 怪物死亡时触发
        /// Process experience gain - triggered when monster dies
        /// </summary>
        private void ProcessExperienceGain(string enemyId, Enemy enemy, string attackerId)
        {
            if (enemy.BaseExperience <= 0) return;

            // 获取击杀者的职业ID
            var attacker = _playerTeam.Members.FirstOrDefault(m => m.Id == attackerId);
            if (attacker == null) return;

            var expEvent = new ExperienceGainEvent
            {
                TimeMs = _clock.NowMs,
                ProfessionId = attacker.Entity.ActiveCombatProfessionId,
                BaseExperience = enemy.BaseExperience,
                MonsterId = enemy.MonsterId
            };
            ExperienceGained?.Invoke(expEvent);
        }

        /// <summary>
        /// 触发队伍状态事件
        /// Fire team status event
        /// </summary>
        private void FireTeamStatusEvent()
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

            TeamStatusChanged?.Invoke(statusEvent);
        }

        /// <summary>
        /// 计算玩家伤害
        /// Roll player damage
        /// </summary>
        private int PlayerRollDamage(int baseDamage, Character character, bool allowCrit)
        {
            double dmg = Math.Floor(_rng.Jitter(baseDamage, character.VariancePct));
            if (dmg < 1) dmg = 1;

            if (allowCrit && _rng.NextDouble() < (character.CritChancePercent / 100.0))
            {
                dmg = Math.Floor(dmg * Math.Max(1.0, character.CritMultiplier));
            }

            return (int)dmg;
        }

        /// <summary>
        /// 计算敌人伤害
        /// Roll enemy damage
        /// </summary>
        private int EnemyRollDamage(int baseDamage, Enemy enemy)
        {
            double dmg = Math.Floor(_rng.Jitter(baseDamage, enemy.VariancePct));
            if (dmg < 1) dmg = 1;
            return (int)dmg;
        }

        /// <summary>
        /// 获取角色名称
        /// Get character name
        /// </summary>
        private string GetCharacterName(string characterId)
        {
            if (characterId.StartsWith("char_"))
            {
                return $"Character_{characterId.Substring(5)}";
            }
            else if (characterId.StartsWith("teammate_"))
            {
                return $"Character_teammate_{characterId.Substring(9)}";
            }
            return $"Character_{characterId}";
        }
        /// <summary>
        /// 获取敌人名称
        /// Get enemy name
        /// </summary>
        private string GetEnemyName(string enemyId)
        {
            // 直接使用传入的 enemyId，因为已经包含了有意义的名称
            return enemyId.Replace("_", " ");
        }

        /// <summary>
        /// 获取战斗快照
        /// Get battle snapshot
        /// </summary>
        public MultiBattleSnapshot GetSnapshot()
        {
            return new MultiBattleSnapshot
            {
                ElapsedMs = _clock.NowMs,
                State = _state,
                PlayerTeamAlive = _playerTeam.AliveCount,
                PlayerTeamTotal = _playerTeam.TotalCount,
                EnemyTeamAlive = _enemyTeam.AliveCount,
                EnemyTeamTotal = _enemyTeam.TotalCount,
                TotalPlayerDamage = _totalPlayerDamage,
                TotalEnemyDamage = _totalEnemyDamage,
                TimeToResumeMs = _state == MultiBattleState.PlayerTeamDeadCooldown ||
                                 _state == MultiBattleState.EnemyTeamDeadCooldown
                    ? Math.Max(0, _resumeAtMs - _clock.NowMs)
                    : 0,
                RngIndex = _rng.Index
            };
        }

        /// <summary>
        /// 构建战斗摘要
        /// Build battle digest
        /// </summary>
        public BattleDigest BuildDigest()
        {
            if (_running) Stop();

            return BattleDigest.FromSegments(
                durationMs: _clock.NowMs,
                tickCount: 0, // TODO: 实现tick计数
                rngStart: _rngIndexStart,
                rngEnd: _rngIndexEnd,
                seed: _rngSeed,
                segments: _segments
            );
        }

        /// <summary>
        /// 检查角色战斗轨道是否可用
        /// Check if character battle track is available
        /// </summary>
        /// <returns>如果角色存在且处于战斗状态返回true，否则返回false / Returns true if character exists and is in fighting state, false otherwise</returns>
        private bool IsCharacterTrackAvailable(string characterId, out CharacterTracks? tracks)
        {
            tracks = null;
            if (!_characterTracks.TryGetValue(characterId, out tracks))
                return false;
            
            return _state == MultiBattleState.Fighting && tracks.IsEnabled;
        }

        /// <summary>
        /// 检查敌人战斗轨道是否可用
        /// Check if enemy battle track is available
        /// </summary>
        /// <returns>如果敌人存在且处于战斗状态返回true，否则返回false / Returns true if enemy exists and is in fighting state, false otherwise</returns>
        private bool IsEnemyTrackAvailable(string enemyId, out EnemyTrack? track)
        {
            track = null;
            if (!_enemyTracks.TryGetValue(enemyId, out track))
                return false;
            
            return _state == MultiBattleState.Fighting && track.IsEnabled;
        }

        /// <summary>
        /// 获取指定角色的攻击进度（0-1范围）
        /// Get attack progress for specified character (0-1 range)
        /// </summary>
        /// <returns>返回0.0-1.0之间的进度值，角色不存在或非战斗状态时返回0.0 / Returns progress value between 0.0-1.0, returns 0.0 if character not found or not in fighting state</returns>
        public double GetCharacterAttackProgress(string characterId)
        {
            if (!IsCharacterTrackAvailable(characterId, out var tracks))
                return 0.0;

            return tracks!.AttackTrack.Progress01(_clock.NowMs);
        }

        /// <summary>
        /// 获取指定角色的特殊技能进度（0-1范围）
        /// Get special skill progress for specified character (0-1 range)
        /// </summary>
        /// <returns>返回0.0-1.0之间的进度值，角色不存在或非战斗状态时返回0.0 / Returns progress value between 0.0-1.0, returns 0.0 if character not found or not in fighting state</returns>
        public double GetCharacterSpecialProgress(string characterId)
        {
            if (!IsCharacterTrackAvailable(characterId, out var tracks))
                return 0.0;

            return tracks!.SpecialTrack.Progress01(_clock.NowMs);
        }

        /// <summary>
        /// 获取指定敌人的攻击进度（0-1范围）
        /// Get attack progress for specified enemy (0-1 range)
        /// </summary>
        /// <returns>返回0.0-1.0之间的进度值，敌人不存在或非战斗状态时返回0.0 / Returns progress value between 0.0-1.0, returns 0.0 if enemy not found or not in fighting state</returns>
        public double GetEnemyAttackProgress(string enemyId)
        {
            if (!IsEnemyTrackAvailable(enemyId, out var track))
                return 0.0;

            return track!.AttackTrack.Progress01(_clock.NowMs);
        }

        /// <summary>
        /// 获取指定角色的攻击剩余时间（毫秒）
        /// Get attack time remaining for specified character (milliseconds)
        /// </summary>
        /// <returns>返回剩余时间（毫秒），角色不存在或非战斗状态时返回0.0 / Returns remaining time in milliseconds, returns 0.0 if character not found or not in fighting state</returns>
        public double GetCharacterAttackTimeRemaining(string characterId)
        {
            if (!IsCharacterTrackAvailable(characterId, out var tracks))
                return 0.0;

            return tracks!.AttackTrack.TimeToNextMs(_clock.NowMs);
        }

        /// <summary>
        /// 获取指定角色的特殊技能剩余时间（毫秒）
        /// Get special skill time remaining for specified character (milliseconds)
        /// </summary>
        /// <returns>返回剩余时间（毫秒），角色不存在或非战斗状态时返回0.0 / Returns remaining time in milliseconds, returns 0.0 if character not found or not in fighting state</returns>
        public double GetCharacterSpecialTimeRemaining(string characterId)
        {
            if (!IsCharacterTrackAvailable(characterId, out var tracks))
                return 0.0;

            return tracks!.SpecialTrack.TimeToNextMs(_clock.NowMs);
        }

        /// <summary>
        /// 获取指定敌人的攻击剩余时间（毫秒）
        /// Get attack time remaining for specified enemy (milliseconds)
        /// </summary>
        /// <returns>返回剩余时间（毫秒），敌人不存在或非战斗状态时返回0.0 / Returns remaining time in milliseconds, returns 0.0 if enemy not found or not in fighting state</returns>
        public double GetEnemyAttackTimeRemaining(string enemyId)
        {
            if (!IsEnemyTrackAvailable(enemyId, out var track))
                return 0.0;

            return track!.AttackTrack.TimeToNextMs(_clock.NowMs);
        }
    }

    /// <summary>
    /// 多单位战斗配置
    /// Multi-unit battle configuration
    /// </summary>
    public class MultiBattleConfig
    {
        /// <summary>
        /// 玩家目标选择策略
        /// Player target selection strategy
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("playerTargetStrategy")]
        public TargetStrategy PlayerTargetStrategy { get; set; } = TargetStrategy.Random;

        /// <summary>
        /// 敌人目标选择策略
        /// Enemy target selection strategy
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("enemyTargetStrategy")]
        public TargetStrategy EnemyTargetStrategy { get; set; } = TargetStrategy.Random;

        /// <summary>
        /// 特殊技能是否为AOE
        /// Whether special skill is AOE
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("specialIsAoe")]
        public bool SpecialIsAoe { get; set; } = true;

        /// <summary>
        /// AOE伤害倍率
        /// AOE damage multiplier
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("aoeDamageMultiplier")]
        public double AoeDamageMultiplier { get; set; } = 0.8;

        /// <summary>
        /// 是否允许玩家复活
        /// Whether to allow player revival
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("allowPlayerRevive")]
        public bool AllowPlayerRevive { get; set; } = true;

        /// <summary>
        /// 是否允许敌人刷新
        /// Whether to allow enemy respawn
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("allowEnemyRespawn")]
        public bool AllowEnemyRespawn { get; set; } = true;

        /// <summary>
        /// 玩家复活冷却时间（毫秒）
        /// Player revive cooldown in milliseconds
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("playerReviveCooldownMs")]
        public int PlayerReviveCooldownMs { get; set; } = 5000;

        /// <summary>
        /// 敌人刷新冷却时间（毫秒）
        /// Enemy respawn cooldown in milliseconds
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("enemyRespawnCooldownMs")]
        public int EnemyRespawnCooldownMs { get; set; } = 3000;

        /// <summary>
        /// 复活时是否满血
        /// Whether to revive with full HP
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("reviveWithFullHp")]
        public bool ReviveWithFullHp { get; set; } = true;
    }

    /// <summary>
    /// 多单位战斗快照
    /// Multi-unit battle snapshot
    /// </summary>
    public class MultiBattleSnapshot
    {
        public int ElapsedMs { get; set; }
        public MultiBattleState State { get; set; }
        public int PlayerTeamAlive { get; set; }
        public int PlayerTeamTotal { get; set; }
        public int EnemyTeamAlive { get; set; }
        public int EnemyTeamTotal { get; set; }
        public int TotalPlayerDamage { get; set; }
        public int TotalEnemyDamage { get; set; }
        public int TimeToResumeMs { get; set; }
        public int RngIndex { get; set; }
    }
}