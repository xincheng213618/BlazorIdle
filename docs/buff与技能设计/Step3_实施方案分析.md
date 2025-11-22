# Step3 定期技能检查系统 - 实施方案分析

**文档版本：** v1.0  
**创建日期：** 2025-11-22  
**状态：** 设计分析阶段

---

## 一、任务理解

根据问题描述，本次任务需要：

1. **分析当前项目进度**：充分理解 `Step2_实施进度追踪.md` 中的已完成内容
2. **分析设计文档**：深入理解 `Step3_定期技能检查系统设计.md` 的设计意图
3. **设计实施方案**：重点关注以下触发条件系统：
   - ✅ **战斗开始时触发**：光环效果（玩家死亡后清除，复活后重新触发）
   - ✅ **血量触发**：如 HP < 30% 触发保命技能
   - ✅ **怪物数量触发**：如敌人 >= 3 触发 AOE
   - ✅ **其他战斗条件**：队友数量、Buff 状态等

**重要说明**：用户要求**先设计，不急于修改代码**，需要先确认方案。

---

## 二、当前项目进度分析

### 2.1 已完成的功能（基于 Step2_实施进度追踪.md）

#### ✅ Phase 1-5: 技能系统基础设施
- **Phase 1**: 技能配置基础设施（44个技能定义，skills.json + monsterskills.json）
- **Phase 2**: 技能槽位系统（固定技能 + 可配置技能）
- **Phase 2.5**: 技能学习与装备系统（持久化管理）
- **Phase 3**: 目标选择系统（5种策略：CurrentTarget, EnemiesAll, AlliesLowestHpPct, Self, AlliesAll）
- **Phase 3+**: 怪物技能系统基础整合（P3 模式）
- **Phase 4**: 技能条件判定系统（HP/Buff/资源条件）
- **Phase 5**: 资源消耗与冷却系统（CooldownManager + ResourceManager）

#### ✅ Phase 6-8: 技能触发和施法系统
- **Phase 6**: Window-GCD 机制（3个窗口：PreAttack, PostAttack, PostCast）
- **Phase 7**: 触发类技能系统（4种触发时机 + 概率触发 + 条件覆盖）
- **Phase 8**: 施法技能集成（CastingController + Track 暂停机制 + 施法条 UI）

#### ✅ Phase 9-10: AutoCastEngine 和 UI 系统
- **Phase 9**: AutoCastEngine 核心框架（统一技能调度）
- **Phase 9+**: 怪物技能系统完整实施（施法技能支持）
- **Phase 9.5**: 职业固定技能差异化（4职业特色系统）
- **Phase 10**: UI 系统（技能学习面板 + 战斗技能图标 + 冷却显示）

#### ✅ 重要优化和修复
- Per-Character 冷却管理架构重构（解决多角色冷却混淆）
- BattleDemo 动态面板创建重构
- 副本战斗装备技能修复
- 技能资源检测功能
- 玩家/怪物施法时机修复

### 2.2 当前测试覆盖
```
✅ 644 个测试全部通过
✅ 零破坏性变更
✅ 代码质量：A/A+ 级
```

### 2.3 现有触发系统能力

从 **Phase 7** 的实施来看，当前 `TriggerProcessor` 已支持：

| 触发时机 | 说明 | 已实现 |
|---------|------|-------|
| `OnAttackHit` | 普攻命中时触发 | ✅ |
| `OnAttackCrit` | 普攻暴击时触发 | ✅ |
| `OnPostAttackWindow` | PostAttack 窗口触发 | ✅ |
| `OnPostCastWindow` | PostCast 窗口触发 | ✅ |

**已实现的触发特性：**
- ✅ 概率触发（`ProcChance` 0.0-1.0）
- ✅ 优先级排序（`Priority` 字段）
- ✅ 条件覆盖（`trigger.Conditions ?? skill.Conditions`）
- ✅ 强制触发（`IgnoreRequirements = true`）
- ✅ 触发安全机制（MaxTriggersPerWindow=5, MaxRecursionDepth=3）

### 2.4 现有条件检查能力

从 **Phase 4** 的实施来看，当前 `ConditionChecker` 已支持：

| 条件类型 | 说明 | 已实现 |
|---------|------|-------|
| `HpBelowPct` | HP 百分比低于阈值 | ✅ |
| `HpAbovePct` | HP 百分比高于阈值 | ✅ |
| `RequireBuffId` | 必须拥有指定 Buff | ✅ |
| `ForbidBuffId` | 禁止拥有指定 Buff | ✅ |
| `RequireResource` | 资源数量要求 | ✅ |
| `RequireBuffStacks` | Buff 层数要求 | ✅ |

