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

        public string Version { get; private set; } = "unloaded";
        public bool IsLoaded => _loaded;

        public async Task EnsureLoadedAsync(CancellationToken ct = default)
        {
            if (_loaded) return;

            Shared.DTOs.GameConfigResponse? response = null;
            try
            {
                response = await _http.GetFromJsonAsync<Shared.DTOs.GameConfigResponse>($"{_apiConfig.GameConfigApiUrl}/all", ct);
            }
            catch
            {
                // ignore, will fallback
            }

            var profs = response?.Professions ?? DefaultGameConfig.DefaultProfessions();
            var mons = response?.Monsters ?? DefaultGameConfig.DefaultMonsters();
            var items = response?.Items ?? new List<ItemDefinition>();
            var dungeons = response?.Dungeons ?? new List<DungeonDef>();
            var battleScenarios = response?.BattleScenarios ?? new List<BattleScenarioDef>();

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

            Version = response?.Version ?? $"p:{_professions.Count}-m:{_monsters.Count}-i:{_items.Count}-d:{_dungeons.Count}-b:{_battleScenarios.Count}";
            _loaded = true;
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
    }
}