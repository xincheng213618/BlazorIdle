using System;
using System.Collections.Generic;

namespace BlazorIdle.Game
{
    public sealed class PlayerConfig
    {
        public double AttackRateAPS { get; set; } = 2.0;
        public int DamagePerAttack { get; set; } = 15;
        public double HastePercent { get; set; } = 0.0;

        public double SpecialIntervalSec { get; set; } = 5.0;
        public int SpecialDamage { get; set; } = 120;

        public double VariancePct { get; set; } = 0.05;
    }

    public sealed class EnemyState
    {
        public int MaxHp { get; set; } = 300;
        public int Hp { get; set; } = 300;
    }

    public sealed class BattleSnapshot
    {
        public int ElapsedMs { get; init; }
        public int EnemyHp { get; init; }
        public int EnemyMaxHp { get; init; }
        public bool Finished { get; init; }
        public int TotalDamage { get; init; }
        public int RngIndex { get; init; }
    }

    public sealed class BattleInstance
    {
        private readonly IGameClock _clock;
        private readonly RngContext _rng;
        private readonly PlayerConfig _player;
        private readonly EnemyState _enemy;

        private readonly TrackState _attackTrack;
        private readonly TrackState _specialTrack;

        private bool _running;
        private int _totalDamage;
        private int _tickCount;

        private readonly List<CombatSegment> _segments = new();
        private SegmentAggregator _aggregator = new(new SegmentAggregatorOptions { MaxEvents = 12, MaxDurationMs = 2000 });

        private readonly int _rngSeed;
        private int _rngIndexStart;
        private int _rngIndexEnd;

        // 新增：当产生一次战斗事件（Attack/Special）时触发
        public event Action<CombatEvent>? CombatEventFired;

        public BattleInstance(IGameClock clock, RngContext rng, PlayerConfig player, EnemyState enemy)
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

            _rngSeed = rng.Seed;
        }

        public void Start()
        {
            _running = true;
            _clock.Reset();
            _totalDamage = 0;
            _tickCount = 0;
            _enemy.Hp = Math.Max(1, _enemy.MaxHp);

            _attackTrack.Reset(0);
            _specialTrack.Reset(0);

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
            if (_enemy.Hp <= 0) { Stop(); return; }

            _clock.AdvanceBy(tickMs);
            _tickCount++;

            // Attack
            if (_attackTrack.TryTrigger(_clock.NowMs))
            {
                var dmg = (int)Math.Floor(_rng.Jitter(_player.DamagePerAttack, _player.VariancePct));
                if (dmg < 1) dmg = 1;
                ApplyDamage(dmg);

                var ev = new CombatEvent
                {
                    Source = EventSource.Attack,
                    TimeMs = _clock.NowMs,
                    Damage = dmg,
                    RngIndexAfter = _rng.Index,
                    EnemyHpAfter = _enemy.Hp
                };
                var flushed = _aggregator.AddEvent(ev);
                if (flushed != null) _segments.Add(flushed);

                CombatEventFired?.Invoke(ev);
            }

            // Special
            if (_specialTrack.TryTrigger(_clock.NowMs))
            {
                var dmg = (int)Math.Floor(_rng.Jitter(_player.SpecialDamage, _player.VariancePct));
                if (dmg < 1) dmg = 1;
                ApplyDamage(dmg);

                var ev = new CombatEvent
                {
                    Source = EventSource.Special,
                    TimeMs = _clock.NowMs,
                    Damage = dmg,
                    RngIndexAfter = _rng.Index,
                    EnemyHpAfter = _enemy.Hp
                };
                var flushed = _aggregator.AddEvent(ev);
                if (flushed != null) _segments.Add(flushed);

                CombatEventFired?.Invoke(ev);
            }

            // 时间阈值 Flush
            var segByTime = _aggregator.Tick(_clock.NowMs, _rng.Index);
            if (segByTime != null) _segments.Add(segByTime);

            if (_enemy.Hp <= 0)
            {
                Stop();
            }
        }

        private void ApplyDamage(int dmg)
        {
            _enemy.Hp = Math.Max(0, _enemy.Hp - dmg);
            _totalDamage += dmg;
        }

        public BattleSnapshot GetSnapshot()
        {
            return new BattleSnapshot
            {
                ElapsedMs = _clock.NowMs,
                EnemyHp = _enemy.Hp,
                EnemyMaxHp = _enemy.MaxHp,
                Finished = !_running || _enemy.Hp <= 0,
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
            var hasteFactor = 1.0 + _player.HastePercent / 100.0;
            var attackDps = _player.DamagePerAttack * _player.AttackRateAPS * hasteFactor;
            var specialDps = _player.SpecialDamage / Math.Max(0.1, _player.SpecialIntervalSec);
            return attackDps + specialDps;
        }

        // ===== UI 辅助：轨道进度与剩余时间 =====
        public double AttackProgress01 => _attackTrack.Progress01(_clock.NowMs);
        public double SpecialProgress01 => _specialTrack.Progress01(_clock.NowMs);
        public double AttackTimeToNextMs => _attackTrack.TimeToNextMs(_clock.NowMs);
        public double SpecialTimeToNextMs => _specialTrack.TimeToNextMs(_clock.NowMs);
    }
}