# Buff与技能设计 - Step 0 实施进度追踪

## 📋 概述

本文档用于追踪 Step 0（统一技能施放接口与 Track 抽象）的实施进度。

**目标：** 在不改变现有玩法与数值表现的前提下，为后续 Buff/技能系统预留最小抽象与接口。

**当前战斗系统架构：**
- 使用 `MultiBattleInstance` 处理多角色对多怪物的战斗
- 使用 `DungeonManager` 管理副本进度和波次切换
- 使用 `CharacterTracks` 管理每个角色的攻击和特殊技能轨道
- 使用 `EnemyTrack` 管理每个敌人的攻击轨道（**怪物攻击已使用 Track 系统**）
- 前端通过 `BattleDemo.razor` 组件与战斗系统交互
- `BattleInstance`（单体战斗）已被 `MultiBattleInstance`（多单位战斗）替代

**重要变更：**
- ⚠️ **怪物攻击逻辑已更新**：怪物攻击现在通过 `EnemyTrack.AttackTrack` 管理，使用与玩家类似的 Track 系统
- ⚠️ **不再使用 BattleInstance**：文档中涉及 `BattleInstance` 的部分需要更新为 `MultiBattleInstance`

**原则：**
- ✅ 保持当前 Attack 频率、Special 触发节奏与数值不变
- ✅ 所有触发统一走 SkillCast 管道（包括玩家和怪物攻击）
- ✅ 引入 Track 抽象（Legacy 适配器）
- ✅ 预留但不启用：施法、Buff、资源等功能

---

## 🎯 实施阶段

### 阶段 1：创建核心接口与数据结构

**状态：** ⬜ 未开始

**目标：** 建立 SkillCast 统一入口的基础接口

**任务清单：**

- [ ] 1.1 创建目录 `BlazorIdle.Shared/Game/Skills/`
- [ ] 1.2 创建 `SkillCastOptions.cs`
  ```csharp
  public sealed class SkillCastOptions
  {
      public bool ForceCrit { get; set; } = false;
      public string SourceTrack { get; set; } = "";  // "attack" | "special" | "enemy_attack" | "manual"
      public string? BundleId { get; set; }
  }
  ```
- [ ] 1.3 创建 `SkillCastResult.cs`
  ```csharp
  public sealed class SkillCastResult
  {
      public int DamageDealt { get; set; }
      public bool IsCrit { get; set; }
      public Dictionary<string, int> ResourceChanges { get; set; } = new();
      public List<string> BuffChanges { get; set; } = new();  // 预留
  }
  ```
- [ ] 1.4 创建 `ISkillResolver.cs`
  ```csharp
  public interface ISkillResolver
  {
      SkillCastResult Cast(string skillId, BattleContext ctx, SkillCastOptions? opts = null);
      IReadOnlyList<SkillCastResult> CastBundle(IReadOnlyList<string> skillIds, BattleContext ctx, SkillCastOptions opts);
  }
  ```
- [ ] 1.5 创建 `BattleContext.cs`
  ```csharp
  public sealed class BattleContext
  {
      public Character? Player { get; init; }  // 可选，用于玩家技能
      public Enemy? Enemy { get; init; }       // 可选，用于敌人技能
      public BattleTeam<Character>? PlayerTeam { get; init; }  // 多单位战斗的玩家队伍
      public BattleTeam<Enemy>? EnemyTeam { get; init; }       // 多单位战斗的敌人队伍
      public RngContext Rng { get; init; }
      public IGameClock Clock { get; init; }
      // 预留其他上下文字段
  }
  ```

**验收标准：**
- ✅ 所有接口文件编译通过
- ✅ 接口符合设计文档规范
- ✅ 命名空间为 `BlazorIdle.Game.Skills`

**完成时间：** _待填写_

**提交哈希：** _待填写_

---

### 阶段 2：创建 Track 抽象接口

**状态：** ⬜ 未开始

**目标：** 将"触发时机"与"效果处理"解耦

**任务清单：**

- [ ] 2.1 创建目录 `BlazorIdle.Shared/Game/Tracks/`（如果不存在）
- [ ] 2.2 创建 `ITrack.cs`
  ```csharp
  public interface ITrack
  {
      string Id { get; }                // "attack" | "special"
      bool IsSuspended { get; }         // 预留：施法/控制暂停
      void Suspend(string reason);      // 预留
      void Resume(string reason);       // 预留
      void Tick(double dt, BattleContext ctx);
  }
  ```
- [ ] 2.3 创建 `ProgressPolicy.cs`
  ```csharp
  public enum ProgressPolicy
  {
      Presence,   // 仅"有敌人时"增长（当前行为）
      Encounter   // 遭遇期间持续增长（预留）
  }
  ```
- [ ] 2.4 创建 `TrackConfig.cs`
  ```csharp
  public sealed class TrackConfig
  {
      public List<string> BoundSkills { get; set; } = new();
      public List<string> OnFireTriggers { get; set; } = new();
      public ProgressPolicy ProgressPolicy { get; set; } = ProgressPolicy.Presence;
  }
  ```

**验收标准：**
- ✅ ITrack 接口包含预留的暂停/恢复功能
- ✅ ProgressPolicy 支持两种模式
- ✅ 编译通过

**完成时间：** _待填写_

**提交哈希：** _待填写_

---

### 阶段 3：实现 Legacy Track 适配器

**状态：** ⬜ 未开始

**目标：** 封装现有触发逻辑，保持行为不变

**任务清单：**

