using System;
using System.Collections.Generic;

namespace BlazorIdle.Game
{
    public enum BattleOutcome
    {
        Ongoing = 0,
        Victory = 1, // 敌人死亡（单次交锋结果）
        Defeat = 2   // 玩家死亡（单次交锋结果）
    }

    // 持续战斗循环状态
    public enum LoopState
    {
        Fighting = 0,
        PlayerDeadCooldown = 1,
        EnemyDeadCooldown = 2
    }

    public sealed class BattleSnapshot
    {
        public int ElapsedMs { get; init; }
        public int EnemyHp { get; init; }
        public int EnemyMaxHp { get; init; }
        public int PlayerHp { get; init; }
        public int PlayerMaxHp { get; init; }

        public BattleOutcome Outcome { get; init; }

        public LoopState LoopState { get; init; }
        public bool WaitingPlayerRevive { get; init; }
        public bool WaitingEnemyRespawn { get; init; }
        public int TimeToResumeMs { get; init; }

        public int TotalDamage { get; init; }
        public int RngIndex { get; init; }

        public bool Finished => false; // 持续战斗模式不自动结束
    }

    public sealed class BattleInstance
    {
        private readonly IGameClock _clock;
        private readonly RngContext _rng;
        private readonly Character _player;
        private readonly Enemy _enemy;

        private readonly TrackState _attackTrack;
        private readonly TrackState _specialTrack;
        private readonly TrackState _enemyAttackTrack;

        private bool _running;
        private int _totalDamage;
        private int _tickCount;

        private int _playerHp;

        private readonly List<CombatSegment> _segments = new();
        private SegmentAggregator _aggregator = new(new SegmentAggregatorOptions { MaxEvents = 12, MaxDurationMs = 2000 });

        private readonly int _rngSeed;
        private int _rngIndexStart;
        private int _rngIndexEnd;

        // 持续战斗循环
        private LoopState _loopState = LoopState.Fighting;
        private int _resumeAtMs = 0;

        /// <summary>
        /// 战斗事件 - 伤害、暴击等事件
        /// Combat event - damage, crit events etc
        /// </summary>
        public event Action<CombatEvent>? CombatEventFired;

        /// <summary>
        /// 掉落物事件 - 怪物死亡时触发
        /// Loot drop event - triggered when monster dies
        /// </summary>
        public event Action<LootDropEvent>? LootDropped;

        /// <summary>
        /// 经验获得事件 - 怪物死亡时触发
        /// Experience gain event - triggered when monster dies
        /// </summary>
        public event Action<ExperienceGainEvent>? ExperienceGained;

        public BattleInstance(IGameClock clock, RngContext rng, Character player, Enemy enemy)
        {
            _clock = clock;
            _rng = rng;
            _player = player;
            _enemy = enemy;

            var attackIntervalMs = 1000.0 / Math.Max(0.1, _player.AttackRateAPS);
            _attackTrack = new TrackState(TrackType.Attack, attackIntervalMs);
            _attackTrack.SetHaste(1.0 + _player.HastePercent / 100.0);

            var specialIntervalMs = Math.Max(100.0, _player.SpecialIntervalSec * 1000.0);
            _specialTrack = new TrackState(TrackType.Special, specialIntervalMs);

            var enemyAtkIntervalMs = Math.Max(100.0, _enemy.AttackIntervalSec * 1000.0);
            _enemyAttackTrack = new TrackState(TrackType.EnemyAttack, enemyAtkIntervalMs);

            _rngSeed = rng.Seed;
        }

        public void Start()
        {
            _running = true;
            _clock.Reset();
            _totalDamage = 0;
            _tickCount = 0;

            _enemy.Hp = Math.Max(1, _enemy.MaxHp);
            _playerHp = Math.Max(1, _player.MaxHp);

            _attackTrack.Reset(0);
            _specialTrack.Reset(0);
            _enemyAttackTrack.Reset(0);

            _segments.Clear();
            _aggregator = new SegmentAggregator(new SegmentAggregatorOptions { MaxEvents = 12, MaxDurationMs = 2000 });
            _aggregator.Reset();

            _rngIndexStart = _rng.Index;

            _loopState = LoopState.Fighting;
            _resumeAtMs = 0;
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            var seg = _aggregator.ForceFlush(_clock.NowMs, _rng.Index);
            if (seg != null) _segments.Add(seg);
            _rngIndexEnd = _rng.Index;
        }

        public bool IsRunning => _running;

