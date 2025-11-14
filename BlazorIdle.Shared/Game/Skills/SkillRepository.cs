using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// Repository of skill definitions (Phase 5).
    /// Provides centralized skill configuration.
    /// </summary>
    public sealed class SkillRepository
    {
        private readonly Dictionary<string, SkillDef> _skills = new();

        public SkillRepository()
        {
            InitializeDefaultSkills();
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
            RegisterSkill(new SkillDef
            {
                Id = SkillIds.AttackBasic,
                DamageMultiplier = 1.0,
                CanCrit = true,
                AlwaysHits = true
            });

            // Special pulse - Phase 3: Migrated to use BuffConfigId
            // Tests various buff types: self-buffs, instant heal, DoT, HoT, and debuffs
            RegisterSkill(new SkillDef
            {
                Id = SkillIds.SpecialPulse,
                DamageMultiplier = 1.0,
                CanCrit = true,
                AlwaysHits = true,
                IsAoe = true,
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
                AlwaysHits = true
            });
        }
    }
}