**重要特性：**
- ✅ 支持玩家和怪物统一条件检查
- ✅ 条件 AND 逻辑（所有条件必须同时满足）
- ✅ 防御式设计（空值检查、边界情况处理）

---

## 三、Step3 核心需求分析

### 3.1 需求优先级评估

根据用户描述和设计文档，我将需求分为以下优先级：

| 优先级 | 需求 | 说明 | 现有能力 |
|-------|------|------|---------|
| ⭐⭐⭐ **P0** | 光环效果系统 | 战斗开始触发，死亡清除，复活重新触发 | ❌ 缺失 |
| ⭐⭐⭐ **P0** | 血量触发条件 | HP 阈值触发技能 | ✅ 部分支持 |
| ⭐⭐ **P1** | 怪物数量触发 | 根据敌人/队友数量触发 | ❌ 缺失 |
| ⭐⭐ **P1** | 定期检查机制 | 每秒检查条件触发（如自动喝药） | ❌ 缺失 |
| ⭐ **P2** | 消耗品系统 | 自动使用药水/食物 | ❌ 缺失 |

### 3.2 光环效果系统（P0 - 最高优先级）

#### 需求描述
- **战斗开始时自动激活**：如战士的"力量光环"在战斗开始立即生效
- **玩家死亡后清除**：死亡时清除所有 Buff（包括光环）
- **玩家复活后重新触发**：复活时光环自动重新激活

#### 当前能力评估
| 功能 | 现状 | 缺失 |
|------|------|------|
| 战斗开始触发点 | ❌ | `OnBattleStart` 触发时机未实现 |
| Buff 系统 | ✅ | 已完善支持 |
| 死亡清除 Buff | ❌ | 玩家死亡机制未实现 |
| 复活机制 | ❌ | 玩家复活机制未实现 |

#### 设计方案

**方案 A：扩展现有 TriggerProcessor（推荐）**

新增触发时机枚举：
```csharp
public enum TriggerWhen
{
    // 现有触发时机
    OnAttackHit,           // 普攻命中
    OnAttackCrit,          // 普攻暴击
    OnPostAttackWindow,    // PostAttack 窗口
    OnPostCastWindow,      // PostCast 窗口
    
    // 新增触发时机 - 光环系统
    OnBattleStart,         // 战斗开始时（全体触发）
    OnPlayerDeath,         // 玩家死亡时（触发清除逻辑）
    OnPlayerRevive,        // 玩家复活时（重新触发光环）
    OnAllyRevive,          // 队友复活时（其他队友可触发）
}
```

**实施要点：**

1. **战斗开始触发**
```csharp
// MultiBattleInstance.StartBattle() 中
private void TriggerBattleStartSkills()
{
    // 遍历所有玩家
    foreach (var character in _playerTeam.GetLivingMembers())
    {
        // 获取角色的被动技能
        var passiveSkills = GetCharacterPassiveSkills(character.Id);
        
        // 触发 OnBattleStart 技能
        foreach (var skill in passiveSkills)
        {
            if (skill.Triggers?.Any(t => t.When == "OnBattleStart") == true)
            {
                // 调用 TriggerProcessor 处理
                var triggeredSkills = _triggerProcessor.ProcessTriggers(
                    when: "OnBattleStart",
                    casterId: character.Id,
                    sourceSkill: skill,
                    context: CreateBattleContext(character),
                    isCasterPlayer: true,
                    casterCharacterData: _characterDataMap?.GetValueOrDefault(character.Id),
                    casterProfessionId: character.ProfessionId);
                
                // 执行触发的技能效果
                foreach (var triggeredSkill in triggeredSkills)
                {
                    ExecuteSkill(triggeredSkill, character.Id, isCasterPlayer: true);
                }
            }
        }
    }
}
```

2. **玩家死亡清除 Buff**
```csharp
// MultiBattleInstance 中
private void HandlePlayerDeath(string characterId)
{
    // 清除该玩家的所有 Buff
    if (_playerBuffOwners.TryGetValue(characterId, out var buffOwner))
    {
        buffOwner.ClearAllBuffs(); // 需要在 BuffOwner 中添加此方法
    }
    
    // 可选：触发 OnPlayerDeath 技能（如队友反应技能）
    // ProcessDeathTriggers(characterId, isCasterPlayer: true);
}
```