- [ ] 3.1 创建 `AttackTrackLegacy.cs`（玩家攻击）
  - 实现 `ITrack` 接口
  - 内部持有 `TrackState` 实例（来自 `CharacterTracks.AttackTrack`）
  - 在 `Tick()` 中调用 `_trackState.CollectTriggers()`
  - 触发时调用 `skillResolver.CastBundle(["attack_basic"])`
  - 保持 APS 和 Haste 计算逻辑
  ```csharp
  public sealed class AttackTrackLegacy : ITrack
  {
      private readonly TrackState _trackState;
      private readonly ISkillResolver _skillResolver;
      private readonly TrackConfig _config;
      private bool _isSuspended = false;
      
      public string Id => "attack";
      public bool IsSuspended => _isSuspended;
      
      // 实现其他成员...
  }
  ```
- [ ] 3.2 创建 `SpecialTrackLegacy.cs`（玩家特殊技能）
  - 实现 `ITrack` 接口
  - 内部持有 `TrackState` 实例（来自 `CharacterTracks.SpecialTrack`）
  - 实现 `ShouldAdvance()` 双模式门控函数
  - 默认使用 `ProgressPolicy.Presence`
  - 触发时调用 `skillResolver.CastBundle(["special_pulse"])`
  ```csharp
  public sealed class SpecialTrackLegacy : ITrack
  {
      private readonly TrackState _trackState;
      private readonly ISkillResolver _skillResolver;
      private readonly TrackConfig _config;
      private bool _isSuspended = false;
      
      public string Id => "special";
      public bool IsSuspended => _isSuspended;
      
      private bool ShouldAdvance(BattleContext ctx)
      {
          bool encounterActive = /* 根据上下文判断 */;
          bool enemiesAlive = ctx.EnemyTeam.AliveCount > 0;  // 多单位战斗
          
          if (_config.ProgressPolicy == ProgressPolicy.Presence)
              return encounterActive && enemiesAlive && !_isSuspended;
          else
              return encounterActive && !_isSuspended;
      }
      
      // 实现其他成员...
  }
  ```
- [ ] 3.3 创建 `EnemyAttackTrackLegacy.cs`（怪物攻击）
  - 实现 `ITrack` 接口
  - 内部持有 `TrackState` 实例（来自 `EnemyTrack.AttackTrack`）
  - 在 `Tick()` 中调用 `_trackState.CollectTriggers()`
  - 触发时调用 `skillResolver.CastBundle(["enemy_attack_basic"])`
  - 保持现有的怪物攻击间隔计算逻辑
  ```csharp
  public sealed class EnemyAttackTrackLegacy : ITrack
  {
      private readonly TrackState _trackState;
      private readonly ISkillResolver _skillResolver;
      private readonly TrackConfig _config;
      private bool _isSuspended = false;
      
      public string Id => "enemy_attack";
      public bool IsSuspended => _isSuspended;
      
      // 实现其他成员...
  }
  ```

**验收标准：**
- ✅ Legacy Track 能够驱动技能触发（玩家和怪物）
- ✅ 数值与原逻辑完全一致
- ✅ Suspend/Resume 接口存在但暂不影响逻辑
- ✅ SpecialTrackLegacy 的门控函数正确实现
- ✅ EnemyAttackTrackLegacy 能正确处理怪物攻击

**完成时间：** _待填写_

**提交哈希：** _待填写_

---

### 阶段 4：实现 SkillResolver 基础版

**状态：** ⬜ 未开始

**目标：** 实现统一的技能效果处理器

**任务清单：**

- [ ] 4.1 创建 `SkillResolver.cs` 实现 `ISkillResolver`
- [ ] 4.2 实现 `Cast()` 方法
  - 处理 "attack_basic" 技能（玩家普攻）
  - 处理 "special_pulse" 技能（玩家特殊技能，可能是 AOE）
  - 处理 "enemy_attack_basic" 技能（怪物攻击）
  - 使用现有的伤害计算逻辑（DamagePerAttack, SpecialDamage, Enemy.DamagePerHit）
  - 保持暴击和浮动计算不变
  - 支持 AOE 技能（特殊技能可能打击所有敌人）
  - 返回 `SkillCastResult`
  ```csharp
  public SkillCastResult Cast(string skillId, BattleContext ctx, SkillCastOptions? opts = null)
  {
      opts ??= new SkillCastOptions();
      
      // 根据技能类型确定基础伤害
      int baseDamage = skillId switch
      {
          "attack_basic" => ctx.Player?.DamagePerAttack ?? 0,
          "special_pulse" => ctx.Player?.SpecialDamage ?? 0,
          "enemy_attack_basic" => ctx.Enemy?.DamagePerHit ?? 0,
          _ => 0
      };
      
      // 应用浮动
      double variancePct = skillId.StartsWith("enemy_") 
          ? ctx.Enemy?.VariancePct ?? 0.0
          : ctx.Player?.VariancePct ?? 0.0;
      double dmg = Math.Floor(ctx.Rng.Jitter(baseDamage, variancePct));
      if (dmg < 1) dmg = 1;
      
      // 检查暴击（仅玩家攻击有暴击）
      bool isCrit = false;
      if (!skillId.StartsWith("enemy_") && ctx.Player != null)
      {
          isCrit = opts.ForceCrit || ctx.Rng.NextDouble() < (ctx.Player.CritChancePercent / 100.0);
          if (isCrit)
          {
              dmg = Math.Floor(dmg * Math.Max(1.0, ctx.Player.CritMultiplier));
          }
      }
      
      return new SkillCastResult
      {
          DamageDealt = (int)dmg,
          IsCrit = isCrit
      };
  }
  ```
