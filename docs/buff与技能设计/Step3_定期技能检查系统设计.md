# Step3 - 定期技能检查系统设计

**文档版本：** v1.0  
**创建日期：** 2025-11-20  
**状态：** 设计阶段

---

## 一、概述

### 1.1 背景

当前战斗系统中，技能触发主要依赖攻击事件：
- **主动施法技能**：通过 AttackTrack 触发后检查施法
- **被动技能**：在攻击、施法等事件的窗口中触发
- **DoT/HoT 效果**：通过独立的时间轨道周期触发

**局限性：**
- 无法实现"战斗开始立即生效"的光环效果
- 无法支持战斗中使用消耗品（食物、药剂）
- 缺少基于条件（HP阈值、资源等）的自动触发机制

### 1.2 目标

设计并实现一个**定期技能检查系统**，支持：
1. **光环效果**：战斗开始时自动激活，持续整场战斗
2. **消耗品系统**：战斗中可使用食物/药剂，提供回血/增益效果
3. **条件触发**：基于 HP、资源、状态等条件自动触发技能
4. **可扩展性**：为未来更多战斗机制提供基础

### 1.3 设计原则

- **最小侵入**：复用现有技能系统，不破坏当前战斗逻辑
- **统一建模**：光环、消耗品、条件触发都用技能系统表达
- **性能优先**：定期检查频率合理，避免性能问题
- **清晰分离**：新系统与现有攻击/施法系统职责明确

---

## 二、系统架构

### 2.1 触发类型扩展

为技能系统增加新的触发类型：

```csharp
public enum SkillTriggerType
{
    // === 现有触发类型 ===
    Manual,           // 手动触发
    OnAttack,         // 攻击时触发（被动）
    OnCast,           // 施法时触发（被动）
    OnHit,            // 受击时触发（被动）
    // ... 其他现有类型
    
    // === 新增触发类型 ===
    OnBattleStart,    // 战斗开始时触发
    Periodic,         // 定期检查触发
    OnCondition,      // 条件满足时触发
}
```

### 2.2 定期检查系统组件

#### 2.2.1 核心组件

```
┌─────────────────────────────────────────────────────┐
│          MultiBattleInstance                        │
│  ┌───────────────────────────────────────────────┐ │
│  │    PeriodicSkillChecker (新增)                │ │
│  │  - 管理定期检查的技能列表                      │ │
│  │  - 每秒执行检查逻辑                            │ │
│  │  - 触发满足条件的技能                          │ │
│  └───────────────────────────────────────────────┘ │
│                      │                              │
│                      ▼                              │
│  ┌───────────────────────────────────────────────┐ │
│  │    现有战斗系统                                │ │
│  │  - AttackTrack (攻击)                         │ │
│  │  - CastingController (施法)                   │ │
│  │  - BuffManager (Buff/DoT)                     │ │
│  └───────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

#### 2.2.2 触发时机

```
战斗生命周期：

StartBattle() 
  │
  ├─> 触发 OnBattleStart 技能（光环）
  │
  ├─> 启动 AttackTrack
  ├─> 启动 DoT/HoT Track
  └─> 启动 PeriodicCheck Track (新增)
        │
        ├─ 每 1000ms 触发一次
        │   │
        │   ├─> 检查 Periodic 技能
        │   ├─> 检查 OnCondition 技能
        │   └─> 执行满足条件的技能效果
        │
        └─ 战斗结束停止
