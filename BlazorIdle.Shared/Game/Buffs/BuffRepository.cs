using BlazorIdle.Models;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Gets the singleton instance of BuffRepository.
        /// </summary>
        public static BuffRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BuffRepository();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private BuffRepository()
        {
            InitializeDefaultBuffs();
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
        /// These are examples that can be used for testing or as a starting point.
        /// </summary>
        private void InitializeDefaultBuffs()
        {
            // Warrior buff: Rage Boost (+15% damage, +10% haste, +5% crit for 6s)
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
                    BuffEffect.StatAdditive("HastePercent", 0.10),
                    BuffEffect.StatAdditive("CritChancePercent", 0.05)
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
                    BuffEffect.StatAdditive("HastePercent", 0.15)
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
