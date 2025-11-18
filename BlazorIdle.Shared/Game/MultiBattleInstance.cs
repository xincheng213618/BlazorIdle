using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Tracks;
using BlazorIdle.Game.Config;

namespace BlazorIdle.Game
{
    /// <summary>
    /// 多单位战斗实例 - 支持多角色对多怪物的战斗
    /// Multi-unit battle instance - supports multiple characters vs multiple monsters
    /// </summary>
    public sealed class MultiBattleInstance
    {
        private readonly IGameClock _clock;
        private readonly RngContext _rng;
        private readonly BattleTeam<Character> _playerTeam;
        private readonly BattleTeam<Enemy> _enemyTeam;

        // 每个角色的战斗轨道
        private readonly Dictionary<string, CharacterTracks> _characterTracks = new();
        // 每个怪物的攻击轨道
        private readonly Dictionary<string, EnemyTrack> _enemyTracks = new();
        
        // Phase 7: 新技能系统组件 / New skill system components
        private readonly ISkillResolver _skillResolver;
        private readonly CastingController _castingController;
        private readonly CombatConfig _combatConfig;
        private readonly SkillRepository _skillRepository;
        
        // Phase 2: 资源系统 / Resource system
        private readonly Dictionary<string, Resources.ResourceBucketCollection> _playerResources = new();
        private readonly Resources.ResourceConfig _resourceConfig = new();
        
        // Phase 2.7: 职业资源配置映射 / Profession resource configuration map
        private readonly Dictionary<string, Shared.Models.ProfessionResourceConfig>? _professionResourceConfigs;
        
        // Phase 4: Buff 系统 / Buff system
        private readonly Dictionary<string, Buffs.CharacterBuffOwner> _playerBuffOwners = new();
        private readonly Dictionary<string, Buffs.EnemyBuffOwner> _enemyBuffOwners = new();
        
        // Phase 4: 条件检查器（单例复用）/ Condition checker (singleton reuse)
        private readonly ConditionChecker _conditionChecker = new();
        
        // Phase 5: 冷却和资源管理器 / Cooldown and resource managers
        private readonly CooldownManager _cooldownManager = new();
        private readonly ResourceManager _resourceManager = new();
        
        // Note: Legacy Tracks are created but not actively used in the current simplified implementation.
        // They are preserved for potential future use or alternative implementation paths.
        // Current implementation directly uses TrackState + SkillResolver for better clarity.
        // If memory optimization is critical, these can be removed safely.

        /// <summary>
        /// 获取玩家队伍引用（只读）
        /// Get player team reference (read-only)
        /// </summary>
        public BattleTeam<Character> PlayerTeam => _playerTeam;

        /// <summary>
        /// 获取敌人队伍引用（只读）
        /// Get enemy team reference (read-only)
        /// </summary>
        public BattleTeam<Enemy> EnemyTeam => _enemyTeam;

        // 战斗配置
        private readonly MultiBattleConfig _config;

        // 战斗状态
        private bool _running;
        private MultiBattleState _state = MultiBattleState.NotStarted;
        private int _resumeAtMs = 0;
        private int _lastTickTime = 0;

        // 战斗统计
        private readonly Dictionary<string, int> _damageDealtByCharacter = new();
        private readonly Dictionary<string, int> _damageTakenByCharacter = new();
        private readonly Dictionary<string, int> _damageDealtByEnemy = new();
        private readonly Dictionary<string, int> _healingDone = new();
        private int _totalPlayerDamage = 0;
        private int _totalEnemyDamage = 0;

        // 战斗段落聚合器
        private readonly List<CombatSegment> _segments = new();
        private SegmentAggregator _aggregator;

        // RNG追踪
        private readonly int _rngSeed;
        private int _rngIndexStart;
        private int _rngIndexEnd;

        // 事件
        public event Action<MultiCombatEvent>? CombatEventFired;
        public event Action<LootDropEvent>? LootDropped;
        public event Action<ExperienceGainEvent>? ExperienceGained;
        public event Action<TeamStatusEvent>? TeamStatusChanged;
        // Phase 9: Buff 事件 / Phase 9: Buff events
        public event Action<Buffs.BuffApplyEvent>? BuffApplied;
        public event Action<Buffs.BuffRemoveEvent>? BuffRemoved;
        public event Action<Buffs.BuffTickEvent>? BuffTicked;
        public event Action<Buffs.HealEvent>? Healed;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public MultiBattleInstance(
            IGameClock clock,
            RngContext rng,
            BattleTeam<Character> playerTeam,
            BattleTeam<Enemy> enemyTeam,
            MultiBattleConfig? config = null,
            Dictionary<string, Resources.ResourceBucketCollection>? preservedResources = null,
            Dictionary<string, Shared.Models.ProfessionResourceConfig>? professionResourceConfigs = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _playerTeam = playerTeam ?? throw new ArgumentNullException(nameof(playerTeam));
            _enemyTeam = enemyTeam ?? throw new ArgumentNullException(nameof(enemyTeam));
            _config = config ?? new MultiBattleConfig();
            _professionResourceConfigs = professionResourceConfigs;

            _rngSeed = rng.Seed;
            _aggregator = new SegmentAggregator(new SegmentAggregatorOptions
            {
                MaxEvents = 20,
                MaxDurationMs = 3000
            });

            // Phase 7: 初始化新技能系统组件 / Initialize new skill system components
            _combatConfig = new CombatConfig();
            _skillRepository = new SkillRepository();
            _skillResolver = new SkillResolver(_combatConfig, _skillRepository);
            _castingController = new CastingController();

            InitializeTracks(preservedResources);
        }

        /// <summary>
        /// 初始化所有战斗轨道
        /// Initialize all battle tracks
        /// </summary>
        private void InitializeTracks(Dictionary<string, Resources.ResourceBucketCollection>? preservedResources = null)
        {
            // 为每个角色初始化战斗轨道
            foreach (var member in _playerTeam.Members)
            {
                var character = member.Entity;
                var tracks = new CharacterTracks(member.Id, character);
                _characterTracks[member.Id] = tracks;

                // Phase 2 & 2.6 & 2.7: 为每个玩家创建或恢复资源集合
                // Create or restore resource collection for each player
                if (preservedResources != null && preservedResources.TryGetValue(member.Id, out var existingResources))
                {
                    // 使用保留的资源集合（用于副本波次之间保持资源）
                    // Use preserved resource collection (for maintaining resources between dungeon waves)
                    _playerResources[member.Id] = existingResources;
                }
                else
                {
                    // Phase 2.7: 根据职业配置创建资源集合
                    // Phase 2.7: Create resource collection based on profession configuration
                    if (_professionResourceConfigs != null && 
                        _professionResourceConfigs.TryGetValue(character.ActiveCombatProfessionId, out var professionResourceConfig))
                    {
                        // 使用职业特定的资源配置
                        // Use profession-specific resource configuration
                        _playerResources[member.Id] = new Resources.ResourceBucketCollection(
                            professionResourceConfig.Id, 
                            professionResourceConfig.Max, 
                            professionResourceConfig.Initial);
                    }
                    else
                    {
                        // 后备：创建默认的资源集合（rage, max=10, initial=0）
                        // Fallback: Create default resource collection (rage, max=10, initial=0)
                        _playerResources[member.Id] = new Resources.ResourceBucketCollection();
                    }
                }

                // 初始化统计
                _damageDealtByCharacter[member.Id] = 0;
                _damageTakenByCharacter[member.Id] = 0;
                
                // Phase 4: 创建 Buff 所有者 / Create buff owner
                // Phase 9 fix: Add callbacks to sync HP changes from buffs to BattleMember
                _playerBuffOwners[member.Id] = new Buffs.CharacterBuffOwner(
                    character,
                    member.Id,
                    _playerResources[member.Id],
                    onDamageReceived: (amount, meta) =>
                    {
                        // Sync HP change to BattleMember's CurrentHp
                        var m = _playerTeam.GetMember(member.Id);
                        if (m != null)
                        {
                            m.SyncHpFromEntity();
                        }
                    },
                    onHealReceived: (amount, meta) =>
                    {
                        // Sync HP change to BattleMember's CurrentHp
                        var m = _playerTeam.GetMember(member.Id);
                        if (m != null)
                        {
                            m.SyncHpFromEntity();
                        }
                    });
            }

            // 为每个怪物初始化攻击轨道
            foreach (var member in _enemyTeam.Members)
            {
                var enemy = member.Entity;
                var track = new EnemyTrack(member.Id, enemy);
                _enemyTracks[member.Id] = track;

                // 初始化统计
                _damageDealtByEnemy[member.Id] = 0;
                
                // Phase 4: 创建 Buff 所有者 / Create buff owner
                // Phase 9 fix: Add callbacks to sync HP changes from buffs to BattleMember
                _enemyBuffOwners[member.Id] = new Buffs.EnemyBuffOwner(
                    enemy,
                    member.Id,
                    onDamageReceived: (amount, meta) =>
                    {
                        // Sync HP change to BattleMember's CurrentHp
                        var m = _enemyTeam.GetMember(member.Id);
                        if (m != null)
                        {
                            m.SyncHpFromEntity();
                        }
                    },
                    onHealReceived: (amount, meta) =>
                    {
                        // Sync HP change to BattleMember's CurrentHp
                        var m = _enemyTeam.GetMember(member.Id);
                        if (m != null)
                        {
                            m.SyncHpFromEntity();
                        }
                    });
            }
        }



        /// <summary>
        /// 开始战斗
        /// Start battle
        /// </summary>
        /// <param name="resetPlayerTeam">是否重置玩家队伍状态（默认true）/ Whether to reset player team state (default true)</param>
        public void Start(bool resetPlayerTeam = true)
        {
            if (_running) return;

            _running = true;
            _clock.Reset();
            _state = MultiBattleState.Fighting;
            _resumeAtMs = 0;

            // 重置队伍状态
            if (resetPlayerTeam)
            {
                _playerTeam.Reset();
            }
            _enemyTeam.Reset();

            // 重置所有轨道
            var now = _clock.NowMs;
            foreach (var tracks in _characterTracks.Values)
            {
                tracks.Reset(now);
            }
            foreach (var track in _enemyTracks.Values)
            {
                track.Reset(now);
            }

            // 重置统计
            foreach (var key in _damageDealtByCharacter.Keys.ToList())
            {
                _damageDealtByCharacter[key] = 0;
            }
            foreach (var key in _damageTakenByCharacter.Keys.ToList())
            {
                _damageTakenByCharacter[key] = 0;
            }
            foreach (var key in _damageDealtByEnemy.Keys.ToList())
            {
                _damageDealtByEnemy[key] = 0;
            }
            _healingDone.Clear();
            _totalPlayerDamage = 0;
            _totalEnemyDamage = 0;

            // 重置段落聚合器
            _segments.Clear();
            _aggregator.Reset();
            _rngIndexStart = _rng.Index;

            // 触发初始状态事件
            FireTeamStatusEvent();
        }

