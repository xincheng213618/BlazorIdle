namespace BlazorIdle.Shared.Models;

using BlazorIdle.Game.Purchase;

/// <summary>
/// 用户模型 - 存储用户账号信息和角色槽位配置
/// User model - stores user account information and character slot configuration
/// </summary>
public class User
{
    /// <summary>
    /// 用户唯一标识ID
    /// Unique user ID
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// 用户名
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码哈希值
    /// Password hash
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// 账号创建时间
    /// Account creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 最大可创建角色数量（角色槽位数）
    /// Maximum number of characters that can be created (character slots)
    /// </summary>
    public int MaxCharacterSlots { get; set; } = 3;
    
    /// <summary>
    /// 已使用的角色槽位数（当前角色数量）
    /// Number of character slots used (current character count)
    /// </summary>
    public int UsedCharacterSlots { get; set; } = 0;

    /// <summary>
    /// 账号级购买状态 - 跨角色共享的限购计数 (Step 4 Phase 2)
    /// Account-level purchase state - purchase limits shared across characters
    /// </summary>
    public AccountPurchaseState AccountPurchaseState { get; set; } = new AccountPurchaseState();
}
