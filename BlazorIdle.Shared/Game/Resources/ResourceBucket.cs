namespace BlazorIdle.Game.Resources;

/// <summary>
/// 资源桶，管理单一类型资源（如 rage）
/// Resource bucket, manages a single type of resource (e.g., rage)
/// </summary>
public class ResourceBucket
{
    /// <summary>
    /// 资源 ID（如 "rage"）
    /// Resource ID (e.g., "rage")
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// 当前资源值
    /// Current resource value
    /// </summary>
    public int Current { get; private set; }
    
    /// <summary>
    /// 资源上限
    /// Resource maximum
    /// </summary>
    public int Max { get; private set; }
    
    /// <summary>
    /// 预留：溢出转换目标资源 ID（Step 1 不实现）
    /// Reserved: overflow conversion target resource ID (not implemented in Step 1)
    /// </summary>
    public string? ConvertTarget { get; set; }
    
    /// <summary>
    /// 预留：溢出转换比例（Step 1 不实现）
    /// Reserved: overflow conversion ratio (not implemented in Step 1)
    /// </summary>
    public double ConvertRatio { get; set; } = 0.0;

    public ResourceBucket(string id, int max = 10, int initial = 0)
    {
        Id = id;
        Max = max;
        Current = Math.Clamp(initial, 0, max);
    }

    /// <summary>
    /// 获得资源，自动 clamp 到上限
    /// Gain resource, automatically clamped to maximum
    /// </summary>
    /// <param name="amount">获得的数量 / Amount to gain</param>
    /// <param name="reason">获得原因（用于日志）/ Reason for gaining (for logging)</param>
    /// <returns>实际获得的数量 / Actual amount gained</returns>
    public int Gain(int amount, string reason)
    {
        if (amount <= 0) return 0;
        
        int oldValue = Current;
        Current = Math.Min(Current + amount, Max);
        int actualGain = Current - oldValue;
        
        // 预留：溢出转换（Step 1 不实现）
        // Reserved: overflow conversion (not implemented in Step 1)
        // if (Current == Max && amount > actualGain && !string.IsNullOrEmpty(ConvertTarget))
        // {
        //     int overflow = amount - actualGain;
        //     OnOverflow?.Invoke(ConvertTarget, (int)(overflow * ConvertRatio), reason);
        // }
        
        return actualGain;
    }

    /// <summary>
    /// 尝试消耗资源
    /// Try to consume resource
    /// </summary>
    /// <param name="amount">消耗的数量 / Amount to consume</param>
    /// <param name="reason">消耗原因（用于日志）/ Reason for consuming (for logging)</param>
    /// <returns>是否成功消耗 / Whether successfully consumed</returns>
    public bool TryConsume(int amount, string reason)
    {
        if (amount < 0) return false;
        if (Current < amount) return false;
        
        Current -= amount;
        return true;
    }

    /// <summary>
    /// 强制消耗资源（允许负数，用于特殊情况）
    /// Force consume resource (allows negative, for special cases)
    /// </summary>
    public void ForceConsume(int amount, string reason)
    {
        Current = Math.Max(0, Current - amount);
    }

    /// <summary>
    /// 设置资源上限
    /// Set resource maximum
    /// </summary>
    public void SetMax(int newMax)
    {
        if (newMax < 0) newMax = 0;
        Max = newMax;
        Current = Math.Min(Current, Max);
    }

    /// <summary>
    /// 重置资源到初始值
    /// Reset resource to initial value
    /// </summary>
    public void Reset(int value = 0)
    {
        Current = Math.Clamp(value, 0, Max);
    }
}
