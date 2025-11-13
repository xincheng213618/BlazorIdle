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

            // Special pulse - Phase 9: Add test buff for warrior (self-buff with damage/haste/crit boost)
            // Design doc: warrior special applies a 6s buff with +15% damage, +10% haste, +5% crit
            RegisterSkill(new SkillDef
            {
                Id = SkillIds.SpecialPulse,
                DamageMultiplier = 1.0,
                CanCrit = true,
                AlwaysHits = true,
                IsAoe = true,
                OnCastBuffs = new List<BuffOperation>
                {
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.Self,
                        BuffTemplate = new Buffs.BuffInstance(
                            id: "test_warrior_special",
                            ownerId: "template", // Placeholder - will be cloned with actual ownerId during application
                            kind: Buffs.BuffKind.Buff,
                            effects: new List<Buffs.BuffEffect>
                            {
                                Buffs.BuffEffect.StatMultiplier("DamagePerAttack", 0.15),
                                Buffs.BuffEffect.StatAdditive("HastePercent", 10.0),
                                Buffs.BuffEffect.StatAdditive("CritChancePercent", 5.0)
                            },
                            stackingPolicy: Buffs.BuffStackingPolicy.Refresh,
                            durationSec: 6.0,
                            tickIntervalSec: null,
                            maxStacks: 0
                        )
                    }
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
