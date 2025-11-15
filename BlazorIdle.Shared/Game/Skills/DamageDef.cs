namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能伤害定义
    /// Skill damage definition
    /// </summary>
    public sealed class DamageDef
    {
        /// <summary>
        /// 攻击力系数（例：1.2 = 120%攻击力）
        /// Attack coefficient (e.g., 1.2 = 120% of attack)
        /// </summary>
        public double CoefAtk { get; set; } = 1.0;

        /// <summary>
        /// 固定伤害
        /// Flat damage
        /// </summary>
        public int Flat { get; set; }

        /// <summary>
        /// 是否为AoE伤害
        /// Whether this is AoE damage
        /// </summary>
        public bool IsAoe { get; set; }
    }
}
