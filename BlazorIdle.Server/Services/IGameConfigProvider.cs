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

        /// <summary>
        /// 副本定义列表 - 游戏中所有可用的副本
        /// Dungeon definitions list - all available dungeons in the game
        /// </summary>
        IReadOnlyList<DungeonDef> Dungeons { get; }

        /// <summary>
        /// 战斗场景定义列表 - 用于单次战斗的预设场景
        /// Battle scenario definitions list - preset scenarios for single battles
        /// </summary>
        IReadOnlyList<BattleScenarioDef> BattleScenarios { get; }

        string Version { get; }
    }
}