3. **玩家复活重新触发光环**
```csharp
// MultiBattleInstance 中
private void HandlePlayerRevive(string characterId)
{
    // 重新触发 OnPlayerRevive 技能
    var triggeredSkills = _triggerProcessor.ProcessTriggers(
        when: "OnPlayerRevive",
        casterId: characterId,
        sourceSkill: null,
        context: CreateBattleContext(characterId),
        isCasterPlayer: true,
        casterCharacterData: _characterDataMap?.GetValueOrDefault(characterId),
        casterProfessionId: GetCharacterProfession(characterId));
    
    // 执行触发的技能效果
    foreach (var skill in triggeredSkills)
    {
        ExecuteSkill(skill, characterId, isCasterPlayer: true);
    }
}
```

**光环技能配置示例：**
```json
{
  "id": "warrior_strength_aura",
  "name": "力量光环",
  "type": "passive",
  "slotType": "passive",
  "triggers": [
    {
      "when": "OnBattleStart",
      "procChance": 1.0,
      "fireSkillId": "warrior_strength_aura_effect",
      "priority": 10
    },
    {
      "when": "OnPlayerRevive",
      "procChance": 1.0,
      "fireSkillId": "warrior_strength_aura_effect",
      "priority": 10
    }
  ]
}
```

```json
{
  "id": "warrior_strength_aura_effect",
  "name": "力量光环效果",
  "type": "passive",
  "targetPolicy": "allies_all",
  "buffs": [
    {
      "buffId": "strength_aura_buff",
      "target": "AllTargets",
      "durationSec": 9999
    }
  ]
}
```

**Buff 定义：**
```json
{
  "id": "strength_aura_buff",
  "name": "力量光环",
  "type": "Buff",
  "duration": 9999,
  "effects": [
    {
      "type": "ModifyStat",
      "stat": "AttackPower",
      "operation": "MultiplyPercent",
      "value": 10
    }
  ]
}
```

### 3.3 血量触发条件（P0）

#### 需求描述
- HP 低于阈值时自动触发技能（如 HP < 30% 触发保命技能）
- 队友 HP 低于阈值时触发治疗技能

#### 当前能力评估
| 功能 | 现状 |
|------|------|
| `HpBelowPct` 条件 | ✅ 已支持（自身） |
| `HpAbovePct` 条件 | ✅ 已支持（自身） |
| 队友 HP 检查 | ❌ 缺失 |
| 定期检查机制 | ❌ 缺失 |

#### 问题分析

**现有问题：**
- 当前的 `HpBelowPct` 条件只能在**技能施放时**检查
- 无法实现"HP 降到 30% 时自动触发"的效果
- 需要有**定期检查机制**来监控 HP 变化

**解决方案：定期检查 + 条件触发**

### 3.4 怪物数量触发（P1）

#### 需求描述
- 根据敌人数量触发技能（如敌人 >= 3 触发 AOE）
- 根据队友数量触发技能（如存活队友 <= 1 触发团队增益）

#### 当前能力评估
| 功能 | 现状 |
|------|------|
| 敌人数量条件 | ❌ 缺失 |
| 队友数量条件 | ❌ 缺失 |
| 定期检查机制 | ❌ 缺失 |

#### 设计方案

**扩展 SkillConditions 类：**
```csharp
public class SkillConditions
{
    // 现有条件
    public double? HpBelowPct { get; set; }
    public double? HpAbovePct { get; set; }
    public string? RequireBuffId { get; set; }
    public string? ForbidBuffId { get; set; }
    public Dictionary<string, int>? RequireResource { get; set; }
    public Dictionary<string, int>? RequireBuffStacks { get; set; }
    
    // 新增 - 数量条件
    public int? EnemyCountAbove { get; set; }      // 敌人数量 >= 阈值
    public int? EnemyCountBelow { get; set; }      // 敌人数量 <= 阈值
    public int? AllyCountAbove { get; set; }       // 队友数量 >= 阈值
    public int? AllyCountBelow { get; set; }       // 队友数量 <= 阈值
    
    // 新增 - 队友HP条件
    public double? AllyHpBelowPct { get; set; }    // 任意队友 HP < 阈值
}
```

