using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// Repository of skill definitions (Step 2 Phase 1).
    /// Provides centralized skill configuration with JSON loading support.
    /// </summary>
    public sealed class SkillRepository
    {
        private readonly Dictionary<string, SkillDef> _skills = new();

        public SkillRepository()
        {
            InitializeDefaultSkills();
            
            // Try to load from embedded JSON
            bool loaded = TryLoadFromEmbeddedJson();
            if (loaded)
            {
                // Validate configuration after loading
                ValidateSkillConfigurations();
            }
        }

        /// <summary>
        /// Get a skill definition by ID.
        /// </summary>
        public SkillDef? GetSkill(string skillId)
        {
            return _skills.GetValueOrDefault(skillId);
        }

        /// <summary>
        /// Register a skill definition.
        /// Phase 7.10: Added validation for SkillDef.Id consistency.
        /// </summary>
        public void RegisterSkill(SkillDef skillDef)
        {
            if (skillDef == null)
                throw new System.ArgumentNullException(nameof(skillDef));
            
            if (string.IsNullOrEmpty(skillDef.Id))
                throw new System.ArgumentException("SkillDef.Id cannot be null or empty", nameof(skillDef));
            
            // Phase 7.10: Warn if skill is being overwritten
            if (_skills.ContainsKey(skillDef.Id))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SkillRepository] Warning: Overwriting existing skill definition for Id='{skillDef.Id}'");
            }
            
            _skills[skillDef.Id] = skillDef;
        }

        /// <summary>
        /// Initialize default skills (basic attack, special, enemy attack).
        /// </summary>
        private void InitializeDefaultSkills()
        {
            // Basic attack - no special effects yet
            // Phase 3+: Added targetPolicy for target selection integration
            RegisterSkill(new SkillDef
            {
                Id = SkillIds.AttackBasic,
                DamageMultiplier = 1.0,
                CanCrit = true,
                AlwaysHits = true,
                TargetPolicy = "CurrentTarget" // Default: single target
            });

            // Special pulse - Phase 3: Migrated to use BuffConfigId
            // Tests various buff types: self-buffs, instant heal, DoT, HoT, and debuffs
            // Phase 3+: Added targetPolicy for target selection integration
            RegisterSkill(new SkillDef
            {
                Id = SkillIds.SpecialPulse,
                DamageMultiplier = 1.0,
                CanCrit = true,
                AlwaysHits = true,
                IsAoe = true,
                TargetPolicy = "EnemiesAll", // Default: AoE to all enemies
                OnCastBuffs = new List<BuffOperation>
                {
                    // 1. Self-buff: Damage/Haste/Crit boost (warrior power boost)
                    BuffOperation.ApplyByConfigId("warrior_power_boost"),
                    
                    // 2. Instant Heal: Heal self immediately
                    BuffOperation.ApplyByConfigId("instant_heal"),
                    
                    // 3. HoT (Heal over Time): Regeneration buff on self
                    BuffOperation.ApplyByConfigId("regeneration_hot"),
                    
                    // 4. DoT (Damage over Time): Burn debuff on enemies
                    BuffOperation.ApplyByConfigId("burning"),
                    
                    // 5. Stat Reduction Debuff: Weaken enemy damage
                    BuffOperation.ApplyByConfigId("weakened")
                }
            });

            // Enemy basic attack
            RegisterSkill(new SkillDef
            {
                Id = SkillIds.EnemyAttackBasic,
                DamageMultiplier = 1.0,
                CanCrit = false,
                AlwaysHits = true,
                TargetPolicy = "CurrentTarget" // Default: single target
            });
        }

        /// <summary>
        /// Step 2 Phase 1: Attempts to load skill configurations from embedded JSON resource.
        /// </summary>
        /// <returns>True if successfully loaded, false otherwise.</returns>
        private bool TryLoadFromEmbeddedJson()
        {
            try
            {
                var assembly = typeof(SkillRepository).Assembly;
                var resourceName = "BlazorIdle.Shared.Config.skills.json";
                
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        // Try alternative resource name format
                        var resources = assembly.GetManifestResourceNames();
                        resourceName = resources.FirstOrDefault(r => r.EndsWith("skills.json"));
                        
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
                        
                        Console.WriteLine($"[SkillRepository] Warning: Could not find embedded resource 'skills.json'. Available resources: {string.Join(", ", resources)}");
                        return false;
                    }
                    
                    return LoadFromStream(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SkillRepository] Error loading from embedded JSON: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper method to load skills from a stream.
        /// </summary>
        private bool LoadFromStream(System.IO.Stream stream)
        {
            using (var reader = new System.IO.StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                LoadFromJson(json);
                Console.WriteLine($"[SkillRepository] ✅ Successfully loaded {_skills.Count} skills from skills.json");
                return true;
            }
        }

        /// <summary>
        /// Loads skill configurations from JSON string.
        /// </summary>
        /// <param name="json">JSON string containing skill configurations.</param>
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

            var skills = JsonSerializer.Deserialize<List<SkillDef>>(json, options);
            if (skills != null)
            {
                foreach (var skill in skills)
                {
                    RegisterSkill(skill);
                }
            }
        }

        /// <summary>
        /// Step 2 Phase 1: Validates all skill configurations at startup.
        /// Logs errors for production visibility.
        /// </summary>
        private void ValidateSkillConfigurations()
        {
            var errors = new List<string>();
            
            foreach (var kvp in _skills)
            {
                var skill = kvp.Value;
                
                // Validate required fields
                if (string.IsNullOrEmpty(skill.Id))
                {
                    errors.Add($"Skill has empty Id");
                }
                else if (skill.Id != kvp.Key)
                {
                    errors.Add($"Skill Id '{skill.Id}' does not match dictionary key '{kvp.Key}'");
                }
                
                // Validate buff references in OnCastBuffs
                if (skill.OnCastBuffs != null && skill.OnCastBuffs.Count > 0)
                {
                    foreach (var buffOp in skill.OnCastBuffs)
                    {
                        if (!string.IsNullOrEmpty(buffOp.BuffConfigId))
                        {
                            // Note: We can't validate buff existence here because BuffRepository
                            // might not be initialized yet. This will be validated at runtime.
                        }
                    }
                }
                
                // Validate buff references in OnHitBuffs
                if (skill.OnHitBuffs != null && skill.OnHitBuffs.Count > 0)
                {
                    foreach (var buffOp in skill.OnHitBuffs)
                    {
                        if (!string.IsNullOrEmpty(buffOp.BuffConfigId))
                        {
                            // Note: We can't validate buff existence here
                        }
                    }
                }
                
                // Validate fireSkillId references in triggers
                if (skill.Triggers != null && skill.Triggers.Count > 0)
                {
                    foreach (var trigger in skill.Triggers)
                    {
                        if (!string.IsNullOrEmpty(trigger.FireSkillId))
                        {
                            // We can validate this since we're checking within the same repository
                            if (!_skills.ContainsKey(trigger.FireSkillId))
                            {
                                errors.Add($"Skill '{skill.Id}' trigger references non-existent skill '{trigger.FireSkillId}'");
                            }
                        }
                    }
                }
                
                // Validate logical constraints
                if (skill.CooldownSec < 0)
                {
                    errors.Add($"Skill '{skill.Id}' has negative cooldown: {skill.CooldownSec}");
                }
                
                if (skill.CastTimeSec < 0)
                {
                    errors.Add($"Skill '{skill.Id}' has negative cast time: {skill.CastTimeSec}");
                }
            }
            
            // Log validation results
            if (errors.Count > 0)
            {
                Console.WriteLine($"[SkillRepository] WARNING: Found {errors.Count} validation errors:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
            else
            {
                Console.WriteLine($"[SkillRepository] Validation passed for {_skills.Count} skills");
            }
        }

        /// <summary>
        /// Gets all skills that belong to a specific profession.
        /// </summary>
        /// <param name="professionId">The profession ID (e.g., "warrior", "mage")</param>
        /// <returns>List of skills for the specified profession</returns>
        public List<SkillDef> GetSkillsByProfession(string professionId)
        {
            return _skills.Values
                .Where(s => s.AllowedProfessions == null || 
                           s.AllowedProfessions.Count == 0 || 
                           s.AllowedProfessions.Contains(professionId))
                .ToList();
        }

        /// <summary>
        /// Gets a skill by its ID.
        /// </summary>
        /// <param name="skillId">The skill ID</param>
        /// <returns>The skill definition, or null if not found</returns>
        public SkillDef? GetSkillById(string skillId)
        {
            return GetSkill(skillId);
        }

        /// <summary>
        /// Gets the count of registered skills.
        /// </summary>
        public int Count => _skills.Count;
    }
}
