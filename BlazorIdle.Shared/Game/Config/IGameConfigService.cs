using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Config
{
    public interface IGameConfigService
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
        /// 战斗配置定义列表 - 可复用的战斗参数配置
        /// Battle configuration definitions list - reusable battle parameter configurations
        /// </summary>
        IReadOnlyList<BattleConfigDef> BattleConfigs { get; }

        ProfessionDef? GetProfession(string id);
        MonsterDef? GetMonster(string id);

        /// <summary>
        /// 获取指定ID的物品定义
        /// Get item definition by ID
        /// </summary>
        ItemDefinition? GetItem(string id);

        /// <summary>
        /// 获取指定ID的副本定义
        /// Get dungeon definition by ID
        /// </summary>
        DungeonDef? GetDungeon(string id);

        /// <summary>
        /// 获取指定ID的战斗场景定义
        /// Get battle scenario definition by ID
        /// </summary>
        BattleScenarioDef? GetBattleScenario(string id);

        /// <summary>
        /// 获取指定ID的战斗配置定义
        /// Get battle configuration definition by ID
        /// </summary>
        BattleConfigDef? GetBattleConfig(string id);

        /// <summary>
        /// 职业最大等级限制
        /// Maximum profession level limit
        /// </summary>
        int MaxProfessionLevel { get; }

        string Version { get; }
        bool IsLoaded { get; }
    }
}
