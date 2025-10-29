using System;
using System.Collections.Generic;

namespace BlazorIdle.Game
{
    /// <summary>
    /// 多单位战斗事件 - 扩展基础战斗事件以支持多单位
    /// Multi-unit combat event - extends base combat event for multi-unit support
    /// </summary>
    public class MultiCombatEvent : CombatEvent
    {
        /// <summary>
        /// 攻击者ID（角色或怪物的唯一标识）
        /// Attacker ID (unique identifier for character or monster)
        /// </summary>
        public string AttackerId { get; set; } = string.Empty;

        /// <summary>
        /// 攻击者名称（用于显示）
        /// Attacker name (for display)
        /// </summary>
        public string AttackerName { get; set; } = string.Empty;

        /// <summary>
        /// 防御者ID（角色或怪物的唯一标识）
        /// Defender ID (unique identifier for character or monster)
        /// </summary>
        public string DefenderId { get; set; } = string.Empty;

        /// <summary>
        /// 防御者名称（用于显示）
        /// Defender name (for display)
        /// </summary>
        public string DefenderName { get; set; } = string.Empty;

        /// <summary>
        /// 是否为AOE攻击
        /// Whether this is an AOE attack
        /// </summary>
        public bool IsAoe { get; set; }

        /// <summary>
        /// 是否击杀了目标
        /// Whether the target was killed
        /// </summary>
        public bool IsKill { get; set; }
    }

    /// <summary>
    /// 队伍状态事件 - 用于通知队伍整体状态变化
    /// Team status event - notifies team overall status changes
    /// </summary>
    public class TeamStatusEvent
    {
        /// <summary>
        /// 事件发生时间（毫秒）
        /// Event time in milliseconds
        /// </summary>
        public int TimeMs { get; set; }

        /// <summary>
        /// 玩家队伍状态
        /// Player team status
        /// </summary>
        public TeamStatus PlayerTeamStatus { get; set; } = new();

        /// <summary>
        /// 敌人队伍状态
        /// Enemy team status
        /// </summary>
        public TeamStatus EnemyTeamStatus { get; set; } = new();

        /// <summary>
        /// 当前战斗状态
        /// Current battle state
        /// </summary>
        public MultiBattleState BattleState { get; set; }
    }

    /// <summary>
    /// 队伍状态信息
    /// Team status information
    /// </summary>
    public class TeamStatus
    {
        /// <summary>
        /// 队伍ID
        /// Team ID
        /// </summary>
        public string TeamId { get; set; } = string.Empty;

        /// <summary>
        /// 队伍名称
        /// Team name
        /// </summary>
        public string TeamName { get; set; } = string.Empty;

        /// <summary>
        /// 各成员的当前生命值
        /// Current HP for each member
        /// </summary>
        public Dictionary<string, int> MemberHp { get; set; } = new();

        /// <summary>
        /// 各成员的最大生命值
        /// Max HP for each member
        /// </summary>
        public Dictionary<string, int> MemberMaxHp { get; set; } = new();

        /// <summary>
        /// 存活成员数量
        /// Number of alive members
        /// </summary>
        public int AliveCount { get; set; }

        /// <summary>
        /// 总成员数量
        /// Total member count
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 队伍是否全部阵亡
        /// Whether the entire team is dead
        /// </summary>
        public bool IsAllDead => AliveCount == 0;

        /// <summary>
        /// 队伍总生命值百分比
        /// Team total HP percentage
        /// </summary>
        public double TotalHpPercent
        {
            get
            {
                if (MemberMaxHp.Values.Sum() == 0) return 0;
                return (double)MemberHp.Values.Sum() / MemberMaxHp.Values.Sum();
            }
        }
    }

    /// <summary>
    /// 多单位战斗状态
    /// Multi-unit battle state
    /// </summary>
    public enum MultiBattleState
    {
        /// <summary>
        /// 未开始
        /// Not started
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// 战斗中
        /// Fighting
        /// </summary>
        Fighting = 1,

        /// <summary>
        /// 玩家队伍阵亡冷却
        /// Player team dead cooldown
        /// </summary>
        PlayerTeamDeadCooldown = 2,

        /// <summary>
        /// 敌人队伍阵亡冷却
        /// Enemy team dead cooldown
        /// </summary>
        EnemyTeamDeadCooldown = 3,

        /// <summary>
        /// 战斗胜利
        /// Battle victory
        /// </summary>
        Victory = 4,

        /// <summary>
        /// 战斗失败
        /// Battle defeat
        /// </summary>
        Defeat = 5,

        /// <summary>
        /// 战斗暂停
        /// Battle paused
        /// </summary>
        Paused = 6
    }

    /// <summary>
    /// 战斗目标选择策略
    /// Battle target selection strategy
    /// </summary>
    public enum TargetStrategy
    {
        /// <summary>
        /// 随机目标
        /// Random target
        /// </summary>
        Random = 1,

        /// <summary>
        /// 最低生命值
        /// Lowest HP
        /// </summary>
        LowestHp = 2,

        /// <summary>
        /// 最低生命值百分比
        /// Lowest HP percentage
        /// </summary>
        LowestHpPercent = 3,

        /// <summary>
        /// 最高生命值
        /// Highest HP
        /// </summary>
        HighestHp = 4,

        /// <summary>
        /// 最近目标（未来实现）
        /// Nearest target (future implementation)
        /// </summary>
        Nearest = 5,

        /// <summary>
        /// 威胁值最高（未来实现）
        /// Highest threat (future implementation)
        /// </summary>
        HighestThreat = 6
    }
}