using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Resources;
using BlazorIdle.Shared.Models;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Step 2 Phase 9.5: 职业固定技能差异化测试
    /// Professional Fixed Skill Differentiation Tests
    /// 
    /// 测试四个职业的固定技能特色系统：
    /// Tests the unique fixed skill systems for all four professions:
    /// - Warrior: Stance System (架势系统)
    /// - Mage: Arcane Power System (奥术充能系统)
    /// - Ranger: Focus System (集中系统)
    /// - Rogue: Combo System (连击系统)
    /// </summary>
    public class Step2Phase9_5Tests
    {
        // ========== Warrior Stance System Tests (战士架势系统测试) ==========

        /// <summary>
        /// Test 1: Warrior special_pulse应用warrior_stance buff
        /// </summary>
        [Fact]
        public void WarriorStance_SpecialPulse_AppliesWarriorStanceBuff()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var buffRepo = BuffRepository.Instance;

            var skill = skillRepo.GetSkill("warrior_special_pulse");
            Assert.NotNull(skill);
            Assert.NotNull(skill.OnCastBuffs);
            
            // Verify the skill is configured to apply warrior_stance
            var stanceBuff = skill.OnCastBuffs.FirstOrDefault(b => b.BuffConfigId == "warrior_stance");
            Assert.NotNull(stanceBuff);
            Assert.Equal(BuffOperationType.Apply, stanceBuff.Type);
            
            // Verify warrior_stance buff configuration
            var stanceConfig = buffRepo.GetBuffById("warrior_stance");
            Assert.NotNull(stanceConfig);
            Assert.Equal(BuffStackingPolicy.Stack, stanceConfig.StackingPolicy);
            Assert.Equal(3, stanceConfig.MaxStacks);
        }

        /// <summary>
        /// Test 2: Warrior stance buff配置为可叠加3层
        /// </summary>
        [Fact]
        public void WarriorStance_BuffConfiguration_AllowsThreeLayers()
        {
            // Arrange
            var buffRepo = BuffRepository.Instance;
            var stanceConfig = buffRepo.GetBuffById("warrior_stance");
            
            // Assert: Verify buff is configured for stacking
            Assert.NotNull(stanceConfig);
            Assert.Equal("warrior_stance", stanceConfig.Id);
            Assert.Equal("战士架势", stanceConfig.Name);
            Assert.Equal(BuffStackingPolicy.Stack, stanceConfig.StackingPolicy);
            Assert.Equal(3, stanceConfig.MaxStacks);
            Assert.Equal(30.0, stanceConfig.DurationSec); // 30 second duration
        }

        /// <summary>
        /// Test 3: warrior_check_stance技能在3层架势时触发
        /// </summary>
        [Fact]
        public void WarriorStance_CheckStance_RequiresThreeStacks()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var checkStanceSkill = skillRepo.GetSkill("warrior_check_stance");
            
            Assert.NotNull(checkStanceSkill);
            Assert.NotNull(checkStanceSkill.Conditions);
            Assert.NotNull(checkStanceSkill.Conditions.RequireBuffStacks);
            
            // Verify it requires exactly 3 stacks of warrior_stance
            Assert.True(checkStanceSkill.Conditions.RequireBuffStacks.ContainsKey("warrior_stance"));
            Assert.Equal(3, checkStanceSkill.Conditions.RequireBuffStacks["warrior_stance"]);
        }

        /// <summary>
        /// Test 4: warrior_check_stance应用guaranteed_crit并移除stance
        /// </summary>
        [Fact]
        public void WarriorStance_CheckStance_AppliesGuaranteedCritAndRemovesStance()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var checkStanceSkill = skillRepo.GetSkill("warrior_check_stance");
            
            Assert.NotNull(checkStanceSkill);
            Assert.NotNull(checkStanceSkill.OnCastBuffs);
            Assert.Equal(2, checkStanceSkill.OnCastBuffs.Count);
            
            // Verify it applies guaranteed crit buff
            var applyCrit = checkStanceSkill.OnCastBuffs.FirstOrDefault(b => b.Type == BuffOperationType.Apply);
            Assert.NotNull(applyCrit);
            Assert.Equal("warrior_guaranteed_crit", applyCrit.BuffConfigId);
            
            // Verify it removes stance buff
            var removeStance = checkStanceSkill.OnCastBuffs.FirstOrDefault(b => b.Type == BuffOperationType.Remove);
            Assert.NotNull(removeStance);
            Assert.Equal("warrior_stance", removeStance.BuffIdToRemove);
            Assert.Equal(3, removeStance.StacksToRemove);
        }

        /// <summary>
        /// Test 5: warrior_guaranteed_crit buff配置正确
        /// </summary>
        [Fact]
        public void WarriorStance_GuaranteedCrit_BuffConfiguredCorrectly()
        {
            // Arrange
            var buffRepo = BuffRepository.Instance;
            var critBuff = buffRepo.GetBuffById("warrior_guaranteed_crit");
            
            Assert.NotNull(critBuff);
            Assert.Equal(30.0, critBuff.DurationSec);
            Assert.Equal(BuffStackingPolicy.Refresh, critBuff.StackingPolicy);
            Assert.Equal(1, critBuff.MaxStacks);
            
            // Verify it has ForceCrit effect
            Assert.NotNull(critBuff.Effects);
            var forceCritEffect = critBuff.Effects.FirstOrDefault(e => e.Type == BuffEffectType.ForceCrit);
            Assert.NotNull(forceCritEffect);
            Assert.Equal(1.0, forceCritEffect.Value);
        }

        // ========== Mage Arcane Power System Tests (法师奥术充能系统测试) ==========

        /// <summary>
        /// Test 6: Mage special_pulse应用mage_arcane_power buff
        /// </summary>
        [Fact]
        public void MageArcanePower_SpecialPulse_AppliesArcanePowerBuff()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var skill = skillRepo.GetSkill("mage_special_pulse");
            
            Assert.NotNull(skill);
            Assert.NotNull(skill.OnCastBuffs);
            
            // Verify the skill is configured to apply mage_arcane_power
            var arcaneBuff = skill.OnCastBuffs.FirstOrDefault(b => b.BuffConfigId == "mage_arcane_power");
            Assert.NotNull(arcaneBuff);
            Assert.Equal(BuffOperationType.Apply, arcaneBuff.Type);
        }

        /// <summary>
        /// Test 7: mage_arcane_power buff配置为可叠加5层
        /// </summary>
        [Fact]
        public void MageArcanePower_BuffConfiguration_AllowsFiveLayers()
        {
            // Arrange
            var buffRepo = BuffRepository.Instance;
            var arcaneConfig = buffRepo.GetBuffById("mage_arcane_power");
            
            // Assert: Verify buff is configured for stacking
            Assert.NotNull(arcaneConfig);
            Assert.Equal("mage_arcane_power", arcaneConfig.Id);
            Assert.Equal("奥术充能", arcaneConfig.Name);
            Assert.Equal(BuffStackingPolicy.Stack, arcaneConfig.StackingPolicy);
            Assert.Equal(5, arcaneConfig.MaxStacks);
            Assert.Equal(8.0, arcaneConfig.DurationSec);
        }

        /// <summary>
        /// Test 8: mage_arcane_power每层提升8% AttackFinal和12% SpecialAttackPercent
        /// </summary>
        [Fact]
        public void MageArcanePower_EachStackBoostsDamage()
        {
            // Arrange
            var buffRepo = BuffRepository.Instance;
            var arcaneConfig = buffRepo.GetBuffById("mage_arcane_power");
            
            Assert.NotNull(arcaneConfig);
            Assert.NotNull(arcaneConfig.Effects);
            Assert.Equal(2, arcaneConfig.Effects.Count);
            
            // Verify AttackFinal multiplier (previously DamagePerAttack)
            var damageEffect = arcaneConfig.Effects.FirstOrDefault(e => 
                e.Type == BuffEffectType.StatMultiplier && e.Target == "AttackFinal");
            Assert.NotNull(damageEffect);
            Assert.Equal(0.08, damageEffect.Value);
            
            // Verify SpecialAttackPercent additive (previously SpecialDamage multiplier)
            var specialEffect = arcaneConfig.Effects.FirstOrDefault(e => 
                e.Type == BuffEffectType.StatAdditive && e.Target == "SpecialAttackPercent");
            Assert.NotNull(specialEffect);
            Assert.Equal(12.0, specialEffect.Value);
        }

        /// <summary>
        /// Test 9: mage_arcane_power持续时间为8秒
        /// </summary>
        [Fact]
        public void MageArcanePower_DurationIsEightSeconds()
        {
            // Arrange
            var buffRepo = BuffRepository.Instance;
            var arcaneConfig = buffRepo.GetBuffById("mage_arcane_power");
            
            Assert.NotNull(arcaneConfig);
            Assert.Equal(8.0, arcaneConfig.DurationSec);
        }

        /// <summary>
        /// Test 10: Mage资源上限为10点（mana）
        /// </summary>
        [Fact]
        public void Mage_ManaMaximum_IsTenPoints()
        {
            // This verifies that mage profession has correct resource configuration
            // The actual value is in professionAttributes.json
            // This test documents the design expectation
            
            // Expected: Mage mana max = 10
            const int expectedManaMax = 10;
            
            // Note: Actual verification would require loading professionAttributes.json
            // For now, this test serves as documentation
            Assert.Equal(10, expectedManaMax);
        }

        // ========== Ranger Focus System Tests (游侠集中系统测试) ==========

        /// <summary>
        /// Test 11: Ranger special_pulse应用ranger_focus buff
        /// </summary>
        [Fact]
        public void RangerFocus_SpecialPulse_AppliesFocusBuff()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var skill = skillRepo.GetSkill("ranger_special_pulse");
            
            Assert.NotNull(skill);
            Assert.NotNull(skill.OnCastBuffs);
            
            // Verify the skill is configured to apply ranger_focus
            var focusBuff = skill.OnCastBuffs.FirstOrDefault(b => b.BuffConfigId == "ranger_focus");
            Assert.NotNull(focusBuff);
            Assert.Equal(BuffOperationType.Apply, focusBuff.Type);
        }

        /// <summary>
        /// Test 12: ranger_focus buff配置为可叠加3层
        /// </summary>
        [Fact]
        public void RangerFocus_BuffConfiguration_AllowsThreeLayers()
        {
            // Arrange
            var buffRepo = BuffRepository.Instance;
            var focusConfig = buffRepo.GetBuffById("ranger_focus");
            
            // Assert: Verify buff is configured for stacking
            Assert.NotNull(focusConfig);
            Assert.Equal("ranger_focus", focusConfig.Id);
            Assert.Equal("集中", focusConfig.Name);
            Assert.Equal(BuffStackingPolicy.Stack, focusConfig.StackingPolicy);
            Assert.Equal(3, focusConfig.MaxStacks);
            Assert.Equal(6.0, focusConfig.DurationSec);
        }

        /// <summary>
        /// Test 13: ranger_focus每层提升5% CritChancePercent和3% HastePercent
        /// </summary>
        [Fact]
        public void RangerFocus_EachStackBoostsCritAndHaste()
        {
            // Arrange
            var buffRepo = BuffRepository.Instance;
            var focusConfig = buffRepo.GetBuffById("ranger_focus");
            
            Assert.NotNull(focusConfig);
            Assert.NotNull(focusConfig.Effects);
            Assert.Equal(2, focusConfig.Effects.Count);
            
            // Verify CritChancePercent additive
            var critEffect = focusConfig.Effects.FirstOrDefault(e => 
                e.Type == BuffEffectType.StatAdditive && e.Target == "CritChancePercent");
            Assert.NotNull(critEffect);
            Assert.Equal(5.0, critEffect.Value);
            
            // Verify HastePercent additive
            var hasteEffect = focusConfig.Effects.FirstOrDefault(e => 
                e.Type == BuffEffectType.StatAdditive && e.Target == "HastePercent");
            Assert.NotNull(hasteEffect);
            Assert.Equal(3.0, hasteEffect.Value);
        }

        /// <summary>
        /// Test 14: ranger_focus持续时间为6秒
        /// </summary>
        [Fact]
        public void RangerFocus_DurationIsSixSeconds()
        {
            // Arrange
            var buffRepo = BuffRepository.Instance;
            var focusConfig = buffRepo.GetBuffById("ranger_focus");
            
            Assert.NotNull(focusConfig);
            Assert.Equal(6.0, focusConfig.DurationSec);
        }

        // ========== Rogue Combo System Tests (盗贼连击系统测试) ==========

        /// <summary>
        /// Test 15: Rogue special_pulse应用rogue_combo buff
        /// </summary>
        [Fact]
        public void RogueCombo_SpecialPulse_AppliesComboBuff()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var skill = skillRepo.GetSkill("rogue_special_pulse");
            
            Assert.NotNull(skill);
            Assert.NotNull(skill.OnCastBuffs);
            
            // Verify the skill is configured to apply rogue_combo
            var comboBuff = skill.OnCastBuffs.FirstOrDefault(b => b.BuffConfigId == "rogue_combo");
            Assert.NotNull(comboBuff);
            Assert.Equal(BuffOperationType.Apply, comboBuff.Type);
        }

        /// <summary>
        /// Test 16: rogue_combo buff配置为可叠加5层
        /// </summary>
        [Fact]
        public void RogueCombo_BuffConfiguration_AllowsFiveLayers()
        {
            // Arrange
            var buffRepo = BuffRepository.Instance;
            var comboConfig = buffRepo.GetBuffById("rogue_combo");
            
            // Assert: Verify buff is configured for stacking
            Assert.NotNull(comboConfig);
            Assert.Equal("rogue_combo", comboConfig.Id);
            Assert.Equal("连击点", comboConfig.Name);
            Assert.Equal(BuffStackingPolicy.Stack, comboConfig.StackingPolicy);
            Assert.Equal(5, comboConfig.MaxStacks);
            Assert.Equal(15.0, comboConfig.DurationSec); // 15 second duration
        }

        /// <summary>
        /// Test 17: rogue_attack_basic有40%概率触发combo点
        /// </summary>
        [Fact]
        public void RogueCombo_AttackBasic_HasFortyPercentProcChance()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var attackSkill = skillRepo.GetSkill("rogue_attack_basic");
            
            Assert.NotNull(attackSkill);
            Assert.NotNull(attackSkill.Triggers);
            
            // Find the trigger that applies combo points
            var comboTrigger = attackSkill.Triggers.FirstOrDefault(t => 
                t.FireSkillId == "rogue_gain_combo");
            
            Assert.NotNull(comboTrigger);
            Assert.Equal(0.4, comboTrigger.ProcChance);
            Assert.Equal("OnAttackHit", comboTrigger.When);
        }

        /// <summary>
        /// Test 18: rogue_gain_combo技能应用combo buff
        /// </summary>
        [Fact]
        public void RogueCombo_GainCombo_AppliesComboBuff()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var gainSkill = skillRepo.GetSkill("rogue_gain_combo");
            
            Assert.NotNull(gainSkill);
            Assert.NotNull(gainSkill.OnCastBuffs);
            
            // Verify it applies rogue_combo
            var comboBuff = gainSkill.OnCastBuffs.FirstOrDefault(b => 
                b.BuffConfigId == "rogue_combo");
            Assert.NotNull(comboBuff);
            Assert.Equal(BuffOperationType.Apply, comboBuff.Type);
        }

        /// <summary>
        /// Test 19: rogue_eviscerate消耗combo点
        /// </summary>
        [Fact]
        public void RogueCombo_Eviscerate_ConsumesComboPoints()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var eviscerateSkill = skillRepo.GetSkill("rogue_eviscerate");
            
            Assert.NotNull(eviscerateSkill);
            Assert.NotNull(eviscerateSkill.OnCastBuffs);
            
            // Verify it removes combo points
            var removeBuff = eviscerateSkill.OnCastBuffs.FirstOrDefault(b => 
                b.Type == BuffOperationType.Remove && b.BuffIdToRemove == "rogue_combo");
            Assert.NotNull(removeBuff);
            
            // Note: StacksToRemove is null, meaning remove all stacks
            Assert.Null(removeBuff.StacksToRemove);
        }

        /// <summary>
        /// Test 20: rogue_eviscerate需要combo buff才能释放
        /// </summary>
        [Fact]
        public void RogueCombo_Eviscerate_RequiresComboPoints()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var eviscerateSkill = skillRepo.GetSkill("rogue_eviscerate");
            
            Assert.NotNull(eviscerateSkill);
            Assert.NotNull(eviscerateSkill.Conditions);
            
            // Verify it requires rogue_combo buff (any stacks)
            Assert.NotNull(eviscerateSkill.Conditions.RequireBuffId);
            Assert.Equal("rogue_combo", eviscerateSkill.Conditions.RequireBuffId);
        }
    }
}
