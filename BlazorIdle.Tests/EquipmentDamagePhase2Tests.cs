using BlazorIdle.Game.Combat;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 装备与伤害系统 Phase 2 单元测试 - 伤害计算管线
    /// Equipment and Damage System Phase 2 Unit Tests - Damage Calculation Pipeline
    /// </summary>
    public class EquipmentDamagePhase2Tests
    {
        #region VarianceConfig Tests

        [Fact]
        public void VarianceConfig_CreateDefault_ReturnsValidDefaults()
        {
            // Act
            var config = VarianceConfig.CreateDefault();

            // Assert
            Assert.Equal(0.95, config.DefaultMin);
            Assert.Equal(1.05, config.DefaultMax);
        }

        [Fact]
        public void VarianceConfig_GetVariance_ReturnsValueInRange()
        {
            // Arrange
            var config = VarianceConfig.CreateDefault();
            var rng = new Random(42);

            // Act & Assert - run multiple times to ensure range
            for (int i = 0; i < 100; i++)
            {
                var variance = config.GetVariance(rng);
                Assert.InRange(variance, 0.95, 1.05);
            }
        }

        [Fact]
        public void VarianceConfig_LoadFromRepository_ReturnsValidConfig()
        {
            // Arrange
            CombatConfigRepository.ClearCache();

            // Act
            var config = CombatConfigRepository.LoadVarianceConfig();

            // Assert
            Assert.NotNull(config);
            Assert.InRange(config.DefaultMin, 0.9, 1.0);
            Assert.InRange(config.DefaultMax, 1.0, 1.1);
        }

        #endregion

        #region StanceConfig Tests

        [Fact]
        public void StanceConfig_CreateDefault_ReturnsValidDefaults()
        {
            // Act
            var config = StanceConfig.CreateDefault();

            // Assert
            Assert.Equal(0.75, config.Fortify.ThresholdMin);
            Assert.Equal(0.50, config.Backwater.ThresholdMax);
        }

        [Theory]
        [InlineData(1.0, 20.0, 0.0, 20.0)]    // 100% HP, fortify active (max)
        [InlineData(0.75, 20.0, 0.0, 0.0)]    // 75% HP, fortify threshold (no bonus yet)
        [InlineData(0.875, 20.0, 0.0, 10.0)]  // 87.5% HP, half fortify
        [InlineData(0.60, 0.0, 0.0, 0.0)]     // 60% HP, middle zone (no bonus)
        [InlineData(0.50, 0.0, 20.0, 0.0)]    // 50% HP, backwater threshold (no bonus yet)
        [InlineData(0.25, 0.0, 20.0, 10.204)] // 25% HP, half backwater (approx)
        [InlineData(0.01, 0.0, 20.0, 20.0)]   // 1% HP, backwater max
        public void StanceConfig_CalcStancePercent_CalculatesCorrectly(double hpRatio, double fortifyMax, double backwaterMax, double expectedApprox)
        {
            // Arrange
            var config = StanceConfig.CreateDefault();

            // Act
            var result = config.CalcStancePercent(hpRatio, fortifyMax, backwaterMax);

            // Assert
            Assert.Equal(expectedApprox, result, 1); // precision to 1 decimal place
        }

        [Fact]
        public void StanceConfig_MiddleZone_ReturnsZero()
        {
            // Arrange
            var config = StanceConfig.CreateDefault();

            // Act - HP between 50% and 75%
            var result60 = config.CalcStancePercent(0.60, 20, 20);
            var result55 = config.CalcStancePercent(0.55, 20, 20);
            var result70 = config.CalcStancePercent(0.70, 20, 20);

            // Assert
            Assert.Equal(0, result60);
            Assert.Equal(0, result55);
            Assert.Equal(0, result70);
        }

        [Fact]
        public void StanceConfig_LoadFromRepository_ReturnsValidConfig()
        {
            // Arrange
            CombatConfigRepository.ClearCache();

            // Act
            var config = CombatConfigRepository.LoadStanceConfig();

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.Fortify);
            Assert.NotNull(config.Backwater);
            Assert.InRange(config.Fortify.ThresholdMin, 0.5, 1.0);
            Assert.InRange(config.Backwater.ThresholdMax, 0.0, 0.75);
        }

        #endregion

        #region DamageContext Tests

        [Fact]
        public void DamageContext_CreateSimple_ReturnsValidContext()
        {
            // Act
            var ctx = DamageContext.CreateSimple(100, 1.5, 50);

            // Assert
            Assert.Equal(100, ctx.AttackFinal);
            Assert.Equal(1.5, ctx.SkillCoef);
            Assert.Equal(50, ctx.SkillFlat);
            Assert.Equal(1.0, ctx.AttackerHPRatio);
            Assert.Equal(ElementIds.Neutral, ctx.AttackerElement);
            Assert.Equal(ElementIds.Neutral, ctx.DefenderElement);
            Assert.Equal(0, ctx.DefenderDRPct);
            Assert.NotNull(ctx.AttackerStats);
            Assert.NotNull(ctx.Rng);
        }

        [Fact]
        public void DamageContext_AllPropertiesCanBeSet()
        {
            // Arrange & Act
            var ctx = new DamageContext
            {
                AttackFinal = 500,
                SkillCoef = 2.0,
                SkillFlat = 100,
                AttackerStats = new CombatStats { AttackPercent = 50 },
                AttackerHPRatio = 0.8,
                AttackerElement = ElementIds.Fire,
                DefenderElement = ElementIds.Wind,
                DefenderDRPct = 10
            };

            // Assert
            Assert.Equal(500, ctx.AttackFinal);
            Assert.Equal(2.0, ctx.SkillCoef);
            Assert.Equal(100, ctx.SkillFlat);
            Assert.Equal(50, ctx.AttackerStats.AttackPercent);
            Assert.Equal(0.8, ctx.AttackerHPRatio);
            Assert.Equal(ElementIds.Fire, ctx.AttackerElement);
            Assert.Equal(ElementIds.Wind, ctx.DefenderElement);
            Assert.Equal(10, ctx.DefenderDRPct);
        }

        #endregion

        #region DamageResult Tests

        [Fact]
        public void DamageResult_Empty_ReturnsZeroDamage()
        {
            // Act
            var result = DamageResult.Empty();

            // Assert
            Assert.Equal(0, result.FinalDamage);
            Assert.False(result.IsCrit);
        }

        [Fact]
        public void DamageResult_AllPropertiesCanBeSet()
        {
            // Arrange & Act
            var result = new DamageResult
            {
                FinalDamage = 1000,
                IsCrit = true,
                ElementMultiplier = 1.5,
                HasElementAdvantage = true,
                BaseDamage = 500,
                AfterVariance = 520,
                AfterMain = 800,
                AfterCrit = 1200,
                AfterElement = 1800,
                AfterChase = 2000,
                AfterDefense = 1800,
                StancePercent = 10,
                CritMultiplier = 1.5
            };

            // Assert
            Assert.Equal(1000, result.FinalDamage);
            Assert.True(result.IsCrit);
            Assert.Equal(1.5, result.ElementMultiplier);
            Assert.True(result.HasElementAdvantage);
            Assert.Equal(500, result.BaseDamage);
            Assert.Equal(520, result.AfterVariance);
            Assert.Equal(800, result.AfterMain);
            Assert.Equal(1200, result.AfterCrit);
            Assert.Equal(1800, result.AfterElement);
            Assert.Equal(2000, result.AfterChase);
            Assert.Equal(1800, result.AfterDefense);
            Assert.Equal(10, result.StancePercent);
            Assert.Equal(1.5, result.CritMultiplier);
        }

        #endregion

        #region DamageCalculator Creation Tests

        [Fact]
        public void DamageCalculator_CreateDefault_ReturnsValidCalculator()
        {
            // Act
            var calculator = DamageCalculator.CreateDefault();

            // Assert
            Assert.NotNull(calculator);
        }

        [Fact]
        public void DamageCalculator_CreateFromRepository_ReturnsValidCalculator()
        {
            // Arrange
            CombatConfigRepository.ClearCache();

            // Act
            var calculator = DamageCalculator.CreateFromRepository();

            // Assert
            Assert.NotNull(calculator);
        }

        [Fact]
        public void DamageCalculator_NullDependency_ThrowsArgumentNullException()
        {
            // Arrange
            var caps = CombatCapsConfig.CreateDefault();
            var crit = CritConfig.CreateDefault();
            var variance = VarianceConfig.CreateDefault();
            var stance = StanceConfig.CreateDefault();
            var matrix = ElementMatrix.CreateDefault();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DamageCalculator(null!, crit, variance, stance, matrix));
            Assert.Throws<ArgumentNullException>(() => new DamageCalculator(caps, null!, variance, stance, matrix));
            Assert.Throws<ArgumentNullException>(() => new DamageCalculator(caps, crit, null!, stance, matrix));
            Assert.Throws<ArgumentNullException>(() => new DamageCalculator(caps, crit, variance, null!, matrix));
            Assert.Throws<ArgumentNullException>(() => new DamageCalculator(caps, crit, variance, stance, null!));
        }

        #endregion

        #region DamageCalculator Basic Calculation Tests

        [Fact]
        public void DamageCalculator_Calculate_BaseLayer_CalculatesCorrectly()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = DamageContext.CreateSimple(100, 1.5, 50);

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - Base = 100 * 1.5 + 50 = 200
            Assert.Equal(200, result.BaseDamage);
        }

        [Fact]
        public void DamageCalculator_Calculate_VarianceLayer_AppliesVariance()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = DamageContext.CreateSimple(100, 1.0, 0);

            // Act - deterministic uses midpoint (1.0)
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - AfterVariance = 100 * 1.0 = 100
            Assert.Equal(100, result.AfterVariance);
        }

        [Fact]
        public void DamageCalculator_Calculate_MainLayer_AppliesPercentages()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = new CombatStats
                {
                    AttackPercent = 50,      // +50% attack
                    SpecialAttackPercent = 0
                },
                AttackerHPRatio = 1.0,
                Rng = new Random(42)
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - AfterMain = 100 * 1.5 = 150
            Assert.Equal(150, result.AfterMain);
        }

        [Fact]
        public void DamageCalculator_Calculate_MainLayer_MultipleMultipliers()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = new CombatStats
                {
                    AttackPercent = 50,           // +50% attack
                    SpecialAttackPercent = 40     // +40% special attack
                },
                AttackerHPRatio = 1.0,
                Rng = new Random(42)
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - AfterMain = 100 * 1.5 * 1.4 = 210
            Assert.Equal(210, result.AfterMain);
        }

        #endregion

        #region DamageCalculator Crit Layer Tests

        [Fact]
        public void DamageCalculator_Calculate_CritLayer_NoCrit()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = DamageContext.CreateSimple(100, 1.0, 0);

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            Assert.False(result.IsCrit);
            Assert.Equal(1.0, result.CritMultiplier);
            Assert.Equal(result.AfterMain, result.AfterCrit);
        }

        [Fact]
        public void DamageCalculator_Calculate_CritLayer_WithCrit()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = DamageContext.CreateSimple(100, 1.0, 0);

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: true);

            // Assert - Crit multiplier = 1.2 (base)
            Assert.True(result.IsCrit);
            Assert.Equal(1.2, result.CritMultiplier);
            Assert.Equal(result.AfterMain * 1.2, result.AfterCrit);
        }

        [Fact]
        public void DamageCalculator_Calculate_CritLayer_WithCritBonus()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = new CombatStats
                {
                    CritDamageBonusPercent = 50  // +50% crit damage bonus
                },
                Rng = new Random(42)
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: true);

            // Assert - Crit multiplier = 1.2 * 1.5 = 1.8
            Assert.True(result.IsCrit);
            Assert.Equal(1.8, result.CritMultiplier, 2);
        }

        #endregion

        #region DamageCalculator Element Layer Tests

        [Fact]
        public void DamageCalculator_Calculate_ElementLayer_Neutral()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerElement = ElementIds.Neutral,
                DefenderElement = ElementIds.Neutral
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            Assert.Equal(1.0, result.ElementMultiplier);
            Assert.False(result.HasElementAdvantage);
        }

        [Fact]
        public void DamageCalculator_Calculate_ElementLayer_Advantage()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerElement = ElementIds.Fire,
                DefenderElement = ElementIds.Wind
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            Assert.Equal(1.5, result.ElementMultiplier);
            Assert.True(result.HasElementAdvantage);
        }

        [Fact]
        public void DamageCalculator_Calculate_ElementLayer_Disadvantage()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerElement = ElementIds.Wind,
                DefenderElement = ElementIds.Fire
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            Assert.Equal(0.75, result.ElementMultiplier);
            Assert.False(result.HasElementAdvantage);
        }

        #endregion

        #region DamageCalculator Chase Layer Tests

        [Fact]
        public void DamageCalculator_Calculate_ChaseLayer_NoChase()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = DamageContext.CreateSimple(100, 1.0, 0);

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            Assert.Equal(result.AfterElement, result.AfterChase);
        }

        [Fact]
        public void DamageCalculator_Calculate_ChaseLayer_WithChasePercent()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = new CombatStats
                {
                    ChasePercent = 20  // +20% chase
                }
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - AfterChase = AfterElement * 1.2
            Assert.Equal(result.AfterElement * 1.2, result.AfterChase);
        }

        [Fact]
        public void DamageCalculator_Calculate_ChaseLayer_WithChaseFlat()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = new CombatStats
                {
                    ChaseFlat = 50  // +50 flat chase
                }
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - AfterChase = AfterElement + 50
            Assert.Equal(result.AfterElement + 50, result.AfterChase);
        }

        [Fact]
        public void DamageCalculator_Calculate_ChaseLayer_KenChaseOnlyWithAdvantage()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();

            // Without element advantage
            var ctxNoAdvantage = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerElement = ElementIds.Neutral,
                DefenderElement = ElementIds.Neutral,
                AttackerStats = new CombatStats
                {
                    KenChasePercent = 20
                }
            };

            // With element advantage
            var ctxWithAdvantage = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerElement = ElementIds.Fire,
                DefenderElement = ElementIds.Wind,
                AttackerStats = new CombatStats
                {
                    KenChasePercent = 20
                }
            };

            // Act
            var resultNoAdvantage = calculator.CalculateDeterministic(ctxNoAdvantage, forceCrit: false);
            var resultWithAdvantage = calculator.CalculateDeterministic(ctxWithAdvantage, forceCrit: false);

            // Assert - KenChase only applies with element advantage
            Assert.Equal(resultNoAdvantage.AfterElement, resultNoAdvantage.AfterChase);
            Assert.True(resultWithAdvantage.AfterChase > resultWithAdvantage.AfterElement);
        }

        #endregion

        #region DamageCalculator Defense Layer Tests

        [Fact]
        public void DamageCalculator_Calculate_DefenseLayer_NoDR()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                DefenderDRPct = 0
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            Assert.Equal(result.AfterChase, result.AfterDefense);
        }

        [Fact]
        public void DamageCalculator_Calculate_DefenseLayer_WithDR()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                DefenderDRPct = 20  // 20% damage reduction
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - AfterDefense = AfterChase * 0.8
            Assert.Equal(result.AfterChase * 0.8, result.AfterDefense);
        }

        [Fact]
        public void DamageCalculator_Calculate_DefenseLayer_DRCapped()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                DefenderDRPct = 100  // Over cap (90%)
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - DR capped at 90%, so 10% of damage goes through
            Assert.Equal(result.AfterChase * 0.1, result.AfterDefense, 5); // precision to 5 decimal places
        }

        #endregion

        #region DamageCalculator Stance Tests

        [Fact]
        public void DamageCalculator_Calculate_Stance_Fortify()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerHPRatio = 1.0,  // Full HP
                AttackerStats = new CombatStats
                {
                    FortifyMaxPercent = 20  // Max 20% fortify bonus
                }
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - 20% stance bonus at full HP
            Assert.Equal(20, result.StancePercent);
        }

        [Fact]
        public void DamageCalculator_Calculate_Stance_Backwater()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerHPRatio = 0.01,  // Very low HP
                AttackerStats = new CombatStats
                {
                    BackwaterMaxPercent = 20  // Max 20% backwater bonus
                }
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - ~20% stance bonus at very low HP
            Assert.Equal(20, result.StancePercent, 1);
        }

        [Fact]
        public void DamageCalculator_Calculate_Stance_NoStanceInMiddle()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerHPRatio = 0.60,  // Middle zone
                AttackerStats = new CombatStats
                {
                    FortifyMaxPercent = 20,
                    BackwaterMaxPercent = 20
                }
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - No stance bonus in middle zone
            Assert.Equal(0, result.StancePercent);
        }

        #endregion

        #region DamageCalculator End-to-End Tests

        [Fact]
        public void DamageCalculator_Calculate_MinimumDamageIsOne()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 1,
                SkillCoef = 0.1,
                SkillFlat = 0,
                DefenderDRPct = 90  // Max DR
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert - Minimum damage is 1
            Assert.True(result.FinalDamage >= 1);
        }

        [Fact]
        public void DamageCalculator_Calculate_FullPipeline()
        {
            // Arrange - complex scenario matching design document example
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 1000,
                SkillCoef = 1.2,
                SkillFlat = 80,
                AttackerStats = new CombatStats
                {
                    AttackPercent = 80,
                    SpecialAttackPercent = 40,
                    FortifyMaxPercent = 10,      // Will give 10% bonus at full HP
                    CritDamageBonusPercent = 40,
                    ChasePercent = 15,
                    KenChasePercent = 10,
                    ChaseFlat = 50
                },
                AttackerHPRatio = 1.0,           // Full HP for fortify
                AttackerElement = ElementIds.Fire,
                DefenderElement = ElementIds.Wind,  // Element advantage
                DefenderDRPct = 5
            };

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: true);

            // Assert - verify each layer
            Assert.Equal(1280, result.BaseDamage);                    // 1000*1.2 + 80
            Assert.True(result.AfterVariance > 0);
            Assert.True(result.AfterMain > result.AfterVariance);     // Main layer multiplies
            Assert.True(result.AfterCrit > result.AfterMain);         // Crit adds damage
            Assert.Equal(1.68, result.CritMultiplier, 2);             // 1.2 * 1.4
            Assert.Equal(1.5, result.ElementMultiplier);              // Element advantage
            Assert.True(result.HasElementAdvantage);
            Assert.True(result.AfterChase > result.AfterElement);     // Chase adds damage
            Assert.True(result.AfterDefense < result.AfterChase);     // DR reduces damage
            Assert.True(result.FinalDamage > 0);
        }

        [Fact]
        public void DamageCalculator_Calculate_Randomness()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = new CombatStats { CritChancePercent = 50 },
                Rng = new Random(42)
            };

            // Act - run many calculations
            int critCount = 0;
            for (int i = 0; i < 1000; i++)
            {
                var result = calculator.Calculate(ctx);
                if (result.IsCrit) critCount++;
            }

            // Assert - crit rate should be approximately 50%
            double critRate = critCount / 1000.0;
            Assert.InRange(critRate, 0.4, 0.6);
        }

        #endregion
    }
}
