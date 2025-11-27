using System;
using System.Collections.Generic;
using BlazorIdle.Game.Config;
using BlazorIdle.Game.Consumables;
using BlazorIdle.Shared.Models;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 消耗品系统 Phase 4 测试 - 消耗品管理器
    /// Consumable system Phase 4 tests - Consumable Manager
    /// </summary>
    public class ConsumableSystemPhase4Tests
    {
        #region Slot ID Validation Tests

        [Theory]
        [InlineData("potion_1", true)]
        [InlineData("potion_2", true)]
        [InlineData("food_1", true)]
        [InlineData("food_2", true)]
        [InlineData("potion_3", false)]
        [InlineData("food_3", false)]
        [InlineData("invalid", false)]
        [InlineData("", false)]
        public void IsValidSlotId_ReturnsCorrectResult(string slotId, bool expected)
        {
            Assert.Equal(expected, ConsumableManager.IsValidSlotId(slotId));
        }

        [Theory]
        [InlineData("potion_1", true)]
        [InlineData("potion_2", true)]
        [InlineData("food_1", false)]
        [InlineData("food_2", false)]
        public void IsPotionSlot_ReturnsCorrectResult(string slotId, bool expected)
        {
            Assert.Equal(expected, ConsumableManager.IsPotionSlot(slotId));
        }

        [Theory]
        [InlineData("food_1", true)]
        [InlineData("food_2", true)]
        [InlineData("potion_1", false)]
        [InlineData("potion_2", false)]
        public void IsFoodSlot_ReturnsCorrectResult(string slotId, bool expected)
        {
            Assert.Equal(expected, ConsumableManager.IsFoodSlot(slotId));
        }

        #endregion

        #region Category Matching Tests

        [Theory]
        [InlineData("potion", "potion_1", true)]
        [InlineData("potion", "potion_2", true)]
        [InlineData("potion", "food_1", false)]
        [InlineData("food", "food_1", true)]
        [InlineData("food", "food_2", true)]
        [InlineData("food", "potion_1", false)]
        [InlineData("POTION", "potion_1", true)] // Case insensitive
        [InlineData("FOOD", "food_1", true)]     // Case insensitive
        public void IsCategoryMatchingSlot_ReturnsCorrectResult(string category, string slotId, bool expected)
        {
            Assert.Equal(expected, ConsumableManager.IsCategoryMatchingSlot(category, slotId));
        }

        [Theory]
        [InlineData("potion_1", "potion")]
        [InlineData("potion_2", "potion")]
        [InlineData("food_1", "food")]
        [InlineData("food_2", "food")]
        [InlineData("invalid", null)]
        public void GetSlotCategory_ReturnsCorrectCategory(string slotId, string? expected)
        {
            Assert.Equal(expected, ConsumableManager.GetSlotCategory(slotId));
        }

        #endregion

        #region Battle Lock Tests

        [Fact]
        public void CanModifyConsumables_ReturnsFalse_WhenInBattle()
        {
            Assert.False(ConsumableManager.CanModifyConsumables(isInBattle: true));
        }

        [Fact]
        public void CanModifyConsumables_ReturnsTrue_WhenNotInBattle()
        {
            Assert.True(ConsumableManager.CanModifyConsumables(isInBattle: false));
        }

        #endregion

        #region Equip Tests

        [Fact]
        public void EquipConsumable_ReturnsLockedInBattle_WhenInBattle()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();

            // Act
            var result = manager.EquipConsumable(characterData, "food_1", "apple", isInBattle: true);

            // Assert
            Assert.Equal(ConsumableOperationResult.LockedInBattle, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsInvalidCharacterData_WhenNull()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);

            // Act
            var result = manager.EquipConsumable(null!, "food_1", "apple");

            // Assert
            Assert.Equal(ConsumableOperationResult.InvalidCharacterData, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsInvalidSlotId_WhenSlotInvalid()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();

            // Act
            var result = manager.EquipConsumable(characterData, "invalid_slot", "apple");

            // Assert
            Assert.Equal(ConsumableOperationResult.InvalidSlotId, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsItemNotFound_WhenItemDoesNotExist()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();

            // Act
            var result = manager.EquipConsumable(characterData, "food_1", "nonexistent_item");

            // Assert
            Assert.Equal(ConsumableOperationResult.ItemNotFound, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsNotConsumable_WhenItemNotConsumable()
        {
            // Arrange
            var configService = new MockGameConfigService();
            configService.AddItem(new ItemDefinition
            {
                Id = "gold",
                Name = "Gold",
                Type = ItemType.Currency
            });
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("gold", 100);

            // Act
            var result = manager.EquipConsumable(characterData, "food_1", "gold");

            // Assert
            Assert.Equal(ConsumableOperationResult.NotConsumable, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsCategoryMismatch_WhenPotionInFoodSlot()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestPotion(configService, "strength_potion");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("strength_potion", 10);

            // Act
            var result = manager.EquipConsumable(characterData, "food_1", "strength_potion");

            // Assert
            Assert.Equal(ConsumableOperationResult.CategoryMismatch, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsCategoryMismatch_WhenFoodInPotionSlot()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestFood(configService, "apple");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("apple", 10);

            // Act
            var result = manager.EquipConsumable(characterData, "potion_1", "apple");

            // Assert
            Assert.Equal(ConsumableOperationResult.CategoryMismatch, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsInsufficientInventory_WhenNoStock()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestFood(configService, "apple");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            // Don't add any apples to inventory

            // Act
            var result = manager.EquipConsumable(characterData, "food_1", "apple");

            // Assert
            Assert.Equal(ConsumableOperationResult.InsufficientInventory, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsAlreadyEquipped_WhenSameItemInDifferentSlot()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestFood(configService, "apple");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("apple", 10);

            // First equip
            manager.EquipConsumable(characterData, "food_1", "apple");

            // Act - Try to equip same item in different slot
            var result = manager.EquipConsumable(characterData, "food_2", "apple");

            // Assert
            Assert.Equal(ConsumableOperationResult.AlreadyEquipped, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsSuccess_WhenValidEquip()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestFood(configService, "apple");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("apple", 10);

            // Act
            var result = manager.EquipConsumable(characterData, "food_1", "apple");

            // Assert
            Assert.Equal(ConsumableOperationResult.Success, result);
            var slot = characterData.EquippedConsumables!.GetSlot("food_1");
            Assert.NotNull(slot);
            Assert.Equal("apple", slot.ItemId);
            Assert.Equal("consumable_apple", slot.SkillId);
        }

        [Fact]
        public void EquipConsumable_AllowsReequipSameSlot()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestFood(configService, "apple");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("apple", 10);

            // First equip
            manager.EquipConsumable(characterData, "food_1", "apple");

            // Act - Re-equip same item in same slot
            var result = manager.EquipConsumable(characterData, "food_1", "apple");

            // Assert
            Assert.Equal(ConsumableOperationResult.Success, result);
        }

        #endregion

        #region Unequip Tests

        [Fact]
        public void UnequipConsumable_ReturnsLockedInBattle_WhenInBattle()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();

            // Act
            var result = manager.UnequipConsumable(characterData, "food_1", isInBattle: true);

            // Assert
            Assert.Equal(ConsumableOperationResult.LockedInBattle, result);
        }

        [Fact]
        public void UnequipConsumable_ReturnsSuccess_WhenSlotEmpty()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();

            // Act
            var result = manager.UnequipConsumable(characterData, "food_1");

            // Assert
            Assert.Equal(ConsumableOperationResult.Success, result);
        }

        [Fact]
        public void UnequipConsumable_ClearsSlot()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestFood(configService, "apple");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("apple", 10);
            manager.EquipConsumable(characterData, "food_1", "apple");

            // Act
            var result = manager.UnequipConsumable(characterData, "food_1");

            // Assert
            Assert.Equal(ConsumableOperationResult.Success, result);
            Assert.True(manager.IsSlotEmpty(characterData, "food_1"));
        }

        #endregion

        #region Query Tests

        [Fact]
        public void GetEquippedConsumable_ReturnsEmptySlot_WhenNotEquipped()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var characterData = new CharacterData { Id = "test" };
            // EquippedConsumables is auto-initialized with empty slots

            // Act
            var result = manager.GetEquippedConsumable(characterData, "food_1");

            // Assert - Returns empty slot data, not null
            Assert.NotNull(result);
            Assert.False(result.IsEquipped);
            Assert.Null(result.ItemId);
        }

        [Fact]
        public void GetEquippedConsumable_ReturnsSlotData()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestFood(configService, "apple");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("apple", 10);
            manager.EquipConsumable(characterData, "food_1", "apple");

            // Act
            var result = manager.GetEquippedConsumable(characterData, "food_1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("apple", result.ItemId);
        }

        [Fact]
        public void IsSlotEmpty_ReturnsTrue_WhenEmpty()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();

            // Act & Assert
            Assert.True(manager.IsSlotEmpty(characterData, "food_1"));
        }

        [Fact]
        public void IsSlotEmpty_ReturnsFalse_WhenEquipped()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestFood(configService, "apple");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("apple", 10);
            manager.EquipConsumable(characterData, "food_1", "apple");

            // Act & Assert
            Assert.False(manager.IsSlotEmpty(characterData, "food_1"));
        }

        [Fact]
        public void GetEquippedItemQuantity_ReturnsInventoryQuantity()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestFood(configService, "apple");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("apple", 50);
            manager.EquipConsumable(characterData, "food_1", "apple");

            // Act
            int quantity = manager.GetEquippedItemQuantity(characterData, "food_1");

            // Assert
            Assert.Equal(50, quantity);
        }

        [Fact]
        public void GetAvailableConsumables_ReturnsMatchingItems()
        {
            // Arrange
            var configService = new MockGameConfigService();
            AddTestFood(configService, "apple");
            AddTestFood(configService, "bread");
            AddTestPotion(configService, "strength_potion");
            var manager = new ConsumableManager(configService);
            var characterData = CreateTestCharacterData();
            characterData.Inventory.AddItem("apple", 10);
            characterData.Inventory.AddItem("bread", 5);
            characterData.Inventory.AddItem("strength_potion", 3);

            // Act
            var foodItems = manager.GetAvailableConsumables(characterData, "food");
            var potionItems = manager.GetAvailableConsumables(characterData, "potion");

            // Assert
            Assert.Equal(2, foodItems.Count);
            Assert.Single(potionItems);
            Assert.Contains(foodItems, x => x.Item.Id == "apple" && x.Quantity == 10);
            Assert.Contains(foodItems, x => x.Item.Id == "bread" && x.Quantity == 5);
            Assert.Contains(potionItems, x => x.Item.Id == "strength_potion" && x.Quantity == 3);
        }

        #endregion

        #region Helper Methods

        private CharacterData CreateTestCharacterData()
        {
            return new CharacterData
            {
                Id = "test_char",
                EquippedConsumables = new ConsumableEquipmentConfig()
            };
        }

        private void AddTestFood(MockGameConfigService configService, string id)
        {
            configService.AddItem(new ItemDefinition
            {
                Id = id,
                Name = id,
                Type = ItemType.Consumable,
                ConsumableConfig = new ConsumableConfig
                {
                    Category = "food",
                    SkillId = $"consumable_{id}",
                    CooldownSec = 30.0
                }
            });
        }

        private void AddTestPotion(MockGameConfigService configService, string id)
        {
            configService.AddItem(new ItemDefinition
            {
                Id = id,
                Name = id,
                Type = ItemType.Consumable,
                ConsumableConfig = new ConsumableConfig
                {
                    Category = "potion",
                    SkillId = $"consumable_{id}",
                    CooldownSec = 60.0
                }
            });
        }

        /// <summary>
        /// Mock implementation of IGameConfigService for testing
        /// </summary>
        private class MockGameConfigService : IGameConfigService
        {
            private readonly List<ItemDefinition> _items = new();
            private readonly Dictionary<string, ItemDefinition> _itemLookup = new();

            public void AddItem(ItemDefinition item)
            {
                _items.Add(item);
                _itemLookup[item.Id] = item;
            }

            public IReadOnlyList<ItemDefinition> Items => _items;
            public ItemDefinition? GetItem(string id) => _itemLookup.TryGetValue(id, out var item) ? item : null;

            // Unused members
            public Task EnsureLoadedAsync(CancellationToken ct = default) => Task.CompletedTask;
            public IReadOnlyList<ProfessionDef> Professions => Array.Empty<ProfessionDef>();
            public IReadOnlyList<MonsterDef> Monsters => Array.Empty<MonsterDef>();
            public IReadOnlyList<DungeonDef> Dungeons => Array.Empty<DungeonDef>();
            public IReadOnlyList<BattleScenarioDef> BattleScenarios => Array.Empty<BattleScenarioDef>();
            public IReadOnlyList<BattleConfigDef> BattleConfigs => Array.Empty<BattleConfigDef>();
            public IReadOnlyDictionary<string, ProfessionAttributeConfig> ProfessionAttributes => new Dictionary<string, ProfessionAttributeConfig>();
            public ProfessionDef? GetProfession(string id) => null;
            public MonsterDef? GetMonster(string id) => null;
            public DungeonDef? GetDungeon(string id) => null;
            public BattleScenarioDef? GetBattleScenario(string id) => null;
            public BattleConfigDef? GetBattleConfig(string id) => null;
            public int MaxProfessionLevel => 100;
            public string Version => "1.0";
            public bool IsLoaded => true;
        }

        #endregion
    }
}
