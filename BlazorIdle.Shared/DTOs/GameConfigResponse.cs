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
    }
}