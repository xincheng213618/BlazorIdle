using System.Text.Json;
using BlazorIdle.Game.Config;
using Microsoft.Extensions.Hosting;

namespace BlazorIdle.Server.Services
{
    public sealed class GameConfigProvider : IGameConfigProvider
    {
        private readonly IHostEnvironment _env;
        private readonly List<ProfessionDef> _professions = new();
        private readonly List<MonsterDef> _monsters = new();
        private volatile bool _loaded;

        public GameConfigProvider(IHostEnvironment env)
        {
            _env = env;
        }

        public IReadOnlyList<ProfessionDef> Professions => _professions;
        public IReadOnlyList<MonsterDef> Monsters => _monsters;
        public string Version { get; private set; } = "unloaded";

        public async Task EnsureLoadedAsync(CancellationToken ct = default)
        {
            if (_loaded) return;

            var contentRoot = _env.ContentRootPath;
            var profPath = Path.Combine(contentRoot, "Config", "professions.json");
            var monPath = Path.Combine(contentRoot, "Config", "monsters.json");

            List<ProfessionDef>? profs = null;
            List<MonsterDef>? mons = null;

            try
            {
                if (File.Exists(profPath))
                {
                    await using var s = File.OpenRead(profPath);
                    profs = await JsonSerializer.DeserializeAsync<List<ProfessionDef>>(s, cancellationToken: ct);
                }
            }
            catch { /* ignore to fallback */ }

            try
            {
                if (File.Exists(monPath))
                {
                    await using var s = File.OpenRead(monPath);
                    mons = await JsonSerializer.DeserializeAsync<List<MonsterDef>>(s, cancellationToken: ct);
                }
            }
            catch { /* ignore to fallback */ }

            // fallback to shared defaults
            profs ??= DefaultGameConfig.DefaultProfessions();
            mons ??= DefaultGameConfig.DefaultMonsters();

            _professions.Clear();
            _professions.AddRange(profs.Where(p => !string.IsNullOrWhiteSpace(p.Id)));

            _monsters.Clear();
            _monsters.AddRange(mons.Where(m => !string.IsNullOrWhiteSpace(m.Id)));

            Version = $"p:{_professions.Count}-m:{_monsters.Count}";
            _loaded = true;
        }
    }
}