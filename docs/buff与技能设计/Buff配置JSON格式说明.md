# Buff 配置 JSON 格式说明

**版本**: v1.0  
**日期**: 2025-11-14  
**文件位置**: `BlazorIdle.Shared/Config/buffs.json`

## 概述

本文档详细说明 `buffs.json` 配置文件的格式、字段含义、验证规则和最佳实践。

## 文件结构

buffs.json 是一个包含 buff 配置对象的 JSON 数组：

```json
[
  {
    "id": "buff_unique_id",
    "name": "显示名称",
    "description": "效果描述",
    "icon": "🔥",
    "kind": "Buff",
    "durationSec": 10.0,
    "tickIntervalSec": 2.0,
    "stackingPolicy": "Stack",
    "maxStacks": 3,
    "defaultTarget": "Self",
    "effects": [
      {
        "type": "StatMultiplier",
        "target": "DamagePerAttack",
        "value": 0.15
      }
    ]
  }
]
```

## 字段说明

### 必填字段

#### `id` (string, 必填)
- **说明**: Buff 的唯一标识符
- **格式**: 小写字母、数字和下划线，建议使用蛇形命名法
- **示例**: `"warrior_rage_boost"`, `"mage_burn_dot"`, `"damage_boost"`
- **规则**: 
  - 不能为空
  - 必须在整个配置文件中唯一
  - 推荐命名规范：`<类型>_<效果>_<类别>`

#### `name` (string, 必填)
- **说明**: Buff 的显示名称（用户界面）
- **格式**: 任意字符串
- **示例**: `"狂暴"`, `"燃烧"`, `"伤害增幅"`
- **规则**: 不能为空

#### `kind` (string, 必填)
- **说明**: Buff 类型（增益或减益）
- **可选值**: 
  - `"Buff"` - 增益效果（正面）
  - `"Debuff"` - 减益效果（负面）
- **示例**: `"Buff"`, `"Debuff"`
- **规则**: 必须是枚举值之一

#### `effects` (array, 必填)
- **说明**: Buff 产生的效果列表
- **格式**: Effect 对象数组
- **规则**: 
  - 数组不能为空（至少需要一个效果）
  - 每个效果对象必须包含 `type` 字段
- **示例**: 见"效果类型"章节

### 可选字段

#### `description` (string, 可选)
- **说明**: Buff 的详细描述
- **示例**: `"增加攻击力、攻击速度和暴击率"`
- **默认值**: `null`

#### `icon` (string, 可选)
- **说明**: UI 显示的图标（emoji 或 CSS 类名）
- **示例**: `"⚔️"`, `"🔥"`, `"💚"`
- **默认值**: `null`

#### `durationSec` (number, 可选)
- **说明**: Buff 持续时间（秒）
- **格式**: 正浮点数
- **示例**: `10.0`, `6.5`, `15.0`
- **特殊值**: 
  - `null` 或未设置 = 永久 buff
  - `0.1` = 极短时间（用于瞬发效果）
- **规则**: 如果设置，必须 > 0

#### `tickIntervalSec` (number, 可选)
- **说明**: DoT/HoT 效果的触发间隔（秒）
- **格式**: 正浮点数
- **示例**: `1.0`, `2.0`, `3.0`
- **用途**: 仅用于 `DamageOverTime` 和 `HealOverTime` 效果
- **默认值**: `null`（不触发周期效果）
- **规则**: 如果设置，必须 > 0

#### `stackingPolicy` (string, 可选)
- **说明**: Buff 重复应用时的行为
- **可选值**:
  - `"Refresh"` - 刷新持续时间（默认）
  - `"Stack"` - 叠加效果
  - `"Ignore"` - 忽略新的应用
- **示例**: `"Stack"`, `"Refresh"`
- **默认值**: `"Refresh"`

#### `maxStacks` (number, 可选)
- **说明**: 最大叠加层数
- **格式**: 非负整数
- **示例**: `1`, `3`, `5`
- **特殊值**:
  - `0` = 无限叠加（不推荐）
  - `1` = 不可叠加（单一实例）
- **默认值**: `0`
- **规则**: 必须 >= 0

#### `defaultTarget` (string, 可选)
- **说明**: Buff 应用的默认目标
- **可选值**:
  - `"Self"` - 施法者自己（默认）
  - `"Target"` - 技能目标
  - `"AllEnemies"` - 所有敌人
  - `"AllAllies"` - 所有友方
  - `"RandomEnemy"` - 随机敌人
  - `"LowestHpAlly"` - 生命值最低的友方
