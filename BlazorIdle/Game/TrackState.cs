using System;

namespace BlazorIdle.Game
{
    public enum TrackType
    {
        Attack = 1,
        Special = 2
    }

    // 轨道：记录基础间隔、急速（仅攻击生效）、下一触发时间
    public sealed class TrackState
    {
        public TrackType Type { get; }
        public double BaseIntervalMs { get; }
        public double HasteFactor { get; private set; } = 1.0; // 1.0=无急速
        public double NextTriggerAtMs { get; private set; }

        public TrackState(TrackType type, double baseIntervalMs)
        {
            Type = type;
            BaseIntervalMs = Math.Max(1.0, baseIntervalMs);
        }

        public void SetHaste(double hasteFactor)
        {
            HasteFactor = Math.Max(0.1, hasteFactor);
        }

        public void Reset(double startMs = 0)
        {
            NextTriggerAtMs = startMs + EffectiveInterval();
        }

        public bool TryTrigger(int nowMs)
        {
            if (nowMs + 0.0001 >= NextTriggerAtMs) // 容差
            {
                // 设置下一次时间
                NextTriggerAtMs += EffectiveInterval();
                return true;
            }
            return false;
        }

        private double EffectiveInterval()
        {
            // 仅 Attack 受到急速影响；Special 默认不受
            if (Type == TrackType.Attack)
            {
                return BaseIntervalMs / HasteFactor;
            }
            return BaseIntervalMs;
        }
    }
}