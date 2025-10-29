using System.Text.Json;
using BlazorIdle.Game.Config;
using BlazorIdle.Shared.Models;
using Microsoft.Extensions.Hosting;

namespace BlazorIdle.Server.Services
{
    public sealed class GameConfigProvider : IGameConfigProvider
    {
        private readonly IHostEnvironment _env;
        private readonly List<ProfessionDef> _professions = new();
        private readonly List<MonsterDef> _monsters = new();
        private readonly List<ItemDefinition> _items = new();
        private volatile bool _loaded;

        public GameConfigProvider(IHostEnvironment env)
        {
            _env = env;
        }

        public IReadOnlyList<ProfessionDef> Professions => _professions;
        public IReadOnlyList<MonsterDef> Monsters => _monsters;
        public IReadOnlyList<ItemDefinition> Items => _items;
        public string Version { get; private set; } = "unloaded";

        public async Task EnsureLoadedAsync(CancellationToken ct = default)
        {
            if (_loaded) return;

            var contentRoot = _env.ContentRootPath;
            var profPath = Path.Combine(contentRoot, "Config", "professions.json");
            var monPath = Path.Combine(contentRoot, "Config", "monsters.json");
            var itemsPath = Path.Combine(contentRoot, "Config", "items.json");

            List<ProfessionDef>? profs = null;
            List<MonsterDef>? mons = null;
            List<ItemDefinition>? items = null;

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

            // 加载物品配置
            // Load items configuration
            try
            {
                if (File.Exists(itemsPath))
                {
                    await using var s = File.OpenRead(itemsPath);
                    items = await JsonSerializer.DeserializeAsync<List<ItemDefinition>>(s, cancellationToken: ct);
                }
            }
            catch { /* ignore to fallback */ }

            // fallback to shared defaults
            profs ??= DefaultGameConfig.DefaultProfessions();
            mons ??= DefaultGameConfig.DefaultMonsters();
            items ??= new List<ItemDefinition>();

            _professions.Clear();
            _professions.AddRange(profs.Where(p => !string.IsNullOrWhiteSpace(p.Id)));

            _monsters.Clear();
            _monsters.AddRange(mons.Where(m => !string.IsNullOrWhiteSpace(m.Id)));

            _items.Clear();
            _items.AddRange(items.Where(i => !string.IsNullOrWhiteSpace(i.Id)));

            Version = $"p:{_professions.Count}-m:{_monsters.Count}-i:{_items.Count}";
            _loaded = true;
        }
    }
}