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

            // Special pulse - Phase 9: Comprehensive buff/debuff demonstration
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
                    // 1. Self-buff: Damage/Haste/Crit boost (original warrior buff)
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.Self,
                        BuffTemplate = new Buffs.BuffInstance(
                            id: "warrior_power_boost",
                            ownerId: "template",
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
                    },
                    // 2. Instant Heal: Heal self immediately
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.Self,
                        BuffTemplate = new Buffs.BuffInstance(
                            id: "instant_heal",
                            ownerId: "template",
                            kind: Buffs.BuffKind.Buff,
                            effects: new List<Buffs.BuffEffect>
                            {
                                Buffs.BuffEffect.InstantHeal(20)
                            },
                            stackingPolicy: Buffs.BuffStackingPolicy.Refresh,
                            durationSec: 0.1, // Very short duration for instant effect
                            tickIntervalSec: null,
                            maxStacks: 0
                        )
                    },
                    // 3. HoT (Heal over Time): Regeneration buff on self
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.Self,
                        BuffTemplate = new Buffs.BuffInstance(
                            id: "regeneration",
                            ownerId: "template",
                            kind: Buffs.BuffKind.Buff,
                            effects: new List<Buffs.BuffEffect>
                            {
                                Buffs.BuffEffect.HealOverTime(5)
                            },
                            stackingPolicy: Buffs.BuffStackingPolicy.Stack,
                            durationSec: 8.0,
                            tickIntervalSec: 2.0,
                            maxStacks: 3
                        )
                    },
                    // 4. DoT (Damage over Time): Burn debuff on enemies
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.AllEnemies,
                        BuffTemplate = new Buffs.BuffInstance(
                            id: "burning",
                            ownerId: "template",
                            kind: Buffs.BuffKind.Debuff,
                            effects: new List<Buffs.BuffEffect>
                            {
                                Buffs.BuffEffect.DamageOverTime(8)
                            },
                            stackingPolicy: Buffs.BuffStackingPolicy.Stack,
                            durationSec: 10.0,
                            tickIntervalSec: 2.0,
                            maxStacks: 5
                        )
                    },
                    // 5. Stat Reduction Debuff: Weaken enemy damage
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.AllEnemies,
                        BuffTemplate = new Buffs.BuffInstance(
                            id: "weakened",
                            ownerId: "template",
                            kind: Buffs.BuffKind.Debuff,
                            effects: new List<Buffs.BuffEffect>
                            {
                                Buffs.BuffEffect.StatReduction("DamagePerHit", 0.20)
                            },
                            stackingPolicy: Buffs.BuffStackingPolicy.Refresh,
                            durationSec: 8.0,
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
