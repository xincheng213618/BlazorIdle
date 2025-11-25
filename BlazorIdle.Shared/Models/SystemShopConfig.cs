using System.Text.Json.Serialization;
using BlazorIdle.Game.Purchase;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 系统商店配置
    /// System shop configuration
    /// </summary>
    public class SystemShopConfig
    {
        /// <summary>
        /// 系统商店商品列表
        /// System shop item list
        /// </summary>
        [JsonPropertyName("items")]
        public List<SystemShopItemConfig> Items { get; set; } = new List<SystemShopItemConfig>();

        /// <summary>
        /// 获取角色槽位商品配置
        /// Get character slot item config
        /// </summary>
        public SystemShopItemConfig? GetCharacterSlotConfig()
        {
            return Items.FirstOrDefault(x => x.ItemId == "character_slot");
        }
    }

    /// <summary>
    /// 系统商店商品配置
    /// System shop item configuration
    /// </summary>
    public class SystemShopItemConfig
    {
        /// <summary>
        /// 商品ID
        /// Item ID
        /// </summary>
        [JsonPropertyName("itemId")]
        public string ItemId { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// Display name
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// Description
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 价格
        /// Price
        /// </summary>
        [JsonPropertyName("price")]
        public int Price { get; set; }

        /// <summary>
        /// 货币类型
        /// Currency type
        /// </summary>
        [JsonPropertyName("currencyType")]
        public string CurrencyType { get; set; } = "Gold";

        /// <summary>
        /// 限购规则
        /// Purchase limits
        /// </summary>
        [JsonPropertyName("limits")]
        public PurchaseLimit? Limits { get; set; }
    }
}
