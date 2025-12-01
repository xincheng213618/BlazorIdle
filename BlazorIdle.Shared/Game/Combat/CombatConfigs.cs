namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 战斗配置容器 - 聚合所有战斗相关配置
    /// Combat configuration container - aggregates all combat-related configurations
    /// </summary>
    public sealed class CombatConfigs
    {
        /// <summary>
        /// 属性上限配置
        /// Attribute caps configuration
        /// </summary>
        public CombatCapsConfig Caps { get; }

        /// <summary>
        /// 暴击配置
        /// Critical hit configuration
        /// </summary>
        public CritConfig Crit { get; }

        /// <summary>
        /// 浮动配置
        /// Variance configuration
        /// </summary>
        public VarianceConfig Variance { get; }

        /// <summary>
        /// 态势配置
        /// Stance configuration
        /// </summary>
        public StanceConfig Stance { get; }

        /// <summary>
        /// 元素克制矩阵
        /// Element advantage matrix
        /// </summary>
        public ElementMatrix Elements { get; }

        /// <summary>
        /// 创建战斗配置容器
        /// Create combat configuration container
        /// </summary>
        public CombatConfigs(
            CombatCapsConfig caps,
            CritConfig crit,
            VarianceConfig variance,
            StanceConfig stance,
            ElementMatrix elements)
        {
            Caps = caps ?? throw new ArgumentNullException(nameof(caps));
            Crit = crit ?? throw new ArgumentNullException(nameof(crit));
            Variance = variance ?? throw new ArgumentNullException(nameof(variance));
            Stance = stance ?? throw new ArgumentNullException(nameof(stance));
            Elements = elements ?? throw new ArgumentNullException(nameof(elements));
        }

        /// <summary>
        /// 创建默认配置容器
        /// Create default configuration container
        /// </summary>
        public static CombatConfigs CreateDefault()
        {
            return new CombatConfigs(
                CombatCapsConfig.CreateDefault(),
                CritConfig.CreateDefault(),
                VarianceConfig.CreateDefault(),
                StanceConfig.CreateDefault(),
                ElementMatrix.CreateDefault()
            );
        }

        /// <summary>
        /// 从配置仓库加载配置容器
        /// Load configuration container from repository
        /// </summary>
        public static CombatConfigs LoadFromRepository()
        {
            return new CombatConfigs(
                CombatConfigRepository.LoadCombatCaps(),
                CombatConfigRepository.LoadCritConfig(),
                CombatConfigRepository.LoadVarianceConfig(),
                CombatConfigRepository.LoadStanceConfig(),
                CombatConfigRepository.LoadElementMatrix()
            );
        }
    }
}
