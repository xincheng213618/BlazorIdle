namespace BlazorIdle.Shared.DTOs;

/// <summary>
/// 创建角色请求DTO
/// Create character request DTO
/// </summary>
public class CreateCharacterRequest
{
    /// <summary>
    /// 角色名称
    /// Character name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 职业ID
    /// Profession ID
    /// </summary>
    public string ProfessionId { get; set; } = string.Empty;
}

/// <summary>
/// 角色列表响应DTO
/// Character list response DTO
/// </summary>
public class CharacterListResponse
{
    /// <summary>
    /// 角色列表
    /// List of characters
    /// </summary>
    public List<BlazorIdle.Shared.Models.CharacterData> Characters { get; set; } = new();
    
    /// <summary>
    /// 最大角色槽位数
    /// Maximum character slots
    /// </summary>
    public int MaxSlots { get; set; }
    
    /// <summary>
    /// 已使用的角色槽位数
    /// Used character slots
    /// </summary>
    public int UsedSlots { get; set; }
    
    /// <summary>
    /// 是否可以创建更多角色
    /// Whether more characters can be created
    /// </summary>
    public bool CanCreateMore => UsedSlots < MaxSlots;
}

/// <summary>
/// 角色操作响应DTO
/// Character operation response DTO
/// </summary>
public class CharacterResponse
{
    /// <summary>
    /// 操作是否成功
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 消息（成功提示或错误信息）
    /// Message (success or error message)
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 角色数据（创建或获取成功时返回）
    /// Character data (returned on successful creation or retrieval)
    /// </summary>
    public BlazorIdle.Shared.Models.CharacterData? Character { get; set; }
}