**ConditionChecker 扩展：**
```csharp
// 检查敌人数量条件
private bool CheckEnemyCountCondition(
    BattleTeam<Enemy> enemyTeam, 
    int? enemyCountAbove, 
    int? enemyCountBelow)
{
    int livingEnemyCount = enemyTeam.GetLivingMembers().Count();
    
    if (enemyCountAbove.HasValue && livingEnemyCount < enemyCountAbove.Value)
        return false;
    
    if (enemyCountBelow.HasValue && livingEnemyCount > enemyCountBelow.Value)
        return false;
    
    return true;
}

// 检查队友数量条件
private bool CheckAllyCountCondition(
    BattleTeam<Character> playerTeam, 
    int? allyCountAbove, 
    int? allyCountBelow)
{
    int livingAllyCount = playerTeam.GetLivingMembers().Count();
    
    if (allyCountAbove.HasValue && livingAllyCount < allyCountAbove.Value)
        return false;
    
    if (allyCountBelow.HasValue && livingAllyCount > allyCountBelow.Value)
        return false;
    
    return true;
}

// 检查队友HP条件
private bool CheckAllyHpBelowPctCondition(
    BattleTeam<Character> playerTeam, 
    double? allyHpBelowPct)
{
    if (!allyHpBelowPct.HasValue)
        return true;
    
    // 检查是否有任意队友 HP 低于阈值
    foreach (var ally in playerTeam.GetLivingMembers())
    {
        double hpPct = (ally.Hp * 100.0) / ally.MaxHp;
        if (hpPct < allyHpBelowPct.Value)
            return true;
    }
    
    return false; // 没有队友满足条件
}
```

**技能配置示例：**
```json
{
  "id": "warrior_aoe_when_many_enemies",
  "name": "群体打击",
  "type": "active",
  "slotType": "active",
  "releaseType": "instant",
  "targetPolicy": "enemies_all",
  "cooldownSec": 10,
  "conditions": {
    "enemyCountAbove": 3
  },
  "damage": {
    "base": 50,
    "scaleStat": "AttackPower",
    "scaleRatio": 0.8
  }
}
```

### 3.5 定期检查机制（P1）

#### 需求描述
- 每秒检查一次条件（如 HP、资源、敌人数量等）
- 满足条件时自动触发技能
- 避免性能问题

#### 设计方案

**方案 A：复用 TriggerProcessor + 新增 Periodic 触发时机（推荐）**

```csharp
// 新增触发时机
public enum TriggerWhen
{
    // ... 现有触发时机 ...
    
    // 新增 - 定期触发
    OnPeriodic,            // 定期检查（每秒）
}
```

**实施步骤：**

1. **在 MultiBattleInstance 中添加定期检查轨道**
```csharp
// MultiBattleInstance.cs
private TrackState? _periodicCheckTrack;

// 初始化时创建
private void InitializePeriodicCheckTrack()
{
    _periodicCheckTrack = new TrackState(
        intervalMs: 1000,  // 每秒检查一次
        initialOffsetMs: 0);
}

// 在 AdvanceTick 中调用
private void ProcessPeriodicCheckTrack(int nowMs)
{
    if (_periodicCheckTrack?.ShouldTrigger(nowMs) == true)
    {
        ProcessPeriodicTriggers(nowMs);
        _periodicCheckTrack.MarkTriggered(nowMs);
    }
}

// 处理定期触发
private void ProcessPeriodicTriggers(int nowMs)
{
    // 遍历所有玩家
    foreach (var character in _playerTeam.GetLivingMembers())
    {
        // 获取角色的被动技能
        var passiveSkills = GetCharacterPassiveSkills(character.Id);
        
        // 触发 OnPeriodic 技能
        foreach (var skill in passiveSkills)
        {
            if (skill.Triggers?.Any(t => t.When == "OnPeriodic") == true)
            {
                var triggeredSkills = _triggerProcessor.ProcessTriggers(
                    when: "OnPeriodic",
                    casterId: character.Id,
                    sourceSkill: skill,
                    context: CreateBattleContext(character),
                    isCasterPlayer: true,
                    casterCharacterData: _characterDataMap?.GetValueOrDefault(character.Id),
                    casterProfessionId: character.ProfessionId);
                
                // 执行触发的技能效果
                foreach (var triggeredSkill in triggeredSkills)
                {
                    ExecuteSkill(triggeredSkill, character.Id, isCasterPlayer: true);
                }
            }
        }
    }
}
```

2. **技能配置示例：低血量自动回血**
```json
{
  "id": "auto_heal_when_low_hp",
  "name": "自动回血",
  "type": "passive",
  "slotType": "passive",
  "triggers": [
    {
      "when": "OnPeriodic",
      "procChance": 1.0,
      "fireSkillId": "emergency_heal",
      "priority": 10,
      "conditions": {
        "hpBelowPct": 30
      }
    }
  ]
}
```