- [ ] 4.3 实现 `CastBundle()` 方法
  - 顺序施放技能列表
  - 主技能（索引0）必施放
  - 跟随技能独立检查（暂时跳过，因为 Step 0 没有随技能）
  - 防递归检测
  - 上限控制：maxPerTick=20
  - 为每个 bundle 生成唯一 bundleId
  ```csharp
  private int _castCounter = 0;
  private int _currentTickCasts = 0;
  private int _lastTickTime = 0;
  
  public IReadOnlyList<SkillCastResult> CastBundle(IReadOnlyList<string> skillIds, BattleContext ctx, SkillCastOptions opts)
  {
      // 防超限
      int nowMs = ctx.Clock.NowMs;
      if (nowMs != _lastTickTime)
      {
          _lastTickTime = nowMs;
          _currentTickCasts = 0;
      }
      
      var results = new List<SkillCastResult>();
      string bundleId = $"bundle_{nowMs}_{_castCounter++}";
      
      foreach (var skillId in skillIds)
      {
          if (_currentTickCasts >= 20) break;
          
          var optsWithBundle = new SkillCastOptions
          {
              ForceCrit = opts.ForceCrit,
              SourceTrack = opts.SourceTrack,
              BundleId = bundleId
          };
          
          var result = Cast(skillId, ctx, optsWithBundle);
          results.Add(result);
          _currentTickCasts++;
      }
      
      return results;
  }
  ```

**验收标准：**
- ✅ SkillResolver 能正确计算伤害
- ✅ 暴击率符合预期（统计测试）
- ✅ CastBundle 防递归和上限控制生效
- ✅ bundleId 唯一且正确关联

**完成时间：** _待填写_

**提交哈希：** _待填写_

---

### 阶段 5：创建 CastingController 占位

**状态：** ⬜ 未开始

**目标：** 预留施法系统接口，不启用

**任务清单：**

- [ ] 5.1 创建 `CastingController.cs`
  ```csharp
  public sealed class ActiveCast
  {
      public string SkillId { get; set; } = "";
      public double CastTimeSec { get; set; } = 0;
      public bool PauseAttackTrack { get; set; } = false;
      public bool PauseSpecialTrack { get; set; } = false;
      public double ElapsedSec { get; set; } = 0;
  }
  
  public sealed class CastingController
  {
      public bool IsCasting { get; private set; } = false;
      public ActiveCast? ActiveCast { get; private set; }
      
      public void Tick(double dt)
      {
          // 占位：空实现
          // 后续阶段会实现施法逻辑
      }
      
      public List<string> GetPausedTracks()
      {
          // 占位：始终返回空列表
          return new List<string>();
      }
  }
  ```
- [ ] 5.2 创建 `SkillDef.cs`
  ```csharp
  public sealed class SkillDef
  {
      public string Id { get; set; } = "";
      public double CastTimeSec { get; set; } = 0;  // 预留，Step 0 中为 0
      // 预留其他字段：Cost, Cooldown, Effects 等
  }
  ```

**验收标准：**
- ✅ CastingController 存在但不影响战斗流程
- ✅ Tick() 调用不产生副作用
- ✅ SkillDef 包含预留字段

**完成时间：** _待填写_

**提交哈希：** _待填写_

---

### 阶段 6：创建配置系统

**状态：** ⬜ 未开始

**目标：** 添加开关和配置项

**任务清单：**

- [ ] 6.1 创建 `CombatConfig.cs`
  ```csharp
  public sealed class CombatConfig
  {
      public bool UseChargeTracks { get; set; } = false;      // 后续切换充能条
      public bool EmitCastEvents { get; set; } = true;        // 便于调试
      public int MaxTriggersPerTick { get; set; } = 20;       // 防极端情况
  }
  ```
- [ ] 6.2 创建 `TrackConfigCollection.cs`
  ```csharp
  public sealed class TrackConfigCollection
  {
      public Dictionary<string, TrackConfig> Tracks { get; set; } = new()
      {
          ["attack"] = new TrackConfig
          {
              BoundSkills = new List<string> { "attack_basic" },
              OnFireTriggers = new List<string>(),
              ProgressPolicy = ProgressPolicy.Presence
          },
          ["special"] = new TrackConfig
          {
              BoundSkills = new List<string> { "special_pulse" },
              OnFireTriggers = new List<string>(),
              ProgressPolicy = ProgressPolicy.Presence
          },
          ["enemy_attack"] = new TrackConfig
          {
              BoundSkills = new List<string> { "enemy_attack_basic" },
              OnFireTriggers = new List<string>(),
              ProgressPolicy = ProgressPolicy.Presence
          }
      };
  }
  ```
- [ ] 6.3 创建 `SkillDefCollection.cs`
  ```csharp
  public sealed class SkillDefCollection
  {
      public Dictionary<string, SkillDef> Skills { get; set; } = new()
      {
          ["attack_basic"] = new SkillDef
          {
              Id = "attack_basic",
              CastTimeSec = 0
          },
          ["special_pulse"] = new SkillDef
          {
              Id = "special_pulse",
              CastTimeSec = 0,
              IsAoe = true  // 特殊技能可能是 AOE
          },
          ["enemy_attack_basic"] = new SkillDef
          {
              Id = "enemy_attack_basic",
              CastTimeSec = 0
          }
      };
  }
  ```
- [ ] 6.4 添加 JSON 序列化支持（可选）

**验收标准：**
- ✅ 配置类可序列化为 JSON
- ✅ 配置符合设计文档示例
- ✅ 所有开关默认值正确

**完成时间：** _待填写_

**提交哈希：** _待填写_

---

### 阶段 7：重构 MultiBattleInstance 集成

**状态：** ⬜ 未开始

**目标：** 将 MultiBattleInstance 和 DungeonManager 改为使用新架构

