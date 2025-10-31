using System;

namespace BlazorIdle.Game
{
    // 可测试/可回放：由仿真主动推进时间
    public interface IGameClock
    {
        int NowMs { get; }
        void AdvanceBy(int ms);
        void Reset();
    }

    // 简单仿真时钟：不读系统时间，完全由 AdvanceBy 推进
    public sealed class SimClock : IGameClock
    {
        public int NowMs { get; private set; }

        public void AdvanceBy(int ms)
        {
            if (ms <= 0) return;
            NowMs += ms;
        }

        public void Reset()
        {
            NowMs = 0;
        }
    }
}