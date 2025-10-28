using System.Net.Http.Json;

namespace BlazorIdle.Game.Config
{
    public interface IGameConfigService
    {
        Task EnsureLoadedAsync(CancellationToken ct = default);
        IReadOnlyList<ProfessionDef> Professions { get; }
        IReadOnlyList<MonsterDef> Monsters { get; }
        ProfessionDef? GetProfession(string id);
        MonsterDef? GetMonster(string id);
        string Version { get; }
        bool IsLoaded { get; }
    }

    public sealed class GameConfigService : IGameConfigService
    {
        private readonly HttpClient _http;
        private bool _loaded;
        private readonly List<ProfessionDef> _professions = new();
        private readonly List<MonsterDef> _monsters = new();

        private const string ApiBaseUrl = "https://localhost:7056/api/game-config";

        public GameConfigService(HttpClient http)
        {
            _http = http;
        }

        public IReadOnlyList<ProfessionDef> Professions => _professions;
        public IReadOnlyList<MonsterDef> Monsters => _monsters;

        public string Version { get; private set; } = "unloaded";
        public bool IsLoaded => _loaded;

        public async Task EnsureLoadedAsync(CancellationToken ct = default)
        {
            if (_loaded) return;

            Shared.DTOs.GameConfigResponse? response = null;
            try
            {
                response = await _http.GetFromJsonAsync<Shared.DTOs.GameConfigResponse>($"{ApiBaseUrl}/all", ct);
            }
            catch
            {
                // ignore, will fallback
            }

            var profs = response?.Professions ?? DefaultGameConfig.DefaultProfessions();
            var mons = response?.Monsters ?? DefaultGameConfig.DefaultMonsters();

            _professions.Clear();
            _professions.AddRange(profs.Where(p => !string.IsNullOrWhiteSpace(p.Id)));

            _monsters.Clear();
            _monsters.AddRange(mons.Where(m => !string.IsNullOrWhiteSpace(m.Id)));

            Version = response?.Version ?? $"p:{_professions.Count}-m:{_monsters.Count}";
            _loaded = true;
        }

        public ProfessionDef? GetProfession(string id)
            => _professions.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

        public MonsterDef? GetMonster(string id)
            => _monsters.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}