```json
{
  "id": "emergency_heal",
  "name": "紧急治疗",
  "type": "passive",
  "targetPolicy": "self",
  "cooldownSec": 30,
  "instantHeal": 100
}
```

**方案 B：独立 PeriodicSkillChecker 类（设计文档推荐）**

```csharp
public class PeriodicSkillChecker
{
    private readonly SkillRepository _skillRepository;
    private readonly ConditionChecker _conditionChecker;
    private readonly Func<string, CooldownManager> _getCooldownManager;
    private readonly ResourceManager _resourceManager;
    
    // 注册的定期技能列表
    private readonly List<PeriodicSkillEntry> _registeredSkills = new();
    
    public void RegisterPeriodicSkill(
        string entityId, 
        SkillDef skill, 
        bool isPlayer)
    {
        _registeredSkills.Add(new PeriodicSkillEntry
        {
            EntityId = entityId,
            Skill = skill,
            IsPlayer = isPlayer
        });
    }
    
    public void ProcessPeriodicChecks(int nowMs, MultiBattleInstance battle)
    {
        foreach (var entry in _registeredSkills)
        {
            // 检查冷却
            var cooldownMgr = _getCooldownManager(entry.EntityId);
            if (!cooldownMgr.IsReady(entry.Skill.Id))
                continue;
            
            // 检查条件
            var context = battle.CreateBattleContext(entry.EntityId, entry.IsPlayer);
            if (!_conditionChecker.CheckConditions(entry.Skill, context, entry.IsPlayer, entry.EntityId))
                continue;
            
            // 检查资源
            if (!_resourceManager.CheckResourceCost(entry.Skill, context))
                continue;
            
            // 触发技能
            battle.ExecuteSkill(entry.Skill, entry.EntityId, entry.IsPlayer);
            
            // 记录冷却
            cooldownMgr.StartCooldown(entry.Skill.Id, entry.Skill.CooldownSec);
        }
    }
}

private class PeriodicSkillEntry
{
    public string EntityId { get; set; } = "";
    public SkillDef Skill { get; set; } = null!;
    public bool IsPlayer { get; set; }
}
```

**方案对比：**

| 对比项 | 方案A（扩展 TriggerProcessor） | 方案B（独立 PeriodicSkillChecker） |
|-------|---------------------------|--------------------------------|
| 代码复用 | ✅ 高（复用现有触发逻辑） | ❌ 低（需要重复逻辑） |
| 职责分离 | ⚠️ 一般（触发器职责扩大） | ✅ 好（独立组件） |
| 实施难度 | ✅ 低（3-4h） | ⚠️ 中（6-8h） |
| 维护成本 | ✅ 低（统一维护） | ⚠️ 中（两套逻辑） |
| 性能 | ✅ 相同 | ✅ 相同 |
| 推荐度 | ⭐⭐⭐ | ⭐⭐ |

**推荐：方案 A**，理由：
- 已有 TriggerProcessor 成熟稳定（Phase 7 实施完成）
- 代码复用率高，维护成本低
- 实施时间短，风险小

---

## 四、实施方案推荐

### 4.1 总体方案选择

**推荐：扩展现有触发系统（方案 A）**

**理由：**
1. ✅ **最小侵入**：复用现有 TriggerProcessor 和 ConditionChecker
2. ✅ **统一机制**：所有触发都通过相同流程，便于维护
3. ✅ **风险最低**：无需重构大量代码，测试成本低
4. ✅ **时间最短**：预计 10-14 小时完成（vs 方案B 的 15-20 小时）

### 4.2 详细实施计划

#### 🎯 阶段 1: 光环效果系统（P0 - 4-5小时）

**目标：** 实现战斗开始触发、玩家死亡清除、玩家复活重新触发

**任务清单：**
- [ ] 1.1 扩展 `TriggerDef.When` 支持新时机（+3 种）
  - [ ] `OnBattleStart`：战斗开始时
  - [ ] `OnPlayerDeath`：玩家死亡时（可选）
  - [ ] `OnPlayerRevive`：玩家复活时
- [ ] 1.2 在 `StartBattle()` 中添加 `OnBattleStart` 触发点
- [ ] 1.3 在 `ApplyDamageToPlayer` 中检测死亡并清除 Buff
- [ ] 1.4 添加 `HandlePlayerRevive()` 方法并触发 `OnPlayerRevive`
- [ ] 1.5 在 `BuffOwner` 中添加 `ClearAllBuffs()` 方法
- [ ] 1.6 创建示例光环技能配置
  - [ ] 战士力量光环
  - [ ] 牧师治疗光环
