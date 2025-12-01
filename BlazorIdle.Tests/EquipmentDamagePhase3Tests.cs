using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Items.Equipment;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 装备与伤害系统 Phase 3 单元测试 - 词条系统基础
    /// Equipment and Damage System Phase 3 Unit Tests - Affix System Foundation
    /// </summary>
    public class EquipmentDamagePhase3Tests
    {
        #region Haste Attribute Tests

        [Fact]
        public void CombatStats_HastePercent_DefaultsToZero()
        {
            // Act
            var stats = CombatStats.CreateDefault();

            // Assert
            Assert.Equal(0, stats.HastePercent);
        }

        [Fact]
        public void CombatStats_HastePercent_CanBeSet()
        {
            // Arrange & Act
            var stats = new CombatStats
            {
                HastePercent = 25.0
            };

            // Assert
            Assert.Equal(25.0, stats.HastePercent);
        }

        [Fact]
        public void CombatCapsConfig_HastePct_DefaultsTo40()
        {
            // Act
            var caps = CombatCapsConfig.CreateDefault();

            // Assert
            Assert.Equal(40.0, caps.HastePct);
        }

        [Theory]
        [InlineData(20, 20)]     // Within cap
        [InlineData(40, 40)]     // At cap
        [InlineData(50, 40)]     // Over cap
        [InlineData(-10, 0)]     // Negative
        public void CombatCapsConfig_ClampHastePct_ClampsCorrectly(double input, double expected)
        {
            // Arrange
            var caps = CombatCapsConfig.CreateDefault();

            // Act
            var result = caps.ClampHastePct(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CombatCapsConfig_ApplyCaps_ClipsHastePercent()
        {
            // Arrange
            var caps = CombatCapsConfig.CreateDefault();
            var stats = new CombatStats { HastePercent = 60 }; // Over cap

            // Act
            var capped = caps.ApplyCaps(stats);

            // Assert
            Assert.Equal(40, capped.HastePercent); // Clipped to 40%
        }

        #endregion

        #region AffixScope Enum Tests

        [Fact]
        public void AffixScope_HasCorrectValues()
        {
            // Assert
            Assert.Equal(0, (int)AffixScope.Global);
            Assert.Equal(1, (int)AffixScope.Element);
            Assert.Equal(2, (int)AffixScope.Hybrid);
        }

        #endregion

        #region AffixDef Tests

        [Fact]
        public void AffixDef_Scope_ParsesGlobal()
        {
            // Arrange
            var def = new AffixDef { ScopeString = "global" };

            // Assert
            Assert.Equal(AffixScope.Global, def.Scope);
        }

        [Fact]
        public void AffixDef_Scope_ParsesElement()
        {
            // Arrange
            var def = new AffixDef { ScopeString = "element" };

            // Assert
            Assert.Equal(AffixScope.Element, def.Scope);
        }

        [Fact]
        public void AffixDef_Scope_ParsesHybrid()
        {
            // Arrange
            var def = new AffixDef { ScopeString = "hybrid" };

            // Assert
            Assert.Equal(AffixScope.Hybrid, def.Scope);
        }

        [Fact]
        public void AffixDef_Scope_DefaultsToGlobal()
        {
            // Arrange
            var def = new AffixDef { ScopeString = "unknown" };

            // Assert
            Assert.Equal(AffixScope.Global, def.Scope);
        }

        [Fact]
        public void AffixDef_GetLevelEffects_ReturnsCorrectLevel()
        {
            // Arrange
            var def = new AffixDef
            {
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 4.0 } },
                    new() { { "AttackPercent", 6.0 } },
                    new() { { "AttackPercent", 7.0 } },
                    new() { { "AttackPercent", 8.0 } }
                }
            };

            // Act & Assert
            Assert.Equal(4.0, def.GetLevelEffects(1)["AttackPercent"]);
            Assert.Equal(6.0, def.GetLevelEffects(2)["AttackPercent"]);
            Assert.Equal(7.0, def.GetLevelEffects(3)["AttackPercent"]);
            Assert.Equal(8.0, def.GetLevelEffects(4)["AttackPercent"]);
        }

        [Fact]
        public void AffixDef_GetLevelEffects_ClampsOutOfRange()
        {
            // Arrange
            var def = new AffixDef
            {
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 4.0 } },
                    new() { { "AttackPercent", 8.0 } }
                }
            };

            // Act & Assert
            Assert.Equal(4.0, def.GetLevelEffects(0)["AttackPercent"]); // Below min
            Assert.Equal(8.0, def.GetLevelEffects(99)["AttackPercent"]); // Above max
        }

        [Fact]
        public void AffixDef_MaxLevel_ReturnsCorrectValue()
        {
            // Arrange
            var def = new AffixDef
            {
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 4.0 } },
                    new() { { "AttackPercent", 6.0 } },
                    new() { { "AttackPercent", 7.0 } },
                    new() { { "AttackPercent", 8.0 } }
                }
            };

            // Assert
            Assert.Equal(4, def.MaxLevel);
        }

        [Fact]
        public void AffixDef_HasTag_WorksCorrectly()
        {
            // Arrange
            var def = new AffixDef { Tags = new List<string> { "offense", "core" } };

            // Assert
            Assert.True(def.HasTag("offense"));
            Assert.True(def.HasTag("core"));
            Assert.True(def.HasTag("OFFENSE")); // Case insensitive
            Assert.False(def.HasTag("defense"));
        }

        [Fact]
        public void AffixDef_GetFieldScope_ReturnsCorrectScopeForHybrid()
        {
            // Arrange
            var def = new AffixDef
            {
                ScopeString = "hybrid",
                PerFieldScope = new Dictionary<string, string>
                {
                    { "AttackPercent", "element" },
                    { "CritChancePercent", "global" }
                }
            };

            // Assert
            Assert.Equal(AffixScope.Element, def.GetFieldScope("AttackPercent"));
            Assert.Equal(AffixScope.Global, def.GetFieldScope("CritChancePercent"));
            Assert.Equal(AffixScope.Global, def.GetFieldScope("Unknown")); // Default
        }

        [Fact]
        public void AffixDef_GetFieldScope_ReturnsMainScopeForNonHybrid()
        {
            // Arrange
            var defGlobal = new AffixDef { ScopeString = "global" };
            var defElement = new AffixDef { ScopeString = "element" };

            // Assert
            Assert.Equal(AffixScope.Global, defGlobal.GetFieldScope("AnyField"));
            Assert.Equal(AffixScope.Element, defElement.GetFieldScope("AnyField"));
        }

        #endregion

        #region AffixRules Tests

        [Fact]
        public void AffixRules_AreMutuallyExclusive_DetectsExclusivity()
        {
            // Arrange
            var rules = new AffixRules
            {
                MutuallyExclusive = new Dictionary<string, List<string>>
                {
                    { "assault", new List<string> { "vital_force", "crit_damage_bonus" } }
                }
            };

            // Assert
            Assert.True(rules.AreMutuallyExclusive("assault", "vital_force"));
            Assert.True(rules.AreMutuallyExclusive("vital_force", "assault")); // Bidirectional
            Assert.False(rules.AreMutuallyExclusive("assault", "crit")); // Not in list
            Assert.False(rules.AreMutuallyExclusive("hp", "crit")); // Neither in list
        }

        [Fact]
        public void AffixRules_GetEquipLimit_ReturnsCorrectLimit()
        {
            // Arrange
            var rules = new AffixRules
            {
                EquipLimit = new Dictionary<string, int>
                {
                    { "chase_pct", 8 },
                    { "ken_chase", 6 }
                }
            };

            // Assert
            Assert.Equal(8, rules.GetEquipLimit("chase_pct"));
            Assert.Equal(6, rules.GetEquipLimit("ken_chase"));
            Assert.Null(rules.GetEquipLimit("attack")); // No limit defined
        }

        [Fact]
        public void AffixRules_GetUniqueGroupLimit_ReturnsCorrectLimit()
        {
            // Arrange
            var rules = new AffixRules
            {
                UniqueGroup = new Dictionary<string, int>
                {
                    { "fortify_unique", 1 },
                    { "backwater_unique", 1 }
                }
            };

            // Assert
            Assert.Equal(1, rules.GetUniqueGroupLimit("fortify_unique"));
            Assert.Equal(1, rules.GetUniqueGroupLimit("backwater_unique"));
            Assert.Null(rules.GetUniqueGroupLimit("nonexistent")); // No group
        }

        [Fact]
        public void AffixRules_MaxChaseTypePerItem_DefaultsToOne()
        {
            // Arrange
            var rules = new AffixRules();

            // Assert
            Assert.Equal(1, rules.MaxChaseTypePerItem);
        }

        [Fact]
        public void AffixRules_MaxChaseTypePerItem_ReturnsConfiguredValue()
        {
            // Arrange
            var rules = new AffixRules
            {
                PerItemConstraint = new Dictionary<string, int>
                {
                    { "maxChaseTypePerItem", 2 }
                }
            };

            // Assert
            Assert.Equal(2, rules.MaxChaseTypePerItem);
        }

        #endregion

        #region AffixInstance Tests

        [Fact]
        public void AffixInstance_Create_SetsPropertiesCorrectly()
        {
            // Arrange
            var def = new AffixDef
            {
                Id = "attack",
                Name = "攻击",
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 4.0 } }
                }
            };

            // Act
            var instance = AffixInstance.Create("attack", 1, def);

            // Assert
            Assert.Equal("attack", instance.AffixId);
            Assert.Equal(1, instance.Level);
            Assert.Equal(def, instance.Definition);
        }

        [Fact]
        public void AffixInstance_GetEffects_ReturnsCurrentLevelEffects()
        {
            // Arrange
            var def = new AffixDef
            {
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 4.0 } },
                    new() { { "AttackPercent", 6.0 } }
                }
            };
            var instance = AffixInstance.Create("attack", 2, def);

            // Act
            var effects = instance.GetEffects();

            // Assert
            Assert.Equal(6.0, effects["AttackPercent"]);
        }

        [Fact]
        public void AffixInstance_GetActiveEffects_FiltersGlobalScope()
        {
            // Arrange
            var def = new AffixDef
            {
                ScopeString = "global",
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "HPPercent", 6.0 } }
                }
            };
            var instance = AffixInstance.Create("hp", 1, def);

            // Act - Different elements should not matter for global scope
            var effects = instance.GetActiveEffects("fire", "water");

            // Assert
            Assert.Equal(6.0, effects["HPPercent"]);
        }

        [Fact]
        public void AffixInstance_GetActiveEffects_FiltersElementScope_Matching()
        {
            // Arrange
            var def = new AffixDef
            {
                ScopeString = "element",
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 4.0 } }
                }
            };
            var instance = AffixInstance.Create("attack", 1, def);

            // Act - Elements match
            var effects = instance.GetActiveEffects("fire", "fire");

            // Assert
            Assert.Equal(4.0, effects["AttackPercent"]);
        }

        [Fact]
        public void AffixInstance_GetActiveEffects_FiltersElementScope_NotMatching()
        {
            // Arrange
            var def = new AffixDef
            {
                ScopeString = "element",
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 4.0 } }
                }
            };
            var instance = AffixInstance.Create("attack", 1, def);

            // Act - Elements don't match
            var effects = instance.GetActiveEffects("fire", "water");

            // Assert
            Assert.Empty(effects); // Should be filtered out
        }

        [Fact]
        public void AffixInstance_GetActiveEffects_FiltersHybridScope()
        {
            // Arrange
            var def = new AffixDef
            {
                ScopeString = "hybrid",
                PerFieldScope = new Dictionary<string, string>
                {
                    { "AttackPercent", "element" },
                    { "CritChancePercent", "global" }
                },
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 2.0 }, { "CritChancePercent", 2.0 } }
                }
            };
            var instance = AffixInstance.Create("assault", 1, def);

            // Act - Elements don't match
            var effects = instance.GetActiveEffects("fire", "water");

            // Assert - Only global field should be active
            Assert.False(effects.ContainsKey("AttackPercent"));
            Assert.Equal(2.0, effects["CritChancePercent"]);
        }

        [Fact]
        public void AffixInstance_GetInactiveEffects_ReturnsInactiveElements()
        {
            // Arrange
            var def = new AffixDef
            {
                ScopeString = "element",
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 4.0 } }
                }
            };
            var instance = AffixInstance.Create("attack", 1, def);

            // Act - Elements don't match
            var inactiveEffects = instance.GetInactiveEffects("fire", "water");

            // Assert
            Assert.Equal(4.0, inactiveEffects["AttackPercent"]);
        }

        [Fact]
        public void AffixInstance_LevelUp_IncreasesLevel()
        {
            // Arrange
            var def = new AffixDef
            {
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 4.0 } },
                    new() { { "AttackPercent", 6.0 } }
                }
            };
            var instance = AffixInstance.Create("attack", 1, def);

            // Act
            var result = instance.LevelUp();

            // Assert
            Assert.True(result);
            Assert.Equal(2, instance.Level);
        }

        [Fact]
        public void AffixInstance_LevelUp_FailsAtMaxLevel()
        {
            // Arrange
            var def = new AffixDef
            {
                Levels = new List<Dictionary<string, double>>
                {
                    new() { { "AttackPercent", 4.0 } },
                    new() { { "AttackPercent", 6.0 } }
                }
            };
            var instance = AffixInstance.Create("attack", 2, def);

            // Act
            var result = instance.LevelUp();

            // Assert
            Assert.False(result);
            Assert.Equal(2, instance.Level);
        }

        [Fact]
        public void AffixInstance_Clone_CreatesCopy()
        {
            // Arrange
            var def = new AffixDef { Id = "attack", Name = "攻击" };
            var instance = AffixInstance.Create("attack", 2, def);

            // Act
            var clone = instance.Clone();

            // Assert
            Assert.NotSame(instance, clone);
            Assert.Equal(instance.AffixId, clone.AffixId);
            Assert.Equal(instance.Level, clone.Level);
            Assert.Same(instance.Definition, clone.Definition);
        }

        #endregion

        #region AffixRepository Tests

        [Fact]
        public void AffixRepository_Shared_ReturnsSameInstance()
        {
            // Act
            var repo1 = AffixRepository.Shared;
            var repo2 = AffixRepository.Shared;

            // Assert
            Assert.Same(repo1, repo2);
        }

        [Fact]
        public void AffixRepository_GetAllAffixes_ReturnsAllDefinitions()
        {
            // Arrange
            var repo = AffixRepository.Shared;

            // Act
            var affixes = repo.GetAllAffixes();

            // Assert - Verify all expected core affixes are present
            Assert.NotEmpty(affixes);
            
            // Verify key affixes exist (more maintainable than checking exact count)
            var expectedAffixIds = new[]
            {
                "attack", "special_attack", "hp", "haste", "crit", "crit_damage_bonus",
                "assault", "vital_force", "fortify_boost", "backwater_boost",
                "chase_pct", "chase_flat", "ken_chase", "mitigation", "element_resonance"
            };
            
            foreach (var expectedId in expectedAffixIds)
            {
                Assert.Contains(affixes, a => a.Id == expectedId);
            }
        }

        [Fact]
        public void AffixRepository_GetAffixById_ReturnsCorrectAffix()
        {
            // Arrange
            var repo = AffixRepository.Shared;

            // Act
            var attack = repo.GetAffixById("attack");
            var hp = repo.GetAffixById("hp");
            var haste = repo.GetAffixById("haste");

            // Assert
            Assert.NotNull(attack);
            Assert.Equal("攻击", attack.Name);
            Assert.Equal(AffixScope.Element, attack.Scope);

            Assert.NotNull(hp);
            Assert.Equal("生命", hp.Name);
            Assert.Equal(AffixScope.Global, hp.Scope);

            Assert.NotNull(haste);
            Assert.Equal("急速", haste.Name);
            Assert.Equal(AffixScope.Global, haste.Scope);
        }

        [Fact]
        public void AffixRepository_GetAffixById_ReturnsNullForUnknown()
        {
            // Arrange
            var repo = AffixRepository.Shared;

            // Act
            var result = repo.GetAffixById("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void AffixRepository_GetAffixesByTag_ReturnsFilteredAffixes()
        {
            // Arrange
            var repo = AffixRepository.Shared;

            // Act
            var offenseAffixes = repo.GetAffixesByTag("offense");
            var chaseAffixes = repo.GetAffixesByTag("chase");

            // Assert
            Assert.NotEmpty(offenseAffixes);
            Assert.NotEmpty(chaseAffixes);
            Assert.All(offenseAffixes, a => Assert.True(a.HasTag("offense")));
            Assert.All(chaseAffixes, a => Assert.True(a.HasTag("chase")));
        }

        [Fact]
        public void AffixRepository_GetAffixesByScope_ReturnsFilteredAffixes()
        {
            // Arrange
            var repo = AffixRepository.Shared;

            // Act
            var globalAffixes = repo.GetAffixesByScope(AffixScope.Global);
            var elementAffixes = repo.GetAffixesByScope(AffixScope.Element);
            var hybridAffixes = repo.GetAffixesByScope(AffixScope.Hybrid);

            // Assert
            Assert.NotEmpty(globalAffixes);
            Assert.NotEmpty(elementAffixes);
            Assert.NotEmpty(hybridAffixes);
        }

        [Fact]
        public void AffixRepository_CreateInstance_CreatesInstanceWithDefinition()
        {
            // Arrange
            var repo = AffixRepository.Shared;

            // Act
            var instance = repo.CreateInstance("attack", 2);

            // Assert
            Assert.Equal("attack", instance.AffixId);
            Assert.Equal(2, instance.Level);
            Assert.NotNull(instance.Definition);
            Assert.Equal("攻击", instance.Definition.Name);
        }

        [Fact]
        public void AffixRepository_Rules_LoadsCorrectly()
        {
            // Arrange
            var repo = AffixRepository.Shared;

            // Act
            var rules = repo.Rules;

            // Assert
            Assert.NotNull(rules);
            Assert.True(rules.AreMutuallyExclusive("assault", "vital_force"));
            // Note: equipLimit is now commented out in rules.json for flexibility
            // The functionality is preserved for future use
            Assert.Null(rules.GetEquipLimit("chase_pct")); // Currently no limit configured
            Assert.Null(rules.GetEquipLimit("ken_chase")); // Currently no limit configured
        }

        [Fact]
        public void AffixRepository_AreMutuallyExclusive_Delegate()
        {
            // Arrange
            var repo = AffixRepository.Shared;

            // Assert
            Assert.True(repo.AreMutuallyExclusive("assault", "vital_force"));
            Assert.True(repo.AreMutuallyExclusive("assault", "crit_damage_bonus"));
            Assert.False(repo.AreMutuallyExclusive("attack", "hp"));
        }

        [Fact]
        public void AffixRepository_GetEquipLimit_UnifiedInRules()
        {
            // Arrange
            var repo = AffixRepository.Shared;

            // Assert
            // Equipment limits are now unified in rules.json only
            // Currently commented out for flexibility
            Assert.Null(repo.GetEquipLimit("chase_pct")); // No limit configured
            Assert.Null(repo.GetEquipLimit("ken_chase")); // No limit configured
            Assert.Null(repo.GetEquipLimit("attack")); // No limit
        }

        #endregion

        #region Affix Level Growth Model Tests

        [Fact]
        public void AffixDef_LevelGrowth_FollowsLv1MaxDecreasing()
        {
            // Arrange
            var repo = AffixRepository.Shared;
            var attack = repo.GetAffixById("attack");

            // Assert - Lv1 gives the biggest increase
            Assert.NotNull(attack);
            Assert.Equal(4, attack.Levels.Count);

            var lv1 = attack.GetLevelEffects(1)["AttackPercent"];
            var lv2 = attack.GetLevelEffects(2)["AttackPercent"];
            var lv3 = attack.GetLevelEffects(3)["AttackPercent"];
            var lv4 = attack.GetLevelEffects(4)["AttackPercent"];

            // Check values match design document
            Assert.Equal(4.0, lv1);
            Assert.Equal(6.0, lv2);
            Assert.Equal(7.0, lv3);
            Assert.Equal(8.0, lv4);

            // Verify decreasing increments
            double inc1to2 = lv2 - lv1; // 2.0
            double inc2to3 = lv3 - lv2; // 1.0
            double inc3to4 = lv4 - lv3; // 1.0

            Assert.True(inc1to2 >= inc2to3, "Lv1->2 increment should be >= Lv2->3");
            Assert.True(inc2to3 >= inc3to4, "Lv2->3 increment should be >= Lv3->4");
        }

        [Fact]
        public void AffixDef_HasteAffix_HasCorrectLevelValues()
        {
            // Arrange
            var repo = AffixRepository.Shared;
            var haste = repo.GetAffixById("haste");

            // Assert
            Assert.NotNull(haste);
            Assert.Equal(4, haste.Levels.Count);
            Assert.Equal(AffixScope.Global, haste.Scope);

            Assert.Equal(3.0, haste.GetLevelEffects(1)["HastePercent"]);
            Assert.Equal(4.5, haste.GetLevelEffects(2)["HastePercent"]);
            Assert.Equal(5.5, haste.GetLevelEffects(3)["HastePercent"]);
            Assert.Equal(6.0, haste.GetLevelEffects(4)["HastePercent"]);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Integration_AffixInstanceWithRepositoryDefinition_CalculatesEffectsCorrectly()
        {
            // Arrange
            var repo = AffixRepository.Shared;
            var instance = repo.CreateInstance("assault", 2);

            // Act - Element matches
            var activeMatch = instance.GetActiveEffects("fire", "fire");
            // Act - Element doesn't match
            var activeNoMatch = instance.GetActiveEffects("fire", "water");

            // Assert - When matched, both fields active
            Assert.Equal(3.0, activeMatch["AttackPercent"]);
            Assert.Equal(3.0, activeMatch["CritChancePercent"]);

            // Assert - When not matched, only global field active
            Assert.False(activeNoMatch.ContainsKey("AttackPercent"));
            Assert.Equal(3.0, activeNoMatch["CritChancePercent"]);
        }

        [Fact]
        public void Integration_MultipleAffixesWithScopeFiltering()
        {
            // Arrange
            var repo = AffixRepository.Shared;
            var attackInstance = repo.CreateInstance("attack", 4); // Element scope
            var hpInstance = repo.CreateInstance("hp", 4);         // Global scope
            var hasteInstance = repo.CreateInstance("haste", 4);   // Global scope

            // Act - Fire equipment, Water main element
            var attackActive = attackInstance.GetActiveEffects("fire", "water");
            var hpActive = hpInstance.GetActiveEffects("fire", "water");
            var hasteActive = hasteInstance.GetActiveEffects("fire", "water");

            // Assert - Only global scope affixes active when elements don't match
            Assert.Empty(attackActive); // Element scope, not active
            Assert.Equal(10.0, hpActive["HPPercent"]); // Global, active
            Assert.Equal(6.0, hasteActive["HastePercent"]); // Global, active
        }

        #endregion
    }
}