**背景说明：**
- 当前实际使用的是 `MultiBattleInstance` （多单位战斗）和 `DungeonManager` （副本管理器），已不再使用单体 `BattleInstance`
- 前端通过 `BattleDemo.razor` 组件与战斗系统交互
- `MultiBattleInstance` 支持多角色对多怪物的战斗，使用 `CharacterTracks` 和 `EnemyTrack` 管理各单位的战斗轨道
- 怪物攻击也已经使用了 Track 系统（`EnemyTrack` 包含 `AttackTrack`），通过 `ProcessEnemyActions()` 方法处理

**任务清单：**

- [ ] 7.1 在 `MultiBattleInstance` 中集成新组件
  ```csharp
  private readonly BattleContext _battleContext;
  private readonly ISkillResolver _skillResolver;
  private readonly Dictionary<string, AttackTrackLegacy> _attackTracksLegacy = new();
  private readonly Dictionary<string, SpecialTrackLegacy> _specialTracksLegacy = new();
  private readonly Dictionary<string, EnemyAttackTrackLegacy> _enemyAttackTracksLegacy = new();
  private readonly CastingController _castingController;
  private readonly CombatConfig _combatConfig;
  
  // 在构造函数中初始化，为每个角色和敌人创建 Legacy Track
  public MultiBattleInstance(...)
  {
      // 原有初始化...
      
      _combatConfig = new CombatConfig();
      _skillResolver = new SkillResolver();
      _castingController = new CastingController();
      
      // 为每个角色创建 Legacy Track
      foreach (var member in _playerTeam.Members)
      {
          var battleContext = new BattleContext
          {
              Player = member.Entity,
              Rng = _rng,
              Clock = _clock
          };
          
          var attackConfig = new TrackConfig
          {
              BoundSkills = new List<string> { "attack_basic" },
              ProgressPolicy = ProgressPolicy.Presence
          };
          _attackTracksLegacy[member.Id] = new AttackTrackLegacy(
              _characterTracks[member.Id].AttackTrack, 
              _skillResolver, 
              attackConfig
          );
          
          var specialConfig = new TrackConfig
          {
              BoundSkills = new List<string> { "special_pulse" },
              ProgressPolicy = ProgressPolicy.Presence
          };
          _specialTracksLegacy[member.Id] = new SpecialTrackLegacy(
              _characterTracks[member.Id].SpecialTrack,
              _skillResolver,
              specialConfig
          );
      }
      
      // 为每个敌人创建 Legacy Track
      foreach (var member in _enemyTeam.Members)
      {
          var enemyBattleContext = new BattleContext
          {
              Enemy = member.Entity,
              Rng = _rng,
              Clock = _clock
          };
          
          var enemyAttackConfig = new TrackConfig
          {
              BoundSkills = new List<string> { "enemy_attack_basic" },
              ProgressPolicy = ProgressPolicy.Presence
          };
          _enemyAttackTracksLegacy[member.Id] = new EnemyAttackTrackLegacy(
              _enemyTracks[member.Id].AttackTrack,
              _skillResolver,
              enemyAttackConfig
          );
      }
  }
  ```
- [ ] 7.2 修改 `ProcessCharacterActions()` 方法使用新架构
  ```csharp
  private void ProcessCharacterActions(int now)
  {
      var aliveCharIds = _playerTeam.GetAliveMemberIds();
      
      foreach (var charId in aliveCharIds)
      {
          if (!_characterTracks.TryGetValue(charId, out var tracks))
              continue;
          
          if (!tracks.IsEnabled)
              continue;
          
          var character = tracks.Character;
          
          // 新架构：先调用 CastingController（占位，无操作）
          double dt = (now - _lastTickTime) / 1000.0;
          _castingController.Tick(dt);
          
          // 使用新 Track 触发攻击
          if (_attackTracksLegacy.TryGetValue(charId, out var attackTrack))
          {
              var battleContext = new BattleContext
              {
                  Player = character,
                  Rng = _rng,
                  Clock = _clock
              };
              attackTrack.Tick(dt, battleContext);
          }
          
          // 使用新 Track 触发特殊技能
          if (_specialTracksLegacy.TryGetValue(charId, out var specialTrack))
          {
              var battleContext = new BattleContext
              {
                  Player = character,
                  Rng = _rng,
                  Clock = _clock
              };
              specialTrack.Tick(dt, battleContext);
          }
      }
  }
  ```
- [ ] 7.3 修改 `ProcessEnemyActions()` 方法使用新架构
  ```csharp
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
          
          // 使用新 Track 触发敌人攻击
          if (_enemyAttackTracksLegacy.TryGetValue(enemyId, out var enemyAttackTrack))
          {
              double dt = (now - _lastTickTime) / 1000.0;
              var battleContext = new BattleContext
              {
                  Enemy = enemy,
                  Rng = _rng,
                  Clock = _clock
              };
              enemyAttackTrack.Tick(dt, battleContext);
          }
      }
  }
  ```
- [ ] 7.4 修改 SkillResolver 以支持多单位战斗
  - 添加目标选择机制（使用现有的 `SelectEnemyTarget` 和 `SelectPlayerTarget`）
  - 支持 AOE 技能（特殊技能可能是 AOE）
  - 添加伤害应用回调机制，或让 SkillResolver 返回结果由 Track 调用战斗实例的伤害应用方法

**验收标准：**
- ✅ MultiBattleInstance 编译通过
- ✅ 运行时逻辑与改造前完全一致
- ✅ 所有现有测试通过
- ✅ 战斗数值无变化
- ✅ 怪物攻击使用新的战斗逻辑（通过 EnemyAttackTrackLegacy）

**完成时间：** _待填写_

**提交哈希：** _待填写_

**⚠️ 注意事项：**
- 这个阶段最复杂，需要仔细验证
- 建议先在测试环境验证正确性
- 保留旧代码注释以便对比
- 注意 `DungeonManager` 通过 `MultiBattleInstance` 进行战斗管理，需要确保兼容性
- 前端 `BattleDemo.razor` 组件依赖这些改动，需要验证 UI 交互正常

