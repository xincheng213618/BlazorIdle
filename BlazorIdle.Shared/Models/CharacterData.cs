using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 角色数据模型 - 用于持久化和客户端管理
    /// Character data model - for persistence and client management
    /// </summary>
    public class CharacterData
    {
        /// <summary>
        /// 角色唯一ID
        /// Character unique ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 所属用户ID - 用于关联角色和用户
        /// Owner user ID - used to associate character with user
        /// </summary>
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 职业ID（引用职业模板）
        /// </summary>
        [JsonPropertyName("professionId")]
        public string ProfessionId { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 角色属性（从职业模板初始化，后续可成长）
        // Character stats (initialized from profession template, can grow later)

        [JsonPropertyName("maxHp")]
        public int MaxHp { get; set; }

        [JsonPropertyName("attackRateAPS")]
        public double AttackRateAPS { get; set; }

        [JsonPropertyName("damagePerAttack")]
        public int DamagePerAttack { get; set; }

        [JsonPropertyName("hastePercent")]
        public double HastePercent { get; set; }

        [JsonPropertyName("specialIntervalSec")]
        public double SpecialIntervalSec { get; set; }

        [JsonPropertyName("specialDamage")]
        public int SpecialDamage { get; set; }

        [JsonPropertyName("critChancePercent")]
        public double CritChancePercent { get; set; }

        [JsonPropertyName("critMultiplier")]
        public double CritMultiplier { get; set; }

        [JsonPropertyName("variancePct")]
        public double VariancePct { get; set; }

        [JsonPropertyName("reviveSec")]
        public double ReviveSec { get; set; }

        /// <summary>
        /// 角色库存 - 存储角色拥有的所有物品
        /// Character inventory - stores all items owned by the character
        /// </summary>
        [JsonPropertyName("inventory")]
        public Inventory Inventory { get; set; } = new Inventory();

        /// <summary>
        /// 所有职业的进度数据 - 每个角色同时拥有所有职业
        /// Progress data for all professions - each character has all professions simultaneously
        /// </summary>
        [JsonPropertyName("professions")]
        public Dictionary<string, ProfessionProgress> Professions { get; set; } = new Dictionary<string, ProfessionProgress>();

        /// <summary>
        /// 当前激活的战斗职业ID - 用于战斗时的属性计算
        /// Currently active combat profession ID - used for battle stat calculation
        /// </summary>
        [JsonPropertyName("activeCombatProfessionId")]
        public string ActiveCombatProfessionId { get; set; } = "warrior";

        /// <summary>
        /// 角色已学习的技能 - 跨职业共享 (Step 2 Phase 2.5)
        /// Learned skills - shared across all professions
        /// </summary>
        [JsonPropertyName("learnedSkills")]
        public HashSet<string> LearnedSkills { get; set; } = new HashSet<string>();

        /// <summary>
        /// 角色已装备的技能 - 按职业分组 (Step 2 Phase 2.5)
        /// Equipped skills - grouped by profession
        /// Key: professionId, Value: 装备的技能配置
        /// </summary>
        [JsonPropertyName("equippedSkillsByProfession")]
        public Dictionary<string, EquippedSkillsConfig> EquippedSkillsByProfession { get; set; } = new Dictionary<string, EquippedSkillsConfig>();

        /// <summary>
        /// 账号标记集合 - 用于技能解锁等条件判定 (Step 2 Phase 2.5+)
        /// Account flags collection - used for skill unlocking and condition checks
        /// </summary>
        [JsonPropertyName("accountFlags")]
        public HashSet<string> AccountFlags { get; set; } = new HashSet<string>();

        /// <summary>
        /// 固定技能配置 - 每个职业的普通攻击和特殊攻击技能 (Step 2 Phase 3+)
        /// Fixed skills configuration - normal attack and special attack per profession
        /// Key: professionId, Value: 固定技能ID配置
        /// 支持未来的技能升级和个性化功能
        /// Supports future skill upgrade and personalization features
        /// </summary>
        [JsonPropertyName("fixedSkillsByProfession")]
        public Dictionary<string, ProfessionFixedSkills> FixedSkillsByProfession { get; set; } = new Dictionary<string, ProfessionFixedSkills>();

        /// <summary>
        /// 购买状态 - 追踪商店限购计数和重置时间 (Step 4)
        /// Purchase state - tracks shop purchase limits and reset times
        /// </summary>
        [JsonPropertyName("purchaseState")]
        public BlazorIdle.Game.Purchase.PurchaseState PurchaseState { get; set; } = new BlazorIdle.Game.Purchase.PurchaseState();

        /// <summary>
        /// 角色装备的消耗品配置 - 药水和食物槽位 (药水与食物系统)
        /// Equipped consumables configuration - potion and food slots
        /// 
        /// 设计说明：
        /// - 每个角色独立配置
        /// - 槽位只保存物品ID引用（引用模式）
        /// - 每次战斗使用时，实时从 Inventory 扣除物品
        /// - 配置界面显示的数量是背包中该物品的实时库存
        /// 
        /// Design notes:
        /// - Each character has independent configuration
        /// - Slots store item ID reference (reference mode)
        /// - Each battle use deducts from Inventory in real-time
        /// - Config UI shows real-time stock from inventory
        /// </summary>
        [JsonPropertyName("equippedConsumables")]
        public ConsumableEquipmentConfig EquippedConsumables { get; set; } = new ConsumableEquipmentConfig();
    }
}
