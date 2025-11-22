using System.Text.Json.Serialization;
using BlazorIdle.Game.Purchase;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能购买配置 - 定义技能的价格、限购和商店可见性
    /// Skill purchase configuration - defines skill price, limits, and shop visibility
    /// </summary>
    public class SkillPurchaseConfig
    {
        /// <summary>
        /// 是否启用购买（false = 免费学习，true = 需要金币）
        /// Whether purchase is enabled (false = free learning, true = requires currency)
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 是否可以在商店直接购买（false = 只能通过技能书学习）
        /// Whether can be purchased directly in shop (false = only via skill book)
        /// 这个参数用于实现"有的技能只能用技能书学到"的设计
        /// This parameter implements the design of "some skills can only be learned via skill books"
        /// </summary>
        [JsonPropertyName("canBuyInShop")]
        public bool CanBuyInShop { get; set; } = true;

        /// <summary>
        /// 货币类型
        /// Currency type
        /// </summary>
        [JsonPropertyName("currencyType")]
        public string CurrencyType { get; set; } = "Gold";

        /// <summary>
        /// 基础价格
        /// Base price
        /// </summary>
        [JsonPropertyName("basePrice")]
        public int BasePrice { get; set; }

        /// <summary>
        /// 折扣百分比（0-100）
        /// Discount percentage (0-100)
        /// </summary>
        [JsonPropertyName("discountPercent")]
        public double DiscountPercent { get; set; }

        /// <summary>
        /// 限购配置
        /// Purchase limits
        /// </summary>
        [JsonPropertyName("limits")]
        public PurchaseLimit? Limits { get; set; }
    }
}
