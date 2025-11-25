using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Purchase
{
    /// <summary>
    /// 账号级购买状态 - 存储在 User 表中，跨角色共享
    /// Account-level purchase state - stored in User table, shared across characters
    /// </summary>
    public class AccountPurchaseState
    {
        /// <summary>
        /// 账号限购计数: {商品ID → 购买次数}
        /// Per-account purchase counts: {itemId → count}
        /// </summary>
        [JsonPropertyName("perAccountCounts")]
        public Dictionary<string, int> PerAccountCounts { get; set; } 
            = new Dictionary<string, int>();

        /// <summary>
        /// 获取指定商品的账号购买次数
        /// Get account purchase count for a specific item
        /// </summary>
        public int GetCount(string itemId)
        {
            return PerAccountCounts.TryGetValue(itemId, out var count) ? count : 0;
        }

        /// <summary>
        /// 增加指定商品的账号购买次数
        /// Increment account purchase count for a specific item
        /// </summary>
        public void IncrementCount(string itemId)
        {
            if (!PerAccountCounts.ContainsKey(itemId))
                PerAccountCounts[itemId] = 0;
            
            PerAccountCounts[itemId]++;
        }
    }
}
