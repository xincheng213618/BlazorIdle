using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Purchase
{
    /// <summary>
    /// 购买服务 - 统一的购买流程管理（验证→扣费→应用→计数）
    /// Purchase service - unified purchase flow management (validate → deduct → apply → track)
    /// </summary>
    public class PurchaseService
    {
        private readonly UnlockConditionChecker _unlockChecker;
        private readonly PurchaseLimitTracker _limitTracker;

        /// <summary>
        /// 创建购买服务（仅支持角色级限购，兼容旧代码）
        /// Create purchase service (character-level limits only, legacy compatibility)
        /// </summary>
        public PurchaseService(PurchaseState purchaseState)
        {
            _unlockChecker = new UnlockConditionChecker();
            _limitTracker = new PurchaseLimitTracker(purchaseState);
        }

        /// <summary>
        /// 创建购买服务（支持角色级和账号级限购）
        /// Create purchase service (both character-level and account-level limits)
        /// </summary>
        public PurchaseService(PurchaseState purchaseState, AccountPurchaseState accountPurchaseState)
        {
            _unlockChecker = new UnlockConditionChecker();
            _limitTracker = new PurchaseLimitTracker(purchaseState, accountPurchaseState);
        }

        /// <summary>
        /// 检查是否可以购买
        /// Check if can purchase
        /// </summary>
        /// <param name="config">购买配置</param>
        /// <param name="character">角色数据</param>
        /// <param name="professionId">当前职业ID</param>
        /// <param name="characterLevel">角色等级</param>
        /// <returns>购买检查结果</returns>
        public PurchaseCheckResult CanPurchase(
            PurchasableConfig config,
            CharacterData character,
            string professionId,
            int characterLevel)
        {
            // 1. 检查解锁条件
            var unlockResult = _unlockChecker.Check(config.Unlock, character, professionId, characterLevel);
            if (!unlockResult.IsUnlocked)
                return PurchaseCheckResult.Locked(unlockResult.Reason ?? "未解锁");

            // 2. 检查限购（使用 UserId 作为账户ID）
            var limitResult = _limitTracker.CheckRemaining(config.Id, character.Id, character.UserId.ToString(), config.Limits);
            if (limitResult.IsExhausted)
                return PurchaseCheckResult.LimitReached();

            // 3. 计算价格
            int finalPrice = CalculatePrice(config.BasePrice, config.DiscountPercent);

            // 4. 检查余额
            if (!CurrencyHelper.HasEnough(character.Inventory, config.CurrencyType, finalPrice))
                return PurchaseCheckResult.InsufficientFunds();

            return PurchaseCheckResult.Success(finalPrice);
        }

        /// <summary>
        /// 执行购买（原子操作）
        /// Execute purchase (atomic operation)
        /// </summary>
        /// <param name="config">购买配置</param>
        /// <param name="character">角色数据</param>
        /// <param name="professionId">当前职业ID</param>
        /// <param name="characterLevel">角色等级</param>
        /// <param name="applyEffect">应用效果的回调函数</param>
        /// <returns>购买结果</returns>
        public PurchaseResult Purchase(
            PurchasableConfig config,
            CharacterData character,
            string professionId,
            int characterLevel,
            Action<CharacterData> applyEffect)
        {
            // 1. 再次检查（防止状态变化）
            var checkResult = CanPurchase(config, character, professionId, characterLevel);
            if (!checkResult.CanPurchase)
                return PurchaseResult.Failed(checkResult.Reason ?? "购买失败");

            int finalPrice = checkResult.FinalPrice;

            // 2. 扣费（使用原子操作）
            if (!CurrencyHelper.TryConsume(character.Inventory, config.CurrencyType, finalPrice))
                return PurchaseResult.Failed("扣费失败");

            try
            {
                // 3. 应用效果（由调用者提供）
                applyEffect(character);

                // 4. 更新限购计数（使用 UserId 作为账户ID）
                _limitTracker.IncrementCount(config.Id, character.Id, character.UserId.ToString(), config.Limits);

                string currencyName = CurrencyHelper.GetCurrencyName(config.CurrencyType);
                return PurchaseResult.Successful(finalPrice, $"购买成功（消耗 {finalPrice} {currencyName}）");
            }
            catch (Exception ex)
            {
                // 5. 失败回滚：返还货币
                CurrencyHelper.Add(character.Inventory, config.CurrencyType, finalPrice);
                return PurchaseResult.Failed($"应用效果失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 计算最终价格（基础价格 - 折扣）
        /// Calculate final price (base price - discount)
        /// </summary>
        /// <param name="basePrice">基础价格</param>
        /// <param name="discountPercent">折扣百分比（0-100）</param>
        /// <returns>最终价格（向上取整）</returns>
        public static int CalculatePrice(int basePrice, double discountPercent)
        {
            // 限制折扣范围 0-90%
            double clampedDiscount = Math.Clamp(discountPercent, 0, 90);
            double multiplier = 1.0 - (clampedDiscount / 100.0);
            double rawPrice = basePrice * multiplier;
            
            // 防止溢出：限制在 int.MaxValue 范围内
            if (rawPrice > int.MaxValue)
                return int.MaxValue;
            
            return Math.Max(0, (int)Math.Ceiling(rawPrice));
        }

        /// <summary>
        /// 获取剩余可购买次数
        /// Get remaining purchase count
        /// </summary>
        public int GetRemainingPurchases(string itemId, string characterId, string accountId, PurchaseLimit limits)
        {
            var result = _limitTracker.CheckRemaining(itemId, characterId, accountId, limits);
            return result.Remaining;
        }
    }
}
