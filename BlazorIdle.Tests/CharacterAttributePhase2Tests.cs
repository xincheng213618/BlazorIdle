using BlazorIdle.Game;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Items.Equipment;
using BlazorIdle.Game.Professions;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 角色属性系统 Phase 2 单元测试 - 属性汇总逻辑
    /// Character Attribute System Phase 2 Unit Tests - Stats Aggregation Logic
    /// </summary>
    public class CharacterAttributePhase2Tests
    {
        #region CharacterStatsCalculator Tests

        [Fact]
        public void CharacterStatsCalculator_CreateDefault_ReturnsValidInstance()
        {
            // Act
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Assert
            Assert.NotNull(calculator);
        }

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalStats_WithWarrior_NoEquipment_ReturnsBaseStats()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var stats = calculator.CalculateFinalStats("warrior", null);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(100, stats.AttackFinal); // Warrior base attack
            Assert.Equal(5.0, stats.CritChancePercent); // Warrior initial crit
            Assert.Equal(0.0, stats.HastePercent); // Warrior has no initial haste
        }

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalStats_WithMage_NoEquipment_ReturnsBaseStats()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var stats = calculator.CalculateFinalStats("mage", null);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(80, stats.AttackFinal); // Mage base attack
            Assert.Equal(10.0, stats.CritChancePercent); // Mage initial crit
            Assert.Equal(10.0, stats.CritDamageBonusPercent); // Mage initial crit damage bonus
            Assert.Equal(5.0, stats.SpecialAttackPercent); // Mage initial special attack
        }

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalStats_WithRogue_NoEquipment_ReturnsHighHaste()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var stats = calculator.CalculateFinalStats("rogue", null);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(70, stats.AttackFinal); // Rogue base attack
            Assert.Equal(15.0, stats.CritChancePercent); // Rogue initial crit (highest)
            Assert.Equal(10.0, stats.HastePercent); // Rogue initial haste (highest)
        }

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalStats_WithRanger_NoEquipment_ReturnsBalancedStats()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var stats = calculator.CalculateFinalStats("ranger", null);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(85, stats.AttackFinal); // Ranger base attack
            Assert.Equal(8.0, stats.CritChancePercent); // Ranger initial crit
            Assert.Equal(5.0, stats.HastePercent); // Ranger initial haste
        }

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalStats_InvalidProfession_ReturnsDefaultStats()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var stats = calculator.CalculateFinalStats("invalid", null);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(100, stats.AttackFinal); // Default base attack
        }

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalMaxHp_WithWarrior_NoEquipment_ReturnsBaseHp()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var maxHp = calculator.CalculateFinalMaxHp("warrior", null);

            // Assert
            Assert.Equal(500, maxHp); // Warrior base HP with no HP% bonus
        }

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalMaxHp_WithMage_NoEquipment_ReturnsLowerHp()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var maxHp = calculator.CalculateFinalMaxHp("mage", null);

            // Assert
            Assert.Equal(300, maxHp); // Mage base HP (lowest)
        }

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalAttackRate_WithWarrior_NoEquipment_ReturnsBaseAPS()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var attackRate = calculator.CalculateFinalAttackRate("warrior", null);

            // Assert
            Assert.Equal(0.4, attackRate, 4); // Warrior base APS with no haste
        }

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalAttackRate_WithRogue_NoEquipment_ReturnsHighAPS()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var attackRate = calculator.CalculateFinalAttackRate("rogue", null);

            // Assert
            // Rogue base APS = 0.5, initial haste = 10%
            // Final APS = 0.5 × (1 + 10/100) = 0.55
            Assert.Equal(0.55, attackRate, 4);
        }

        [Fact]
        public void CharacterStatsCalculator_GetProfessionBaseStats_ReturnsCorrectStats()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var baseStats = calculator.GetProfessionBaseStats("warrior");

            // Assert
            Assert.NotNull(baseStats);
            Assert.Equal(100, baseStats.BaseAttack);
            Assert.Equal(500, baseStats.BaseHP);
            Assert.Equal(0.4, baseStats.AttackRateAPS);
        }

        [Fact]
        public void CharacterStatsCalculator_GetProfessionInitialCombatStats_ReturnsCorrectStats()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var initialStats = calculator.GetProfessionInitialCombatStats("mage");

            // Assert
            Assert.NotNull(initialStats);
            Assert.Equal(10.0, initialStats.CritChancePercent);
            Assert.Equal(10.0, initialStats.CritDamageBonusPercent);
            Assert.Equal(5.0, initialStats.SpecialAttackPercent);
        }

        #endregion

        #region Character Integration Tests

        [Fact]
        public void Character_GetFinalCombatStats_ReturnsCalculatedStats()
        {
            // Arrange
            var character = new Character
            {
                ActiveCombatProfessionId = "warrior"
            };

            // Act
            var stats = character.GetFinalCombatStats();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(100, stats.AttackFinal);
            Assert.Equal(5.0, stats.CritChancePercent);
        }

        [Fact]
        public void Character_GetFinalMaxHp_ReturnsCalculatedHp()
        {
            // Arrange
            var character = new Character
            {
                ActiveCombatProfessionId = "warrior"
            };

            // Act
            var maxHp = character.GetFinalMaxHp();

            // Assert
            Assert.Equal(500, maxHp);
        }

        [Fact]
        public void Character_GetFinalAttackRate_ReturnsCalculatedRate()
        {
            // Arrange
            var character = new Character
            {
                ActiveCombatProfessionId = "rogue"
            };

            // Act
            var attackRate = character.GetFinalAttackRate();

            // Assert
            Assert.Equal(0.55, attackRate, 4); // Rogue: 0.5 × (1 + 10/100)
        }

        [Fact]
        public void Character_UpdateStatsFromProfessionAndEquipment_UpdatesAllStats()
        {
            // Arrange
            var character = new Character
            {
                ActiveCombatProfessionId = "mage",
                Hp = 1000 // Set high HP to test clamping
            };

            // Act
            character.UpdateStatsFromProfessionAndEquipment();

            // Assert
            Assert.NotNull(character.CombatStats);
            Assert.Equal(300, character.MaxHp); // Mage base HP
            Assert.Equal(300, character.Hp); // Clamped to MaxHp
            Assert.Equal(10.0, character.CritChancePercent);
            Assert.Equal(0.0, character.HastePercent); // Mage has no initial haste
            Assert.Equal(0.06, character.VariancePct, 4); // Mage variance
            Assert.Equal(6000, character.ReviveMs); // Mage revive: 6.0 sec
        }

        [Fact]
        public void Character_UpdateStatsFromProfessionAndEquipment_DifferentProfessions()
        {
            // Arrange & Act - Warrior
            var warrior = new Character { ActiveCombatProfessionId = "warrior" };
            warrior.UpdateStatsFromProfessionAndEquipment();

            // Arrange & Act - Rogue
            var rogue = new Character { ActiveCombatProfessionId = "rogue" };
            rogue.UpdateStatsFromProfessionAndEquipment();

            // Assert - Compare
            Assert.True(warrior.MaxHp > rogue.MaxHp); // Warrior has more HP
            Assert.True(rogue.CritChancePercent > warrior.CritChancePercent); // Rogue has higher crit
            Assert.True(rogue.HastePercent > warrior.HastePercent); // Rogue has haste
        }

        #endregion

        #region Profession Differentiation Tests

        [Theory]
        [InlineData("warrior", 100, 500, 0.4)]
        [InlineData("mage", 80, 300, 0.35)]
        [InlineData("rogue", 70, 350, 0.5)]
        [InlineData("ranger", 85, 400, 0.45)]
        public void CharacterStatsCalculator_ProfessionDifferences_CorrectBaseStats(
            string professionId, int expectedAttack, int expectedHp, double expectedAps)
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var stats = calculator.CalculateFinalStats(professionId, null);
            var maxHp = calculator.CalculateFinalMaxHp(professionId, null);
            var baseStats = calculator.GetProfessionBaseStats(professionId);

            // Assert
            Assert.Equal(expectedAttack, stats.AttackFinal);
            Assert.Equal(expectedHp, maxHp);
            Assert.Equal(expectedAps, baseStats.AttackRateAPS, 4);
        }

        [Theory]
        [InlineData("warrior", 5.0, 0.0, 0.0)]
        [InlineData("mage", 10.0, 10.0, 5.0)]
        [InlineData("rogue", 15.0, 5.0, 0.0)]
        [InlineData("ranger", 8.0, 0.0, 0.0)]
        public void CharacterStatsCalculator_ProfessionDifferences_CorrectInitialStats(
            string professionId, double expectedCrit, double expectedCritBonus, double expectedSpecialAttack)
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var stats = calculator.CalculateFinalStats(professionId, null);

            // Assert
            Assert.Equal(expectedCrit, stats.CritChancePercent, 4);
            Assert.Equal(expectedCritBonus, stats.CritDamageBonusPercent, 4);
            Assert.Equal(expectedSpecialAttack, stats.SpecialAttackPercent, 4);
        }

        #endregion

        #region Caps Clipping Tests

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalStats_CapsAreApplied()
        {
            // Arrange - Create custom caps with low limits
            var caps = new CombatCapsConfig
            {
                AttackPct = 100.0,
                SpecialAttackPct = 80.0,
                HpPercent = 100.0,
                CritChancePct = 10.0, // Lower cap for testing (Rogue has 15%)
                CritDamageBonusPct = 50.0,
                HastePct = 5.0, // Lower cap for testing (Rogue has 10%)
                FortifyMaxPct = 20.0,
                BackwaterMaxPct = 20.0,
                ChasePct = 30.0,
                KenChasePct = 20.0,
                DamageReductionPct = 90.0,
                ChaseFlatCap = 9999
            };
            var calculator = new CharacterStatsCalculator(caps);

            // Act - Rogue has 15% crit and 10% haste
            var stats = calculator.CalculateFinalStats("rogue", null);

            // Assert - Stats should be capped
            Assert.Equal(10.0, stats.CritChancePercent); // Capped from 15 to 10
            Assert.Equal(5.0, stats.HastePercent); // Capped from 10 to 5
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalStats_NullLoadout_HandledGracefully()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();

            // Act
            var stats = calculator.CalculateFinalStats("warrior", null);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(100, stats.AttackFinal);
        }

        [Fact]
        public void CharacterStatsCalculator_CalculateFinalStats_EmptyLoadout_HandledGracefully()
        {
            // Arrange
            var calculator = CharacterStatsCalculator.CreateDefault();
            var loadout = new EquipmentLoadout(); // Empty loadout

            // Act
            var stats = calculator.CalculateFinalStats("warrior", loadout);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(100, stats.AttackFinal); // Just profession base
        }

        [Fact]
        public void Character_GetFinalCombatStats_WithoutEquipmentLoadout_Works()
        {
            // Arrange
            var character = new Character
            {
                ActiveCombatProfessionId = "warrior",
                EquipmentLoadout = null
            };

            // Act
            var stats = character.GetFinalCombatStats();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(100, stats.AttackFinal);
        }

        #endregion
    }
}
