namespace BlazorIdle.Game.Resources;

/// <summary>
/// 全局资源配置
/// Global resource configuration
/// </summary>
public class ResourceConfig
{
    /// <summary>
    /// 默认 rage 上限
    /// Default rage maximum
    /// </summary>
    public int DefaultRageMax { get; set; } = 10;

    /// <summary>
    /// 每次普通攻击命中获得的 rage
    /// Rage gained per normal attack hit
    /// </summary>
    public int GainPerAttack { get; set; } = 1;

    /// <summary>
    /// 暴击时额外获得的 rage
    /// Extra rage gained on critical hit
    /// </summary>
    public int GainPerCritExtra { get; set; } = 1;
}
