using System;

namespace BlazorIdle.Game
{
    public enum TrackType
    {
        Attack = 1,
        Special = 2,
        EnemyAttack = 3
    }

    // 轨道：记录基础间隔、急速（仅玩家的 Attack 生效）、下一触发时间
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
            NextTriggerAtMs = startMs + EffectiveIntervalMs;
        }

        // 单次尝试触发（保留供简单使用）
        public bool TryTrigger(int nowMs)
        {
            if (nowMs + 0.0001 >= NextTriggerAtMs)
            {
                NextTriggerAtMs += EffectiveIntervalMs;
                return true;
            }
            return false;
        }

        // 收集本 Tick 截止可能的多次触发（避免 Tick>间隔 时漏触发）
        public int CollectTriggers(int nowMs, int maxTriggers = 8)
        {
            int count = 0;
            while (nowMs + 0.0001 >= NextTriggerAtMs && count < maxTriggers)
            {
                NextTriggerAtMs += EffectiveIntervalMs;
                count++;
            }
            return count;
        }

        // 公开实际生效的间隔（用于 UI 进度与调试）
        public double EffectiveIntervalMs =>
            Type == TrackType.Attack ? BaseIntervalMs / HasteFactor : BaseIntervalMs;

        // 当前进度（0..1），0 表示刚开始计时，1 表示即将触发
        public double Progress01(int nowMs)
        {
            var interval = EffectiveIntervalMs;
            if (interval <= 0.0001) return 1.0;

            var lastTriggerAt = NextTriggerAtMs - interval;
            var elapsed = nowMs - lastTriggerAt;
            var p = elapsed / interval;
            return Math.Clamp(p, 0.0, 1.0);
        }

        // 距离下一次触发还需的毫秒（>=0）
        public double TimeToNextMs(int nowMs)
        {
            var remain = NextTriggerAtMs - nowMs;
            return remain <= 0 ? 0 : remain;
        }
    }
}