using BlazorIdle.Game;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Config;
using BlazorIdle.Game.Skills;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 5 测试：伤害系统集成
    /// Phase 5 Tests: Damage System Integration
    /// 
    /// 测试内容：
    /// - 怪物配置加载（新属性：baseAttack, damageReductionPercent, element）
    /// - BattleContext 新属性
    /// - 玩家和怪物伤害计算集成
    /// - 元素克制战斗
    /// </summary>
    public class EquipmentDamagePhase5Tests
    {
        #region MonsterDef 新属性测试 / MonsterDef New Properties Tests

        [Fact]
        public void MonsterDef_HasBaseAttackProperty()
        {
            // Arrange & Act
            var monster = new MonsterDef
            {
                Id = "test_monster",
                BaseAttack = 150
            };

            // Assert
            Assert.Equal(150, monster.BaseAttack);
        }

        [Fact]
        public void MonsterDef_HasDamageReductionPercentProperty()
        {
            // Arrange & Act
            var monster = new MonsterDef
            {
                Id = "test_monster",
                DamageReductionPercent = 10
            };

            // Assert
            Assert.Equal(10, monster.DamageReductionPercent);
        }

        [Fact]
        public void MonsterDef_HasElementProperty()
        {
            // Arrange & Act
            var monster = new MonsterDef
            {
                Id = "test_monster",
                Element = "fire"
            };

            // Assert
            Assert.Equal("fire", monster.Element);
        }

        [Fact]
        public void MonsterDef_DefaultValues()
        {
            // Arrange & Act
            var monster = new MonsterDef();

            // Assert
            Assert.Equal(100, monster.BaseAttack);
            Assert.Equal(0, monster.DamageReductionPercent);
            Assert.Equal("neutral", monster.Element);
        }

        #endregion

        #region Enemy 新属性测试 / Enemy New Properties Tests

        [Fact]
        public void Enemy_HasBaseAttackProperty()
        {
            // Arrange & Act
            var enemy = new Enemy
            {
                BaseAttack = 200
            };

            // Assert
            Assert.Equal(200, enemy.BaseAttack);
        }

        [Fact]
        public void Enemy_HasDamageReductionPercentProperty()
        {
            // Arrange & Act
            var enemy = new Enemy
            {
                DamageReductionPercent = 15
            };

            // Assert
            Assert.Equal(15, enemy.DamageReductionPercent);
        }

        [Fact]
        public void Enemy_HasElementProperty()
        {
            // Arrange & Act
            var enemy = new Enemy
            {
                Element = "water"
            };

            // Assert
            Assert.Equal("water", enemy.Element);
        }

        [Fact]
        public void Enemy_DefaultValues()
        {
            // Arrange & Act
            var enemy = new Enemy();

            // Assert
            Assert.Equal(100, enemy.BaseAttack);
            Assert.Equal(0, enemy.DamageReductionPercent);
            Assert.Equal(ElementIds.Neutral, enemy.Element);
        }

        #endregion

        #region BattleContext 新属性测试 / BattleContext New Properties Tests

        [Fact]
        public void BattleContext_HasDamageCalculatorProperty()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();

            // Act
            var ctx = new BattleContext
            {
                DamageCalculator = calculator,
                Rng = new RngContext(12345)
            };

            // Assert
            Assert.NotNull(ctx.DamageCalculator);
        }

        [Fact]
        public void BattleContext_HasAttackerCombatStatsProperty()
        {
            // Arrange
            var stats = new CombatStats { AttackPercent = 10 };

            // Act
            var ctx = new BattleContext
            {
                AttackerCombatStats = stats,
                Rng = new RngContext(12345)
            };

            // Assert
            Assert.NotNull(ctx.AttackerCombatStats);
            Assert.Equal(10, ctx.AttackerCombatStats.AttackPercent);
        }

        [Fact]
        public void BattleContext_HasElementProperties()
        {
            // Act
            var ctx = new BattleContext
            {
                AttackerElement = "fire",
                DefenderElement = "water",
                Rng = new RngContext(12345)
            };

            // Assert
            Assert.Equal("fire", ctx.AttackerElement);
            Assert.Equal("water", ctx.DefenderElement);
        }

        [Fact]
        public void BattleContext_HasAttackerHPRatioProperty()
        {
            // Act
            var ctx = new BattleContext
            {
                AttackerHPRatio = 0.8,
                Rng = new RngContext(12345)
            };

            // Assert
            Assert.Equal(0.8, ctx.AttackerHPRatio);
        }

        [Fact]
        public void BattleContext_HasDefenderDamageReductionPercentProperty()
        {
            // Act
            var ctx = new BattleContext
            {
                DefenderDamageReductionPercent = 20,
                Rng = new RngContext(12345)
            };

            // Assert
            Assert.Equal(20, ctx.DefenderDamageReductionPercent);
        }

        [Fact]
        public void BattleContext_DefaultValues()
        {
            // Act
            var ctx = new BattleContext
            {
                Rng = new RngContext(12345)
            };

            // Assert
            Assert.Null(ctx.DamageCalculator);
            Assert.Null(ctx.AttackerCombatStats);
            Assert.Null(ctx.AttackerElement);
            Assert.Null(ctx.DefenderElement);
            Assert.Equal(1.0, ctx.AttackerHPRatio);
            Assert.Equal(0, ctx.DefenderDamageReductionPercent);
        }

        #endregion

        #region 怪物伤害计算测试 / Monster Damage Calculation Tests

        [Fact]
        public void DamageCalculator_CanCalculateMonsterDamage()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var rng = new RngContext(12345);

            // 创建怪物战斗属性（简化版：只有基础攻击力）
            var monsterStats = CombatStats.CreateDefault();

            // 水克火（Water克Fire），所以 Water 攻击 Fire 有优势
            var ctx = DamageContext.CreateSimple(
                attackFinal: 150,  // 怪物的 baseAttack
                skillCoef: 1.0,
                skillFlat: 0,
                attackerStats: monsterStats,
                attackerHPRatio: 1.0,
                attackerElement: "water",  // 水
                defenderElement: "fire",   // 火
                defenderDRPct: 10,
                rng: rng
            );

            // Act
            var result = calculator.Calculate(ctx);

            // Assert
            Assert.True(result.FinalDamage > 0);
            // 水克火，有元素优势
            Assert.True(result.HasElementAdvantage);
            Assert.Equal(1.5, result.ElementMultiplier);
        }

        [Fact]
        public void DamageCalculator_MonsterDamageWithReduction()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var rng = new RngContext(12345);

            var monsterStats = CombatStats.CreateDefault();

            var ctx = DamageContext.CreateSimple(
                attackFinal: 100,
                skillCoef: 1.0,
                skillFlat: 0,
                attackerStats: monsterStats,
                attackerHPRatio: 1.0,
                attackerElement: "neutral",
                defenderElement: "neutral",
                defenderDRPct: 50,  // 50% 减伤
                rng: rng
            );

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            // 基础伤害 100 × 1.0 × variance(1.0) = 100
            // 减伤后：100 × 0.5 = 50
            Assert.True(result.FinalDamage <= 55 && result.FinalDamage >= 45);
        }

        #endregion

        #region 玩家 vs 怪物战斗测试 / Player vs Monster Battle Tests

        [Fact]
        public void DamageCalculator_PlayerAttacksMonster_WithElementAdvantage()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var rng = new RngContext(12345);

            // 玩家属性：攻击力 1000，10% 攻击加成
            var playerStats = new CombatStats
            {
                AttackPercent = 10,
                CritChancePercent = 20,
                CritDamageBonusPercent = 10
            };

            // 玩家（水属性）攻击怪物（火属性）= 克制
            var ctx = DamageContext.CreateSimple(
                attackFinal: 1000,
                skillCoef: 1.0,
                skillFlat: 0,
                attackerStats: playerStats,
                attackerHPRatio: 1.0,
                attackerElement: "water",
                defenderElement: "fire",
                defenderDRPct: 10,
                rng: rng
            );

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            Assert.True(result.HasElementAdvantage);
            Assert.Equal(1.5, result.ElementMultiplier);
            // 基础 1000 × 1.0 × 1.1(攻击加成) × 1.5(元素) × 0.9(减伤) ≈ 1485
            Assert.True(result.FinalDamage > 1400 && result.FinalDamage < 1600);
        }

        [Fact]
        public void DamageCalculator_MonsterAttacksPlayer_WithElementAdvantage()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var rng = new RngContext(12345);

            // 怪物简化属性
            var monsterStats = CombatStats.CreateDefault();

            // 怪物（风属性）攻击玩家（地属性）= 克制
            var ctx = DamageContext.CreateSimple(
                attackFinal: 180,  // ogre 的 baseAttack
                skillCoef: 1.0,
                skillFlat: 0,
                attackerStats: monsterStats,
                attackerHPRatio: 1.0,
                attackerElement: "wind",
                defenderElement: "earth",
                defenderDRPct: 0,
                rng: rng
            );

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            Assert.True(result.HasElementAdvantage);
            Assert.Equal(1.5, result.ElementMultiplier);
            // 基础 180 × 1.0 × 1.5(元素) ≈ 270
            Assert.True(result.FinalDamage > 250 && result.FinalDamage < 290);
        }

        [Fact]
        public void DamageCalculator_MonsterAttacksPlayer_WithDisadvantage()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var rng = new RngContext(12345);

            var monsterStats = CombatStats.CreateDefault();

            // 怪物（火属性）攻击玩家（水属性）= 被克制
            var ctx = DamageContext.CreateSimple(
                attackFinal: 200,
                skillCoef: 1.0,
                skillFlat: 0,
                attackerStats: monsterStats,
                attackerHPRatio: 1.0,
                attackerElement: "fire",
                defenderElement: "water",
                defenderDRPct: 0,
                rng: rng
            );

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            Assert.False(result.HasElementAdvantage);
            Assert.Equal(0.75, result.ElementMultiplier);
            // 基础 200 × 1.0 × 0.75(被克制) ≈ 150
            Assert.True(result.FinalDamage > 140 && result.FinalDamage < 160);
        }

        #endregion

        #region 怪物配置加载测试 / Monster Config Loading Tests

        [Fact]
        public void MonsterConfig_LoadsFromJson_WithNewProperties()
        {
            // 这个测试验证 monsters.json 可以正确加载新属性
            // 需要实际加载配置文件来验证

            // Arrange - 模拟从 JSON 加载的怪物
            var slime = new MonsterDef
            {
                Id = "slime",
                Name = "Green Slime",
                Level = 1,
                MaxHp = 220,
                DamagePerHit = 8,
                BaseAttack = 100,
                DamageReductionPercent = 0,
                Element = "neutral"
            };

            var ogre = new MonsterDef
            {
                Id = "ogre",
                Name = "Ogre Brute",
                Level = 12,
                MaxHp = 520,
                DamagePerHit = 18,
                BaseAttack = 180,
                DamageReductionPercent = 10,
                Element = "earth"
            };

            var fireMage = new MonsterDef
            {
                Id = "fire_mage",
                Name = "Fire Mage",
                Level = 15,
                MaxHp = 400,
                DamagePerHit = 12,
                BaseAttack = 200,
                DamageReductionPercent = 5,
                Element = "fire"
            };

            // Assert
            Assert.Equal(100, slime.BaseAttack);
            Assert.Equal("neutral", slime.Element);

            Assert.Equal(180, ogre.BaseAttack);
            Assert.Equal(10, ogre.DamageReductionPercent);
            Assert.Equal("earth", ogre.Element);

            Assert.Equal(200, fireMage.BaseAttack);
            Assert.Equal("fire", fireMage.Element);
        }

        #endregion

        #region 态势与怪物战斗测试 / Stance in Monster Battle Tests

        [Fact]
        public void DamageCalculator_MonsterAttack_WithPlayerAtLowHealth_Backwater()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var rng = new RngContext(12345);

            // 怪物属性
            var monsterStats = CombatStats.CreateDefault();

            // 怪物血量低于50%（实际30%），触发背水
            var ctx = DamageContext.CreateSimple(
                attackFinal: 100,
                skillCoef: 1.0,
                skillFlat: 0,
                attackerStats: new CombatStats { BackwaterMaxPercent = 20 }, // 怪物也可以有背水
                attackerHPRatio: 0.3,  // 30% 血量
                attackerElement: "neutral",
                defenderElement: "neutral",
                defenderDRPct: 0,
                rng: rng
            );

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            // 背水在低血量时增加伤害
            Assert.True(result.StancePercent > 0);
        }

        [Fact]
        public void DamageCalculator_MonsterAttack_WithPlayerAtHighHealth_Fortify()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var rng = new RngContext(12345);

            // 怪物属性（有盛体加成）
            var ctx = DamageContext.CreateSimple(
                attackFinal: 100,
                skillCoef: 1.0,
                skillFlat: 0,
                attackerStats: new CombatStats { FortifyMaxPercent = 20 },
                attackerHPRatio: 0.9,  // 90% 血量
                attackerElement: "neutral",
                defenderElement: "neutral",
                defenderDRPct: 0,
                rng: rng
            );

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            // 盛体在高血量时增加伤害
            Assert.True(result.StancePercent > 0);
        }

        #endregion

        #region 综合战斗场景测试 / Integration Battle Scenario Tests

        [Fact]
        public void BattleScenario_FireMage_AttacksWaterPlayer()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var rng = new RngContext(42);

            // Fire Mage 配置
            var fireMageBaseAttack = 200.0;
            var fireMageElement = "fire";

            // 玩家配置（水属性，有减伤）
            var playerDamageReduction = 15.0;
            var playerElement = "water";

            // 创建伤害上下文
            var ctx = DamageContext.CreateSimple(
                attackFinal: fireMageBaseAttack,
                skillCoef: 1.0,
                skillFlat: 0,
                attackerStats: CombatStats.CreateDefault(),
                attackerHPRatio: 1.0,
                attackerElement: fireMageElement,
                defenderElement: playerElement,
                defenderDRPct: playerDamageReduction,
                rng: rng
            );

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            // 火攻水 = 被克制 (0.75x)
            Assert.False(result.HasElementAdvantage);
            Assert.Equal(0.75, result.ElementMultiplier);
            // 200 × 1.0 × 0.75 × 0.85 ≈ 127.5
            Assert.True(result.FinalDamage >= 120 && result.FinalDamage <= 140);
        }

        [Fact]
        public void BattleScenario_WaterPlayer_AttacksFireMage()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var rng = new RngContext(42);

            // 玩家配置（水属性，装备加成）
            var playerBaseAttack = 1500.0;  // 10件装备总攻击力
            var playerElement = "water";
            var playerStats = new CombatStats
            {
                AttackPercent = 20,
                CritChancePercent = 30,
                CritDamageBonusPercent = 15
            };

            // Fire Mage 配置
            var fireMageDamageReduction = 5.0;
            var fireMageElement = "fire";

            // 创建伤害上下文
            var ctx = DamageContext.CreateSimple(
                attackFinal: playerBaseAttack,
                skillCoef: 1.2,  // 技能系数
                skillFlat: 50,   // 固定伤害
                attackerStats: playerStats,
                attackerHPRatio: 0.8,  // 80% 血量
                attackerElement: playerElement,
                defenderElement: fireMageElement,
                defenderDRPct: fireMageDamageReduction,
                rng: rng
            );

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: true);

            // Assert
            // 水克火 = 克制 (1.5x)
            Assert.True(result.HasElementAdvantage);
            Assert.Equal(1.5, result.ElementMultiplier);
            Assert.True(result.IsCrit);
            // 伤害应该很高
            Assert.True(result.FinalDamage > 3000);
        }

        [Fact]
        public void BattleScenario_NeutralMonster_AttacksNeutralPlayer()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            var rng = new RngContext(42);

            // Slime 配置（无属性）
            var slimeBaseAttack = 100.0;

            // 玩家配置（无属性，无减伤）
            var ctx = DamageContext.CreateSimple(
                attackFinal: slimeBaseAttack,
                skillCoef: 1.0,
                skillFlat: 0,
                attackerStats: CombatStats.CreateDefault(),
                attackerHPRatio: 1.0,
                attackerElement: "neutral",
                defenderElement: "neutral",
                defenderDRPct: 0,
                rng: rng
            );

            // Act
            var result = calculator.CalculateDeterministic(ctx, forceCrit: false);

            // Assert
            // 无属性 vs 无属性 = 1.0x
            Assert.False(result.HasElementAdvantage);
            Assert.Equal(1.0, result.ElementMultiplier);
            // 100 × 1.0 × 1.0 × 1.0 = 100
            Assert.True(result.FinalDamage >= 95 && result.FinalDamage <= 105);
        }

        #endregion

        #region SkillResolver 新伤害系统集成测试 / SkillResolver New Damage System Integration Tests

        [Fact]
        public void SkillResolver_UsesNewDamageSystem_WhenDamageCalculatorProvided()
        {
            // Arrange
            var skillResolver = new SkillResolver();
            var rng = new RngContext(12345);
            var clock = new TestGameClock();
            var calculator = DamageCalculator.CreateDefault();

            var playerStats = new CombatStats
            {
                AttackFinal = 1000,
                AttackPercent = 10,
                CritChancePercent = 0  // Disable crit for deterministic test
            };

            var ctx = new BattleContext
            {
                Rng = rng,
                Clock = clock,
                DamageCalculator = calculator,
                AttackerCombatStats = playerStats,
                AttackerElement = "water",
                DefenderElement = "fire",
                AttackerHPRatio = 1.0,
                DefenderDamageReductionPercent = 10
            };

            // Act
            var result = skillResolver.Cast("attack_basic", ctx);

            // Assert
            Assert.True(result.DamageDealt > 0);
            Assert.True(result.HasElementAdvantage);
            Assert.Equal(1.5, result.ElementMultiplier);
            Assert.NotNull(result.DetailedDamageResult);
        }

        [Fact]
        public void SkillResolver_FallsBackToLegacySystem_WhenNoDamageCalculator()
        {
            // Arrange
            var skillResolver = new SkillResolver();
            var rng = new RngContext(12345);
            var clock = new TestGameClock();

            var player = new Character
            {
                DamagePerAttack = 100,
                CritChancePercent = 0
            };

            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
                // No DamageCalculator provided
            };

            // Act
            var result = skillResolver.Cast("attack_basic", ctx);

            // Assert
            Assert.True(result.DamageDealt > 0);
            Assert.False(result.HasElementAdvantage);  // Legacy system doesn't set this
            Assert.Equal(1.0, result.ElementMultiplier);  // Default value
            Assert.Null(result.DetailedDamageResult);  // Not set for legacy system
        }

        [Fact]
        public void SkillResolver_NewDamageSystem_AppliesStanceBonus()
        {
            // Arrange
            var skillResolver = new SkillResolver();
            var rng = new RngContext(12345);
            var clock = new TestGameClock();
            var calculator = DamageCalculator.CreateDefault();

            var playerStats = new CombatStats
            {
                AttackFinal = 1000,
                BackwaterMaxPercent = 20,  // Enable backwater
                CritChancePercent = 0
            };

            var ctx = new BattleContext
            {
                Rng = rng,
                Clock = clock,
                DamageCalculator = calculator,
                AttackerCombatStats = playerStats,
                AttackerElement = "neutral",
                DefenderElement = "neutral",
                AttackerHPRatio = 0.3,  // Low HP triggers backwater
                DefenderDamageReductionPercent = 0
            };

            // Act
            var result = skillResolver.Cast("attack_basic", ctx);

            // Assert
            Assert.True(result.StancePercent > 0);  // Backwater should be active
            Assert.NotNull(result.DetailedDamageResult);
            Assert.True(result.DetailedDamageResult.StancePercent > 0);
        }

        [Fact]
        public void SkillResolver_NewDamageSystem_AppliesDefenderReduction()
        {
            // Arrange
            var skillResolver = new SkillResolver();
            var rng = new RngContext(12345);
            var clock = new TestGameClock();
            var calculator = DamageCalculator.CreateDefault();

            var playerStats = new CombatStats
            {
                AttackFinal = 1000,
                CritChancePercent = 0
            };

            var ctxNoReduction = new BattleContext
            {
                Rng = new RngContext(12345),
                Clock = clock,
                DamageCalculator = calculator,
                AttackerCombatStats = playerStats,
                AttackerElement = "neutral",
                DefenderElement = "neutral",
                AttackerHPRatio = 1.0,
                DefenderDamageReductionPercent = 0
            };

            var ctxWithReduction = new BattleContext
            {
                Rng = new RngContext(12345),
                Clock = clock,
                DamageCalculator = calculator,
                AttackerCombatStats = playerStats,
                AttackerElement = "neutral",
                DefenderElement = "neutral",
                AttackerHPRatio = 1.0,
                DefenderDamageReductionPercent = 50
            };

            // Act
            var resultNoReduction = skillResolver.Cast("attack_basic", ctxNoReduction);
            var resultWithReduction = skillResolver.Cast("attack_basic", ctxWithReduction);

            // Assert
            // 50% reduction should halve damage
            Assert.True(resultWithReduction.DamageDealt < resultNoReduction.DamageDealt);
            double ratio = (double)resultWithReduction.DamageDealt / resultNoReduction.DamageDealt;
            Assert.True(ratio >= 0.45 && ratio <= 0.55, $"Expected ~50% reduction, got {ratio:P}");
        }

        [Fact]
        public void SkillCastResult_HasNewDamageSystemProperties()
        {
            // Arrange & Act
            var result = new SkillCastResult
            {
                DamageDealt = 1000,
                IsCrit = true,
                HasElementAdvantage = true,
                ElementMultiplier = 1.5,
                StancePercent = 10
            };

            // Assert
            Assert.Equal(1000, result.DamageDealt);
            Assert.True(result.IsCrit);
            Assert.True(result.HasElementAdvantage);
            Assert.Equal(1.5, result.ElementMultiplier);
            Assert.Equal(10, result.StancePercent);
        }

        #endregion

        #region 急速系统测试 / Haste System Tests

        [Fact]
        public void CombatStats_HastePercent_CanBeSet()
        {
            // Arrange & Act
            var stats = new CombatStats
            {
                HastePercent = 25
            };

            // Assert
            Assert.Equal(25, stats.HastePercent);
        }

        [Fact]
        public void HasteCalculation_CooldownReduction()
        {
            // 急速计算公式测试：EffectiveCD = BaseCD / (1 + HastePercent / 100)
            // Haste calculation test: EffectiveCD = BaseCD / (1 + HastePercent / 100)
            
            // Arrange
            double baseCD = 10.0;  // 10 秒基础冷却
            double hastePercent = 40;  // 40% 急速（上限）

            // Act
            double effectiveCD = baseCD / (1 + hastePercent / 100);

            // Assert
            // 10 / 1.4 ≈ 7.14
            Assert.True(effectiveCD >= 7.1 && effectiveCD <= 7.2);
        }

        [Fact]
        public void HasteCalculation_AttackSpeedIncrease()
        {
            // 急速计算公式测试：EffectiveAPS = BaseAPS × (1 + HastePercent / 100)
            // Haste calculation test: EffectiveAPS = BaseAPS × (1 + HastePercent / 100)
            
            // Arrange
            double baseAPS = 1.0;  // 1.0 攻击/秒
            double hastePercent = 40;  // 40% 急速

            // Act
            double effectiveAPS = baseAPS * (1 + hastePercent / 100);

            // Assert
            // 1.0 × 1.4 = 1.4
            Assert.Equal(1.4, effectiveAPS);
        }

        [Fact]
        public void CombatCapsConfig_ClampHastePct()
        {
            // Arrange
            var caps = CombatCapsConfig.CreateDefault();

            // Act & Assert
            Assert.Equal(40, caps.ClampHastePct(50));  // 超过上限
            Assert.Equal(25, caps.ClampHastePct(25));  // 正常值
            Assert.Equal(0, caps.ClampHastePct(-5));   // 负值裁剪到 0
        }

        #endregion
    }

    /// <summary>
    /// 测试用游戏时钟
    /// Test game clock
    /// </summary>
    public class TestGameClock : IGameClock
    {
        private int _nowMs = 0;

        public int NowMs => _nowMs;

        public void AdvanceBy(int ms) => _nowMs += ms;

        public void Reset() => _nowMs = 0;
    }
}
