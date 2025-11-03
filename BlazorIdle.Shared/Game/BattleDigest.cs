using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Game
{
    public sealed class BattleDigest
    {
        public int DurationMs { get; init; }
        public int TickCount { get; init; }
        public int TotalEvents { get; init; }
        public int TotalDamage { get; init; }
        public Dictionary<string, int> DamageBySource { get; init; } = new();
        public int RngIndexStart { get; init; }
        public int RngIndexEnd { get; init; }
        public int Seed { get; init; }
        public int SegmentCount { get; init; }

        /// <summary>
        /// 获得的总基础经验值 - 服务端将应用增益系数
        /// Total base experience gained - server will apply multiplier
        /// </summary>
        public long TotalBaseExperience { get; init; }

        /// <summary>
        /// 经验接收职业ID
        /// Profession ID receiving experience
        /// </summary>
        public string ProfessionId { get; init; } = string.Empty;

        public double AverageDps => DurationMs <= 0 ? 0 : TotalDamage / (DurationMs / 1000.0);

        public static BattleDigest FromSegments(
            int durationMs,
            int tickCount,
            int rngStart,
            int rngEnd,
            int seed,
            IReadOnlyList<CombatSegment> segments)
        {
            var totalEvents = 0;
            var totalDamage = 0;
            var dmgBySource = new Dictionary<string, int>();

            foreach (var s in segments)
            {
                totalEvents += s.EventCount;
                totalDamage += s.DamageBySource.Values.Sum();
                foreach (var kv in s.DamageBySource)
                {
                    dmgBySource.TryGetValue(kv.Key, out var cur);
                    dmgBySource[kv.Key] = cur + kv.Value;
                }
            }

            return new BattleDigest
            {
                DurationMs = durationMs,
                TickCount = tickCount,
                TotalEvents = totalEvents,
                TotalDamage = totalDamage,
                DamageBySource = dmgBySource,
                RngIndexStart = rngStart,
                RngIndexEnd = rngEnd,
                Seed = seed,
                SegmentCount = segments.Count
            };
        }
    }
}