        /// <summary>
        /// 停止战斗
        /// Stop battle
        /// </summary>
        public void Stop()
        {
            if (!_running) return;

            _running = false;
            var seg = _aggregator.ForceFlush(_clock.NowMs, _rng.Index);
            if (seg != null) _segments.Add(seg);
            _rngIndexEnd = _rng.Index;
        }

        /// <summary>
        /// 战斗是否正在运行
        /// Whether battle is running
        /// </summary>
        public bool IsRunning => _running;

        /// <summary>
        /// 推进战斗时钟
        /// Advance battle tick
        /// </summary>
        public void AdvanceTick(int tickMs)
        {
            if (!_running) return;

            _clock.AdvanceBy(tickMs);
            int now = _clock.NowMs;

            // 处理冷却状态
            if (_state == MultiBattleState.PlayerTeamDeadCooldown ||
                _state == MultiBattleState.EnemyTeamDeadCooldown)
            {
                if (now >= _resumeAtMs)
                {
                    HandleCooldownEnd(now);
                }

                var segByTime = _aggregator.Tick(now, _rng.Index);
                if (segByTime != null) _segments.Add(segByTime);
                return;
            }

            // 检查战斗结束条件
            if (_state == MultiBattleState.Fighting)
            {
                // Phase 4: 处理 Buff tick（在行动处理之前）
                // Phase 4: Process buff ticks (before action processing)
                double deltaTimeSec = _lastTickTime > 0 ? (now - _lastTickTime) / 1000.0 : 0;
                if (deltaTimeSec > 0)
                {
                    ProcessBuffTicks(deltaTimeSec);
                    
                    // Phase 5: 更新技能冷却时间
                    // Phase 5: Update skill cooldowns
                    _cooldownManager.TickCooldowns(deltaTimeSec);
                }
                
                // 处理角色行动
                ProcessCharacterActions(now);

                // 处理怪物行动
                ProcessEnemyActions(now);

                // 检查战斗状态
                CheckBattleState(now);
            }

            // 处理段落聚合
            var seg = _aggregator.Tick(now, _rng.Index);
            if (seg != null) _segments.Add(seg);
        }

        /// <summary>
        /// 处理角色行动（Phase 7.2：使用 SkillResolver）
        /// Process character actions (Phase 7.2: Using SkillResolver)
        /// </summary>
        private void ProcessCharacterActions(int now)
        {
            // 调用 CastingController（占位实现，预留给后续施法系统）
            // Call CastingController (placeholder implementation, reserved for future casting system)
            double dt = _lastTickTime > 0 ? (now - _lastTickTime) / 1000.0 : 0;
            _castingController.Tick(dt);
            _lastTickTime = now;
            
            var aliveCharIds = _playerTeam.GetAliveMemberIds();

            foreach (var charId in aliveCharIds)
            {
                if (!_characterTracks.TryGetValue(charId, out var tracks))
                    continue;

                if (!tracks.IsEnabled)
                    continue;

                var character = tracks.Character;

                // Phase 9: 更新急速加成（基于当前 Buff）
                // Phase 9: Update haste bonus (based on current buffs)
                UpdateCharacterHaste(charId, character, tracks);

                // 处理普通攻击 - Phase 7.2: 使用 SkillResolver
                // Process normal attacks - Phase 7.2: Using SkillResolver
                var atkCount = tracks.AttackTrack.CollectTriggers(now);
                for (int i = 0; i < atkCount; i++)
                {
                    ProcessCharacterAttackViaSkillResolver(charId, character);
                }

                // 处理特殊技能 - Phase 7.2: 使用 SkillResolver
                // Process special skills - Phase 7.2: Using SkillResolver
                var spCount = tracks.SpecialTrack.CollectTriggers(now);
                for (int i = 0; i < spCount; i++)
                {
                    ProcessCharacterSpecialViaSkillResolver(charId, character);
                }
            }
        }

        /// <summary>
        /// Phase 9: 更新角色的急速加成（基于当前 Buff 效果）
        /// Phase 9: Update character's haste bonus (based on current buff effects)
        /// </summary>
        private void UpdateCharacterHaste(string charId, Character character, CharacterTracks tracks)
        {
            // 获取基础急速
            // Get base haste
            double baseHastePercent = character.HastePercent;
            
            // 应用 Buff 效果到急速
            // Apply buff effects to haste
            if (_playerBuffOwners.TryGetValue(charId, out var buffOwner))
            {
                double modifiedHaste = baseHastePercent;
                
                // 按应用时间排序 Buff（与 SkillResolver 一致）
                // Sort buffs by application time (consistent with SkillResolver)
                var sortedBuffs = buffOwner.Buffs.Values
                    .OrderBy(b => b.AppliedAtMs)
                    .ToList();
                
                foreach (var buff in sortedBuffs)
                {
                    foreach (var effect in buff.Effects)
                    {
                        // 只处理影响急速的效果
                        // Only process effects targeting haste
                        if (effect.Target != "HastePercent")
                            continue;
                        
                        switch (effect.Type)
                        {
                            case Buffs.BuffEffectType.StatMultiplier:
                                modifiedHaste *= (1.0 + effect.Value);
                                break;
                            case Buffs.BuffEffectType.StatAdditive:
                                modifiedHaste += effect.Value;
                                break;
                            case Buffs.BuffEffectType.StatReduction:
                                modifiedHaste *= (1.0 - effect.Value);
                                break;
                        }
                    }
                }
                
                // 更新攻击轨道的急速倍率
                // Update attack track haste multiplier
                tracks.AttackTrack.SetHaste(1.0 + modifiedHaste / 100.0);
            }
        }

