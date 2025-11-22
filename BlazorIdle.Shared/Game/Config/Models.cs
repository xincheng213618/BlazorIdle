using System.Text.Json.Serialization;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Config
{
    public sealed class ProfessionDef
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("desc")] public string? Desc { get; set; }
        
        // 职业类型 - 战斗职业或非战斗职业
        // Profession type - combat or non-combat
        [JsonPropertyName("type")] public ProfessionType Type { get; set; } = ProfessionType.Combat;

        // 生存
        [JsonPropertyName("maxHp")] public int MaxHp { get; set; } = 200;

        // 攻击（受急速）
        [JsonPropertyName("attackRateAPS")] public double AttackRateAPS { get; set; } = 2.0;
        [JsonPropertyName("damagePerAttack")] public int DamagePerAttack { get; set; } = 15;
        [JsonPropertyName("hastePercent")] public double HastePercent { get; set; } = 0.0;

        // Special（不受急速）
        [JsonPropertyName("specialIntervalSec")] public double SpecialIntervalSec { get; set; } = 5.0;
        [JsonPropertyName("specialDamage")] public int SpecialDamage { get; set; } = 120;

        // 暴击
        [JsonPropertyName("critChancePercent")] public double CritChancePercent { get; set; } = 15.0;
        [JsonPropertyName("critMultiplier")] public double CritMultiplier { get; set; } = 1.5;

        // 伤害浮动
        [JsonPropertyName("variancePct")] public double VariancePct { get; set; } = 0.05;

        // 新增：玩家阵亡后的复活时间（秒）
        [JsonPropertyName("reviveSec")] public double ReviveSec { get; set; } = 5.0;
    }

    public sealed class MonsterDef
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("desc")] public string? Desc { get; set; }
        [JsonPropertyName("level")] public int Level { get; set; } = 1;

        // 生存
        [JsonPropertyName("maxHp")] public int MaxHp { get; set; } = 300;

        // 敌人攻击（第三轨）
        [JsonPropertyName("attackIntervalSec")] public double AttackIntervalSec { get; set; } = 1.5;
        [JsonPropertyName("damagePerHit")] public int DamagePerHit { get; set; } = 12;

        // 伤害浮动
        [JsonPropertyName("variancePct")] public double VariancePct { get; set; } = 0.05;

        // 新增：怪物死亡后的刷新时间（秒）
        [JsonPropertyName("respawnSec")] public double RespawnSec { get; set; } = 3.0;

        /// <summary>
        /// 掉落物列表 - 怪物死亡时可能掉落的物品
        /// Loot drops list - items that may drop when monster dies
        /// </summary>
        [JsonPropertyName("lootDrops")] public List<LootDrop> LootDrops { get; set; } = new();

        /// <summary>
        /// 基础经验值 - 击败怪物后获得的经验
        /// Base experience - experience gained after defeating monster
        /// </summary>
        [JsonPropertyName("baseExperience")] public long BaseExperience { get; set; } = 50;

        /// <summary>
        /// 普通攻击技能ID - Phase 7: 怪物技能系统
        /// Normal attack skill ID - Phase 7: Monster skill system
        /// </summary>
        [JsonPropertyName("normalAttackSkillId")] public string? NormalAttackSkillId { get; set; }

        /// <summary>
        /// Phase 9: 施法技能ID列表 - 怪物可释放的施法技能
        /// Phase 9: Cast skill IDs list - Cast skills that monster can release
        /// </summary>
        [JsonPropertyName("castSkillIds")] public List<string>? CastSkillIds { get; set; }

        /// <summary>
        /// Phase 9: 瞬发技能ID列表 - 怪物可释放的瞬发技能
        /// Phase 9: Instant skill IDs list - Instant skills that monster can release
        /// </summary>
        [JsonPropertyName("instantSkillIds")] public List<string>? InstantSkillIds { get; set; }

        /// <summary>
        /// Step3 Phase 1: 定期技能ID列表 - 怪物的定期检查技能
        /// Step3 Phase 1: Periodic skill IDs list - Periodic check skills for monster
        /// 
        /// 用于定期检查触发的技能（如光环、条件触发等）
        /// Used for periodically checked triggered skills (like auras, conditional triggers, etc.)
        /// </summary>
        [JsonPropertyName("periodicSkillIds")] public List<string>? PeriodicSkillIds { get; set; }
    }
}