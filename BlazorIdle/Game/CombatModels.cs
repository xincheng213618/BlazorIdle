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

    public class CombatEvent
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

    /// <summary>
    /// 掉落物事件 - 怪物死亡时触发，通知掉落了什么物品
    /// Loot drop event - triggered when monster dies, notifies what items dropped
    /// </summary>
    public sealed class LootDropEvent
    {
        /// <summary>
        /// 事件发生时间（毫秒）
        /// Event time in milliseconds
        /// </summary>
        public int TimeMs { get; init; }

        /// <summary>
        /// 掉落的物品ID
        /// Dropped item ID
        /// </summary>
        public string ItemId { get; init; } = string.Empty;

        /// <summary>
        /// 掉落的物品数量
        /// Dropped item quantity
        /// </summary>
        public int Quantity { get; init; }

        /// <summary>
        /// 掉落来源怪物ID（可选）
        /// Source monster ID (optional)
        /// </summary>
        public string? MonsterId { get; init; }
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