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
        public int DamagePerAttack { get; set; } = 15;
        public double HastePercent { get; set; } = 0.0;  // 仅影响 Attack 轨

        // Special（不受急速）
        public double SpecialIntervalSec { get; set; } = 5.0;
        public int SpecialDamage { get; set; } = 120;

        // 暴击
        public double CritChancePercent { get; set; } = 15.0;
        public double CritMultiplier { get; set; } = 1.5;

        // 浮动
        public double VariancePct { get; set; } = 0.05;  // ±5%

        // 新增：复活时间（毫秒）
        public int ReviveMs { get; set; } = 5000;

        // 职业系统
        /// <summary>
        /// 当前激活的战斗职业ID - 用于经验分配
        /// Currently active combat profession ID - used for experience allocation
        /// </summary>
        public string ActiveCombatProfessionId { get; set; } = "warrior";
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
    }
}