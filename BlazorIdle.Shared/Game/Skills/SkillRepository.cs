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
        /// </summary>
        public void RegisterSkill(SkillDef skillDef)
        {
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

            // Special pulse - no special effects yet (will be configured in Phase 6)
            RegisterSkill(new SkillDef
            {
                Id = SkillIds.SpecialPulse,
                DamageMultiplier = 1.0,
                CanCrit = true,
                AlwaysHits = true,
                IsAoe = true
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
