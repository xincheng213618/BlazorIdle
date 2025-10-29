using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlazorIdle.Game.Config;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Server.Services
{
    public interface IGameConfigProvider
    {
        Task EnsureLoadedAsync(CancellationToken ct = default);
        IReadOnlyList<ProfessionDef> Professions { get; }
        IReadOnlyList<MonsterDef> Monsters { get; }
        
        /// <summary>
        /// 物品定义列表 - 游戏中所有可用的物品
        /// Item definitions list - all available items in the game
        /// </summary>
        IReadOnlyList<ItemDefinition> Items { get; }
        
        string Version { get; }
    }
}