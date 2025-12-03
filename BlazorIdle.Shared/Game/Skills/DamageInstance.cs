using BlazorIdle.Game.Combat;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 伤害实例 - 表示一次已计算的伤害（立即或待应用）
    /// Damage instance - represents a calculated damage (immediate or pending)
    /// 
    /// 由 SkillResolver 创建，由 SkillExecutor/DamageApplier 处理
    /// Created by SkillResolver, processed by SkillExecutor/DamageApplier
    /// </summary>
    public sealed class DamageInstance
    {
        /// <summary>
        /// 击中编号（对应 DamageHit.HitIndex）
        /// Hit index (corresponds to DamageHit.HitIndex)
        /// </summary>
        public int HitIndex { get; set; }

        /// <summary>
        /// 计算后的伤害值
        /// Calculated damage value
        /// </summary>
        public int Damage { get; set; }

        /// <summary>
        /// 是否暴击
        /// Whether this hit was a critical strike
        /// </summary>
        public bool IsCrit { get; set; }

        /// <summary>
        /// 目标ID
        /// Target ID
        /// </summary>
        public string TargetId { get; set; } = "";

        /// <summary>
        /// 应用时间（秒，相对于技能施放时间，0=立即）
        /// Time to apply damage (seconds relative to skill cast time, 0=immediate)
        /// </summary>
        public double ApplyAtSec { get; set; }

        /// <summary>
        /// 详细伤害计算结果（可选，用于调试/显示）
        /// Detailed damage calculation result (optional, for debugging/display)
        /// </summary>
        public DamageResult? DetailedResult { get; set; }

        /// <summary>
        /// 来源技能ID
        /// Source skill ID
        /// </summary>
        public string SkillId { get; set; } = "";

        /// <summary>
        /// 施法者ID
        /// Caster ID
        /// </summary>
        public string CasterId { get; set; } = "";

        /// <summary>
        /// 是否为延迟伤害（待应用）
        /// Whether this is a pending (delayed) damage
        /// </summary>
        public bool IsPending => ApplyAtSec > 0;

        /// <summary>
        /// 是否立即伤害
        /// Whether this is an immediate damage
        /// </summary>
        public bool IsImmediate => ApplyAtSec <= 0;

        /// <summary>
        /// 施法者是否为玩家（用于区分玩家/怪物伤害）
        /// Whether the caster is a player (to distinguish player/monster damage)
        /// </summary>
        public bool IsCasterPlayer { get; set; } = true;

        /// <summary>
        /// Bundle ID（用于追踪同一次技能施放产生的多段伤害）
        /// Bundle ID (for tracking multiple hits from the same skill cast)
        /// </summary>
        public string? BundleId { get; set; }

        /// <summary>
        /// 是否有元素克制优势
        /// Whether attacker has element advantage
        /// </summary>
        public bool HasElementAdvantage { get; set; }

        /// <summary>
        /// 元素倍率
        /// Element multiplier
        /// </summary>
        public double ElementMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 创建实例副本
        /// Create a copy of this instance
        /// </summary>
        public DamageInstance Clone()
        {
            return new DamageInstance
            {
                HitIndex = HitIndex,
                Damage = Damage,
                IsCrit = IsCrit,
                TargetId = TargetId,
                ApplyAtSec = ApplyAtSec,
                DetailedResult = DetailedResult,
                SkillId = SkillId,
                CasterId = CasterId,
                IsCasterPlayer = IsCasterPlayer,
                BundleId = BundleId,
                HasElementAdvantage = HasElementAdvantage,
                ElementMultiplier = ElementMultiplier
            };
        }
    }
}
