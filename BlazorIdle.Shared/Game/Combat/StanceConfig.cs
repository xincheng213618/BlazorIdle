using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 盛体态势配置
    /// Fortify stance configuration
    /// </summary>
    public sealed class FortifyStanceConfig
    {
        /// <summary>
        /// 盛体触发阈值（HP比例 >= 此值时触发）
        /// Fortify trigger threshold (triggers when HP ratio >= this value)
        /// </summary>
        [JsonPropertyName("thresholdMin")]
        public double ThresholdMin { get; set; } = 0.75;

        /// <summary>
        /// 描述
        /// Description
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "HPRatio >= 75% 时触发盛体";
    }

    /// <summary>
    /// 背水态势配置
    /// Backwater stance configuration
    /// </summary>
    public sealed class BackwaterStanceConfig
    {
        /// <summary>
        /// 背水触发阈值（HP比例 <= 此值时触发）
        /// Backwater trigger threshold (triggers when HP ratio <= this value)
        /// </summary>
        [JsonPropertyName("thresholdMax")]
        public double ThresholdMax { get; set; } = 0.50;

        /// <summary>
        /// 描述
        /// Description
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "HPRatio <= 50% 时触发背水";
    }

    /// <summary>
    /// 态势系统配置 - 从 Config/combat/stance.json 加载
    /// Stance system configuration - loaded from Config/combat/stance.json
    /// </summary>
    public sealed class StanceConfig
    {
        /// <summary>
        /// 盛体态势配置
        /// Fortify stance configuration
        /// </summary>
        [JsonPropertyName("fortify")]
        public FortifyStanceConfig Fortify { get; set; } = new();

        /// <summary>
        /// 背水态势配置
        /// Backwater stance configuration
        /// </summary>
        [JsonPropertyName("backwater")]
        public BackwaterStanceConfig Backwater { get; set; } = new();

        /// <summary>
        /// 创建默认配置
        /// Create default configuration
        /// </summary>
        public static StanceConfig CreateDefault() => new StanceConfig
        {
            Fortify = new FortifyStanceConfig { ThresholdMin = 0.75 },
            Backwater = new BackwaterStanceConfig { ThresholdMax = 0.50 }
        };

        /// <summary>
        /// 计算态势百分比（盛体 + 背水互斥）
        /// Calculate stance percentage (fortify + backwater mutually exclusive)
        /// </summary>
        /// <param name="hpRatio">HP比例 (0-1) / HP ratio (0-1)</param>
        /// <param name="fortifyMaxPct">盛体上限百分比 / Fortify max percentage</param>
        /// <param name="backwaterMaxPct">背水上限百分比 / Backwater max percentage</param>
        /// <returns>态势加成百分比 / Stance bonus percentage</returns>
        public double CalcStancePercent(double hpRatio, double fortifyMaxPct, double backwaterMaxPct)
        {
            // 盛体：HP >= 75%
            // Fortify: HP >= 75%
            if (hpRatio >= Fortify.ThresholdMin)
            {
                // FortifyRaw = (HPRatio - 0.75) / 0.25 ∈ [0, 1]
                double fortifyRaw = (hpRatio - Fortify.ThresholdMin) / (1.0 - Fortify.ThresholdMin);
                fortifyRaw = Math.Clamp(fortifyRaw, 0, 1);
                return fortifyRaw * fortifyMaxPct;
            }

            // 背水：HP <= 50%
            // Backwater: HP <= 50%
            if (hpRatio <= Backwater.ThresholdMax)
            {
                // BackwaterRaw = (0.50 - HPRatio) / 0.49 ∈ [0, 1]
                // 注意：最低HP为1%（不能为0），所以除以 ThresholdMax - 0.01
                double divisor = Backwater.ThresholdMax - 0.01;
                if (divisor <= 0) divisor = 0.49; // 防止除零
                double backwaterRaw = (Backwater.ThresholdMax - hpRatio) / divisor;
                backwaterRaw = Math.Clamp(backwaterRaw, 0, 1);
                return backwaterRaw * backwaterMaxPct;
            }

            // 中间区间 (50% < HP < 75%)：无态势加成
            // Middle range (50% < HP < 75%): no stance bonus
            return 0;
        }
    }
}
