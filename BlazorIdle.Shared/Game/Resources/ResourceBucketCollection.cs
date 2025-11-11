namespace BlazorIdle.Game.Resources;

/// <summary>
/// 资源桶集合，管理一个实体的所有资源
/// Resource bucket collection, manages all resources for an entity
/// </summary>
public class ResourceBucketCollection
{
    private readonly Dictionary<string, ResourceBucket> _buckets = new();

    /// <summary>
    /// 创建集合并初始化默认资源桶（rage）
    /// Create collection and initialize default resource bucket (rage)
    /// </summary>
    public ResourceBucketCollection()
    {
        // 默认创建 rage 资源桶，上限 10
        // Create default rage resource bucket with max 10
        _buckets["rage"] = new ResourceBucket("rage", max: 10, initial: 0);
    }

    /// <summary>
    /// 获取指定 ID 的资源桶
    /// Get resource bucket by ID
    /// </summary>
    public ResourceBucket GetBucket(string id)
    {
        if (!_buckets.TryGetValue(id, out var bucket))
        {
            throw new KeyNotFoundException($"Resource bucket '{id}' not found.");
        }
        return bucket;
    }

    /// <summary>
    /// 检查是否存在指定 ID 的资源桶
    /// Check if resource bucket exists by ID
    /// </summary>
    public bool HasBucket(string id) => _buckets.ContainsKey(id);

    /// <summary>
    /// 添加新的资源桶
    /// Add new resource bucket
    /// </summary>
    public void AddBucket(string id, int max = 10, int initial = 0)
    {
        if (_buckets.ContainsKey(id))
        {
            throw new InvalidOperationException($"Resource bucket '{id}' already exists.");
        }
        _buckets[id] = new ResourceBucket(id, max, initial);
    }

    /// <summary>
    /// 移除资源桶
    /// Remove resource bucket
    /// </summary>
    public bool RemoveBucket(string id) => _buckets.Remove(id);

    /// <summary>
    /// 获取所有资源桶的只读视图
    /// Get read-only view of all resource buckets
    /// </summary>
    public IReadOnlyDictionary<string, ResourceBucket> GetAll() => _buckets;
}
