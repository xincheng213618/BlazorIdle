using System.Net.Http.Json;
using BlazorIdle.Shared.Models;
using BlazorIdle.Configuration;

namespace BlazorIdle.Game.Config
{
    public sealed class GameConfigService : IGameConfigService
    {
        private readonly HttpClient _http;
        private readonly ApiConfiguration _apiConfig;
        private bool _loaded;
        private readonly List<ProfessionDef> _professions = new();
        private readonly List<MonsterDef> _monsters = new();
        private readonly List<ItemDefinition> _items = new();
        private readonly List<DungeonDef> _dungeons = new();
        private readonly List<BattleScenarioDef> _battleScenarios = new();
        private readonly List<BattleConfigDef> _battleConfigs = new();
        private readonly Dictionary<string, ProfessionAttributeConfig> _professionAttributes = new();
        private readonly List<LevelExperienceRequirement> _experienceCurve = new();
        private readonly List<ShopItemConfig> _consumableShopItems = new();
        private readonly List<ShopItemConfig> _potionShopItems = new();
        private readonly List<ShopItemConfig> _foodShopItems = new();
        private int _maxProfessionLevel = 100;

        public GameConfigService(HttpClient http, ApiConfiguration apiConfig)
        {
            _http = http;
            _apiConfig = apiConfig;
        }

        public IReadOnlyList<ProfessionDef> Professions => _professions;
        public IReadOnlyList<MonsterDef> Monsters => _monsters;
        public IReadOnlyList<ItemDefinition> Items => _items;
        public IReadOnlyList<DungeonDef> Dungeons => _dungeons;
        public IReadOnlyList<BattleScenarioDef> BattleScenarios => _battleScenarios;
        public IReadOnlyList<BattleConfigDef> BattleConfigs => _battleConfigs;
        public IReadOnlyDictionary<string, ProfessionAttributeConfig> ProfessionAttributes => _professionAttributes;
        public int MaxProfessionLevel => _maxProfessionLevel;
        public IReadOnlyList<ShopItemConfig> ConsumableShopItems => _consumableShopItems;
        public IReadOnlyList<ShopItemConfig> PotionShopItems => _potionShopItems;
        public IReadOnlyList<ShopItemConfig> FoodShopItems => _foodShopItems;

        public string Version { get; private set; } = "unloaded";
        public bool IsLoaded => _loaded;

        public async Task EnsureLoadedAsync(CancellationToken ct = default)
        {
            if (_loaded) return;

            // Load all configurations from embedded resources (ConfigRepository)
            // 从嵌入资源中加载所有配置（不再依赖API）

            // Load items
            var items = ConfigRepository.LoadItems();

            // Load professions
            var profs = ConfigRepository.LoadProfessions();

            // Load monsters
            var mons = ConfigRepository.LoadMonsters();

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

            // Load shop configurations
            // 加载商店配置
            var consumableShop = ConfigRepository.LoadConsumableShop();
            var potionShop = ConfigRepository.LoadPotionShop();
            var foodShop = ConfigRepository.LoadFoodShop();

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

            _professionAttributes.Clear();
            // ProfessionAttributes 已废弃，使用新的 ProfessionStatsRepository
            // ProfessionAttributes is deprecated, use new ProfessionStatsRepository
            _professionAttributes.Clear();

            _experienceCurve.Clear();
            _experienceCurve.AddRange(experienceCurve.OrderBy(e => e.Level));

            _maxProfessionLevel = professionLimits.MaxProfessionLevel;

            _consumableShopItems.Clear();
            _consumableShopItems.AddRange(consumableShop.Where(s => !string.IsNullOrWhiteSpace(s.ItemId)));

            _potionShopItems.Clear();
            _potionShopItems.AddRange(potionShop.Where(s => !string.IsNullOrWhiteSpace(s.ItemId)));

            _foodShopItems.Clear();
            _foodShopItems.AddRange(foodShop.Where(s => !string.IsNullOrWhiteSpace(s.ItemId)));

            Version = $"p:{_professions.Count}-m:{_monsters.Count}-i:{_items.Count}-d:{_dungeons.Count}-bs:{_battleScenarios.Count}-bc:{_battleConfigs.Count}-exp:{_experienceCurve.Count}-maxLvl:{_maxProfessionLevel}-cs:{_consumableShopItems.Count}-ps:{_potionShopItems.Count}-fs:{_foodShopItems.Count}";
            _loaded = true;

            // Note: We no longer fetch from API - all configs are loaded from embedded resources
            // 注意：不再从API获取配置 - 所有配置都从嵌入资源加载
            await Task.CompletedTask; // Keep async signature for backward compatibility
        }

        public ProfessionDef? GetProfession(string id)
            => _professions.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

        public MonsterDef? GetMonster(string id)
            => _monsters.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));

        public ItemDefinition? GetItem(string id)
            => _items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));

        public DungeonDef? GetDungeon(string id)
            => _dungeons.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));

        public BattleScenarioDef? GetBattleScenario(string id)
            => _battleScenarios.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));

        public BattleConfigDef? GetBattleConfig(string id)
            => _battleConfigs.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}