        public void AdvanceTick(int tickMs)
        {
            if (!_running) return;

            _clock.AdvanceBy(tickMs);
            _tickCount++;

            int now = _clock.NowMs;

            // 冷却中：只等待到点，恢复后“双方”轨道都重置为 now，避免积压触发
            if (_loopState == LoopState.PlayerDeadCooldown)
            {
                if (now >= _resumeAtMs)
                {
                    // 玩家复活
                    _playerHp = Math.Max(1, _player.MaxHp);
                    // 恢复时重置双方轨道，防止累计触发
                    _attackTrack.Reset(now);
                    _specialTrack.Reset(now);
                    _enemyAttackTrack.Reset(now);
                    _loopState = LoopState.Fighting;
                }
                var segByTimeWait = _aggregator.Tick(now, _rng.Index);
                if (segByTimeWait != null) _segments.Add(segByTimeWait);
                return;
            }
            else if (_loopState == LoopState.EnemyDeadCooldown)
            {
                if (now >= _resumeAtMs)
                {
                    // 敌人刷新
                    _enemy.Hp = Math.Max(1, _enemy.MaxHp);
                    // 恢复时重置双方轨道，防止累计触发
                    _enemyAttackTrack.Reset(now);
                    _attackTrack.Reset(now);
                    _specialTrack.Reset(now);
                    _loopState = LoopState.Fighting;
                }
                var segByTimeWait = _aggregator.Tick(now, _rng.Index);
                if (segByTimeWait != null) _segments.Add(segByTimeWait);
                return;
            }

            // Fighting：正常推进
            var atkCount = _attackTrack.CollectTriggers(now);
            for (int i = 0; i < atkCount; i++)
            {
                int dmg = PlayerRollDamage(_player.DamagePerAttack, allowCrit: true);
                ApplyDamageToEnemy(dmg, EventSource.Attack);
                if (_enemy.Hp <= 0)
                {
                    EnterEnemyCooldown(now);
                    break;
                }
            }

            if (_loopState == LoopState.Fighting)
            {
                var spCount = _specialTrack.CollectTriggers(now);
                for (int i = 0; i < spCount; i++)
                {
                    int dmg = PlayerRollDamage(_player.SpecialDamage, allowCrit: true);
                    ApplyDamageToEnemy(dmg, EventSource.Special);
                    if (_enemy.Hp <= 0)
                    {
                        EnterEnemyCooldown(now);
                        break;
                    }
                }
            }

            if (_loopState == LoopState.Fighting)
            {
                var enemyCount = _enemyAttackTrack.CollectTriggers(now);
                for (int i = 0; i < enemyCount; i++)
                {
                    int dmg = EnemyRollDamage(_enemy.DamagePerHit);
                    ApplyDamageToPlayer(dmg, EventSource.EnemyAttack);
                    if (_playerHp <= 0)
                    {
                        EnterPlayerCooldown(now);
                        break;
                    }
                }
            }

            var segByTime = _aggregator.Tick(now, _rng.Index);
            if (segByTime != null) _segments.Add(segByTime);
        }

        private void EnterEnemyCooldown(int now)
        {
            _loopState = LoopState.EnemyDeadCooldown;
            // 触发掉落物事件
            // Trigger loot drop events
            ProcessLootDrops(now);

            _resumeAtMs = now + Math.Max(0, _enemy.RespawnMs);
            // 进入冷却时可选择立即把玩家轨道推进到 now+interval，以便视觉上“清空并静止”
            // 我们在 UI 端已返回 0 进度，这里无需额外处理；真正避免积压由恢复时 Reset(now) 保证。
        }

        private void EnterPlayerCooldown(int now)
        {
            _loopState = LoopState.PlayerDeadCooldown;
            _resumeAtMs = now + Math.Max(0, _player.ReviveMs);
            // 同上，真正避免积压由恢复时 Reset(now) 保证。
        }


        /// <summary>
        /// 处理掉落物 - 根据配置随机生成掉落物
        /// Process loot drops - randomly generate loot based on configuration
        /// </summary>
        private void ProcessLootDrops(int now)
        {
            if (_enemy.LootDrops == null || _enemy.LootDrops.Count == 0)
                return;

            foreach (var drop in _enemy.LootDrops)
            {
                // 检查掉落概率
                // Check drop chance
                if (_rng.NextDouble() > drop.DropChance)
                    continue;

                // 随机掉落数量（在最小和最大之间）
                // Random drop quantity (between min and max)
                int quantity = drop.MinQuantity;
                if (drop.MaxQuantity > drop.MinQuantity)
                {
                    quantity = _rng.NextRange(drop.MinQuantity, drop.MaxQuantity);
                }

                if (quantity > 0)
                {
                    // 触发掉落事件
                    // Trigger loot drop event
                    var lootEvent = new LootDropEvent
                    {
                        TimeMs = now,
                        ItemId = drop.ItemId,
                        Quantity = quantity
                    };
                    LootDropped?.Invoke(lootEvent);
                }
            }
        }

        /// <summary>
        /// 处理经验获得 - 怪物死亡时触发
        /// Process experience gain - triggered when monster dies
        /// </summary>
        private void ProcessExperienceGain(int now)
        {
            if (_enemy.BaseExperience <= 0) return;

            // 触发经验事件，实际计算由服务端完成
            // Trigger experience event, actual calculation done on server
            var expEvent = new ExperienceGainEvent
            {
                TimeMs = now,
                ProfessionId = _player.ActiveCombatProfessionId,
                BaseExperience = _enemy.BaseExperience,
                MonsterId = _enemy.MonsterId
            };
            ExperienceGained?.Invoke(expEvent);
        }
        
