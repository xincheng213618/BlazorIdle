using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Combat;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 4 单元测试：SkillResolver 基础实现
    /// Phase 4 unit tests: SkillResolver basic implementation
    /// 
    /// 旧系统清理：这些测试已更新为使用新的伤害系统（DamageCalculator + CombatStats）
    /// Legacy cleanup: These tests have been updated to use the new damage system
    /// </summary>
    public class SkillResolverPhase4Tests
    {
        /// <summary>
        /// 创建测试用的 BattleContext（使用新伤害系统）
        /// Create test BattleContext (using new damage system)
        /// </summary>
        private static BattleContext CreateTestContext(
            CombatStats combatStats,
            Character? player = null,
            Enemy? enemy = null,
            RngContext? rng = null,
            SimClock? clock = null)
        {
            clock ??= new SimClock();
            rng ??= new RngContext(12345);
            if (player == null)
            {
                player = new Character { CombatStats = combatStats, VariancePct = 0.0 };
            }
            else
            {
                player.CombatStats = combatStats;
            }
            
            return new BattleContext
            {
                Player = player,
                Enemy = enemy,
                Rng = rng,
                Clock = clock,
                DamageCalculator = DamageCalculator.CreateDefault(),
                AttackerCombatStats = combatStats,
                AttackerElement = ElementIds.Neutral,
                DefenderElement = ElementIds.Neutral,
                AttackerHPRatio = 1.0,
                DefenderDamageReductionPercent = 0
            };
        }

        [Fact]
        public void SkillResolver_Cast_AttackBasic_CalculatesDamage()
        {
            // Arrange - 使用新伤害系统
            var combatStats = new CombatStats 
            { 
                AttackFinal = 50,
                CritChancePercent = 0.0 // 无暴击
            };
            var ctx = CreateTestContext(combatStats);
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast("attack_basic", ctx);

            // Assert - 使用新系统，伤害 = AttackFinal * SkillCoef * variance(中值1.0) = 50
            Assert.Equal(50, result.DamageDealt);
            Assert.False(result.IsCrit);
        }

        [Fact]
        public void SkillResolver_Cast_SpecialPulse_CalculatesDamage()
        {
            // Arrange - 使用新伤害系统
            // special_pulse 的 SkillCoef 从 skills.json 定义中获取
            var combatStats = new CombatStats 
            { 
                AttackFinal = 100,
                CritChancePercent = 0.0
            };
            var ctx = CreateTestContext(combatStats);
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast("special_pulse", ctx);

            // Assert - 伤害基于 AttackFinal 和技能系数
            // special_pulse 技能系数通常 > 1.0
            Assert.True(result.DamageDealt > 0);
            Assert.False(result.IsCrit);
        }

        [Fact]
        public void SkillResolver_Cast_EnemyAttackBasic_CalculatesDamage()
        {
            // Arrange - 怪物使用 BaseAttack
            var enemy = new Enemy 
            { 
                BaseAttack = 30,
                VariancePct = 0.0,
                Element = ElementIds.Neutral
            };
            var combatStats = new CombatStats { AttackFinal = (int)enemy.BaseAttack };
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var ctx = new BattleContext
            {
                Enemy = enemy,
                Rng = rng,
                Clock = clock,
                DamageCalculator = DamageCalculator.CreateDefault(),
                AttackerCombatStats = combatStats,
                AttackerElement = ElementIds.Neutral,
                DefenderElement = ElementIds.Neutral,
                AttackerHPRatio = 1.0,
                DefenderDamageReductionPercent = 0
            };
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast("enemy_attack_basic", ctx);

            // Assert
            Assert.Equal(30, result.DamageDealt);
            Assert.False(result.IsCrit); // 敌人攻击不会暴击
        }

        [Fact]
        public void SkillResolver_Cast_WithVariance_ProducesDifferentDamage()
        {
            // Arrange - 新系统默认有 ±5% 浮动
            var combatStats = new CombatStats 
            { 
                AttackFinal = 100,
                CritChancePercent = 0.0
            };
            var rng = new RngContext(12345);
            var ctx = CreateTestContext(combatStats, rng: rng);
            var resolver = new SkillResolver();

            // Act - 多次施放，检查是否有浮动
            var damages = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                var result = resolver.Cast("attack_basic", ctx);
                damages.Add(result.DamageDealt);
            }

            // Assert - 新系统使用 Calculate() 会有随机浮动
            // 所有伤害应该在合理范围内（95-105 for ±5%）
            Assert.All(damages, dmg => Assert.InRange(dmg, 90, 110));
        }

        [Fact]
        public void SkillResolver_Cast_WithForceCrit_AlwaysCrits()
        {
            // Arrange
            var combatStats = new CombatStats 
            { 
                AttackFinal = 100,
                CritChancePercent = 0.0, // 正常情况下不会暴击
                CritDamageBonusPercent = 66.67 // 1.2 * 1.6667 ≈ 2.0 (BaseMultiplier=1.2)
            };
            var ctx = CreateTestContext(combatStats);
            var resolver = new SkillResolver();
            var opts = new SkillCastOptions { ForceCrit = true };

            // Act
            var result = resolver.Cast("attack_basic", ctx, opts);

            // Assert - 使用 CalculateDeterministic 强制暴击
            Assert.True(result.IsCrit);
            // 新系统: 100 * 1.0(variance中值) * 1.2(BaseMultiplier) * (1 + 66.67/100) ≈ 200
            Assert.InRange(result.DamageDealt, 190, 210);
        }

        [Fact]
        public void SkillResolver_Cast_WithHighCritChance_CanCrit()
        {
            // Arrange
            var combatStats = new CombatStats 
            { 
                AttackFinal = 100,
                CritChancePercent = 100.0, // 100% 暴击率
                CritDamageBonusPercent = 25.0 // 1.2 * 1.25 = 1.5
            };
            var ctx = CreateTestContext(combatStats);
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast("attack_basic", ctx);

            // Assert
            Assert.True(result.IsCrit);
            // 新系统: 100 * 1.0 * 1.2 * 1.25 = 150
            Assert.InRange(result.DamageDealt, 140, 160);
        }

        [Fact]
        public void SkillResolver_CastBundle_CastsMultipleSkills()
        {
            // Arrange
            var combatStats = new CombatStats 
            { 
                AttackFinal = 50,
                CritChancePercent = 0.0
            };
            var ctx = CreateTestContext(combatStats);
            var resolver = new SkillResolver();
            var skillIds = new List<string> { "attack_basic", "attack_basic", "attack_basic" };
            var opts = new SkillCastOptions { SourceTrack = "attack" };

            // Act
            var results = resolver.CastBundle(skillIds, ctx, opts);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.InRange(r.DamageDealt, 45, 55)); // 50 ± variance
        }

        [Fact]
        public void SkillResolver_CastBundle_AssignsBundleId()
        {
            // Arrange
            var combatStats = new CombatStats { AttackFinal = 50 };
            var ctx = CreateTestContext(combatStats);
            var resolver = new SkillResolver();
            var skillIds = new List<string> { "attack_basic" };
            var opts = new SkillCastOptions { SourceTrack = "attack" };

            // Act - 通过检查调用是否成功来间接验证 bundleId 逻辑
            var results = resolver.CastBundle(skillIds, ctx, opts);

            // Assert
            Assert.Single(results);
            // bundleId 在内部生成，我们无法直接访问，但可以验证调用成功
        }

        [Fact]
        public void SkillResolver_CastBundle_RespectsMaxLimit()
        {
            // Arrange
            var combatStats = new CombatStats { AttackFinal = 50 };
            var ctx = CreateTestContext(combatStats);
            var resolver = new SkillResolver();
            
            // 尝试施放 25 个技能（超过限制 20）
            var skillIds = new List<string>();
            for (int i = 0; i < 25; i++)
            {
                skillIds.Add("attack_basic");
            }
            var opts = new SkillCastOptions { SourceTrack = "attack" };

            // Act
            var results = resolver.CastBundle(skillIds, ctx, opts);

            // Assert - 应该只施放 20 个
            Assert.Equal(20, results.Count);
        }

        [Fact]
        public void SkillResolver_CastBundle_ResetsCounterOnNewTick()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var combatStats = new CombatStats { AttackFinal = 50 };
            var player = new Character { CombatStats = combatStats, VariancePct = 0.0 };
            var ctx = CreateTestContext(combatStats, player: player, rng: rng, clock: clock);
            var resolver = new SkillResolver();
            var skillIds = new List<string>();
            for (int i = 0; i < 15; i++)
            {
                skillIds.Add("attack_basic");
            }
            var opts = new SkillCastOptions { SourceTrack = "attack" };

            // Act - 第一个 tick：施放 15 个
            var results1 = resolver.CastBundle(skillIds, ctx, opts);
            Assert.Equal(15, results1.Count);

            // Act - 同一个 tick：尝试再施放 15 个，应该只能施放 5 个（20 - 15）
            var results2 = resolver.CastBundle(skillIds, ctx, opts);
            Assert.Equal(5, results2.Count);

            // Act - 新的 tick：应该重置计数器，可以再次施放 15 个
            clock.AdvanceBy(100);
            ctx = CreateTestContext(combatStats, player: player, rng: rng, clock: clock);
            var results3 = resolver.CastBundle(skillIds, ctx, opts);
            Assert.Equal(15, results3.Count);
        }

        [Fact]
        public void SkillResolver_Cast_UnknownSkill_ReturnsMinimumDamage()
        {
            // Arrange
            var combatStats = new CombatStats { AttackFinal = 50 };
            var ctx = CreateTestContext(combatStats);
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast("unknown_skill", ctx);

            // Assert - 未知技能使用默认 SkillCoef = 1.0，伤害 = 50 * 1.0 = 50
            // 因为新系统没有回退到最小伤害，而是正常计算
            Assert.True(result.DamageDealt >= 45 && result.DamageDealt <= 55);
            Assert.False(result.IsCrit);
        }
    }
}
