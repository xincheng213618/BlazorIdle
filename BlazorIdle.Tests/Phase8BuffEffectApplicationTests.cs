using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Buffs;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 8: 测试 Buff 效果应用到属性计算
    /// Phase 8: Test buff effects applied to attribute calculations
    /// </summary>
    public class Phase8BuffEffectApplicationTests
    {
        /// <summary>
        /// 创建测试用的战斗上下文
        /// Create battle context for testing
        /// </summary>
        private BattleContext CreateTestContext(
            int baseDamage = 100,
            double critChance = 0.0,
            double critMultiplier = 2.0,
            CharacterBuffOwner? buffOwner = null)
        {
            var clock = new SimClock();
            var rng = new RngContext(42);
            
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = baseDamage,
                AttackRateAPS = 1.0,
                CritChancePercent = critChance,
                CritMultiplier = critMultiplier,
                HastePercent = 0.0,
                VariancePct = 0.0, // No variance for predictable tests
                SpecialIntervalSec = 10.0,
                SpecialDamage = 100,
                ActiveCombatProfessionId = "warrior"
            };

            return new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock,
                PlayerBuffOwner = buffOwner
            };
        }

        [Fact]
        public void StatMultiplier_IncreasesBaseDamage()
        {
            // Arrange - 创建 +50% 伤害的 buff
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            var buff = new BuffInstance(
                id: "damage_boost",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.5) // +50% damage
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            );
            buffOwner.ApplyBuff(buff);

            var ctx = CreateTestContext(baseDamage: 100, buffOwner: buffOwner);
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert - 100 * 1.5 = 150
            Assert.Equal(150, result.DamageDealt);
        }

        [Fact]
        public void StatAdditive_AddsFlatDamage()
        {
            // Arrange - 创建 +30 固定伤害的 buff
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            var buff = new BuffInstance(
                id: "flat_damage",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("DamagePerAttack", 30) // +30 flat damage
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            );
            buffOwner.ApplyBuff(buff);

            var ctx = CreateTestContext(baseDamage: 100, buffOwner: buffOwner);
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert - 100 + 30 = 130
            Assert.Equal(130, result.DamageDealt);
        }

        [Fact]
        public void StatReduction_ReducesBaseDamage()
        {
            // Arrange - 创建 -20% 伤害的 debuff
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            var buff = new BuffInstance(
                id: "weaken",
                ownerId: "player1",
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatReduction("DamagePerAttack", 0.2) // -20% damage
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            );
            buffOwner.ApplyBuff(buff);

            var ctx = CreateTestContext(baseDamage: 100, buffOwner: buffOwner);
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert - 100 * 0.8 = 80
            Assert.Equal(80, result.DamageDealt);
        }

        [Fact]
        public void MultipleBuffs_StackCorrectly()
        {
            // Arrange - 创建多个 buff：+50% 伤害，+20 固定伤害
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            
            // Buff 1: +50% multiplier
            var buff1 = new BuffInstance(
                id: "damage_boost",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.5)
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            );
            buffOwner.ApplyBuff(buff1);

            // Buff 2: +20 additive
            var buff2 = new BuffInstance(
                id: "flat_damage",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("DamagePerAttack", 20)
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            );
            buffOwner.ApplyBuff(buff2);

            var ctx = CreateTestContext(baseDamage: 100, buffOwner: buffOwner);
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert - (100 * 1.5) + 20 = 150 + 20 = 170
            Assert.Equal(170, result.DamageDealt);
        }

        [Fact]
        public void ForceCrit_ForcesNextAttackToCrit()
        {
            // Arrange - 创建强制暴击 buff，基础暴击率为 0%
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                CritChancePercent = 0.0, // 0% 暴击率
                CritMultiplier = 2.0,
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            var buff = new BuffInstance(
                id: "force_crit",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.ForceCrit()
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            );
            buffOwner.ApplyBuff(buff);

            var ctx = CreateTestContext(
                baseDamage: 100,
                critChance: 0.0,
                critMultiplier: 2.0,
                buffOwner: buffOwner
            );
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert - 应该暴击：100 * 2.0 = 200
            Assert.True(result.IsCrit);
            Assert.Equal(200, result.DamageDealt);
        }

        [Fact]
        public void CritChanceBuff_IncreaseCritRate()
        {
            // Arrange - 创建 +100% 暴击率的 buff（保证暴击）
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                CritChancePercent = 0.0, // 0% 基础暴击率
                CritMultiplier = 2.0,
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            var buff = new BuffInstance(
                id: "crit_boost",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatAdditive("CritChancePercent", 100.0) // +100% 暴击率
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            );
            buffOwner.ApplyBuff(buff);

            var ctx = CreateTestContext(
                baseDamage: 100,
                critChance: 0.0,
                critMultiplier: 2.0,
                buffOwner: buffOwner
            );
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert - 应该暴击（0% + 100% = 100% 暴击率）
            Assert.True(result.IsCrit);
            Assert.Equal(200, result.DamageDealt);
        }

        [Fact]
        public void CritMultiplierBuff_IncreaseCritDamage()
        {
            // Arrange - 创建 +50% 暴击倍率的 buff
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                CritChancePercent = 100.0, // 100% 暴击率保证暴击
                CritMultiplier = 2.0,
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            var buff = new BuffInstance(
                id: "crit_damage_boost",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("CritMultiplier", 0.5) // +50% 暴击倍率
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            );
            buffOwner.ApplyBuff(buff);

            var ctx = CreateTestContext(
                baseDamage: 100,
                critChance: 100.0,
                critMultiplier: 2.0,
                buffOwner: buffOwner
            );
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert - 暴击倍率从 2.0 变为 3.0：100 * 3.0 = 300
            Assert.True(result.IsCrit);
            Assert.Equal(300, result.DamageDealt);
        }

        [Fact]
        public void NoBuffOwner_NormalDamageCalculation()
        {
            // Arrange - 没有 buff owner
            var ctx = CreateTestContext(baseDamage: 100, buffOwner: null);
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert - 基础伤害 100
            Assert.Equal(100, result.DamageDealt);
        }

        [Fact]
        public void EmptyBuffs_NormalDamageCalculation()
        {
            // Arrange - 有 buff owner 但没有 buff
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            var ctx = CreateTestContext(baseDamage: 100, buffOwner: buffOwner);
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert - 基础伤害 100
            Assert.Equal(100, result.DamageDealt);
        }

        [Fact]
        public void SpecialSkill_AppliesBuffEffects()
        {
            // Arrange - 测试 Special 技能也应用 buff 效果
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 50,
                SpecialDamage = 100, // Special 技能基础伤害
                CritChancePercent = 0.0,
                VariancePct = 0.0, // No variance for predictable tests
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            var buff = new BuffInstance(
                id: "special_boost",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("SpecialDamage", 0.5) // +50% Special 伤害
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            );
            buffOwner.ApplyBuff(buff);

            var ctx = new BattleContext
            {
                Player = player,
                Rng = new RngContext(42),
                Clock = new SimClock(),
                PlayerBuffOwner = buffOwner
            };
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.SpecialPulse, ctx);

            // Assert - 100 * 1.5 = 150
            Assert.Equal(150, result.DamageDealt);
        }

        [Fact]
        public void BuffsOnlyAffectTargetedStats()
        {
            // Arrange - 创建只影响 SpecialDamage 的 buff，不应影响普通攻击
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                SpecialDamage = 100,
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            var buff = new BuffInstance(
                id: "special_only",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("SpecialDamage", 1.0) // +100% Special 伤害
                },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            );
            buffOwner.ApplyBuff(buff);

            var ctx = CreateTestContext(baseDamage: 100, buffOwner: buffOwner);
            var resolver = new SkillResolver();

            // Act - 使用普通攻击
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert - 普通攻击不应受影响，仍然是 100
            Assert.Equal(100, result.DamageDealt);
        }

        [Fact]
        public void ComplexBuffInteraction_MultipleEffectsAndCrit()
        {
            // Arrange - 复杂场景：多个 buff + 暴击
            var player = new Character
            {
                MaxHp = 1000,
                Hp = 1000,
                DamagePerAttack = 100,
                CritChancePercent = 100.0, // 保证暴击
                CritMultiplier = 2.0,
                ActiveCombatProfessionId = "warrior"
            };

            var buffOwner = new CharacterBuffOwner(player, "player1");
            
            // Buff 1: +30% 基础伤害
            buffOwner.ApplyBuff(new BuffInstance(
                id: "damage_boost",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.3) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            ));

            // Buff 2: +10 固定伤害
            buffOwner.ApplyBuff(new BuffInstance(
                id: "flat_damage",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.StatAdditive("DamagePerAttack", 10) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            ));

            // Buff 3: +50% 暴击倍率
            buffOwner.ApplyBuff(new BuffInstance(
                id: "crit_boost",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.StatMultiplier("CritMultiplier", 0.5) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 10.0
            ));

            var ctx = CreateTestContext(
                baseDamage: 100,
                critChance: 100.0,
                critMultiplier: 2.0,
                buffOwner: buffOwner
            );
            var resolver = new SkillResolver();

            // Act
            var result = resolver.Cast(SkillIds.AttackBasic, ctx);

            // Assert
            // 基础伤害: 100
            // 应用 +30% 倍率: 100 * 1.3 = 130
            // 应用 +10 固定: 130 + 10 = 140
            // 暴击倍率: 2.0 * 1.5 = 3.0
            // 最终伤害: 140 * 3.0 = 420
            Assert.True(result.IsCrit);
            Assert.Equal(420, result.DamageDealt);
        }
    }
}
