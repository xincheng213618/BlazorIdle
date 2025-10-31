using System;

namespace BlazorIdle.Game
{
    // 轻量确定性 RNG（XorShift32），记录消费序，便于 Digest 审计
    public sealed class RngContext
    {
        private uint _state;
        public int Seed { get; }
        public int Index { get; private set; }

        public RngContext(int seed)
        {
            if (seed == 0) seed = 1; // 避免全 0
            Seed = seed;
            _state = (uint)seed;
            Index = 0;
        }

        private uint NextUInt()
        {
            // xorshift32
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            Index++;
            return x;
        }

        public double NextDouble() // [0,1)
        {
            return (NextUInt() / (double)uint.MaxValue);
        }

        public int NextRange(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive) (minInclusive, maxInclusive) = (maxInclusive, minInclusive);
            var span = (long)maxInclusive - minInclusive + 1;
            var v = (int)(minInclusive + (long)(NextDouble() * span));
            if (v > maxInclusive) v = maxInclusive;
            return v;
        }

        public double Jitter(double baseValue, double pct) // pct=0.05 -> ±5%
        {
            var r = (NextDouble() * 2.0) - 1.0; // [-1,1)
            return baseValue * (1.0 + r * pct);
        }
    }
}