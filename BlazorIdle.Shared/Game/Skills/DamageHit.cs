namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 多段伤害定义 - 定义技能的单次击中参数
    /// Multi-hit damage definition - defines parameters for a single hit of a skill
    /// 
    /// 用于支持连击、弹幕等多段伤害技能
    /// Used to support multi-hit skills like combos, barrages, etc.
    /// </summary>
    public sealed class DamageHit
    {
        /// <summary>
        /// 击中编号（用于标识连击段落，从0开始）
        /// Hit index (identifies combo stage, starting from 0)
        /// </summary>
        public int HitIndex { get; set; }

        /// <summary>
        /// 攻击力系数（例：0.5 = 50%攻击力）
        /// Attack coefficient (e.g., 0.5 = 50% of attack)
        /// </summary>
        public double CoefAtk { get; set; } = 1.0;

        /// <summary>
        /// 固定伤害
        /// Flat damage
        /// </summary>
        public int Flat { get; set; }

        /// <summary>
        /// 延迟时间（秒）- 0表示立即应用
        /// Delay time in seconds - 0 means apply immediately
        /// </summary>
        public double DelaySec { get; set; }

        /// <summary>
        /// 是否独立判定暴击（true = 每段独立判定，false = 使用第一段结果）
        /// Whether to independently roll for crit (true = each hit rolls independently, false = use first hit's result)
        /// </summary>
        public bool IndependentCrit { get; set; } = true;

        /// <summary>
        /// 目标策略覆盖（null = 使用技能默认策略）
        /// Target policy override (null = use skill's default policy)
        /// </summary>
        public string? TargetPolicy { get; set; }

        /// <summary>
        /// 创建此击中定义的副本
        /// Create a copy of this hit definition
        /// </summary>
        public DamageHit Clone()
        {
            return new DamageHit
            {
                HitIndex = HitIndex,
                CoefAtk = CoefAtk,
                Flat = Flat,
                DelaySec = DelaySec,
                IndependentCrit = IndependentCrit,
                TargetPolicy = TargetPolicy
            };
        }

        /// <summary>
        /// 从单段伤害定义创建 DamageHit（用于向后兼容）
        /// Create DamageHit from single damage definition (for backward compatibility)
        /// </summary>
        /// <param name="damage">单段伤害定义 / Single damage definition</param>
        /// <returns>等效的 DamageHit / Equivalent DamageHit</returns>
        public static DamageHit FromDamageDef(DamageDef? damage)
        {
            return new DamageHit
            {
                HitIndex = 0,
                CoefAtk = damage?.CoefAtk ?? 1.0,
                Flat = damage?.Flat ?? 0,
                DelaySec = 0,
                IndependentCrit = true,
                TargetPolicy = null
            };
        }
    }
}