```

### 2.3 技能配置扩展

#### 2.3.1 光环技能示例

```json
{
  "id": "warrior_strength_aura",
  "name": "力量光环",
  "triggerType": "OnBattleStart",
  "effects": [
    {
      "type": "ApplyBuff",
      "buffId": "strength_aura_buff",
      "target": "AllAllies",
      "duration": 999
    }
  ]
}
```

#### 2.3.2 消耗品技能示例

```json
{
  "id": "health_potion",
  "name": "生命药水",
  "triggerType": "Periodic",
  "checkInterval": 1.0,
  "conditions": [
    {
      "type": "HPBelowPercent",
      "value": 50
    },
    {
      "type": "ItemAvailable",
      "itemId": "health_potion"
    }
  ],
  "effects": [
    {
      "type": "Heal",
      "value": 100
    },
    {
      "type": "ConsumeItem",
      "itemId": "health_potion",
      "count": 1
    }
  ],
  "cooldown": 30
}
```

#### 2.3.3 条件触发技能示例

```json
{
  "id": "last_stand",
  "name": "背水一战",
  "triggerType": "OnCondition",
  "checkInterval": 1.0,
  "conditions": [
    {
      "type": "HPBelowPercent",
      "value": 20
    },
    {
      "type": "NoBuff",
      "buffId": "last_stand_active"
    }
  ],
  "effects": [
    {
      "type": "ApplyBuff",
      "buffId": "last_stand_buff",
      "duration": 10
    }
  ],
  "cooldown": 60
}
```

---

## 三、详细设计

### 3.1 PeriodicSkillChecker 类

#### 3.1.1 核心接口

```csharp
public class PeriodicSkillChecker
{
    // 战斗开始时初始化
    public void Initialize(
        List<Character> playerTeam, 
        List<Enemy> enemyTeam);
    
    // 注册需要定期检查的技能
    public void RegisterPeriodicSkill(
        string entityId, 
        SkillDefinition skill, 
        bool isPlayer);
    
    // 每秒调用一次
    public void ProcessPeriodicChecks(int nowMs);
    
    // 战斗结束时清理
    public void Cleanup();
}
```

#### 3.1.2 检查逻辑

```csharp
public void ProcessPeriodicChecks(int nowMs)
{
    foreach (var entry in _registeredSkills)
    {
        // 检查冷却时间
        if (IsOnCooldown(entry.SkillId, nowMs))
            continue;
        
        // 检查触发条件
        if (!CheckConditions(entry, nowMs))
            continue;
        
        // 执行技能效果
        ExecuteSkillEffects(entry);
        
        // 记录冷却时间
        SetCooldown(entry.SkillId, nowMs);
    }
}
```

### 3.2 条件检查系统

#### 3.2.1 条件类型

```csharp
public enum ConditionType
{
    HPBelowPercent,      // HP 低于百分比
    HPAbovePercent,      // HP 高于百分比
    MPBelowPercent,      // MP 低于百分比
    MPAbovePercent,      // MP 高于百分比
    HasBuff,             // 拥有指定 Buff
    NoBuff,              // 没有指定 Buff
    ItemAvailable,       // 物品可用
    AllyCountBelow,      // 存活队友数量低于
    EnemyCountBelow,     // 存活敌人数量低于
    TimeElapsed,         // 战斗时间超过
}
```

#### 3.2.2 条件评估器

```csharp
public interface IConditionEvaluator
{
    bool Evaluate(
        string entityId, 
        ConditionDefinition condition, 
        MultiBattleInstance battle);
}

public class HPBelowPercentEvaluator : IConditionEvaluator
{
    public bool Evaluate(...)
    {
        var character = battle.GetCharacter(entityId);
        var hpPercent = (character.CurrentHP / character.MaxHP) * 100;
        return hpPercent < condition.Value;
    }
}
```

### 3.3 时间轨道整合

#### 3.3.1 复用 DoT Track 机制

```csharp
// 在 MultiBattleInstance 中
private void InitializePeriodicCheckTrack()
{
    // 复用 TrackState 机制
    var periodicTrack = new TrackState(
        intervalMs: 1000,  // 每秒检查一次
        initialOffsetMs: 0);
    
    _periodicCheckTrack = periodicTrack;
}

