namespace BlazorIdle.Configuration
{
    /// <summary>
    /// 心跳配置类 - 管理角色数据自动保存的配置参数
    /// Heartbeat configuration class - manages auto-save configuration parameters for character data
    /// </summary>
    public class HeartbeatConfiguration
    {
        /// <summary>
        /// 自动保存间隔时间（秒）
        /// Auto-save interval (seconds)
        /// 默认值：30秒
        /// Default: 30 seconds
        /// </summary>
        public int SaveIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// 是否启用自动保存
        /// Whether auto-save is enabled
        /// 默认值：true
        /// Default: true
        /// </summary>
        public bool EnableAutoSave { get; set; } = true;

        /// <summary>
        /// 获取保存间隔的TimeSpan对象
        /// Get save interval as TimeSpan
        /// </summary>
        public TimeSpan SaveInterval => TimeSpan.FromSeconds(SaveIntervalSeconds);
    }
}
