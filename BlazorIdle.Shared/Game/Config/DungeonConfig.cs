using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Config
{
    /// <summary>
    /// 副本定义 - 描述一个完整的副本配置
    /// Dungeon definition - describes a complete dungeon configuration
    /// </summary>
    public class DungeonDef
    {
        /// <summary>
        /// 副本唯一标识
        /// Dungeon unique identifier
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 副本名称
        /// Dungeon name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 副本描述
        /// Dungeon description
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 副本类型（普通/精英/史诗等）
        /// Dungeon type (normal/elite/epic etc.)
        /// </summary>
        [JsonPropertyName("dungeonType")]
        public DungeonType DungeonType { get; set; } = DungeonType.Normal;

        /// <summary>
        /// 推荐等级
        /// Recommended level
        /// </summary>
        [JsonPropertyName("recommendedLevel")]
        public int RecommendedLevel { get; set; } = 1;

        /// <summary>
        /// 推荐战力
        /// Recommended power
        /// </summary>
        [JsonPropertyName("recommendedPower")]
        public int RecommendedPower { get; set; } = 0;

        /// <summary>
        /// 最大队伍人数
        /// Maximum team size
        /// </summary>
        [JsonPropertyName("maxTeamSize")]
        public int MaxTeamSize { get; set; } = 4;

        /// <summary>
        /// 最小队伍人数
        /// Minimum team size
        /// </summary>
        [JsonPropertyName("minTeamSize")]
        public int MinTeamSize { get; set; } = 1;

        /// <summary>
        /// 副本波次列表
        /// Dungeon wave list
        /// </summary>
        [JsonPropertyName("waves")]
        public List<DungeonWave> Waves { get; set; } = new();

        /// <summary>
        /// 副本完成奖励
        /// Dungeon completion rewards
        /// </summary>
        [JsonPropertyName("completionRewards")]
        public List<LootDrop> CompletionRewards { get; set; } = new();

        /// <summary>
        /// 首次通关奖励
        /// First clear rewards
        /// </summary>
        [JsonPropertyName("firstClearRewards")]
        public List<LootDrop>? FirstClearRewards { get; set; }

        /// <summary>
        /// 重复刷新冷却时间（秒）
        /// Repeat cooldown time (seconds)
        /// </summary>
        [JsonPropertyName("repeatCooldownSec")]
        public double RepeatCooldownSec { get; set; } = 0;

        /// <summary>
        /// 是否允许自动战斗
        /// Whether auto-battle is allowed
        /// </summary>
        [JsonPropertyName("allowAutoBattle")]
        public bool AllowAutoBattle { get; set; } = true;

        /// <summary>
        /// 是否允许复活
        /// Whether revival is allowed
        /// </summary>
        [JsonPropertyName("allowRevive")]
        public bool AllowRevive { get; set; } = true;

        /// <summary>
        /// 副本时间限制（秒，0表示无限制）
        /// Dungeon time limit (seconds, 0 means no limit)
        /// </summary>
        [JsonPropertyName("timeLimitSec")]
        public double TimeLimitSec { get; set; } = 0;

        /// <summary>
        /// 战斗配置（可选，不设置则使用默认值）
        /// Battle configuration (optional, uses defaults if not set)
        /// </summary>
        [JsonPropertyName("battleConfig")]
        public MultiBattleConfig? BattleConfig { get; set; }

        /// <summary>
        /// 战斗配置ID（引用 battleConfigs.json 中的配置）
        /// Battle configuration ID (references config in battleConfigs.json)
        /// </summary>
        [JsonPropertyName("battleConfigId")]
        public string? BattleConfigId { get; set; }
    }

    /// <summary>
    /// 副本波次定义
    /// Dungeon wave definition
    /// </summary>
    public class DungeonWave
    {
        /// <summary>
        /// 波次编号（从1开始）
        /// Wave number (starts from 1)
        /// </summary>
        [JsonPropertyName("waveNumber")]
        public int WaveNumber { get; set; }

        /// <summary>
        /// 波次名称（可选）
        /// Wave name (optional)
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// 波次描述（可选）
        /// Wave description (optional)
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 波次类型
        /// Wave type
        /// </summary>
        [JsonPropertyName("waveType")]
        public WaveType WaveType { get; set; } = WaveType.Normal;

        /// <summary>
        /// 波次中的怪物组
        /// Monster groups in wave
        /// </summary>
        [JsonPropertyName("monsterGroups")]
        public List<MonsterGroup> MonsterGroups { get; set; } = new();

        /// <summary>
        /// 波次开始延迟（秒）
        /// Wave start delay (seconds)
        /// </summary>
        [JsonPropertyName("waveStartDelaySec")]
        public double WaveStartDelaySec { get; set; } = 2.0;

        /// <summary>
        /// 波次完成后延迟（秒）
        /// Delay after wave completion (seconds)
        /// </summary>
        [JsonPropertyName("waveEndDelaySec")]
        public double WaveEndDelaySec { get; set; } = 1.5;

        /// <summary>
        /// 波次奖励（可选）
        /// Wave rewards (optional)
        /// </summary>
        [JsonPropertyName("waveRewards")]
        public List<LootDrop>? WaveRewards { get; set; }

        /// <summary>
        /// 胜利条件（默认消灭所有怪物）
        /// Victory condition (default: eliminate all monsters)
        /// </summary>
        [JsonPropertyName("victoryCondition")]
        public VictoryCondition VictoryCondition { get; set; } = VictoryCondition.EliminateAll;

        /// <summary>
        /// 时间限制（秒，0表示无限制）
        /// Time limit (seconds, 0 means no limit)
        /// </summary>
        [JsonPropertyName("timeLimitSec")]
        public double TimeLimitSec { get; set; } = 0;
    }

    /// <summary>
    /// 怪物组 - 定义一组同时出现的怪物
    /// Monster group - defines a group of monsters that appear together
    /// </summary>
    public class MonsterGroup
    {
        /// <summary>
        /// 怪物ID（引用MonsterDef）
        /// Monster ID (references MonsterDef)
        /// </summary>
        [JsonPropertyName("monsterId")]
        public string MonsterId { get; set; } = string.Empty;

        /// <summary>
        /// 怪物数量
        /// Monster count
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; } = 1;

        /// <summary>
        /// 出现延迟（秒，相对于波次开始）
        /// Spawn delay (seconds, relative to wave start)
        /// </summary>
        [JsonPropertyName("spawnDelaySec")]
        public double SpawnDelaySec { get; set; } = 0;

        /// <summary>
        /// 生命值倍率
        /// HP multiplier
        /// </summary>
        [JsonPropertyName("hpMultiplier")]
        public double HpMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 伤害倍率
        /// Damage multiplier
        /// </summary>
        [JsonPropertyName("damageMultiplier")]
        public double DamageMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 攻速倍率
        /// Attack speed multiplier
        /// </summary>
        [JsonPropertyName("attackSpeedMultiplier")]
        public double AttackSpeedMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 是否为精英怪物
        /// Whether it's an elite monster
        /// </summary>
        [JsonPropertyName("isElite")]
        public bool IsElite { get; set; } = false;

        /// <summary>
        /// 是否为Boss
        /// Whether it's a boss
        /// </summary>
        [JsonPropertyName("isBoss")]
        public bool IsBoss { get; set; } = false;

        /// <summary>
        /// 特殊掉落（覆盖怪物默认掉落）
        /// Special drops (overrides monster default drops)
        /// </summary>
        [JsonPropertyName("specialDrops")]
        public List<LootDrop>? SpecialDrops { get; set; }
    }

    /// <summary>
    /// 副本类型
    /// Dungeon type
    /// </summary>
    public enum DungeonType
    {
        /// <summary>
        /// 普通副本
        /// Normal dungeon
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 精英副本
        /// Elite dungeon
        /// </summary>
        Elite = 2,

        /// <summary>
        /// 史诗副本
        /// Epic dungeon
        /// </summary>
        Epic = 3,

        /// <summary>
        /// 传说副本
        /// Legendary dungeon
        /// </summary>
        Legendary = 4,

        /// <summary>
        /// 活动副本
        /// Event dungeon
        /// </summary>
        Event = 5
    }

    /// <summary>
    /// 波次类型
    /// Wave type
    /// </summary>
    public enum WaveType
    {
        /// <summary>
        /// 普通波次
        /// Normal wave
        /// </summary>
        Normal = 1,

        /// <summary>
        /// 精英波次
        /// Elite wave
        /// </summary>
        Elite = 2,

        /// <summary>
        /// Boss波次
        /// Boss wave
        /// </summary>
        Boss = 3,

        /// <summary>
        /// 奖励波次
        /// Bonus wave
        /// </summary>
        Bonus = 4,

        /// <summary>
        /// 生存波次
        /// Survival wave
        /// </summary>
        Survival = 5
    }

    /// <summary>
    /// 胜利条件
    /// Victory condition
    /// </summary>
    public enum VictoryCondition
    {
        /// <summary>
        /// 消灭所有敌人
        /// Eliminate all enemies
        /// </summary>
        EliminateAll = 1,

        /// <summary>
        /// 消灭指定目标
        /// Eliminate specific targets
        /// </summary>
        EliminateTarget = 2,

        /// <summary>
        /// 存活指定时间
        /// Survive for specified time
        /// </summary>
        Survive = 3,

        /// <summary>
        /// 保护目标
        /// Protect target
        /// </summary>
        Protect = 4
    }

    /// <summary>
    /// 战斗场景定义 - 用于BattleDemo等单次战斗场景
    /// Battle scenario definition - used for single battle scenarios like BattleDemo
    /// </summary>
    public class BattleScenarioDef
    {
        /// <summary>
        /// 场景唯一标识
        /// Scenario unique identifier
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 场景名称
        /// Scenario name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 场景描述
        /// Scenario description
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 怪物组列表 - 定义这个场景中出现的所有怪物
        /// Monster groups list - defines all monsters that appear in this scenario
        /// </summary>
        [JsonPropertyName("monsterGroups")]
        public List<MonsterGroup> MonsterGroups { get; set; } = new();

        /// <summary>
        /// 战斗配置（可选，不设置则使用默认值）
        /// Battle configuration (optional, uses defaults if not set)
        /// </summary>
        [JsonPropertyName("battleConfig")]
        public MultiBattleConfig? BattleConfig { get; set; }

        /// <summary>
        /// 战斗配置ID（引用 battleConfigs.json 中的配置）
        /// Battle configuration ID (references config in battleConfigs.json)
        /// </summary>
        [JsonPropertyName("battleConfigId")]
        public string? BattleConfigId { get; set; }
    }

    /// <summary>
    /// 战斗配置定义 - 可复用的战斗参数配置
    /// Battle configuration definition - reusable battle parameter configuration
    /// </summary>
    public class BattleConfigDef
    {
        /// <summary>
        /// 配置唯一标识
        /// Configuration unique identifier
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 配置名称
        /// Configuration name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 配置描述
        /// Configuration description
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 玩家目标选择策略
        /// Player target selection strategy
        /// </summary>
        [JsonPropertyName("playerTargetStrategy")]
        public TargetStrategy PlayerTargetStrategy { get; set; } = TargetStrategy.Random;

        /// <summary>
        /// 敌人目标选择策略
        /// Enemy target selection strategy
        /// </summary>
        [JsonPropertyName("enemyTargetStrategy")]
        public TargetStrategy EnemyTargetStrategy { get; set; } = TargetStrategy.Random;

        /// <summary>
        /// 特殊技能是否为AOE
        /// Whether special skill is AOE
        /// </summary>
        [JsonPropertyName("specialIsAoe")]
        public bool SpecialIsAoe { get; set; } = true;

        /// <summary>
        /// AOE伤害倍率
        /// AOE damage multiplier
        /// </summary>
        [JsonPropertyName("aoeDamageMultiplier")]
        public double AoeDamageMultiplier { get; set; } = 0.8;

        /// <summary>
        /// 是否允许玩家复活
        /// Whether to allow player revival
        /// </summary>
        [JsonPropertyName("allowPlayerRevive")]
        public bool AllowPlayerRevive { get; set; } = true;

        /// <summary>
        /// 是否允许敌人刷新
        /// Whether to allow enemy respawn
        /// </summary>
        [JsonPropertyName("allowEnemyRespawn")]
        public bool AllowEnemyRespawn { get; set; } = true;

        /// <summary>
        /// 玩家复活冷却时间（毫秒）
        /// Player revive cooldown in milliseconds
        /// </summary>
        [JsonPropertyName("playerReviveCooldownMs")]
        public int PlayerReviveCooldownMs { get; set; } = 5000;

        /// <summary>
        /// 敌人刷新冷却时间（毫秒）
        /// Enemy respawn cooldown in milliseconds
        /// </summary>
        [JsonPropertyName("enemyRespawnCooldownMs")]
        public int EnemyRespawnCooldownMs { get; set; } = 3000;

        /// <summary>
        /// 复活时是否满血
        /// Whether to revive with full HP
        /// </summary>
        [JsonPropertyName("reviveWithFullHp")]
        public bool ReviveWithFullHp { get; set; } = true;

        /// <summary>
        /// 转换为 MultiBattleConfig
        /// Convert to MultiBattleConfig
        /// </summary>
        public MultiBattleConfig ToMultiBattleConfig()
        {
            return new MultiBattleConfig
            {
                PlayerTargetStrategy = PlayerTargetStrategy,
                EnemyTargetStrategy = EnemyTargetStrategy,
                SpecialIsAoe = SpecialIsAoe,
                AoeDamageMultiplier = AoeDamageMultiplier,
                AllowPlayerRevive = AllowPlayerRevive,
                AllowEnemyRespawn = AllowEnemyRespawn,
                PlayerReviveCooldownMs = PlayerReviveCooldownMs,
                EnemyRespawnCooldownMs = EnemyRespawnCooldownMs,
                ReviveWithFullHp = ReviveWithFullHp
            };
        }
    }
}