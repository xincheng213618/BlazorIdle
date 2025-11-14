# Buff 配置化管理系统

**版本**: v1.0  
**日期**: 2025-11-14  
**状态**: ✅ 已实施 (Phase 1-2 完成)

---

## 一、概述

### 目标

将 Buff 定义从代码内联转移到集中配置文件，实现：
- 统一的 Buff 管理
- 配置驱动的 Buff 系统
- 通过 ID 引用而非内联定义
- 无需 API 传输（配置在 Shared 项目）

### 设计模式

参考 `items.json` 和 `monsters.json` 的设计，采用：
- **BuffConfig.cs** - 强类型数据模型
- **buffs.json** - 配置数据文件
- **BuffRepository** - 集中管理类

---

## 二、架构设计

### 文件组织

```
BlazorIdle.Shared/
├── Config/
│   └── buffs.json              # Buff 配置文件
├── Models/
│   └── BuffConfig.cs            # Buff 配置数据模型
└── Game/Buffs/
    ├── BuffRepository.cs        # Buff 仓库管理
    ├── BuffInstance.cs          # 运行时 Buff 实例
    └── BuffEffect.cs            # Buff 效果定义
```

### 数据流

```
buffs.json 
    ↓ LoadFromJson
BuffRepository.Instance 
    ↓ GetBuffById
BuffConfig 
    ↓ ToBuffInstance
BuffInstance (运行时)
    ↓ ApplyBuff
IBuffOwner (玩家/敌人)
```

---

## 三、核心组件

### 1. BuffConfig (数据模型)

```csharp
public class BuffConfig
{
    public string Id { get; set; }               // 唯一标识
    public string Name { get; set; }             // 显示名称
    public string? Description { get; set; }     // 描述
    public string? Icon { get; set; }            // 图标
    public BuffKind Kind { get; set; }           // Buff/Debuff
    public List<BuffEffect> Effects { get; set; }
    public double? DurationSec { get; set; }
    public double? TickIntervalSec { get; set; }
    public BuffStackingPolicy StackingPolicy { get; set; }
    public int MaxStacks { get; set; }
    public BuffTarget DefaultTarget { get; set; } // 默认目标

    public BuffInstance ToBuffInstance(string ownerId, string? sourceSkillId = null);
}
```

### 2. BuffRepository (管理类)

```csharp
public class BuffRepository
{
    // 单例访问
    public static BuffRepository Instance { get; }
    
    // 测试用
    public static BuffRepository CreateNew();
    
    // 核心方法
    public void RegisterBuff(BuffConfig config);
    public BuffConfig? GetBuffById(string buffId);
    public bool HasBuff(string buffId);
    public IReadOnlyDictionary<string, BuffConfig> GetAllBuffs();
    public List<BuffConfig> GetBuffsByKind(BuffKind kind);
    public void LoadFromJson(string json);
}
```

### 3. BuffOperation (应用指令)

```csharp
public class BuffOperation
{
    public BuffOperationType Type { get; set; }
    
    // Phase 2: 支持配置 ID
    public string? BuffConfigId { get; set; }
    
    // 向后兼容
    public BuffInstance? BuffTemplate { get; set; }
    
    // 可选：覆盖 BuffConfig 的默认目标
    public BuffTarget? TargetOverride { get; set; }
    
    // 辅助方法
    public static BuffOperation ApplyByConfigId(string buffConfigId, BuffTarget? targetOverride = null);
    public static BuffOperation ApplyByTemplate(BuffInstance template, BuffTarget target);
    public static BuffOperation Remove(string buffId, BuffTarget target, string? reason = null);
}
```

---

## 四、配置文件格式

### buffs.json 示例

```json
[
  {
    "id": "warrior_rage_boost",
    "name": "狂暴",
    "description": "增加攻击力、攻击速度和暴击率",
    "icon": "⚔️",
    "kind": "Buff",
    "durationSec": 6.0,
    "stackingPolicy": "Refresh",
    "maxStacks": 1,
    "defaultTarget": "Self",
    "effects": [
      {
        "type": "StatMultiplier",
        "target": "DamagePerAttack",
        "value": 0.15
      },
      {
        "type": "StatAdditive",
        "target": "HastePercent",
        "value": 0.10
      }
    ]
  }
]
```