- [ ] 1.7 编写单元测试（8-10 个）
  - [ ] OnBattleStart 触发测试（3 个）
  - [ ] 死亡清除 Buff 测试（2 个）
  - [ ] 复活重新触发测试（3 个）
  - [ ] 光环 Buff 效果测试（2 个）

**验收标准：**
- ✅ 战斗开始时光环自动激活
- ✅ 玩家死亡时所有 Buff 清除
- ✅ 玩家复活时光环重新触发
- ✅ 8-10 个单元测试全部通过
- ✅ 所有现有测试（644个）继续通过

**预估工作量：** 4-5 小时

---

#### 🎯 阶段 2: 数量条件扩展（P1 - 2-3小时）

**目标：** 支持基于敌人/队友数量的条件触发

**任务清单：**
- [ ] 2.1 扩展 `SkillConditions` 类（+5 个字段）
  - [ ] `EnemyCountAbove`：敌人数量 >=
  - [ ] `EnemyCountBelow`：敌人数量 <=
  - [ ] `AllyCountAbove`：队友数量 >=
  - [ ] `AllyCountBelow`：队友数量 <=
  - [ ] `AllyHpBelowPct`：任意队友 HP <
- [ ] 2.2 在 `ConditionChecker` 中实现新条件检查方法
  - [ ] `CheckEnemyCountCondition()`
  - [ ] `CheckAllyCountCondition()`
  - [ ] `CheckAllyHpBelowPctCondition()`
- [ ] 2.3 创建示例技能配置
  - [ ] 群体打击（敌人 >= 3 触发）
  - [ ] 团队急救（队友 HP < 30% 触发）
- [ ] 2.4 编写单元测试（8-10 个）
  - [ ] 敌人数量条件测试（3 个）
  - [ ] 队友数量条件测试（3 个）
  - [ ] 队友 HP 条件测试（2 个）
  - [ ] 综合条件测试（2 个）

**验收标准：**
- ✅ 敌人数量条件正确判定
- ✅ 队友数量条件正确判定
- ✅ 队友 HP 条件正确判定
- ✅ 8-10 个单元测试全部通过
- ✅ 所有现有测试继续通过

**预估工作量：** 2-3 小时

---

#### 🎯 阶段 3: 定期检查机制（P1 - 3-4小时）

**目标：** 实现每秒定期检查条件触发

**任务清单：**
- [ ] 3.1 扩展 `TriggerDef.When` 支持 `OnPeriodic`
- [ ] 3.2 在 `MultiBattleInstance` 中初始化定期检查轨道
  - [ ] 添加 `_periodicCheckTrack` 字段
  - [ ] 在 `Start()` 中调用 `InitializePeriodicCheckTrack()`
- [ ] 3.3 实现 `ProcessPeriodicTriggers()` 方法
  - [ ] 遍历所有玩家
  - [ ] 检查 `OnPeriodic` 触发器
  - [ ] 调用 `TriggerProcessor.ProcessTriggers()`
- [ ] 3.4 在 `AdvanceTick()` 中集成定期检查
- [ ] 3.5 创建示例技能配置
  - [ ] 自动回血（HP < 30% 每秒检查）
  - [ ] 自动回蓝（MP < 20% 每秒检查）
- [ ] 3.6 编写单元测试（10-12 个）
  - [ ] 定期触发机制测试（3 个）
  - [ ] HP 条件定期检查测试（3 个）
  - [ ] 冷却时间测试（2 个）
  - [ ] 性能测试（1 个）
  - [ ] 集成测试（3 个）

**验收标准：**
- ✅ 定期检查每秒触发一次
- ✅ HP 低于阈值时自动触发技能
- ✅ 冷却时间正确管理
- ✅ 性能测试通过（< 1ms per check）
- ✅ 10-12 个单元测试全部通过
- ✅ 所有现有测试继续通过

**预估工作量：** 3-4 小时

---

#### 🎯 阶段 4: 集成测试和文档（P2 - 2-3小时）

**目标：** 完善集成测试和文档

**任务清单：**
- [ ] 4.1 编写端到端集成测试（5-8 个）
  - [ ] 光环系统完整流程测试（2 个）
  - [ ] 定期检查完整流程测试（2 个）
  - [ ] 复杂场景测试（2-4 个）
- [ ] 4.2 更新文档
  - [ ] 更新 `Step2_实施进度追踪.md`
  - [ ] 创建 `Step3_实施总结.md`
  - [ ] 更新 API 文档