---

### 阶段 8：增强事件记录系统

**状态：** ⬜ 未开始

**目标：** 扩展 Segment 支持新事件类型

**任务清单：**

- [ ] 8.1 扩展 `CombatEvent` 类
  ```csharp
  public class CombatEvent
  {
      // 现有字段...
      public ActorType Attacker { get; init; }
      public ActorType Defender { get; init; }
      public EventSource Source { get; init; }
      public int TimeMs { get; init; }
      public int Damage { get; init; }
      public bool Crit { get; init; }
      public int RngIndexAfter { get; init; }
      public int DefenderHpAfter { get; init; }
      
      // 新增字段
      public string? SkillId { get; init; }       // "attack_basic" | "special_pulse"
      public string? BundleId { get; init; }      // 关联同时施放的技能
  }
  ```
- [ ] 8.2 修改 `ApplyDamageToEnemy` 方法以包含新字段
  ```csharp
  private void ApplyDamageToEnemy(int dmg, EventSource src, string? skillId, string? bundleId)
  {
      _enemy.Hp = Math.Max(0, _enemy.Hp - dmg);
      _totalDamage += dmg;
      
      var ev = new CombatEvent
      {
          Attacker = ActorType.Player,
          Defender = ActorType.Enemy,
          Source = src,
          TimeMs = _clock.NowMs,
          Damage = dmg,
          Crit = false,  // 需要从 SkillCastResult 传递
          RngIndexAfter = _rng.Index,
          DefenderHpAfter = _enemy.Hp,
          SkillId = skillId,
          BundleId = bundleId
      };
      
      // 根据配置决定是否记录详细事件
      if (_combatConfig.EmitCastEvents)
      {
          var flushed = _aggregator.AddEvent(ev);
          if (flushed != null) _segments.Add(flushed);
      }
      
      CombatEventFired?.Invoke(ev);
  }
  ```
- [ ] 8.3 预留事件类型扩展
  ```csharp
  public enum CombatEventType
  {
      Damage,          // 现有
      CastStart,       // 预留：施法开始
      CastComplete,    // 预留：施法完成
      CastInterrupted  // 预留：施法打断
  }
  ```

**验收标准：**
- ✅ Segment 中能看到 skillId 和 bundleId
- ✅ EmitCastEvents 开关生效
- ✅ 事件类型枚举包含预留值

**完成时间：** _待填写_

**提交哈希：** _待填写_

---

### 阶段 9：添加回滚开关

**状态：** ⬜ 未开始

**目标：** 确保可以快速回退到旧代码

**任务清单：**

- [ ] 9.1 在 `CombatConfig` 中添加回滚开关
  ```csharp
  public sealed class CombatConfig
  {
      public bool UseLegacyPath { get; set; } = false;  // true = 使用旧代码
      public bool UseChargeTracks { get; set; } = false;
      public bool EmitCastEvents { get; set; } = true;
      public int MaxTriggersPerTick { get; set; } = 20;
  }
  ```
- [ ] 9.2 在 `MultiBattleInstance.AdvanceTick()` 中添加分支
  ```csharp
  public void AdvanceTick(int tickMs)
  {
      if (!_running) return;
      
      _clock.AdvanceBy(tickMs);
      int now = _clock.NowMs;
      
      // 冷却逻辑...
      
      if (_state == MultiBattleState.Fighting)
      {
          if (_combatConfig.UseLegacyPath)
          {
              // 旧路径：直接使用 TrackState
              ProcessCharacterActionsLegacy(now);
              ProcessEnemyActionsLegacy(now);
          }
          else
          {
              // 新路径：使用 Track + SkillResolver
              ProcessCharacterActionsNew(now);
              ProcessEnemyActionsNew(now);
          }
      }
      
      // 检查战斗状态
      CheckBattleState(now);
      
      // 处理段落聚合
      var seg = _aggregator.Tick(now, _rng.Index);
      if (seg != null) _segments.Add(seg);
  }
  
  private void ProcessCharacterActionsLegacy(int now)
  {
      // 复制原有的触发逻辑
      var aliveCharIds = _playerTeam.GetAliveMemberIds();
      
      foreach (var charId in aliveCharIds)
      {
          if (!_characterTracks.TryGetValue(charId, out var tracks))
              continue;
          
          if (!tracks.IsEnabled)
              continue;
          
          var character = tracks.Character;
          
          // 处理普通攻击
          var atkCount = tracks.AttackTrack.CollectTriggers(now);
          for (int i = 0; i < atkCount; i++)
          {
              ProcessCharacterAttack(charId, character);
          }
          
          // 处理特殊技能
          var spCount = tracks.SpecialTrack.CollectTriggers(now);
          for (int i = 0; i < spCount; i++)
          {
              ProcessCharacterSpecial(charId, character);
          }
      }
  }
  
  private void ProcessEnemyActionsLegacy(int now)
  {
      // 复制原有的敌人攻击逻辑
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
              ProcessEnemyAttack(enemyId, enemy);
          }
      }
  }
  
  private void ProcessCharacterActionsNew(int now)
  {
      // 新架构逻辑：使用 Legacy Track + SkillResolver
      var aliveCharIds = _playerTeam.GetAliveMemberIds();
      double dt = (now - _lastTickTime) / 1000.0;
      
      foreach (var charId in aliveCharIds)
      {
          if (!_characterTracks.TryGetValue(charId, out var tracks))
              continue;
          
          if (!tracks.IsEnabled)
              continue;
          
          _castingController.Tick(dt);
          
          if (_attackTracksLegacy.TryGetValue(charId, out var attackTrack))
          {
              var battleContext = CreateBattleContext(tracks.Character);
              attackTrack.Tick(dt, battleContext);
          }
          
          if (_specialTracksLegacy.TryGetValue(charId, out var specialTrack))
          {
              var battleContext = CreateBattleContext(tracks.Character);
              specialTrack.Tick(dt, battleContext);
          }
      }
  }
  
  private void ProcessEnemyActionsNew(int now)
  {
      // 新架构逻辑：使用 EnemyAttackTrackLegacy + SkillResolver
      var aliveEnemyIds = _enemyTeam.GetAliveMemberIds();
      double dt = (now - _lastTickTime) / 1000.0;
      
      foreach (var enemyId in aliveEnemyIds)
      {
          if (!_enemyTracks.TryGetValue(enemyId, out var track))
              continue;
          
          if (!track.IsEnabled)
              continue;
          
          if (_enemyAttackTracksLegacy.TryGetValue(enemyId, out var enemyAttackTrack))
          {
              var battleContext = CreateBattleContext(track.Enemy);
              enemyAttackTrack.Tick(dt, battleContext);
          }
      }
  }
  
  // 辅助方法：创建战斗上下文
  private BattleContext CreateBattleContext(Character character)
  {
      return new BattleContext
      {
          Player = character,
          PlayerTeam = _playerTeam,
          EnemyTeam = _enemyTeam,
          Rng = _rng,
          Clock = _clock
      };
  }
  
  private BattleContext CreateBattleContext(Enemy enemy)
  {
      return new BattleContext
      {
          Enemy = enemy,
          PlayerTeam = _playerTeam,
          EnemyTeam = _enemyTeam,
          Rng = _rng,
          Clock = _clock
      };
  }
  ```