### 字段说明

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| id | string | ✅ | 唯一标识符 |
| name | string | ✅ | 显示名称 |
| description | string | ❌ | 效果描述 |
| icon | string | ❌ | UI 图标 (emoji) |
| kind | enum | ✅ | "Buff" 或 "Debuff" |
| durationSec | double? | ❌ | 持续时间（null=永久） |
| tickIntervalSec | double? | ❌ | Tick 间隔（DoT/HoT） |
| stackingPolicy | enum | ✅ | "Refresh", "Stack", "Ignore" |
| maxStacks | int | ❌ | 最大层数（0=无限） |
| defaultTarget | enum | ✅ | 默认目标类型 |
| effects | array | ✅ | 效果列表 |

### 效果类型 (BuffEffectType)

| 类型 | 说明 | 参数 |
|------|------|------|
| StatMultiplier | 属性倍率 | target, value |
| StatAdditive | 属性加成 | target, value |
| StatReduction | 属性减少 | target, value |
| DamageOverTime | 持续伤害 | amountPerTick |
| HealOverTime | 持续治疗 | amountPerTick |
| InstantHeal | 即时治疗 | amountPerTick |
| ForceCrit | 强制暴击 | - |

### 目标类型 (BuffTarget)

| 类型 | 说明 |
|------|------|
| Self | 施法者自己 |
| Target | 技能主要目标 |
| AllEnemies | 所有敌人 |
| AllAllies | 所有队友 |
| RandomEnemy | 随机一个敌人 |
| LowestHpAlly | 血量最低队友 |

---

## 五、使用方法

### 1. 定义新 Buff

在 `buffs.json` 中添加配置：

```json
{
  "id": "my_new_buff",
  "name": "我的新Buff",
  "kind": "Buff",
  "durationSec": 10.0,
  "defaultTarget": "Self",
  "effects": [
    {
      "type": "StatMultiplier",
      "target": "DamagePerAttack",
      "value": 0.20
    }
  ]
}
```

### 2. 在技能中引用 Buff

#### 方式 A: 通过配置 ID（推荐）

```csharp
// SkillDef 中配置
var skillDef = new SkillDef
{
    Id = "warrior_special",
    OnCastBuffs = new List<BuffOperation>
    {
        BuffOperation.ApplyByConfigId("warrior_rage_boost")
    }
};
```

#### 方式 B: 覆盖默认目标

```csharp
// 将法师的 DoT 应用到单个目标而非全体
var op = BuffOperation.ApplyByConfigId("mage_burn_dot", BuffTarget.Target);
```

#### 方式 C: 向后兼容（inline）

```csharp
var template = new BuffInstance
{
    Id = "custom_buff",
    Kind = BuffKind.Buff,
    // ... 其他属性
};
var op = BuffOperation.ApplyByTemplate(template, BuffTarget.Self);
```

### 3. 代码中查询 Buff

```csharp
// 获取 BuffConfig
var config = BuffRepository.Instance.GetBuffById("damage_boost");

// 转换为运行时实例
var instance = config.ToBuffInstance("player1", "attack_skill");

// 应用到战斗实体
buffOwner.ApplyBuff(instance);
```

---

## 六、默认 Buff 库

系统提供 10 个预定义 Buff：

| ID | 名称 | 类型 | 效果 | 用途 |
|----|------|------|------|------|
| warrior_rage_boost | 狂暴 | Buff | +15%伤害, +10%急速, +5%暴击 | 战士特殊技能 |
| mage_burn_dot | 燃烧 | Debuff | 6点/秒 持续伤害 | 法师 DoT |
| damage_boost | 伤害增幅 | Buff | +10%伤害 (可叠加3层) | 通用增益 |
| regeneration | 恢复 | Buff | 10点/3秒 持续治疗 | 治疗效果 |
| weakness | 虚弱 | Debuff | -20%伤害 | 减益效果 |
| shield | 护盾 | Buff | +25%伤害减免 | 防御效果 |
| haste | 急速 | Buff | +15%急速 (可叠加2层) | 攻速增益 |
| poison | 中毒 | Debuff | 4点/2秒 持续伤害 (可叠加3层) | 毒性 DoT |
| crit_boost | 致命强化 | Buff | +10%暴击率, +20%暴击伤害 | 暴击增益 |
| force_crit | 必定暴击 | Buff | 下次攻击必定暴击 | 特殊效果 |

---

## 七、与现有系统集成

### MultiBattleInstance 集成

