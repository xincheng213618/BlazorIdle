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
        private readonly List<DungeonDef> _dungeons = new();
        private readonly List<BattleScenarioDef> _battleScenarios = new();
        private readonly List<BattleConfigDef> _battleConfigs = new();
        private readonly List<LevelExperienceRequirement> _experienceCurve = new();
        private readonly Dictionary<string, ProfessionAttributeConfig> _professionAttributes = new();
        private int _maxProfessionLevel = 100;
        private volatile bool _loaded;

        public GameConfigProvider(IHostEnvironment env)
        {
            _env = env;
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

            var contentRoot = _env.ContentRootPath;
            var profPath = Path.Combine(contentRoot, "Config", "professions.json");
            var monPath = Path.Combine(contentRoot, "Config", "monsters.json");
            var dungeonsPath = Path.Combine(contentRoot, "Config", "dungeons.json");
            var battleScenariosPath = Path.Combine(contentRoot, "Config", "battleScenarios.json");
            var battleConfigsPath = Path.Combine(contentRoot, "Config", "battleConfigs.json");
            var experienceCurvePath = Path.Combine(contentRoot, "Config", "experienceCurve.json");
            var professionLimitsPath = Path.Combine(contentRoot, "Config", "professionLimits.json");
            var professionAttributesPath = Path.Combine(contentRoot, "Config", "professionAttributes.json");

            List<ProfessionDef>? profs = null;
            List<MonsterDef>? mons = null;
            List<DungeonDef>? dungeons = null;
            List<BattleScenarioDef>? battleScenarios = null;
            List<BattleConfigDef>? battleConfigs = null;
            List<LevelExperienceRequirement>? experienceCurve = null;
            ExperienceConfig? professionLimits = null;
            Dictionary<string, ProfessionAttributeConfig>? professionAttributes = null;

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

            // 加载副本配置
            try
            {
                if (File.Exists(dungeonsPath))
                {
                    await using var s = File.OpenRead(dungeonsPath);
                    dungeons = await JsonSerializer.DeserializeAsync<List<DungeonDef>>(s, cancellationToken: ct);
                }
            }
            catch { /* ignore to fallback */ }

            // 加载战斗场景配置
            try
            {
                if (File.Exists(battleScenariosPath))
                {
                    await using var s = File.OpenRead(battleScenariosPath);
                    battleScenarios = await JsonSerializer.DeserializeAsync<List<BattleScenarioDef>>(s, cancellationToken: ct);
                }
            }
            catch { /* ignore to fallback */ }

            // 加载战斗配置
            try
            {
                if (File.Exists(battleConfigsPath))
                {
                    await using var s = File.OpenRead(battleConfigsPath);
                    battleConfigs = await JsonSerializer.DeserializeAsync<List<BattleConfigDef>>(s, cancellationToken: ct);
                }
            }
            catch { /* ignore to fallback */ }

            // 加载经验曲线配置
            try
            {
                if (File.Exists(experienceCurvePath))
                {
                    await using var s = File.OpenRead(experienceCurvePath);
                    experienceCurve = await JsonSerializer.DeserializeAsync<List<LevelExperienceRequirement>>(s, cancellationToken: ct);
                }
            }
            catch { /* ignore to fallback */ }

            // 加载职业限制配置
            try
            {
                if (File.Exists(professionLimitsPath))
                {
                    await using var s = File.OpenRead(professionLimitsPath);
                    professionLimits = await JsonSerializer.DeserializeAsync<ExperienceConfig>(s, cancellationToken: ct);
                }
            }
            catch { /* ignore to fallback */ }

            // Phase 2.7: 加载职业属性配置（包含资源配置）
            // Phase 2.7: Load profession attribute configuration (includes resource configuration)
            try
            {
                if (File.Exists(professionAttributesPath))
                {
                    await using var s = File.OpenRead(professionAttributesPath);
                    professionAttributes = await JsonSerializer.DeserializeAsync<Dictionary<string, ProfessionAttributeConfig>>(s, cancellationToken: ct);
                }
            }
            catch { /* ignore to fallback */ }

            // fallback to shared defaults
            profs ??= DefaultGameConfig.DefaultProfessions();
            mons ??= DefaultGameConfig.DefaultMonsters();
            dungeons ??= new List<DungeonDef>();
            battleScenarios ??= new List<BattleScenarioDef>();
            battleConfigs ??= new List<BattleConfigDef>();
            experienceCurve ??= CreateDefaultExperienceCurve();
            professionAttributes ??= new Dictionary<string, ProfessionAttributeConfig>();

            _professions.Clear();
            _professions.AddRange(profs.Where(p => !string.IsNullOrWhiteSpace(p.Id)));

            _monsters.Clear();
            _monsters.AddRange(mons.Where(m => !string.IsNullOrWhiteSpace(m.Id)));

            // Items are now loaded from Shared/Config via ConfigRepository on client side
            // No need to load or populate _items on server side

            _dungeons.Clear();
            _dungeons.AddRange(dungeons.Where(d => !string.IsNullOrWhiteSpace(d.Id)));

            _battleScenarios.Clear();
            _battleScenarios.AddRange(battleScenarios.Where(b => !string.IsNullOrWhiteSpace(b.Id)));

            _battleConfigs.Clear();
            _battleConfigs.AddRange(battleConfigs.Where(b => !string.IsNullOrWhiteSpace(b.Id)));

            _experienceCurve.Clear();
            _experienceCurve.AddRange(experienceCurve.OrderBy(e => e.Level));

            _professionAttributes.Clear();
            foreach (var kvp in professionAttributes)
            {
                _professionAttributes[kvp.Key] = kvp.Value;
            }

            // 设置职业最大等级
            _maxProfessionLevel = professionLimits?.MaxProfessionLevel ?? 100;

            Version = $"p:{_professions.Count}-m:{_monsters.Count}-d:{_dungeons.Count}-bs:{_battleScenarios.Count}-bc:{_battleConfigs.Count}-exp:{_experienceCurve.Count}-maxLvl:{_maxProfessionLevel}-profAttrs:{_professionAttributes.Count}";
            _loaded = true;
        }

        private static List<LevelExperienceRequirement> CreateDefaultExperienceCurve()
        {
            var curve = new List<LevelExperienceRequirement>();
            for (int level = 1; level <= 20; level++)
            {
                long expRequired = level == 1 ? 0 : (long)(100 * Math.Pow(1.5, level - 2));
                curve.Add(new LevelExperienceRequirement { Level = level, ExperienceRequired = expRequired });
            }
            return curve;
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