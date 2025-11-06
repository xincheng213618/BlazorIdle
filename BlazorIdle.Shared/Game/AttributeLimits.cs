namespace BlazorIdle.Shared.Game
{
    /// <summary>
    /// 属性系统的常量定义
    /// 集中管理所有硬编码的数值，提高代码可维护性
    /// </summary>
    public static class AttributeLimits
    {
        /// <summary>
        /// 最小角色等级
        /// </summary>
        public const int MIN_LEVEL = 1;

        /// <summary>
        /// 最大合理等级（用于边界测试）
        /// </summary>
        public const int MAX_REASONABLE_LEVEL = 10000;

        /// <summary>
        /// 最小属性值（不能为负）
        /// </summary>
        public const double MIN_STAT_VALUE = 0.0;

        /// <summary>
        /// 最小伤害值
        /// </summary>
        public const int MIN_DAMAGE = 1;

        /// <summary>
        /// 最小生命值
        /// </summary>
        public const int MIN_HP = 1;

        /// <summary>
        /// 默认最大暴击率 (60%)
        /// 注意：实际上限由职业配置中的 CritCap 决定
        /// </summary>
        public const double DEFAULT_MAX_CRIT_CHANCE = 0.6;

        /// <summary>
        /// 默认最大急速 (40%)
        /// 注意：实际上限由职业配置中的 HasteCap 决定
        /// </summary>
        public const double DEFAULT_MAX_HASTE = 0.4;

        /// <summary>
        /// 百分比最小值
        /// </summary>
        public const double MIN_PERCENT = 0.0;

        /// <summary>
        /// 百分比最大值 (100%)
        /// </summary>
        public const double MAX_PERCENT = 1.0;

        /// <summary>
        /// 性能测试：单次计算目标耗时 (毫秒)
        /// </summary>
        public const double PERFORMANCE_TARGET_SINGLE_CALC_MS = 1.0;

        /// <summary>
        /// 性能测试：批量计算目标耗时 (毫秒)
        /// </summary>
        public const double PERFORMANCE_TARGET_BATCH_CALC_MS = 100.0;

        /// <summary>
        /// 性能测试：DPS计算目标耗时 (毫秒)
        /// </summary>
        public const double PERFORMANCE_TARGET_DPS_CALC_MS = 50.0;

        /// <summary>
        /// 性能测试：目标内存占用 (KB)
        /// </summary>
        public const double PERFORMANCE_TARGET_MEMORY_KB = 200.0;

        /// <summary>
        /// 性能测试：完整流程目标耗时 (毫秒)
        /// </summary>
        public const double PERFORMANCE_TARGET_FULL_FLOW_MS = 1000.0;

        /// <summary>
        /// 性能测试：批量计算次数
        /// </summary>
        public const int PERFORMANCE_BATCH_COUNT = 1000;

        /// <summary>
        /// 性能测试：完整流程测试次数
        /// </summary>
        public const int PERFORMANCE_FULL_FLOW_COUNT = 100;
    }
}
