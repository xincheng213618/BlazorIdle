using System;

namespace BlazorIdle.Game
{
    public enum ActorType
    {
        Player = 1,
        Enemy = 2
    }

    public enum EventSource
    {
        Attack = 1,
        Special = 2,
        EnemyAttack = 3
    }

    public sealed class CombatEvent
    {
        public ActorType Attacker { get; init; }
        public ActorType Defender { get; init; }
        public EventSource Source { get; init; }
        public int TimeMs { get; init; }
        public int Damage { get; init; }
        public bool Crit { get; init; }
        public int RngIndexAfter { get; init; } // 该事件消耗完 RNG 后的索引
        public int DefenderHpAfter { get; init; }  // 结算后防守方血量
    }

    public sealed class CombatSegment
    {
        public int StartMs { get; init; }
        public int EndMs { get; init; }
        public int EventCount { get; init; }
        public System.Collections.Generic.Dictionary<string, int> DamageBySource { get; init; } = new();
        public int RngIndexStart { get; init; }
        public int RngIndexEnd { get; init; }
    }
}