- [ ] 4.3 性能优化（可选）
  - [ ] 条件缓存机制
  - [ ] 智能检查（仅检查可能触发的技能）

**验收标准：**
- ✅ 5-8 个集成测试全部通过
- ✅ 文档完整更新
- ✅ 所有测试（660+ 个）继续通过

**预估工作量：** 2-3 小时

---

### 4.3 总体时间估算

| 阶段 | 描述 | 预估时间 |
|------|------|---------|
| 阶段 1 | 光环效果系统（P0） | 4-5h |
| 阶段 2 | 数量条件扩展（P1） | 2-3h |
| 阶段 3 | 定期检查机制（P1） | 3-4h |
| 阶段 4 | 集成测试和文档（P2） | 2-3h |
| **总计** | | **11-15 小时** |

### 4.4 测试覆盖目标

```
现有测试基线：644 个
新增测试预估：26-32 个
- 阶段 1：8-10 个
- 阶段 2：8-10 个
- 阶段 3：10-12 个

完成后总测试：670-676 个
```

---

## 五、关键设计决策

### 5.1 光环续期机制

**设计文档建议：** 光环 Buff 持续 999 秒，剩余 < 5 秒时自动续期

**问题分析：**
- ❓ 999 秒 = 16.65 分钟，大部分战斗不会持续这么久
- ❓ 是否需要续期机制？增加复杂度

**推荐方案：** 简化为战斗结束前持续

**实现：**
```csharp
// 方案 1: 使用超长持续时间（简单）
"durationSec": 9999  // 2.77 小时，实际战斗不会这么久

// 方案 2: 添加 DurationType 枚举（复杂）
public enum BuffDurationType
{
    Timed,              // 固定时间
    UntilBattleEnd,     // 战斗结束前
    Permanent,          // 永久（仅非战斗场景）
}
```

**推荐：方案 1**，理由：
- ✅ 实现简单，无需修改 Buff 系统
- ✅ 9999 秒足够覆盖所有战斗场景
- ✅ 不增加系统复杂度

### 5.2 消耗品系统优先级

**设计文档包含：** 消耗品装备槽、自动使用药水/食物

**问题分析：**
- ❓ 消耗品系统涉及装备槽扩展、物品管理、背包系统等
- ❓ 工作量较大（预估 8-12 小时）
- ❓ 是否属于 Step3 的核心范围？

**推荐方案：** 消耗品系统作为独立阶段（Step3.5 或 Step4）

**Step3 范围：** 仅实现触发机制，不实现消耗品装备槽

**实现：** 技能配置中模拟消耗品效果
```json
{
  "id": "auto_heal_when_low_hp",
  "name": "自动回血（模拟药水）",
  "type": "passive",
  "triggers": [
    {
      "when": "OnPeriodic",
      "fireSkillId": "emergency_heal",
      "conditions": {
        "hpBelowPct": 30
      }
    }
  ]
}
```

### 5.3 性能影响评估

**问题：** 每秒检查所有角色的所有 Periodic 技能，是否会有性能问题？

**分析：**
```
假设场景：
- 4 个玩家
- 每个玩家 2 个定期技能
- 每次检查耗时 0.1ms

总耗时 = 4 * 2 * 0.1ms = 0.8ms / 秒
CPU占比 = 0.8ms / 1000ms = 0.08%
```

**结论：** 性能影响可忽略不计

**优化策略（可选）：**
1. **条件缓存**：相同条件 100-200ms 内缓存结果
2. **智能检查**：仅检查有可能触发的技能（如 HP 变化时才检查 HP 条件）
3. **批量处理**：一次检查处理所有玩家，减少函数调用开销

**推荐：** 初期不做优化，性能测试后按需优化

---

## 六、风险评估

### 6.1 风险识别

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|----------|
| 性能问题（定期检查过于频繁） | 中 | 低 | 1. 合理间隔（1000ms）<br>2. 条件缓存<br>3. 性能测试 |
| 光环触发时机不正确 | 高 | 低 | 1. 充分单元测试<br>2. 集成测试<br>3. 手动验证 |
| 与现有系统集成问题 | 中 | 低 | 1. 最小侵入设计<br>2. 渐进式实施<br>3. 充分测试 |
| 玩家死亡/复活逻辑缺失 | 高 | 中 | 1. 先实现简单版本<br>2. 后续完善 |
| Bug 导致无限触发 | 高 | 低 | 1. 冷却时间强制检查<br>2. 触发次数限制<br>3. 安全机制 |

### 6.2 回滚计划