        private int PlayerRollDamage(int baseDamage, bool allowCrit)
        {
            double dmg = Math.Floor(_rng.Jitter(baseDamage, _player.VariancePct));
            if (dmg < 1) dmg = 1;
            if (allowCrit && _rng.NextDouble() < (_player.CritChancePercent / 100.0))
            {
                dmg = Math.Floor(dmg * Math.Max(1.0, _player.CritMultiplier));
            }
            return (int)dmg;
        }

        private int EnemyRollDamage(int baseDamage)
        {
            double dmg = Math.Floor(_rng.Jitter(baseDamage, _enemy.VariancePct));
            if (dmg < 1) dmg = 1;
            return (int)dmg;
        }

        private void ApplyDamageToEnemy(int dmg, EventSource src)
        {
            _enemy.Hp = Math.Max(0, _enemy.Hp - dmg);
            _totalDamage += dmg;

            var ev = new CombatEvent
            {
                Attacker = ActorType.Player,
                Defender = ActorType.Enemy,
                Source = src,
                TimeMs = _clock.NowMs,
                Damage = dmg,
                Crit = false,
                RngIndexAfter = _rng.Index,
                DefenderHpAfter = _enemy.Hp
            };
            var flushed = _aggregator.AddEvent(ev);
            if (flushed != null) _segments.Add(flushed);
            CombatEventFired?.Invoke(ev);
        }

        private void ApplyDamageToPlayer(int dmg, EventSource src)
        {
            _playerHp = Math.Max(0, _playerHp - dmg);

            var ev = new CombatEvent
            {
                Attacker = ActorType.Enemy,
                Defender = ActorType.Player,
                Source = src,
                TimeMs = _clock.NowMs,
                Damage = dmg,
                Crit = false,
                RngIndexAfter = _rng.Index,
                DefenderHpAfter = _playerHp
            };
            var flushed = _aggregator.AddEvent(ev);
            if (flushed != null) _segments.Add(flushed);
            CombatEventFired?.Invoke(ev);
        }

        private BattleOutcome CurrentExchangeOutcome
        {
            get
            {
                if (_playerHp <= 0) return BattleOutcome.Defeat;
                if (_enemy.Hp <= 0) return BattleOutcome.Victory;
                return BattleOutcome.Ongoing;
            }
        }

        public BattleSnapshot GetSnapshot()
        {
            var now = _clock.NowMs;
            var waitingPlayer = _loopState == LoopState.PlayerDeadCooldown;
            var waitingEnemy = _loopState == LoopState.EnemyDeadCooldown;
            var timeToResume = (waitingPlayer || waitingEnemy) ? Math.Max(0, _resumeAtMs - now) : 0;

            return new BattleSnapshot
            {
                ElapsedMs = now,
                EnemyHp = _enemy.Hp,
                EnemyMaxHp = _enemy.MaxHp,
                PlayerHp = _playerHp,
                PlayerMaxHp = _player.MaxHp,
                Outcome = CurrentExchangeOutcome,
                LoopState = _loopState,
                WaitingPlayerRevive = waitingPlayer,
                WaitingEnemyRespawn = waitingEnemy,
                TimeToResumeMs = timeToResume,
                TotalDamage = _totalDamage,
                RngIndex = _rng.Index
            };
        }

        public IReadOnlyList<CombatSegment> Segments => _segments;

        public BattleDigest BuildDigest()
        {
            if (_running) Stop();

            var duration = _clock.NowMs;
            var rngEnd = _rngIndexEnd;

            return BattleDigest.FromSegments(
                durationMs: duration,
                tickCount: _tickCount,
                rngStart: _rngIndexStart,
                rngEnd: rngEnd,
                seed: _rngSeed,
                segments: _segments);
        }

        public double TheoreticalDps()
        {
            var hasteFactor = 1.0 + _player.HastePercent / 100.0;
            var attackDps = _player.DamagePerAttack * _player.AttackRateAPS * hasteFactor;
            var specialDps = _player.SpecialDamage / Math.Max(0.1, _player.SpecialIntervalSec);
            return attackDps + specialDps;
        }

        // ===== UI 辅助：冷却状态进度清空并暂停 =====
        public double AttackProgress01 =>
            _loopState == LoopState.Fighting ? _attackTrack.Progress01(_clock.NowMs) : 0.0;

        public double SpecialProgress01 =>
            _loopState == LoopState.Fighting ? _specialTrack.Progress01(_clock.NowMs) : 0.0;

        public double EnemyProgress01 =>
            _loopState == LoopState.Fighting ? _enemyAttackTrack.Progress01(_clock.NowMs) : 0.0;

        public double AttackTimeToNextMs =>
            _loopState == LoopState.Fighting ? _attackTrack.TimeToNextMs(_clock.NowMs) : 0.0;

        public double SpecialTimeToNextMs =>
            _loopState == LoopState.Fighting ? _specialTrack.TimeToNextMs(_clock.NowMs) : 0.0;

        public double EnemyTimeToNextMs =>
            _loopState == LoopState.Fighting ? _enemyAttackTrack.TimeToNextMs(_clock.NowMs) : 0.0;
    }
}