- [ ] 9.3 添加单元测试验证两条路径结果一致

**验收标准：**
- ✅ 切换开关后，战斗数值完全不变
- ✅ 两条路径的测试结果一致
- ✅ 回滚路径与原始代码完全相同

**完成时间：** _待填写_

**提交哈希：** _待填写_

---

### 阶段 10：验收测试

**状态：** ⬜ 未开始

**目标：** 确保改造后与改造前完全等价

**任务清单：**

- [ ] 10.1 创建 `SkillResolverTests.cs`
  ```csharp
  [Fact]
  public void Cast_AttackBasic_CalculatesDamageCorrectly()
  {
      // 测试基础伤害计算
  }
  
  [Fact]
  public void Cast_WithCrit_AppliesCritMultiplier()
  {
      // 测试暴击伤害
  }
  
  [Fact]
  public void CastBundle_PreventsRecursion()
  {
      // 测试防递归
  }
  
  [Fact]
  public void CastBundle_EnforcesMaxTriggers()
  {
      // 测试上限控制
  }
  ```
- [ ] 10.2 创建 `TrackLegacyTests.cs`
  ```csharp
  [Fact]
  public void AttackTrackLegacy_TriggersAtCorrectFrequency()
  {
      // 测试触发频率
  }
  
  [Fact]
  public void AttackTrackLegacy_AppliesHasteCorrectly()
  {
      // 测试急速影响
  }
  
  [Fact]
  public void SpecialTrackLegacy_PresenceMode_StopsWhenNoEnemy()
  {
      // 测试 presence 模式
  }
  ```
