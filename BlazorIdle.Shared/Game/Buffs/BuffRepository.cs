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
        /// Loads buffs from embedded JSON resource or falls back to default buffs.
        /// </summary>
        private BuffRepository()
        {
            // P0 Fix: Load from JSON file instead of hardcoded initialization
            bool loaded = TryLoadFromEmbeddedJson();
            if (!loaded)
            {
                // Fallback to hardcoded initialization if JSON loading fails
                System.Diagnostics.Debug.WriteLine("[BuffRepository] Failed to load from JSON, using hardcoded defaults");
                InitializeDefaultBuffs();
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
        /// Initializes default buff configurations.
        /// P0 Fix: Simplified - only used as fallback when JSON loading fails.
        /// P1 Fix: Fixed HastePercent to use 10.0 (not 0.10) to match original implementation.
        /// </summary>
        private void InitializeDefaultBuffs()
        {
            // Note: This is a fallback. Normally buffs are loaded from buffs.json
            
            // Warrior buff: Rage Boost (+15% damage, +10 haste, +5 crit for 6s)
            RegisterBuff(new BuffConfig
            {
                Id = "warrior_rage_boost",
                Name = "狂暴",
                Description = "增加攻击力、攻击速度和暴击率",
                Icon = "⚔️",
                Kind = BuffKind.Buff,
                DurationSec = 6.0,
                StackingPolicy = BuffStackingPolicy.Refresh,
                MaxStacks = 1,
                DefaultTarget = Skills.BuffTarget.Self,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.15),
                    BuffEffect.StatAdditive("HastePercent", 10.0),  // P1 Fix: 10.0 not 0.10
                    BuffEffect.StatAdditive("CritChancePercent", 5.0)
                }
            });

            // Mage DoT: Burn (6 damage per second for 6s)
            RegisterBuff(new BuffConfig
            {
                Id = "mage_burn_dot",
                Name = "燃烧",
                Description = "持续造成火焰伤害",
                Icon = "🔥",
                Kind = BuffKind.Debuff,
                DurationSec = 6.0,
                TickIntervalSec = 1.0,
                StackingPolicy = BuffStackingPolicy.Refresh,
                MaxStacks = 1,
                DefaultTarget = Skills.BuffTarget.AllEnemies,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.DamageOverTime(6)
                }
            });

            // Generic damage buff
            RegisterBuff(new BuffConfig
            {
                Id = "damage_boost",
                Name = "伤害增幅",
                Description = "提高攻击伤害",
                Icon = "💪",
                Kind = BuffKind.Buff,
                DurationSec = 10.0,
                StackingPolicy = BuffStackingPolicy.Stack,
                MaxStacks = 3,
                DefaultTarget = Skills.BuffTarget.Self,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.10)
                }
            });

            // Generic HoT: Regeneration
            RegisterBuff(new BuffConfig
            {
                Id = "regeneration",
                Name = "恢复",
                Description = "持续恢复生命值",
                Icon = "💚",
                Kind = BuffKind.Buff,
                DurationSec = 15.0,
                TickIntervalSec = 3.0,
                StackingPolicy = BuffStackingPolicy.Refresh,
                MaxStacks = 1,
                DefaultTarget = Skills.BuffTarget.Self,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.HealOverTime(10)
                }
            });

            // Debuff: Weakness (-20% damage)
            RegisterBuff(new BuffConfig
            {
                Id = "weakness",
                Name = "虚弱",
                Description = "降低攻击伤害",
                Icon = "😰",
                Kind = BuffKind.Debuff,
                DurationSec = 8.0,
                StackingPolicy = BuffStackingPolicy.Refresh,
                MaxStacks = 1,
                DefaultTarget = Skills.BuffTarget.Target,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatReduction("DamagePerAttack", 0.20)
                }
            });

            // Buff: Shield (reduces incoming damage)
            RegisterBuff(new BuffConfig
            {
                Id = "shield",
                Name = "护盾",
                Description = "减少受到的伤害",
                Icon = "🛡️",
                Kind = BuffKind.Buff,
                DurationSec = 12.0,
                StackingPolicy = BuffStackingPolicy.Ignore,
                MaxStacks = 1,
                DefaultTarget = Skills.BuffTarget.Self,
                Effects = new List<BuffEffect>
                {
                    // Note: Damage reduction on self not yet fully implemented in combat
                    // This is a placeholder for future implementation
                    BuffEffect.StatMultiplier("DamageReduction", 0.25)
                }
            });

            // Buff: Haste (increases attack speed)
            RegisterBuff(new BuffConfig
            {
                Id = "haste",
                Name = "急速",
                Description = "提高攻击速度",
                Icon = "⚡",
                Kind = BuffKind.Buff,
                DurationSec = 10.0,
                StackingPolicy = BuffStackingPolicy.Stack,
                MaxStacks = 2,
                DefaultTarget = Skills.BuffTarget.Self,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("HastePercent", 15.0)  // P1 Fix: 15.0 not 0.15
                }
            });

            // Debuff: Poison DoT
            RegisterBuff(new BuffConfig
            {
                Id = "poison",
                Name = "中毒",
                Description = "持续受到毒素伤害",
                Icon = "☠️",
                Kind = BuffKind.Debuff,
                DurationSec = 10.0,
                TickIntervalSec = 2.0,
                StackingPolicy = BuffStackingPolicy.Stack,
                MaxStacks = 3,
                DefaultTarget = Skills.BuffTarget.Target,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.DamageOverTime(4)
                }
            });

            // Buff: Crit Boost
            RegisterBuff(new BuffConfig
            {
                Id = "crit_boost",
                Name = "致命强化",
                Description = "提高暴击率和暴击伤害",
                Icon = "💥",
                Kind = BuffKind.Buff,
                DurationSec = 8.0,
                StackingPolicy = BuffStackingPolicy.Refresh,
                MaxStacks = 1,
                DefaultTarget = Skills.BuffTarget.Self,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("CritChancePercent", 0.10),
                    BuffEffect.StatMultiplier("CritMultiplier", 0.20)
                }
            });

            // Buff: Force Crit (next attack is guaranteed crit)
            RegisterBuff(new BuffConfig
            {
                Id = "force_crit",
                Name = "必定暴击",
                Description = "下次攻击必定暴击",
                Icon = "🎯",
                Kind = BuffKind.Buff,
                DurationSec = 5.0,
                StackingPolicy = BuffStackingPolicy.Ignore,
                MaxStacks = 1,
                DefaultTarget = Skills.BuffTarget.Self,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.ForceCrit()
                }
            });

            // Phase 3: Additional buffs used in SkillRepository
            // Warrior Power Boost (for special pulse)
            RegisterBuff(new BuffConfig
            {
                Id = "warrior_power_boost",
                Name = "战士强化",
                Description = "大幅增加攻击力、攻击速度和暴击率",
                Icon = "⚔️",
                Kind = BuffKind.Buff,
                DurationSec = 6.0,
                StackingPolicy = BuffStackingPolicy.Refresh,
                MaxStacks = 1,
                DefaultTarget = Skills.BuffTarget.Self,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.15),
                    BuffEffect.StatAdditive("HastePercent", 10.0),
                    BuffEffect.StatAdditive("CritChancePercent", 5.0)
                }
            });

            // Instant Heal
            RegisterBuff(new BuffConfig
            {
                Id = "instant_heal",
                Name = "瞬间治疗",
                Description = "立即恢复生命值",
                Icon = "💚",
                Kind = BuffKind.Buff,
                DurationSec = 0.1,
                StackingPolicy = BuffStackingPolicy.Refresh,
                MaxStacks = 1,
                DefaultTarget = Skills.BuffTarget.Self,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.InstantHeal(20)
                }
            });

            // Regeneration HoT (stackable)
            RegisterBuff(new BuffConfig
            {
                Id = "regeneration_hot",
                Name = "持续恢复",
                Description = "持续恢复生命值（可叠加）",
                Icon = "💚",
                Kind = BuffKind.Buff,
                DurationSec = 8.0,
                TickIntervalSec = 2.0,
                StackingPolicy = BuffStackingPolicy.Stack,
                MaxStacks = 3,
                DefaultTarget = Skills.BuffTarget.Self,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.HealOverTime(5)
                }
            });

            // Burning DoT (stackable)
            RegisterBuff(new BuffConfig
            {
                Id = "burning",
                Name = "燃烧",
                Description = "持续造成火焰伤害（可叠加）",
                Icon = "🔥",
                Kind = BuffKind.Debuff,
                DurationSec = 10.0,
                TickIntervalSec = 2.0,
                StackingPolicy = BuffStackingPolicy.Stack,
                MaxStacks = 5,
                DefaultTarget = Skills.BuffTarget.AllEnemies,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.DamageOverTime(8)
                }
            });

            // Weakened (stat reduction on enemies)
            RegisterBuff(new BuffConfig
            {
                Id = "weakened",
                Name = "虚弱",
                Description = "降低敌人伤害",
                Icon = "😰",
                Kind = BuffKind.Debuff,
                DurationSec = 8.0,
                StackingPolicy = BuffStackingPolicy.Refresh,
                MaxStacks = 1,
                DefaultTarget = Skills.BuffTarget.AllEnemies,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatReduction("DamagePerHit", 0.20)
                }
            });
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
                Console.WriteLine($"[BuffRepository] Successfully loaded {_buffs.Count} buffs from JSON");
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
