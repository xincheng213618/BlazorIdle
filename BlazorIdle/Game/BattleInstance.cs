using System;

namespace BlazorIdle.Game
{
    public sealed class PlayerConfig
    {
        // 攻击（受急速）
        public double AttackRateAPS { get; set; } = 2.0;   // 每秒攻击次数
        public int DamagePerAttack { get; set; } = 15;     // 每次攻击伤害
        public double HastePercent { get; set; } = 0.0;    // 0 = 无急速

        // Special（不受急速）
        public double SpecialIntervalSec { get; set; } = 5.0;
        public int SpecialDamage { get; set; } = 120;

        // 浮动
        public double VariancePct { get; set; } = 0.05;    // ±5%
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

    // 最小可用的双轨战斗实例：固定 Tick 驱动，确定性 RNG
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
        }

        public void Start()
        {
            _running = true;
            _clock.Reset();
            _totalDamage = 0;
            _enemy.Hp = Math.Max(1, _enemy.MaxHp);

            _attackTrack.Reset(0);
            _specialTrack.Reset(0);
        }

        public void Stop()
        {
            _running = false;
        }

        public bool IsRunning => _running;

        // 每次调用推进 tickMs；按轨道尝试触发
        public void AdvanceTick(int tickMs)
        {
            if (!_running) return;
            if (_enemy.Hp <= 0) { _running = false; return; }

            _clock.AdvanceBy(tickMs);

            // Attack 触发
            if (_attackTrack.TryTrigger(_clock.NowMs))
            {
                var dmg = (int)Math.Floor(_rng.Jitter(_player.DamagePerAttack, _player.VariancePct));
                if (dmg < 1) dmg = 1;
                ApplyDamage(dmg);
            }

            // Special 触发
            if (_specialTrack.TryTrigger(_clock.NowMs))
            {
                var dmg = (int)Math.Floor(_rng.Jitter(_player.SpecialDamage, _player.VariancePct));
                if (dmg < 1) dmg = 1;
                ApplyDamage(dmg);
            }

            if (_enemy.Hp <= 0)
            {
                _running = false;
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

        public double TheoreticalDps()
        {
            // 急速只影响攻击；Special 是额外的脉冲伤害
            var hasteFactor = 1.0 + _player.HastePercent / 100.0;
            var attackDps = _player.DamagePerAttack * _player.AttackRateAPS * hasteFactor;
            var specialDps = _player.SpecialDamage / Math.Max(0.1, _player.SpecialIntervalSec);
            return attackDps + specialDps;
        }
    }
}