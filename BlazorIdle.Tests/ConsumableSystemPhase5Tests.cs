using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using BlazorIdle.Shared.Models;
using BlazorIdle.Game.Config;
using BlazorIdle.Game.Consumables;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 5 测试 - 验证消耗品UI组件的后端逻辑
    /// Phase 5 tests - validate consumable UI component backend logic
    /// </summary>
    public class ConsumableSystemPhase5Tests
    {
        #region Mock 配置服务

        private class MockGameConfigService : IGameConfigService
        {
            private readonly List<ItemDefinition> _items = new();
            private readonly Dictionary<string, ItemDefinition> _itemLookup = new();

            public MockGameConfigService()
            {
                // 添加测试用的药水
                AddItem(new ItemDefinition
                {
                    Id = "strength_potion",
                    Name = "力量药水",
                    Icon = "💪",
                    Description = "增加攻击力10%",
                    Type = ItemType.Consumable,
                    ConsumableConfig = new ConsumableConfig
                    {
                        Category = "potion",
                        SkillId = "consumable_strength_potion",
                        CooldownSec = 30
                    }
                });

                AddItem(new ItemDefinition
                {
                    Id = "speed_potion",
                    Name = "速度药水",
                    Icon = "⚡",
                    Description = "增加攻击速度15%",
                    Type = ItemType.Consumable,
                    ConsumableConfig = new ConsumableConfig
                    {
                        Category = "potion",
                        SkillId = "consumable_speed_potion",
                        CooldownSec = 45
                    }
                });

                // 添加测试用的食物
                AddItem(new ItemDefinition
                {
                    Id = "apple",
                    Name = "苹果",
                    Icon = "🍎",
                    Description = "恢复50HP",
                    Type = ItemType.Consumable,
                    ConsumableConfig = new ConsumableConfig
                    {
                        Category = "food",
                        SkillId = "consumable_apple",
                        CooldownSec = 10
                    }
                });

                AddItem(new ItemDefinition
                {
                    Id = "bread",
                    Name = "面包",
                    Icon = "🍞",
                    Description = "恢复100HP",
                    Type = ItemType.Consumable,
                    ConsumableConfig = new ConsumableConfig
                    {
                        Category = "food",
                        SkillId = "consumable_bread",
                        CooldownSec = 15
                    }
                });

                // 非消耗品物品
                AddItem(new ItemDefinition
                {
                    Id = "iron_sword",
                    Name = "铁剑",
                    Type = ItemType.Equipment
                });
            }

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

        #region 辅助方法

        private CharacterData CreateTestCharacterData()
        {
            return new CharacterData
            {
                Id = "test_char",
                Name = "测试角色",
                EquippedConsumables = new ConsumableEquipmentConfig()
            };
        }

        private void AddItemToInventory(CharacterData character, string itemId, int quantity)
        {
            character.Inventory.AddItem(itemId, quantity);
        }

        #endregion

        #region UI槽位显示测试

        [Fact]
        public void GetAvailableConsumables_ReturnsOnlyPotions_WhenCategoryIsPotion()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "strength_potion", 5);
            AddItemToInventory(character, "apple", 10);

            // Act
            var potions = manager.GetAvailableConsumables(character, "potion");
            var foods = manager.GetAvailableConsumables(character, "food");

            // Assert
            Assert.Single(potions);
            Assert.Equal("strength_potion", potions[0].Item.Id);
            Assert.Single(foods);
            Assert.Equal("apple", foods[0].Item.Id);
        }

        [Fact]
        public void GetAvailableConsumables_IncludesEquippedItemsEvenWithZeroQuantity()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            
            // 装备药水后不添加库存
            AddItemToInventory(character, "strength_potion", 1);
            manager.EquipConsumable(character, "potion_1", "strength_potion", false);
            // 模拟使用完了
            character.Inventory.Items.First(i => i.ItemId == "strength_potion").Quantity = 0;

            // Act
            var potions = manager.GetAvailableConsumables(character, "potion");

            // Assert - 已装备的物品即使库存为0也应该显示
            Assert.Single(potions);
            Assert.Equal("strength_potion", potions[0].Item.Id);
            Assert.Equal(0, potions[0].Quantity);
        }

        [Fact]
        public void GetEquippedItemQuantity_ReturnsCorrectQuantity()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "apple", 25);
            manager.EquipConsumable(character, "food_1", "apple", false);

            // Act
            var quantity = manager.GetEquippedItemQuantity(character, "food_1");

            // Assert
            Assert.Equal(25, quantity);
        }

        [Fact]
        public void GetEquippedItemQuantity_UpdatesWhenInventoryChanges()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "bread", 10);
            manager.EquipConsumable(character, "food_2", "bread", false);

            // Act - 模拟战斗中消耗
            character.Inventory.RemoveItem("bread", 3);
            var quantityAfterConsumption = manager.GetEquippedItemQuantity(character, "food_2");

            // 模拟购买补充
            character.Inventory.AddItem("bread", 15);
            var quantityAfterReplenish = manager.GetEquippedItemQuantity(character, "food_2");

            // Assert
            Assert.Equal(7, quantityAfterConsumption);
            Assert.Equal(22, quantityAfterReplenish);
        }

        #endregion

        #region 物品选择弹窗逻辑测试

        [Fact]
        public void EquipConsumable_ReturnsSuccess_WhenSelectingValidItem()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "speed_potion", 3);

            // Act
            var result = manager.EquipConsumable(character, "potion_2", "speed_potion", false);

            // Assert
            Assert.Equal(ConsumableOperationResult.Success, result);
            var slot = manager.GetEquippedConsumable(character, "potion_2");
            Assert.Equal("speed_potion", slot?.ItemId);
        }

        [Fact]
        public void EquipConsumable_ReturnsAlreadyEquipped_WhenItemInOtherSlot()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "strength_potion", 5);
            manager.EquipConsumable(character, "potion_1", "strength_potion", false);

            // Act - 尝试在另一个槽位装备同一物品
            var result = manager.EquipConsumable(character, "potion_2", "strength_potion", false);

            // Assert
            Assert.Equal(ConsumableOperationResult.AlreadyEquipped, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsInsufficientInventory_WhenNoStock()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            // 不添加库存

            // Act
            var result = manager.EquipConsumable(character, "potion_1", "strength_potion", false);

            // Assert
            Assert.Equal(ConsumableOperationResult.InsufficientInventory, result);
        }

        [Fact]
        public void EquipConsumable_ReplacesExisting_WhenSlotAlreadyEquipped()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "strength_potion", 5);
            AddItemToInventory(character, "speed_potion", 3);
            manager.EquipConsumable(character, "potion_1", "strength_potion", false);

            // Act - 装备新物品替换旧物品
            var result = manager.EquipConsumable(character, "potion_1", "speed_potion", false);

            // Assert
            Assert.Equal(ConsumableOperationResult.Success, result);
            var slot = manager.GetEquippedConsumable(character, "potion_1");
            Assert.Equal("speed_potion", slot?.ItemId);
        }

        #endregion

        #region 战斗锁定测试

        [Fact]
        public void EquipConsumable_ReturnsLockedInBattle_WhenBattleActive()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "strength_potion", 5);

            // Act
            var result = manager.EquipConsumable(character, "potion_1", "strength_potion", isInBattle: true);

            // Assert
            Assert.Equal(ConsumableOperationResult.LockedInBattle, result);
        }

        [Fact]
        public void UnequipConsumable_ReturnsLockedInBattle_WhenBattleActive()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "apple", 10);
            manager.EquipConsumable(character, "food_1", "apple", false);

            // Act
            var result = manager.UnequipConsumable(character, "food_1", isInBattle: true);

            // Assert
            Assert.Equal(ConsumableOperationResult.LockedInBattle, result);
        }

        [Fact]
        public void CanModifyConsumables_ReturnsFalse_WhenInBattle()
        {
            // Act & Assert
            Assert.False(ConsumableManager.CanModifyConsumables(isInBattle: true));
            Assert.True(ConsumableManager.CanModifyConsumables(isInBattle: false));
        }

        #endregion

        #region 库存耗尽样式测试

        [Fact]
        public void IsSlotEmpty_ReturnsFalse_WhenEquippedButOutOfStock()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "bread", 1);
            manager.EquipConsumable(character, "food_2", "bread", false);
            // 模拟使用完
            character.Inventory.Items.First(i => i.ItemId == "bread").Quantity = 0;

            // Act
            var isEmpty = manager.IsSlotEmpty(character, "food_2");
            var quantity = manager.GetEquippedItemQuantity(character, "food_2");

            // Assert - 槽位仍然有装备（配置保留），但库存为0
            Assert.False(isEmpty);
            Assert.Equal(0, quantity);
        }

        [Fact]
        public void SlotData_PreservesItemId_WhenInventoryDepleted()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "apple", 1);
            manager.EquipConsumable(character, "food_1", "apple", false);
            
            // 消耗完毕
            character.Inventory.RemoveItem("apple", 1);

            // Act
            var slot = manager.GetEquippedConsumable(character, "food_1");

            // Assert - 配置保留，等待补充
            Assert.NotNull(slot);
            Assert.Equal("apple", slot.ItemId);
            Assert.True(slot.IsEquipped);
        }

        #endregion

        #region 实时库存更新测试

        [Fact]
        public void Inventory_FiresChangedEvent_WhenItemsModified()
        {
            // Arrange
            var character = CreateTestCharacterData();
            var eventFired = false;
            character.Inventory.Changed += () => eventFired = true;

            // Act
            character.Inventory.AddItem("test_item", 5);

            // Assert
            Assert.True(eventFired);
        }

        [Fact]
        public void EquippedQuantity_UpdatesRealTime_WhenPurchasingMore()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "strength_potion", 3);
            manager.EquipConsumable(character, "potion_1", "strength_potion", false);

            // Act - 模拟购买更多
            character.Inventory.AddItem("strength_potion", 10);
            var newQuantity = manager.GetEquippedItemQuantity(character, "potion_1");

            // Assert
            Assert.Equal(13, newQuantity);
        }

        #endregion

        #region 物品类别验证测试

        [Fact]
        public void EquipConsumable_ReturnsCategoryMismatch_WhenPotionInFoodSlot()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "strength_potion", 5);

            // Act
            var result = manager.EquipConsumable(character, "food_1", "strength_potion", false);

            // Assert
            Assert.Equal(ConsumableOperationResult.CategoryMismatch, result);
        }

        [Fact]
        public void EquipConsumable_ReturnsCategoryMismatch_WhenFoodInPotionSlot()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "apple", 10);

            // Act
            var result = manager.EquipConsumable(character, "potion_2", "apple", false);

            // Assert
            Assert.Equal(ConsumableOperationResult.CategoryMismatch, result);
        }

        [Theory]
        [InlineData("potion_1", "potion")]
        [InlineData("potion_2", "potion")]
        [InlineData("food_1", "food")]
        [InlineData("food_2", "food")]
        public void GetSlotCategory_ReturnsCorrectCategory(string slotId, string expectedCategory)
        {
            // Act
            var category = ConsumableManager.GetSlotCategory(slotId);

            // Assert
            Assert.Equal(expectedCategory, category);
        }

        #endregion

        #region 卸载操作测试

        [Fact]
        public void UnequipConsumable_ClearsSlot()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "apple", 10);
            manager.EquipConsumable(character, "food_1", "apple", false);

            // Act
            var result = manager.UnequipConsumable(character, "food_1", false);

            // Assert
            Assert.Equal(ConsumableOperationResult.Success, result);
            Assert.True(manager.IsSlotEmpty(character, "food_1"));
        }

        [Fact]
        public void UnequipConsumable_DoesNotAffectOtherSlots()
        {
            // Arrange
            var configService = new MockGameConfigService();
            var manager = new ConsumableManager(configService);
            var character = CreateTestCharacterData();
            AddItemToInventory(character, "apple", 10);
            AddItemToInventory(character, "bread", 5);
            manager.EquipConsumable(character, "food_1", "apple", false);
            manager.EquipConsumable(character, "food_2", "bread", false);

            // Act
            manager.UnequipConsumable(character, "food_1", false);

            // Assert
            Assert.True(manager.IsSlotEmpty(character, "food_1"));
            Assert.False(manager.IsSlotEmpty(character, "food_2"));
            Assert.Equal("bread", manager.GetEquippedConsumable(character, "food_2")?.ItemId);
        }

        #endregion
    }
}