- **示例**: `"Self"`, `"AllEnemies"`
- **默认值**: `"Self"`
- **注意**: 技能可以通过 `BuffOperation.TargetOverride` 覆盖此值

## 效果类型 (Effects)

每个 buff 可以包含一个或多个效果。每个效果对象的结构如下：

### StatMultiplier（属性乘法）

增加或减少属性的百分比值（乘法效果）。

```json
{
  "type": "StatMultiplier",
  "target": "DamagePerAttack",
  "value": 0.15
}
```

- **target** (必填): 目标属性名称
  - `"DamagePerAttack"` - 攻击伤害
  - `"CritMultiplier"` - 暴击倍率
  - `"DamageReduction"` - 伤害减免
- **value** (必填): 乘法系数
  - `0.15` = +15%
  - `0.50` = +50%
  - `-0.20` = -20%（减益）

### StatAdditive（属性加法）

增加或减少属性的绝对值（加法效果）。

```json
{
  "type": "StatAdditive",
  "target": "HastePercent",
  "value": 10.0
}
```

- **target** (必填): 目标属性名称
  - `"HastePercent"` - 急速百分比
  - `"CritChancePercent"` - 暴击率百分比
- **value** (必填): 加法值
  - `10.0` = +10%（注意：10.0 表示 10%，不是 0.10）
  - `5.0` = +5%
  - `-15.0` = -15%（减益）

**重要**: HastePercent 和 CritChancePercent 使用整数形式（10.0 = 10%），不是小数形式（0.10）。

### StatReduction（属性减少）

减少目标属性值（专用减益效果）。

```json
{
  "type": "StatReduction",
  "target": "DamagePerHit",
  "value": 0.20
}
```

- **target** (必填): 目标属性名称
  - `"DamagePerAttack"` - 攻击伤害
  - `"DamagePerHit"` - 命中伤害
- **value** (必填): 减少百分比
  - `0.20` = -20%
  - `0.50` = -50%

### DamageOverTime（持续伤害 DoT）

造成周期性伤害。

```json
{
  "type": "DamageOverTime",
  "amountPerTick": 8
}
```

- **amountPerTick** (必填): 每次触发的伤害量
- **注意**: 需要配合 `tickIntervalSec` 字段使用
- **示例**: `amountPerTick: 8` + `tickIntervalSec: 2.0` = 每2秒造成8点伤害

### HealOverTime（持续治疗 HoT）

提供周期性治疗。

```json
{
  "type": "HealOverTime",
  "amountPerTick": 5
}
```

- **amountPerTick** (必填): 每次触发的治疗量
- **注意**: 需要配合 `tickIntervalSec` 字段使用
- **示例**: `amountPerTick: 5` + `tickIntervalSec: 2.0` = 每2秒恢复5点生命

### InstantHeal（瞬间治疗）

立即恢复生命值。

```json
{
  "type": "InstantHeal",
  "amountPerTick": 20
}
```

- **amountPerTick** (必填): 立即治疗量
- **注意**: 通常配合极短的 duration（如 0.1 秒）使用

### ForceCrit（强制暴击）

下次攻击必定暴击。

```json
{
  "type": "ForceCrit"
}
```

- **无需额外参数**
- **用途**: 特殊技能效果

## 完整示例

### 示例 1: 战士狂暴（多重效果）

```json
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
      "value": 10.0
    },
    {
      "type": "StatAdditive",
      "target": "CritChancePercent",
      "value": 5.0
    }
  ]
}
```

### 示例 2: 燃烧 DoT（可叠加）

```json
{
  "id": "burning",
  "name": "燃烧",
  "description": "持续造成火焰伤害（可叠加）",
  "icon": "🔥",
  "kind": "Debuff",
  "durationSec": 10.0,
  "tickIntervalSec": 2.0,
  "stackingPolicy": "Stack",
  "maxStacks": 5,
  "defaultTarget": "AllEnemies",
  "effects": [
    {
      "type": "DamageOverTime",
      "amountPerTick": 8
    }
  ]
}
```

### 示例 3: 瞬间治疗

```json
{
  "id": "instant_heal",
  "name": "瞬间治疗",
  "description": "立即恢复生命值",
  "icon": "💚",
  "kind": "Buff",
  "durationSec": 0.1,
  "stackingPolicy": "Refresh",
  "maxStacks": 1,
  "defaultTarget": "Self",
  "effects": [
    {
      "type": "InstantHeal",
      "amountPerTick": 20
    }
  ]
}
```

### 示例 4: 虚弱减益

