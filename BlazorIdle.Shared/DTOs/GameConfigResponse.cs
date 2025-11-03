using System.Collections.Generic;
using BlazorIdle.Game.Config;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Shared.DTOs
{
    public sealed class GameConfigResponse
    {
        public string Version { get; set; } = "unloaded";
        public List<ProfessionDef> Professions { get; set; } = new();
        public List<MonsterDef> Monsters { get; set; } = new();

        /// <summary>
        /// 物品定义列表 - 游戏中所有可用的物品
        /// Item definitions list - all available items in the game
        /// </summary>
        public List<ItemDefinition> Items { get; set; } = new();

        /// <summary>
        /// 副本定义列表 - 游戏中所有可用的副本
        /// Dungeon definitions list - all available dungeons in the game
        /// </summary>
        public List<DungeonDef> Dungeons { get; set; } = new();

        /// <summary>
        /// 战斗场景定义列表 - 用于单次战斗的预设场景
        /// Battle scenario definitions list - preset scenarios for single battles
        /// </summary>
        public List<BattleScenarioDef> BattleScenarios { get; set; } = new();

        /// <summary>
        /// 战斗配置定义列表 - 可复用的战斗参数配置（内部配置，不向用户公开）
        /// Battle configuration definitions list - reusable battle parameter configurations (internal config, not exposed to users)
        /// </summary>
        public List<BattleConfigDef> BattleConfigs { get; set; } = new();

        /// <summary>
        /// 职业最大等级限制
        /// Maximum profession level limit
        /// </summary>
        public int MaxProfessionLevel { get; set; } = 100;
    }
}