using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Purchase
{
    /// <summary>
    /// 货币类型枚举
    /// Currency type enumeration
    /// </summary>
    public enum CurrencyType
    {
        Gold,    // 金币
        Gem,     // 宝石（预留）
        Token    // 代币（预留）
    }

    /// <summary>
    /// 购买限制配置
    /// Purchase limit configuration
    /// </summary>
    public class PurchaseLimit
    {
        /// <summary>
        /// 每日限购次数（-1 表示无限制）
        /// Per-day purchase limit (-1 means unlimited)
        /// </summary>
        [JsonPropertyName("perDay")]
        public int PerDay { get; set; } = -1;

        /// <summary>
        /// 账号限购次数（-1 表示无限制）
        /// Per-account purchase limit (-1 means unlimited)
        /// </summary>
        [JsonPropertyName("perAccount")]
        public int PerAccount { get; set; } = -1;

        /// <summary>
        /// 角色限购次数（-1 表示无限制）
        /// Per-character purchase limit (-1 means unlimited)
        /// </summary>
        [JsonPropertyName("perCharacter")]
        public int PerCharacter { get; set; } = -1;
    }

    /// <summary>
    /// 解锁条件配置
    /// Unlock condition configuration
    /// </summary>
    public class UnlockCondition
    {
        /// <summary>
        /// 允许的职业列表（null 或空表示所有职业）
        /// Allowed professions (null or empty means all professions)
        /// </summary>
        [JsonPropertyName("allowedProfessions")]
        public List<string>? AllowedProfessions { get; set; }

        /// <summary>
        /// 最低等级要求
        /// Minimum level requirement
        /// </summary>
        [JsonPropertyName("minLevel")]
        public int MinLevel { get; set; }

        /// <summary>
        /// 需要的账户标记
        /// Required account flags
        /// </summary>
        [JsonPropertyName("requireAccountFlags")]
        public List<string>? RequireAccountFlags { get; set; }

        /// <summary>
        /// 需要已学习的技能（前置技能）
        /// Required learned skills (prerequisites)
        /// </summary>
        [JsonPropertyName("requireLearnedSkills")]
        public List<string>? RequireLearnedSkills { get; set; }

        /// <summary>
        /// 未解锁时的提示信息
        /// Message when locked
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    /// <summary>
    /// 可购买物品配置
    /// Purchasable item configuration
    /// </summary>
    public class PurchasableConfig
    {
        /// <summary>
        /// 商品ID
        /// Item ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// Display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 货币类型
        /// Currency type
        /// </summary>
        public CurrencyType CurrencyType { get; set; } = CurrencyType.Gold;

        /// <summary>
        /// 基础价格
        /// Base price
        /// </summary>
        public int BasePrice { get; set; }

        /// <summary>
        /// 折扣百分比（0-100）
        /// Discount percentage (0-100)
        /// </summary>
        public double DiscountPercent { get; set; }

        /// <summary>
        /// 限购配置
        /// Purchase limits
        /// </summary>
        public PurchaseLimit Limits { get; set; } = new PurchaseLimit();

        /// <summary>
        /// 解锁条件
        /// Unlock conditions
        /// </summary>
        public UnlockCondition? Unlock { get; set; }
    }

    /// <summary>
    /// 购买检查结果
    /// Purchase check result
    /// </summary>
    public class PurchaseCheckResult
    {
        /// <summary>
        /// 是否可以购买
        /// Whether purchase is allowed
        /// </summary>
        public bool CanPurchase { get; set; }

        /// <summary>
        /// 最终价格
        /// Final price
        /// </summary>
        public int FinalPrice { get; set; }

        /// <summary>
        /// 失败原因
        /// Reason for failure
        /// </summary>
        public string? Reason { get; set; }

        public static PurchaseCheckResult Success(int finalPrice) 
            => new PurchaseCheckResult { CanPurchase = true, FinalPrice = finalPrice };

        public static PurchaseCheckResult Locked(string reason) 
            => new PurchaseCheckResult { CanPurchase = false, Reason = reason };

        public static PurchaseCheckResult LimitReached() 
            => new PurchaseCheckResult { CanPurchase = false, Reason = "已达购买上限" };

        public static PurchaseCheckResult InsufficientFunds() 
            => new PurchaseCheckResult { CanPurchase = false, Reason = "金币不足" };
    }

    /// <summary>
    /// 购买执行结果
    /// Purchase execution result
    /// </summary>
    public class PurchaseResult
    {
        /// <summary>
        /// 是否成功
        /// Whether successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 消费金额
        /// Amount spent
        /// </summary>
        public int SpentAmount { get; set; }

        /// <summary>
        /// 失败原因或成功消息
        /// Failure reason or success message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        public static PurchaseResult Successful(int spentAmount, string message = "购买成功") 
            => new PurchaseResult { Success = true, SpentAmount = spentAmount, Message = message };

        public static PurchaseResult Failed(string message) 
            => new PurchaseResult { Success = false, Message = message };
    }

    /// <summary>
    /// 解锁检查结果
    /// Unlock check result
    /// </summary>
    public class UnlockCheckResult
    {
        /// <summary>
        /// 是否已解锁
        /// Whether unlocked
        /// </summary>
        public bool IsUnlocked { get; set; }

        /// <summary>
        /// 未解锁原因
        /// Reason for being locked
        /// </summary>
        public string? Reason { get; set; }

        public static UnlockCheckResult Unlocked() 
            => new UnlockCheckResult { IsUnlocked = true };

        public static UnlockCheckResult Locked(string reason) 
            => new UnlockCheckResult { IsUnlocked = false, Reason = reason };
    }

    /// <summary>
    /// 限购检查结果
    /// Limit check result
    /// </summary>
    public class LimitCheckResult
    {
        /// <summary>
        /// 是否已达上限
        /// Whether limit is exhausted
        /// </summary>
        public bool IsExhausted { get; set; }

        /// <summary>
        /// 失败原因
        /// Reason for exhaustion
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 剩余可购买次数（-1 表示无限制）
        /// Remaining purchases (-1 means unlimited)
        /// </summary>
        public int Remaining { get; set; } = -1;

        public static LimitCheckResult Available(int remaining = -1) 
            => new LimitCheckResult { IsExhausted = false, Remaining = remaining };

        public static LimitCheckResult Exhausted(string reason) 
            => new LimitCheckResult { IsExhausted = true, Reason = reason, Remaining = 0 };
    }
}