```json
{
  "id": "weakened",
  "name": "虚弱",
  "description": "降低敌人伤害",
  "icon": "😰",
  "kind": "Debuff",
  "durationSec": 8.0,
  "stackingPolicy": "Refresh",
  "maxStacks": 1,
  "defaultTarget": "AllEnemies",
  "effects": [
    {
      "type": "StatReduction",
      "target": "DamagePerHit",
      "value": 0.20
    }
  ]
}
```

## 验证规则

系统启动时会自动验证所有 buff 配置，检查项包括：

### 基本验证
- ✅ `id` 不能为空
- ✅ `name` 不能为空
- ✅ `effects` 数组不能为空
- ✅ `id` 必须在文件中唯一

### 逻辑验证
- ✅ `maxStacks` 必须 >= 0
- ✅ `durationSec` 如果设置，必须 > 0
- ✅ `tickIntervalSec` 如果设置，必须 > 0
- ✅ `kind` 必须是 "Buff" 或 "Debuff"
- ✅ `stackingPolicy` 必须是有效的枚举值
- ✅ `defaultTarget` 必须是有效的枚举值

### 验证失败处理
如果验证失败，系统会：
1. 在控制台输出详细错误信息
2. 列出所有验证失败的 buff
3. 继续加载其他有效的 buff（不会崩溃）

## 常见问题 (FAQ)

### Q1: maxStacks = 0 表示什么？
**A**: 表示无限叠加。但通常不推荐使用，建议设置一个合理的上限（如 5 或 10）以避免性能问题。

### Q2: maxStacks = 1 是否表示不可叠加？
**A**: 是的。`maxStacks = 1` 表示只能有一个实例，新的应用会根据 `stackingPolicy` 决定行为（刷新、忽略等）。

### Q3: HastePercent 应该用 0.10 还是 10.0？
**A**: **使用 10.0**。系统内部会将其除以 100，所以 `10.0` 表示 10%，`15.0` 表示 15%。

### Q4: 如何创建永久 buff？
**A**: 不设置 `durationSec` 字段，或设置为 `null`。

### Q5: tickIntervalSec 什么时候需要设置？
**A**: 仅当 buff 包含 `DamageOverTime` 或 `HealOverTime` 效果时需要设置。其他效果类型不需要。

### Q6: 可以让一个 buff 同时有多个效果吗？
**A**: 可以！`effects` 数组可以包含多个效果对象。例如，战士狂暴同时增加伤害、急速和暴击率。

### Q7: defaultTarget 和技能的目标有什么关系？
**A**: `defaultTarget` 是 buff 的默认目标。技能可以通过 `BuffOperation.TargetOverride` 覆盖这个值，提供更灵活的控制。

### Q8: 如何调试配置错误？
**A**: 
1. 查看控制台输出，系统会显示验证信息
2. 查找 `[BuffRepository]` 标签的日志
3. 确认看到 "✅ Successfully loaded X buffs from buffs.json"

## 最佳实践

### 命名规范
- **id**: 使用蛇形命名法，描述性强
  - ✅ `warrior_rage_boost`, `mage_burn_dot`
  - ❌ `buff1`, `test`, `x`

### 叠加策略
- **增益 buff**: 通常使用 `"Refresh"` 或 `"Ignore"`
- **DoT/HoT**: 使用 `"Stack"` 允许多层伤害/治疗
- **控制效果**: 使用 `"Ignore"` 避免重复应用

### 持续时间设置
- **短期爆发**: 3-8 秒
- **中期增益**: 10-20 秒
- **长期 buff**: 30-60 秒
- **瞬发效果**: 0.1 秒

### 效果平衡
- **伤害增幅**: 10-30%
- **急速加成**: 10-25%
- **暴击率**: 5-15%
- **DoT 伤害**: 每秒 3-10 点

## 技术参考

### JSON Schema（未来支持）
计划添加 JSON Schema 支持以启用 IDE 自动补全和验证。

### 相关文件
- **配置文件**: `BlazorIdle.Shared/Config/buffs.json`
- **数据模型**: `BlazorIdle.Shared/Models/BuffConfig.cs`
- **加载器**: `BlazorIdle.Shared/Game/Buffs/BuffRepository.cs`
- **使用示例**: `BlazorIdle.Shared/Game/Skills/SkillRepository.cs`

### 相关文档
- [Buff配置化管理说明.md](./Buff配置化管理说明.md) - 系统架构和使用指南
- [Step1_实施进度追踪.md](./Step1_实施进度追踪.md) - 实施进度和历史

---

**版本历史**
- v1.0 (2025-11-14) - 初始版本，基于 P0/P1 修复后的系统
