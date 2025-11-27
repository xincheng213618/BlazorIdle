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
        EnemyAttack = 3,
        Cast = 4,           // 施法技能 / Cast skill
        Skill = 5,          // 通用技能 / Generic skill
        Trigger = 6,        // 触发技能 / Triggered skill
        PostAttack = 7,     // PostAttack 窗口技能 / PostAttack window skill
        PostCast = 8,       // PostCast 窗口技能 / PostCast window skill
        Consumable = 9      // 消耗品技能 / Consumable skill
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

    /// <summary>
    /// 经验获得事件 - 怪物死亡时触发，通知获得的经验值
    /// Experience gain event - triggered when monster dies, notifies experience gained
    /// </summary>
    public sealed class ExperienceGainEvent
    {
        /// <summary>
        /// 事件发生时间（毫秒）
        /// Event time in milliseconds
        /// </summary>
        public int TimeMs { get; init; }

        /// <summary>
        /// 获得经验的职业ID
        /// Profession ID that gained experience
        /// </summary>
        public string ProfessionId { get; init; } = string.Empty;

        /// <summary>
        /// 获得的基础经验值（未应用增益）
        /// Base experience gained (before multiplier)
        /// </summary>
        public long BaseExperience { get; init; }

        /// <summary>
        /// 经验来源怪物ID（可选）
        /// Source monster ID (optional)
        /// </summary>
        public string? MonsterId { get; init; }
    }

    /// <summary>
    /// 施法开始事件 - Phase 8
    /// Cast start event - Phase 8
    /// </summary>
    public sealed class CastStartEvent
    {
        /// <summary>
        /// 事件发生时间（毫秒）
        /// Event time in milliseconds
        /// </summary>
        public int TimeMs { get; init; }

        /// <summary>
        /// 施法者ID
        /// Caster ID
        /// </summary>
        public string CasterId { get; init; } = string.Empty;

        /// <summary>
        /// 技能ID
        /// Skill ID
        /// </summary>
        public string SkillId { get; init; } = string.Empty;

        /// <summary>
        /// 施法时间（秒）
        /// Cast time in seconds
        /// </summary>
        public double CastTimeSec { get; init; }

        /// <summary>
        /// 是否暂停攻击轨道
        /// Whether attack track is paused
        /// </summary>
        public bool PauseAttackTrack { get; init; }
    }

    /// <summary>
    /// 施法完成事件 - Phase 8
    /// Cast complete event - Phase 8
    /// </summary>
    public sealed class CastCompleteEvent
    {
        /// <summary>
        /// 事件发生时间（毫秒）
        /// Event time in milliseconds
        /// </summary>
        public int TimeMs { get; init; }

        /// <summary>
        /// 施法者ID
        /// Caster ID
        /// </summary>
        public string CasterId { get; init; } = string.Empty;

        /// <summary>
        /// 技能ID
        /// Skill ID
        /// </summary>
        public string SkillId { get; init; } = string.Empty;

        /// <summary>
        /// 实际施法时间（秒）
        /// Actual cast time in seconds
        /// </summary>
        public double ActualCastTimeSec { get; init; }
    }

    /// <summary>
    /// 施法中断事件 - Phase 8
    /// Cast interrupt event - Phase 8
    /// </summary>
    public sealed class CastInterruptEvent
    {
        /// <summary>
        /// 事件发生时间（毫秒）
        /// Event time in milliseconds
        /// </summary>
        public int TimeMs { get; init; }

        /// <summary>
        /// 施法者ID
        /// Caster ID
        /// </summary>
        public string CasterId { get; init; } = string.Empty;

        /// <summary>
        /// 技能ID
        /// Skill ID
        /// </summary>
        public string SkillId { get; init; } = string.Empty;

        /// <summary>
        /// 中断原因
        /// Interrupt reason
        /// </summary>
        public string Reason { get; init; } = string.Empty;

        /// <summary>
        /// 已施法时间（秒）
        /// Elapsed cast time in seconds
        /// </summary>
        public double ElapsedSec { get; init; }
    }

    /// <summary>
    /// 消耗品使用事件 - 战斗中消耗品被使用时触发
    /// Consumable used event - triggered when consumable is used in battle
    /// </summary>
    public sealed class ConsumableUsedEvent
    {
        /// <summary>
        /// 事件发生时间（毫秒）
        /// Event time in milliseconds
        /// </summary>
        public int TimeMs { get; init; }

        /// <summary>
        /// 使用消耗品的角色ID
        /// Character ID that used the consumable
        /// </summary>
        public string CharacterId { get; init; } = string.Empty;

        /// <summary>
        /// 使用的物品ID
        /// Used item ID
        /// </summary>
        public string ItemId { get; init; } = string.Empty;

        /// <summary>
        /// 触发的技能ID
        /// Triggered skill ID
        /// </summary>
        public string SkillId { get; init; } = string.Empty;

        /// <summary>
        /// 槽位ID (potion_1, potion_2, food_1, food_2)
        /// Slot ID
        /// </summary>
        public string SlotId { get; init; } = string.Empty;

        /// <summary>
        /// 使用后剩余数量
        /// Remaining quantity after use
        /// </summary>
        public int RemainingCount { get; init; }
    }

    /// <summary>
    /// 消耗品库存耗尽事件 - 战斗中消耗品库存为0时触发
    /// Consumable out of stock event - triggered when consumable inventory is 0 in battle
    /// </summary>
    public sealed class ConsumableOutOfStockEvent
    {
        /// <summary>
        /// 事件发生时间（毫秒）
        /// Event time in milliseconds
        /// </summary>
        public int TimeMs { get; init; }

        /// <summary>
        /// 角色ID
        /// Character ID
        /// </summary>
        public string CharacterId { get; init; } = string.Empty;

        /// <summary>
        /// 耗尽的物品ID
        /// Depleted item ID
        /// </summary>
        public string ItemId { get; init; } = string.Empty;

        /// <summary>
        /// 槽位ID (potion_1, potion_2, food_1, food_2)
        /// Slot ID
        /// </summary>
        public string SlotId { get; init; } = string.Empty;
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