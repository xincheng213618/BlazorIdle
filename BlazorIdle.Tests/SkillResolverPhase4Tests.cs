using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 4 单元测试：SkillResolver 基础实现
    /// Phase 4 unit tests: SkillResolver basic implementation
    /// </summary>
    public class SkillResolverPhase4Tests
    {
        [Fact]
        public void SkillResolver_Cast_AttackBasic_CalculatesDamage()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character 
            { 
                DamagePerAttack = 50, 
                VariancePct = 0.0, // 无浮动，便于测试
                CritChancePercent = 0.0 // 无暴击
            };
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast("attack_basic", ctx);

            // Assert
            Assert.Equal(50, result.DamageDealt);
            Assert.False(result.IsCrit);
        }

        [Fact]
        public void SkillResolver_Cast_SpecialPulse_CalculatesDamage()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character 
            { 
                SpecialDamage = 120, 
                VariancePct = 0.0,
                CritChancePercent = 0.0
            };
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast("special_pulse", ctx);

            // Assert
            Assert.Equal(120, result.DamageDealt);
            Assert.False(result.IsCrit);
        }

        [Fact]
        public void SkillResolver_Cast_EnemyAttackBasic_CalculatesDamage()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var enemy = new Enemy 
            { 
                DamagePerHit = 30, 
                VariancePct = 0.0
            };
            var ctx = new BattleContext
            {
                Enemy = enemy,
                Rng = rng,
                Clock = clock
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
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character 
            { 
                DamagePerAttack = 100, 
                VariancePct = 0.1, // ±10%
                CritChancePercent = 0.0
            };
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
            var resolver = new SkillResolver();

            // Act - 多次施放，检查是否有浮动
            var damages = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                var result = resolver.Cast("attack_basic", ctx);
                damages.Add(result.DamageDealt);
            }

            // Assert - 应该至少有一些不同的值（由于浮动）
            Assert.True(damages.Distinct().Count() > 1);
            // 所有伤害应该在 90-110 范围内
            Assert.All(damages, dmg => Assert.InRange(dmg, 85, 115));
        }

        [Fact]
        public void SkillResolver_Cast_WithForceCrit_AlwaysCrits()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character 
            { 
                DamagePerAttack = 100, 
                VariancePct = 0.0,
                CritChancePercent = 0.0, // 正常情况下不会暴击
                CritMultiplier = 2.0
            };
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
            var resolver = new SkillResolver();
            var opts = new SkillCastOptions { ForceCrit = true };

            // Act
            var result = resolver.Cast("attack_basic", ctx, opts);

            // Assert
            Assert.True(result.IsCrit);
            Assert.Equal(200, result.DamageDealt); // 100 * 2.0
        }

        [Fact]
        public void SkillResolver_Cast_WithHighCritChance_CanCrit()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character 
            { 
                DamagePerAttack = 100, 
                VariancePct = 0.0,
                CritChancePercent = 100.0, // 100% 暴击率
                CritMultiplier = 1.5
            };
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast("attack_basic", ctx);

            // Assert
            Assert.True(result.IsCrit);
            Assert.Equal(150, result.DamageDealt); // 100 * 1.5
        }

        [Fact]
        public void SkillResolver_CastBundle_CastsMultipleSkills()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character 
            { 
                DamagePerAttack = 50, 
                VariancePct = 0.0,
                CritChancePercent = 0.0
            };
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
            var resolver = new SkillResolver();
            var skillIds = new List<string> { "attack_basic", "attack_basic", "attack_basic" };
            var opts = new SkillCastOptions { SourceTrack = "attack" };

            // Act
            var results = resolver.CastBundle(skillIds, ctx, opts);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.Equal(50, r.DamageDealt));
        }

        [Fact]
        public void SkillResolver_CastBundle_AssignsBundleId()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character { DamagePerAttack = 50, VariancePct = 0.0 };
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
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
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character { DamagePerAttack = 50, VariancePct = 0.0 };
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
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
            var player = new Character { DamagePerAttack = 50, VariancePct = 0.0 };
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
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
            ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
            var results3 = resolver.CastBundle(skillIds, ctx, opts);
            Assert.Equal(15, results3.Count);
        }

        [Fact]
        public void SkillResolver_Cast_UnknownSkill_ReturnsMinimumDamage()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character { DamagePerAttack = 50 };
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast("unknown_skill", ctx);

            // Assert - 未知技能返回最小伤害 1（代码中确保 dmg >= 1）
            Assert.Equal(1, result.DamageDealt);
            Assert.False(result.IsCrit);
        }
    }
}
