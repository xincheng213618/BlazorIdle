using System;
using System.Collections.Generic;
using BlazorIdle.Game.Config;

namespace BlazorIdle.Game
{
    /// <summary>
    /// 副本状态枚举
    /// Dungeon state enumeration
    /// </summary>
    public enum DungeonState
    {
        NotStarted,       // 未开始
        Preparing,        // 准备中
        WaveStartDelay,   // 波次开始延迟
        Fighting,         // 战斗中
        WaveEndDelay,     // 波次结束延迟
        CompletionDelay,  // 完成延迟（等待重新开始）
        Completed,        // 已完成
        Failed,          // 失败
        Stopped          // 已停止
    }

    /// <summary>
    /// 波次变更类型
    /// Wave change type
    /// </summary>
    public enum WaveChangeType
    {
        Preparing,   // 准备中
        Started,     // 已开始
        Completed    // 已完成
    }

    /// <summary>
    /// 副本进度事件
    /// Dungeon progress event
    /// </summary>
    public class DungeonProgressEvent
    {
        public string DungeonId { get; set; } = string.Empty;
        public string DungeonName { get; set; } = string.Empty;
        public int CurrentWave { get; set; }
        public int TotalWaves { get; set; }
        public DungeonState State { get; set; }
        public int ElapsedMs { get; set; }
        public int CompletionCount { get; set; }
        public bool AutoRepeatEnabled { get; set; }
    }

    /// <summary>
    /// 副本波次事件
    /// Dungeon wave event
    /// </summary>
    public class DungeonWaveEvent
    {
        public int WaveNumber { get; set; }
        public string WaveName { get; set; } = string.Empty;
        public WaveType WaveType { get; set; }
        public WaveChangeType ChangeType { get; set; }
        public int TimeMs { get; set; }
    }

    /// <summary>
    /// 副本完成事件
    /// Dungeon complete event
    /// </summary>
    public class DungeonCompleteEvent
    {
        public string DungeonId { get; set; } = string.Empty;
        public string DungeonName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int CompletionTimeMs { get; set; }
        public int CompletionCount { get; set; }
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public Dictionary<string, int> TotalLoot { get; set; } = new();
    }

    /// <summary>
    /// 副本统计事件
    /// Dungeon stats event
    /// </summary>
    public class DungeonStatsEvent
    {
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public Dictionary<string, int> TotalLoot { get; set; } = new();
        public int CompletionCount { get; set; }
        public int ElapsedMs { get; set; }
    }

    /// <summary>
    /// 副本快照
    /// Dungeon snapshot
    /// </summary>
    public class DungeonSnapshot
    {
        public string DungeonId { get; set; } = string.Empty;
        public string DungeonName { get; set; } = string.Empty;
        public DungeonState State { get; set; }
        public int CurrentWaveIndex { get; set; }
        public int TotalWaves { get; set; }
        public int ElapsedMs { get; set; }
        public int CompletionCount { get; set; }
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public Dictionary<string, int> TotalLoot { get; set; } = new();
        public bool AutoRepeatEnabled { get; set; }
        public MultiBattleSnapshot? BattleSnapshot { get; set; }
        public BattleTeam<Enemy>? CurrentEnemyTeam { get; set; }
    }
}