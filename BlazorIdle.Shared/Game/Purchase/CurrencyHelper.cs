using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Purchase
{
    /// <summary>
    /// 货币辅助类 - 封装 Inventory 中的金币操作
    /// Currency helper - wraps gold coin operations in Inventory
    /// </summary>
    public static class CurrencyHelper
    {
        /// <summary>
        /// 金币物品ID
        /// Gold coin item ID
        /// </summary>
        public const string GoldCoinItemId = "gold_coin";

        /// <summary>
        /// 宝石物品ID（预留）
        /// Gem item ID (reserved)
        /// </summary>
        public const string GemItemId = "gem";

        /// <summary>
        /// 代币物品ID（预留）
        /// Token item ID (reserved)
        /// </summary>
        public const string TokenItemId = "token";

        /// <summary>
        /// 获取货币物品ID
        /// Get currency item ID
        /// </summary>
        public static string GetCurrencyItemId(CurrencyType currencyType)
        {
            return currencyType switch
            {
                CurrencyType.Gold => GoldCoinItemId,
                CurrencyType.Gem => GemItemId,
                CurrencyType.Token => TokenItemId,
                _ => GoldCoinItemId
            };
        }

        /// <summary>
        /// 获取货币余额
        /// Get currency balance
        /// </summary>
        public static int GetBalance(Inventory inventory, CurrencyType currencyType)
        {
            if (inventory == null)
                return 0;

            string itemId = GetCurrencyItemId(currencyType);
            return inventory.GetItemQuantity(itemId);
        }

        /// <summary>
        /// 尝试消费货币
        /// Try to consume currency
        /// </summary>
        /// <param name="inventory">库存</param>
        /// <param name="currencyType">货币类型</param>
        /// <param name="amount">消费金额</param>
        /// <returns>是否成功</returns>
        public static bool TryConsume(Inventory inventory, CurrencyType currencyType, int amount)
        {
            if (inventory == null || amount <= 0)
                return false;

            string itemId = GetCurrencyItemId(currencyType);
            int currentBalance = inventory.GetItemQuantity(itemId);

            if (currentBalance < amount)
                return false;

            return inventory.RemoveItem(itemId, amount);
        }

        /// <summary>
        /// 添加货币
        /// Add currency
        /// </summary>
        public static void Add(Inventory inventory, CurrencyType currencyType, int amount)
        {
            if (inventory == null || amount <= 0)
                return;

            string itemId = GetCurrencyItemId(currencyType);
            inventory.AddItem(itemId, amount);
        }

        /// <summary>
        /// 检查是否有足够的货币
        /// Check if has enough currency
        /// </summary>
        public static bool HasEnough(Inventory inventory, CurrencyType currencyType, int amount)
        {
            return GetBalance(inventory, currencyType) >= amount;
        }

        /// <summary>
        /// 获取货币显示名称
        /// Get currency display name
        /// </summary>
        public static string GetCurrencyName(CurrencyType currencyType)
        {
            return currencyType switch
            {
                CurrencyType.Gold => "金币",
                CurrencyType.Gem => "宝石",
                CurrencyType.Token => "代币",
                _ => "金币"
            };
        }

        /// <summary>
        /// 获取货币图标
        /// Get currency icon
        /// </summary>
        public static string GetCurrencyIcon(CurrencyType currencyType)
        {
            return currencyType switch
            {
                CurrencyType.Gold => "🪙",
                CurrencyType.Gem => "💎",
                CurrencyType.Token => "🎫",
                _ => "🪙"
            };
        }
    }
}