```csharp
private void ApplyBuffOperation(BuffOperation operation, ...)
{
    // 1. 优先使用 BuffConfigId
    if (!string.IsNullOrEmpty(operation.BuffConfigId))
    {
        var buffConfig = BuffRepository.Instance.GetBuffById(operation.BuffConfigId);
        templateToUse = buffConfig.ToBuffInstance("", operation.BuffTemplate?.SourceSkillId);
        targetToUse = operation.TargetOverride ?? buffConfig.DefaultTarget;
    }
    // 2. 回退到 BuffTemplate（向后兼容）
    else if (operation.BuffTemplate != null)
    {
        templateToUse = operation.BuffTemplate;
    }
    
    // 3. 应用到解析的目标
    var targets = ResolveBuffTargets(targetToUse, ...);
    foreach (var target in targets)
    {
        target.ApplyBuff(buffToApply);
    }
}
```

### SkillRepository 集成 (Phase 3)

```csharp
// 默认技能定义
new SkillDef
{
    Id = "SpecialPulse",
    OnCastBuffs = new List<BuffOperation>
    {
        BuffOperation.ApplyByConfigId("warrior_rage_boost") // 使用配置 ID
    }
}
```

---

## 八、测试

### 单元测试覆盖

- **Phase 1 (23 tests)**: BuffConfig, BuffRepository
- **Phase 2 (12 tests)**: BuffOperation, MultiBattleInstance 集成

### 测试策略

1. **配置加载测试** - JSON 序列化/反序列化
2. **仓库管理测试** - 注册、查询、筛选
3. **BuffOperation 测试** - 创建、优先级、向后兼容
4. **集成测试** - 端到端 buff 应用流程

---

## 九、性能考虑

### 优化措施

1. **单例模式** - BuffRepository.Instance 避免重复加载
2. **内存缓存** - BuffConfig 在内存中缓存，无需重复查询
3. **延迟加载** - JSON 文件仅加载一次
4. **浅拷贝** - ToBuffInstance 仅拷贝必要字段

### 性能基准

- **BuffRepository.GetBuffById**: O(1) 字典查询
- **BuffConfig.ToBuffInstance**: O(n) 其中 n = Effects.Count
- **内存占用**: ~10KB (10个默认 buff)

---

## 十、未来扩展

### 可扩展点

1. **动态加载** - 从服务器加载自定义 buff
2. **Buff 组合** - 支持 buff 之间的依赖关系
3. **条件触发** - 基于条件的 buff 激活
4. **Buff 链** - 一个 buff 触发另一个 buff
5. **UI 增强** - Buff 编辑器，可视化配置

### 兼容性

- ✅ 向后兼容 inline BuffTemplate
- ✅ 不影响现有测试
- ✅ 不需要数据库迁移
- ✅ 不需要 API 更改

---

## 十一、常见问题 (FAQ)

### Q1: 如何添加新的 Buff？

A: 在 `buffs.json` 中添加新条目，或在代码中调用 `BuffRepository.Instance.RegisterBuff(config)`。

### Q2: BuffConfigId 和 BuffTemplate 可以同时使用吗？

A: 可以，但 BuffConfigId 优先级更高。建议只使用其中一个。

### Q3: 如何覆盖 Buff 的默认目标？

A: 使用 `BuffOperation.ApplyByConfigId(id, targetOverride)`。

### Q4: Buff 配置支持热重载吗？

A: 当前不支持。需要重启应用。未来可以添加动态重载功能。

### Q5: 如何处理 BuffConfig 不存在的情况？

A: `ApplyBuffOperation` 会记录警告并优雅返回，不会抛出异常。

### Q6: 性能开销如何？

A: 极小。BuffRepository 使用内存缓存，查询为 O(1)。ToBuffInstance 开销可忽略。

---

## 十二、更新日志

### v1.0 (2025-11-14)

- ✅ Phase 1: 实现 BuffConfig, BuffRepository, buffs.json
- ✅ Phase 2: BuffOperation 支持 BuffConfigId
- ✅ Phase 2: MultiBattleInstance 集成
- ✅ 23 + 12 = 35 单元测试全部通过
- ✅ 所有 334 个测试通过

---

## 十三、相关文档

- [Step1_设计方案.md](./docs_step1_Step1-设计方案.md) - 整体 Buff 系统设计
- [Step1_实施进度追踪.md](./Step1_实施进度追踪.md) - 实施进度
- [buffs.json](../../BlazorIdle.Shared/Config/buffs.json) - 配置文件

---

**维护者**: @copilot  
**审核**: @Solaireshen97  
**最后更新**: 2025-11-14
