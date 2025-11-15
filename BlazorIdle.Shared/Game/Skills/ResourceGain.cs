namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 资源获得定义
    /// Resource gain definition
    /// </summary>
    public sealed class ResourceGain
    {
        /// <summary>
        /// 资源桶ID（如 "rage", "mana", "energy"）
        /// Resource bucket ID (e.g., "rage", "mana", "energy")
        /// </summary>
        public string BucketId { get; set; } = "";

        /// <summary>
        /// 获得数量
        /// Amount to gain
        /// </summary>
        public int Amount { get; set; }
    }
}