        /// <summary>
        /// Phase 3+ / Monster Skill System: 通用技能执行函数 - 同时支持玩家和怪物
        /// Phase 3+ / Monster Skill System: Generic skill execution function - supports both players and monsters
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="skillId">要执行的技能ID / Skill ID to execute</param>
        /// <param name="sourceTrack">技能来源轨道（用于追踪）/ Source track for tracking</param>
        /// <param name="isCasterPlayer">施法者是否为玩家 / Is caster a player</param>
        /// <param name="eventSource">事件来源类型（仅玩家使用）/ Event source type (player only)</param>
        private void ExecuteSkill(
            string casterId, 
            string skillId, 
            string sourceTrack,
            bool isCasterPlayer,
            EventSource eventSource = EventSource.Attack)
        {
            if (isCasterPlayer)
            {
                // 玩家施法逻辑
                // Player casting logic
                var member = _playerTeam.GetMember(casterId);
                if (member == null) return;
                var character = member.Entity;

                // 选择一个默认目标用于上下文（用于CurrentTarget策略）
                // Select a default target for context (used for CurrentTarget policy)
                var defaultTargetId = SelectEnemyTarget(_config.PlayerTargetStrategy);
                var defaultTarget = defaultTargetId != null ? _enemyTeam.GetMember(defaultTargetId) : null;

                // 创建战斗上下文
                // Create battle context
                var ctx = new BattleContext
                {
                    Player = character,
                    Enemy = defaultTarget?.Entity,
                    PlayerTeam = _playerTeam,
                    EnemyTeam = _enemyTeam,
                    Rng = _rng,
                    Clock = _clock,
                    PlayerResources = _playerResources.GetValueOrDefault(casterId),
                    PlayerBuffOwner = _playerBuffOwners.GetValueOrDefault(casterId),
                    EnemyBuffOwners = _enemyBuffOwners,
                    CurrentTargetId = defaultTargetId
                };

                // Phase 4: 检查技能施放条件
                // Phase 4: Check skill casting conditions
                var skillDef = _skillRepository.GetSkillById(skillId);
                if (skillDef?.Conditions != null)
                {
                    if (!_conditionChecker.CheckConditions(skillDef, ctx, isCasterPlayer: true, casterId: casterId))
                    {
                        // 条件不满足，跳过技能施放
                        // Conditions not met, skip skill casting
                        return;
                    }
                }

                // Phase 5: 检查冷却时间
                // Phase 5: Check cooldown
                if (skillDef != null && !_cooldownManager.IsReady(skillId))
                {
                    // 技能还在冷却中，跳过施放
                    // Skill is still on cooldown, skip casting
                    return;
                }

                // Phase 5: 检查资源消耗
                // Phase 5: Check resource cost
                if (skillDef != null && !_resourceManager.CheckResourceCost(skillDef, ctx))
                {
                    // 资源不足，跳过施放
                    // Insufficient resources, skip casting
                    return;
                }

                // Phase 5: 消耗资源（瞬发技能在施放时消耗）
                // Phase 5: Consume resources (instant skills consume on cast)
                if (skillDef != null && skillDef.ReleaseType == "instant")
                {
                    _resourceManager.ConsumeResourceCost(skillDef, ctx);
                }

                // 使用 SkillResolver 执行技能
                // Execute skill using SkillResolver
                var opts = new SkillCastOptions 
                { 
                    SourceTrack = sourceTrack,
                    CasterId = casterId
                };
                var result = _skillResolver.Cast(skillId, ctx, opts);

                // 解析目标（来自技能的targetPolicy）
                // Resolve targets (from skill's targetPolicy)
                List<string> targetIds = result.TargetIds?.Count > 0 ? result.TargetIds : 
                    (defaultTargetId != null ? new List<string> { defaultTargetId } : new List<string>());

                // 应用效果到所有解析的目标
                // Apply effects to all resolved targets
                if (targetIds.Count > 0)
                {
                    bool isAoe = targetIds.Count > 1;
                    int damagePerTarget = isAoe ? (int)(result.DamageDealt * _config.AoeDamageMultiplier) : result.DamageDealt;

                    foreach (var targetId in targetIds)
                    {
                        var target = _enemyTeam.GetMember(targetId);
                        if (target == null) continue;

                        // 只有造成伤害时才记录伤害事件
                        // Only log damage event if damage is dealt
                        if (damagePerTarget > 0)
                        {
                            ApplyDamageToEnemy(casterId, member, targetId, target, damagePerTarget, eventSource, 
                                isAoe: isAoe, isCrit: result.IsCrit, skillId: skillId, bundleId: result.BundleId);
                        }

                        // 即时治疗每个目标
                        // Instant heal per target
                        ApplyInstantHeal(result, casterId, targetId, isCasterPlayer: true, skillId: skillId);
                    }
                    
                    // Buff操作和资源变化只应用一次（不是每个目标）
                    // Buff operations and resource changes apply once (not per target)
                    // 使用第一个目标ID作为上下文（buff系统会根据BuffTarget类型正确解析实际目标）
                    // Use first target ID as context (buff system will resolve actual targets based on BuffTarget type)
                    string? primaryTargetId = targetIds.Count > 0 ? targetIds[0] : null;
                    ProcessBuffOperations(result, casterId, primaryTargetId, isCasterPlayer: true);
                    ApplyResourceChanges(result, casterId, isCasterPlayer: true, skillId: skillId);
                }

                // 向后兼容 - 如果技能没有定义资源获得，使用职业配置作为回退（仅普通攻击）
                // Backward compatibility - if skill doesn't define resource gains, use profession config as fallback (normal attack only)
                if (sourceTrack == "attack" && 
                    (result.ResourceChanges == null || result.ResourceChanges.Count == 0) && 
                    _playerResources.TryGetValue(casterId, out var resources))
                {
                    // 获取职业资源配置
                    // Get profession resource config
                    string resourceId = "rage";
                    int gainPerAttack = _resourceConfig.GainPerAttack;
                    int gainPerCritExtra = _resourceConfig.GainPerCritExtra;
                    
                    if (_professionResourceConfigs != null &&
                        _professionResourceConfigs.TryGetValue(character.ActiveCombatProfessionId, out var profConfig))
                    {
                        resourceId = profConfig.Id;
                        gainPerAttack = profConfig.GainPerAttack;
                        gainPerCritExtra = profConfig.GainPerCritExtra;
                    }
                    
                    if (resources.HasBucket(resourceId))
                    {
                        var bucket = resources.GetBucket(resourceId);
                        
                        // 命中产生资源
                        int gained = bucket.Gain(gainPerAttack, "attack_hit");
                        if (gained > 0)
                        {
                            RecordResourceGain(casterId, resourceId, gained, bucket.Current, "attack_hit", 
                                skillId: skillId, bundleId: result.BundleId);
                        }
                        
                        // 暴击额外产生资源
                        if (result.IsCrit)
                        {
                            int critGain = bucket.Gain(gainPerCritExtra, "crit_bonus");
                            if (critGain > 0)
                            {
                                RecordResourceGain(casterId, resourceId, critGain, bucket.Current, "crit_bonus",
                                    skillId: skillId, bundleId: result.BundleId);
                            }
                        }
                    }
                }

                // Phase 5: 启动冷却
                // Phase 5: Start cooldown
                if (skillDef != null && skillDef.CooldownSec > 0)
                {
                    _cooldownManager.StartCooldown(skillId, skillDef.CooldownSec);
                }
            }
            else
            {
                // Monster Skill System: 怪物施法逻辑
                // Monster Skill System: Monster casting logic
                var member = _enemyTeam.GetMember(casterId);
                if (member == null) return;
                var enemy = member.Entity;

                // 选择一个默认目标用于上下文
                // Select a default target for context
                var defaultTargetId = SelectPlayerTarget(_config.EnemyTargetStrategy);
                var defaultTarget = defaultTargetId != null ? _playerTeam.GetMember(defaultTargetId) : null;

                // 创建战斗上下文（Enemy 作为施法者）
                // Create battle context (Enemy as caster)
                var ctx = new BattleContext
                {
                    Enemy = enemy,
                    Player = defaultTarget?.Entity,
                    PlayerTeam = _playerTeam,
                    EnemyTeam = _enemyTeam,
                    Rng = _rng,
                    Clock = _clock,
                    PlayerResources = defaultTargetId != null ? _playerResources.GetValueOrDefault(defaultTargetId) : null,
                    PlayerBuffOwner = defaultTargetId != null ? _playerBuffOwners.GetValueOrDefault(defaultTargetId) : null,
                    EnemyBuffOwners = _enemyBuffOwners,
                    CurrentTargetId = defaultTargetId
                };

                // Phase 4: 检查技能施放条件（怪物）
                // Phase 4: Check skill casting conditions (monster)
                var skillDef = _skillRepository.GetSkillById(skillId);
                if (skillDef?.Conditions != null)
                {
                    if (!_conditionChecker.CheckConditions(skillDef, ctx, isCasterPlayer: false, casterId: casterId))
                    {
                        // 条件不满足，跳过技能施放
                        // Conditions not met, skip skill casting
                        return;
                    }
                }

                // 使用 SkillResolver 执行技能
                // Execute skill using SkillResolver
                var opts = new SkillCastOptions 
                { 
                    SourceTrack = sourceTrack,
                    CasterId = casterId
                };
                var result = _skillResolver.Cast(skillId, ctx, opts);

                // 解析目标（来自技能的targetPolicy）
                // Resolve targets (from skill's targetPolicy)
                List<string> targetIds = result.TargetIds?.Count > 0 ? result.TargetIds : 
                    (defaultTargetId != null ? new List<string> { defaultTargetId } : new List<string>());

                // 应用效果到所有解析的目标
                // Apply effects to all resolved targets
                if (targetIds.Count > 0)
                {
                    bool isAoe = targetIds.Count > 1;
                    int damagePerTarget = isAoe ? (int)(result.DamageDealt * _config.AoeDamageMultiplier) : result.DamageDealt;

                    foreach (var targetId in targetIds)
                    {
                        var target = _playerTeam.GetMember(targetId);
                        if (target == null) continue;

                        // 应用伤害（传递技能ID和BundleID）
                        // Apply damage (pass skill ID and bundle ID)
                        if (damagePerTarget > 0)
                        {
                            ApplyDamageToPlayer(casterId, member, targetId, target, damagePerTarget,
                                skillId: skillId, bundleId: result.BundleId);
                        }

                        // 即时治疗每个目标
                        // Instant heal per target
                        ApplyInstantHeal(result, casterId, targetId, isCasterPlayer: false, skillId: skillId);
                    }
                    
                    // Buff操作和资源变化只应用一次（不是每个目标）
                    // Buff operations and resource changes apply once (not per target)
                    string? primaryTargetId = targetIds.Count > 0 ? targetIds[0] : null;
                    ProcessBuffOperations(result, casterId, primaryTargetId, isCasterPlayer: false);
                    ApplyResourceChanges(result, casterId, isCasterPlayer: false, skillId: skillId);
                }
            }
        }

        /// <summary>
        /// 通过 SkillResolver 处理角色普通攻击（Phase 7.2 + Phase 3+ Target Selection Integration）
        /// Process character normal attack via SkillResolver (Phase 7.2 + Phase 3+ Target Selection Integration)
        /// </summary>
        private void ProcessCharacterAttackViaSkillResolver(string charId, Character character)
        {
            // Phase 3+: 从角色实体获取普通攻击技能ID
            // Phase 3+: Get normal attack skill ID from character entity
            string skillId = character.GetNormalAttackSkillId();
            
            // Monster Skill System: 调用统一的通用技能执行函数
            // Monster Skill System: Call unified generic skill execution function
            ExecuteSkill(charId, skillId, "attack", isCasterPlayer: true, EventSource.Attack);
        }

        /// <summary>
        /// 通过 SkillResolver 处理角色特殊技能（Phase 7.2 + Phase 3+ Target Selection Integration）
        /// Process character special skill via SkillResolver (Phase 7.2 + Phase 3+ Target Selection Integration)
        /// </summary>
        private void ProcessCharacterSpecialViaSkillResolver(string charId, Character character)
        {
            // Phase 3+: 从角色实体获取特殊攻击技能ID
            // Phase 3+: Get special attack skill ID from character entity
            string skillId = character.GetSpecialAttackSkillId();
            
            // Monster Skill System: 调用统一的通用技能执行函数
            // Monster Skill System: Call unified generic skill execution function
            ExecuteSkill(charId, skillId, "special", isCasterPlayer: true, EventSource.Special);
        }

        /// <summary>
        /// 处理角色普通攻击
        /// Process character normal attack
        /// </summary>
        private void ProcessCharacterAttack(string charId, Character character)
        {
            var targetId = SelectEnemyTarget(_config.PlayerTargetStrategy);
            if (targetId == null) return;

            var member = _playerTeam.GetMember(charId);
            var target = _enemyTeam.GetMember(targetId);
            if (member == null || target == null) return;

            // 计算伤害
            int damage = PlayerRollDamage(character.DamagePerAttack, character, true);

            // 应用伤害
            ApplyDamageToEnemy(charId, member, targetId, target, damage, EventSource.Attack);
        }

        /// <summary>
        /// 处理角色特殊技能
        /// Process character special skill
        /// </summary>
        private void ProcessCharacterSpecial(string charId, Character character)
        {
            var member = _playerTeam.GetMember(charId);
            if (member == null) return;

            if (_config.SpecialIsAoe)
            {
                // AOE技能 - 打击所有存活敌人
                var aliveEnemies = _enemyTeam.GetAliveMemberIds();
                int baseDamage = character.SpecialDamage;

                foreach (var enemyId in aliveEnemies)
                {
                    var target = _enemyTeam.GetMember(enemyId);
                    if (target == null) continue;

                    // AOE伤害可以有衰减
                    int damage = PlayerRollDamage(
                        (int)(baseDamage * _config.AoeDamageMultiplier),
                        character,
                        true
                    );

                    ApplyDamageToEnemy(charId, member, enemyId, target, damage, EventSource.Special, true);
                }
            }
            else
            {
                // 单体技能
                var targetId = SelectEnemyTarget(_config.PlayerTargetStrategy);
                if (targetId == null) return;

                var target = _enemyTeam.GetMember(targetId);
                if (target == null) return;

                int damage = PlayerRollDamage(character.SpecialDamage, character, true);
                ApplyDamageToEnemy(charId, member, targetId, target, damage, EventSource.Special);
            }
        }

