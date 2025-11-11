namespace BlazorIdle.Game.Resources;

/// <summary>
/// 资源获得事件
/// Resource gain event
/// </summary>
public class ResourceGainEvent : CombatEvent
{
    /// <summary>
    /// 获得资源的实体 ID
    /// Actor ID who gained the resource
    /// </summary>
    public string ActorId { get; set; } = "";

    /// <summary>
    /// 资源桶 ID（如 "rage"）
    /// Resource bucket ID (e.g., "rage")
    /// </summary>
    public string BucketId { get; set; } = "";

    /// <summary>
    /// 获得的数量（可能因 clamp 而小于请求数量）
    /// Amount gained (may be less than requested due to clamping)
    /// </summary>
    public int Delta { get; set; }

    /// <summary>
    /// 获得后的新值
    /// New value after gaining
    /// </summary>
    public int NewValue { get; set; }

    /// <summary>
    /// 获得原因（如 "attack_hit", "crit_bonus"）
    /// Reason for gaining (e.g., "attack_hit", "crit_bonus")
    /// </summary>
    public string Reason { get; set; } = "";

    /// <summary>
    /// 可选：关联的技能 ID
    /// Optional: Associated skill ID
    /// </summary>
    public string? SkillId { get; set; }

    /// <summary>
    /// 可选：关联的 Bundle ID
    /// Optional: Associated bundle ID
    /// </summary>
    public string? BundleId { get; set; }
}
