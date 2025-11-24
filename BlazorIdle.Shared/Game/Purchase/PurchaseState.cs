using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Purchase
{
    /// <summary>
    /// 购买状态 - 追踪限购计数和重置时间
    /// Purchase state - tracks purchase limits and reset times
    /// </summary>
    public class PurchaseState
    {
        /// <summary>
        /// 每日限购计数: {角色ID → {商品ID → 购买次数}}
        /// Per-day purchase counts: {characterId → {itemId → count}}
        /// </summary>
        [JsonPropertyName("perDayCountsByChar")]
        public Dictionary<string, Dictionary<string, int>> PerDayCountsByChar { get; set; } 
            = new Dictionary<string, Dictionary<string, int>>();

        /// <summary>
        /// 账号限购计数: {商品ID → 购买次数}
        /// Per-account purchase counts: {itemId → count}
        /// </summary>
        [JsonPropertyName("perAccountCounts")]
        public Dictionary<string, int> PerAccountCounts { get; set; } 
            = new Dictionary<string, int>();

        /// <summary>
        /// 角色限购计数: {角色ID → {商品ID → 购买次数}}
        /// Per-character purchase counts: {characterId → {itemId → count}}
        /// </summary>
        [JsonPropertyName("perCharacterCounts")]
        public Dictionary<string, Dictionary<string, int>> PerCharacterCounts { get; set; } 
            = new Dictionary<string, Dictionary<string, int>>();

        /// <summary>
        /// 最后每日重置日期（本地时间）
        /// Last daily reset date (local time)
        /// </summary>
        [JsonPropertyName("lastDailyReset")]
        public string LastDailyReset { get; set; } = DateOnly.FromDateTime(DateTime.Now).ToString("O");
    }
}