        /// <summary>
        /// 处理怪物行动（Phase 7.3：使用 SkillResolver）
        /// Process enemy actions (Phase 7.3: Using SkillResolver)
        /// </summary>
        private void ProcessEnemyActions(int now)
        {
            var aliveEnemyIds = _enemyTeam.GetAliveMemberIds();

            foreach (var enemyId in aliveEnemyIds)
            {
                if (!_enemyTracks.TryGetValue(enemyId, out var track))
                    continue;

                if (!track.IsEnabled)
                    continue;

                var enemy = track.Enemy;

                var count = track.AttackTrack.CollectTriggers(now);
                for (int i = 0; i < count; i++)
                {
                    ProcessEnemyAttackViaSkillResolver(enemyId, enemy);
                }
            }
        }

        /// <summary>
        /// 通过 SkillResolver 处理怪物攻击（Phase 7.3 + Monster Skill System）
        /// Process enemy attack via SkillResolver (Phase 7.3 + Monster Skill System)
        /// </summary>
        private void ProcessEnemyAttackViaSkillResolver(string enemyId, Enemy enemy)
        {
            // Monster Skill System: 从怪物实体获取普通攻击技能ID
            // Monster Skill System: Get normal attack skill ID from enemy entity
            string skillId = enemy.GetNormalAttackSkillId();
            
            // Monster Skill System: 调用统一的通用技能执行函数
            // Monster Skill System: Call unified generic skill execution function
            ExecuteSkill(enemyId, skillId, "enemy_attack", isCasterPlayer: false);
        }

        /// <summary>
        /// 应用伤害到敌人
        /// Apply damage to enemy
        /// </summary>
        private void ApplyDamageToEnemy(
            string attackerId,
            BattleMember<Character> attacker,
            string defenderId,
            BattleMember<Enemy> defender,
            int damage,
            EventSource source,
            bool isAoe = false,
            bool isCrit = false,
            string? skillId = null,
            string? bundleId = null)
        {
            // 应用伤害
            int actualDamage = defender.TakeDamage(damage);
            bool isKill = defender.IsDead;
            
            // Phase 9.11: 同步HP变化到Entity（供Buff系统使用）
            // Phase 9.11: Sync HP change to Entity (for buff system use)
            defender.SyncHpToEntity();

            // 更新统计
            attacker.RecordDamageDealt(actualDamage, isKill);
            _damageDealtByCharacter[attackerId] += actualDamage;
            _totalPlayerDamage += actualDamage;

            // 创建事件
            var ev = new MultiCombatEvent
            {
                Attacker = ActorType.Player,
                Defender = ActorType.Enemy,
                AttackerId = attackerId,
                AttackerName = GetCharacterName(attackerId),
                DefenderId = defenderId,
                DefenderName = GetEnemyName(defenderId),
                Source = source,
                TimeMs = _clock.NowMs,
                Damage = actualDamage,
                Crit = isCrit,
                IsAoe = isAoe,
                IsKill = isKill,
                RngIndexAfter = _rng.Index,
                DefenderHpAfter = defender.CurrentHp,
                SkillId = skillId,
                BundleId = bundleId
            };

            // 聚合事件
            var flushed = _aggregator.AddEvent(ev);
            if (flushed != null) _segments.Add(flushed);

            // 触发事件
            CombatEventFired?.Invoke(ev);

            // 如果击杀，处理掉落物和经验
            if (isKill)
            {
                ProcessLootDrops(defenderId, defender.Entity);
                ProcessExperienceGain(defenderId, defender.Entity, attackerId);
            }
        }

        /// <summary>
        /// 应用伤害到玩家
        /// Apply damage to player
        /// </summary>
        private void ApplyDamageToPlayer(
            string attackerId,
            BattleMember<Enemy> attacker,
            string defenderId,
            BattleMember<Character> defender,
            int damage,
            string? skillId = null,
            string? bundleId = null)
        {
            // 应用伤害
            int actualDamage = defender.TakeDamage(damage);
            bool isKill = defender.IsDead;
            
            // Phase 9.11: 同步HP变化到Entity（供Buff系统使用）
            // Phase 9.11: Sync HP change to Entity (for buff system use)
            defender.SyncHpToEntity();

            // 更新统计
            attacker.RecordDamageDealt(actualDamage, isKill);
            _damageDealtByEnemy[attackerId] += actualDamage;
            _damageTakenByCharacter[defenderId] += actualDamage;
            _totalEnemyDamage += actualDamage;

            // 创建事件
            var ev = new MultiCombatEvent
            {
                Attacker = ActorType.Enemy,
                Defender = ActorType.Player,
                AttackerId = attackerId,
                AttackerName = GetEnemyName(attackerId),
                DefenderId = defenderId,
                DefenderName = GetCharacterName(defenderId),
                Source = EventSource.EnemyAttack,
                TimeMs = _clock.NowMs,
                Damage = actualDamage,
                Crit = false,
                IsAoe = false,
                IsKill = isKill,
                RngIndexAfter = _rng.Index,
                DefenderHpAfter = defender.CurrentHp,
                SkillId = skillId,
                BundleId = bundleId
            };

            // 聚合事件
            var flushed = _aggregator.AddEvent(ev);
            if (flushed != null) _segments.Add(flushed);

            // 触发事件
            CombatEventFired?.Invoke(ev);
        }

        /// <summary>
        /// Phase 2: 记录资源获得事件
        /// Phase 2: Record resource gain event
        /// </summary>
        private void RecordResourceGain(
            string actorId, 
            string bucketId, 
            int delta, 
            int newValue, 
            string reason,
            string? skillId = null,
            string? bundleId = null)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new Resources.ResourceGainEvent
                {
                    TimeMs = _clock.NowMs,
                    ActorId = actorId,
                    BucketId = bucketId,
                    Delta = delta,
                    NewValue = newValue,
                    Reason = reason,
                    SkillId = skillId,
                    BundleId = bundleId,
                    Attacker = ActorType.Player,
                    Defender = ActorType.Player,
                    Source = EventSource.Attack,
                    Damage = 0,
                    Crit = false,
                    RngIndexAfter = _rng.Index,
                    DefenderHpAfter = 0
                };
                
