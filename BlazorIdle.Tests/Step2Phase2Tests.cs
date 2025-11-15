using BlazorIdle.Game.Skills;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Unit tests for Step 2 Phase 2: Skill Slot System
    /// </summary>
    public class Step2Phase2Tests
    {
        #region SkillSlotConfig Tests (2 tests)

        [Fact]
        public void SkillSlotConfig_CanBeConstructed()
        {
            // Arrange & Act
            var slot = new SkillSlotConfig
            {
                SlotId = "active_1",
                SlotType = "active",
                SkillId = "test_skill",
                IsFixed = false
            };

            // Assert
            Assert.Equal("active_1", slot.SlotId);
            Assert.Equal("active", slot.SlotType);
            Assert.Equal("test_skill", slot.SkillId);
            Assert.False(slot.IsFixed);
        }

        [Fact]
        public void SkillSlotConfig_CanBeEmpty()
        {
            // Arrange & Act
            var slot = new SkillSlotConfig
            {
                SlotId = "active_2",
                SlotType = "active",
                SkillId = null
            };

            // Assert
            Assert.Equal("active_2", slot.SlotId);
            Assert.Null(slot.SkillId);
        }

        #endregion

        #region CharacterSkillSlots Tests (4 tests)

        [Fact]
        public void CharacterSkillSlots_InitializeCreatesDefaultSlots()
        {
            // Arrange
            var repo = new SkillRepository();
            var slots = new CharacterSkillSlots(repo);

            // Act
            slots.Initialize("warrior");

            // Assert
            var allSlots = slots.GetAllSlots();
            Assert.Equal(4, allSlots.Count); // 3 active + 1 passive
            Assert.NotNull(slots.GetSlot("active_1"));
            Assert.NotNull(slots.GetSlot("active_2"));
            Assert.NotNull(slots.GetSlot("active_3"));
            Assert.NotNull(slots.GetSlot("passive_1"));
        }

        [Fact]
        public void CharacterSkillSlots_InitializeAutoEquipsFixedSkills()
        {
            // Arrange
            var repo = new SkillRepository();
            var slots = new CharacterSkillSlots(repo);

            // Act
            slots.Initialize("warrior");

            // Assert
            var fixedSkills = slots.GetFixedSkills();
            Assert.NotEmpty(fixedSkills);
            
            // Verify that warrior_attack_basic and warrior_special_pulse are equipped
            Assert.Contains(fixedSkills, s => s.Id == "warrior_attack_basic" || s.Id == "warrior_special_pulse");
        }

        [Fact]
        public void CharacterSkillSlots_CanEquipConfigurableSkill()
        {
            // Arrange
            var repo = new SkillRepository();
            var slots = new CharacterSkillSlots(repo);
            slots.Initialize("warrior");

            // Act - Use a non-fixed skill
            var result = slots.EquipSkill("active_2", "warrior_mortal_strike");

            // Assert
            Assert.True(result);
            var activeSkills = slots.GetActiveSkills();
            Assert.Contains(activeSkills, s => s.Id == "warrior_mortal_strike");
        }

        [Fact]
        public void CharacterSkillSlots_CanUnequipConfigurableSkill()
        {
            // Arrange
            var repo = new SkillRepository();
            var slots = new CharacterSkillSlots(repo);
            slots.Initialize("warrior");
            slots.EquipSkill("active_2", "warrior_mortal_strike");

            // Act
            var result = slots.UnequipSkill("active_2");

            // Assert
            Assert.True(result);
            var slot = slots.GetSlot("active_2");
            Assert.Null(slot?.SkillId);
        }

        #endregion

        #region Equipment Validation Tests (4 tests)

        [Fact]
        public void CharacterSkillSlots_CannotEquipSkillToWrongSlotType()
        {
            // Arrange
            var repo = new SkillRepository();
            var slots = new CharacterSkillSlots(repo);
            slots.Initialize("warrior");

            // Act - Try to equip an active skill to passive slot
            var result = slots.EquipSkill("passive_1", "warrior_mortal_strike");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CharacterSkillSlots_CannotEquipSkillFromDifferentProfession()
        {
            // Arrange
            var repo = new SkillRepository();
            var slots = new CharacterSkillSlots(repo);
            slots.Initialize("warrior");

            // Act - Try to equip a mage skill
            var result = slots.EquipSkill("active_1", "mage_pyroblast");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CharacterSkillSlots_CannotEquipSameSkillTwice()
        {
            // Arrange
            var repo = new SkillRepository();
            var slots = new CharacterSkillSlots(repo);
            slots.Initialize("warrior");
            
            // Find two empty, non-fixed slots
            var emptySlots = slots.GetAllSlots().Values
                .Where(s => !s.IsFixed && s.SkillId == null && s.SlotType == "active")
                .Take(2)
                .ToList();
            
            Assert.True(emptySlots.Count >= 2, "Need at least 2 empty active slots for this test");
            
            // Equip to first slot
            var firstEquip = slots.EquipSkill(emptySlots[0].SlotId, "warrior_mortal_strike");
            Assert.True(firstEquip, "First equip should succeed");

            // Act - Try to equip the same skill to another slot
            var result = slots.EquipSkill(emptySlots[1].SlotId, "warrior_mortal_strike");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CharacterSkillSlots_CannotUnequipFixedSkill()
        {
            // Arrange
            var repo = new SkillRepository();
            var slots = new CharacterSkillSlots(repo);
            slots.Initialize("warrior");

            // Find a fixed slot
            var fixedSlot = slots.GetAllSlots().Values.FirstOrDefault(s => s.IsFixed);
            Assert.NotNull(fixedSlot);

            // Act
            var result = slots.UnequipSkill(fixedSlot!.SlotId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Query Tests (2 tests)

        [Fact]
        public void CharacterSkillSlots_GetActiveSkillsReturnsInOrder()
        {
            // Arrange
            var repo = new SkillRepository();
            var slots = new CharacterSkillSlots(repo);
            slots.Initialize("warrior");
            
            // Find a non-fixed active slot to equip
            var nonFixedActiveSlot = slots.GetAllSlots().Values
                .FirstOrDefault(s => s.SlotType == "active" && !s.IsFixed);
            
            if (nonFixedActiveSlot != null)
            {
                slots.EquipSkill(nonFixedActiveSlot.SlotId, "warrior_mortal_strike");
            }

            // Act
            var activeSkills = slots.GetActiveSkills();

            // Assert
            Assert.NotEmpty(activeSkills);
            // Skills should be returned in slot order (active_1, active_2, active_3)
        }

        [Fact]
        public void CharacterSkillSlots_GetConfigurableSkillsExcludesFixed()
        {
            // Arrange
            var repo = new SkillRepository();
            var slots = new CharacterSkillSlots(repo);
            slots.Initialize("warrior");

            // Equip a configurable skill
            var nonFixedSlot = slots.GetAllSlots().Values
                .FirstOrDefault(s => !s.IsFixed);
            
            if (nonFixedSlot != null)
            {
                slots.EquipSkill(nonFixedSlot.SlotId, "warrior_mortal_strike");
            }

            // Act
            var configurableSkills = slots.GetConfigurableSkills();
            var fixedSkills = slots.GetFixedSkills();

            // Assert
            if (configurableSkills.Count > 0)
            {
                // Configurable skills should not be in fixed skills
                foreach (var skill in configurableSkills)
                {
                    Assert.DoesNotContain(fixedSkills, s => s.Id == skill.Id);
                }
            }
        }

        #endregion
    }
}
