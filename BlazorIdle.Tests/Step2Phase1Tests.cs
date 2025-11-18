using BlazorIdle.Game.Skills;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Unit tests for Step 2 Phase 1: Skill Configuration Infrastructure
    /// </summary>
    public class Step2Phase1Tests
    {
        #region SkillDef Construction Tests (3 tests)

        [Fact]
        public void SkillDef_CanBeConstructed_WithBasicProperties()
        {
            // Arrange & Act
            var skill = new SkillDef
            {
                Id = "test_skill",
                Name = "Test Skill",
                Description = "A test skill",
                Type = "active",
                SlotType = "active",
                Fixed = false,
                ReleaseType = "instant",
                TargetPolicy = "current_target"
            };

            // Assert
            Assert.Equal("test_skill", skill.Id);
            Assert.Equal("Test Skill", skill.Name);
            Assert.Equal("A test skill", skill.Description);
            Assert.Equal("active", skill.Type);
            Assert.Equal("active", skill.SlotType);
            Assert.False(skill.Fixed);
            Assert.Equal("instant", skill.ReleaseType);
            Assert.Equal("current_target", skill.TargetPolicy);
        }

        [Fact]
        public void SkillDef_CanBeConstructed_WithDamageAndResources()
        {
            // Arrange & Act
            var skill = new SkillDef
            {
                Id = "warrior_slam",
                Name = "Slam",
                Damage = new DamageDef
                {
                    CoefAtk = 1.2,
                    Flat = 20
                },
                Costs = new System.Collections.Generic.List<ResourceCost>
                {
                    new ResourceCost { BucketId = "rage", Amount = 3 }
                },
                Gains = new System.Collections.Generic.List<ResourceGain>
                {
                    new ResourceGain { BucketId = "rage", Amount = 1 }
                }
            };

            // Assert
            Assert.NotNull(skill.Damage);
            Assert.Equal(1.2, skill.Damage.CoefAtk);
            Assert.Equal(20, skill.Damage.Flat);
            Assert.NotNull(skill.Costs);
            Assert.Single(skill.Costs);
            Assert.Equal("rage", skill.Costs[0].BucketId);
            Assert.Equal(3, skill.Costs[0].Amount);
            Assert.NotNull(skill.Gains);
            Assert.Single(skill.Gains);
            Assert.Equal("rage", skill.Gains[0].BucketId);
            Assert.Equal(1, skill.Gains[0].Amount);
        }

        [Fact]
        public void SkillDef_CanBeConstructed_WithConditionsAndTriggers()
        {
            // Arrange & Act
            var skill = new SkillDef
            {
                Id = "rogue_eviscerate",
                Name = "Eviscerate",
                Conditions = new SkillConditions
                {
                    RequireBuffId = "rogue_combo",
                    HpBelowPct = 100
                },
                Triggers = new System.Collections.Generic.List<TriggerDef>
                {
                    new TriggerDef
                    {
                        When = "OnAttackHit",
                        ProcChance = 0.4,
                        FireSkillId = "rogue_gain_combo",
                        Priority = 10
                    }
                },
                Unlock = new UnlockConfig
                {
                    MinLevel = 3
                },
                AllowedProfessions = new System.Collections.Generic.List<string> { "rogue" }
            };

            // Assert
            Assert.NotNull(skill.Conditions);
            Assert.Equal("rogue_combo", skill.Conditions.RequireBuffId);
            Assert.Equal(100, skill.Conditions.HpBelowPct);
            Assert.NotNull(skill.Triggers);
            Assert.Single(skill.Triggers);
            Assert.Equal("OnAttackHit", skill.Triggers[0].When);
            Assert.Equal(0.4, skill.Triggers[0].ProcChance);
            Assert.Equal("rogue_gain_combo", skill.Triggers[0].FireSkillId);
            Assert.NotNull(skill.Unlock);
            Assert.Equal(3, skill.Unlock.MinLevel);
            Assert.NotNull(skill.AllowedProfessions);
            Assert.Contains("rogue", skill.AllowedProfessions);
        }

        #endregion

        #region Supporting Class Tests (4 tests)

        [Fact]
        public void UnlockConfig_CanBeConstructed()
        {
            // Arrange & Act
            var unlock = new UnlockConfig
            {
                MinLevel = 5,
                AccountFlags = new System.Collections.Generic.List<string> { "flag1", "flag2" },
                RequiresProfessionLevel = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "warrior", 10 }
                }
            };

            // Assert
            Assert.Equal(5, unlock.MinLevel);
            Assert.NotNull(unlock.AccountFlags);
            Assert.Equal(2, unlock.AccountFlags.Count);
            Assert.NotNull(unlock.RequiresProfessionLevel);
            Assert.Equal(10, unlock.RequiresProfessionLevel["warrior"]);
        }

        [Fact]
        public void SkillConditions_CanBeConstructed()
        {
            // Arrange & Act
            var conditions = new SkillConditions
            {
                HpBelowPct = 50,
                HpAbovePct = 20,
                RequireBuffId = "test_buff",
                ForbidBuffId = "forbidden_buff",
                RequireResource = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "mana", 10 }
                }
            };

            // Assert
            Assert.Equal(50, conditions.HpBelowPct);
            Assert.Equal(20, conditions.HpAbovePct);
            Assert.Equal("test_buff", conditions.RequireBuffId);
            Assert.Equal("forbidden_buff", conditions.ForbidBuffId);
            Assert.NotNull(conditions.RequireResource);
            Assert.Equal(10, conditions.RequireResource["mana"]);
        }

        [Fact]
        public void TriggerDef_CanBeConstructed()
        {
            // Arrange & Act
            var trigger = new TriggerDef
            {
                When = "OnAttackCrit",
                ProcChance = 1.0,
                FireSkillId = "triggered_skill",
                Priority = 5,
                IgnoreRequirements = true
            };

            // Assert
            Assert.Equal("OnAttackCrit", trigger.When);
            Assert.Equal(1.0, trigger.ProcChance);
            Assert.Equal("triggered_skill", trigger.FireSkillId);
            Assert.Equal(5, trigger.Priority);
            Assert.True(trigger.IgnoreRequirements);
        }

        [Fact]
        public void DamageDef_CanBeConstructed()
        {
            // Arrange & Act
            var damage = new DamageDef
            {
                CoefAtk = 1.5,
                Flat = 30
            };

            // Assert
            Assert.Equal(1.5, damage.CoefAtk);
            Assert.Equal(30, damage.Flat);
            // Note: AOE is determined by targetPolicy in SkillDef, not in DamageDef
        }

        #endregion

        #region SkillRepository Loading Tests (4 tests)

        [Fact]
        public void SkillRepository_LoadsFromEmbeddedJson()
        {
            // Arrange & Act
            var repo = new SkillRepository();

            // Assert
            Assert.True(repo.Count > 0, "SkillRepository should load skills from embedded JSON");
            
            // Verify default skills are still present
            Assert.NotNull(repo.GetSkill("attack_basic"));
            Assert.NotNull(repo.GetSkill("special_pulse"));
            Assert.NotNull(repo.GetSkill("enemy_attack_basic"));
        }

        [Fact]
        public void SkillRepository_LoadsWarriorSkills()
        {
            // Arrange
            var repo = new SkillRepository();

            // Act
            var warriorSkill = repo.GetSkillById("warrior_slam");
            var warriorSpecial = repo.GetSkillById("warrior_special_pulse");

            // Assert
            Assert.NotNull(warriorSkill);
            Assert.Equal("猛击", warriorSkill.Name);
            Assert.Contains("warrior", warriorSkill.AllowedProfessions);
            
            Assert.NotNull(warriorSpecial);
            Assert.Equal("战士战吼", warriorSpecial.Name);
            Assert.True(warriorSpecial.Fixed);
        }

        [Fact]
        public void SkillRepository_LoadsMageSkills()
        {
            // Arrange
            var repo = new SkillRepository();

            // Act
            var mageSkill = repo.GetSkillById("mage_pyroblast");
            var mageBasic = repo.GetSkillById("mage_attack_basic");

            // Assert
            Assert.NotNull(mageSkill);
            Assert.Equal("炎爆术", mageSkill.Name);
            Assert.Equal("cast", mageSkill.ReleaseType);
            Assert.Equal(2.5, mageSkill.CastTimeSec);
            Assert.Contains("mage", mageSkill.AllowedProfessions);
            
            Assert.NotNull(mageBasic);
            Assert.True(mageBasic.Fixed);
        }

        [Fact]
        public void SkillRepository_GetSkillsByProfession_ReturnsCorrectSkills()
        {
            // Arrange
            var repo = new SkillRepository();

            // Act
            var warriorSkills = repo.GetSkillsByProfession("warrior");
            var mageSkills = repo.GetSkillsByProfession("mage");
            var rogueSkills = repo.GetSkillsByProfession("rogue");
            var rangerSkills = repo.GetSkillsByProfession("ranger");

            // Assert
            Assert.NotEmpty(warriorSkills);
            Assert.Contains(warriorSkills, s => s.Id == "warrior_slam");
            Assert.DoesNotContain(warriorSkills, s => s.Id == "mage_pyroblast");
            
            Assert.NotEmpty(mageSkills);
            Assert.Contains(mageSkills, s => s.Id == "mage_pyroblast");
            Assert.DoesNotContain(mageSkills, s => s.Id == "warrior_slam");
            
            Assert.NotEmpty(rogueSkills);
            Assert.Contains(rogueSkills, s => s.Id == "rogue_backstab");
            
            Assert.NotEmpty(rangerSkills);
            Assert.Contains(rangerSkills, s => s.Id == "ranger_arcane_shot");
        }

        #endregion

        #region Configuration Validation Tests (4 tests)

        [Fact]
        public void SkillRepository_ValidatesTriggerReferences()
        {
            // Arrange
            var repo = new SkillRepository();
            
            // Act - Get a skill with triggers
            var warriorPulse = repo.GetSkillById("warrior_special_pulse");
            
            // Assert
            Assert.NotNull(warriorPulse);
            Assert.NotNull(warriorPulse.Triggers);
            Assert.NotEmpty(warriorPulse.Triggers);
            
            // Verify the referenced skill exists
            var referencedSkillId = warriorPulse.Triggers[0].FireSkillId;
            var referencedSkill = repo.GetSkillById(referencedSkillId);
            Assert.NotNull(referencedSkill);
        }

        [Fact]
        public void SkillRepository_LoadsSkillsWithDamageCorrectly()
        {
            // Arrange
            var repo = new SkillRepository();
            
            // Act
            var slam = repo.GetSkillById("warrior_slam");
            
            // Assert
            Assert.NotNull(slam);
            Assert.NotNull(slam.Damage);
            Assert.Equal(1.2, slam.Damage.CoefAtk);
            Assert.Equal(20, slam.Damage.Flat);
            // Note: AOE is determined by targetPolicy in SkillDef, not in DamageDef
        }

        [Fact]
        public void SkillRepository_LoadsSkillsWithResourceCostsCorrectly()
        {
            // Arrange
            var repo = new SkillRepository();
            
            // Act
            var mortalStrike = repo.GetSkillById("warrior_mortal_strike");
            
            // Assert
            Assert.NotNull(mortalStrike);
            Assert.NotNull(mortalStrike.Costs);
            Assert.NotEmpty(mortalStrike.Costs);
            Assert.Equal("rage", mortalStrike.Costs[0].BucketId);
            Assert.Equal(3, mortalStrike.Costs[0].Amount);
        }

        [Fact]
        public void SkillRepository_LoadsSkillsWithUnlockConditionsCorrectly()
        {
            // Arrange
            var repo = new SkillRepository();
            
            // Act
            var desperateStrike = repo.GetSkillById("warrior_desperate_strike");
            
            // Assert
            Assert.NotNull(desperateStrike);
            Assert.NotNull(desperateStrike.Unlock);
            Assert.Equal(10, desperateStrike.Unlock.MinLevel);
            Assert.NotNull(desperateStrike.Conditions);
            Assert.Equal(50, desperateStrike.Conditions.HpBelowPct);
        }

        #endregion
    }
}
