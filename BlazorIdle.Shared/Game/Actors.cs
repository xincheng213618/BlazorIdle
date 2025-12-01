using BlazorIdle.Game.Combat;

namespace BlazorIdle.Game
{
    // 玩家角色
    public sealed class Character
    {
        // 生存
        public int MaxHp { get; set; } = 200;
        public int Hp { get; set; } = 200;

        // 基础输出（Attack，受急速影响）
        public double AttackRateAPS { get; set; } = 2.0; // 次/秒
        [Obsolete("将在新伤害系统中移除，使用 CombatStats.AttackFinal 替代")]
        public int DamagePerAttack { get; set; } = 15;
        public double HastePercent { get; set; } = 0.0;  // 仅影响 Attack 轨

        // Special（不受急速）
        public double SpecialIntervalSec { get; set; } = 5.0;
        [Obsolete("将在新伤害系统中移除，统一使用 AttackFinal × SkillCoef")]
        public int SpecialDamage { get; set; } = 120;

        // 暴击
        public double CritChancePercent { get; set; } = 15.0;
        [Obsolete("将在新伤害系统中移除，使用 CritConfig.BaseMultiplier + CombatStats.CritDamageBonusPercent 替代")]
        public double CritMultiplier { get; set; } = 1.5;

        // 浮动
        public double VariancePct { get; set; } = 0.05;  // ±5%

        // 新增：复活时间（毫秒）
        public int ReviveMs { get; set; } = 5000;

        #region 新伤害系统属性 / New Damage System Properties

        /// <summary>
        /// 角色元素属性（默认无属性）
        /// Character element attribute (default neutral)
        /// </summary>
        public string Element { get; set; } = ElementIds.Neutral;

        /// <summary>
        /// 汇总后的战斗属性（由装备词条计算）
        /// Aggregated combat stats (calculated from equipment affixes)
        /// </summary>
        public CombatStats? CombatStats { get; set; }

        #endregion

        // 职业系统
        /// <summary>
        /// 当前激活的战斗职业ID - 用于经验分配
        /// Currently active combat profession ID - used for experience allocation
        /// </summary>
        public string ActiveCombatProfessionId { get; set; } = "warrior";

        // Phase 3+: 固定技能系统 - 存储当前职业的固定技能ID
        // Phase 3+: Fixed skills system - stores fixed skill IDs for current profession
        /// <summary>
        /// 普通攻击技能ID - 从CharacterData的FixedSkillsByProfession初始化
        /// Normal attack skill ID - initialized from CharacterData.FixedSkillsByProfession
        /// </summary>
        public string? NormalAttackSkillId { get; set; }

        /// <summary>
        /// 特殊攻击技能ID - 从CharacterData的FixedSkillsByProfession初始化
        /// Special attack skill ID - initialized from CharacterData.FixedSkillsByProfession
        /// </summary>
        public string? SpecialAttackSkillId { get; set; }

        /// <summary>
        /// 获取普通攻击技能ID - 如果未设置则返回默认值
        /// Get normal attack skill ID - returns default if not set
        /// </summary>
        public string GetNormalAttackSkillId()
        {
            return NormalAttackSkillId ?? "attack_basic";
        }

        /// <summary>
        /// 获取特殊攻击技能ID - 如果未设置则返回默认值
        /// Get special attack skill ID - returns default if not set
        /// </summary>
        public string GetSpecialAttackSkillId()
        {
            return SpecialAttackSkillId ?? "special_pulse";
        }
    }

    // 敌人
    public sealed class Enemy
    {
        public int MaxHp { get; set; } = 300;
        public int Hp { get; set; } = 300;

        // 敌人攻击（第三条轨）
        public double AttackIntervalSec { get; set; } = 1.5;
        public int DamagePerHit { get; set; } = 12;

        // 浮动
        public double VariancePct { get; set; } = 0.05;

        // 新增：刷新时间（毫秒）
        public int RespawnMs { get; set; } = 3000;

        #region 新伤害系统属性 / New Damage System Properties

        /// <summary>
        /// 敌人元素属性（默认无属性）
        /// Enemy element attribute (default neutral)
        /// </summary>
        public string Element { get; set; } = Combat.ElementIds.Neutral;

        /// <summary>
        /// 减伤百分比（最终伤害 × (1 - 减伤%)）
        /// Damage reduction percentage (FinalDamage × (1 - DamageReduction%))
        /// </summary>
        public double DamageReductionPercent { get; set; } = 0;

        #endregion

        /// <summary>
        /// 掉落物列表 - 敌人死亡时掉落的物品配置
        /// Loot drops list - items dropped when enemy dies
        /// </summary>
        public List<Config.LootDrop> LootDrops { get; set; } = new();

        /// <summary>
        /// 基础经验值 - 击败敌人后获得的经验
        /// Base experience - experience gained after defeating enemy
        /// </summary>
        public long BaseExperience { get; set; } = 0;

        /// <summary>
        /// 敌人ID - 用于追踪经验来源
        /// Enemy ID - used to track experience source
        /// </summary>
        public string? MonsterId { get; set; }

        // Monster Skill System: 普通攻击技能ID
        // Monster Skill System: Normal attack skill ID
        /// <summary>
        /// 普通攻击技能ID - 从monsters.json配置初始化（可选）
        /// Normal attack skill ID - initialized from monsters.json configuration (optional)
        /// 如果未设置，所有普通怪物使用统一的 "enemy_attack_basic" 技能
        /// If not set, all normal monsters use the unified "enemy_attack_basic" skill
        /// </summary>
        public string? NormalAttackSkillId { get; set; }

        /// <summary>
        /// Phase 9 Monster Skills: 施法技能ID列表 - 从monsters.json配置
        /// Phase 9 Monster Skills: Cast skill IDs list - configured from monsters.json
        /// </summary>
        public List<string>? CastSkillIds { get; set; }

        /// <summary>
        /// Phase 9 Monster Skills: 瞬发技能ID列表 - 从monsters.json配置
        /// Phase 9 Monster Skills: Instant skill IDs list - configured from monsters.json
        /// </summary>
        public List<string>? InstantSkillIds { get; set; }

        /// <summary>
        /// Step3 Phase 1: 定期技能ID列表 - 从monsters.json配置
        /// Step3 Phase 1: Periodic skill IDs list - configured from monsters.json
        /// 
        /// 用于定期检查触发的技能（如光环、条件触发等）
        /// Used for periodically checked triggered skills (like auras, conditional triggers, etc.)
        /// </summary>
        public List<string>? PeriodicSkillIds { get; set; }

        /// <summary>
        /// 获取普通攻击技能ID - 如果未设置则返回默认通用技能
        /// Get normal attack skill ID - returns default universal skill if not set
        /// </summary>
        public string GetNormalAttackSkillId()
        {
            return NormalAttackSkillId ?? "monster_attack_basic";
        }

        /// <summary>
        /// Phase 9: 检查怪物是否配置了额外技能
        /// Phase 9: Check if monster has configured additional skills
        /// </summary>
        public bool HasConfiguredSkills()
        {
            return (CastSkillIds != null && CastSkillIds.Count > 0) ||
                   (InstantSkillIds != null && InstantSkillIds.Count > 0);
        }
    }
}