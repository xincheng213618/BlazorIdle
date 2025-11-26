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
        // Per-character cooldown managers for independent cooldown tracking
        // 每个角色独立的冷却管理器，用于独立跟踪冷却
        private readonly Dictionary<string, CooldownManager> _cooldownManagers = new();
        private readonly ResourceManager _resourceManager = new();
        
        // Phase 9: AutoCastEngine for unified skill scheduling / AutoCastEngine 统一技能调度
        private readonly AutoCastEngine _autoCastEngine;
        
        // Phase 6: WindowExecutor for window-based skill execution / WindowExecutor 用于窗口化技能执行
        // Uses GetOrCreateCooldownManager delegate for per-character cooldown tracking
        // 使用 GetOrCreateCooldownManager 委托实现每个角色独立的冷却跟踪
        private readonly WindowExecutor _windowExecutor;
        
        // Phase 9: Character data mapping for skill selection / 角色数据映射用于技能选择
        private readonly Dictionary<string, Shared.Models.CharacterData>? _characterDataMap;
        
        // Phase 7: TriggerProcessor for skill triggers / TriggerProcessor 用于技能触发
        // Uses GetOrCreateCooldownManager delegate for per-character cooldown tracking
        // 使用 GetOrCreateCooldownManager 委托实现每个角色独立的冷却跟踪
        private readonly TriggerProcessor _triggerProcessor;
        
        // 消耗品系统: 游戏配置服务，用于获取物品配置
        // Consumable system: Game config service for item configuration
        private readonly Config.IGameConfigService? _gameConfigService;
        
        // Step3: Periodic skill check system / 定期技能检查系统
        // Accumulator for periodic skill checks (every 1 second)
        // 定期技能检查累积器（每秒检查一次）
        private double _periodicCheckAccumulator = 0.0;
        
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
        // Phase 8: 施法事件 / Phase 8: Casting events
        public event Action<CastStartEvent>? CastStarted;
        public event Action<CastCompleteEvent>? CastCompleted;
        public event Action<CastInterruptEvent>? CastInterrupted;
        // 消耗品系统事件 / Consumable system events
        public event Action<ConsumableUsedEvent>? ConsumableUsed;
        public event Action<ConsumableOutOfStockEvent>? ConsumableOutOfStock;

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
            Dictionary<string, Shared.Models.ProfessionResourceConfig>? professionResourceConfigs = null,
            Dictionary<string, Shared.Models.CharacterData>? characterDataMap = null,
            Config.IGameConfigService? gameConfigService = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _playerTeam = playerTeam ?? throw new ArgumentNullException(nameof(playerTeam));
            _enemyTeam = enemyTeam ?? throw new ArgumentNullException(nameof(enemyTeam));
            _config = config ?? new MultiBattleConfig();
            _professionResourceConfigs = professionResourceConfigs;
            _characterDataMap = characterDataMap;
            _gameConfigService = gameConfigService;

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
            
            // Phase 9: 初始化 AutoCastEngine / Initialize AutoCastEngine
            _autoCastEngine = new AutoCastEngine(_skillRepository, _conditionChecker, GetOrCreateCooldownManager, _resourceManager);
            
            // Phase 6: 初始化 WindowExecutor / Initialize WindowExecutor
            _windowExecutor = new WindowExecutor(_skillRepository, _conditionChecker, GetOrCreateCooldownManager, _resourceManager);
            
            // Phase 7: 初始化 TriggerProcessor / Initialize TriggerProcessor
            _triggerProcessor = new TriggerProcessor(_skillRepository, _conditionChecker, GetOrCreateCooldownManager, _resourceManager);

            // Phase 8: 注册施法事件处理器 / Register casting event handlers
            _castingController.OnCastComplete += HandleCastComplete;
            _castingController.OnCastInterrupt += HandleCastInterrupt;

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

            // Phase 5: 清除所有冷却管理器（避免内存泄漏）
            // Phase 5: Clear all cooldown managers (prevent memory leak)
            _cooldownManagers.Clear();

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

            // Phase 8: 战斗开始时，检查每个角色是否应该立即开始施法
            // Phase 8: At battle start, check if each character should start casting immediately
            foreach (var charId in _playerTeam.GetAliveMemberIds())
            {
                TryStartCasting(charId, now);
            }

            // Phase 9: 战斗开始时，检查怪物是否应该立即开始施法
            // Phase 9: At battle start, check if monsters should start casting immediately
            foreach (var enemyId in _enemyTeam.GetAliveMemberIds())
            {
                var member = _enemyTeam.GetMember(enemyId);
                if (member != null && member.Entity.HasConfiguredSkills())
                {
                    TryStartMonsterCasting(enemyId, member.Entity, now);
                }
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
                    
                    // Phase 5: 更新所有角色的技能冷却时间
                    // Phase 5: Update skill cooldowns for all characters
                    foreach (var cooldownManager in _cooldownManagers.Values)
                    {
                        cooldownManager.TickCooldowns(deltaTimeSec);
                    }
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

                // Phase 8: 如果角色正在施法，跳过轨道检查
                // Phase 8: If character is casting, skip track checks
                if (_castingController.IsCastingForCaster(charId))
                {
                    continue;
                }

                // Phase 8/9: Window-GCD 集成 - Attack Track 处理
                // Phase 8/9: Window-GCD Integration - Attack Track processing
                // 如果 Attack Track 准备好了，检查是否应该施法或普攻
                // If Attack Track is ready, check if should cast or normal attack
                var atkCount = tracks.AttackTrack.CollectTriggers(now);
                for (int i = 0; i < atkCount; i++)
                {
                    ProcessAttackDecisionPoint(charId, character, now);
                }

                // Special 轨道暂时保留（可能在未来移除）
                // Keep Special track for now (may be removed in future)
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

                // Phase 5 & 6: 二次检查冷却和资源（必要的安全检查）
                // Phase 5 & 6: Double-check cooldown and resources (necessary safety check)
                // WindowExecutor 在选择技能时已检查，但在选择和执行之间可能有其他技能消耗资源或启动冷却
                // WindowExecutor checks during selection, but resources may be consumed or cooldowns started between selection and execution
                // 这个检查防止在同一窗口内多个技能执行时的竞争条件
                // This check prevents race conditions when multiple skills execute in the same window
                var casterCooldownMgr = GetOrCreateCooldownManager(casterId);
                if (skillDef != null && !casterCooldownMgr.IsReady(skillId))
                {
                    // 技能还在冷却中，跳过施放
                    // Skill is still on cooldown, skip casting
                    return;
                }

                // Phase 5 & 6: 检查资源消耗（二次验证，见上方注释）
                // Phase 5 & 6: Check resource cost (double verification, see comment above)
                if (skillDef != null && !_resourceManager.CheckResourceCost(skillDef, ctx))
                {
                    // 资源不足，跳过施放
                    // Insufficient resources, skip casting
                    return;
                }

                // Note: 资源消耗由 SkillResolver 处理并通过 ApplyResourceChanges 应用
                // Note: Resource consumption is handled by SkillResolver and applied via ApplyResourceChanges

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
                        // 处理伤害（针对敌人目标）
                        // Process damage (for enemy targets)
                        var enemyTarget = _enemyTeam.GetMember(targetId);
                        if (enemyTarget != null && damagePerTarget > 0)
                        {
                            ApplyDamageToEnemy(casterId, member, targetId, enemyTarget, damagePerTarget, eventSource, 
                                isAoe: isAoe, isCrit: result.IsCrit, skillId: skillId, bundleId: result.BundleId);
                        }
                        
                        // Phase 5: 即时治疗应用到每个目标（支持治疗队友）
                        // Phase 5: Instant heal applies to each target (supports healing allies)
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
                    casterCooldownMgr.StartCooldown(skillId, skillDef.CooldownSec);
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
                        // 处理伤害（针对玩家目标）
                        // Process damage (for player targets)
                        var playerTarget = _playerTeam.GetMember(targetId);
                        if (playerTarget != null && damagePerTarget > 0)
                        {
                            ApplyDamageToPlayer(casterId, member, targetId, playerTarget, damagePerTarget,
                                source: eventSource, skillId: skillId, bundleId: result.BundleId);
                        }
                        
                        // Phase 5: 即时治疗应用到每个目标（支持治疗队友）
                        // Phase 5: Instant heal applies to each target (supports healing allies)
                        ApplyInstantHeal(result, casterId, targetId, isCasterPlayer: false, skillId: skillId);
                    }
                    
                    // Buff操作和资源变化只应用一次（不是每个目标）
                    // Buff operations and resource changes apply once (not per target)
                    string? primaryTargetId = targetIds.Count > 0 ? targetIds[0] : null;
                    ProcessBuffOperations(result, casterId, primaryTargetId, isCasterPlayer: false);
                    ApplyResourceChanges(result, casterId, isCasterPlayer: false, skillId: skillId);
                }

                // Phase 9: 启动怪物技能冷却
                // Phase 9: Start monster skill cooldown
                if (skillDef != null && skillDef.CooldownSec > 0)
                {
                    var cooldownMgr = GetOrCreateCooldownManager(casterId);
                    cooldownMgr.StartCooldown(skillId, skillDef.CooldownSec);
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
            
            // Phase 7: Special 技能执行后处理窗口触发器
            // Phase 7: Process window triggers after special skill execution
            // Special 技能通常是瞬发技能，使用 OnPostAttackWindow 触发时机
            // Special skills are usually instant, use OnPostAttackWindow trigger timing
            ProcessWindowTriggers(charId, "OnPostAttackWindow", skillId, isCasterPlayer: true);
        }

        /// <summary>
        /// Phase 9: 处理攻击决策点（t=0）- Window-GCD 集成
        /// Phase 9: Process attack decision point (t=0) - Window-GCD Integration
        /// </summary>
        private void ProcessAttackDecisionPoint(string charId, Character character, int now)
        {
            // 获取 CharacterData（如果可用）
            // Get CharacterData (if available)
            Shared.Models.CharacterData? characterData = null;
            if (_characterDataMap != null && _characterDataMap.TryGetValue(charId, out var data))
            {
                characterData = data;
            }

            // 如果还是没有 CharacterData，回退到旧的逻辑
            // If still no CharacterData, fallback to old logic
            if (characterData == null)
            {
                ProcessCharacterAttackViaSkillResolver(charId, character);
                return;
            }

            // 构建战斗上下文
            // Build battle context
            var context = new BattleContext
            {
                Player = character,
                PlayerBuffOwner = _playerBuffOwners.GetValueOrDefault(charId),
                PlayerResources = _playerResources.GetValueOrDefault(charId),
                Rng = _rng,
                Clock = _clock,
                CurrentTargetId = SelectEnemyTarget(_config.PlayerTargetStrategy)
            };

            // FIX: 永远先执行普通攻击，然后检查施法
            // FIX: Always execute normal attack first, then check for casting
            // 这样避免浪费攻击进度条准备时间
            // This avoids wasting attack track preparation time
            
            // === 窗口执行路径：NormalAttack → PostAttack → TryStartCasting ===
            // === Window execution path: NormalAttack → PostAttack → TryStartCasting ===
            
            string normalAttackSkillId = character.GetNormalAttackSkillId();
            var normalAttackSkill = _skillRepository.GetSkill(normalAttackSkillId);
            bool normalAttackIsGcd = normalAttackSkill?.IsGcd ?? true;

            ExecuteSkill(charId, normalAttackSkillId, "attack", isCasterPlayer: true, EventSource.Attack);

            // Phase 6: PostAttack 窗口：使用 WindowExecutor 执行瞬发技能
            // Phase 6: PostAttack window: Use WindowExecutor to execute instant skills
            var instantSkills = _windowExecutor.ExecuteWindow(WindowType.PostAttack, charId, characterData, character.ActiveCombatProfessionId, context, normalAttackIsGcd);
            foreach (var skill in instantSkills)
            {
                ExecuteSkill(charId, skill.Id, "postattack", isCasterPlayer: true, EventSource.PostAttack);
            }
            
            // Phase 7: PostAttack 窗口触发器
            // Phase 7: PostAttack window triggers
            ProcessWindowTriggers(charId, "OnPostAttackWindow", normalAttackSkillId, isCasterPlayer: true);
            
            // FIX: 普攻完成后立即尝试施法（如果有可用的施法技能）
            // FIX: After normal attack, immediately try to start casting (if there's an available cast skill)
            // 这样可以避免等待下一次 AttackTrack 触发才检查施法
            // This avoids waiting for the next AttackTrack trigger to check for casting
            TryStartCasting(charId, now);
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
        /// 
        /// Phase 9: 扩展支持怪物施法和瞬发技能
        /// Phase 9: Extended to support monster cast and instant skills
        /// </summary>
        private void ProcessEnemyAttackViaSkillResolver(string enemyId, Enemy enemy)
        {
            // Phase 9: Check if monster has configured skills (cast/instant)
            if (enemy.HasConfiguredSkills())
            {
                ProcessMonsterSkillAttack(enemyId, enemy);
            }
            else
            {
                // Legacy path: Use normal attack skill
                string skillId = enemy.GetNormalAttackSkillId();
                ExecuteSkill(enemyId, skillId, "enemy_attack", isCasterPlayer: false);
            }
        }

        /// <summary>
        /// Phase 9: 战斗开始时尝试让怪物开始施法
        /// Phase 9: Try to start monster casting at battle start
        /// </summary>
        private bool TryStartMonsterCasting(string enemyId, Enemy enemy, int now)
        {
            // Monster shouldn't be casting yet
            if (_castingController.IsCastingForCaster(enemyId))
                return false;

            // Get monster's buff owner for context
            if (!_enemyBuffOwners.TryGetValue(enemyId, out var buffOwner))
                return false;

            // Create battle context for monster
            var context = new BattleContext
            {
                Player = null,  // Monster is caster, not player
                Enemy = enemy,
                PlayerBuffOwner = null,
                EnemyBuffOwners = _enemyBuffOwners,  // Pass the dictionary
                Rng = _rng,
                Clock = _clock,
                CurrentTargetId = SelectTargetForEnemy()  // Get a player target
            };

            // Try to select a cast skill
            var castSkill = _autoCastEngine.SelectMonsterCastSkill(enemyId, enemy, enemyId, context);
            if (castSkill != null && castSkill.CastTimeSec > 0)
            {
                // Start casting immediately
                double haste = 0.0;  // Monsters don't have haste for now
                bool castStarted = _castingController.StartCast(enemyId, castSkill.Id, castSkill.CastTimeSec, haste, pauseAttackTrack: true);
                
                if (castStarted && _enemyTracks.TryGetValue(enemyId, out var track))
                {
                    // Pause attack track during cast
                    track.AttackTrack.Pause(now);
                    
                    // Record event
                    RecordMonsterCastStart(enemyId, castSkill.Id, castSkill.CastTimeSec);
                    
                    return true;  // Started casting
                }
            }
            
            return false;  // Did not start casting
        }

        /// <summary>
        /// Phase 9: 处理怪物使用配置的技能系统（施法和瞬发技能）
        /// Phase 9: Process monster using configured skill system (cast and instant skills)
        /// </summary>
        private void ProcessMonsterSkillAttack(string enemyId, Enemy enemy)
        {
            // Get monster's buff owner for context
            if (!_enemyBuffOwners.TryGetValue(enemyId, out var buffOwner))
                return;

            // Create battle context for monster
            var context = new BattleContext
            {
                Player = null,  // Monster is caster, not player
                Enemy = enemy,
                PlayerBuffOwner = null,
                EnemyBuffOwners = _enemyBuffOwners,  // Pass the dictionary
                Rng = _rng,
                Clock = _clock,
                CurrentTargetId = SelectTargetForEnemy()  // Get a player target
            };

            // FIX: 永远先执行普通攻击，然后检查施法（与玩家行为一致）
            // FIX: Always execute normal attack first, then check for casting (matching player behavior)
            // 这样避免浪费攻击进度条准备时间
            // This avoids wasting attack track preparation time
            
            // Execute normal attack
            string normalAttackId = enemy.GetNormalAttackSkillId();
            ExecuteSkill(enemyId, normalAttackId, "enemy_attack", isCasterPlayer: false);

            // Phase 9: PostAttack Window - Execute instant skills after normal attack
            var instantSkills = _autoCastEngine.ExecuteMonsterWindow(enemy, enemyId, context, gcdAlreadyUsed: false, "PostAttack");
            foreach (var skill in instantSkills)
            {
                ExecuteSkill(enemyId, skill.Id, "enemy_skill", isCasterPlayer: false, EventSource.PostAttack);
            }
            
            // PostAttack window triggers
            ProcessWindowTriggers(enemyId, "OnPostAttackWindow", normalAttackId, isCasterPlayer: false);
            
            // FIX: After normal attack and instant skills, try to start casting
            // This matches player behavior - check for casting after completing attack sequence
            int nowMs = _clock.NowMs;
            TryStartMonsterCasting(enemyId, enemy, nowMs);
        }

        /// <summary>
        /// Phase 9: 记录怪物开始施法
        /// Phase 9: Record monster cast start
        /// </summary>
        private void RecordMonsterCastStart(string enemyId, string skillId, double castTime)
        {
            // This could be expanded to create a cast start event if needed
            // For now, the CastingController will handle the cast completion event
        }

        /// <summary>
        /// Phase 9: 为怪物选择一个玩家目标
        /// Phase 9: Select a player target for monster
        /// </summary>
        private string SelectTargetForEnemy()
        {
            // Use existing target selection logic
            var alivePlayerIds = _playerTeam.GetAliveMemberIds();
            if (alivePlayerIds.Count == 0)
                return string.Empty;

            // For now, use random selection (could be enhanced based on target policy)
            int randomIndex = _rng.NextRange(0, alivePlayerIds.Count - 1);
            return alivePlayerIds[randomIndex];
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

            // Phase 7: 处理攻击触发器 / Process attack triggers
            // 只在非触发技能造成伤害时才处理触发器，避免无限递归
            // Only process triggers for non-trigger skills to avoid infinite recursion
            if (source != EventSource.Trigger)
            {
                ProcessAttackTriggers(attackerId, skillId, isCrit, isCasterPlayer: true);
            }

            // 如果击杀，处理掉落物和经验
            if (isKill)
            {
                ProcessLootDrops(defenderId, defender.Entity);
                ProcessExperienceGain(defenderId, defender.Entity, attackerId);
                
                // Phase 8: 中断对该目标的施法 / Interrupt casting on this target
                CheckAndInterruptCasting(defenderId, isTargetPlayer: false);
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
            EventSource source = EventSource.EnemyAttack,
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
                Source = source,
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

            // Phase 7: 处理怪物攻击触发器 / Process monster attack triggers
            // 只在非触发技能造成伤害时才处理触发器，避免无限递归
            // Only process triggers for non-trigger skills to avoid infinite recursion
            if (source != EventSource.Trigger)
            {
                ProcessAttackTriggers(attackerId, skillId, isCrit: false, isCasterPlayer: false);
            }
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
            
            // Step3: 处理定期技能检查
            // Step3: Process periodic skill checks
            ProcessPeriodicSkillChecks(deltaTimeSec);
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
        /// Step3 Phase 1: 处理定期技能检查（每秒检查一次）
        /// Step3 Phase 1: Process periodic skill checks (check every 1 second)
        /// 
        /// 集成到 ProcessBuffTicks 中，复用现有的 BuffOwner 遍历逻辑
        /// Integrated into ProcessBuffTicks, reusing existing BuffOwner traversal logic
        /// </summary>
        private void ProcessPeriodicSkillChecks(double deltaTimeSec)
        {
            // 累积时间，每秒检查一次
            // Accumulate time, check every second
            _periodicCheckAccumulator += deltaTimeSec;
            
            if (_periodicCheckAccumulator < 1.0)
                return;
            
            // 减去1秒，保留余数以保持精度
            // Subtract 1 second, keep remainder for precision
            _periodicCheckAccumulator -= 1.0;
            
            int nowMs = _clock.NowMs;
            
            // Step3 Phase 1.4: 检查所有玩家的定期技能
            // Step3 Phase 1.4: Check all players' periodic skills
            foreach (var characterMember in _playerTeam.GetAliveMembers())
            {
                ProcessCharacterPeriodicSkills(characterMember.Id, nowMs);
                // 消耗品系统: 检查消耗品触发
                // Consumable system: Check consumable triggers
                ProcessCharacterConsumableChecks(characterMember.Id, nowMs);
            }
            
            // Step3 Phase 1.5: 检查所有怪物的定期技能
            // Step3 Phase 1.5: Check all monsters' periodic skills
            foreach (var enemyMember in _enemyTeam.GetAliveMembers())
            {
                ProcessEnemyPeriodicSkills(enemyMember.Id, nowMs);
            }
        }

        /// <summary>
        /// Step3 Phase 1.4: 处理角色的定期技能检查
        /// Step3 Phase 1.4: Process character's periodic skill checks
        /// </summary>
        /// <param name="characterId">角色ID / Character ID</param>
        /// <param name="nowMs">当前时间（毫秒）/ Current time (milliseconds)</param>
        private void ProcessCharacterPeriodicSkills(string characterId, int nowMs)
        {
            // Step3 Optimization: 获取角色数据
            // Step3 Optimization: Get character data
            if (_characterDataMap == null || !_characterDataMap.TryGetValue(characterId, out var characterData))
                return;

            // Step3 Optimization: 获取当前职业ID，添加 null 安全检查
            // Step3 Optimization: Get current profession ID with null safety checks
            var member = _playerTeam.GetMember(characterId);
            if (member == null)
                return;

            var character = member.Entity as Character;
            if (character == null || string.IsNullOrEmpty(character.ActiveCombatProfessionId))
                return;

            string professionId = character.ActiveCombatProfessionId;

            // Step3 Optimization: 获取装备的技能
            // Step3 Optimization: Get equipped skills
            if (!characterData.EquippedSkillsByProfession.TryGetValue(professionId, out var config))
                return;

            // Step3 Optimization: 快速路径 - 检查被动技能槽位是否有技能
            // Step3 Optimization: Fast path - check if passive slot has a skill
            if (string.IsNullOrEmpty(config.PassiveSlot))
                return;

            var passiveSkill = _skillRepository.GetSkillById(config.PassiveSlot);
            if (passiveSkill == null || passiveSkill.Triggers == null || passiveSkill.Triggers.Count == 0)
                return;

            // 创建战斗上下文
            // Create battle context
            var context = new BattleContext
            {
                Player = character,
                PlayerBuffOwner = _playerBuffOwners.GetValueOrDefault(characterId),
                PlayerResources = _playerResources.GetValueOrDefault(characterId),
                PlayerTeam = _playerTeam,
                EnemyTeam = _enemyTeam,
                EnemyBuffOwners = _enemyBuffOwners,
                Rng = _rng,
                Clock = _clock,
                CurrentTargetId = SelectEnemyTarget(_config.PlayerTargetStrategy)
            };

            // 检查是否有 OnPeriodic 触发器
            // Check if there are OnPeriodic triggers
            foreach (var trigger in passiveSkill.Triggers)
            {
                if (trigger.When != "OnPeriodic")
                    continue;

                // 检查触发条件（使用trigger的conditions或技能的conditions）
                // Check trigger conditions (use trigger's conditions or skill's conditions)
                var conditionsToCheck = trigger.Conditions ?? passiveSkill.Conditions;
                if (conditionsToCheck != null)
                {
                    // 创建临时技能定义用于条件检查
                    // Create temporary skill definition for condition checking
                    var tempSkill = new SkillDef
                    {
                        Id = passiveSkill.Id,
                        Conditions = conditionsToCheck
                    };

                    if (!_conditionChecker.CheckConditions(tempSkill, context, isCasterPlayer: true, characterId))
                        continue;
                }

                // 检查概率触发
                // Check proc chance
                if (trigger.ProcChance < 1.0)
                {
                    double roll = _rng.NextDouble();
                    if (roll > trigger.ProcChance)
                        continue;
                }

                // 获取要触发的技能
                // Get the skill to trigger
                if (string.IsNullOrEmpty(trigger.FireSkillId))
                    continue;

                var skillToFire = _skillRepository.GetSkillById(trigger.FireSkillId);
                if (skillToFire == null)
                    continue;

                // 检查冷却（除非ignoreRequirements为true）
                // Check cooldown (unless ignoreRequirements is true)
                if (!trigger.IgnoreRequirements)
                {
                    var cooldownManager = GetOrCreateCooldownManager(characterId);
                    if (!cooldownManager.IsReady(skillToFire.Id))
                        continue;
                }

                // 执行触发的技能
                // Execute the triggered skill
                ExecuteSkill(characterId, skillToFire.Id, "periodic_trigger", isCasterPlayer: true, EventSource.Trigger);
            }
        }

        /// <summary>
        /// Step3 Phase 1.5: 处理怪物的定期技能检查
        /// Step3 Phase 1.5: Process enemy's periodic skill checks
        /// </summary>
        /// <param name="enemyId">怪物ID / Enemy ID</param>
        /// <param name="nowMs">当前时间（毫秒）/ Current time (milliseconds)</param>
        private void ProcessEnemyPeriodicSkills(string enemyId, int nowMs)
        {
            // Step3 Optimization: 获取怪物成员，添加 null 安全检查
            // Step3 Optimization: Get enemy member with null safety checks
            var member = _enemyTeam.GetMember(enemyId);
            if (member == null)
                return;

            var enemy = member.Entity as Enemy;
            // Step3 Optimization: 快速路径 - 检查是否有定期技能
            // Step3 Optimization: Fast path - check if there are periodic skills
            if (enemy == null || enemy.PeriodicSkillIds == null || enemy.PeriodicSkillIds.Count == 0)
                return;

            // 创建战斗上下文
            // Create battle context
            var context = new BattleContext
            {
                Enemy = enemy,
                PlayerTeam = _playerTeam,
                EnemyTeam = _enemyTeam,
                EnemyBuffOwners = _enemyBuffOwners,
                Rng = _rng,
                Clock = _clock,
                CurrentTargetId = SelectPlayerTarget(_config.EnemyTargetStrategy)
            };

            // 遍历怪物的定期技能
            // Iterate through monster's periodic skills
            foreach (var skillId in enemy.PeriodicSkillIds)
            {
                var skill = _skillRepository.GetSkillById(skillId);
                if (skill == null || skill.Triggers == null || skill.Triggers.Count == 0)
                    continue;

                // 检查是否有 OnPeriodic 触发器
                // Check if there are OnPeriodic triggers
                foreach (var trigger in skill.Triggers)
                {
                    if (trigger.When != "OnPeriodic")
                        continue;

                    // 检查触发条件（使用trigger的conditions或技能的conditions）
                    // Check trigger conditions (use trigger's conditions or skill's conditions)
                    var conditionsToCheck = trigger.Conditions ?? skill.Conditions;
                    if (conditionsToCheck != null)
                    {
                        // 创建临时技能定义用于条件检查
                        // Create temporary skill definition for condition checking
                        var tempSkill = new SkillDef
                        {
                            Id = skill.Id,
                            Conditions = conditionsToCheck
                        };

                        if (!_conditionChecker.CheckConditions(tempSkill, context, isCasterPlayer: false, enemyId))
                            continue;
                    }

                    // 检查概率触发
                    // Check proc chance
                    if (trigger.ProcChance < 1.0)
                    {
                        double roll = _rng.NextDouble();
                        if (roll > trigger.ProcChance)
                            continue;
                    }

                    // 获取要触发的技能
                    // Get the skill to trigger
                    if (string.IsNullOrEmpty(trigger.FireSkillId))
                        continue;

                    var skillToFire = _skillRepository.GetSkillById(trigger.FireSkillId);
                    if (skillToFire == null)
                        continue;

                    // 检查冷却（除非ignoreRequirements为true）
                    // Check cooldown (unless ignoreRequirements is true)
                    if (!trigger.IgnoreRequirements)
                    {
                        var cooldownManager = GetOrCreateCooldownManager(enemyId);
                        if (!cooldownManager.IsReady(skillToFire.Id))
                            continue;
                    }

                    // 执行触发的技能
                    // Execute the triggered skill
                    ExecuteSkill(enemyId, skillToFire.Id, "periodic_trigger", isCasterPlayer: false, EventSource.Trigger);
                }
            }
        }

        #region 消耗品系统 / Consumable System

        /// <summary>
        /// 消耗品系统: 处理角色的消耗品检查
        /// Consumable system: Process character's consumable checks
        /// 
        /// 触发优先级: 先药水后食物，按槽位顺序 (potion_1 → potion_2 → food_1 → food_2)
        /// Trigger priority: Potions first, then food, in slot order
        /// </summary>
        /// <param name="characterId">角色ID / Character ID</param>
        /// <param name="nowMs">当前时间（毫秒）/ Current time (milliseconds)</param>
        private void ProcessCharacterConsumableChecks(string characterId, int nowMs)
        {
            // 检查必要的依赖是否可用
            // Check if required dependencies are available
            if (_characterDataMap == null || !_characterDataMap.TryGetValue(characterId, out var characterData))
                return;
            
            if (_gameConfigService == null)
                return;

            // 获取角色的消耗品配置
            // Get character's consumable configuration
            var consumableConfig = characterData.EquippedConsumables;
            if (consumableConfig == null)
                return;

            // 获取角色成员
            // Get character member
            var member = _playerTeam.GetMember(characterId);
            if (member == null)
                return;

            var character = member.Entity as Character;
            if (character == null)
                return;

            // 创建战斗上下文
            // Create battle context
            var context = new BattleContext
            {
                Player = character,
                PlayerBuffOwner = _playerBuffOwners.GetValueOrDefault(characterId),
                PlayerResources = _playerResources.GetValueOrDefault(characterId),
                PlayerTeam = _playerTeam,
                EnemyTeam = _enemyTeam,
                EnemyBuffOwners = _enemyBuffOwners,
                Rng = _rng,
                Clock = _clock,
                CurrentTargetId = SelectEnemyTarget(_config.PlayerTargetStrategy)
            };

            // 处理药水槽位（优先触发，提供增益效果）
            // Process potion slots (higher priority, provides buff effects)
            foreach (var kvp in consumableConfig.PotionSlots.OrderBy(x => x.Key))
            {
                ProcessConsumableSlot(characterId, kvp.Key, kvp.Value, context, nowMs, characterData);
            }

            // 处理食物槽位（其次触发，提供恢复效果）
            // Process food slots (lower priority, provides recovery effects)
            foreach (var kvp in consumableConfig.FoodSlots.OrderBy(x => x.Key))
            {
                ProcessConsumableSlot(characterId, kvp.Key, kvp.Value, context, nowMs, characterData);
            }
        }

        /// <summary>
        /// 消耗品系统: 处理单个消耗品槽位
        /// Consumable system: Process a single consumable slot
        /// </summary>
        private void ProcessConsumableSlot(
            string characterId,
            string slotId,
            Shared.Models.ConsumableSlotData slotData,
            BattleContext context,
            int nowMs,
            Shared.Models.CharacterData characterData)
        {
            // 检查槽位是否装备
            // Check if slot is equipped
            if (string.IsNullOrEmpty(slotData.ItemId) || string.IsNullOrEmpty(slotData.SkillId))
                return;

            // 获取物品配置
            // Get item configuration
            var itemConfig = _gameConfigService?.GetItem(slotData.ItemId);
            if (itemConfig?.ConsumableConfig == null)
                return;

            // 检查触发条件
            // Check trigger conditions
            if (itemConfig.ConsumableConfig.TriggerConditions != null)
            {
                var tempSkill = new SkillDef { Conditions = itemConfig.ConsumableConfig.TriggerConditions };
                if (!_conditionChecker.CheckConditions(tempSkill, context, isCasterPlayer: true, characterId))
                    return;
            }

            // 检查冷却（按物品ID追踪，同类物品共享冷却）
            // Check cooldown (tracked per item ID, same item type shares cooldown)
            var cooldownManager = GetOrCreateCooldownManager(characterId);
            string cooldownKey = $"consumable_{slotData.ItemId}";
            if (!cooldownManager.IsReady(cooldownKey))
                return;

            // 检查背包库存（不自动卸载，保留配置等待玩家补充）
            // Check inventory (no auto-unequip, preserve config waiting for player to replenish)
            int currentCount = characterData.Inventory.GetItemQuantity(slotData.ItemId);
            if (currentCount <= 0)
            {
                // 库存不足，触发库存耗尽事件（UI可据此显示特殊样式）
                // Out of stock, trigger event (UI can show special style)
                ConsumableOutOfStock?.Invoke(new ConsumableOutOfStockEvent
                {
                    TimeMs = nowMs,
                    CharacterId = characterId,
                    ItemId = slotData.ItemId,
                    SlotId = slotId
                });
                return;
            }

            // 扣除背包库存
            // Deduct from inventory
            if (!characterData.Inventory.TryConsumeItem(slotData.ItemId, 1))
                return;

            // 执行消耗品技能
            // Execute consumable skill
            ExecuteSkill(characterId, slotData.SkillId, $"consumable_{slotId}", isCasterPlayer: true, EventSource.Consumable);

            // 启动冷却（使用物品配置的冷却时间）
            // Start cooldown (using item's cooldown time)
            double cooldownSec = itemConfig.ConsumableConfig.CooldownSec;
            if (cooldownSec > 0)
            {
                cooldownManager.StartCooldown(cooldownKey, cooldownSec);
            }

            // 触发消耗品使用事件
            // Trigger consumable used event
            ConsumableUsed?.Invoke(new ConsumableUsedEvent
            {
                TimeMs = nowMs,
                CharacterId = characterId,
                ItemId = slotData.ItemId,
                SkillId = slotData.SkillId,
                SlotId = slotId,
                RemainingCount = characterData.Inventory.GetItemQuantity(slotData.ItemId)
            });
        }

        #endregion

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
                
                // Phase 7: Support stack reduction
                // If StacksToRemove is specified and > 0, reduce stacks instead of removing entirely
                if (operation.StacksToRemove.HasValue && operation.StacksToRemove.Value > 0)
                {
                    target.ReduceBuffStacks(operation.BuffIdToRemove, operation.StacksToRemove.Value, reason);
                }
                else
                {
                    // Remove entire buff (all stacks)
                    target.RemoveBuff(operation.BuffIdToRemove, reason);
                }

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

            // Phase 5: 即时治疗应用到指定目标（如果没有指定目标，则应用到施法者）
            // Phase 5: Instant heal applies to specified target (if no target specified, applies to caster)
            Buffs.IBuffOwner? target = null;
            string actualTargetId = targetId ?? casterId;

            if (isCasterPlayer)
            {
                // 玩家技能：目标应该在玩家队伍中（治疗队友或自己）
                // Player skill: target should be in player team (heal allies or self)
                if (_playerBuffOwners.TryGetValue(actualTargetId, out var playerOwner))
                    target = playerOwner;
            }
            else
            {
                // 怪物技能：目标应该在怪物队伍中（治疗怪物队友或自己）
                // Monster skill: target should be in monster team (heal monster allies or self)
                if (_enemyBuffOwners.TryGetValue(actualTargetId, out var enemyOwner))
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

        /// <summary>
        /// Phase 8: 检查指定角色是否正在施法
        /// Phase 8: Check if specified character is casting
        /// </summary>
        /// <returns>如果正在施法返回true，否则返回false / Returns true if casting, false otherwise</returns>
        public bool IsCastingForCharacter(string characterId)
        {
            return _castingController.IsCastingForCaster(characterId);
        }

        /// <summary>
        /// Phase 8: 获取指定角色的施法进度（0-1范围）
        /// Phase 8: Get casting progress for specified character (0-1 range)
        /// </summary>
        /// <returns>返回0.0-1.0之间的进度值 / Returns progress value between 0.0-1.0</returns>
        public double GetCastingProgress(string characterId)
        {
            return _castingController.GetCastProgress(characterId);
        }

        /// <summary>
        /// Phase 8: 获取指定角色的施法剩余时间（毫秒）
        /// Phase 8: Get casting time remaining for specified character (milliseconds)
        /// </summary>
        /// <returns>返回剩余时间（毫秒）/ Returns remaining time in milliseconds</returns>
        public double GetCastingTimeRemaining(string characterId)
        {
            return _castingController.GetRemainingCastTime(characterId) * 1000.0; // Convert seconds to milliseconds
        }

        /// <summary>
        /// Phase 8: 获取指定角色正在施法的技能ID
        /// Phase 8: Get skill ID being cast by specified character
        /// </summary>
        /// <returns>技能ID，如果未施法返回null / Skill ID, or null if not casting</returns>
        public string? GetCastingSkillId(string characterId)
        {
            var activeCast = _castingController.GetActiveCast(characterId);
            return activeCast?.SkillId;
        }

        /// <summary>
        /// Phase 8: 获取技能仓库（用于UI获取技能信息）
        /// Phase 8: Get skill repository (for UI to get skill information)
        /// </summary>
        /// <returns>技能仓库 / Skill repository</returns>
        public SkillRepository? GetSkillRepository()
        {
            return _skillRepository;
        }

        /// <summary>
        /// Get or create cooldown manager for a specific caster
        /// 获取或创建指定施法者的冷却管理器
        /// </summary>
        private CooldownManager GetOrCreateCooldownManager(string casterId)
        {
            if (string.IsNullOrEmpty(casterId))
            {
                throw new ArgumentException("CasterId cannot be null or empty", nameof(casterId));
            }

            if (!_cooldownManagers.TryGetValue(casterId, out var manager))
            {
                manager = new CooldownManager();
                _cooldownManagers[casterId] = manager;
            }
            return manager;
        }

        /// <summary>
        /// Phase 10.3: 获取技能剩余冷却时间
        /// Phase 10.3: Get remaining cooldown time for a skill
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <returns>剩余冷却时间（秒）/ Remaining cooldown time in seconds</returns>
        public double GetSkillRemainingCooldown(string casterId, string skillId)
        {
            var manager = GetOrCreateCooldownManager(casterId);
            return manager.GetRemainingCooldown(skillId);
        }

        /// <summary>
        /// Phase 10.3: 检查技能是否准备就绪（不在冷却中）
        /// Phase 10.3: Check if a skill is ready (not on cooldown)
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        /// <returns>如果技能可用返回 true / True if skill is ready</returns>
        public bool IsSkillReady(string casterId, string skillId)
        {
            var manager = GetOrCreateCooldownManager(casterId);
            return manager.IsReady(skillId);
        }

        /// <summary>
        /// Phase 7: 处理攻击触发器
        /// Phase 7: Process attack triggers
        /// </summary>
        private void ProcessAttackTriggers(string casterId, string? skillId, bool isCrit, bool isCasterPlayer)
        {
            // 获取源技能定义（如果有）
            // Get source skill definition (if any)
            SkillDef? sourceSkill = skillId != null ? _skillRepository.GetSkill(skillId) : null;
            
            // 创建战斗上下文用于触发检查
            // Create battle context for trigger checking
            BattleContext context;
            Shared.Models.CharacterData? characterData = null;
            string? professionId = null;
            
            if (isCasterPlayer)
            {
                var member = _playerTeam.GetMember(casterId);
                if (member == null) return;
                
                var defaultTargetId = SelectEnemyTarget(_config.PlayerTargetStrategy);
                var defaultTarget = defaultTargetId != null ? _enemyTeam.GetMember(defaultTargetId) : null;
                
                context = new BattleContext
                {
                    Player = member.Entity,
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
                
                // 获取角色数据用于装备技能触发
                // Get character data for equipped skill triggers
                if (_characterDataMap != null)
                {
                    _characterDataMap.TryGetValue(casterId, out characterData);
                    professionId = member.Entity.ActiveCombatProfessionId;
                }
            }
            else
            {
                // 怪物攻击
                // Monster attack
                var member = _enemyTeam.GetMember(casterId);
                if (member == null) return;
                
                var defaultTargetId = SelectPlayerTarget(_config.EnemyTargetStrategy);
                var defaultTarget = defaultTargetId != null ? _playerTeam.GetMember(defaultTargetId) : null;
                
                context = new BattleContext
                {
                    Player = defaultTarget?.Entity,
                    Enemy = member.Entity,
                    PlayerTeam = _playerTeam,
                    EnemyTeam = _enemyTeam,
                    Rng = _rng,
                    Clock = _clock,
                    PlayerResources = defaultTargetId != null ? _playerResources.GetValueOrDefault(defaultTargetId) : null,
                    PlayerBuffOwner = defaultTargetId != null ? _playerBuffOwners.GetValueOrDefault(defaultTargetId) : null,
                    EnemyBuffOwners = _enemyBuffOwners,
                    CurrentTargetId = defaultTargetId
                };
            }
            
            // 处理 OnAttackHit 触发
            // Process OnAttackHit triggers
            var hitTriggers = _triggerProcessor.ProcessTriggers(
                "OnAttackHit",
                casterId,
                sourceSkill,
                context,
                isCasterPlayer,
                characterData,
                professionId,
                wasCrit: isCrit);
            
            // 如果是暴击，处理 OnAttackCrit 触发
            // If crit, process OnAttackCrit triggers
            List<SkillDef> critTriggers = new List<SkillDef>();
            if (isCrit)
            {
                critTriggers = _triggerProcessor.ProcessTriggers(
                    "OnAttackCrit",
                    casterId,
                    sourceSkill,
                    context,
                    isCasterPlayer,
                    characterData,
                    professionId,
                    wasCrit: true);
            }
            
            // 执行所有触发的技能
            // Execute all triggered skills
            foreach (var triggeredSkill in hitTriggers.Concat(critTriggers))
            {
                ExecuteSkill(casterId, triggeredSkill.Id, "trigger", isCasterPlayer, EventSource.Trigger);
            }
        }

        /// <summary>
        /// Phase 7: 处理窗口触发器
        /// Phase 7: Process window triggers
        /// </summary>
        private void ProcessWindowTriggers(string casterId, string windowType, string? sourceSkillId, bool isCasterPlayer)
        {
            // 获取源技能定义（如果有）
            // Get source skill definition (if any)
            SkillDef? sourceSkill = sourceSkillId != null ? _skillRepository.GetSkill(sourceSkillId) : null;
            
            // 创建战斗上下文
            // Create battle context
            BattleContext context;
            Shared.Models.CharacterData? characterData = null;
            string? professionId = null;
            
            if (isCasterPlayer)
            {
                var member = _playerTeam.GetMember(casterId);
                if (member == null) return;
                
                var defaultTargetId = SelectEnemyTarget(_config.PlayerTargetStrategy);
                var defaultTarget = defaultTargetId != null ? _enemyTeam.GetMember(defaultTargetId) : null;
                
                context = new BattleContext
                {
                    Player = member.Entity,
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
                
                if (_characterDataMap != null)
                {
                    _characterDataMap.TryGetValue(casterId, out characterData);
                    professionId = member.Entity.ActiveCombatProfessionId;
                }
            }
            else
            {
                // 怪物目前不使用窗口触发，但保持接口一致性
                // Monsters don't currently use window triggers, but keep interface consistent
                return;
            }
            
            // 处理窗口触发
            // Process window triggers
            var triggers = _triggerProcessor.ProcessTriggers(
                windowType,
                casterId,
                sourceSkill,
                context,
                isCasterPlayer,
                characterData,
                professionId,
                wasCrit: false);
            
            // 执行所有触发的技能
            // Execute all triggered skills
            foreach (var triggeredSkill in triggers)
            {
                EventSource eventSource = windowType == "OnPostAttackWindow" ? EventSource.PostAttack : EventSource.PostCast;
                ExecuteSkill(casterId, triggeredSkill.Id, "windowtrigger", isCasterPlayer, eventSource);
            }
        }

        /// <summary>
        /// Phase 8: 检查并尝试开始施法
        /// Phase 8: Check and try to start casting
        /// </summary>
        /// <param name="charId">角色ID / Character ID</param>
        /// <param name="now">当前时间 / Current time</param>
        /// <returns>是否开始了施法 / Whether casting was started</returns>
        private bool TryStartCasting(string charId, int now)
        {
            // 如果已经在施法，返回 false
            // If already casting, return false
            if (_castingController.IsCastingForCaster(charId))
            {
                return false;
            }

            // 获取角色和轨道
            // Get character and tracks
            var member = _playerTeam.GetMember(charId);
            if (member == null) return false;
            var character = member.Entity;

            if (!_characterTracks.TryGetValue(charId, out var tracks))
                return false;

            // 获取 CharacterData
            // Get CharacterData
            Shared.Models.CharacterData? characterData = null;
            if (_characterDataMap != null && _characterDataMap.TryGetValue(charId, out var data))
            {
                characterData = data;
            }

            if (characterData == null)
                return false;

            // 构建战斗上下文
            // Build battle context
            var context = new BattleContext
            {
                Player = character,
                PlayerBuffOwner = _playerBuffOwners.GetValueOrDefault(charId),
                PlayerResources = _playerResources.GetValueOrDefault(charId),
                Rng = _rng,
                Clock = _clock,
                CurrentTargetId = SelectEnemyTarget(_config.PlayerTargetStrategy)
            };

            // 检查是否有施法技能可用
            // Check if there's a cast skill available
            var castSkills = _windowExecutor.ExecuteWindow(WindowType.PreAttack, charId, characterData, character.ActiveCombatProfessionId, context, gcdAlreadyUsed: false);
            var castSkill = castSkills.FirstOrDefault();

            if (castSkill != null && castSkill.CastTimeSec > 0)
            {
                // 获取急速加成 / Get haste bonus
                double hastePercent = character.HastePercent;
                if (_playerBuffOwners.TryGetValue(charId, out var buffOwner))
                {
                    // 应用 Buff 效果到急速 / Apply buff effects to haste
                    var sortedBuffs = buffOwner.Buffs.Values
                        .OrderBy(b => b.AppliedAtMs)
                        .ToList();
                    
                    foreach (var buff in sortedBuffs)
                    {
                        foreach (var effect in buff.Effects)
                        {
                            if (effect.Target != "HastePercent") continue;
                            
                            switch (effect.Type)
                            {
                                case Buffs.BuffEffectType.StatMultiplier:
                                    hastePercent *= (1.0 + effect.Value);
                                    break;
                                case Buffs.BuffEffectType.StatAdditive:
                                    hastePercent += effect.Value;
                                    break;
                                case Buffs.BuffEffectType.StatReduction:
                                    hastePercent *= (1.0 - effect.Value);
                                    break;
                            }
                        }
                    }
                }

                // 开始施法 / Start casting
                bool castStarted = _castingController.StartCast(charId, castSkill.Id, castSkill.CastTimeSec, hastePercent, pauseAttackTrack: true);
                
                if (castStarted)
                {
                    // 暂停攻击轨道 / Pause attack track
                    tracks.PauseAttackTrack(now);

                    // 记录施法开始事件 / Record cast start event
                    var actualCastTime = castSkill.CastTimeSec / (1.0 + hastePercent / 100.0);
                    CastStarted?.Invoke(new CastStartEvent
                    {
                        TimeMs = now,
                        CasterId = charId,
                        SkillId = castSkill.Id,
                        CastTimeSec = actualCastTime,
                        PauseAttackTrack = true
                    });

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Phase 8: 处理施法完成事件
        /// Phase 8: Handle cast complete event
        /// </summary>
        private void HandleCastComplete(string casterId, string skillId)
        {
            int now = _clock.NowMs;

            // 获取施法的技能定义 / Get cast skill definition
            var skillDef = _skillRepository.GetSkill(skillId);
            if (skillDef == null) return;

            // Phase 9: Check if caster is a player or monster
            var playerMember = _playerTeam.GetMember(casterId);
            if (playerMember != null)
            {
                HandlePlayerCastComplete(casterId, skillId, skillDef, playerMember.Entity, now);
            }
            else
            {
                // Phase 9: Monster cast complete
                var enemyMember = _enemyTeam.GetMember(casterId);
                if (enemyMember != null)
                {
                    HandleMonsterCastComplete(casterId, skillId, skillDef, enemyMember.Entity, now);
                }
            }
        }

        /// <summary>
        /// Phase 9: 处理玩家施法完成
        /// Phase 9: Handle player cast complete
        /// </summary>
        private void HandlePlayerCastComplete(string casterId, string skillId, SkillDef skillDef, Character character, int now)
        {

            // 执行施法技能效果 / Execute cast skill effects
            ExecuteSkill(casterId, skillId, "cast", isCasterPlayer: true, EventSource.Cast);

            // 获取 CharacterData / Get CharacterData
            Shared.Models.CharacterData? characterData = null;
            if (_characterDataMap != null && _characterDataMap.TryGetValue(casterId, out var data))
            {
                characterData = data;
            }

            // Phase 6: PostCast 窗口：施法完成后执行瞬发技能
            // Phase 6: PostCast window: Execute instant skills after casting
            if (characterData != null)
            {
                // 构建战斗上下文 / Build battle context
                var context = new BattleContext
                {
                    Player = character,
                    PlayerBuffOwner = _playerBuffOwners.GetValueOrDefault(casterId),
                    PlayerResources = _playerResources.GetValueOrDefault(casterId),
                    Rng = _rng,
                    Clock = _clock,
                    CurrentTargetId = SelectEnemyTarget(_config.PlayerTargetStrategy)
                };

                bool castSkillIsGcd = skillDef.IsGcd;
                var postCastSkills = _windowExecutor.ExecuteWindow(WindowType.PostCast, casterId, characterData, character.ActiveCombatProfessionId, context, castSkillIsGcd);
                foreach (var skill in postCastSkills)
                {
                    ExecuteSkill(casterId, skill.Id, "postcast", isCasterPlayer: true, EventSource.PostCast);
                }

                // Phase 7: PostCast 窗口触发器 / PostCast window triggers
                ProcessWindowTriggers(casterId, "OnPostCastWindow", skillId, isCasterPlayer: true);
            }

            // 记录施法完成事件 / Record cast complete event
            var activeCast = _castingController.GetActiveCast(casterId);
            CastCompleted?.Invoke(new CastCompleteEvent
            {
                TimeMs = now,
                CasterId = casterId,
                SkillId = skillId,
                ActualCastTimeSec = activeCast?.ElapsedSec ?? 0
            });

            // Phase 8: 施法完成后，立即检查是否应该开始下一个施法
            // Phase 8: After cast completes, immediately check if should start next cast
            bool startedNewCast = TryStartCasting(casterId, now);
            
            // Phase 9 Fix: 如果没有开始新的施法，恢复攻击轨道
            // Phase 9 Fix: If didn't start new cast, resume attack track
            if (!startedNewCast)
            {
                if (_characterTracks.TryGetValue(casterId, out var tracks))
                {
                    // 恢复攻击轨道（使用暂停前的剩余时间）
                    // Resume attack track (using remaining time before pause)
                    tracks.ResumeAttackTrack(now);
                }
            }
        }

        /// <summary>
        /// Phase 9: 处理怪物施法完成
        /// Phase 9: Handle monster cast complete
        /// </summary>
        private void HandleMonsterCastComplete(string monsterId, string skillId, SkillDef skillDef, Enemy enemy, int now)
        {
            // 执行施法技能效果 / Execute cast skill effects
            ExecuteSkill(monsterId, skillId, "enemy_cast", isCasterPlayer: false, EventSource.Cast);

            // Phase 9: PostCast 窗口：怪物施法完成后执行瞬发技能
            // Phase 9: PostCast window: Execute instant skills after monster casting
            if (_enemyBuffOwners.TryGetValue(monsterId, out var buffOwner))
            {
                // 构建战斗上下文 / Build battle context
                var context = new BattleContext
                {
                    Player = null,
                    Enemy = enemy,
                    PlayerBuffOwner = null,
                    EnemyBuffOwners = _enemyBuffOwners,  // Pass the dictionary
                    Rng = _rng,
                    Clock = _clock,
                    CurrentTargetId = SelectTargetForEnemy()
                };

                bool castSkillIsGcd = skillDef.IsGcd;
                var postCastSkills = _autoCastEngine.ExecuteMonsterWindow(enemy, monsterId, context, castSkillIsGcd, "PostCast");
                foreach (var skill in postCastSkills)
                {
                    ExecuteSkill(monsterId, skill.Id, "enemy_postcast", isCasterPlayer: false, EventSource.PostCast);
                }

                // Phase 7: PostCast 窗口触发器 / PostCast window triggers
                ProcessWindowTriggers(monsterId, "OnPostCastWindow", skillId, isCasterPlayer: false);
            }

            // 记录施法完成事件 / Record cast complete event
            var activeCast = _castingController.GetActiveCast(monsterId);
            CastCompleted?.Invoke(new CastCompleteEvent
            {
                TimeMs = now,
                CasterId = monsterId,
                SkillId = skillId,
                ActualCastTimeSec = activeCast?.ElapsedSec ?? 0
            });

            // Phase 9 Fix: 怪物施法完成后也需要尝试开始下一次施法
            // Phase 9 Fix: Monster should also try to start next cast after cast completes
            bool startedNewCast = TryStartMonsterCasting(monsterId, enemy, now);
            
            // Phase 9 Fix: 如果没有开始新的施法，恢复攻击轨道
            // Phase 9 Fix: If didn't start new cast, resume attack track
            if (!startedNewCast)
            {
                if (_enemyTracks.TryGetValue(monsterId, out var track))
                {
                    // 恢复攻击轨道（使用暂停前的剩余时间）
                    // Resume attack track (using remaining time before pause)
                    track.AttackTrack.Resume(now);
                }
            }
        }

        /// <summary>
        /// Phase 8: 处理施法中断事件
        /// Phase 8: Handle cast interrupt event
        /// </summary>
        private void HandleCastInterrupt(string casterId, string skillId, string reason)
        {
            int now = _clock.NowMs;

            // 恢复攻击轨道 / Resume attack track
            if (_characterTracks.TryGetValue(casterId, out var tracks))
            {
                tracks.ResumeAttackTrack(now);
            }

            // 记录施法中断事件 / Record cast interrupt event
            var activeCast = _castingController.GetActiveCast(casterId);
            CastInterrupted?.Invoke(new CastInterruptEvent
            {
                TimeMs = now,
                CasterId = casterId,
                SkillId = skillId,
                Reason = reason,
                ElapsedSec = activeCast?.ElapsedSec ?? 0
            });
        }

        /// <summary>
        /// Phase 8: 检查并中断施法（所有敌人死亡时）
        /// Phase 8: Check and interrupt casting (when all enemies are dead)
        /// </summary>
        private void CheckAndInterruptCasting(string targetId, bool isTargetPlayer)
        {
            // 如果死亡的是敌人，检查是否所有敌人都死了
            // If target is an enemy, check if all enemies are dead
            if (!isTargetPlayer)
            {
                // 只有当所有敌人都死亡时才中断施法
                // Only interrupt casting when ALL enemies are dead
                var aliveEnemies = _enemyTeam.GetAliveMemberIds();
                if (aliveEnemies.Count == 0)
                {
                    // 所有敌人死亡，中断所有正在施法的玩家
                    // All enemies dead, interrupt all casting players
                    var pausedTracks = _castingController.GetPausedTracks();
                    foreach (var casterId in pausedTracks)
                    {
                        if (_castingController.IsCastingForCaster(casterId))
                        {
                            var activeCast = _castingController.GetActiveCast(casterId);
                            if (activeCast != null)
                            {
                                _castingController.CancelCast(casterId, "all_enemies_dead");
                                // HandleCastInterrupt will be called by the event handler
                            }
                        }
                    }
                }
                // 否则不中断，HandleCastComplete 会自动重新选择目标
                // Otherwise don't interrupt, HandleCastComplete will auto-retarget
            }
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