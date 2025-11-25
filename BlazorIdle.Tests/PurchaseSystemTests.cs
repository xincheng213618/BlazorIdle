using Xunit;
using BlazorIdle.Game.Purchase;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 购买系统测试 - Step 4
    /// Purchase system tests
    /// </summary>
    public class PurchaseSystemTests
    {
        #region Price Calculation Tests

        [Fact]
        public void CalculatePrice_NoDiscount_ReturnsBasePrice()
        {
            // Arrange
            int basePrice = 100;
            double discount = 0;

            // Act
            int result = PurchaseService.CalculatePrice(basePrice, discount);

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void CalculatePrice_WithDiscount_ReturnsDiscountedPrice()
        {
            // Arrange
            int basePrice = 100;
            double discount = 20; // 20%

            // Act
            int result = PurchaseService.CalculatePrice(basePrice, discount);

            // Assert
            Assert.Equal(80, result); // 100 * (1 - 0.2) = 80
        }

        [Fact]
        public void CalculatePrice_RoundsUp()
        {
            // Arrange
            int basePrice = 100;
            double discount = 15; // 15%

            // Act
            int result = PurchaseService.CalculatePrice(basePrice, discount);

            // Assert
            // 100 * 0.85 = 85, no rounding needed
            // But 101 * 0.85 = 85.85, should round up to 86
            Assert.Equal(85, result);

            int result2 = PurchaseService.CalculatePrice(101, discount);
            Assert.Equal(86, result2);
        }

        [Fact]
        public void CalculatePrice_MaxDiscount90Percent()
        {
            // Arrange
            int basePrice = 100;
            double discount = 95; // 95% (should be clamped to 90%)

            // Act
            int result = PurchaseService.CalculatePrice(basePrice, discount);

            // Assert
            Assert.Equal(10, result); // 100 * (1 - 0.9) = 10
        }

        #endregion

        #region Currency Helper Tests

        [Fact]
        public void CurrencyHelper_GetBalance_ReturnsCorrectAmount()
        {
            // Arrange
            var inventory = new Inventory();
            inventory.AddItem("gold_coin", 1000);

            // Act
            int balance = CurrencyHelper.GetBalance(inventory, CurrencyType.Gold);

            // Assert
            Assert.Equal(1000, balance);
        }

        [Fact]
        public void CurrencyHelper_TryConsume_Success()
        {
            // Arrange
            var inventory = new Inventory();
            inventory.AddItem("gold_coin", 1000);

            // Act
            bool success = CurrencyHelper.TryConsume(inventory, CurrencyType.Gold, 500);

            // Assert
            Assert.True(success);
            Assert.Equal(500, CurrencyHelper.GetBalance(inventory, CurrencyType.Gold));
        }

        [Fact]
        public void CurrencyHelper_TryConsume_InsufficientFunds()
        {
            // Arrange
            var inventory = new Inventory();
            inventory.AddItem("gold_coin", 100);

            // Act
            bool success = CurrencyHelper.TryConsume(inventory, CurrencyType.Gold, 500);

            // Assert
            Assert.False(success);
            Assert.Equal(100, CurrencyHelper.GetBalance(inventory, CurrencyType.Gold));
        }

        [Fact]
        public void CurrencyHelper_Add_IncreasesBalance()
        {
            // Arrange
            var inventory = new Inventory();
            inventory.AddItem("gold_coin", 100);

            // Act
            CurrencyHelper.Add(inventory, CurrencyType.Gold, 50);

            // Assert
            Assert.Equal(150, CurrencyHelper.GetBalance(inventory, CurrencyType.Gold));
        }

        #endregion

        #region Unlock Condition Tests

        [Fact]
        public void UnlockConditionChecker_NoCondition_AlwaysUnlocked()
        {
            // Arrange
            var checker = new UnlockConditionChecker();
            var character = CreateTestCharacter();

            // Act
            var result = checker.Check(null, character, "warrior", 10);

            // Assert
            Assert.True(result.IsUnlocked);
        }

        [Fact]
        public void UnlockConditionChecker_ProfessionRestriction_Success()
        {
            // Arrange
            var checker = new UnlockConditionChecker();
            var character = CreateTestCharacter();
            var condition = new UnlockCondition
            {
                AllowedProfessions = new List<string> { "warrior", "mage" }
            };

            // Act
            var result = checker.Check(condition, character, "warrior", 10);

            // Assert
            Assert.True(result.IsUnlocked);
        }

        [Fact]
        public void UnlockConditionChecker_ProfessionRestriction_Fail()
        {
            // Arrange
            var checker = new UnlockConditionChecker();
            var character = CreateTestCharacter();
            var condition = new UnlockCondition
            {
                AllowedProfessions = new List<string> { "mage" }
            };

            // Act
            var result = checker.Check(condition, character, "warrior", 10);

            // Assert
            Assert.False(result.IsUnlocked);
            Assert.Contains("仅限", result.Reason);
        }

        [Fact]
        public void UnlockConditionChecker_LevelRequirement_Success()
        {
            // Arrange
            var checker = new UnlockConditionChecker();
            var character = CreateTestCharacter();
            var condition = new UnlockCondition
            {
                MinLevel = 5
            };

            // Act
            var result = checker.Check(condition, character, "warrior", 10);

            // Assert
            Assert.True(result.IsUnlocked);
        }

        [Fact]
        public void UnlockConditionChecker_LevelRequirement_Fail()
        {
            // Arrange
            var checker = new UnlockConditionChecker();
            var character = CreateTestCharacter();
            var condition = new UnlockCondition
            {
                MinLevel = 15
            };

            // Act
            var result = checker.Check(condition, character, "warrior", 10);

            // Assert
            Assert.False(result.IsUnlocked);
            Assert.Contains("需要等级", result.Reason);
        }

        [Fact]
        public void UnlockConditionChecker_AccountFlag_Success()
        {
            // Arrange
            var checker = new UnlockConditionChecker();
            var character = CreateTestCharacter();
            character.AccountFlags.Add("achievement_first_blood");
            var condition = new UnlockCondition
            {
                RequireAccountFlags = new List<string> { "achievement_first_blood" }
            };

            // Act
            var result = checker.Check(condition, character, "warrior", 10);

            // Assert
            Assert.True(result.IsUnlocked);
        }

        [Fact]
        public void UnlockConditionChecker_AccountFlag_Fail()
        {
            // Arrange
            var checker = new UnlockConditionChecker();
            var character = CreateTestCharacter();
            var condition = new UnlockCondition
            {
                RequireAccountFlags = new List<string> { "achievement_first_blood" }
            };

            // Act
            var result = checker.Check(condition, character, "warrior", 10);

            // Assert
            Assert.False(result.IsUnlocked);
            Assert.Contains("账户条件", result.Reason);
        }

        #endregion

        #region Purchase Limit Tracker Tests

        [Fact]
        public void PurchaseLimitTracker_NoLimit_AlwaysAvailable()
        {
            // Arrange
            var state = new PurchaseState();
            var tracker = new PurchaseLimitTracker(state);
            var limits = new PurchaseLimit(); // All -1 (unlimited)

            // Act
            var result = tracker.CheckRemaining("skill1", "char1", "account1", limits);

            // Assert
            Assert.False(result.IsExhausted);
            Assert.Equal(-1, result.Remaining);
        }

        [Fact]
        public void PurchaseLimitTracker_PerCharacter_TracksProperly()
        {
            // Arrange
            var state = new PurchaseState();
            var tracker = new PurchaseLimitTracker(state);
            var limits = new PurchaseLimit { PerCharacter = 3 };

            // Act & Assert
            var result1 = tracker.CheckRemaining("skill1", "char1", "account1", limits);
            Assert.False(result1.IsExhausted);
            Assert.Equal(3, result1.Remaining);

            tracker.IncrementCount("skill1", "char1", "account1", limits);
            var result2 = tracker.CheckRemaining("skill1", "char1", "account1", limits);
            Assert.False(result2.IsExhausted);
            Assert.Equal(2, result2.Remaining);

            tracker.IncrementCount("skill1", "char1", "account1", limits);
            tracker.IncrementCount("skill1", "char1", "account1", limits);
            var result3 = tracker.CheckRemaining("skill1", "char1", "account1", limits);
            Assert.True(result3.IsExhausted);
            Assert.Equal(0, result3.Remaining);
        }

        [Fact]
        public void PurchaseLimitTracker_PerAccount_SharedAcrossCharacters()
        {
            // Arrange
            var state = new PurchaseState();
            var tracker = new PurchaseLimitTracker(state);
            var limits = new PurchaseLimit { PerAccount = 2 };

            // Act - same account, different characters
            tracker.IncrementCount("skill1", "char1", "account1", limits);
            tracker.IncrementCount("skill1", "char2", "account1", limits);
            
            // Assert - third character from same account should be exhausted
            var result = tracker.CheckRemaining("skill1", "char3", "account1", limits);
            Assert.True(result.IsExhausted);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void PurchaseService_FullFlow_Success()
        {
            // Arrange
            var character = CreateTestCharacter();
            character.Inventory.AddItem("gold_coin", 1000);
            var state = new PurchaseState();
            var service = new PurchaseService(state);

            var config = new PurchasableConfig
            {
                Id = "test_skill",
                DisplayName = "测试技能",
                CurrencyType = CurrencyType.Gold,
                BasePrice = 500,
                DiscountPercent = 0,
                Limits = new PurchaseLimit { PerCharacter = 1 }
            };

            bool effectApplied = false;

            // Act
            var result = service.Purchase(
                config,
                character,
                "warrior",
                10,
                (c) => { effectApplied = true; }
            );

            // Assert
            Assert.True(result.Success);
            Assert.Equal(500, result.SpentAmount);
            Assert.True(effectApplied);
            Assert.Equal(500, CurrencyHelper.GetBalance(character.Inventory, CurrencyType.Gold));
        }

        [Fact]
        public void PurchaseService_InsufficientFunds_Fail()
        {
            // Arrange
            var character = CreateTestCharacter();
            character.Inventory.AddItem("gold_coin", 100);
            var state = new PurchaseState();
            var service = new PurchaseService(state);

            var config = new PurchasableConfig
            {
                Id = "test_skill",
                DisplayName = "测试技能",
                CurrencyType = CurrencyType.Gold,
                BasePrice = 500,
                DiscountPercent = 0,
                Limits = new PurchaseLimit()
            };

            // Act
            var result = service.Purchase(
                config,
                character,
                "warrior",
                10,
                (c) => { }
            );

            // Assert
            Assert.False(result.Success);
            Assert.Contains("金币不足", result.Message);
            Assert.Equal(100, CurrencyHelper.GetBalance(character.Inventory, CurrencyType.Gold));
        }

        [Fact]
        public void PurchaseService_LimitReached_Fail()
        {
            // Arrange
            var character = CreateTestCharacter();
            character.Inventory.AddItem("gold_coin", 2000);
            var state = new PurchaseState();
            var service = new PurchaseService(state);

            var config = new PurchasableConfig
            {
                Id = "test_skill",
                DisplayName = "测试技能",
                CurrencyType = CurrencyType.Gold,
                BasePrice = 500,
                DiscountPercent = 0,
                Limits = new PurchaseLimit { PerCharacter = 1 }
            };

            // Act - First purchase
            var result1 = service.Purchase(config, character, "warrior", 10, (c) => { });
            Assert.True(result1.Success);

            // Act - Second purchase (should fail)
            var result2 = service.Purchase(config, character, "warrior", 10, (c) => { });

            // Assert
            Assert.False(result2.Success);
            Assert.Contains("上限", result2.Message);
        }

        #endregion

        #region Persistence Tests (Step 4 Phase 1)

        [Fact]
        public void PurchaseState_SerializesToJson_Correctly()
        {
            // Arrange
            var state = new PurchaseState();
            state.PerDayCountsByChar["char1"] = new Dictionary<string, int> { { "item1", 3 } };
            state.PerAccountCounts["account1:item2"] = 5;
            state.PerCharacterCounts["char1"] = new Dictionary<string, int> { { "item3", 2 } };
            state.LastDailyReset = "2025-11-25";

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(state);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<PurchaseState>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.PerDayCountsByChar["char1"]["item1"]);
            Assert.Equal(5, deserialized.PerAccountCounts["account1:item2"]);
            Assert.Equal(2, deserialized.PerCharacterCounts["char1"]["item3"]);
            Assert.Equal("2025-11-25", deserialized.LastDailyReset);
        }

        [Fact]
        public void PurchaseState_PreservedAcrossPurchases()
        {
            // Arrange - Simulate character with existing purchase state
            var character = CreateTestCharacter();
            character.Inventory.AddItem("gold_coin", 5000);
            
            // Pre-set some purchase state (simulating loaded from DB)
            character.PurchaseState.PerCharacterCounts["test_char_1"] = new Dictionary<string, int>
            {
                { "existing_purchase", 1 }
            };

            var service = new PurchaseService(character.PurchaseState);

            var config = new PurchasableConfig
            {
                Id = "new_skill",
                DisplayName = "新技能",
                CurrencyType = CurrencyType.Gold,
                BasePrice = 500,
                DiscountPercent = 0,
                Limits = new PurchaseLimit { PerCharacter = 3 }
            };

            // Act
            var result = service.Purchase(config, character, "warrior", 10, (c) => { });

            // Assert
            Assert.True(result.Success);
            // Existing purchase state should be preserved
            Assert.Equal(1, character.PurchaseState.PerCharacterCounts["test_char_1"]["existing_purchase"]);
            // New purchase should be recorded
            Assert.Equal(1, character.PurchaseState.PerCharacterCounts["test_char_1"]["new_skill"]);
        }

        [Fact]
        public void UpdateCharacterRequest_IncludesPurchaseState()
        {
            // Arrange
            var purchaseState = new PurchaseState();
            purchaseState.PerCharacterCounts["char1"] = new Dictionary<string, int> { { "skill1", 2 } };

            // Act
            var request = new BlazorIdle.Shared.DTOs.UpdateCharacterRequest
            {
                PurchaseState = purchaseState
            };

            // Assert
            Assert.NotNull(request.PurchaseState);
            Assert.Equal(2, request.PurchaseState.PerCharacterCounts["char1"]["skill1"]);
        }

        [Fact]
        public void PurchaseState_EmptyState_InitializesCorrectly()
        {
            // Arrange & Act
            var state = new PurchaseState();

            // Assert
            Assert.NotNull(state.PerDayCountsByChar);
            Assert.NotNull(state.PerAccountCounts);
            Assert.NotNull(state.PerCharacterCounts);
            Assert.NotNull(state.LastDailyReset);
            Assert.Empty(state.PerDayCountsByChar);
            Assert.Empty(state.PerAccountCounts);
            Assert.Empty(state.PerCharacterCounts);
        }

        [Fact]
        public void PurchaseLimitTracker_DailyReset_ClearsPerDayCounts()
        {
            // Arrange
            var state = new PurchaseState();
            state.LastDailyReset = DateOnly.FromDateTime(DateTime.Now.AddDays(-1)).ToString("O");
            state.PerDayCountsByChar["char1"] = new Dictionary<string, int> { { "item1", 5 } };
            state.PerCharacterCounts["char1"] = new Dictionary<string, int> { { "item1", 3 } };

            // Act
            var tracker = new PurchaseLimitTracker(state);

            // Assert
            // Per-day counts should be cleared (new day)
            Assert.Empty(state.PerDayCountsByChar);
            // Per-character counts should be preserved
            Assert.Equal(3, state.PerCharacterCounts["char1"]["item1"]);
            // LastDailyReset should be updated to today
            Assert.Equal(DateOnly.FromDateTime(DateTime.Now).ToString("O"), state.LastDailyReset);
        }

        #endregion

        #region Account-Level Limit Tests (Step 4 Phase 2)

        [Fact]
        public void AccountPurchaseState_SerializesToJson_Correctly()
        {
            // Arrange
            var state = new AccountPurchaseState();
            state.PerAccountCounts["item1"] = 3;
            state.PerAccountCounts["item2"] = 5;

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(state);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<AccountPurchaseState>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.PerAccountCounts["item1"]);
            Assert.Equal(5, deserialized.PerAccountCounts["item2"]);
        }

        [Fact]
        public void AccountPurchaseState_GetCount_ReturnsZeroForMissingItem()
        {
            // Arrange
            var state = new AccountPurchaseState();

            // Act
            int count = state.GetCount("nonexistent_item");

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void AccountPurchaseState_IncrementCount_WorksCorrectly()
        {
            // Arrange
            var state = new AccountPurchaseState();

            // Act
            state.IncrementCount("item1");
            state.IncrementCount("item1");
            state.IncrementCount("item2");

            // Assert
            Assert.Equal(2, state.GetCount("item1"));
            Assert.Equal(1, state.GetCount("item2"));
        }

        [Fact]
        public void PurchaseLimitTracker_WithAccountState_UsesAccountState()
        {
            // Arrange
            var characterState = new PurchaseState();
            var accountState = new AccountPurchaseState();
            accountState.PerAccountCounts["skill1"] = 1;  // Already bought once
            
            var tracker = new PurchaseLimitTracker(characterState, accountState);
            var limits = new PurchaseLimit { PerAccount = 2 };

            // Act
            var result = tracker.CheckRemaining("skill1", "char1", "account1", limits);

            // Assert
            Assert.False(result.IsExhausted);
            Assert.Equal(1, result.Remaining); // 2 - 1 = 1 remaining
        }

        [Fact]
        public void PurchaseLimitTracker_WithAccountState_SharedAcrossCharacters()
        {
            // Arrange
            var char1State = new PurchaseState();
            var char2State = new PurchaseState();
            var accountState = new AccountPurchaseState();
            
            var limits = new PurchaseLimit { PerAccount = 2 };

            // Act - Character 1 buys
            var tracker1 = new PurchaseLimitTracker(char1State, accountState);
            tracker1.IncrementCount("skill1", "char1", "account1", limits);
            tracker1.IncrementCount("skill1", "char1", "account1", limits);

            // Assert - Character 2 should see the same limit
            var tracker2 = new PurchaseLimitTracker(char2State, accountState);
            var result = tracker2.CheckRemaining("skill1", "char2", "account1", limits);
            
            Assert.True(result.IsExhausted);
            Assert.Equal(0, result.Remaining);
        }

        [Fact]
        public void PurchaseService_WithAccountState_EnforcesAccountLimit()
        {
            // Arrange
            var character = CreateTestCharacter();
            character.Inventory.AddItem("gold_coin", 5000);
            var accountState = new AccountPurchaseState();
            accountState.PerAccountCounts["limited_skill"] = 1; // Already bought once

            var service = new PurchaseService(character.PurchaseState, accountState);

            var config = new PurchasableConfig
            {
                Id = "limited_skill",
                DisplayName = "限购技能",
                CurrencyType = CurrencyType.Gold,
                BasePrice = 100,
                DiscountPercent = 0,
                Limits = new PurchaseLimit { PerAccount = 1 }
            };

            // Act
            var result = service.Purchase(config, character, "warrior", 10, (c) => { });

            // Assert
            Assert.False(result.Success);
            Assert.Contains("上限", result.Message);
        }

        [Fact]
        public void PurchaseLimitTracker_WithoutAccountState_FallsBackToLegacy()
        {
            // Arrange - Use constructor without AccountPurchaseState (legacy mode)
            var characterState = new PurchaseState();
            // Legacy format uses accountId:itemId as key
            characterState.PerAccountCounts["account1:skill1"] = 1; // Legacy data with proper key format
            
            var tracker = new PurchaseLimitTracker(characterState);
            var limits = new PurchaseLimit { PerAccount = 2 };

            // Act
            var result = tracker.CheckRemaining("skill1", "char1", "account1", limits);

            // Assert - Should read from legacy PerAccountCounts
            Assert.False(result.IsExhausted);
            Assert.Equal(1, result.Remaining);
        }

        #endregion

        #region Character Slot Shop Tests (Step 4 Phase 3)

        [Fact]
        public void User_DefaultMaxCharacterSlots_IsOne()
        {
            // Arrange & Act
            var user = new BlazorIdle.Shared.Models.User();

            // Assert
            Assert.Equal(1, user.MaxCharacterSlots);
        }

        [Fact]
        public void AccountPurchaseState_TrackSlotPurchase()
        {
            // Arrange
            var state = new AccountPurchaseState();
            const string slotItemId = "character_slot";

            // Act
            state.IncrementCount(slotItemId);

            // Assert
            Assert.Equal(1, state.GetCount(slotItemId));
        }

        [Fact]
        public void AccountPurchaseState_SlotPurchaseLimit()
        {
            // Arrange
            var state = new AccountPurchaseState();
            const string slotItemId = "character_slot";
            const int maxPurchases = 1;

            // Act - Purchase once
            state.IncrementCount(slotItemId);
            bool canPurchaseMore = state.GetCount(slotItemId) < maxPurchases;

            // Assert
            Assert.False(canPurchaseMore);
        }

        #endregion

        #region Configuration Tests (Step 4 Phase 4)

        [Fact]
        public void UserConfig_LoadsFromJson()
        {
            // Act
            var config = BlazorIdle.Game.Config.ConfigRepository.LoadUserConfig();

            // Assert
            Assert.NotNull(config);
            Assert.Equal(1, config.DefaultCharacterSlots);
            Assert.Equal(2, config.CharacterNameMinLength);
            Assert.Equal(20, config.CharacterNameMaxLength);
        }

        [Fact]
        public void SystemShopConfig_LoadsFromJson()
        {
            // Act
            var config = BlazorIdle.Game.Config.ConfigRepository.LoadSystemShop();

            // Assert
            Assert.NotNull(config);
            Assert.NotEmpty(config.Items);
            
            var slotItem = config.GetCharacterSlotConfig();
            Assert.NotNull(slotItem);
            Assert.Equal("character_slot", slotItem.ItemId);
            Assert.True(slotItem.Price > 0); // 价格应该是正数（实际价格在配置文件中定义）
            Assert.NotNull(slotItem.Limits);
            Assert.Equal(1, slotItem.Limits.PerAccount);
        }

        #endregion

        #region Helper Methods

        private CharacterData CreateTestCharacter()
        {
            return new CharacterData
            {
                Id = "test_char_1",
                Name = "Test Character",
                ProfessionId = "warrior",
                Inventory = new Inventory(),
                AccountFlags = new HashSet<string>(),
                LearnedSkills = new HashSet<string>(),
                PurchaseState = new PurchaseState()
            };
        }

        #endregion
    }
}
