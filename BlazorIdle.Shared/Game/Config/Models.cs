using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Config
{
    public sealed class ProfessionDef
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("desc")] public string? Desc { get; set; }

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
    }
}