- [ ] 10.3 创建 `MultiBattleInstanceIntegrationTests.cs`
  ```csharp
  [Fact]
  public void MultiBattle_LegacyVsNew_ProducesSameResults()
  {
      // A/B 对比测试
      // 运行 1000 tick，对比两条路径
      var legacyBattle = CreateMultiBattle(useLegacy: true);
      var newBattle = CreateMultiBattle(useLegacy: false);
      
      for (int i = 0; i < 1000; i++)
      {
          legacyBattle.AdvanceTick(100);
          newBattle.AdvanceTick(100);
      }
      
      var legacyDigest = legacyBattle.BuildDigest();
      var newDigest = newBattle.BuildDigest();
      
      // 验证总伤害
      Assert.InRange(newDigest.TotalDamage, 
                     legacyDigest.TotalDamage * 0.99, 
                     legacyDigest.TotalDamage * 1.01);
      
      // 验证 DPS
      double legacyDps = legacyDigest.TotalDamage / (legacyDigest.DurationMs / 1000.0);
      double newDps = newDigest.TotalDamage / (newDigest.DurationMs / 1000.0);
      Assert.InRange(newDps, legacyDps * 0.99, legacyDps * 1.01);
  }
  
  [Fact]
  public void EnemyAttack_LegacyVsNew_ProducesSameResults()
  {
      // 专门测试怪物攻击逻辑是否一致
      var legacyBattle = CreateMultiBattle(useLegacy: true);
      var newBattle = CreateMultiBattle(useLegacy: false);
      
      for (int i = 0; i < 500; i++)
      {
          legacyBattle.AdvanceTick(100);
          newBattle.AdvanceTick(100);
      }
      
      var legacySnapshot = legacyBattle.GetSnapshot();
      var newSnapshot = newBattle.GetSnapshot();
      
      // 验证玩家受到的总伤害一致
      Assert.InRange(newSnapshot.TotalEnemyDamage,
                     legacySnapshot.TotalEnemyDamage * 0.99,
                     legacySnapshot.TotalEnemyDamage * 1.01);
  }
  
  [Fact]
  public void MultiCharacter_CoordinatedAttacks_ProduceSameResults()
  {
      // 测试多角色协同攻击
      var playerTeam = CreatePlayerTeam(characterCount: 3);
      var enemyTeam = CreateEnemyTeam(enemyCount: 2);
      
      var legacyBattle = CreateMultiBattle(playerTeam, enemyTeam, useLegacy: true);
      var newBattle = CreateMultiBattle(playerTeam, enemyTeam, useLegacy: false);
      
      for (int i = 0; i < 1000; i++)
      {
          legacyBattle.AdvanceTick(100);
          newBattle.AdvanceTick(100);
      }
      
      var legacySnapshot = legacyBattle.GetSnapshot();
      var newSnapshot = newBattle.GetSnapshot();
      
      // 验证多角色总输出一致
      Assert.InRange(newSnapshot.TotalPlayerDamage,
                     legacySnapshot.TotalPlayerDamage * 0.99,
                     legacySnapshot.TotalPlayerDamage * 1.01);
  }
  
  [Fact]
  public void AoeSpecial_MultipleEnemies_DamageConsistency()
  {
      // 测试 AOE 技能对多个敌人的伤害一致性
      var playerTeam = CreatePlayerTeam(characterCount: 1);
      var enemyTeam = CreateEnemyTeam(enemyCount: 5);
      
      var legacyBattle = CreateMultiBattle(playerTeam, enemyTeam, useLegacy: true);
      var newBattle = CreateMultiBattle(playerTeam, enemyTeam, useLegacy: false);
      
      // 触发特殊技能
      for (int i = 0; i < 500; i++)
      {
          legacyBattle.AdvanceTick(100);
          newBattle.AdvanceTick(100);
      }
      
      var legacySnapshot = legacyBattle.GetSnapshot();
      var newSnapshot = newBattle.GetSnapshot();
      
      // 验证 AOE 伤害分配一致
      Assert.InRange(newSnapshot.TotalPlayerDamage,
                     legacySnapshot.TotalPlayerDamage * 0.99,
                     legacySnapshot.TotalPlayerDamage * 1.01);
  }
  
  [Fact]
  public void TargetSelection_DifferentStrategies_ConsistentBehavior()
  {
      // 测试不同目标选择策略的一致性
      var strategies = new[] 
      { 
          TargetStrategy.Random,
          TargetStrategy.LowestHp,
          TargetStrategy.LowestHpPercent,
          TargetStrategy.HighestHp
      };
      
      foreach (var strategy in strategies)
      {
          var config = new MultiBattleConfig { PlayerTargetStrategy = strategy };
          var legacyBattle = CreateMultiBattle(config, useLegacy: true);
          var newBattle = CreateMultiBattle(config, useLegacy: false);
          
          // 设置相同的随机种子确保可比性
          for (int i = 0; i < 300; i++)
          {
              legacyBattle.AdvanceTick(100);
              newBattle.AdvanceTick(100);
          }
          
          var legacySnapshot = legacyBattle.GetSnapshot();
          var newSnapshot = newBattle.GetSnapshot();
          
          // 验证目标选择行为一致
          Assert.Equal(legacySnapshot.State, newSnapshot.State);
      }
  }
  ```
- [ ] 10.4 创建性能测试
  ```csharp
  [Fact]
  public void Performance_1000Ticks_CompletesInTime()
  {
      var battle = CreateBattle(useLegacy: false);
      var sw = Stopwatch.StartNew();
      
      for (int i = 0; i < 1000; i++)
      {
          battle.AdvanceTick(100);
      }
      
      sw.Stop();
      Assert.True(sw.ElapsedMilliseconds < 50, 
                  $"Expected < 50ms, actual: {sw.ElapsedMilliseconds}ms");
  }
  ```
- [ ] 10.5 运行所有测试并记录结果

**验收标准：**
- ✅ 所有单元测试通过
- ✅ A/B 对比测试：总伤害误差 < 1%
- ✅ DPS 误差 < 1%
- ✅ 触发次数一致
- ✅ 性能测试：1000 tick < 50ms
- ✅ 暴击分布统计学等价

**完成时间：** _待填写_

**提交哈希：** _待填写_

---

## 📋 最终清理阶段

**状态：** ⬜ 未开始

**前置条件：** 阶段 1-10 全部完成且验收通过

**任务清单：**

- [ ] 11.1 移除回滚开关（`UseLegacyPath`）
- [ ] 11.2 删除 `AdvanceTickLegacy()` 方法
- [ ] 11.3 删除旧的直接触发逻辑代码
- [ ] 11.4 清理注释掉的旧代码
- [ ] 11.5 更新相关文档
  - [ ] 更新 `docs/buff与技能设计/docs_step0_第0步-设计方案.md`，标记为"已完成"
  - [ ] 在文档中添加"实施总结"部分
- [ ] 11.6 最终代码审查
  - [ ] 确保没有死代码
  - [ ] 确保命名一致
  - [ ] 确保注释准确
- [ ] 11.7 运行完整测试套件
- [ ] 11.8 提交最终版本

**验收标准：**
- ✅ 代码库中无旧逻辑残留
- ✅ 所有测试通过
- ✅ 代码整洁可读
- ✅ 文档已更新

**完成时间：** _待填写_

**提交哈希：** _待填写_

---

## 📊 进度总览

| 阶段 | 状态 | 完成时间 | 提交哈希 |
|------|------|----------|----------|
| 阶段 1 - 核心接口 | ⬜ 未开始 | - | - |
| 阶段 2 - Track 抽象 | ⬜ 未开始 | - | - |
| 阶段 3 - Legacy 适配器 | ⬜ 未开始 | - | - |
| 阶段 4 - SkillResolver | ⬜ 未开始 | - | - |
| 阶段 5 - CastingController | ⬜ 未开始 | - | - |
| 阶段 6 - 配置系统 | ⬜ 未开始 | - | - |
| 阶段 7 - MultiBattleInstance 集成 | ⬜ 未开始 | - | - |
| 阶段 8 - 事件系统 | ⬜ 未开始 | - | - |
| 阶段 9 - 回滚开关 | ⬜ 未开始 | - | - |
| 阶段 10 - 验收测试 | ⬜ 未开始 | - | - |
| 最终清理 | ⬜ 未开始 | - | - |

