using BlazorIdle.Game.Config;
using BlazorIdle.Shared.Models;
using Microsoft.Extensions.Hosting;

namespace BlazorIdle.Server.Services
{
    /// <summary>
    /// Server-side game configuration provider.
    /// Now loads all configurations from Shared ConfigRepository (embedded resources).
    /// 服务端游戏配置提供者 - 现在从共享的 ConfigRepository（嵌入资源）加载所有配置。
    /// </summary>
    public sealed class GameConfigProvider : IGameConfigProvider
    {
        private readonly List<ProfessionDef> _professions = new();
        private readonly List<MonsterDef> _monsters = new();
        private readonly List<ItemDefinition> _items = new();
        private readonly List<DungeonDef> _dungeons = new();
        private readonly List<BattleScenarioDef> _battleScenarios = new();
        private readonly List<BattleConfigDef> _battleConfigs = new();
        private readonly List<LevelExperienceRequirement> _experienceCurve = new();
        private readonly Dictionary<string, ProfessionAttributeConfig> _professionAttributes = new();
        private int _maxProfessionLevel = 100;
        private volatile bool _loaded;

        public GameConfigProvider(IHostEnvironment _)
        {
            // IHostEnvironment no longer needed for file loading, but kept for DI compatibility
            // IHostEnvironment 不再需要用于文件加载，但保留用于 DI 兼容性
        }

        public IReadOnlyList<ProfessionDef> Professions => _professions;
        public IReadOnlyList<MonsterDef> Monsters => _monsters;
        public IReadOnlyList<ItemDefinition> Items => _items;
        public IReadOnlyList<DungeonDef> Dungeons => _dungeons;
        public IReadOnlyList<BattleScenarioDef> BattleScenarios => _battleScenarios;
        public IReadOnlyList<BattleConfigDef> BattleConfigs => _battleConfigs;
        public IReadOnlyList<LevelExperienceRequirement> ExperienceCurve => _experienceCurve;
        public IReadOnlyDictionary<string, ProfessionAttributeConfig> ProfessionAttributes => _professionAttributes;
        public int MaxProfessionLevel => _maxProfessionLevel;
        public string Version { get; private set; } = "unloaded";

        public async Task EnsureLoadedAsync(CancellationToken ct = default)
        {
            if (_loaded) return;

            // Load all configurations from Shared ConfigRepository (embedded resources)
            // 从共享的 ConfigRepository（嵌入资源）加载所有配置

            // Load professions
            var profs = ConfigRepository.LoadProfessions();

            // Load monsters
            var mons = ConfigRepository.LoadMonsters();

            // Load items
            var items = ConfigRepository.LoadItems();

            // Load dungeons
            var dungeons = ConfigRepository.LoadDungeons();

            // Load battle scenarios
            var battleScenarios = ConfigRepository.LoadBattleScenarios();

            // Load battle configs
            var battleConfigs = ConfigRepository.LoadBattleConfigs();

            // Load experience curve
            var experienceCurve = ConfigRepository.LoadExperienceCurve();

            // Load profession limits
            var professionLimits = ConfigRepository.LoadProfessionLimits();

            _professions.Clear();
            _professions.AddRange(profs.Where(p => !string.IsNullOrWhiteSpace(p.Id)));

            _monsters.Clear();
            _monsters.AddRange(mons.Where(m => !string.IsNullOrWhiteSpace(m.Id)));

            _items.Clear();
            _items.AddRange(items.Where(i => !string.IsNullOrWhiteSpace(i.Id)));

            _dungeons.Clear();
            _dungeons.AddRange(dungeons.Where(d => !string.IsNullOrWhiteSpace(d.Id)));

            _battleScenarios.Clear();
            _battleScenarios.AddRange(battleScenarios.Where(b => !string.IsNullOrWhiteSpace(b.Id)));

            _battleConfigs.Clear();
            _battleConfigs.AddRange(battleConfigs.Where(b => !string.IsNullOrWhiteSpace(b.Id)));

            _experienceCurve.Clear();
            _experienceCurve.AddRange(experienceCurve.OrderBy(e => e.Level));

            // ProfessionAttributes 已废弃，使用新的 ProfessionStatsRepository
            // ProfessionAttributes is deprecated, use new ProfessionStatsRepository
            _professionAttributes.Clear();

            // 设置职业最大等级
            _maxProfessionLevel = professionLimits.MaxProfessionLevel;

            Version = $"p:{_professions.Count}-m:{_monsters.Count}-i:{_items.Count}-d:{_dungeons.Count}-bs:{_battleScenarios.Count}-bc:{_battleConfigs.Count}-exp:{_experienceCurve.Count}-maxLvl:{_maxProfessionLevel}";
            _loaded = true;

            // Note: We no longer read from file system - all configs are loaded from embedded resources
            // 注意：不再从文件系统读取 - 所有配置都从嵌入资源加载
            await Task.CompletedTask; // Keep async signature for backward compatibility
        }

        public ProfessionDef? GetProfession(string id) => _professions.FirstOrDefault(p => p.Id == id);
        public MonsterDef? GetMonster(string id) => _monsters.FirstOrDefault(m => m.Id == id);
        public ItemDefinition? GetItem(string id) => _items.FirstOrDefault(i => i.Id == id);
        public DungeonDef? GetDungeon(string id) => _dungeons.FirstOrDefault(d => d.Id == id);
        public BattleScenarioDef? GetBattleScenario(string id) => _battleScenarios.FirstOrDefault(b => b.Id == id);
        public BattleConfigDef? GetBattleConfig(string id) => _battleConfigs.FirstOrDefault(b => b.Id == id);
        
        public long GetExperienceRequired(int level)
        {
            var requirement = _experienceCurve.FirstOrDefault(e => e.Level == level);
            if (requirement != null) return requirement.ExperienceRequired;
            
            // If level not in curve, extrapolate
            if (level <= 1) return 0;
            var lastLevel = _experienceCurve.LastOrDefault();
            if (lastLevel == null) return 100 * level;
            
            // Simple extrapolation
            return (long)(lastLevel.ExperienceRequired * Math.Pow(1.5, level - lastLevel.Level));
        }

        public bool IsLoaded => _loaded;
    }
}