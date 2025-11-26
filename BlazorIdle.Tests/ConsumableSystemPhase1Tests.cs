using System.Text.Json;
using BlazorIdle.Shared.Models;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 消耗品系统 Phase 1 测试 - 数据模型扩展
    /// Consumable system Phase 1 tests - Data model extension
    /// </summary>
    public class ConsumableSystemPhase1Tests
    {
        #region ConsumableSlotData Tests

        [Fact]
        public void ConsumableSlotData_Default_IsEmpty()
        {
            // Arrange & Act
            var slot = new ConsumableSlotData();

            // Assert
            Assert.Null(slot.ItemId);
            Assert.Null(slot.SkillId);
            Assert.False(slot.IsEquipped);
        }

        [Fact]
        public void ConsumableSlotData_Empty_CreatesEmptySlot()
        {
            // Arrange & Act
            var slot = ConsumableSlotData.Empty();

            // Assert
            Assert.False(slot.IsEquipped);
        }

        [Fact]
        public void ConsumableSlotData_Create_SetsItemAndSkill()
        {
            // Arrange
            const string itemId = "health_potion";
            const string skillId = "consumable_health_potion";

            // Act
            var slot = ConsumableSlotData.Create(itemId, skillId);

            // Assert
            Assert.Equal(itemId, slot.ItemId);
            Assert.Equal(skillId, slot.SkillId);
            Assert.True(slot.IsEquipped);
        }

        [Fact]
        public void ConsumableSlotData_Clear_RemovesEquipment()
        {
            // Arrange
            var slot = ConsumableSlotData.Create("health_potion", "consumable_health_potion");
            Assert.True(slot.IsEquipped);

            // Act
            slot.Clear();

            // Assert
            Assert.Null(slot.ItemId);
            Assert.Null(slot.SkillId);
            Assert.False(slot.IsEquipped);
        }

        [Fact]
        public void ConsumableSlotData_Serialization_RoundTrip()
        {
            // Arrange
            var original = ConsumableSlotData.Create("health_potion", "consumable_health_potion");

            // Act
            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<ConsumableSlotData>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.ItemId, deserialized.ItemId);
            Assert.Equal(original.SkillId, deserialized.SkillId);
        }

        #endregion

        #region ConsumableEquipmentConfig Tests

        [Fact]
        public void ConsumableEquipmentConfig_Default_HasFourSlots()
        {
            // Arrange & Act
            var config = new ConsumableEquipmentConfig();

            // Assert
            Assert.Equal(2, config.PotionSlots.Count);
            Assert.Equal(2, config.FoodSlots.Count);
            Assert.Contains("potion_1", config.PotionSlots.Keys);
            Assert.Contains("potion_2", config.PotionSlots.Keys);
            Assert.Contains("food_1", config.FoodSlots.Keys);
            Assert.Contains("food_2", config.FoodSlots.Keys);
        }

        [Fact]
        public void ConsumableEquipmentConfig_GetSlot_ReturnsCorrectSlot()
        {
            // Arrange
            var config = new ConsumableEquipmentConfig();
            config.PotionSlots["potion_1"] = ConsumableSlotData.Create("strength_potion", "consumable_strength_potion");

            // Act
            var slot = config.GetSlot("potion_1");

            // Assert
            Assert.NotNull(slot);
            Assert.Equal("strength_potion", slot.ItemId);
        }

        [Fact]
        public void ConsumableEquipmentConfig_GetSlot_InvalidSlot_ReturnsNull()
        {
            // Arrange
            var config = new ConsumableEquipmentConfig();

            // Act
            var slot = config.GetSlot("invalid_slot");

            // Assert
            Assert.Null(slot);
        }

        [Fact]
        public void ConsumableEquipmentConfig_IsItemEquipped_ReturnsTrueWhenEquipped()
        {
            // Arrange
            var config = new ConsumableEquipmentConfig();
            config.PotionSlots["potion_1"] = ConsumableSlotData.Create("strength_potion", "consumable_strength_potion");

            // Act & Assert
            Assert.True(config.IsItemEquipped("strength_potion"));
            Assert.False(config.IsItemEquipped("health_potion"));
            Assert.False(config.IsItemEquipped(""));
            Assert.False(config.IsItemEquipped(null!));
        }

        [Fact]
        public void ConsumableEquipmentConfig_GetEquippedSlotId_ReturnsCorrectSlot()
        {
            // Arrange
            var config = new ConsumableEquipmentConfig();
            config.FoodSlots["food_2"] = ConsumableSlotData.Create("apple", "consumable_apple");

            // Act
            var slotId = config.GetEquippedSlotId("apple");

            // Assert
            Assert.Equal("food_2", slotId);
        }

        [Fact]
        public void ConsumableEquipmentConfig_GetEquippedSlotId_NotEquipped_ReturnsNull()
        {
            // Arrange
            var config = new ConsumableEquipmentConfig();

            // Act
            var slotId = config.GetEquippedSlotId("apple");

            // Assert
            Assert.Null(slotId);
        }

        [Fact]
        public void ConsumableEquipmentConfig_GetAllEquippedItemIds_ReturnsAllItems()
        {
            // Arrange
            var config = new ConsumableEquipmentConfig();
            config.PotionSlots["potion_1"] = ConsumableSlotData.Create("strength_potion", "consumable_strength_potion");
            config.FoodSlots["food_1"] = ConsumableSlotData.Create("apple", "consumable_apple");
            config.FoodSlots["food_2"] = ConsumableSlotData.Create("bread", "consumable_bread");

            // Act
            var items = config.GetAllEquippedItemIds();

            // Assert
            Assert.Equal(3, items.Count);
            Assert.Contains("strength_potion", items);
            Assert.Contains("apple", items);
            Assert.Contains("bread", items);
        }

        [Fact]
        public void ConsumableEquipmentConfig_Serialization_RoundTrip()
        {
            // Arrange
            var original = new ConsumableEquipmentConfig();
            original.PotionSlots["potion_1"] = ConsumableSlotData.Create("strength_potion", "consumable_strength_potion");
            original.FoodSlots["food_1"] = ConsumableSlotData.Create("apple", "consumable_apple");

            // Act
            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<ConsumableEquipmentConfig>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("strength_potion", deserialized.PotionSlots["potion_1"].ItemId);
            Assert.Equal("apple", deserialized.FoodSlots["food_1"].ItemId);
        }

        #endregion

        #region CharacterData Extension Tests

        [Fact]
        public void CharacterData_EquippedConsumables_DefaultInitialized()
        {
            // Arrange & Act
            var characterData = new CharacterData();

            // Assert
            Assert.NotNull(characterData.EquippedConsumables);
            Assert.Equal(2, characterData.EquippedConsumables.PotionSlots.Count);
            Assert.Equal(2, characterData.EquippedConsumables.FoodSlots.Count);
        }

        [Fact]
        public void CharacterData_EquippedConsumables_Serialization()
        {
            // Arrange
            var characterData = new CharacterData
            {
                Id = "test-character",
                Name = "TestHero"
            };
            characterData.EquippedConsumables.PotionSlots["potion_1"] = 
                ConsumableSlotData.Create("strength_potion", "consumable_strength_potion");

            // Act
            var json = JsonSerializer.Serialize(characterData);
            var deserialized = JsonSerializer.Deserialize<CharacterData>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.EquippedConsumables);
            Assert.Equal("strength_potion", deserialized.EquippedConsumables.PotionSlots["potion_1"].ItemId);
        }

        #endregion

        #region ConsumableConfig Tests

        [Fact]
        public void ConsumableConfig_Default_Values()
        {
            // Arrange & Act
            var config = new ConsumableConfig();

            // Assert
            Assert.Equal("food", config.Category);
            Assert.Equal(string.Empty, config.SkillId);
            Assert.Null(config.TriggerConditions);
            Assert.Equal(30.0, config.CooldownSec);
            Assert.True(config.IsFood);
            Assert.False(config.IsPotion);
        }

        [Fact]
        public void ConsumableConfig_IsPotion_ReturnsTrue()
        {
            // Arrange
            var config = new ConsumableConfig { Category = "potion" };

            // Act & Assert
            Assert.True(config.IsPotion);
            Assert.False(config.IsFood);
        }

        [Fact]
        public void ConsumableConfig_IsFood_ReturnsTrue()
        {
            // Arrange
            var config = new ConsumableConfig { Category = "food" };

            // Act & Assert
            Assert.True(config.IsFood);
            Assert.False(config.IsPotion);
        }

        [Fact]
        public void ConsumableConfig_Serialization_RoundTrip()
        {
            // Arrange
            var original = new ConsumableConfig
            {
                Category = "potion",
                SkillId = "consumable_strength_potion",
                CooldownSec = 120.0
            };

            // Act
            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<ConsumableConfig>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("potion", deserialized.Category);
            Assert.Equal("consumable_strength_potion", deserialized.SkillId);
            Assert.Equal(120.0, deserialized.CooldownSec);
        }

        #endregion

        #region ItemDefinition ConsumableConfig Tests

        [Fact]
        public void ItemDefinition_ConsumableConfig_Null_ByDefault()
        {
            // Arrange & Act
            var item = new ItemDefinition();

            // Assert
            Assert.Null(item.ConsumableConfig);
        }

        [Fact]
        public void ItemDefinition_ConsumableConfig_CanBeSet()
        {
            // Arrange
            var item = new ItemDefinition
            {
                Id = "health_potion",
                Name = "生命药水",
                Type = ItemType.Consumable,
                ConsumableConfig = new ConsumableConfig
                {
                    Category = "food",
                    SkillId = "consumable_health_potion",
                    CooldownSec = 30.0
                }
            };

            // Assert
            Assert.NotNull(item.ConsumableConfig);
            Assert.Equal("food", item.ConsumableConfig.Category);
            Assert.Equal("consumable_health_potion", item.ConsumableConfig.SkillId);
        }

        [Fact]
        public void ItemDefinition_WithConsumableConfig_Serialization()
        {
            // Arrange
            var original = new ItemDefinition
            {
                Id = "strength_potion",
                Name = "力量药水",
                Type = ItemType.Consumable,
                ConsumableConfig = new ConsumableConfig
                {
                    Category = "potion",
                    SkillId = "consumable_strength_potion",
                    CooldownSec = 120.0
                }
            };

            // Act
            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<ItemDefinition>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.ConsumableConfig);
            Assert.Equal("potion", deserialized.ConsumableConfig.Category);
            Assert.Equal("consumable_strength_potion", deserialized.ConsumableConfig.SkillId);
            Assert.Equal(120.0, deserialized.ConsumableConfig.CooldownSec);
        }

        #endregion

        #region Slot Uniqueness Validation Tests (Phase 1.5)

        [Fact]
        public void ConsumableEquipmentConfig_SameItemInMultipleSlots_DetectedByIsItemEquipped()
        {
            // Arrange
            var config = new ConsumableEquipmentConfig();
            config.PotionSlots["potion_1"] = ConsumableSlotData.Create("strength_potion", "consumable_strength_potion");

            // Act - 尝试检查同一物品是否已装备
            var isAlreadyEquipped = config.IsItemEquipped("strength_potion");

            // Assert - 应该检测到已装备
            Assert.True(isAlreadyEquipped);
        }

        [Fact]
        public void ConsumableEquipmentConfig_DifferentItemsCanBeEquipped()
        {
            // Arrange
            var config = new ConsumableEquipmentConfig();

            // Act - 装备不同的物品
            config.PotionSlots["potion_1"] = ConsumableSlotData.Create("strength_potion", "consumable_strength_potion");
            config.PotionSlots["potion_2"] = ConsumableSlotData.Create("speed_potion", "consumable_speed_potion");
            config.FoodSlots["food_1"] = ConsumableSlotData.Create("apple", "consumable_apple");
            config.FoodSlots["food_2"] = ConsumableSlotData.Create("bread", "consumable_bread");

            // Assert - 都不应该互相冲突
            Assert.False(config.IsItemEquipped("non_existent"));
            Assert.True(config.IsItemEquipped("strength_potion"));
            Assert.True(config.IsItemEquipped("speed_potion"));
            Assert.True(config.IsItemEquipped("apple"));
            Assert.True(config.IsItemEquipped("bread"));
        }

        #endregion
    }
}
