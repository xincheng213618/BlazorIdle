using BlazorIdle.Game.Combat;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 装备与伤害系统 Phase 1 单元测试
    /// Equipment and Damage System Phase 1 Unit Tests
    /// </summary>
    public class EquipmentDamagePhase1Tests
    {
        #region CombatStats Tests

        [Fact]
        public void CombatStats_CreateDefault_ReturnsValidDefaults()
        {
            // Act
            var stats = CombatStats.CreateDefault();

            // Assert
            Assert.Equal(100, stats.AttackFinal);
            Assert.Equal(0, stats.AttackPercent);
            Assert.Equal(0, stats.SpecialAttackPercent);
            Assert.Equal(0, stats.HPPercent);
            Assert.Equal(0, stats.CritChancePercent);
            Assert.Equal(0, stats.CritDamageBonusPercent);
            Assert.Equal(0, stats.FortifyMaxPercent);
            Assert.Equal(0, stats.BackwaterMaxPercent);
            Assert.Equal(0, stats.ChasePercent);
            Assert.Equal(0, stats.KenChasePercent);
            Assert.Equal(0, stats.ChaseFlat);
            Assert.Equal(0, stats.Armor);
            Assert.Equal(0, stats.DamageReductionPercent);
        }

        [Fact]
        public void CombatStats_AllPropertiesCanBeSet()
        {
            // Arrange & Act
            var stats = new CombatStats
            {
                AttackFinal = 500,
                AttackPercent = 80,
                SpecialAttackPercent = 60,
                HPPercent = 100,
                CritChancePercent = 50,
                CritDamageBonusPercent = 40,
                FortifyMaxPercent = 15,
                BackwaterMaxPercent = 18,
                ChasePercent = 25,
                KenChasePercent = 15,
                ChaseFlat = 100,
                Armor = 50,
                DamageReductionPercent = 30
            };

            // Assert
            Assert.Equal(500, stats.AttackFinal);
            Assert.Equal(80, stats.AttackPercent);
            Assert.Equal(60, stats.SpecialAttackPercent);
            Assert.Equal(100, stats.HPPercent);
            Assert.Equal(50, stats.CritChancePercent);
            Assert.Equal(40, stats.CritDamageBonusPercent);
            Assert.Equal(15, stats.FortifyMaxPercent);
            Assert.Equal(18, stats.BackwaterMaxPercent);
            Assert.Equal(25, stats.ChasePercent);
            Assert.Equal(15, stats.KenChasePercent);
            Assert.Equal(100, stats.ChaseFlat);
            Assert.Equal(50, stats.Armor);
            Assert.Equal(30, stats.DamageReductionPercent);
        }

        #endregion

        #region CombatCapsConfig Tests

        [Fact]
        public void CombatCapsConfig_CreateDefault_ReturnsValidDefaults()
        {
            // Act
            var caps = CombatCapsConfig.CreateDefault();

            // Assert
            Assert.Equal(100.0, caps.AttackPct);
            Assert.Equal(80.0, caps.SpecialAttackPct);
            Assert.Equal(100.0, caps.HpPercent);
            Assert.Equal(80.0, caps.CritChancePct);
            Assert.Equal(50.0, caps.CritDamageBonusPct);
            Assert.Equal(20.0, caps.FortifyMaxPct);
            Assert.Equal(20.0, caps.BackwaterMaxPct);
            Assert.Equal(30.0, caps.ChasePct);
            Assert.Equal(20.0, caps.KenChasePct);
            Assert.Equal(90.0, caps.DamageReductionPct);
            Assert.Equal(9999, caps.ChaseFlatCap);
        }

        [Theory]
        [InlineData(50, 50)]     // Within cap
        [InlineData(100, 100)]   // At cap
        [InlineData(150, 100)]   // Over cap
        [InlineData(-10, 0)]     // Negative
        public void CombatCapsConfig_ClampAttackPct_ClampsCorrectly(double input, double expected)
        {
            // Arrange
            var caps = CombatCapsConfig.CreateDefault();

            // Act
            var result = caps.ClampAttackPct(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(40, 40)]     // Within cap
        [InlineData(80, 80)]     // At cap
        [InlineData(100, 80)]    // Over cap
        public void CombatCapsConfig_ClampSpecialAttackPct_ClampsCorrectly(double input, double expected)
        {
            // Arrange
            var caps = CombatCapsConfig.CreateDefault();

            // Act
            var result = caps.ClampSpecialAttackPct(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(30, 30)]     // Within cap
        [InlineData(50, 50)]     // At cap
        [InlineData(70, 50)]     // Over cap
        public void CombatCapsConfig_ClampCritDamageBonusPct_ClampsCorrectly(double input, double expected)
        {
            // Arrange
            var caps = CombatCapsConfig.CreateDefault();

            // Act
            var result = caps.ClampCritDamageBonusPct(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(5000, 5000)]    // Within cap
        [InlineData(9999, 9999)]    // At cap
        [InlineData(15000, 9999)]   // Over cap
        [InlineData(-100, 0)]       // Negative
        public void CombatCapsConfig_ClampChaseFlat_ClampsCorrectly(int input, int expected)
        {
            // Arrange
            var caps = CombatCapsConfig.CreateDefault();

            // Act
            var result = caps.ClampChaseFlat(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CombatCapsConfig_ApplyCaps_ClampsAllProperties()
        {
            // Arrange
            var caps = CombatCapsConfig.CreateDefault();
            var stats = new CombatStats
            {
                AttackFinal = 1000,
                AttackPercent = 150,         // Over cap (100)
                SpecialAttackPercent = 100,  // Over cap (80)
                HPPercent = 120,             // Over cap (100)
                CritChancePercent = 90,      // Over cap (80)
                CritDamageBonusPercent = 60, // Over cap (50)
                FortifyMaxPercent = 25,      // Over cap (20)
                BackwaterMaxPercent = 30,    // Over cap (20)
                ChasePercent = 40,           // Over cap (30)
                KenChasePercent = 25,        // Over cap (20)
                ChaseFlat = 15000,           // Over cap (9999)
                Armor = 100,
                DamageReductionPercent = 95  // Over cap (90)
            };

            // Act
            var clamped = caps.ApplyCaps(stats);

            // Assert
            Assert.Equal(1000, clamped.AttackFinal); // Not clamped
            Assert.Equal(100, clamped.AttackPercent);
            Assert.Equal(80, clamped.SpecialAttackPercent);
            Assert.Equal(100, clamped.HPPercent);
            Assert.Equal(80, clamped.CritChancePercent);
            Assert.Equal(50, clamped.CritDamageBonusPercent);
            Assert.Equal(20, clamped.FortifyMaxPercent);
            Assert.Equal(20, clamped.BackwaterMaxPercent);
            Assert.Equal(30, clamped.ChasePercent);
            Assert.Equal(20, clamped.KenChasePercent);
            Assert.Equal(9999, clamped.ChaseFlat);
            Assert.Equal(100, clamped.Armor); // Not clamped
            Assert.Equal(90, clamped.DamageReductionPercent);
        }

        #endregion

        #region CritConfig Tests

        [Fact]
        public void CritConfig_CreateDefault_ReturnsBaseMultiplier1_2()
        {
            // Act
            var config = CritConfig.CreateDefault();

            // Assert
            Assert.Equal(1.2, config.BaseMultiplier);
        }

        #endregion

        #region ElementIds Tests

        [Fact]
        public void ElementIds_AllConstantsAreDefined()
        {
            // Assert
            Assert.Equal("fire", ElementIds.Fire);
            Assert.Equal("water", ElementIds.Water);
            Assert.Equal("wind", ElementIds.Wind);
            Assert.Equal("earth", ElementIds.Earth);
            Assert.Equal("light", ElementIds.Light);
            Assert.Equal("dark", ElementIds.Dark);
            Assert.Equal("neutral", ElementIds.Neutral);
        }

        #endregion

        #region ElementMatrix Tests

        [Fact]
        public void ElementMatrix_CreateDefault_HasCorrectMultipliers()
        {
            // Act
            var matrix = ElementMatrix.CreateDefault();

            // Assert
            Assert.Equal(1.5, matrix.Config.AdvantageMultiplier);
            Assert.Equal(0.75, matrix.Config.DisadvantageMultiplier);
            Assert.Equal(1.0, matrix.Config.NeutralMultiplier);
        }

        [Theory]
        [InlineData("fire", "wind", 1.5)]     // Fire beats Wind
        [InlineData("wind", "earth", 1.5)]    // Wind beats Earth
        [InlineData("earth", "water", 1.5)]   // Earth beats Water
        [InlineData("water", "fire", 1.5)]    // Water beats Fire
        [InlineData("light", "dark", 1.5)]    // Light beats Dark
        [InlineData("dark", "light", 1.5)]    // Dark beats Light
        public void ElementMatrix_GetMultiplier_ReturnsAdvantageMultiplier(string attacker, string defender, double expected)
        {
            // Arrange
            var matrix = ElementMatrix.CreateDefault();

            // Act
            var result = matrix.GetMultiplier(attacker, defender);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("wind", "fire", 0.75)]    // Wind weak to Fire
        [InlineData("earth", "wind", 0.75)]   // Earth weak to Wind
        [InlineData("water", "earth", 0.75)]  // Water weak to Earth
        [InlineData("fire", "water", 0.75)]   // Fire weak to Water
        public void ElementMatrix_GetMultiplier_ReturnsDisadvantageMultiplier(string attacker, string defender, double expected)
        {
            // Arrange
            var matrix = ElementMatrix.CreateDefault();

            // Act
            var result = matrix.GetMultiplier(attacker, defender);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("fire", "earth", 1.0)]    // No relation
        [InlineData("water", "wind", 1.0)]    // No relation
        [InlineData("fire", "fire", 1.0)]     // Same element
        [InlineData("neutral", "fire", 1.0)]  // Neutral attacker
        [InlineData("fire", "neutral", 1.0)]  // Neutral defender
        public void ElementMatrix_GetMultiplier_ReturnsNeutralMultiplier(string attacker, string defender, double expected)
        {
            // Arrange
            var matrix = ElementMatrix.CreateDefault();

            // Act
            var result = matrix.GetMultiplier(attacker, defender);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("fire", "wind", "advantage")]
        [InlineData("wind", "fire", "disadvantage")]
        [InlineData("fire", "earth", "neutral")]
        [InlineData("fire", "fire", "neutral")]
        [InlineData("neutral", "fire", "neutral")]
        public void ElementMatrix_GetRelation_ReturnsCorrectRelation(string attacker, string defender, string expected)
        {
            // Arrange
            var matrix = ElementMatrix.CreateDefault();

            // Act
            var result = matrix.GetRelation(attacker, defender);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("fire", "wind", true)]
        [InlineData("wind", "fire", false)]
        [InlineData("fire", "earth", false)]
        public void ElementMatrix_HasAdvantage_ReturnsCorrectValue(string attacker, string defender, bool expected)
        {
            // Arrange
            var matrix = ElementMatrix.CreateDefault();

            // Act
            var result = matrix.HasAdvantage(attacker, defender);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("fire", "wind", false)]
        [InlineData("wind", "fire", true)]
        [InlineData("fire", "earth", false)]
        public void ElementMatrix_HasDisadvantage_ReturnsCorrectValue(string attacker, string defender, bool expected)
        {
            // Arrange
            var matrix = ElementMatrix.CreateDefault();

            // Act
            var result = matrix.HasDisadvantage(attacker, defender);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ElementMatrix_GetMultiplier_WithEmptyStrings_ReturnsNeutral()
        {
            // Arrange
            var matrix = ElementMatrix.CreateDefault();

            // Act & Assert
            Assert.Equal(1.0, matrix.GetMultiplier("", "fire"));
            Assert.Equal(1.0, matrix.GetMultiplier("fire", ""));
            Assert.Equal(1.0, matrix.GetMultiplier("", ""));
        }

        #endregion

        #region Config Loading Tests

        [Fact]
        public void CombatConfigRepository_LoadCombatCaps_ReturnsValidConfig()
        {
            // Arrange
            CombatConfigRepository.ClearCache();

            // Act
            var caps = CombatConfigRepository.LoadCombatCaps();

            // Assert
            Assert.NotNull(caps);
            Assert.Equal(100.0, caps.AttackPct);
            Assert.Equal(80.0, caps.SpecialAttackPct);
            Assert.Equal(50.0, caps.CritDamageBonusPct);
        }

        [Fact]
        public void CombatConfigRepository_LoadCritConfig_ReturnsValidConfig()
        {
            // Arrange
            CombatConfigRepository.ClearCache();

            // Act
            var config = CombatConfigRepository.LoadCritConfig();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(1.2, config.BaseMultiplier);
        }

        [Fact]
        public void CombatConfigRepository_LoadElementTypes_ReturnsAllElements()
        {
            // Arrange
            CombatConfigRepository.ClearCache();

            // Act
            var types = CombatConfigRepository.LoadElementTypes();

            // Assert
            Assert.NotNull(types);
            Assert.Equal(7, types.Count);
            Assert.Contains(types, t => t.Id == "fire");
            Assert.Contains(types, t => t.Id == "water");
            Assert.Contains(types, t => t.Id == "wind");
            Assert.Contains(types, t => t.Id == "earth");
            Assert.Contains(types, t => t.Id == "light");
            Assert.Contains(types, t => t.Id == "dark");
            Assert.Contains(types, t => t.Id == "neutral");
        }

        [Fact]
        public void CombatConfigRepository_LoadElementMatrix_ReturnsValidMatrix()
        {
            // Arrange
            CombatConfigRepository.ClearCache();

            // Act
            var matrix = CombatConfigRepository.LoadElementMatrix();

            // Assert
            Assert.NotNull(matrix);
            Assert.Equal(1.5, matrix.Config.AdvantageMultiplier);
            Assert.Equal(0.75, matrix.Config.DisadvantageMultiplier);
            Assert.Equal(6, matrix.Config.Relations.Count);
        }

        [Fact]
        public void CombatConfigRepository_CachesResults()
        {
            // Arrange
            CombatConfigRepository.ClearCache();

            // Act
            var first = CombatConfigRepository.LoadCombatCaps();
            var second = CombatConfigRepository.LoadCombatCaps();

            // Assert - Same reference means cache is working
            Assert.Same(first, second);
        }

        #endregion

        #region Character/Enemy Element Tests

        [Fact]
        public void Character_DefaultElement_IsNeutral()
        {
            // Arrange & Act
            var character = new BlazorIdle.Game.Character();

            // Assert
            Assert.Equal(ElementIds.Neutral, character.Element);
        }

        [Fact]
        public void Character_Element_CanBeSet()
        {
            // Arrange
            var character = new BlazorIdle.Game.Character();

            // Act
            character.Element = ElementIds.Fire;

            // Assert
            Assert.Equal(ElementIds.Fire, character.Element);
        }

        [Fact]
        public void Character_CombatStats_DefaultIsNull()
        {
            // Arrange & Act
            var character = new BlazorIdle.Game.Character();

            // Assert
            Assert.Null(character.CombatStats);
        }

        [Fact]
        public void Character_CombatStats_CanBeSet()
        {
            // Arrange
            var character = new BlazorIdle.Game.Character();
            var stats = CombatStats.CreateDefault();
            stats.AttackFinal = 500;

            // Act
            character.CombatStats = stats;

            // Assert
            Assert.NotNull(character.CombatStats);
            Assert.Equal(500, character.CombatStats.AttackFinal);
        }

        [Fact]
        public void Enemy_DefaultElement_IsNeutral()
        {
            // Arrange & Act
            var enemy = new BlazorIdle.Game.Enemy();

            // Assert
            Assert.Equal(ElementIds.Neutral, enemy.Element);
        }

        [Fact]
        public void Enemy_Element_CanBeSet()
        {
            // Arrange
            var enemy = new BlazorIdle.Game.Enemy();

            // Act
            enemy.Element = ElementIds.Water;

            // Assert
            Assert.Equal(ElementIds.Water, enemy.Element);
        }

        [Fact]
        public void Enemy_Armor_DefaultIsZero()
        {
            // Arrange & Act
            var enemy = new BlazorIdle.Game.Enemy();

            // Assert
            Assert.Equal(0, enemy.Armor);
        }

        [Fact]
        public void Enemy_Armor_CanBeSet()
        {
            // Arrange
            var enemy = new BlazorIdle.Game.Enemy();

            // Act
            enemy.Armor = 50;

            // Assert
            Assert.Equal(50, enemy.Armor);
        }

        [Fact]
        public void Enemy_DamageReductionPercent_DefaultIsZero()
        {
            // Arrange & Act
            var enemy = new BlazorIdle.Game.Enemy();

            // Assert
            Assert.Equal(0, enemy.DamageReductionPercent);
        }

        [Fact]
        public void Enemy_DamageReductionPercent_CanBeSet()
        {
            // Arrange
            var enemy = new BlazorIdle.Game.Enemy();

            // Act
            enemy.DamageReductionPercent = 25;

            // Assert
            Assert.Equal(25, enemy.DamageReductionPercent);
        }

        #endregion
    }
}