**如果新系统出现严重问题：**
1. **功能开关**：添加配置开关禁用定期检查
2. **降级方案**：光环效果降级为 OnAttackHit 触发
3. **快速修复**：准备 hotfix 流程
4. **监控告警**：设置性能和错误监控

---

## 七、待确认问题

### 7.1 玩家死亡和复活机制

**问题：** 当前战斗系统中是否已有玩家死亡和复活机制？

**影响：** 如果没有，需要先实现基础的死亡/复活逻辑

**建议：**
- 🔍 **需要确认**：当前 HP 降到 0 时的处理逻辑
- 🔍 **需要确认**：是否有复活技能/物品/机制
- 📝 **如果没有**：Step3 先实现光环战斗开始触发，死亡/复活机制后续补充

### 7.2 光环 Buff 持续时间

**问题：** 光环 Buff 应该持续多久？

**选项：**
- A. 9999 秒（推荐）
- B. 999 秒 + 自动续期机制
- C. 新增 `UntilBattleEnd` 持续类型

**建议：** 选择 **A**，理由见 5.1 节

### 7.3 消耗品系统范围

**问题：** 消耗品系统是否在 Step3 范围内？

**建议：** 不包含在 Step3，理由见 5.2 节

### 7.4 定期检查间隔

**问题：** 定期检查间隔应该是多少？

**选项：**
- A. 1000ms（1秒）
- B. 500ms（0.5秒）
- C. 2000ms（2秒）

**建议：** 选择 **A**（1秒），理由：
- ✅ 对大多数场景足够及时
- ✅ 性能影响小
- ✅ 可根据实际测试调整

---

## 八、实施建议总结

### 8.1 推荐方案

**✅ 扩展现有触发系统（方案 A）**

**理由：**
1. 最小侵入，复用现有 TriggerProcessor
2. 统一触发机制，便于维护
3. 实施时间短（11-15h vs 15-20h）
4. 风险低，测试成本小

### 8.2 实施优先级

| 优先级 | 阶段 | 核心功能 | 时间 |
|-------|------|---------|------|
| ⭐⭐⭐ | 阶段 1 | 光环效果系统 | 4-5h |
| ⭐⭐ | 阶段 2 | 数量条件扩展 | 2-3h |
| ⭐⭐ | 阶段 3 | 定期检查机制 | 3-4h |
| ⭐ | 阶段 4 | 集成测试和文档 | 2-3h |

### 8.3 关键决策

1. **光环持续时间**：使用 9999 秒，不实现续期机制
2. **消耗品系统**：不在 Step3 范围，作为独立阶段
3. **定期检查间隔**：1000ms（1秒）
4. **玩家死亡/复活**：先实现战斗开始触发，死亡/复活逻辑后续补充

### 8.4 成功标准

- ✅ 光环效果在战斗开始时自动激活
- ✅ HP 低于阈值时自动触发技能（通过定期检查）
- ✅ 敌人/队友数量条件正确判定
- ✅ 所有新增测试通过（26-32 个）
- ✅ 所有现有测试继续通过（644 个）
- ✅ 性能测试通过（< 1ms per check）
- ✅ 文档完整更新

---

## 九、下一步行动

### 9.1 等待用户确认

请用户确认以下问题：

1. ✅ **是否同意推荐方案**（扩展现有触发系统）？
2. ✅ **玩家死亡/复活机制**：当前是否已有？如果没有，是否先实现战斗开始触发？
3. ✅ **消耗品系统**：是否在 Step3 范围内？建议作为独立阶段。
4. ✅ **光环持续时间**：是否同意使用 9999 秒，不实现续期机制？
5. ✅ **定期检查间隔**：1000ms（1秒）是否合适？

### 9.2 确认后开始实施

用户确认后，将按照以下顺序实施：

```
阶段 1（4-5h）→ 阶段 2（2-3h）→ 阶段 3（3-4h）→ 阶段 4（2-3h）
```

每个阶段完成后会提交 PR 和进度更新。

---

**文档维护：**
- 实施过程中的决策记录在 `决策日志` 章节
- 每个阶段完成后更新 `Step2_实施进度追踪.md`

**相关文档：**
- `Step2_实施进度追踪.md` - 当前进度（644 测试通过）
- `Step3_定期技能检查系统设计.md` - 设计文档
- `TriggerProcessor.cs` - 触发处理器实现
- `ConditionChecker.cs` - 条件检查器实现

---

**最后更新：** 2025-11-22  
**维护者：** @copilot  
**状态：** ✅ 分析完成，等待用户确认