private void ProcessPeriodicCheckTrack(int nowMs)
{
    if (_periodicCheckTrack.ShouldTrigger(nowMs))
    {
        _periodicSkillChecker.ProcessPeriodicChecks(nowMs);
        _periodicCheckTrack.MarkTriggered(nowMs);
    }
}
```

#### 3.3.2 集成到战斗循环

```csharp
public void ProcessTimeStep(int deltaMs)
{
    int nowMs = _clock.NowMs;
    
    // 现有系统
    ProcessAttackTracks(nowMs);
    ProcessCasting(nowMs);
    ProcessDoTHoT(nowMs);
    
    // 新增：定期技能检查
    ProcessPeriodicCheckTrack(nowMs);
}
```

---

## 四、应用场景

### 4.1 光环效果实现

#### 4.1.1 战士力量光环

**技能定义：**
```json
{
  "id": "warrior_strength_aura",
  "name": "力量光环",
  "triggerType": "OnBattleStart",
  "passive": true,
  "effects": [
    {
      "type": "ApplyBuff",
      "buffId": "strength_aura",
      "target": "AllAllies",
      "duration": 999
    }
  ]
}
```

**Buff 定义：**
```json
{
  "id": "strength_aura",
  "name": "力量光环",
  "type": "Buff",
  "duration": 999,
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

**执行流程：**
```
1. StartBattle()
2. 遍历所有玩家的被动技能
3. 发现 "warrior_strength_aura" (triggerType=OnBattleStart)
4. 立即执行技能效果
5. 给全队施加 "strength_aura" Buff (999秒)
6. 战斗全程生效
```

#### 4.1.2 光环续期机制

**长持续 Buff 自动续期：**
```csharp
// 在 ProcessPeriodicChecks 中
private void RefreshAuraBuffs(int nowMs)
{
    foreach (var auraSkill in _activeAuras)
    {
        // 检查 Buff 剩余时间
        var buffRemaining = GetBuffRemainingTime(auraSkill.BuffId);
        
        // 如果剩余时间 < 5秒，重新施加 Buff
        if (buffRemaining < 5000)
        {
            ReapplyBuff(auraSkill.BuffId, duration: 999);
        }
    }
}
```

### 4.2 消耗品系统实现

#### 4.2.1 生命药水

**装备系统扩展：**
```csharp
public class ConsumableSlot
{
    public string ItemId { get; set; }
    public int Count { get; set; }
    public int MaxCount { get; set; }
}

public class CharacterEquipment
{
    // 现有装备槽
    public Dictionary<string, string> EquipmentSlots { get; set; }
    
    // 新增：消耗品槽
    public Dictionary<string, ConsumableSlot> ConsumableSlots { get; set; }
}
```

**技能定义：**
```json
{
  "id": "auto_health_potion",
  "name": "自动饮用生命药水",
  "triggerType": "Periodic",
  "checkInterval": 1.0,
  "conditions": [
    {
      "type": "HPBelowPercent",
      "value": 50
    },
    {
      "type": "ItemAvailable",
      "slot": "consumable_1"
    }
  ],
  "effects": [
    {
      "type": "Heal",
      "target": "Self",
      "value": 100
    },
    {
      "type": "ConsumeItem",
      "slot": "consumable_1",
      "count": 1
    }
  ],
  "cooldown": 30
}
```

**执行流程：**
```
每秒检查：
1. HP < 50%? → 是
2. consumable_1 槽位有药水? → 是
3. 冷却完成? → 是
4. 执行效果：
   - 回复 100 HP
   - 消耗 1 个药水
   - 开始 30 秒冷却
```

#### 4.2.2 战斗食物

**增益型消耗品：**
```json
{
  "id": "strength_food",
  "name": "力量食物",
  "triggerType": "OnBattleStart",
  "conditions": [
    {
      "type": "ItemAvailable",
      "slot": "food"
    }
  ],
  "effects": [
    {
      "type": "ApplyBuff",
      "buffId": "food_strength",
      "target": "Self",
      "duration": 300
    },
    {
      "type": "ConsumeItem",
      "slot": "food",
      "count": 1
    }
  ]
}
```

### 4.3 条件触发技能

#### 4.3.1 背水一战

**低血量触发增益：**
```json
{
  "id": "last_stand",
  "name": "背水一战",
  "triggerType": "OnCondition",
  "checkInterval": 1.0,
  "conditions": [
    {
      "type": "HPBelowPercent",
      "value": 20
    },
    {
      "type": "NoBuff",
      "buffId": "last_stand_active"
    }
  ],
  "effects": [
    {
      "type": "ApplyBuff",
      "buffId": "last_stand_buff",
      "target": "Self",
      "duration": 10
    }
  ],
  "cooldown": 60
}
```

**执行流程：**
```
每秒检查：
1. HP < 20%? → 是
2. 没有 last_stand_active Buff? → 是
3. 冷却完成? → 是
4. 施加 last_stand_buff (10秒，+50% 攻击力，+30% 防御)
5. 开始 60 秒冷却
```

#### 4.3.2 团队急救

**队友低血量触发治疗：**
```json
{
  "id": "team_emergency_heal",
  "name": "团队急救",
  "triggerType": "OnCondition",
  "checkInterval": 1.0,
  "conditions": [
    {
      "type": "AllyHPBelowPercent",
      "value": 30
    },
    {
      "type": "HasClass",
      "class": "Priest"
    }
  ],
  "effects": [
    {
      "type": "Heal",
      "target": "LowestHPAlly",
      "value": 150
    }
  ],
  "cooldown": 20
}
```

---

## 五、实施计划

### 5.1 Phase 1: 核心框架（第1周）

**目标：** 建立定期检查基础架构

**任务清单：**
- [ ] 设计 `PeriodicSkillChecker` 类
- [ ] 扩展 `SkillTriggerType` 枚举
- [ ] 实现 `ProcessPeriodicCheckTrack` 时间轨道
- [ ] 集成到 `MultiBattleInstance` 战斗循环
- [ ] 编写单元测试

**交付物：**
- `PeriodicSkillChecker.cs`
- `SkillTriggerType` 更新
- 基础单元测试（10+）

### 5.2 Phase 2: 条件系统（第2周）

**目标：** 实现条件评估框架

**任务清单：**
- [ ] 设计 `IConditionEvaluator` 接口
- [ ] 实现基础条件评估器：
  - [ ] `HPBelowPercentEvaluator`
  - [ ] `MPBelowPercentEvaluator`
  - [ ] `HasBuffEvaluator`
  - [ ] `ItemAvailableEvaluator`
- [ ] 条件组合逻辑（AND/OR）
- [ ] 编写条件测试

**交付物：**
- 4+ 条件评估器实现
- 条件系统单元测试（20+）

### 5.3 Phase 3: OnBattleStart 技能（第3周）

**目标：** 支持战斗开始触发的光环效果

**任务清单：**
- [ ] 在 `StartBattle()` 中添加 OnBattleStart 触发逻辑
- [ ] 实现光环 Buff 自动续期机制
- [ ] 创建示例光环技能：
  - [ ] 战士力量光环
  - [ ] 牧师治疗光环
- [ ] 编写集成测试

**交付物：**
- OnBattleStart 触发实现
- 2+ 光环技能示例
- 集成测试（10+）

### 5.4 Phase 4: Periodic 技能（第4周）

**目标：** 支持定期检查的消耗品和条件触发

**任务清单：**
- [ ] 实现 Periodic 技能检查逻辑
- [ ] 设计消耗品装备槽
- [ ] 实现消耗品技能：
  - [ ] 生命药水
  - [ ] 魔法药水
  - [ ] 增益食物
- [ ] 实现条件触发技能：
  - [ ] 背水一战
  - [ ] 团队急救
- [ ] 编写完整测试

**交付物：**
- Periodic 技能实现
- 消耗品系统原型
- 3+ 消耗品技能示例
- 2+ 条件触发技能示例
- 完整测试套件（30+）

### 5.5 Phase 5: UI 和文档（第5周）

**目标：** 完善 UI 显示和文档

**任务清单：**
- [ ] 光环效果 UI 显示
- [ ] 消耗品槽位 UI
- [ ] 条件触发技能图标提示
- [ ] 战斗日志扩展
- [ ] 更新用户文档
- [ ] 更新开发文档

**交付物：**
- 光环/消耗品 UI 组件
- 完整用户文档
- 技术文档更新

---

## 六、技术考量

### 6.1 性能优化

#### 6.1.1 检查频率

**建议：**
- 默认检查间隔：1000ms（每秒1次）
- 关键技能可配置更高频率（500ms）
- 非关键技能可降低频率（2000ms）

**性能评估：**
```
假设：
- 每个玩家 2 个定期技能
- 4 个玩家
- 每次检查耗时 0.1ms

总耗时 = 4 * 2 * 0.1ms = 0.8ms / 秒
占比 = 0.8ms / 1000ms = 0.08%
```

#### 6.1.2 条件缓存

```csharp
// 缓存常用条件结果
private Dictionary<string, (bool result, int cacheUntilMs)> _conditionCache;

public bool EvaluateCondition(...)
{
    string cacheKey = GetConditionCacheKey(condition);
    
    if (_conditionCache.TryGetValue(cacheKey, out var cached))
    {
        if (nowMs < cached.cacheUntilMs)
            return cached.result;
    }
    
    bool result = DoEvaluate(condition);
    
    // 缓存 100ms
    _conditionCache[cacheKey] = (result, nowMs + 100);
    
    return result;
}
```

### 6.2 技能冲突处理

#### 6.2.1 优先级系统

```csharp
public enum SkillPriority
{
    Critical = 1,    // 保命技能（背水一战）
    High = 2,        // 治疗/急救
    Normal = 3,      // 一般消耗品
    Low = 4,         // 增益食物
}
```

#### 6.2.2 冲突解决

```csharp
// 同一秒触发多个技能时的处理
private void ResolveTriggerConflicts(List<SkillTrigger> triggers)
{
    // 按优先级排序
    triggers.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    
    // 按优先级执行，高优先级可能阻止低优先级
    foreach (var trigger in triggers)
    {
        if (CanExecute(trigger))
        {
            Execute(trigger);
        }
    }
}
```

### 6.3 可扩展性设计

#### 6.3.1 插件式条件评估器

```csharp
public class ConditionEvaluatorRegistry
{
    private Dictionary<ConditionType, IConditionEvaluator> _evaluators;
    
    public void Register(ConditionType type, IConditionEvaluator evaluator)
    {
        _evaluators[type] = evaluator;
    }
    
    public IConditionEvaluator Get(ConditionType type)
    {
        return _evaluators.GetValueOrDefault(type);
    }
}
```

#### 6.3.2 自定义触发器

```csharp
public interface ICustomTrigger
{
    bool ShouldTrigger(string entityId, MultiBattleInstance battle);
    void OnTrigger(string entityId, MultiBattleInstance battle);
}

// 用户可以实现自定义触发逻辑
public class ComboCounterTrigger : ICustomTrigger
{
    public bool ShouldTrigger(...)
    {
        // 检查连击数
        return battle.GetComboCount(entityId) >= 5;
    }
    
    public void OnTrigger(...)
    {
        // 触发连击奖励
        battle.ApplyBuff(entityId, "combo_bonus");
    }
}
```

---

## 七、测试策略

### 7.1 单元测试

#### 7.1.1 核心组件测试

```csharp
[TestClass]
public class PeriodicSkillCheckerTests
{
    [TestMethod]
    public void ProcessPeriodicChecks_SkillOnCooldown_ShouldNotTrigger()
    {
        // Arrange
        var checker = new PeriodicSkillChecker();
        var skill = CreatePeriodicSkill(cooldown: 30);
        checker.RegisterSkill(skill);
        checker.ProcessPeriodicChecks(1000); // 第一次触发
        
        // Act
        checker.ProcessPeriodicChecks(2000); // 第二次尝试（冷却中）
        
        // Assert
        Assert.AreEqual(1, skill.TriggerCount);
    }
    
    [TestMethod]
    public void ProcessPeriodicChecks_ConditionNotMet_ShouldNotTrigger()
    {
        // Arrange: HP > 50%
        // Assert: 不触发低血量技能
    }
}
```

#### 7.1.2 条件评估器测试

```csharp
[TestClass]
public class HPBelowPercentEvaluatorTests
{
    [TestMethod]
    public void Evaluate_HPBelow50Percent_ReturnsTrue()
    {
        // Arrange
        var character = CreateCharacter(hp: 50, maxHp: 100);
        var condition = new Condition { Type = "HPBelowPercent", Value = 60 };
        var evaluator = new HPBelowPercentEvaluator();
        
        // Act
        bool result = evaluator.Evaluate(character.Id, condition, battle);
        
        // Assert
        Assert.IsTrue(result);
    }
}
```

### 7.2 集成测试

#### 7.2.1 光环效果测试

```csharp
[TestMethod]
public void OnBattleStart_StrengthAura_AppliesBuffToAllAllies()
{
    // Arrange
    var warrior = CreateWarrior(hasStrengthAura: true);
    var mage = CreateMage();
    var battle = CreateBattle(playerTeam: [warrior, mage]);
    
    // Act
    battle.StartBattle();
    
    // Assert
    Assert.IsTrue(warrior.HasBuff("strength_aura"));
    Assert.IsTrue(mage.HasBuff("strength_aura"));
    Assert.AreEqual(1.1, warrior.AttackPowerMultiplier, 0.01);
    Assert.AreEqual(1.1, mage.AttackPowerMultiplier, 0.01);
}
```

#### 7.2.2 消耗品自动使用测试

```csharp
[TestMethod]
public void Periodic_HealthPotion_AutoUsedWhenHPBelow50()
{
    // Arrange
    var character = CreateCharacter(hp: 40, maxHp: 100);
    character.EquipConsumable("health_potion", count: 3);
    var battle = CreateBattle(playerTeam: [character]);
    battle.StartBattle();
    
    // Act
    battle.ProcessTimeStep(1000); // 1秒后触发检查
    
    // Assert
    Assert.AreEqual(140, character.CurrentHP); // 40 + 100
    Assert.AreEqual(2, character.GetConsumableCount("health_potion"));
}
```

### 7.3 性能测试

```csharp
[TestMethod]
public void PerformanceTest_1000PeriodicChecks_Under10ms()
{
    // Arrange
    var checker = new PeriodicSkillChecker();
    for (int i = 0; i < 1000; i++)
    {
        checker.RegisterSkill(CreatePeriodicSkill());
    }
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    checker.ProcessPeriodicChecks(1000);
    stopwatch.Stop();
    
    // Assert
    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10);
}
```

---

## 八、风险和缓解

### 8.1 风险识别

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|----------|
| 性能问题（检查过于频繁） | 高 | 中 | 1. 合理设置检查间隔<br>2. 实现条件缓存<br>3. 性能测试和监控 |
| 技能冲突（同时触发多个技能） | 中 | 高 | 1. 实现优先级系统<br>2. 冲突解决机制<br>3. 充分测试边界情况 |
| 与现有系统集成问题 | 高 | 低 | 1. 最小侵入设计<br>2. 充分的集成测试<br>3. 渐进式部署 |
| 配置复杂度增加 | 中 | 中 | 1. 提供配置示例<br>2. 完善文档<br>3. 验证工具 |
| Bug 导致无限触发 | 高 | 低 | 1. 冷却时间强制检查<br>2. 触发次数限制<br>3. 异常监控 |

### 8.2 回滚计划

**如果新系统出现严重问题：**
1. **功能开关**：添加配置开关禁用定期检查
2. **降级方案**：光环效果降级为被动触发
3. **快速修复**：准备 hotfix 流程
4. **监控告警**：设置性能和错误监控

---

## 九、未来扩展

### 9.1 短期扩展（6个月内）

1. **更多条件类型**
   - 基于距离的条件（近战/远程）
   - 基于 Buff 层数的条件
   - 基于目标状态的条件

2. **复杂触发逻辑**
   - 条件组合（AND/OR/NOT）
   - 概率触发
   - 多阶段触发

3. **高级消耗品**
   - 持续回血食物
   - 复活药水
   - 临时变身药剂

### 9.2 长期扩展（1年内）

1. **AI 辅助决策**
   - 智能药水使用
   - 团队协作技能
   - 战术切换

2. **动态战斗事件**
   - 环境效果（天气、地形）
   - 随机事件触发
   - Boss 阶段转换

3. **跨战斗持久效果**
   - 战斗前准备（食物/药剂）
   - 战斗间恢复
   - 长期增益

---

## 十、总结

### 10.1 核心价值

1. **功能完整性**：支持光环、消耗品、条件触发三大核心场景
2. **系统可扩展**：为未来战斗机制提供统一框架
3. **设计优雅**：复用现有技能系统，最小化代码侵入
4. **性能可控**：合理的检查频率，优化的执行逻辑

### 10.2 成功标准

- [ ] 所有单元测试通过（100+ 测试用例）
- [ ] 性能测试达标（< 1% CPU 占用）
- [ ] 集成测试覆盖所有场景
- [ ] 文档完整清晰
- [ ] 用户反馈积极

### 10.3 下一步行动

1. **评审本设计文档**：团队讨论，确定最终方案
2. **创建实施分支**：`feature/periodic-skill-check`
3. **开始 Phase 1 开发**：核心框架实现
4. **持续迭代**：按照5周计划逐步交付

---

**文档维护：**
- 设计变更需更新此文档
- 实施过程中的决策记录在 `决策日志` 章节
- 每个 Phase 完成后更新进度

**相关文档：**
- `Step2_实施进度追踪.md` - 当前进度
- `step2_技能系统-总体设计.md` - 技能系统设计
- `Buff配置化管理说明.md` - Buff 系统说明
