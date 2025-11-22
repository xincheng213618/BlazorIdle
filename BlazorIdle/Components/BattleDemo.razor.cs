using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlazorIdle.Game;
using BlazorIdle.Game.Config;
using BlazorIdle.Game.Skills;
using BlazorIdle.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorIdle.Components
{
    public partial class BattleDemo
    {
        // Phase 9: SkillRepository for skill name lookup
        private readonly SkillRepository _skillRepository = new SkillRepository();
        // ===== 可配置常量 - Configurable Constants =====

        // 战斗循环间隔（毫秒）- 控制游戏更新频率
        // Battle loop interval (ms) - controls game update frequency
        private const int TickMs = 100;

        // 自动循环延迟时间（毫秒）- 地牢完成后到下次开始的等待时间
        // Auto-repeat delay time (ms) - waiting time between dungeon completion and next start
        private const int AutoRepeatDelayMs = 2000;

        // AOE伤害倍率 - 群体技能对每个目标的伤害比例
        // AOE damage multiplier - damage ratio of area attacks to each target
        private const double AoeDamageMultiplier = 0.7;

        // 默认敌人刷新时间（毫秒）- 当怪物配置未指定刷新时间时使用
        // Default enemy respawn time (ms) - used when monster config doesn't specify respawn time
        private const int DefaultEnemyRespawnMs = 3000;

        // 最大日志条目 - 保留的日志记录数量上限
        // Max log entries - maximum number of log records to keep
        private const int MaxLogEntries = 200;

        [Parameter]
        public CharacterData? SelectedCharacter { get; set; }

        [Parameter]
        public EventCallback<CharacterData> OnCharacterDataChanged { get; set; }

        /// <summary>
        /// 战斗模式枚举 - 区分普通战斗与副本战斗
        /// Battle mode enum - differentiates normal and dungeon battle
        /// </summary>
        private enum BattleMode
        {
            Normal,    // 普通战斗模式
            Dungeon    // 副本战斗模式
        }

        // 战斗模式相关
        // Battle mode related
        private BattleMode currentBattleMode = BattleMode.Normal;

        // 配置选择
        // Configuration selection
        private List<ProfessionDef> professions = new();
        private List<MonsterDef> monsters = new();
        private List<BattleScenarioDef> battleScenarios = new();
        private string? selectedScenarioId;
        private BattleScenarioDef? currentScenario => battleScenarios.FirstOrDefault(s => s.Id == selectedScenarioId);
        private string configVersion = "loading";
        private bool configReady = false;

        // 副本战斗配置
        // Dungeon battle related configuration
        private List<DungeonDef> dungeons = new();
        private string? selectedDungeonId;
        private DungeonDef? currentDungeon => dungeons.FirstOrDefault(d => d.Id == selectedDungeonId);
        private DungeonManager? dungeonManager;
        private DungeonSnapshot? dungeonSnapshot;

        // 多单位战斗系统（用于普通战斗与副本战斗）
        // Multi-unit battle system (used for normal battle and dungeon battle)
        private MultiBattleInstance? battle;
        private MultiBattleSnapshot snapshot = new MultiBattleSnapshot();

        // 战斗队伍 - 封装玩家与敌人
        // Battle teams - wrapping single character and monster
        private BattleTeam<Character>? playerTeam;
        private BattleTeam<Enemy>? enemyTeam;

        private BattleDigest? digest;
        private bool isRunning = false;

        // 理论DPS计算（基于角色属性）
        // Theoretical DPS calculation (based on character attributes)
        private double theoreticalDps
        {
            get
            {
                if (SelectedCharacter == null) return 0.0;

                var hasteFactor = 1.0 + SelectedCharacter.HastePercent / 100.0;
                var attackDps = SelectedCharacter.DamagePerAttack * SelectedCharacter.AttackRateAPS * hasteFactor;
                var specialDps = SelectedCharacter.SpecialDamage / Math.Max(0.1, SelectedCharacter.SpecialIntervalSec);
                return attackDps + specialDps;
            }
        }

        // 状态文本 - 根据战斗状态显示不同提示
        // Status text - displays different prompt messages based on battle state
        private string PlayerStatusText
        {
            get
            {
                if (snapshot.State == MultiBattleState.PlayerTeamDeadCooldown)
                    return $"等待复活 {(snapshot.TimeToResumeMs / 1000.0):0.00}s";
                return "作战中";
            }
        }

        private string EnemyStatusText
        {
            get
            {
                if (snapshot.State == MultiBattleState.EnemyTeamDeadCooldown)
                    return $"等待刷新 {(snapshot.TimeToResumeMs / 1000.0):0.00}s";
                return "作战中";
            }
        }

        // 从战斗实例获取角色攻击进度
        // Get character attack progress from battle instance
        private double attackProgress01
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return 0.0;
                return battle.GetCharacterAttackProgress(SelectedCharacter.Id);
            }
        }

        // 获取角色技能进度
        // Get character special skill progress
        private double specialProgress01
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return 0.0;
                return battle.GetCharacterSpecialProgress(SelectedCharacter.Id);
            }
        }

        // 获取敌人攻击进度 - 暂不显示（多敌人场景）
        // Get enemy attack progress - not displayed for now (in multi-enemy scenario)
        private double enemyProgress01 => 0.0;

        // 获取角色攻击剩余时间
        // Get character attack time remaining
        private double attackRemainMs
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return 0.0;
                return battle.GetCharacterAttackTimeRemaining(SelectedCharacter.Id);
            }
        }

        // 获取角色技能剩余时间
        // Get character special skill time remaining
        private double specialRemainMs
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return 0.0;
                return battle.GetCharacterSpecialTimeRemaining(SelectedCharacter.Id);
            }
        }

        // 获取敌人攻击剩余时间 - 暂不显示（多敌人场景）
        // Get enemy attack time remaining - not displayed for now (in multi-enemy scenario)
        private double enemyRemainMs => 0.0;

        // Phase 8: 获取施法状态
        // Phase 8: Get casting state
        private bool isCasting
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return false;
                return battle.IsCastingForCharacter(SelectedCharacter.Id);
            }
        }

        private double castingProgress01
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return 0.0;
                return battle.GetCastingProgress(SelectedCharacter.Id);
            }
        }

        private double castingRemainMs
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return 0.0;
                return battle.GetCastingTimeRemaining(SelectedCharacter.Id);
            }
        }

        private string? castingSkillName
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return null;
                var skillId = battle.GetCastingSkillId(SelectedCharacter.Id);
                if (string.IsNullOrEmpty(skillId)) return null;
                
                // 从 SkillRepository 获取技能名称
                // Get skill name from SkillRepository
                var skill = battle.GetSkillRepository()?.GetSkillById(skillId);
                return skill?.Name ?? skillId;
            }
        }

        // Phase 2.5/2.7: 获取玩家资源信息
        // Phase 2.5/2.7: Get player resource information
        private Dictionary<string, int>? playerResources
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return null;
                var resourceSnapshot = battle.GetResourceSnapshot();
                return resourceSnapshot.GetValueOrDefault(SelectedCharacter.Id);
            }
        }

        // Phase 2.7: 获取玩家职业资源配置
        // Phase 2.7: Get player profession resource configuration
        private ProfessionResourceConfig? playerResourceConfig
        {
            get
            {
                if (SelectedCharacter == null) return null;
                var professionId = SelectedCharacter.ActiveCombatProfessionId;
                if (string.IsNullOrEmpty(professionId)) return null;
                
                if (GameConfig.ProfessionAttributes.TryGetValue(professionId, out var profAttr))
                {
                    return profAttr.Resource;
                }
                return null;
            }
        }

        // Phase 9: 获取玩家 Buff 列表
        // Phase 9: Get player buff list
        private List<BlazorIdle.Game.Buffs.BuffInstance>? playerBuffs
        {
            get
            {
                if (battle == null || SelectedCharacter == null) return null;
                return battle.GetPlayerBuffs(SelectedCharacter.Id);
            }
        }

        // Phase 10.3: 获取玩家装备的技能列表（带缓存优化）
        // Phase 10.3: Get player equipped skills list (with caching optimization)
        private List<CharacterPanel.EquippedSkillData>? playerEquippedSkills
        {
            get
            {
                if (battle == null || SelectedCharacter == null)
                {
                    _cachedPlayerSkills = null;
                    _lastSkillConfigKey = null;
                    return null;
                }
                
                var professionId = SelectedCharacter.ActiveCombatProfessionId ?? SelectedCharacter.ProfessionId;
                if (!SelectedCharacter.EquippedSkillsByProfession.TryGetValue(professionId, out var equipConfig))
                {
                    _cachedPlayerSkills = null;
                    _lastSkillConfigKey = null;
                    return null;
                }
                
                // 生成配置键用于检测变化 / Generate config key to detect changes
                var activeSkills = string.Join(",", equipConfig.ActiveSlots.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value}"));
                var configKey = $"{professionId}|{activeSkills}|{equipConfig.PassiveSlot}";
                
                // 如果配置未变化且缓存存在，更新冷却时间后返回缓存 / If config unchanged and cache exists, update cooldowns and return cache
                if (_lastSkillConfigKey == configKey && _cachedPlayerSkills != null)
                {
                    // 只更新冷却时间和资源状态（性能优化）/ Only update cooldowns and resource status (performance optimization)
                    foreach (var skillData in _cachedPlayerSkills)
                    {
                        if (skillData.Skill != null)
                        {
                            skillData.RemainingCooldown = battle.GetSkillRemainingCooldown(SelectedCharacter.Id, skillData.Skill.Id);
                            skillData.IsResourceInsufficient = !CheckSkillResourceSufficient(skillData.Skill, SelectedCharacter.Id);
                        }
                    }
                    return _cachedPlayerSkills;
                }
                
                // 配置已变化，重建列表 / Config changed, rebuild list
                var skillRepo = battle.GetSkillRepository();
                if (skillRepo == null)
                {
                    _cachedPlayerSkills = null;
                    _lastSkillConfigKey = null;
                    return null;
                }
                
                var result = new List<CharacterPanel.EquippedSkillData>();
                
                // 主动技能槽位 / Active skill slots
                foreach (var kvp in equipConfig.ActiveSlots.OrderBy(x => x.Key))
                {
                    var skillId = kvp.Value;
                    if (!string.IsNullOrEmpty(skillId))
                    {
                        var skill = skillRepo.GetSkill(skillId);
                        if (skill != null)
                        {
                            result.Add(new CharacterPanel.EquippedSkillData
                            {
                                Skill = skill,
                                SlotId = kvp.Key,
                                RemainingCooldown = battle.GetSkillRemainingCooldown(SelectedCharacter.Id, skillId),
                                IsResourceInsufficient = !CheckSkillResourceSufficient(skill, SelectedCharacter.Id),
                                IsConditionNotMet = false, // TODO: 实际检查条件 / Actually check conditions
                                JustTriggered = false
                            });
                        }
                    }
                }
                
                // 被动技能槽位 / Passive skill slot
                if (!string.IsNullOrEmpty(equipConfig.PassiveSlot))
                {
                    var skill = skillRepo.GetSkill(equipConfig.PassiveSlot);
                    if (skill != null)
                    {
                        result.Add(new CharacterPanel.EquippedSkillData
                        {
                            Skill = skill,
                            SlotId = "passive_1",
                            RemainingCooldown = battle.GetSkillRemainingCooldown(SelectedCharacter.Id, equipConfig.PassiveSlot),
                            IsResourceInsufficient = !CheckSkillResourceSufficient(skill, SelectedCharacter.Id),
                            IsConditionNotMet = false,
                            JustTriggered = false
                        });
                    }
                }
                
                _cachedPlayerSkills = result.Count > 0 ? result : null;
                _lastSkillConfigKey = configKey;
                return _cachedPlayerSkills;
            }
        }

        // 缓存玩家装备技能列表 - Cache player equipped skills list
        private List<CharacterPanel.EquippedSkillData>? _cachedPlayerSkills = null;
        private string? _lastSkillConfigKey = null; // 用于检测技能配置变化 / Used to detect skill config changes

        /// <summary>
        /// Phase 10.6: 检查技能资源是否充足 / Check if skill has sufficient resources
        /// </summary>
        private bool CheckSkillResourceSufficient(Game.Skills.SkillDef skill, string characterId)
        {
            if (battle == null || skill == null)
                return true; // 无法检查时默认充足 / Default to sufficient when unable to check

            // 获取角色当前资源 / Get character's current resources
            var resourceSnapshot = battle.GetResourceSnapshot();
            if (!resourceSnapshot.TryGetValue(characterId, out var resources))
                return true; // 没有资源信息时默认充足 / Default to sufficient when no resource info

            // 检查所有资源消耗 / Check all resource costs
            if (skill.Costs != null && skill.Costs.Count > 0)
            {
                foreach (var cost in skill.Costs)
                {
                    var currentAmount = resources.GetValueOrDefault(cost.BucketId, 0);
                    if (currentAmount < cost.Amount)
                    {
                        return false; // 资源不足 / Insufficient resource
                    }
                }
            }

            return true; // 资源充足 / Sufficient resources
        }
        
        // 日志列表 - 存储战斗日志
        // Log list - stores battle logs
        private readonly List<string> logs = new();

        private CancellationTokenSource? _cts;

        /// <summary>
        /// 组件初始化 - 加载游戏配置数据
        /// Component initialization - load game configuration data
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            await GameConfig.EnsureLoadedAsync();
            professions = GameConfig.Professions.ToList();
            monsters = GameConfig.Monsters.ToList();
            battleScenarios = GameConfig.BattleScenarios.ToList();
            dungeons = GameConfig.Dungeons.ToList(); // 加载副本配置 - Load dungeon configuration
            configVersion = GameConfig.Version;

            selectedScenarioId = battleScenarios.FirstOrDefault()?.Id;
            selectedDungeonId = dungeons.FirstOrDefault()?.Id;

            configReady = battleScenarios.Count > 0 && monsters.Count > 0;

            if (SelectedCharacter != null)
            {
                ResetBattle();
            }
        }

        protected override void OnParametersSet()
        {
            // 动态创建模式：不再自动创建战斗实例
            // Dynamic creation mode: no longer auto-create battle instances
            // 用户需要显式点击"开始战斗"按钮来创建
            // User must explicitly click "Start Battle" button to create
        }

        /// <summary>
        /// 选择变更回调 - 在场景或副本选择变更时重置战斗
        /// Selection change callback - reset battle when scenario or dungeon selection changes
        /// </summary>
        private void OnSelectionChanged(ChangeEventArgs _)
        {
            if (!isRunning) ResetBattle();
        }

        /// <summary>
        /// 战斗模式切换处理 - 切换普通战斗或副本战斗
        /// Battle mode change handler - switch between normal and dungeon battle
        /// </summary>
        private void OnBattleModeChanged(BattleMode mode)
        {
            if (isRunning) return; // 战斗中不允许切换 - Don't allow switching during battle

            currentBattleMode = mode;
            ResetBattle();
            StateHasChanged();
        }

        private string GetProfessionName(string professionId)
        {
            return professions.FirstOrDefault(p => p.Id == professionId)?.Name ?? professionId;
        }

        /// <summary>
        /// Phase 2.7: 构建职业资源配置映射
        /// Phase 2.7: Build profession resource configuration map
        /// </summary>
        private Dictionary<string, ProfessionResourceConfig>? BuildProfessionResourceConfigs()
        {
            if (GameConfig.ProfessionAttributes == null || GameConfig.ProfessionAttributes.Count == 0)
                return null;

            var configs = new Dictionary<string, ProfessionResourceConfig>();
            foreach (var kvp in GameConfig.ProfessionAttributes)
            {
                if (kvp.Value.Resource != null)
                {
                    configs[kvp.Key] = kvp.Value.Resource;
                }
            }
            return configs.Count > 0 ? configs : null;
        }

        /// <summary>
        /// 构建战斗实例 - 使用 monsterGroups 创建多单位战斗
        /// Build battle instance - creates multi-unit battle using monsterGroups
        /// </summary>
        private void BuildBattle()
        {
            if (SelectedCharacter == null || currentScenario == null) return;

            // 取消旧的战斗实例事件
            if (battle is not null)
            {
                battle.CombatEventFired -= OnCombatEvent;
                battle.LootDropped -= OnLootDropped;
                battle.ExperienceGained -= OnExperienceGained;
                battle.BuffApplied -= OnBuffApplied;
                battle.BuffRemoved -= OnBuffRemoved;
                battle.BuffTicked -= OnBuffTicked;
                battle.Healed -= OnHealed;
            }

            // 构建时钟和随机数上下文
            var clock = new SimClock();
            int seed = HashSeed(SelectedCharacter.Id, currentScenario.Id, configVersion);
            var rng = new RngContext(seed);

            // 构造角色实体
            var character = new Character
            {
                MaxHp = Math.Max(1, SelectedCharacter.MaxHp),
                Hp = Math.Max(1, SelectedCharacter.MaxHp),
                AttackRateAPS = SelectedCharacter.AttackRateAPS,
                DamagePerAttack = SelectedCharacter.DamagePerAttack,
                HastePercent = SelectedCharacter.HastePercent,
                SpecialIntervalSec = SelectedCharacter.SpecialIntervalSec,
                SpecialDamage = SelectedCharacter.SpecialDamage,
                CritChancePercent = SelectedCharacter.CritChancePercent,
                CritMultiplier = SelectedCharacter.CritMultiplier,
                VariancePct = SelectedCharacter.VariancePct,
                ReviveMs = (int)Math.Round(Math.Max(0, SelectedCharacter.ReviveSec) * 1000.0),
                ActiveCombatProfessionId = SelectedCharacter.ActiveCombatProfessionId,
                // Phase 3+: 从CharacterData复制固定技能ID
                // Phase 3+: Copy fixed skill IDs from CharacterData
                NormalAttackSkillId = SelectedCharacter.FixedSkillsByProfession.TryGetValue(SelectedCharacter.ActiveCombatProfessionId, out var fixedSkills) 
                    ? fixedSkills.NormalAttack : null,
                SpecialAttackSkillId = SelectedCharacter.FixedSkillsByProfession.TryGetValue(SelectedCharacter.ActiveCombatProfessionId, out var fixedSkills2) 
                    ? fixedSkills2.SpecialAttack : null
            };

            // 玩家队伍
            playerTeam = new BattleTeam<Character>("player_team", "玩家队伍", TeamType.Player);
            playerTeam.AddMember(SelectedCharacter.Id, character, character.MaxHp);

            // 敌人队伍
            enemyTeam = new BattleTeam<Enemy>("enemy_team", currentScenario.Name, TeamType.Enemy);

            // 敌人
            int enemyIndex = 0;
            foreach (var monsterGroup in currentScenario.MonsterGroups)
            {
                var monsterDef = GameConfig.GetMonster(monsterGroup.MonsterId);
                if (monsterDef == null) continue;

                for (int i = 0; i < monsterGroup.Count; i++)
                {
                    enemyIndex++;

                    int maxHp = (int)Math.Round(Math.Max(1, monsterDef.MaxHp) * monsterGroup.HpMultiplier);
                    int damagePerHit = (int)Math.Round(Math.Max(1, monsterDef.DamagePerHit) * monsterGroup.DamageMultiplier);
                    double attackInterval = Math.Max(0.1, monsterDef.AttackIntervalSec / monsterGroup.AttackSpeedMultiplier);

                    var enemy = new Enemy
                    {
                        MaxHp = maxHp,
                        Hp = maxHp,
                        AttackIntervalSec = attackInterval,
                        DamagePerHit = damagePerHit,
                        VariancePct = monsterDef.VariancePct,
                        RespawnMs = (int)Math.Round(Math.Max(0, monsterDef.RespawnSec) * 1000.0),
                        LootDrops = monsterGroup.SpecialDrops?.ToList() ?? monsterDef.LootDrops?.ToList() ?? new List<Game.Config.LootDrop>(),
                        BaseExperience = monsterDef.BaseExperience,
                        MonsterId = monsterDef.Id,
                        NormalAttackSkillId = monsterDef.NormalAttackSkillId,  // Phase 7: 设置怪物攻击技能ID
                        // Phase 9: Copy skill lists from monster definition
                        CastSkillIds = monsterDef.CastSkillIds,
                        InstantSkillIds = monsterDef.InstantSkillIds,
                        // Step3 Phase 1: Copy periodic skill list
                        PeriodicSkillIds = monsterDef.PeriodicSkillIds
                    };

                    string enemyId = $"{monsterGroup.MonsterId}_{enemyIndex}";
                    enemyTeam.AddMember(enemyId, enemy, enemy.MaxHp);
                }
            }

            // 战斗配置 - 优先使用 battleConfigId，其次使用嵌入的 battleConfig，最后使用默认值
            // Battle config - prioritize battleConfigId, then embedded battleConfig, finally defaults
            MultiBattleConfig config;
            
            if (!string.IsNullOrEmpty(currentScenario.BattleConfigId))
            {
                // 从配置服务获取战斗配置
                var configDef = GameConfig.GetBattleConfig(currentScenario.BattleConfigId);
                config = configDef?.ToMultiBattleConfig() ?? new MultiBattleConfig
                {
                    PlayerTargetStrategy = TargetStrategy.LowestHp,
                    EnemyTargetStrategy = TargetStrategy.Random,
                    SpecialIsAoe = enemyTeam.TotalCount > 1,
                    AoeDamageMultiplier = AoeDamageMultiplier,
                    AllowPlayerRevive = true,
                    AllowEnemyRespawn = true,
                    PlayerReviveCooldownMs = character.ReviveMs,
                    EnemyRespawnCooldownMs = enemyTeam.Members.Any()
                        ? enemyTeam.Members.First().Entity.RespawnMs
                        : DefaultEnemyRespawnMs,
                    ReviveWithFullHp = true
                };
            }
            else if (currentScenario.BattleConfig != null)
            {
                // 使用嵌入的配置（向后兼容）
                config = currentScenario.BattleConfig;
            }
            else
            {
                // 使用默认配置
                config = new MultiBattleConfig
                {
                    PlayerTargetStrategy = TargetStrategy.LowestHp,
                    EnemyTargetStrategy = TargetStrategy.Random,
                    SpecialIsAoe = enemyTeam.TotalCount > 1,
                    AoeDamageMultiplier = AoeDamageMultiplier,
                    AllowPlayerRevive = true,
                    AllowEnemyRespawn = true,
                    PlayerReviveCooldownMs = character.ReviveMs,
                    EnemyRespawnCooldownMs = enemyTeam.Members.Any()
                        ? enemyTeam.Members.First().Entity.RespawnMs
                        : DefaultEnemyRespawnMs,
                    ReviveWithFullHp = true
                };
            }

            // 根据实际情况覆盖某些属性
            // Override certain properties based on actual values
            config.PlayerReviveCooldownMs = character.ReviveMs;
            config.EnemyRespawnCooldownMs = enemyTeam.Members.Any()
                ? enemyTeam.Members.First().Entity.RespawnMs
                : DefaultEnemyRespawnMs;

            // Phase 2.7: 获取职业资源配置
            // Phase 2.7: Get profession resource configurations
            var professionResourceConfigs = BuildProfessionResourceConfigs();

            // Phase 7: 创建 CharacterData 映射用于触发器系统
            // Phase 7: Create CharacterData map for trigger system
            var characterDataMap = new Dictionary<string, Shared.Models.CharacterData>();
            if (SelectedCharacter != null)
            {
                // Phase 10.6: 记录职业切换调试信息 / Log profession switching debug info
                Logger.LogInformation("BuildBattle: ActiveCombatProfessionId = {ProfessionId}", SelectedCharacter.ActiveCombatProfessionId);
                Logger.LogInformation("BuildBattle: EquippedSkillsByProfession keys = {Keys}", 
                    string.Join(", ", SelectedCharacter.EquippedSkillsByProfession.Keys));
                
                if (SelectedCharacter.EquippedSkillsByProfession.TryGetValue(SelectedCharacter.ActiveCombatProfessionId, out var equipConfig))
                {
                    Logger.LogInformation("BuildBattle: Found equipped skills config for {ProfessionId}, ActiveSlots = {Slots}",
                        SelectedCharacter.ActiveCombatProfessionId,
                        string.Join(", ", equipConfig.ActiveSlots.Select(kvp => $"{kvp.Key}:{kvp.Value}")));
                }
                else
                {
                    Logger.LogWarning("BuildBattle: No equipped skills config found for profession {ProfessionId}!",
                        SelectedCharacter.ActiveCombatProfessionId);
                }

                //// Phase 7/8: 临时测试 - 确保角色有装备技能配置
                //// Phase 7/8: Temporary test - ensure character has equipped skills
                //if (!SelectedCharacter.EquippedSkillsByProfession.ContainsKey(SelectedCharacter.ActiveCombatProfessionId))
                //{
                //    // Phase 8: 根据职业设置临时测试技能
                //    // Phase 8: Set temporary test skills based on profession
                //    if (SelectedCharacter.ActiveCombatProfessionId == "mage")
                //    {
                //        // Phase 8: 法师临时技能 - 2个施法技能 + 1个瞬发非GCD技能
                //        // Phase 8: Mage temporary skills - 2 cast skills + 1 instant non-GCD skill
                //        SelectedCharacter.EquippedSkillsByProfession[SelectedCharacter.ActiveCombatProfessionId] = 
                //            new Shared.Models.EquippedSkillsConfig
                //            {
                //                ProfessionId = SelectedCharacter.ActiveCombatProfessionId,
                //                ActiveSlots = new Dictionary<string, string?>
                //                {
                //                    { "active_1", "mage_pyroblast" },      // 施法技能: 2.5s cast
                //                    { "active_2", "mage_frostbolt_cast" }, // 施法技能: 1.8s cast
                //                    { "active_3", "mage_arcane_blast" }    // 瞬发非GCD技能
                //                },
                //                PassiveSlot = null
                //            };
                //    }
                //    else
                //    {
                //        // 其他职业使用战士技能（向后兼容）
                //        // Other professions use warrior skills (backward compatible)
                //        SelectedCharacter.EquippedSkillsByProfession[SelectedCharacter.ActiveCombatProfessionId] = 
                //            new Shared.Models.EquippedSkillsConfig
                //            {
                //                ProfessionId = SelectedCharacter.ActiveCombatProfessionId,
                //                ActiveSlots = new Dictionary<string, string?>
                //                {
                //                    { "active_1", "warrior_mortal_strike" },
                //                    { "active_2", "warrior_thunderclap" },
                //                    { "active_3", "warrior_slam" }
                //                },
                //                PassiveSlot = "warrior_bloodlust"  // Phase 7: 测试被动触发技能
                //            };
                //    }
                //}
                
                characterDataMap[SelectedCharacter.Id] = SelectedCharacter;
            }

            // 创建战斗实例并订阅事件
            battle = new MultiBattleInstance(clock, rng, playerTeam, enemyTeam, config, null, professionResourceConfigs, characterDataMap);
            battle.CombatEventFired += OnCombatEvent;
            battle.LootDropped += OnLootDropped;
            battle.ExperienceGained += OnExperienceGained;
            // Phase 9: 订阅 Buff 事件 / Phase 9: Subscribe to buff events
            battle.BuffApplied += OnBuffApplied;
            battle.BuffRemoved += OnBuffRemoved;
            battle.BuffTicked += OnBuffTicked;
            battle.Healed += OnHealed;

            digest = null;
        }

        /// <summary>
        /// 构建副本战斗 - 创建副本管理器和玩家队伍
        /// Build dungeon battle - create dungeon manager and player team
        /// </summary>
        private void BuildDungeonBattle()
        {
            if (SelectedCharacter == null || currentDungeon == null) return;

            // 取消已有的副本事件订阅
            if (dungeonManager != null)
            {
                dungeonManager.CombatEventFired -= OnCombatEvent;
                dungeonManager.LootDropped -= OnLootDropped;
                dungeonManager.ExperienceGained -= OnExperienceGained;
                dungeonManager.WaveChanged -= OnDungeonWaveChanged;
                dungeonManager.DungeonCompleted -= OnDungeonCompleted;
                dungeonManager.BuffApplied -= OnBuffApplied;
                dungeonManager.BuffRemoved -= OnBuffRemoved;
                dungeonManager.BuffTicked -= OnBuffTicked;
                dungeonManager.Healed -= OnHealed;
            }

            var clock = new SimClock();
            int seed = HashSeed(SelectedCharacter.Id, currentDungeon.Id, configVersion);
            var rng = new RngContext(seed);

            var character = new Character
            {
                MaxHp = Math.Max(1, SelectedCharacter.MaxHp),
                Hp = Math.Max(1, SelectedCharacter.MaxHp),
                AttackRateAPS = SelectedCharacter.AttackRateAPS,
                DamagePerAttack = SelectedCharacter.DamagePerAttack,
                HastePercent = SelectedCharacter.HastePercent,
                SpecialIntervalSec = SelectedCharacter.SpecialIntervalSec,
                SpecialDamage = SelectedCharacter.SpecialDamage,
                CritChancePercent = SelectedCharacter.CritChancePercent,
                CritMultiplier = SelectedCharacter.CritMultiplier,
                VariancePct = SelectedCharacter.VariancePct,
                ReviveMs = (int)Math.Round(Math.Max(0, SelectedCharacter.ReviveSec) * 1000.0),
                ActiveCombatProfessionId = SelectedCharacter.ActiveCombatProfessionId,
                // Phase 3+: 从CharacterData复制固定技能ID
                // Phase 3+: Copy fixed skill IDs from CharacterData
                NormalAttackSkillId = SelectedCharacter.FixedSkillsByProfession.TryGetValue(SelectedCharacter.ActiveCombatProfessionId, out var fixedSkills) 
                    ? fixedSkills.NormalAttack : null,
                SpecialAttackSkillId = SelectedCharacter.FixedSkillsByProfession.TryGetValue(SelectedCharacter.ActiveCombatProfessionId, out var fixedSkills2) 
                    ? fixedSkills2.SpecialAttack : null
            };

            playerTeam = new BattleTeam<Character>("player_team", "玩家队伍", TeamType.Player);
            playerTeam.AddMember(SelectedCharacter.Id, character, character.MaxHp);

            // Phase 2.7: 获取职业资源配置
            // Phase 2.7: Get profession resource configurations
            var professionResourceConfigs = BuildProfessionResourceConfigs();

            // Phase 10.6: 创建角色数据映射用于技能系统
            // Phase 10.6: Create character data map for skill system
            var characterDataMap = new Dictionary<string, Shared.Models.CharacterData>();
            if (SelectedCharacter != null)
            {
                // Phase 10.6: 记录职业切换调试信息 / Log profession switching debug info
                Logger.LogInformation("BuildDungeonBattle: ActiveCombatProfessionId = {ProfessionId}", SelectedCharacter.ActiveCombatProfessionId);
                Logger.LogInformation("BuildDungeonBattle: EquippedSkillsByProfession keys = {Keys}", 
                    string.Join(", ", SelectedCharacter.EquippedSkillsByProfession.Keys));
                
                if (SelectedCharacter.EquippedSkillsByProfession.TryGetValue(SelectedCharacter.ActiveCombatProfessionId, out var equipConfig))
                {
                    Logger.LogInformation("BuildDungeonBattle: Found equipped skills config for {ProfessionId}, ActiveSlots = {Slots}",
                        SelectedCharacter.ActiveCombatProfessionId,
                        string.Join(", ", equipConfig.ActiveSlots.Select(kvp => $"{kvp.Key}:{kvp.Value}")));
                }
                else
                {
                    Logger.LogWarning("BuildDungeonBattle: No equipped skills config found for profession {ProfessionId}!",
                        SelectedCharacter.ActiveCombatProfessionId);
                }
                
                characterDataMap[SelectedCharacter.Id] = SelectedCharacter;
            }

            dungeonManager = new DungeonManager(currentDungeon, clock, rng, playerTeam, GameConfig, professionResourceConfigs, characterDataMap);

            // 默认开启自动循环
            dungeonManager.EnableAutoRepeat(AutoRepeatDelayMs);

            dungeonManager.CombatEventFired += OnCombatEvent;
            dungeonManager.LootDropped += OnLootDropped;
            dungeonManager.ExperienceGained += OnExperienceGained;
            dungeonManager.WaveChanged += OnDungeonWaveChanged;
            dungeonManager.DungeonCompleted += OnDungeonCompleted;
            // Phase 9: 订阅 Buff 事件 / Phase 9: Subscribe to buff events
            dungeonManager.BuffApplied += OnBuffApplied;
            dungeonManager.BuffRemoved += OnBuffRemoved;
            dungeonManager.BuffTicked += OnBuffTicked;
            dungeonManager.Healed += OnHealed;

            dungeonSnapshot = null;
        }

        /// <summary>
        /// 生成哈希种子 - 用于确定性随机数
        /// Generate hash seed - for deterministic random number generation
        /// </summary>
        private int HashSeed(params object[] arr)
        {
            unchecked
            {
                int h = 17;
                foreach (var o in arr) h = h * 31 + (o?.GetHashCode() ?? 0);
                return h == 0 ? 1234567 : h;
            }
        }

        /// <summary>
        /// 开始战斗 - 根据当前模式启动普通战斗或副本战斗
        /// Start battle - starts normal battle or dungeon battle based on current mode
        /// </summary>
        private void StartBattle()
        {
            if (!configReady || isRunning) return;

            logs.Clear();

            if (currentBattleMode == BattleMode.Normal)
            {
                BuildBattle();
                battle!.Start();
                snapshot = battle!.GetSnapshot();
                digest = null;
            }
            else
            {
                BuildDungeonBattle();
                dungeonManager!.StartDungeon();
                dungeonSnapshot = dungeonManager.GetSnapshot();
            }

            isRunning = true;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = RunLoopAsync(_cts.Token);
        }

        /// <summary>
        /// 停止战斗 - 停止当前战斗并清理资源
        /// Stop battle - stops current battle and cleans up resources
        /// </summary>
        private void StopBattle()
        {
            if (!isRunning) return;
            isRunning = false;
            _cts?.Cancel();

            if (currentBattleMode == BattleMode.Normal)
            {
                battle?.Stop();
                if (battle != null)
                {
                    snapshot = battle.GetSnapshot();
                    digest ??= battle.BuildDigest();
                }
            }
            else
            {
                dungeonManager?.StopDungeon();
                battle = null;

                if (dungeonManager != null)
                {
                    dungeonSnapshot = dungeonManager.GetSnapshot();
                }
            }

            StateHasChanged();
        }

        /// <summary>
        /// 重置战斗 - 停止当前战斗并清理实例（不重新构建，等待开始战斗时创建）
        /// Reset battle - stop current battle and cleanup instances (don't rebuild, wait for start to create)
        /// </summary>
        private void ResetBattle()
        {
            StopBattle();

            // 清理战斗实例和队伍（动态创建模式：只在开始战斗时创建）
            // Cleanup battle instances and teams (dynamic creation mode: only create when starting battle)
            battle = null;
            playerTeam = null;
            enemyTeam = null;
            dungeonManager = null;
            
            // 重置快照为初始状态 / Reset snapshots to initial state
            snapshot = new MultiBattleSnapshot();
            dungeonSnapshot = null;

            StateHasChanged();
        }

        /// <summary>
        /// 战斗循环 - 定期更新战斗状态
        /// Battle loop - periodically updates battle state
        /// </summary>
        private async Task RunLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && isRunning)
                {
                    await Task.Delay(TickMs, token);

                    if (currentBattleMode == BattleMode.Normal)
                    {
                        battle!.AdvanceTick(TickMs);
                        snapshot = battle!.GetSnapshot();
                    }
                    else
                    {
                        dungeonManager!.AdvanceTick(TickMs);
                        dungeonSnapshot = dungeonManager.GetSnapshot();

                        if (dungeonSnapshot?.CurrentEnemyTeam != null)
                        {
                            enemyTeam = dungeonSnapshot.CurrentEnemyTeam;

                            if (dungeonManager.CurrentBattle is not null)
                            {
                                battle = dungeonManager.CurrentBattle;
                            }

                            if (dungeonSnapshot.BattleSnapshot != null)
                            {
                                snapshot = dungeonSnapshot.BattleSnapshot;
                            }
                        }
                    }

                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (TaskCanceledException) { }
        }

        /// <summary>
        /// 战斗事件处理 - 处理伤害事件并记录日志
        /// Combat event handler - handles damage events in battle and logs them
        /// </summary>
        private void OnCombatEvent(MultiCombatEvent ev)
        {
            var sec = ev.TimeMs / 1000.0;

            // Phase 9: 更新 EventSource 显示，支持新的事件类型
            // Phase 9: Update EventSource display to support new event types
            var src = ev.Source switch
            {
                EventSource.Attack => "普攻",
                EventSource.Special => "技能",
                EventSource.EnemyAttack => "攻击",
                EventSource.Cast => "施法",
                EventSource.Skill => "技能",
                EventSource.Trigger => "触发",
                EventSource.PostAttack => "技能",  // PostAttack 窗口触发的技能
                EventSource.PostCast => "技能",    // PostCast 窗口触发的技能
                _ => "未知"
            };

            // Phase 9: 如果有技能ID，获取技能名称
            // Phase 9: If skill ID exists, get skill name from repository
            string? skillName = null;
            if (!string.IsNullOrEmpty(ev.SkillId))
            {
                var skillDef = _skillRepository.GetSkill(ev.SkillId);
                // 使用技能名称，如果没有名称则fallback到ID
                // Use skill name, fallback to ID if name is not available
                skillName = !string.IsNullOrEmpty(skillDef?.Name) ? skillDef.Name : ev.SkillId;
            }

            var attackerName = ev.Attacker == ActorType.Player
                ? (SelectedCharacter?.Name ?? ev.AttackerName ?? ev.AttackerId)
                : (GetEnemyDisplayName(ev.AttackerId) ?? ev.AttackerName ?? ev.AttackerId);

            var defenderName = ev.Defender == ActorType.Player
                ? (SelectedCharacter?.Name ?? ev.DefenderName ?? ev.DefenderId)
                : (GetEnemyDisplayName(ev.DefenderId) ?? ev.DefenderName ?? ev.DefenderId);

            // Phase 9: 如果有技能名称，显示 "技能名 (技能)" 格式，否则只显示事件源
            // Phase 9: If skill name exists, display "SkillName (Source)" format, otherwise just show event source
            var actionDesc = skillName != null ? $"{skillName} ({src})" : src;
            
            var line =
                $"[{sec:0.00}s] {attackerName} {actionDesc} 对 {defenderName} 造成 {ev.Damage} 伤害，{defenderName} HP：{ev.DefenderHpAfter}";

            // Phase 9: 显示暴击标记
            // Phase 9: Show crit indicator
            if (ev.Crit) line += " [暴击!]";
            if (ev.IsAoe) line += " [AOE]";
            if (ev.IsKill) line += " [击杀!]";

            logs.Add(line);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);

            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 获取敌人显示名 - 从ID提取怪物类型并返回对应名称
        /// Get enemy display name - extracts monster type from ID and gets corresponding name
        /// </summary>
        private string GetEnemyDisplayName(string enemyId)
        {
            var parts = enemyId.Split('_');
            if (parts.Length >= 2)
            {
                var monsterId = parts[0];
                var index = parts[1];
                var monsterDef = GameConfig.GetMonster(monsterId);
                if (monsterDef != null)
                {
                    return $"{monsterDef.Name}#{index}";
                }
            }
            return enemyId;
        }

        /// <summary>
        /// 清空战斗日志
        /// Clear combat logs
        /// </summary>
        private void ClearLogs()
        {
            logs.Clear();
            StateHasChanged();
        }

        /// <summary>
        /// 获取玩家当前生命值 - 从玩家队伍读取
        /// Get player current HP - retrieved from player team
        /// </summary>
        private int GetPlayerHp()
        {
            if (playerTeam == null || SelectedCharacter == null) return 0;
            var member = playerTeam.GetMember(SelectedCharacter.Id);
            return member?.CurrentHp ?? 0;
        }

        /// <summary>
        /// 获取玩家最大生命值 - 从玩家队伍读取
        /// Get player max HP - retrieved from player team
        /// </summary>
        private int GetPlayerMaxHp()
        {
            if (playerTeam == null || SelectedCharacter == null) return 1;
            var member = playerTeam.GetMember(SelectedCharacter.Id);
            return member?.MaxHp ?? 1;
        }

        /// <summary>
        /// 获取敌人当前生命值 - 多敌人场景返回总和
        /// Get enemy current HP - returns sum in multi-enemy scenario
        /// </summary>
        private int GetEnemyHp()
        {
            if (enemyTeam == null) return 0;
            return enemyTeam.Members.Sum(m => m.CurrentHp);
        }

        /// <summary>
        /// 获取敌人最大生命值 - 多敌人场景返回总和
        /// Get enemy max HP - returns sum in multi-enemy scenario
        /// </summary>
        private int GetEnemyMaxHp()
        {
            if (enemyTeam == null) return 1;
            var total = enemyTeam.Members.Sum(m => m.MaxHp);
            return total > 0 ? total : 1;
        }

        /// <summary>
        /// 战利品掉落事件 - 添加到角色背包并记录统一格式日志
        /// Handle loot drop event - adds loot to character inventory and logs it
        /// </summary>
        private void OnLootDropped(LootDropEvent lootEvent)
        {
            if (SelectedCharacter == null) return;

            SelectedCharacter.Inventory.AddItem(lootEvent.ItemId, lootEvent.Quantity);

            var sec = lootEvent.TimeMs / 1000.0;
            var charName = string.IsNullOrWhiteSpace(SelectedCharacter?.Name) ? "未知角色" : SelectedCharacter!.Name;
            var itemDef = GameConfig.GetItem(lootEvent.ItemId);
            var itemName = itemDef?.Name ?? lootEvent.ItemId;

            var logLine = $"[{sec:0.00}s] {charName} 获得了 {itemName} x{lootEvent.Quantity}";

            logs.Add(logLine);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);

            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 处理经验获得事件 - 将经验添加到对应职业并记录日志
        /// Handle experience gain event - adds experience to profession and logs it
        /// </summary>
        private async void OnExperienceGained(ExperienceGainEvent expEvent)
        {
            if (SelectedCharacter == null) return;

            // 获取对应职业的进度
            if (!SelectedCharacter.Professions.TryGetValue(expEvent.ProfessionId, out var progress))
            {
                return;
            }

            // TODO: 应用增益系数（预留给未来的buff系统）
            // TODO: Apply multiplier (reserved for future buff system)
            double multiplier = 0.0;
            long actualExp = (long)(expEvent.BaseExperience * (1.0 + multiplier));

            // 添加经验
            progress.Experience += actualExp;

            // 检查升级
            bool leveledUp = false;
            int oldLevel = progress.Level;
            int maxLevel = GameConfig.MaxProfessionLevel;
            while (progress.Experience >= progress.ExperienceToNext && progress.Level < maxLevel)
            {
                // 升级
                progress.Level++;
                progress.Experience -= progress.ExperienceToNext;
                
                // 更新下一级所需经验（简化实现，使用固定增长）
                // TODO: 应该从经验曲线配置加载
                progress.ExperienceToNext = (long)(progress.ExperienceToNext * 1.5);
                
                leveledUp = true;
            }

            // 如果升级且是激活职业，触发属性重算并更新战斗实例
            // If leveled up and is active profession, recalculate attributes and update battle instance
            if (leveledUp && expEvent.ProfessionId == SelectedCharacter.ActiveCombatProfessionId)
            {
                try
                {
                    // 重新计算并应用属性
                    // Recalculate and apply attributes
                    await AttributeService.RecalculateAndApplyAsync(SelectedCharacter);
                    Logger.LogInformation(
                        "Recalculated attributes for character {CharacterId} after level-up from {OldLevel} to {NewLevel}",
                        SelectedCharacter.Id, oldLevel, progress.Level);

                    // 更新战斗角色实例的属性
                    // Update battle character instance attributes
                    if (playerTeam != null && playerTeam.Members.Count > 0)
                    {
                        var battleMember = playerTeam.Members[0];
                        var battleChar = battleMember.Entity;
                        
                        // Update member's max HP (also scales current HP proportionally)
                        battleMember.UpdateMaxHp(SelectedCharacter.MaxHp);
                        
                        // Update character entity stats
                        battleChar.MaxHp = SelectedCharacter.MaxHp;
                        battleChar.Hp = Math.Min(battleChar.Hp, battleChar.MaxHp);
                        battleChar.DamagePerAttack = SelectedCharacter.DamagePerAttack;
                        battleChar.HastePercent = SelectedCharacter.HastePercent;
                        battleChar.CritChancePercent = SelectedCharacter.CritChancePercent;
                        battleChar.CritMultiplier = SelectedCharacter.CritMultiplier;
                        
                        Logger.LogInformation(
                            "Updated combat character instance: HP={HP}/{MaxHP}, Damage={Damage}, Haste={Haste}%, Crit={Crit}%",
                            battleMember.CurrentHp, battleMember.MaxHp, battleChar.DamagePerAttack, 
                            battleChar.HastePercent, battleChar.CritChancePercent);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error recalculating attributes during combat level-up");
                }
            }

            // 记录日志
            var sec = expEvent.TimeMs / 1000.0;
            var charName = string.IsNullOrWhiteSpace(SelectedCharacter?.Name) ? "未知角色" : SelectedCharacter!.Name;
            var profDef = GameConfig.Professions.FirstOrDefault(p => p.Id == expEvent.ProfessionId);
            var profName = profDef?.Name ?? expEvent.ProfessionId;

            var logLine = $"[{sec:0.00}s] {charName} ({profName}) 获得 {actualExp} 经验";
            if (leveledUp)
            {
                logLine += $" - 🎉 升级到 Lv.{progress.Level}！";
                if (expEvent.ProfessionId == SelectedCharacter.ActiveCombatProfessionId)
                {
                    logLine += $" (属性已实时更新)";
                }
            }

            logs.Add(logLine);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);

            // 通知父组件角色数据已更改
            // Notify parent component that character data has changed
            if (OnCharacterDataChanged.HasDelegate && SelectedCharacter != null)
            {
                await OnCharacterDataChanged.InvokeAsync(SelectedCharacter);
            }

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 副本波次变更事件 - 记录波次状态
        /// Dungeon wave change event handler - logs wave status changes
        /// </summary>
        private void OnDungeonWaveChanged(DungeonWaveEvent ev)
        {
            var changeText = ev.ChangeType switch
            {
                WaveChangeType.Preparing => "准备",
                WaveChangeType.Started => "开始",
                WaveChangeType.Completed => "完成",
                _ => ""
            };

            AddLog("副本", $"{changeText} {ev.WaveName}");
        }

        /// <summary>
        /// 副本完成事件 - 记录副本完成或失败
        /// Dungeon complete event handler - logs dungeon completion or failure
        /// </summary>
        private void OnDungeonCompleted(DungeonCompleteEvent ev)
        {
            if (ev.Success)
            {
                AddLog("系统", $"副本通关，第 {ev.CompletionCount} 次完成");
                
                // 处理副本完成经验奖励
                // Handle dungeon completion experience reward
                if (currentDungeon != null && currentDungeon.CompletionExperience > 0 && SelectedCharacter != null)
                {
                    var expEvent = new ExperienceGainEvent
                    {
                        TimeMs = dungeonSnapshot?.ElapsedMs ?? 0,
                        ProfessionId = SelectedCharacter.ActiveCombatProfessionId,
                        BaseExperience = currentDungeon.CompletionExperience,
                        MonsterId = null
                    };
                    OnExperienceGained(expEvent);
                }
            }
            else
            {
                AddLog("系统", "副本失败");
            }
        }

        /// <summary>
        /// 添加日志 - 统一日志格式
        /// Add log - unified log adding method
        /// </summary>
        private void AddLog(string source, string message)
        {
            var time = currentBattleMode == BattleMode.Dungeon && dungeonSnapshot != null
                ? dungeonSnapshot.ElapsedMs
                : snapshot.ElapsedMs;

            var timeStr = $"[{time / 1000.0:F1}s]";
            var logLine = $"{timeStr} [{source}] {message}";

            logs.Add(logLine);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);
        }

        /// <summary>
        /// 获取副本波次进度显示文本
        /// Get dungeon wave progress display text
        /// </summary>
        private string GetDungeonWaveProgress()
        {
            if (dungeonSnapshot == null) return "0 / 0";

            int currentWave = dungeonSnapshot.CurrentWaveIndex + 1;
            int totalWaves = dungeonSnapshot.TotalWaves;

            if (dungeonSnapshot.State == DungeonState.Completed ||
                dungeonSnapshot.State == DungeonState.CompletionDelay)
            {
                return $"{totalWaves} / {totalWaves}";
            }

            currentWave = Math.Min(currentWave, totalWaves);
            return $"{currentWave} / {totalWaves}";
        }

        /// <summary>
        /// 获取副本进度百分比
        /// Get dungeon progress percentage
        /// </summary>
        private int GetDungeonProgressPercent()
        {
            if (dungeonSnapshot == null || dungeonSnapshot.TotalWaves == 0) return 0;

            int currentWave = dungeonSnapshot.CurrentWaveIndex + 1;
            if (dungeonSnapshot.State == DungeonState.Completed ||
                dungeonSnapshot.State == DungeonState.CompletionDelay)
            {
                return 100;
            }

            return Math.Min(100, (int)(currentWave * 100.0 / dungeonSnapshot.TotalWaves));
        }

        /// <summary>
        /// 获取副本状态文本
        /// Get dungeon state text
        /// </summary>
        private string GetDungeonStateText()
        {
            if (dungeonSnapshot == null) return "未开始";

            return dungeonSnapshot.State switch
            {
                DungeonState.NotStarted => "未开始",
                DungeonState.Preparing => "准备中",
                DungeonState.WaveStartDelay => "波次准备",
                DungeonState.Fighting => "作战中",
                DungeonState.WaveEndDelay => "波次间隔",
                DungeonState.CompletionDelay => "等待重新开始",
                DungeonState.Completed => "已完成",
                DungeonState.Failed => "失败",
                DungeonState.Stopped => "已停止",
                _ => dungeonSnapshot.State.ToString()
            };
        }

        /// <summary>
        /// 获取副本状态徽章样式类
        /// Get dungeon state badge CSS class
        /// </summary>
        private string GetDungeonStateBadgeClass()
        {
            if (dungeonSnapshot == null) return "bg-secondary";

            return dungeonSnapshot.State switch
            {
                DungeonState.Fighting => "bg-danger",
                DungeonState.Completed => "bg-success",
                DungeonState.Failed => "bg-dark",
                DungeonState.Preparing or DungeonState.WaveStartDelay or DungeonState.WaveEndDelay => "bg-warning",
                DungeonState.CompletionDelay => "bg-info",
                _ => "bg-secondary"
            };
        }

        /// <summary>
        /// 时长显示格式化
        /// Format duration display - formats time display
        /// </summary>
        private static string FormatDuration(int ms)
        {
            var ts = TimeSpan.FromMilliseconds(ms);
            if (ts.TotalHours >= 1)
            {
                return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            }
            return $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
        }

        // 资源清理 - 取消事件并释放资源
        /// <summary>
        /// Phase 9: Buff 应用事件处理 - 记录 Buff 应用到日志
        /// Phase 9: Buff applied event handler - log buff application
        /// </summary>
        private void OnBuffApplied(BlazorIdle.Game.Buffs.BuffApplyEvent ev)
        {
            var sec = ev.TimeMs / 1000.0;
            var ownerName = GetCharacterOrEnemyName(ev.OwnerId);
            var kindText = ev.Kind == BlazorIdle.Game.Buffs.BuffKind.Buff ? "增益" : "减益";
            var durationText = ev.DurationSec.HasValue ? $"{ev.DurationSec.Value:F1}秒" : "永久";
            
            // Look up buff name from repository for better readability
            var buffConfig = BlazorIdle.Game.Buffs.BuffRepository.Instance.GetBuffById(ev.BuffId);
            var buffName = buffConfig?.Name ?? ev.BuffId;
            
            var line = $"[{sec:0.00}s] {ownerName} 获得 {kindText} [{buffName}]，持续 {durationText}";
            if (ev.Stacks > 1) line += $"，层数：{ev.Stacks}";
            
            logs.Add(line);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);
            
            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Phase 9: Buff 移除事件处理 - 记录 Buff 移除到日志
        /// Phase 9: Buff removed event handler - log buff removal
        /// </summary>
        private void OnBuffRemoved(BlazorIdle.Game.Buffs.BuffRemoveEvent ev)
        {
            var sec = ev.TimeMs / 1000.0;
            var ownerName = GetCharacterOrEnemyName(ev.OwnerId);
            var reasonText = ev.Reason switch
            {
                "expired" => "过期",
                "dispelled" => "被驱散",
                "manual" => "手动移除",
                "skill_effect" => "技能效果",
                _ => ev.Reason
            };
            
            // Look up buff name from repository for better readability
            var buffConfig = BlazorIdle.Game.Buffs.BuffRepository.Instance.GetBuffById(ev.BuffId);
            var buffName = buffConfig?.Name ?? ev.BuffId;
            var kindText = buffConfig?.Kind == BlazorIdle.Game.Buffs.BuffKind.Buff ? "增益" : "减益";
            
            var line = $"[{sec:0.00}s] {ownerName} 失去 {kindText} [{buffName}]，原因：{reasonText}";
            
            logs.Add(line);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);
            
            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Phase 9: 获取角色或敌人名称（用于 Buff 事件）
        /// Phase 9: Get character or enemy name (for buff events)
        /// </summary>
        private string GetCharacterOrEnemyName(string entityId)
        {
            // 如果是玩家角色
            if (SelectedCharacter != null && entityId == SelectedCharacter.Id)
            {
                return SelectedCharacter.Name;
            }
            
            // 如果是敌人
            return GetEnemyDisplayName(entityId);
        }

        /// <summary>
        /// Phase 9: Buff Tick 事件处理 - 记录 DoT/HoT 到日志
        /// Phase 9: Buff tick event handler - log DoT/HoT effects
        /// </summary>
        private void OnBuffTicked(BlazorIdle.Game.Buffs.BuffTickEvent ev)
        {
            var sec = ev.TimeMs / 1000.0;
            var ownerName = GetCharacterOrEnemyName(ev.OwnerId);
            var tickTypeText = ev.TickType == BlazorIdle.Game.Buffs.BuffTickType.DamageOverTime ? "DoT" : "HoT";
            
            string line;
            if (ev.TickType == BlazorIdle.Game.Buffs.BuffTickType.DamageOverTime)
            {
                line = $"[{sec:0.00}s] [{tickTypeText}] {ownerName} 的 [{ev.BuffId}] 造成 {ev.Amount} 伤害，HP: {ev.ResultingHp}";
            }
            else
            {
                line = $"[{sec:0.00}s] [{tickTypeText}] {ownerName} 的 [{ev.BuffId}] 恢复 {ev.Amount} 生命值，HP: {ev.ResultingHp}";
            }
            
            logs.Add(line);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);
            
            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Phase 9: 治疗事件处理 - 记录治疗效果到日志
        /// Phase 9: Heal event handler - log healing effects
        /// </summary>
        private void OnHealed(BlazorIdle.Game.Buffs.HealEvent ev)
        {
            var sec = ev.TimeMs / 1000.0;
            var ownerName = GetCharacterOrEnemyName(ev.OwnerId);
            
            var line = $"[{sec:0.00}s] [治疗] {ownerName} 恢复 {ev.Amount} 生命值 (来源: {ev.Source})，HP: {ev.ResultingHp}";
            
            logs.Add(line);
            if (logs.Count > MaxLogEntries) logs.RemoveRange(0, logs.Count - MaxLogEntries);
            
            _ = InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (battle is not null)
            {
                battle.CombatEventFired -= OnCombatEvent;
                battle.LootDropped -= OnLootDropped;
                battle.ExperienceGained -= OnExperienceGained;
                battle.BuffApplied -= OnBuffApplied;
                battle.BuffRemoved -= OnBuffRemoved;
                battle.BuffTicked -= OnBuffTicked;
                battle.Healed -= OnHealed;
            }

            if (dungeonManager is not null)
            {
                dungeonManager.CombatEventFired -= OnCombatEvent;
                dungeonManager.LootDropped -= OnLootDropped;
                dungeonManager.ExperienceGained -= OnExperienceGained;
                dungeonManager.WaveChanged -= OnDungeonWaveChanged;
                dungeonManager.DungeonCompleted -= OnDungeonCompleted;
                dungeonManager.BuffApplied -= OnBuffApplied;
                dungeonManager.BuffRemoved -= OnBuffRemoved;
                dungeonManager.BuffTicked -= OnBuffTicked;
                dungeonManager.Healed -= OnHealed;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }
    }
}