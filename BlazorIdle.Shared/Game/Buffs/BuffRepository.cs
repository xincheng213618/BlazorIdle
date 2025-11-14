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
            // Load from JSON file - no fallback to verify JSON is being used
            bool loaded = TryLoadFromEmbeddedJson();
            if (!loaded)
            {
                // No fallback - throw exception to make it clear JSON loading failed
                var errorMsg = "[BuffRepository] CRITICAL ERROR: Failed to load buffs.json from embedded resources. " +
                              "Cannot initialize buff system. Check that buffs.json is properly embedded in the assembly.";
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
        /// P0 Fix: Attempts to load buff configurations from embedded JSON resource.
        /// </summary>
        /// <returns>True if successfully loaded, false otherwise.</returns>
        private bool TryLoadFromEmbeddedJson()
        {
            try
            {
                var assembly = typeof(BuffRepository).Assembly;
                var resourceName = "BlazorIdle.Shared.Config.buffs.json";
                
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        // Try alternative resource name format
                        var resources = assembly.GetManifestResourceNames();
                        resourceName = resources.FirstOrDefault(r => r.EndsWith("buffs.json"));
                        
                        if (resourceName != null)
                        {
                            stream?.Dispose();
                            using (var alternativeStream = assembly.GetManifestResourceStream(resourceName))
                            {
                                if (alternativeStream != null)
                                {
                                    return LoadFromStream(alternativeStream);
                                }
                            }
                        }
                        
                        Console.WriteLine($"[BuffRepository] Warning: Could not find embedded resource 'buffs.json'. Available resources: {string.Join(", ", resources)}");
                        return false;
                    }
                    
                    return LoadFromStream(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BuffRepository] Error loading from embedded JSON: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper method to load buffs from a stream.
        /// </summary>
        private bool LoadFromStream(System.IO.Stream stream)
        {
            using (var reader = new System.IO.StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                LoadFromJson(json);
                Console.WriteLine($"[BuffRepository] ✅ Successfully loaded {_buffs.Count} buffs from buffs.json");
                
                // Log a sample buff to confirm values are from JSON
                var sampleBuff = GetBuffById("warrior_power_boost");
                if (sampleBuff != null)
                {
                    var hasteEffect = sampleBuff.Effects.FirstOrDefault(e => e.Target == "HastePercent");
                    if (hasteEffect != null)
                    {
                        Console.WriteLine($"[BuffRepository] Sample verification - warrior_power_boost HastePercent = {hasteEffect.Value}");
                    }
                }
                
                return true;
            }
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