**总体进度：** 0/11 (0%)

---

## 🎯 验收指标汇总

### 功能验收
- [ ] 普攻频率：与改造前误差 < 0.1%
- [ ] Special触发：与改造前误差 < 0.1%
- [ ] 暴击分布：统计学等价
- [ ] DPS计算：理论值与实测值匹配

### 可观测性验收
- [ ] Segment 包含 SkillCastEvent
- [ ] 事件携带 bundleId 和 skillId
- [ ] EmitCastEvents 开关可控

### 性能验收
- [ ] 1000 tick < 50ms
- [ ] maxTriggersPerTick 防护生效
- [ ] 无死循环或性能回退

### 架构验收
- [ ] ITrack 与 ISkillResolver 接口清晰
- [ ] Legacy 适配器完整封装旧逻辑
- [ ] CastingController 占位就绪
- [ ] 配置系统支持后续扩展
- [ ] 代码整洁可读，无死代码

---

## 📝 使用说明

### 如何更新进度

1. **标记任务完成：** 将任务前的 `[ ]` 改为 `[x]`
2. **更新阶段状态：** 
   - ⬜ 未开始
   - 🔄 进行中
   - ✅ 已完成
3. **填写完成信息：** 在对应阶段填写完成时间和提交哈希
4. **更新进度表：** 更新底部的进度总览表格

### 示例

完成阶段 1 后：
```markdown
### 阶段 1：创建核心接口与数据结构

**状态：** ✅ 已完成

**任务清单：**
- [x] 1.1 创建目录 `BlazorIdle.Shared/Game/Skills/`
- [x] 1.2 创建 `SkillCastOptions.cs`
- [x] 1.3 创建 `SkillCastResult.cs`
- [x] 1.4 创建 `ISkillResolver.cs`
- [x] 1.5 创建 `BattleContext.cs`

**完成时间：** 2025-01-15 14:30

**提交哈希：** `a1b2c3d`
```

---

## 🔗 相关文档

- [Step 0 设计方案](./docs_step0_第0步-设计方案.md)
- [战斗系统重构设计](../战斗系统重构设计/)
- [角色属性设计](../角色属性设计/)

## 📌 实际实现说明

### 与设计文档的关系

**重要说明：**
- 本实施进度追踪文档基于 [Step 0 设计方案](./docs_step0_第0步-设计方案.md) 编写
- 设计方案中的示例基于**单体战斗场景**（`BattleInstance`），主要用于说明核心设计思路
- 实际实现已扩展为**多单位战斗场景**（`MultiBattleInstance` + `DungeonManager`）
- **核心设计思路保持不变**：统一 SkillCast 管道、Track 抽象、Legacy 适配器等
- **实施差异**：需要为每个角色和敌人创建独立的 Track 实例，而非单个全局 Track

### 战斗系统架构

**当前使用的组件：**

1. **MultiBattleInstance** (`BlazorIdle.Shared/Game/MultiBattleInstance.cs`)
   - 负责多角色对多怪物的战斗逻辑
   - 管理角色和敌人的战斗轨道（CharacterTracks 和 EnemyTrack）
   - 处理攻击、特殊技能、目标选择、伤害计算等
   - 支持 AOE 技能（特殊技能可以打击所有存活敌人）

2. **DungeonManager** (`BlazorIdle.Shared/Game/DungeonManager.cs`)
   - 管理副本的进度、波次切换和战斗循环
   - 创建并管理 MultiBattleInstance 实例
   - 处理副本奖励、经验获得和掉落物

3. **CharacterTracks** (`BlazorIdle.Shared/Game/CharacterTracks.cs`)
   - 管理单个角色的攻击轨道（AttackTrack）和特殊技能轨道（SpecialTrack）
   - 使用 TrackState 跟踪触发时机

4. **EnemyTrack** (`BlazorIdle.Shared/Game/CharacterTracks.cs`)
   - 管理单个敌人的攻击轨道（AttackTrack）
   - 使用 TrackState 跟踪触发时机
   - **怪物攻击已经使用 Track 系统**，通过 `ProcessEnemyActions()` 方法处理

5. **BattleDemo.razor** (`BlazorIdle/Components/BattleDemo.razor`)
   - 前端战斗演示组件
   - 支持普通战斗和副本战斗两种模式
   - 通过 DungeonManager 和 MultiBattleInstance 进行战斗

**已废弃的组件：**
- **BattleInstance** - 单体战斗实例，已被 MultiBattleInstance 替代

### 怪物攻击逻辑

怪物攻击已经使用了新的战斗逻辑：

```csharp
// MultiBattleInstance.cs - ProcessEnemyActions()
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
        
        // 使用 TrackState 收集触发次数
        var count = track.AttackTrack.CollectTriggers(now);
        for (int i = 0; i < count; i++)
        {
            ProcessEnemyAttack(enemyId, enemy);
        }
    }
}
```

怪物的攻击间隔通过 `EnemyTrack` 管理：

```csharp
// CharacterTracks.cs - EnemyTrack 构造函数
public EnemyTrack(string enemyId, Enemy enemy)
{
    EnemyId = enemyId;
    Enemy = enemy;
    
    // 根据怪物的 AttackIntervalSec 创建攻击轨道
    var attackIntervalMs = Math.Max(100.0, enemy.AttackIntervalSec * 1000.0);
    AttackTrack = new TrackState(TrackType.EnemyAttack, attackIntervalMs);
    
    IsEnabled = true;
}
```

---

**最后更新：** 2025-11-10
**维护者：** @copilot
