namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 资源消耗定义
    /// Resource cost definition
    /// </summary>
    public sealed class ResourceCost
    {
        /// <summary>
        /// 资源桶ID（如 "rage", "mana", "energy"）
        /// Resource bucket ID (e.g., "rage", "mana", "energy")
        /// </summary>
        public string BucketId { get; set; } = "";

        /// <summary>
        /// 消耗数量
        /// Amount to consume
        /// </summary>
        public int Amount { get; set; }
    }
}
