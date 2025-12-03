using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Combat;
using BlazorIdle.Shared.Models;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 消耗品系统 Phase 3 测试 - 战斗逻辑实现
    /// Consumable system Phase 3 tests - Battle logic implementation
    /// </summary>
    public class ConsumableSystemPhase3Tests
    {
        #region EventSource Tests

        [Fact]
        public void EventSource_HasConsumableValue()
        {
            // Assert - Verify EventSource.Consumable exists and has correct value
            Assert.Equal(9, (int)EventSource.Consumable);
        }

        #endregion

        #region ConsumableUsedEvent Tests

        [Fact]
        public void ConsumableUsedEvent_CanBeCreated()
        {
            // Arrange & Act
            var evt = new ConsumableUsedEvent
            {
                TimeMs = 1000,
                CharacterId = "char1",
                ItemId = "health_potion",
                SkillId = "consumable_health_potion",
                SlotId = "food_1",
                RemainingCount = 49
            };

            // Assert
            Assert.Equal(1000, evt.TimeMs);
            Assert.Equal("char1", evt.CharacterId);
            Assert.Equal("health_potion", evt.ItemId);
            Assert.Equal("consumable_health_potion", evt.SkillId);
            Assert.Equal("food_1", evt.SlotId);
            Assert.Equal(49, evt.RemainingCount);
        }

        #endregion

        #region ConsumableOutOfStockEvent Tests

        [Fact]
        public void ConsumableOutOfStockEvent_CanBeCreated()
        {
            // Arrange & Act
            var evt = new ConsumableOutOfStockEvent
            {
                TimeMs = 2000,
                CharacterId = "char1",
                ItemId = "health_potion",
                SlotId = "food_1"
            };

            // Assert
            Assert.Equal(2000, evt.TimeMs);
            Assert.Equal("char1", evt.CharacterId);
            Assert.Equal("health_potion", evt.ItemId);
            Assert.Equal("food_1", evt.SlotId);
        }

        #endregion

        #region MultiBattleInstance Event Tests

        [Fact]
        public void MultiBattleInstance_HasConsumableUsedEvent()
        {
            // Arrange
            var clock = new TestGameClock();
            var rng = new RngContext(42);
            var playerTeam = CreateTestPlayerTeam();
            var enemyTeam = CreateTestEnemyTeam();

            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam);

            // Act - Subscribe to event
            ConsumableUsedEvent? firedEvent = null;
            battle.ConsumableUsed += evt => firedEvent = evt;

            // Assert - Event should be subscribable (no exception thrown)
            Assert.Null(firedEvent); // Not fired yet
        }

        [Fact]
        public void MultiBattleInstance_HasConsumableOutOfStockEvent()
        {
            // Arrange
            var clock = new TestGameClock();
            var rng = new RngContext(42);
            var playerTeam = CreateTestPlayerTeam();
            var enemyTeam = CreateTestEnemyTeam();

            var battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam);

            // Act - Subscribe to event
            ConsumableOutOfStockEvent? firedEvent = null;
            battle.ConsumableOutOfStock += evt => firedEvent = evt;

            // Assert - Event should be subscribable (no exception thrown)
            Assert.Null(firedEvent); // Not fired yet
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void MultiBattleInstance_WithGameConfigService_CanBeCreated()
        {
            // Arrange
            var clock = new TestGameClock();
            var rng = new RngContext(42);
            var playerTeam = CreateTestPlayerTeam();
            var enemyTeam = CreateTestEnemyTeam();

            // Act - Create with null gameConfigService (optional parameter)
            var battle = new MultiBattleInstance(
                clock, rng, playerTeam, enemyTeam,
                config: null,
                preservedResources: null,
                professionResourceConfigs: null,
                characterDataMap: null,
                gameConfigService: null);

            // Assert - Should not throw
            Assert.NotNull(battle);
        }

        [Fact]
        public void ConsumableEquipmentConfig_TriggerOrder_PotionsFirst()
        {
            // Arrange
            var config = new ConsumableEquipmentConfig();
            config.PotionSlots["potion_1"] = ConsumableSlotData.Create("strength_potion", "consumable_strength_potion");
            config.PotionSlots["potion_2"] = ConsumableSlotData.Create("speed_potion", "consumable_speed_potion");
            config.FoodSlots["food_1"] = ConsumableSlotData.Create("apple", "consumable_apple");
            config.FoodSlots["food_2"] = ConsumableSlotData.Create("bread", "consumable_bread");

            // Act - Get all items in order (potions first, then food)
            var orderedSlots = new List<string>();
            foreach (var kvp in config.PotionSlots.OrderBy(x => x.Key))
            {
                if (!string.IsNullOrEmpty(kvp.Value.ItemId))
                    orderedSlots.Add(kvp.Value.ItemId);
            }
            foreach (var kvp in config.FoodSlots.OrderBy(x => x.Key))
            {
                if (!string.IsNullOrEmpty(kvp.Value.ItemId))
                    orderedSlots.Add(kvp.Value.ItemId);
            }

            // Assert - Order should be potion_1, potion_2, food_1, food_2
            Assert.Equal(4, orderedSlots.Count);
            Assert.Equal("strength_potion", orderedSlots[0]); // potion_1
            Assert.Equal("speed_potion", orderedSlots[1]);    // potion_2
            Assert.Equal("apple", orderedSlots[2]);           // food_1
            Assert.Equal("bread", orderedSlots[3]);           // food_2
        }

        [Fact]
        public void Inventory_TryConsumeItem_DeductsCorrectly()
        {
            // Arrange
            var inventory = new Inventory();
            inventory.AddItem("health_potion", 10);

            // Act
            bool success = inventory.TryConsumeItem("health_potion", 1);

            // Assert
            Assert.True(success);
            Assert.Equal(9, inventory.GetItemQuantity("health_potion"));
        }

        [Fact]
        public void Inventory_TryConsumeItem_FailsWhenOutOfStock()
        {
            // Arrange
            var inventory = new Inventory();
            // Don't add any items

            // Act
            bool success = inventory.TryConsumeItem("health_potion", 1);

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void ConsumableConfig_CooldownSec_UsedForTracking()
        {
            // Arrange
            var config = new ConsumableConfig
            {
                Category = "food",
                SkillId = "consumable_health_potion",
                CooldownSec = 30.0
            };

            // Assert
            Assert.Equal(30.0, config.CooldownSec);
        }

        #endregion

        #region Helper Methods

        private BattleTeam<Character> CreateTestPlayerTeam()
        {
            var character = new Character
            {
                ActiveCombatProfessionId = "warrior",
                MaxHp = 100,
                Hp = 100,
                AttackRateAPS = 1.0,
                CritChancePercent = 5.0,
                HastePercent = 0.0,
                VariancePct = 10.0,
                SpecialIntervalSec = 2.0
            };
            
            // 初始化新伤害系统
            character.CombatStats = new CombatStats
            {
                AttackFinal = 10
            };

            var team = new BattleTeam<Character>("player_team", "Player Team", TeamType.Player);
            team.AddMember("test_char", character, maxHp: 100, currentHp: 100);
            return team;
        }

        private BattleTeam<Enemy> CreateTestEnemyTeam()
        {
            var enemy = new Enemy 
            { 
                MaxHp = 50, 
                Hp = 50, 
                DamagePerHit = 5, 
                AttackIntervalSec = 1.0 
            };
            
            var team = new BattleTeam<Enemy>("enemy_team", "Enemy Team", TeamType.Enemy);
            team.AddMember("test_enemy", enemy, maxHp: 50, currentHp: 50);
            return team;
        }

        private class TestGameClock : IGameClock
        {
            public int NowMs { get; set; } = 0;
            public void AdvanceBy(int ms) => NowMs += ms;
            public void Reset() => NowMs = 0;
        }

        #endregion

        #region DungeonManager Consumable Event Tests

        [Fact]
        public void DungeonManager_ConsumableEvents_AreExposed()
        {
            // This test verifies that DungeonManager has the ConsumableUsed and ConsumableOutOfStock events
            // The actual event forwarding is tested in integration tests
            
            // Assert - DungeonManager should have these events defined
            var dungeonManagerType = typeof(BlazorIdle.Game.DungeonManager);
            
            var consumableUsedEvent = dungeonManagerType.GetEvent("ConsumableUsed");
            var consumableOutOfStockEvent = dungeonManagerType.GetEvent("ConsumableOutOfStock");
            
            Assert.NotNull(consumableUsedEvent);
            Assert.NotNull(consumableOutOfStockEvent);
        }

        #endregion
    }
}