                var flushed = _aggregator.AddEvent(evt);
                if (flushed != null) _segments.Add(flushed);
            }
        }

        /// <summary>
        /// Phase 7: 记录 Buff 应用事件
        /// Phase 7: Record buff apply event
        /// </summary>
        private void RecordBuffApply(
            string ownerId,
            string buffId,
            Buffs.BuffKind kind,
            double? durationSec,
            int stacks,
            string effectsSummary,
            string? sourceSkillId = null,
            string? bundleId = null)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new Buffs.BuffApplyEvent
                {
                    TimeMs = _clock.NowMs,
                    OwnerId = ownerId,
                    BuffId = buffId,
                    Kind = kind,
                    DurationSec = durationSec,
                    Stacks = stacks,
                    EffectsSummary = effectsSummary,
                    SourceSkillId = sourceSkillId,
                    BundleId = bundleId,
                    Attacker = ActorType.Player,
                    Defender = ActorType.Player,
                    Source = EventSource.Attack,
                    Damage = 0,
                    Crit = false,
                    RngIndexAfter = _rng.Index,
                    DefenderHpAfter = 0
                };

                var flushed = _aggregator.AddEvent(evt);
                if (flushed != null) _segments.Add(flushed);
                
                // Phase 9: 触发 Buff 应用事件
                // Phase 9: Fire buff applied event
                BuffApplied?.Invoke(evt);
            }
        }

        /// <summary>
        /// Phase 7: 记录 Buff 移除事件
        /// Phase 7: Record buff remove event
        /// </summary>
        private void RecordBuffRemove(
            string ownerId,
            string buffId,
            string reason,
            string? bundleId = null)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new Buffs.BuffRemoveEvent
                {
                    TimeMs = _clock.NowMs,
                    OwnerId = ownerId,
                    BuffId = buffId,
                    Reason = reason,
                    BundleId = bundleId,
                    Attacker = ActorType.Player,
                    Defender = ActorType.Player,
                    Source = EventSource.Attack,
                    Damage = 0,
                    Crit = false,
                    RngIndexAfter = _rng.Index,
                    DefenderHpAfter = 0
                };

                var flushed = _aggregator.AddEvent(evt);
                if (flushed != null) _segments.Add(flushed);
                
                // Phase 9: 触发 Buff 移除事件
                // Phase 9: Fire buff removed event
                BuffRemoved?.Invoke(evt);
            }
        }

        /// <summary>
        /// Phase 7: 记录 Buff Tick 事件
        /// Phase 7: Record buff tick event
        /// </summary>
        private void RecordBuffTick(
            string ownerId,
            string buffId,
            Buffs.BuffTickType tickType,
            int amount,
            int resultingHp,
            string? bundleId = null)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new Buffs.BuffTickEvent
                {
                    TimeMs = _clock.NowMs,
                    OwnerId = ownerId,
                    BuffId = buffId,
                    TickType = tickType,
                    Amount = amount,
                    ResultingHp = resultingHp,
                    BundleId = bundleId,
                    Attacker = ActorType.Player,
                    Defender = ActorType.Player,
                    Source = EventSource.Attack,
                    Damage = 0,
                    Crit = false,
                    RngIndexAfter = _rng.Index,
                    DefenderHpAfter = 0
                };

                var flushed = _aggregator.AddEvent(evt);
                if (flushed != null) _segments.Add(flushed);
                
                // Phase 9: 触发 Buff Tick 事件
                // Phase 9: Fire buff tick event
                BuffTicked?.Invoke(evt);
            }
        }

        /// <summary>
        /// Phase 7: 记录治疗事件
        /// Phase 7: Record heal event
        /// </summary>
        private void RecordHeal(
            string ownerId,
            int amount,
            int resultingHp,
            string healSource,
            string? bundleId = null)
        {
            if (_combatConfig.EmitCastEvents)
            {
                var evt = new Buffs.HealEvent
                {
                    TimeMs = _clock.NowMs,
                    OwnerId = ownerId,
                    Amount = amount,
                    ResultingHp = resultingHp,
                    BundleId = bundleId,
                    Source = healSource,  // HealEvent's own Source property
                    Attacker = ActorType.Player,
                    Defender = ActorType.Player,
                    Damage = 0,
                    Crit = false,
                    RngIndexAfter = _rng.Index,
                    DefenderHpAfter = 0
                };

                var flushed = _aggregator.AddEvent(evt);
                if (flushed != null) _segments.Add(flushed);
                
                // Phase 9: 触发治疗事件
                // Phase 9: Fire heal event
                Healed?.Invoke(evt);
            }
        }

        /// <summary>
        /// 选择敌人目标
        /// Select enemy target
        /// </summary>
        private string? SelectEnemyTarget(TargetStrategy strategy)
        {
            switch (strategy)
            {
                case TargetStrategy.Random:
                    return _enemyTeam.GetRandomAliveMemberId(_rng);

                case TargetStrategy.LowestHp:
                    return _enemyTeam.GetLowestHpMemberId();

                case TargetStrategy.LowestHpPercent:
                    return _enemyTeam.GetLowestHpPercentMemberId();

                case TargetStrategy.HighestHp:
                    var aliveEnemies = _enemyTeam.GetAliveMembers();
                    var highest = aliveEnemies.OrderByDescending(m => m.CurrentHp).FirstOrDefault();
                    return highest?.Id;

                default:
                    return _enemyTeam.GetRandomAliveMemberId(_rng);
            }
        }

        /// <summary>
        /// 选择玩家目标
        /// Select player target
        /// </summary>
        private string? SelectPlayerTarget(TargetStrategy strategy)
        {
            switch (strategy)
            {
                case TargetStrategy.Random:
                    return _playerTeam.GetRandomAliveMemberId(_rng);

                case TargetStrategy.LowestHp:
                    return _playerTeam.GetLowestHpMemberId();

                case TargetStrategy.LowestHpPercent:
                    return _playerTeam.GetLowestHpPercentMemberId();

                case TargetStrategy.HighestHp:
                    var alivePlayers = _playerTeam.GetAliveMembers();
                    var highest = alivePlayers.OrderByDescending(m => m.CurrentHp).FirstOrDefault();
                    return highest?.Id;

                default:
                    return _playerTeam.GetRandomAliveMemberId(_rng);
            }
        }

        /// <summary>
        /// 处理 Buff tick（Phase 4）
        /// Process buff ticks (Phase 4)
        /// </summary>
        private void ProcessBuffTicks(double deltaTimeSec)
        {
            // 处理玩家 Buff
            // Process player buffs
            foreach (var kvp in _playerBuffOwners)
            {
                var charId = kvp.Key;
                var buffOwner = kvp.Value;
                
                // 获取角色成员
                var member = _playerTeam.GetMember(charId);
                if (member == null || member.IsDead) continue;
                
                ProcessEntityBuffs(buffOwner, deltaTimeSec);
            }
            
            // 处理敌人 Buff
            // Process enemy buffs
            foreach (var kvp in _enemyBuffOwners)
            {
                var enemyId = kvp.Key;
                var buffOwner = kvp.Value;
                
                // 获取敌人成员
                var member = _enemyTeam.GetMember(enemyId);
                if (member == null || member.IsDead) continue;
                
                ProcessEntityBuffs(buffOwner, deltaTimeSec);
            }
        }

        /// <summary>
        /// 处理单个实体的 Buff（Phase 4）
        /// Process buffs for a single entity (Phase 4)
        /// </summary>
        private void ProcessEntityBuffs(Buffs.IBuffOwner buffOwner, double deltaTimeSec)
        {
            // 收集过期的 Buff
            // Collect expired buffs
            var expiredBuffs = new List<string>();
            
            foreach (var kvp in buffOwner.Buffs)
            {
                var buff = kvp.Value;
                
                // Tick buff（返回触发的 tick 次数）
                // Tick buff (returns number of ticks triggered)
                int tickCount = buff.Tick(deltaTimeSec);
                
                // 处理 DoT/HoT
                // Process DoT/HoT
                if (tickCount > 0)
                {
                    if (buff.HasDamageOverTime())
                    {
                        int damagePerTick = buff.GetDamagePerTick();
                        int totalDamage = damagePerTick * tickCount;
                        
                        var damageMeta = new Buffs.DamageMeta("dot_tick", buff.Id);
                        buffOwner.ReceiveDamage(totalDamage, damageMeta);
                        
                        // Phase 7: 记录 BuffTickEvent (DoT)
                        // Phase 7: Record BuffTickEvent (DoT)
                        RecordBuffTick(
                            ownerId: buffOwner.Id,
                            buffId: buff.Id,
                            tickType: Buffs.BuffTickType.DamageOverTime,
                            amount: totalDamage,
                            resultingHp: buffOwner.CurrentHp,
                            bundleId: null
                        );
                    }
                    
                    if (buff.HasHealOverTime())
                    {
                        int healPerTick = buff.GetHealPerTick();
                        int totalHeal = healPerTick * tickCount;
                        
                        var healMeta = new Buffs.HealMeta("hot_tick", buff.Id);
                        buffOwner.ReceiveHeal(totalHeal, healMeta);
                        
                        // Phase 7: 记录 BuffTickEvent (HoT)
                        // Phase 7: Record BuffTickEvent (HoT)
                        RecordBuffTick(
                            ownerId: buffOwner.Id,
                            buffId: buff.Id,
                            tickType: Buffs.BuffTickType.HealOverTime,
                            amount: totalHeal,
                            resultingHp: buffOwner.CurrentHp,
                            bundleId: null
                        );
                    }
                }
                
                // 检查是否过期
                // Check if expired
                if (buff.IsExpired())
                {
                    expiredBuffs.Add(buff.Id);
                }
            }
            
            // 移除过期的 Buff
            // Remove expired buffs
            foreach (var buffId in expiredBuffs)
            {
                buffOwner.RemoveBuff(buffId, "expired");
                
                // Phase 7: 记录 BuffRemoveEvent (过期)
                // Phase 7: Record BuffRemoveEvent (expired)
                RecordBuffRemove(
                    ownerId: buffOwner.Id,
                    buffId: buffId,
                    reason: "expired",
                    bundleId: null
                );
            }
        }

        /// <summary>
        /// Phase 6: 处理技能释放结果中的 Buff 操作
        /// Phase 6: Process buff operations from skill cast result
        /// </summary>
        private void ProcessBuffOperations(
            SkillCastResult result,
            string casterId,
            string? targetId,
            bool isCasterPlayer)
        {
            if (result.BuffOperations == null || result.BuffOperations.Count == 0)
                return;

            // Pass bundleId from result to buff operations
            string? bundleId = result.BundleId;

            foreach (var operation in result.BuffOperations)
            {
                if (operation.Type == Skills.BuffOperationType.Apply)
                {
                    ApplyBuffOperation(operation, casterId, targetId, isCasterPlayer, bundleId);
                }
                else if (operation.Type == Skills.BuffOperationType.Remove)
                {
                    RemoveBuffOperation(operation, casterId, targetId, isCasterPlayer, bundleId);
                }
            }
        }

        /// <summary>
        /// Phase 6: 应用 Buff 操作
        /// Phase 6: Apply buff operation
        /// </summary>
        private void ApplyBuffOperation(
            Skills.BuffOperation operation,
            string casterId,
            string? targetId,
            bool isCasterPlayer,
            string? bundleId = null)
        {
            // Phase 2: 支持通过 BuffConfigId 引用配置化的 buff
            // Phase 2: Support referencing configured buffs via BuffConfigId
            Buffs.BuffInstance? templateToUse = null;
            Skills.BuffTarget targetToUse = operation.Target;

            if (!string.IsNullOrEmpty(operation.BuffConfigId))
            {
                // 从 BuffRepository 查找配置
                // Look up configuration from BuffRepository
                var buffConfig = Buffs.BuffRepository.Instance.GetBuffById(operation.BuffConfigId);
                if (buffConfig == null)
                {
                    // P1 Fix: Improved error logging for production visibility
                    var errorMsg = $"[ApplyBuffOperation] ERROR: BuffConfig '{operation.BuffConfigId}' not found in repository. " +
                                   $"Caster: {casterId}, Target: {targetId}";
                    Console.WriteLine(errorMsg);
                    System.Diagnostics.Debug.WriteLine(errorMsg);
                    return;
                }

                // 将 BuffConfig 转换为 BuffInstance 模板
                // Convert BuffConfig to BuffInstance template
                // Note: OwnerId will be set later for each target
                templateToUse = buffConfig.ToBuffInstance("", operation.BuffTemplate?.SourceSkillId);
                
                // 使用 TargetOverride 或 BuffConfig 的 DefaultTarget
                // Use TargetOverride or BuffConfig's DefaultTarget
                targetToUse = operation.TargetOverride ?? buffConfig.DefaultTarget;
            }
            else if (operation.BuffTemplate != null)
            {
                // 向后兼容：使用 inline BuffTemplate
                // Backward compatibility: use inline BuffTemplate
                templateToUse = operation.BuffTemplate;
            }
            else
            {
                // 既没有 BuffConfigId 也没有 BuffTemplate，无法应用
                // Neither BuffConfigId nor BuffTemplate provided, cannot apply
                return;
            }

            // 解析目标列表
            // Resolve target list
            var targets = ResolveBuffTargets(targetToUse, casterId, targetId, isCasterPlayer);

            foreach (var target in targets)
            {
                // 克隆 buff 模板并设置 OwnerId
                // Clone buff template and set OwnerId
                // Phase 6 Fix: Store original duration separately for proper cloning
                // BuffTemplate should have full duration, not remaining
                // Critical fix: Deep copy Effects list to avoid shared references
                // Method B: Set AppliedAtMs for time-ordered stacking
                var buffToApply = new Buffs.BuffInstance(
                    id: templateToUse.Id,
                    ownerId: target.Id, // Phase 6: 设置正确的 OwnerId
                    kind: templateToUse.Kind,
                    effects: new List<Buffs.BuffEffect>(templateToUse.Effects), // Deep copy
                    stackingPolicy: templateToUse.StackingPolicy,
                    durationSec: templateToUse.RemainingDurationSec, // Use template's duration
                    tickIntervalSec: templateToUse.TickIntervalSec,
                    maxStacks: templateToUse.MaxStacks,
                    appliedAtMs: _clock.NowMs // Set application timestamp for time-ordered stacking
                );

                // 应用 buff
                // Apply buff
                target.ApplyBuff(buffToApply);

                // Phase 7: 记录 BuffApplyEvent
                // Phase 7: Record BuffApplyEvent
                string effectsSummary = string.Join(", ", buffToApply.Effects.Select(e => 
                    $"{e.Type}={e.Value}"));
                RecordBuffApply(
                    ownerId: target.Id,
                    buffId: buffToApply.Id,
                    kind: buffToApply.Kind,
                    durationSec: buffToApply.RemainingDurationSec,
                    stacks: buffToApply.Stacks,
                    effectsSummary: effectsSummary,
                    sourceSkillId: templateToUse.SourceSkillId,
                    bundleId: bundleId // Fix: Pass bundleId from skill cast context
                );
            }
        }

        /// <summary>
        /// Phase 6: 移除 Buff 操作
        /// Phase 6: Remove buff operation
        /// </summary>
        private void RemoveBuffOperation(
            Skills.BuffOperation operation,
            string casterId,
            string? targetId,
            bool isCasterPlayer,
            string? bundleId = null)
        {
            if (string.IsNullOrEmpty(operation.BuffIdToRemove))
                return;

            // 解析目标列表
            // Resolve target list
            var targets = ResolveBuffTargets(operation.Target, casterId, targetId, isCasterPlayer);

            foreach (var target in targets)
            {
                string reason = operation.Reason ?? "skill_effect";
                target.RemoveBuff(operation.BuffIdToRemove, reason);

                // Phase 7: 记录 BuffRemoveEvent
                // Phase 7: Record BuffRemoveEvent
                RecordBuffRemove(
                    ownerId: target.Id,
                    buffId: operation.BuffIdToRemove,
                    reason: reason,
                    bundleId: bundleId // Fix: Pass bundleId from skill cast context
                );
            }
        }

        /// <summary>
        /// Phase 6: 解析 Buff 目标
        /// Phase 6: Resolve buff targets
        /// Phase 7.9: Added diagnostic logging for target resolution failures
        /// </summary>
        private List<Buffs.IBuffOwner> ResolveBuffTargets(
            Skills.BuffTarget targetType,
            string casterId,
            string? primaryTargetId,
            bool isCasterPlayer)
        {
            var targets = new List<Buffs.IBuffOwner>();

            switch (targetType)
            {
                case Skills.BuffTarget.Self:
                    // 施法者自己
                    // Caster itself
                    if (isCasterPlayer)
                    {
                        if (_playerBuffOwners.TryGetValue(casterId, out var playerOwner))
                            targets.Add(playerOwner);
                        else
                            LogTargetResolutionFailure(targetType, casterId, isCasterPlayer, "Player buff owner not found");
                    }
                    else
                    {
                        if (_enemyBuffOwners.TryGetValue(casterId, out var enemyOwner))
                            targets.Add(enemyOwner);
                        else
                            LogTargetResolutionFailure(targetType, casterId, isCasterPlayer, "Enemy buff owner not found");
                    }
                    break;

                case Skills.BuffTarget.Target:
                    // 主要目标
                    // Primary target
                    if (!string.IsNullOrEmpty(primaryTargetId))
                    {
                        if (isCasterPlayer)
                        {
                            // 玩家施法，目标是敌人
                            // Player casts, target is enemy
                            if (_enemyBuffOwners.TryGetValue(primaryTargetId, out var enemyOwner))
                                targets.Add(enemyOwner);
                            else
                                LogTargetResolutionFailure(targetType, primaryTargetId, isCasterPlayer, "Enemy target not found");
                        }
                        else
                        {
                            // 敌人施法，目标是玩家
                            // Enemy casts, target is player
                            if (_playerBuffOwners.TryGetValue(primaryTargetId, out var playerOwner))
                                targets.Add(playerOwner);
                            else
                                LogTargetResolutionFailure(targetType, primaryTargetId, isCasterPlayer, "Player target not found");
                        }
                    }
                    else
                    {
                        LogTargetResolutionFailure(targetType, casterId, isCasterPlayer, "Primary target ID is null or empty");
                    }
                    break;

                case Skills.BuffTarget.AllEnemies:
                    // 所有敌人
                    // All enemies
                    if (isCasterPlayer)
                    {
                        // 玩家施法，目标是所有存活的敌人
                        // Player casts, targets are all alive enemies
                        foreach (var enemyId in _enemyTeam.GetAliveMemberIds())
                        {
                            if (_enemyBuffOwners.TryGetValue(enemyId, out var enemyOwner))
                                targets.Add(enemyOwner);
                        }
                    }
                    else
                    {
                        // 敌人施法，目标是所有存活的玩家
                        // Enemy casts, targets are all alive players
                        foreach (var playerId in _playerTeam.GetAliveMemberIds())
                        {
                            if (_playerBuffOwners.TryGetValue(playerId, out var playerOwner))
                                targets.Add(playerOwner);
                        }
                    }
                    break;

                case Skills.BuffTarget.AllAllies:
                    // 所有队友（包括自己）
                    // All allies (including self)
                    if (isCasterPlayer)
                    {
                        // 玩家施法，目标是所有存活的玩家
                        // Player casts, targets are all alive players
                        foreach (var playerId in _playerTeam.GetAliveMemberIds())
                        {
                            if (_playerBuffOwners.TryGetValue(playerId, out var playerOwner))
                                targets.Add(playerOwner);
                        }
                    }
                    else
                    {
                        // 敌人施法，目标是所有存活的敌人
                        // Enemy casts, targets are all alive enemies
                        foreach (var enemyId in _enemyTeam.GetAliveMemberIds())
                        {
                            if (_enemyBuffOwners.TryGetValue(enemyId, out var enemyOwner))
                                targets.Add(enemyOwner);
                        }
                    }
                    break;

                case Skills.BuffTarget.RandomEnemy:
                    // 随机敌人
                    // Random enemy
                    if (isCasterPlayer)
                    {
                        var aliveEnemies = _enemyTeam.GetAliveMemberIds().ToList();
                        if (aliveEnemies.Count > 0)
                        {
                            var randomIndex = _rng.NextRange(0, aliveEnemies.Count - 1);
                            var randomId = aliveEnemies[randomIndex];
                            if (_enemyBuffOwners.TryGetValue(randomId, out var enemyOwner))
                                targets.Add(enemyOwner);
                        }
                    }
                    else
                    {
                        var alivePlayers = _playerTeam.GetAliveMemberIds().ToList();
                        if (alivePlayers.Count > 0)
                        {
                            var randomIndex = _rng.NextRange(0, alivePlayers.Count - 1);
                            var randomId = alivePlayers[randomIndex];
                            if (_playerBuffOwners.TryGetValue(randomId, out var playerOwner))
                                targets.Add(playerOwner);
                        }
                    }
                    break;

                case Skills.BuffTarget.LowestHpAlly:
                    // 血量最低的队友（按百分比）
                    // Lowest HP ally (by percentage)
                    // Phase 6 Fix: Compare HP percentage, not absolute HP
                    if (isCasterPlayer)
                    {
                        Buffs.IBuffOwner? lowestHpOwner = null;
                        double lowestHpPercentage = double.MaxValue;

                        foreach (var playerId in _playerTeam.GetAliveMemberIds())
                        {
                            if (_playerBuffOwners.TryGetValue(playerId, out var playerOwner))
                            {
                                double hpPercentage = playerOwner.MaxHp > 0 
                                    ? (double)playerOwner.CurrentHp / playerOwner.MaxHp 
                                    : 1.0;
                                
                                if (hpPercentage < lowestHpPercentage)
                                {
                                    lowestHpPercentage = hpPercentage;
                                    lowestHpOwner = playerOwner;
                                }
                            }
                        }

                        if (lowestHpOwner != null)
                            targets.Add(lowestHpOwner);
                    }
                    else
                    {
                        Buffs.IBuffOwner? lowestHpOwner = null;
                        double lowestHpPercentage = double.MaxValue;

                        foreach (var enemyId in _enemyTeam.GetAliveMemberIds())
                        {
                            if (_enemyBuffOwners.TryGetValue(enemyId, out var enemyOwner))
                            {
                                double hpPercentage = enemyOwner.MaxHp > 0 
                                    ? (double)enemyOwner.CurrentHp / enemyOwner.MaxHp 
                                    : 1.0;
                                
                                if (hpPercentage < lowestHpPercentage)
                                {
                                    lowestHpPercentage = hpPercentage;
                                    lowestHpOwner = enemyOwner;
                                }
                            }
                        }

                        if (lowestHpOwner != null)
                            targets.Add(lowestHpOwner);
                    }
                    break;
            }

            // Phase 7.9: Log warning if no targets were resolved
            if (targets.Count == 0)
            {
                LogTargetResolutionFailure(targetType, casterId, isCasterPlayer, "No valid targets found after resolution");
            }

            return targets;
        }

        /// <summary>
        /// Phase 7.9: 记录目标解析失败的诊断信息
        /// Phase 7.9: Log diagnostic information for target resolution failures
        /// </summary>
        private void LogTargetResolutionFailure(
            Skills.BuffTarget targetType,
            string entityId,
            bool isCasterPlayer,
            string reason)
        {
            // 使用 System.Diagnostics 进行诊断输出
            // Use System.Diagnostics for diagnostic output
            // 在生产环境中，这可以替换为更完善的日志系统
            // In production, this can be replaced with a more robust logging system
            System.Diagnostics.Debug.WriteLine(
                $"[MultiBattle] Target resolution failed: " +
                $"TargetType={targetType}, " +
                $"EntityId={entityId}, " +
                $"IsCasterPlayer={isCasterPlayer}, " +
                $"Reason={reason}, " +
                $"Time={_clock.NowMs}ms");
        }

        /// <summary>
        /// Phase 6: 应用即时治疗
        /// Phase 6: Apply instant heal
        /// </summary>
        private void ApplyInstantHeal(
            SkillCastResult result,
            string casterId,
            string? targetId,
            bool isCasterPlayer,
            string? skillId = null)
        {
            if (result.InstantHeal <= 0)
                return;

            // 即时治疗通常施加在施法者自己身上
            // Instant heal is usually applied to the caster
            Buffs.IBuffOwner? target = null;

            if (isCasterPlayer)
            {
                if (_playerBuffOwners.TryGetValue(casterId, out var playerOwner))
                    target = playerOwner;
            }
            else
            {
                if (_enemyBuffOwners.TryGetValue(casterId, out var enemyOwner))
                    target = enemyOwner;
            }

            if (target != null)
            {
                var healMeta = new Buffs.HealMeta("instant_heal", "skill_cast");
                int healAmount = result.InstantHeal;
                target.ReceiveHeal(healAmount, healMeta);

                // Phase 7: 记录 HealEvent
                // Phase 7: Record HealEvent
                RecordHeal(
                    ownerId: target.Id,
                    amount: healAmount,
                    resultingHp: target.CurrentHp,
                    healSource: skillId ?? "unknown_skill",
                    bundleId: result.BundleId
                );
            }
        }

        /// <summary>
        /// Phase 6: 应用资源消耗/获得
        /// Phase 6: Apply resource costs/gains
        /// </summary>
        private void ApplyResourceChanges(
            SkillCastResult result,
            string casterId,
            bool isCasterPlayer,
            string? skillId = null)
        {
            if (result.ResourceChanges == null || result.ResourceChanges.Count == 0)
                return;

            // 只应用给施法者
            // Only apply to caster
            if (!isCasterPlayer)
                return; // 敌人暂不支持资源系统 / Enemies don't have resource system yet

            if (!_playerResources.TryGetValue(casterId, out var resources))
                return;

            foreach (var kvp in result.ResourceChanges)
            {
                string resourceId = kvp.Key;
                int amount = kvp.Value;

                if (!resources.HasBucket(resourceId))
                    continue;

                var bucket = resources.GetBucket(resourceId);
                
                // 应用资源变化（正数为增加，负数为消耗）
                // Apply resource change (positive = gain, negative = cost)
                string reason;
                int actualChange;
                
                if (amount > 0)
                {
                    actualChange = bucket.Gain(amount, "skill_resource_gain");
                    reason = "skill_resource_gain";
                }
                else if (amount < 0)
                {
                    bucket.ForceConsume(-amount, "skill_resource_cost"); // Convert negative to positive for ForceConsume
                    actualChange = amount; // Negative for cost
                    reason = "skill_resource_cost";
                }
                else
                {
                    continue; // Skip zero changes
                }

                // Phase 7: 记录 ResourceGainEvent (包括消耗)
                // Phase 7: Record ResourceGainEvent (includes costs as negative)
                RecordResourceGain(
                    actorId: casterId,
                    bucketId: resourceId,
                    delta: actualChange,
                    newValue: bucket.Current,
                    reason: reason,
                    skillId: skillId,
                    bundleId: result.BundleId
                );
            }
        }

        /// <summary>
        /// 检查战斗状态
        /// Check battle state
        /// </summary>
        private void CheckBattleState(int now)
        {
            if (_playerTeam.IsAllDead)
            {
                if (_config.AllowPlayerRevive)
                {
                    _state = MultiBattleState.PlayerTeamDeadCooldown;
                    _resumeAtMs = now + _config.PlayerReviveCooldownMs;
                }
                else
                {
                    _state = MultiBattleState.Defeat;
                    Stop();
                }
                FireTeamStatusEvent();
            }
            else if (_enemyTeam.IsAllDead)
            {
                if (_config.AllowEnemyRespawn)
                {
                    _state = MultiBattleState.EnemyTeamDeadCooldown;
                    _resumeAtMs = now + _config.EnemyRespawnCooldownMs;
                }
                else
                {
                    _state = MultiBattleState.Victory;
                    Stop();
                }
                FireTeamStatusEvent();
            }
        }

        /// <summary>
        /// 处理冷却结束
        /// Handle cooldown end
        /// </summary>
        private void HandleCooldownEnd(int now)
        {
            if (_state == MultiBattleState.PlayerTeamDeadCooldown)
            {
                // 复活玩家队伍
                _playerTeam.ReviveAll(_config.ReviveWithFullHp);

                // Phase 4: 清除玩家所有 buff 和重置资源
                // Clear all player buffs and reset resources on death
                foreach (var kvp in _playerBuffOwners)
                {
                    var charId = kvp.Key;
                    var buffOwner = kvp.Value;
                    
                    // 清除所有 buff
                    buffOwner.ClearAllBuffs();
                    
                    // 重置资源
                    if (_playerResources.TryGetValue(charId, out var resources))
                    {
                        // 获取资源配置以重置到初始值
                        if (_professionResourceConfigs != null &&
                            buffOwner.Character.ActiveCombatProfessionId != null &&
                            _professionResourceConfigs.TryGetValue(buffOwner.Character.ActiveCombatProfessionId, out var profConfig))
                        {
                            // 重置到职业初始资源值
                            var bucket = resources.GetBucket(profConfig.Id);
                            bucket.Reset(profConfig.Initial);
                        }
                        else
                        {
                            // 后备：重置所有资源到 0
                            foreach (var bucket in resources.GetAll().Values)
                            {
                                bucket.Reset(0);
                            }
                        }
                    }
                }

                // 对称重置：玩家与敌人轨道全部重置到 now，避免冷却期间积压触发
                foreach (var tracks in _characterTracks.Values)
                {
                    tracks.Reset(now);
                }
                foreach (var track in _enemyTracks.Values)
                {
                    track.Reset(now);
                }

                _state = MultiBattleState.Fighting;
            }
            else if (_state == MultiBattleState.EnemyTeamDeadCooldown)
            {
                // 刷新敌人队伍
                _enemyTeam.ReviveAll(true);

                // Phase 4: 清除敌人所有 buff
                // Clear all enemy buffs on death
                foreach (var buffOwner in _enemyBuffOwners.Values)
                {
                    buffOwner.ClearAllBuffs();
                }

                // 对称重置：敌人与玩家轨道全部重置到 now，避免冷却期间积压触发
                foreach (var track in _enemyTracks.Values)
                {
                    track.Reset(now);
                }
                foreach (var tracks in _characterTracks.Values)
                {
                    tracks.Reset(now);
                }

                _state = MultiBattleState.Fighting;
            }

            FireTeamStatusEvent();
        }

        /// <summary>
        /// 处理掉落物
        /// Process loot drops
        /// </summary>
        private void ProcessLootDrops(string enemyId, Enemy enemy)
        {
            if (enemy.LootDrops == null || enemy.LootDrops.Count == 0)
                return;

            foreach (var drop in enemy.LootDrops)
            {
                if (_rng.NextDouble() > drop.DropChance)
                    continue;

                int quantity = drop.MinQuantity;
                if (drop.MaxQuantity > drop.MinQuantity)
                {
                    quantity = _rng.NextRange(drop.MinQuantity, drop.MaxQuantity);
                }

                if (quantity > 0)
                {
                    var lootEvent = new LootDropEvent
                    {
                        TimeMs = _clock.NowMs,
                        ItemId = drop.ItemId,
                        Quantity = quantity,
                        MonsterId = enemyId
                    };
                    LootDropped?.Invoke(lootEvent);
                }
            }
        }

        /// <summary>
        /// 处理经验获得 - 怪物死亡时触发
        /// Process experience gain - triggered when monster dies
        /// </summary>
        private void ProcessExperienceGain(string enemyId, Enemy enemy, string attackerId)
        {
            if (enemy.BaseExperience <= 0) return;

            // 获取击杀者的职业ID
            var attacker = _playerTeam.Members.FirstOrDefault(m => m.Id == attackerId);
            if (attacker == null) return;

            var expEvent = new ExperienceGainEvent
            {
                TimeMs = _clock.NowMs,
                ProfessionId = attacker.Entity.ActiveCombatProfessionId,
                BaseExperience = enemy.BaseExperience,
                MonsterId = enemy.MonsterId
            };
            ExperienceGained?.Invoke(expEvent);
        }

        /// <summary>
        /// 触发队伍状态事件
        /// Fire team status event
        /// </summary>
        private void FireTeamStatusEvent()
        {
            var playerStatus = new TeamStatus
            {
                TeamId = _playerTeam.TeamId,
                TeamName = _playerTeam.TeamName,
                AliveCount = _playerTeam.AliveCount,
                TotalCount = _playerTeam.TotalCount
            };

            foreach (var member in _playerTeam.Members)
            {
                playerStatus.MemberHp[member.Id] = member.CurrentHp;
                playerStatus.MemberMaxHp[member.Id] = member.MaxHp;
            }

            var enemyStatus = new TeamStatus
            {
                TeamId = _enemyTeam.TeamId,
                TeamName = _enemyTeam.TeamName,
                AliveCount = _enemyTeam.AliveCount,
                TotalCount = _enemyTeam.TotalCount
            };

            foreach (var member in _enemyTeam.Members)
            {
                enemyStatus.MemberHp[member.Id] = member.CurrentHp;
                enemyStatus.MemberMaxHp[member.Id] = member.MaxHp;
            }

            var statusEvent = new TeamStatusEvent
            {
                TimeMs = _clock.NowMs,
                PlayerTeamStatus = playerStatus,
                EnemyTeamStatus = enemyStatus,
                BattleState = _state
            };

            TeamStatusChanged?.Invoke(statusEvent);
        }

        /// <summary>
        /// 计算玩家伤害
        /// Roll player damage
        /// </summary>
        private int PlayerRollDamage(int baseDamage, Character character, bool allowCrit)
        {
            double dmg = Math.Floor(_rng.Jitter(baseDamage, character.VariancePct));
            if (dmg < 1) dmg = 1;

            if (allowCrit && _rng.NextDouble() < (character.CritChancePercent / 100.0))
            {
                dmg = Math.Floor(dmg * Math.Max(1.0, character.CritMultiplier));
            }

            return (int)dmg;
        }

        /// <summary>
        /// 计算敌人伤害
        /// Roll enemy damage
        /// </summary>
        private int EnemyRollDamage(int baseDamage, Enemy enemy)
        {
            double dmg = Math.Floor(_rng.Jitter(baseDamage, enemy.VariancePct));
            if (dmg < 1) dmg = 1;
            return (int)dmg;
        }

        /// <summary>
        /// 获取角色名称
        /// Get character name
        /// </summary>
        private string GetCharacterName(string characterId)
        {
            if (characterId.StartsWith("char_"))
            {
                return $"Character_{characterId.Substring(5)}";
            }
            else if (characterId.StartsWith("teammate_"))
            {
                return $"Character_teammate_{characterId.Substring(9)}";
            }
            return $"Character_{characterId}";
        }
        /// <summary>
        /// 获取敌人名称
        /// Get enemy name
        /// </summary>
        private string GetEnemyName(string enemyId)
        {
            // 直接使用传入的 enemyId，因为已经包含了有意义的名称
            return enemyId.Replace("_", " ");
        }

        /// <summary>
        /// 获取战斗快照
        /// Get battle snapshot
        /// </summary>
        public MultiBattleSnapshot GetSnapshot()
        {
            return new MultiBattleSnapshot
            {
                ElapsedMs = _clock.NowMs,
                State = _state,
                PlayerTeamAlive = _playerTeam.AliveCount,
                PlayerTeamTotal = _playerTeam.TotalCount,
                EnemyTeamAlive = _enemyTeam.AliveCount,
                EnemyTeamTotal = _enemyTeam.TotalCount,
                TotalPlayerDamage = _totalPlayerDamage,
                TotalEnemyDamage = _totalEnemyDamage,
                TimeToResumeMs = _state == MultiBattleState.PlayerTeamDeadCooldown ||
                                 _state == MultiBattleState.EnemyTeamDeadCooldown
                    ? Math.Max(0, _resumeAtMs - _clock.NowMs)
                    : 0,
                RngIndex = _rng.Index
            };
        }

        /// <summary>
        /// Phase 2: 获取玩家资源快照（用于 UI 显示）
        /// Phase 2: Get player resource snapshot (for UI display)
        /// </summary>
        public Dictionary<string, Dictionary<string, int>> GetResourceSnapshot()
        {
            var snapshot = new Dictionary<string, Dictionary<string, int>>();
            
            foreach (var (playerId, resources) in _playerResources)
            {
                snapshot[playerId] = new Dictionary<string, int>();
                foreach (var (bucketId, bucket) in resources.GetAll())
                {
                    snapshot[playerId][bucketId] = bucket.Current;
                }
            }
            
            return snapshot;
        }

        /// <summary>
        /// Phase 9: 获取玩家 Buff 快照（用于 UI 显示）
        /// Phase 9: Get player buff snapshot (for UI display)
        /// </summary>
        /// <param name="playerId">玩家 ID</param>
        /// <returns>Buff 列表快照，如果玩家不存在则返回空列表</returns>
        public List<Buffs.BuffInstance> GetPlayerBuffs(string playerId)
        {
            if (!_playerBuffOwners.TryGetValue(playerId, out var owner))
                return new List<Buffs.BuffInstance>();
            
            return owner.Buffs.Values.ToList();
        }

        /// <summary>
        /// Phase 9: 获取敌人 Buff 快照（用于 UI 显示）
        /// Phase 9: Get enemy buff snapshot (for UI display)
        /// </summary>
        /// <param name="enemyId">敌人 ID</param>
        /// <returns>Buff 列表快照，如果敌人不存在则返回空列表</returns>
        public List<Buffs.BuffInstance> GetEnemyBuffs(string enemyId)
        {
            if (!_enemyBuffOwners.TryGetValue(enemyId, out var owner))
                return new List<Buffs.BuffInstance>();
            
            return owner.Buffs.Values.ToList();
        }

        /// <summary>
        /// 构建战斗摘要
        /// Build battle digest
        /// </summary>
        public BattleDigest BuildDigest()
        {
            if (_running) Stop();

            return BattleDigest.FromSegments(
                durationMs: _clock.NowMs,
                tickCount: 0, // Note: Tick counting not implemented - not needed for current functionality
                rngStart: _rngIndexStart,
                rngEnd: _rngIndexEnd,
                seed: _rngSeed,
                segments: _segments
            );
        }

        /// <summary>
        /// 检查角色战斗轨道是否可用
        /// Check if character battle track is available
        /// </summary>
        /// <returns>如果角色存在且处于战斗状态返回true，否则返回false / Returns true if character exists and is in fighting state, false otherwise</returns>
        private bool IsCharacterTrackAvailable(string characterId, out CharacterTracks? tracks)
        {
            tracks = null;
            if (!_characterTracks.TryGetValue(characterId, out tracks))
                return false;
            
            return _state == MultiBattleState.Fighting && tracks.IsEnabled;
        }

        /// <summary>
        /// 检查敌人战斗轨道是否可用
        /// Check if enemy battle track is available
        /// </summary>
        /// <returns>如果敌人存在且处于战斗状态返回true，否则返回false / Returns true if enemy exists and is in fighting state, false otherwise</returns>
        private bool IsEnemyTrackAvailable(string enemyId, out EnemyTrack? track)
        {
            track = null;
            if (!_enemyTracks.TryGetValue(enemyId, out track))
                return false;
            
            return _state == MultiBattleState.Fighting && track.IsEnabled;
        }

        /// <summary>
        /// 获取指定角色的攻击进度（0-1范围）
        /// Get attack progress for specified character (0-1 range)
        /// </summary>
        /// <returns>返回0.0-1.0之间的进度值，角色不存在或非战斗状态时返回0.0 / Returns progress value between 0.0-1.0, returns 0.0 if character not found or not in fighting state</returns>
        public double GetCharacterAttackProgress(string characterId)
        {
            if (!IsCharacterTrackAvailable(characterId, out var tracks))
                return 0.0;

            return tracks!.AttackTrack.Progress01(_clock.NowMs);
        }

        /// <summary>
        /// 获取指定角色的特殊技能进度（0-1范围）
        /// Get special skill progress for specified character (0-1 range)
        /// </summary>
        /// <returns>返回0.0-1.0之间的进度值，角色不存在或非战斗状态时返回0.0 / Returns progress value between 0.0-1.0, returns 0.0 if character not found or not in fighting state</returns>
        public double GetCharacterSpecialProgress(string characterId)
        {
            if (!IsCharacterTrackAvailable(characterId, out var tracks))
                return 0.0;

            return tracks!.SpecialTrack.Progress01(_clock.NowMs);
        }

        /// <summary>
        /// 获取指定敌人的攻击进度（0-1范围）
        /// Get attack progress for specified enemy (0-1 range)
        /// </summary>
        /// <returns>返回0.0-1.0之间的进度值，敌人不存在或非战斗状态时返回0.0 / Returns progress value between 0.0-1.0, returns 0.0 if enemy not found or not in fighting state</returns>
        public double GetEnemyAttackProgress(string enemyId)
        {
            if (!IsEnemyTrackAvailable(enemyId, out var track))
                return 0.0;

            return track!.AttackTrack.Progress01(_clock.NowMs);
        }

        /// <summary>
        /// 获取指定角色的攻击剩余时间（毫秒）
        /// Get attack time remaining for specified character (milliseconds)
        /// </summary>
        /// <returns>返回剩余时间（毫秒），角色不存在或非战斗状态时返回0.0 / Returns remaining time in milliseconds, returns 0.0 if character not found or not in fighting state</returns>
        public double GetCharacterAttackTimeRemaining(string characterId)
        {
            if (!IsCharacterTrackAvailable(characterId, out var tracks))
                return 0.0;

            return tracks!.AttackTrack.TimeToNextMs(_clock.NowMs);
        }

        /// <summary>
        /// 获取指定角色的特殊技能剩余时间（毫秒）
        /// Get special skill time remaining for specified character (milliseconds)
        /// </summary>
        /// <returns>返回剩余时间（毫秒），角色不存在或非战斗状态时返回0.0 / Returns remaining time in milliseconds, returns 0.0 if character not found or not in fighting state</returns>
        public double GetCharacterSpecialTimeRemaining(string characterId)
        {
            if (!IsCharacterTrackAvailable(characterId, out var tracks))
                return 0.0;

            return tracks!.SpecialTrack.TimeToNextMs(_clock.NowMs);
        }

        /// <summary>
        /// 获取指定敌人的攻击剩余时间（毫秒）
        /// Get attack time remaining for specified enemy (milliseconds)
        /// </summary>
        /// <returns>返回剩余时间（毫秒），敌人不存在或非战斗状态时返回0.0 / Returns remaining time in milliseconds, returns 0.0 if enemy not found or not in fighting state</returns>
        public double GetEnemyAttackTimeRemaining(string enemyId)
        {
            if (!IsEnemyTrackAvailable(enemyId, out var track))
                return 0.0;

            return track!.AttackTrack.TimeToNextMs(_clock.NowMs);
        }
    }

    /// <summary>
    /// 多单位战斗配置
    /// Multi-unit battle configuration
    /// </summary>
    public class MultiBattleConfig
    {
        /// <summary>
        /// 玩家目标选择策略
        /// Player target selection strategy
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("playerTargetStrategy")]
        public TargetStrategy PlayerTargetStrategy { get; set; } = TargetStrategy.Random;

        /// <summary>
        /// 敌人目标选择策略
        /// Enemy target selection strategy
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("enemyTargetStrategy")]
        public TargetStrategy EnemyTargetStrategy { get; set; } = TargetStrategy.Random;

        /// <summary>
        /// 特殊技能是否为AOE (已弃用 - 使用 SpecialTargetPolicy 代替)
        /// Whether special skill is AOE (deprecated - use SpecialTargetPolicy instead)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("specialIsAoe")]
        public bool SpecialIsAoe { get; set; } = true;

        /// <summary>
        /// AOE伤害倍率
        /// AOE damage multiplier
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("aoeDamageMultiplier")]
        public double AoeDamageMultiplier { get; set; } = 0.8;

        /// <summary>
        /// 是否允许玩家复活
        /// Whether to allow player revival
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("allowPlayerRevive")]
        public bool AllowPlayerRevive { get; set; } = true;

        /// <summary>
        /// 是否允许敌人刷新
        /// Whether to allow enemy respawn
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("allowEnemyRespawn")]
        public bool AllowEnemyRespawn { get; set; } = true;

        /// <summary>
        /// 玩家复活冷却时间（毫秒）
        /// Player revive cooldown in milliseconds
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("playerReviveCooldownMs")]
        public int PlayerReviveCooldownMs { get; set; } = 5000;

        /// <summary>
        /// 敌人刷新冷却时间（毫秒）
        /// Enemy respawn cooldown in milliseconds
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("enemyRespawnCooldownMs")]
        public int EnemyRespawnCooldownMs { get; set; } = 3000;

        /// <summary>
        /// 复活时是否满血
        /// Whether to revive with full HP
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("reviveWithFullHp")]
        public bool ReviveWithFullHp { get; set; } = true;
    }

    /// <summary>
    /// 多单位战斗快照
    /// Multi-unit battle snapshot
    /// </summary>
    public class MultiBattleSnapshot
    {
        public int ElapsedMs { get; set; }
        public MultiBattleState State { get; set; }
        public int PlayerTeamAlive { get; set; }
        public int PlayerTeamTotal { get; set; }
        public int EnemyTeamAlive { get; set; }
        public int EnemyTeamTotal { get; set; }
        public int TotalPlayerDamage { get; set; }
        public int TotalEnemyDamage { get; set; }
        public int TimeToResumeMs { get; set; }
        public int RngIndex { get; set; }
    }
}