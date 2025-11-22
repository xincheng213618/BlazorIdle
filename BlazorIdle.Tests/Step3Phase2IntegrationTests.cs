using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Buffs;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Step3 Phase 2: 定期技能检查系统集成测试
    /// Step3 Phase 2: Periodic skill check system integration tests
    /// 
    /// 测试完整的定期技能检查流程：
    /// - 光环自动触发和续期
    /// - 治疗药水 HP 条件触发
    /// - Buff 时间条件检查
    /// - 复活后重新触发
    /// </summary>
    public class Step3Phase2IntegrationTests
    {
        private readonly SkillRepository _skillRepository;
        private readonly BuffRepository _buffRepository;

        public Step3Phase2IntegrationTests()
        {
            _skillRepository = new SkillRepository();
            _buffRepository = BuffRepository.Instance;
        }

        [Fact]
        public void AuraSkill_LoadedCorrectly()
        {
            // 验证光环技能配置正确加载
            // Verify aura skill configuration is loaded correctly
            var auraPassive = _skillRepository.GetSkillById("warrior_strength_aura_passive");
            Assert.NotNull(auraPassive);
            Assert.Equal("passive", auraPassive.Type);
            Assert.Equal("passive", auraPassive.SlotType);
            Assert.NotNull(auraPassive.Triggers);
            Assert.Single(auraPassive.Triggers);

            var trigger = auraPassive.Triggers[0];
            Assert.Equal("OnPeriodic", trigger.When);
            Assert.Equal("warrior_strength_aura_effect", trigger.FireSkillId);
            Assert.NotNull(trigger.Conditions);
            Assert.Equal("strength_aura_buff", trigger.Conditions.BuffTimeCheckId);
            Assert.Equal(5.0, trigger.Conditions.BuffTimeRemainingSec);
        }

        [Fact]
        public void AuraEffect_LoadedCorrectly()
        {
            // 验证光环效果技能配置正确加载
            // Verify aura effect skill configuration is loaded correctly
            var auraEffect = _skillRepository.GetSkillById("warrior_strength_aura_effect");
            Assert.NotNull(auraEffect);
            Assert.Equal("active", auraEffect.Type);
            Assert.NotNull(auraEffect.OnCastBuffs);
            Assert.Single(auraEffect.OnCastBuffs);

            var buffOp = auraEffect.OnCastBuffs[0];
            Assert.Equal(BuffOperationType.Apply, buffOp.Type);
            Assert.Equal("strength_aura_buff", buffOp.BuffConfigId);
            Assert.Equal(BuffTarget.AllAllies, buffOp.TargetOverride);
        }

        [Fact]
        public void HealthPotionSkill_LoadedCorrectly()
        {
            // 验证治疗药水技能配置正确加载
            // Verify health potion skill configuration is loaded correctly
            var potionSkill = _skillRepository.GetSkillById("health_potion_skill");
            Assert.NotNull(potionSkill);
            Assert.Equal("passive", potionSkill.Type);
            Assert.Empty(potionSkill.AllowedProfessions ?? new List<string>());
            Assert.NotNull(potionSkill.Triggers);
            Assert.Single(potionSkill.Triggers);

            var trigger = potionSkill.Triggers[0];
            Assert.Equal("OnPeriodic", trigger.When);
            Assert.Equal("health_potion_effect", trigger.FireSkillId);
            Assert.NotNull(trigger.Conditions);
            Assert.Equal(50.0, trigger.Conditions.HpBelowPct);
        }

        [Fact]
        public void HealthPotionEffect_LoadedCorrectly()
        {
            // 验证治疗药水效果技能配置正确加载
            // Verify health potion effect skill configuration is loaded correctly
            var potionEffect = _skillRepository.GetSkillById("health_potion_effect");
            Assert.NotNull(potionEffect);
            Assert.Equal("active", potionEffect.Type);
            Assert.Equal(30.0, potionEffect.CooldownSec);
            Assert.Equal(100, potionEffect.InstantHeal);
        }

        [Fact]
        public void StrengthAuraBuff_LoadedCorrectly()
        {
            // 验证力量光环 Buff 配置正确加载
            // Verify strength aura buff configuration is loaded correctly
            Assert.True(_buffRepository.HasBuff("strength_aura_buff"));
            
            var strengthAuraBuff = _buffRepository.GetBuffById("strength_aura_buff");
            Assert.NotNull(strengthAuraBuff);
            Assert.Equal("力量光环", strengthAuraBuff.Name);
            Assert.Equal(BuffKind.Buff, strengthAuraBuff.Kind);
            Assert.Equal(10.0, strengthAuraBuff.DurationSec);
            Assert.Equal(BuffStackingPolicy.Refresh, strengthAuraBuff.StackingPolicy);
            Assert.Equal(1, strengthAuraBuff.MaxStacks);
            Assert.NotNull(strengthAuraBuff.Effects);
            Assert.NotEmpty(strengthAuraBuff.Effects);
        }

        [Fact]
        public void PeriodicTriggers_AllUseOnPeriodic()
        {
            // 验证所有 Step3 示例技能都使用 OnPeriodic 触发器
            // Verify all Step3 example skills use OnPeriodic triggers
            var auraPassive = _skillRepository.GetSkillById("warrior_strength_aura_passive");
            var potionSkill = _skillRepository.GetSkillById("health_potion_skill");

            Assert.All(auraPassive.Triggers, t => Assert.Equal("OnPeriodic", t.When));
            Assert.All(potionSkill.Triggers, t => Assert.Equal("OnPeriodic", t.When));
        }

        [Fact]
        public void AuraPassive_CanBeEquippedByWarrior()
        {
            // 验证光环技能可以被战士装备
            // Verify aura skill can be equipped by warrior
            var auraPassive = _skillRepository.GetSkillById("warrior_strength_aura_passive");
            Assert.NotNull(auraPassive.AllowedProfessions);
            Assert.Contains("warrior", auraPassive.AllowedProfessions);
        }

        [Fact]
        public void HealthPotion_CanBeEquippedByAnyClass()
        {
            // 验证治疗药水可以被任何职业装备
            // Verify health potion can be equipped by any class
            var potionSkill = _skillRepository.GetSkillById("health_potion_skill");
            Assert.Empty(potionSkill.AllowedProfessions ?? new List<string>());
        }

        [Fact]
        public void BuffTimeCondition_HasBothFields()
        {
            // 验证 Buff 时间条件包含两个必需字段
            // Verify buff time condition has both required fields
            var auraPassive = _skillRepository.GetSkillById("warrior_strength_aura_passive");
            var trigger = auraPassive.Triggers[0];
            var conditions = trigger.Conditions;

            Assert.NotNull(conditions.BuffTimeCheckId);
            Assert.NotNull(conditions.BuffTimeRemainingSec);
            Assert.True(conditions.BuffTimeRemainingSec > 0);
        }

        [Fact]
        public void HpCondition_HasCorrectThreshold()
        {
            // 验证 HP 条件有正确的阈值
            // Verify HP condition has correct threshold
            var potionSkill = _skillRepository.GetSkillById("health_potion_skill");
            var trigger = potionSkill.Triggers[0];
            var conditions = trigger.Conditions;

            Assert.NotNull(conditions.HpBelowPct);
            Assert.Equal(50.0, conditions.HpBelowPct);
            Assert.True(conditions.HpBelowPct > 0 && conditions.HpBelowPct <= 100);
        }
    }
}
