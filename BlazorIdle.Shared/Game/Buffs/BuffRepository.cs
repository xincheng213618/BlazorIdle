using BlazorIdle.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BlazorIdle.Game.Buffs
{
    /// <summary>
    /// Central repository for buff configurations.
    /// Similar to SkillRepository pattern.
    /// </summary>
    public class BuffRepository
    {
        private readonly Dictionary<string, BuffConfig> _buffs = new Dictionary<string, BuffConfig>();
        private static BuffRepository? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Buff配置文件列表 - 按职业/类型分类存储在 buffs 文件夹中
        /// Buff config files - stored in buffs folder by profession/type
        /// </summary>
        private static readonly string[] BuffConfigFiles = new[]
        {
            "buffs.warrior.json",     // 战士Buff
            "buffs.mage.json",        // 法师Buff
            "buffs.rogue.json",       // 盗贼Buff
            "buffs.ranger.json",      // 游侠Buff
            "buffs.common.json",      // 通用Buff
            "buffs.debuffs.json",     // 通用Debuff
            "buffs.consumable.json",  // 消耗品Buff
            "buffs.monster.json"      // 怪物Buff
        };

        /// <summary>
        /// Gets the singleton instance of BuffRepository.
        /// Thread-safe implementation.
        /// </summary>
        public static BuffRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new BuffRepository();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor for singleton pattern.
        /// Loads buffs from embedded JSON resource.
        /// </summary>
        private BuffRepository()
        {
            // 从分类文件加载
            bool loaded = TryLoadFromCategorizedFiles();
            
            if (!loaded)
            {
                // No fallback - throw exception to make it clear JSON loading failed
                var errorMsg = "[BuffRepository] CRITICAL ERROR: Failed to load buff configurations from embedded resources. " +
                              "Cannot initialize buff system. Check that buff config files are properly embedded in the assembly.";
                Console.WriteLine(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }
            
            // P1 Fix: Validate configuration at startup
            ValidateBuffConfigurations();
        }

        /// <summary>
        /// Creates a new BuffRepository instance (for testing).
        /// </summary>
        public static BuffRepository CreateNew()
        {
            return new BuffRepository();
        }

        /// <summary>
        /// 尝试从分类文件加载Buff配置
        /// Attempts to load buff configurations from categorized files.
        /// </summary>
        /// <returns>True if at least one file was successfully loaded, false otherwise.</returns>
        private bool TryLoadFromCategorizedFiles()
        {
            var loadedFiles = new List<string>();
            var failedFiles = new List<string>();
            int totalBuffs = 0;

            var assembly = typeof(BuffRepository).Assembly;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            foreach (var filename in BuffConfigFiles)
            {
                try
                {
                    var resourceName = $"BlazorIdle.Shared.Config.{filename}";
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    
                    if (stream == null)
                    {
                        failedFiles.Add($"{filename}: 资源未找到");
                        continue;
                    }

                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();
                    
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        failedFiles.Add($"{filename}: 文件为空");
                        continue;
                    }

                    var configs = JsonSerializer.Deserialize<List<BuffConfig>>(json, options);
                    if (configs != null && configs.Count > 0)
                    {
                        foreach (var config in configs)
                        {
                            RegisterBuff(config);
                        }
                        totalBuffs += configs.Count;
                        loadedFiles.Add($"{filename} ({configs.Count} buffs)");
                        Console.WriteLine($"[BuffRepository] 已加载Buff分类: {filename} ({configs.Count} 个Buff)");
                    }
                }
                catch (Exception ex)
                {
                    failedFiles.Add($"{filename}: {ex.Message}");
                    Console.Error.WriteLine($"[BuffRepository] 加载Buff分类失败: {filename} - {ex.Message}");
                }
            }

            if (loadedFiles.Count > 0)
            {
                Console.WriteLine($"[BuffRepository] ✅ 成功从分类文件加载 {totalBuffs} 个Buff，来自 {loadedFiles.Count} 个文件");
                return true;
            }

            if (failedFiles.Count > 0)
            {
                Console.WriteLine($"[BuffRepository] 警告: 所有分类文件加载失败。失败详情: {string.Join("; ", failedFiles)}");
            }

            return false;
        }

        /// <summary>
        /// P1 Fix: Validates all buff configurations at startup.
        /// Logs errors for production visibility.
        /// </summary>
        private void ValidateBuffConfigurations()
        {
            var errors = new List<string>();
            
            foreach (var kvp in _buffs)
            {
                var buff = kvp.Value;
                
                // Validate required fields
                if (string.IsNullOrEmpty(buff.Id))
                {
                    errors.Add($"Buff has empty Id");
                }
                else if (buff.Id != kvp.Key)
                {
                    errors.Add($"Buff Id '{buff.Id}' does not match dictionary key '{kvp.Key}'");
                }
                
                if (string.IsNullOrEmpty(buff.Name))
                {
                    errors.Add($"Buff '{buff.Id}' has empty Name");
                }
                
                if (buff.Effects == null || buff.Effects.Count == 0)
                {
                    errors.Add($"Buff '{buff.Id}' has no effects defined");
                }
                
                // Validate logical constraints
                if (buff.MaxStacks < 0)
                {
                    errors.Add($"Buff '{buff.Id}' has negative MaxStacks: {buff.MaxStacks}");
                }
                
                if (buff.DurationSec.HasValue && buff.DurationSec.Value <= 0)
                {
                    errors.Add($"Buff '{buff.Id}' has non-positive duration: {buff.DurationSec}");
                }
                
                if (buff.TickIntervalSec.HasValue && buff.TickIntervalSec.Value <= 0)
                {
                    errors.Add($"Buff '{buff.Id}' has non-positive tick interval: {buff.TickIntervalSec}");
                }
            }
            
            // P1 Fix: Log to Console for production visibility (not just Debug)
            if (errors.Count > 0)
            {
                Console.WriteLine($"[BuffRepository] WARNING: Found {errors.Count} validation errors:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            else
            {
                Console.WriteLine($"[BuffRepository] Validation passed for {_buffs.Count} buffs");
            }
        }

        /// <summary>
        /// Registers a buff configuration.
        /// </summary>
        /// <param name="config">The buff configuration to register.</param>
        /// <exception cref="ArgumentNullException">If config is null.</exception>
        /// <exception cref="ArgumentException">If config.Id is null or empty.</exception>
        public void RegisterBuff(BuffConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrEmpty(config.Id))
                throw new ArgumentException("BuffConfig.Id cannot be null or empty.", nameof(config));

            if (_buffs.ContainsKey(config.Id))
            {
                System.Diagnostics.Debug.WriteLine($"[BuffRepository] Warning: Overwriting buff '{config.Id}'");
            }

            _buffs[config.Id] = config;
        }

        /// <summary>
        /// Gets a buff configuration by ID.
        /// </summary>
        /// <param name="buffId">The buff ID.</param>
        /// <returns>The buff configuration, or null if not found.</returns>
        public BuffConfig? GetBuffById(string buffId)
        {
            if (string.IsNullOrEmpty(buffId))
                return null;

            return _buffs.TryGetValue(buffId, out var config) ? config : null;
        }

        /// <summary>
        /// Checks if a buff configuration exists.
        /// </summary>
        /// <param name="buffId">The buff ID.</param>
        /// <returns>True if the buff exists, false otherwise.</returns>
        public bool HasBuff(string buffId)
        {
            return !string.IsNullOrEmpty(buffId) && _buffs.ContainsKey(buffId);
        }

        /// <summary>
        /// Gets all registered buff configurations.
        /// </summary>
        /// <returns>A read-only dictionary of all buffs.</returns>
        public IReadOnlyDictionary<string, BuffConfig> GetAllBuffs()
        {
            return _buffs;
        }

        /// <summary>
        /// Gets all buff configurations of a specific kind.
        /// </summary>
        /// <param name="kind">The buff kind to filter by.</param>
        /// <returns>List of matching buff configurations.</returns>
        public List<BuffConfig> GetBuffsByKind(BuffKind kind)
        {
            return _buffs.Values.Where(b => b.Kind == kind).ToList();
        }

        /// <summary>
        /// Clears all registered buffs (for testing).
        /// </summary>
        public void Clear()
        {
            _buffs.Clear();
        }

        /// <summary>
        /// Loads buff configurations from JSON string.
        /// </summary>
        /// <param name="json">JSON string containing buff configurations.</param>
        /// <exception cref="JsonException">If JSON is invalid.</exception>
        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };

            var configs = JsonSerializer.Deserialize<List<BuffConfig>>(json, options);
            if (configs != null)
            {
                foreach (var config in configs)
                {
                    RegisterBuff(config);
                }
            }
        }

        /// <summary>
        /// Gets the count of registered buffs.
        /// </summary>
        public int Count => _buffs.Count;
    }
}
