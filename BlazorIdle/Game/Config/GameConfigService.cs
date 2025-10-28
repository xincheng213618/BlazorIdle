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

            List<ProfessionDef>? profs = null;
            List<MonsterDef>? mons = null;

            try
            {
                profs = await _http.GetFromJsonAsync<List<ProfessionDef>>("config/professions.json", ct);
            }
            catch { /* ignore, will fallback */ }

            try
            {
                mons = await _http.GetFromJsonAsync<List<MonsterDef>>("config/monsters.json", ct);
            }
            catch { /* ignore, will fallback */ }

            // Fallback 到内置默认配置，避免空表导致按钮禁用或 First() 异常
            if (profs is null || profs.Count == 0)
            {
                profs = DefaultProfessions();
            }
            if (mons is null || mons.Count == 0)
            {
                mons = DefaultMonsters();
            }

            _professions.Clear();
            _professions.AddRange(profs.Where(p => !string.IsNullOrWhiteSpace(p.Id)));

            _monsters.Clear();
            _monsters.AddRange(mons.Where(m => !string.IsNullOrWhiteSpace(m.Id)));

            Version = $"p:{_professions.Count}-m:{_monsters.Count}";
            _loaded = true;
        }

        public ProfessionDef? GetProfession(string id)
            => _professions.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

        public MonsterDef? GetMonster(string id)
            => _monsters.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));

        // 内置默认（防止资源路径/部署问题导致无法开始）
        private static List<ProfessionDef> DefaultProfessions() => new()
        {
            new ProfessionDef
            {
                Id = "warrior", Name = "Warrior", Desc="Fallback",
                MaxHp = 280, AttackRateAPS = 1.8, DamagePerAttack = 18,
                HastePercent = 0, SpecialIntervalSec = 6, SpecialDamage = 140,
                CritChancePercent = 10, CritMultiplier = 1.5, VariancePct = 0.04
            }
        };

        private static List<MonsterDef> DefaultMonsters() => new()
        {
            new MonsterDef
            {
                Id = "slime", Name = "Green Slime", Desc="Fallback", Level=1,
                MaxHp = 220, AttackIntervalSec = 2.0, DamagePerHit = 8, VariancePct = 0.05
            }
        };
    }
}