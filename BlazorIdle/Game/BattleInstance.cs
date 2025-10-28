using System;
using System.Collections.Generic;

namespace BlazorIdle.Game
{
    public enum BattleOutcome
    {
        Ongoing = 0,
        Victory = 1, // 敌人死亡
        Defeat = 2   // 玩家死亡
    }

    public sealed class BattleSnapshot
    {
        public int ElapsedMs { get; init; }
        public int EnemyHp { get; init; }
        public int EnemyMaxHp { get; init; }
        public int PlayerHp { get; init; }
        public int PlayerMaxHp { get; init; }
        public BattleOutcome Outcome { get; init; }
        public int TotalDamage { get; init; } // 玩家对敌方造成的总伤
        public int RngIndex { get; init; }
        public bool Finished => Outcome == BattleOutcome.Victory || Outcome == BattleOutcome.Defeat;
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
        private int _totalDamage; // 玩家对敌总伤
        private int _tickCount;

        private int _playerHp;

        private readonly List<CombatSegment> _segments = new();
        private SegmentAggregator _aggregator = new(new SegmentAggregatorOptions { MaxEvents = 12, MaxDurationMs = 2000 });

        private readonly int _rngSeed;
        private int _rngIndexStart;
        private int _rngIndexEnd;

        public event Action<CombatEvent>? CombatEventFired;

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

            _rngIndexStart = _rng.Index; // 通常为 0
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            // 结束时强制 Flush 残段
            var seg = _aggregator.ForceFlush(_clock.NowMs, _rng.Index);
            if (seg != null) _segments.Add(seg);
            _rngIndexEnd = _rng.Index;
        }

        public bool IsRunning => _running;

        public void AdvanceTick(int tickMs)
        {
            if (!_running) return;
            if (Outcome != BattleOutcome.Ongoing) { Stop(); return; }

            _clock.AdvanceBy(tickMs);
            _tickCount++;

            int now = _clock.NowMs;

            // 玩家 Attack 可能多次触发
            var atkCount = _attackTrack.CollectTriggers(now);
            for (int i = 0; i < atkCount; i++)
            {
                int dmg = PlayerRollDamage(_player.DamagePerAttack, allowCrit: true);
                ApplyDamageToEnemy(dmg, EventSource.Attack);
            }

            // 玩家 Special
            var spCount = _specialTrack.CollectTriggers(now);
            for (int i = 0; i < spCount; i++)
            {
                int dmg = PlayerRollDamage(_player.SpecialDamage, allowCrit: true);
                ApplyDamageToEnemy(dmg, EventSource.Special);
            }

            // 敌人攻击
            var enemyCount = _enemyAttackTrack.CollectTriggers(now);
            for (int i = 0; i < enemyCount; i++)
            {
                int dmg = EnemyRollDamage(_enemy.DamagePerHit);
                ApplyDamageToPlayer(dmg, EventSource.EnemyAttack);
            }

            // 时间阈值 Flush
            var segByTime = _aggregator.Tick(now, _rng.Index);
            if (segByTime != null) _segments.Add(segByTime);

            if (Outcome != BattleOutcome.Ongoing)
            {
                Stop();
            }
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
            // 敌人暂不暴击，保留浮动
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
                Crit = false, // 可扩展为记录是否暴击
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

        private BattleOutcome Outcome
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
            return new BattleSnapshot
            {
                ElapsedMs = _clock.NowMs,
                EnemyHp = _enemy.Hp,
                EnemyMaxHp = _enemy.MaxHp,
                PlayerHp = _playerHp,
                PlayerMaxHp = _player.MaxHp,
                Outcome = Outcome,
                TotalDamage = _totalDamage,
                RngIndex = _rng.Index
            };
        }

        public IReadOnlyList<CombatSegment> Segments => _segments;

        public BattleDigest BuildDigest()
        {
            // 确保停止时已 Flush
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
            // 急速只影响 Attack；Special 是额外脉冲
            var hasteFactor = 1.0 + _player.HastePercent / 100.0;
            var attackDps = _player.DamagePerAttack * _player.AttackRateAPS * hasteFactor;
            var specialDps = _player.SpecialDamage / Math.Max(0.1, _player.SpecialIntervalSec);
            return attackDps + specialDps;
        }

        // ===== UI 辅助：轨道进度与剩余时间 =====
        public double AttackProgress01 => _attackTrack.Progress01(_clock.NowMs);
        public double SpecialProgress01 => _specialTrack.Progress01(_clock.NowMs);
        public double EnemyProgress01 => _enemyAttackTrack.Progress01(_clock.NowMs);

        public double AttackTimeToNextMs => _attackTrack.TimeToNextMs(_clock.NowMs);
        public double SpecialTimeToNextMs => _specialTrack.TimeToNextMs(_clock.NowMs);
        public double EnemyTimeToNextMs => _enemyAttackTrack.TimeToNextMs(_clock.NowMs);
    }
}