using BlazorIdle.Game.Purchase;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 商店商品配置
    /// Shop item configuration
    /// </summary>
    public class ShopItemConfig
    {
        /// <summary>
        /// 物品ID（对应 items.json 中的 id）
        /// Item ID (corresponds to id in items.json)
        /// </summary>
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        /// 金币价格
        /// Gold price
        /// </summary>
        public int Price { get; set; }

        /// <summary>
        /// 限购规则（可选）
        /// Purchase limits (optional)
        /// </summary>
        public PurchaseLimit? Limits { get; set; }
    }
}
