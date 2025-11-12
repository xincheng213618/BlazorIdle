namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 战斗上下文 - 包含技能施放所需的所有上下文信息
    /// Battle context - contains all context information needed for skill casting
    /// </summary>
    public sealed class BattleContext
    {
        /// <summary>
        /// 玩家角色（可选，用于玩家技能）
        /// Player character (optional, for player skills)
        /// </summary>
        public Character? Player { get; init; }

        /// <summary>
        /// 敌人（可选，用于敌人技能）
        /// Enemy (optional, for enemy skills)
        /// </summary>
        public Enemy? Enemy { get; init; }

        /// <summary>
        /// 玩家队伍（多单位战斗）
        /// Player team (for multi-unit battles)
        /// </summary>
        public BattleTeam<Character>? PlayerTeam { get; init; }

        /// <summary>
        /// 敌人队伍（多单位战斗）
        /// Enemy team (for multi-unit battles)
        /// </summary>
        public BattleTeam<Enemy>? EnemyTeam { get; init; }

        /// <summary>
        /// 随机数上下文（确保可重现性）
        /// Random number context (ensures reproducibility)
        /// </summary>
        public RngContext Rng { get; init; } = null!;

        /// <summary>
        /// 游戏时钟
        /// Game clock
        /// </summary>
        public IGameClock Clock { get; init; } = null!;

        /// <summary>
        /// 玩家资源集合（Phase 2）
        /// Player resource collection (Phase 2)
        /// </summary>
        public Resources.ResourceBucketCollection? PlayerResources { get; init; }

        /// <summary>
        /// 敌人资源集合（预留，怪物暂不使用）
        /// Enemy resource collection (reserved, not used for enemies yet)
        /// </summary>
        public Resources.ResourceBucketCollection? EnemyResources { get; init; }

        /// <summary>
        /// 玩家 Buff 所有者（Phase 4）
        /// Player buff owner (Phase 4)
        /// </summary>
        public Buffs.CharacterBuffOwner? PlayerBuffOwner { get; init; }

        /// <summary>
        /// 敌人 Buff 所有者字典（Phase 4）
        /// Enemy buff owners dictionary (Phase 4)
        /// Key = enemy ID
        /// </summary>
        public Dictionary<string, Buffs.EnemyBuffOwner>? EnemyBuffOwners { get; init; }

        // 预留其他上下文字段
        // Reserved for other context fields
    }
}
