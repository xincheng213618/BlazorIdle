namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 伤害计算上下文 - 包含计算伤害所需的所有输入参数
    /// Damage calculation context - contains all input parameters needed for damage calculation
    /// </summary>
    public sealed class DamageContext
    {
        #region 攻击者属性 / Attacker Attributes

        /// <summary>
        /// 攻击者最终攻击力
        /// Attacker's final attack power
        /// </summary>
        public int AttackFinal { get; set; }

        /// <summary>
        /// 攻击者战斗属性（包含各种百分比加成）
        /// Attacker's combat stats (contains various percentage bonuses)
        /// </summary>
        public CombatStats AttackerStats { get; set; } = CombatStats.CreateDefault();

        /// <summary>
        /// 攻击者HP比例 (0-1)，用于态势计算
        /// Attacker's HP ratio (0-1), used for stance calculation
        /// </summary>
        public double AttackerHPRatio { get; set; } = 1.0;

        /// <summary>
        /// 攻击者元素ID
        /// Attacker's element ID
        /// </summary>
        public string AttackerElement { get; set; } = ElementIds.Neutral;

        #endregion

        #region 技能属性 / Skill Attributes

        /// <summary>
        /// 技能攻击系数（乘区）
        /// Skill attack coefficient (multiplicative)
        /// </summary>
        public double SkillCoef { get; set; } = 1.0;

        /// <summary>
        /// 技能固定伤害（加法）
        /// Skill flat damage (additive)
        /// </summary>
        public int SkillFlat { get; set; } = 0;

        #endregion

        #region 防御者属性 / Defender Attributes

        /// <summary>
        /// 防御者元素ID
        /// Defender's element ID
        /// </summary>
        public string DefenderElement { get; set; } = ElementIds.Neutral;

        /// <summary>
        /// 防御者减伤百分比
        /// Defender's damage reduction percentage
        /// </summary>
        public double DefenderDRPct { get; set; } = 0;

        #endregion

        #region 随机数 / Random

        /// <summary>
        /// 随机数生成器
        /// Random number generator
        /// </summary>
        public Random Rng { get; set; } = new();

        #endregion

        #region 工厂方法 / Factory Methods

        /// <summary>
        /// 创建简单的伤害上下文（用于测试）
        /// Create a simple damage context (for testing)
        /// </summary>
        public static DamageContext CreateSimple(int attackFinal, double skillCoef = 1.0, int skillFlat = 0)
        {
            return new DamageContext
            {
                AttackFinal = attackFinal,
                SkillCoef = skillCoef,
                SkillFlat = skillFlat,
                AttackerStats = CombatStats.CreateDefault(),
                AttackerHPRatio = 1.0,
                AttackerElement = ElementIds.Neutral,
                DefenderElement = ElementIds.Neutral,
                DefenderDRPct = 0,
                Rng = new Random(42) // 固定种子用于可重复测试 / Fixed seed for reproducible tests
            };
        }

        #endregion
    }
}
