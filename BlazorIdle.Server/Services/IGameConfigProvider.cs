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

        /// <summary>
        /// 战斗配置定义列表 - 可复用的战斗参数配置（内部配置）
        /// Battle configuration definitions list - reusable battle parameter configurations (internal config)
        /// </summary>
        IReadOnlyList<BattleConfigDef> BattleConfigs { get; }

        ProfessionDef? GetProfession(string id);
        MonsterDef? GetMonster(string id);
        ItemDefinition? GetItem(string id);
        DungeonDef? GetDungeon(string id);
        BattleScenarioDef? GetBattleScenario(string id);
        BattleConfigDef? GetBattleConfig(string id);

        /// <summary>
        /// 经验曲线 - 等级与所需经验的映射
        /// Experience curve - mapping of levels to required experience
        /// </summary>
        IReadOnlyList<BlazorIdle.Game.Config.LevelExperienceRequirement> ExperienceCurve { get; }

        /// <summary>
        /// 获取指定等级所需的经验值
        /// Get experience required for a specific level
        /// </summary>
        long GetExperienceRequired(int level);

        /// <summary>
        /// 职业最大等级限制
        /// Maximum profession level limit
        /// </summary>
        int MaxProfessionLevel { get; }

        string Version { get; }
        bool IsLoaded { get; }
    }
}