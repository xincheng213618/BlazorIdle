# Buff系统优化设计提案

版本：v1.0  
日期：2025-12-02  
作者：@copilot  
状态：📋 待确认

---

## 📋 概述

本文档描述 Buff 系统优化设计，使 Buff 系统能够正确影响新伤害管线的所有属性。

### 设计目标
1. Buff 效果支持所有 `CombatStats` 属性
2. **Buff 效果不受上限裁剪影响**（上限仅限制装备属性汇总）
3. 保持向后兼容，支持旧的属性名称（如 `DamagePerAttack`）
4. 建立清晰的 Buff 效果应用流程

---

## 🎯 核心设计原则

### 属性上限规则

```
最终属性 = Clamp(职业属性 + 装备属性) + Buff效果

关键规则：
- 装备部分的属性汇总受上限裁剪（如 AttackPercent 上限 100%）
- Buff 效果不受上限裁剪，直接叠加到最终属性
- 例如：玩家装备 AttackPercent = 108% → 裁剪为 100%
       再获得 20% 攻击力 Buff → 最终按 120% 计算
```

### 属性来源公式

```
最终战斗属性 = 
    Clamp(职业基础属性 + 装备基础属性 + 装备词条属性)  // 受上限裁剪
    + Buff 加成                                        // 不受上限裁剪
    + (未来)天赋加成                                   // 规划中
```

---

## 📊 支持的属性列表

### Buff 应支持的所有 CombatStats 属性

| 属性名（新） | 属性名（旧/兼容） | 类型 | 说明 | 伤害管线层级 |
|-------------|------------------|------|------|-------------|
| `AttackFinal` | `DamagePerAttack` | int | 基础攻击力 | 基础层 |
| `AttackPercent` | - | double | 攻击力%加成 | 主体层 |
| `SpecialAttackPercent` | - | double | 特攻%加成 | 主体层 |
| `HPPercent` | - | double | 生命%加成 | 面板（不入伤害） |
| `HastePercent` | - | double | 急速%加成 | 攻速计算 |
| `CritChancePercent` | - | double | 暴击率% | 暴击层 |
| `CritDamageBonusPercent` | `CritMultiplier`¹ | double | 暴击伤害加成% | 暴击层 |
| `FortifyMaxPercent` | - | double | 盛体态势上限% | 主体层（态势） |
| `BackwaterMaxPercent` | - | double | 背水态势上限% | 主体层（态势） |
| `ChasePercent` | - | double | 追击%加成 | 追击层 |
| `KenChasePercent` | - | double | 克制追击%加成 | 追击层 |
| `ChaseFlat` | - | int | 固定追击伤害 | 追击层 |
| `DamageReductionPercent` | `DamageReduction` | double | 减伤% | 减伤层 |

> ¹ `CritMultiplier` 旧值为倍率（如 1.2），新系统使用加成百分比（如 20%），需要转换

### 额外支持的非面板属性

| 属性名 | 类型 | 说明 |
|--------|------|------|
| `SpecialDamage` | int | 特殊技能伤害（兼容旧系统） |

---

## 🔧 实施方案

### Phase 1：属性映射层

创建 `BuffStatMapper` 来处理属性名称映射和兼容性：

```csharp
/// <summary>
/// Buff 属性映射器 - 处理旧属性名到新属性名的映射
/// </summary>
public static class BuffStatMapper
{
    // 旧属性名 → 新属性名映射
    private static readonly Dictionary<string, string> LegacyMapping = new()
    {
        { "DamagePerAttack", "AttackFinal" },
        { "DamageReduction", "DamageReductionPercent" },
        { "CritMultiplier", "CritDamageBonusPercent" }  // 需要值转换
    };

    /// <summary>
    /// 获取规范化的属性名（处理旧名称兼容）
    /// </summary>
    public static string NormalizeStatName(string statName)
    {
        if (LegacyMapping.TryGetValue(statName, out var newName))
            return newName;
        return statName;
    }

    /// <summary>
    /// 检查是否需要值转换（如 CritMultiplier 1.2 → 20%）
    /// </summary>
    public static bool NeedsValueConversion(string statName, out Func<double, double>? converter)
    {
        if (statName == "CritMultiplier")
        {
            // 1.2 倍率 → 20% 加成
            converter = v => (v - 1.0) * 100;
            return true;
        }
        converter = null;
        return false;
    }
}
```

### Phase 2：Buff 效果应用器

修改 `SkillResolver` 中的 Buff 应用逻辑，支持所有属性：

```csharp
/// <summary>
/// 应用 Buff 效果到战斗属性
/// Buff 效果不受上限裁剪
/// </summary>
public static CombatStats ApplyBuffsToCombatStats(
    CombatStats baseStats, 
    IBuffOwner buffOwner)
{
    // 复制基础属性（已经过装备上限裁剪）
    var modified = CloneStats(baseStats);

    // 按应用时间排序 Buff
    var sortedBuffs = buffOwner.Buffs.Values
        .OrderBy(b => b.AppliedAtMs)
        .ToList();

    foreach (var buff in sortedBuffs)
    {
        foreach (var effect in buff.Effects)
        {
            // 跳过非属性效果
            if (effect.Type == BuffEffectType.ForceCrit ||
                effect.Type == BuffEffectType.DamageOverTime ||
                effect.Type == BuffEffectType.HealOverTime)
                continue;

            // 规范化属性名
            var statName = BuffStatMapper.NormalizeStatName(effect.Target);
            
            // 获取效果值（处理值转换）
            double effectValue = effect.Value;
            if (BuffStatMapper.NeedsValueConversion(effect.Target, out var converter))
            {
                effectValue = converter!(effectValue);
            }

            // 应用效果到对应属性（不受上限裁剪）
            ApplyEffectToStat(modified, statName, effect.Type, effectValue, buff.Stacks);
        }
    }

    return modified;
}

private static void ApplyEffectToStat(
    CombatStats stats, 
    string statName, 
    BuffEffectType effectType, 
    double value,
    int stacks)
{
    // 根据属性名应用效果
    switch (statName)
    {
        case "AttackFinal":
            stats.AttackFinal = ApplyEffect(stats.AttackFinal, effectType, value, stacks);
            break;
        case "AttackPercent":
            stats.AttackPercent = ApplyEffectDouble(stats.AttackPercent, effectType, value, stacks);
            break;
        case "SpecialAttackPercent":
            stats.SpecialAttackPercent = ApplyEffectDouble(stats.SpecialAttackPercent, effectType, value, stacks);
            break;
        case "CritChancePercent":
            stats.CritChancePercent = ApplyEffectDouble(stats.CritChancePercent, effectType, value, stacks);
            break;
        case "CritDamageBonusPercent":
            stats.CritDamageBonusPercent = ApplyEffectDouble(stats.CritDamageBonusPercent, effectType, value, stacks);
            break;
        case "HastePercent":
            stats.HastePercent = ApplyEffectDouble(stats.HastePercent, effectType, value, stacks);
            break;
        case "HPPercent":
            stats.HPPercent = ApplyEffectDouble(stats.HPPercent, effectType, value, stacks);
            break;
        case "FortifyMaxPercent":
            stats.FortifyMaxPercent = ApplyEffectDouble(stats.FortifyMaxPercent, effectType, value, stacks);
            break;
        case "BackwaterMaxPercent":
            stats.BackwaterMaxPercent = ApplyEffectDouble(stats.BackwaterMaxPercent, effectType, value, stacks);
            break;
        case "ChasePercent":
            stats.ChasePercent = ApplyEffectDouble(stats.ChasePercent, effectType, value, stacks);
            break;
        case "KenChasePercent":
            stats.KenChasePercent = ApplyEffectDouble(stats.KenChasePercent, effectType, value, stacks);
            break;
        case "ChaseFlat":
            stats.ChaseFlat = ApplyEffect(stats.ChaseFlat, effectType, value, stacks);
            break;
        case "DamageReductionPercent":
            stats.DamageReductionPercent = ApplyEffectDouble(stats.DamageReductionPercent, effectType, value, stacks);
            break;
    }
}

private static int ApplyEffect(int baseValue, BuffEffectType type, double value, int stacks)
{
    double result = baseValue;
    for (int i = 0; i < stacks; i++)
    {
        result = type switch
        {
            BuffEffectType.StatMultiplier => result * (1 + value),
            BuffEffectType.StatAdditive => result + value,
            BuffEffectType.StatReduction => result * (1 - value),
            _ => result
        };
    }
    return (int)Math.Floor(result);
}

private static double ApplyEffectDouble(double baseValue, BuffEffectType type, double value, int stacks)
{
    double result = baseValue;
    for (int i = 0; i < stacks; i++)
    {
        result = type switch
        {
            BuffEffectType.StatMultiplier => result * (1 + value),
            BuffEffectType.StatAdditive => result + value,
            BuffEffectType.StatReduction => result * (1 - value),
            _ => result
        };
    }
    return result;
}
```

### Phase 3：伤害计算集成

修改 `SkillResolver.Cast()` 方法，在创建 `DamageContext` 前应用 Buff：

```csharp
// 1. 获取基础战斗属性（已过装备上限裁剪）
var baseStats = ctx.AttackerCombatStats ?? CombatStats.CreateDefault();

// 2. 应用 Buff 效果（不受上限裁剪）
var buffedStats = casterBuffOwner != null 
    ? BuffStatApplier.ApplyBuffsToCombatStats(baseStats, casterBuffOwner)
    : baseStats;

// 3. 使用 buffedStats 创建 DamageContext
var damageCtx = new DamageContext
{
    AttackFinal = buffedStats.AttackFinal,
    SkillCoef = skillCoef,
    SkillFlat = skillFlat,
    AttackerStats = buffedStats,  // 包含 Buff 加成的属性
    // ...其他属性
};
```

---

## 📁 代码位置规划

| 文件 | 位置 | 说明 | 阶段 |
|------|------|------|------|
| BuffStatMapper.cs | Game/Buffs/ | 属性名称映射（新增） | P1 |
| BuffStatApplier.cs | Game/Buffs/ | Buff 效果应用（新增） | P2 |
| SkillResolver.cs | Game/Skills/ | 集成 Buff 应用（修改） | P3 |
| DamageCalculator.cs | Game/Combat/ | 验证无需修改 | P3 |

---

## 📋 实施阶段

### 阶段 1：属性映射与兼容层（P0 - 必须）

**状态：** 📋 待开始

**目标：** 建立属性名称映射，保证旧 Buff 配置兼容。

**任务清单：**

- [ ] 1.1 创建 `BuffStatMapper` 类
  - [ ] 旧属性名映射（DamagePerAttack → AttackFinal 等）
  - [ ] 值转换逻辑（CritMultiplier 倍率 → 百分比）
  - [ ] 单元测试

- [ ] 1.2 创建 `BuffStatApplier` 类
  - [ ] 支持所有 CombatStats 属性
  - [ ] 实现三种效果类型（Multiplier/Additive/Reduction）
  - [ ] 支持层数叠加
  - [ ] 单元测试

**验收标准：**
- [ ] 旧 Buff 配置（使用 DamagePerAttack）仍能正常工作
- [ ] 新属性名（AttackPercent 等）正确映射
- [ ] Buff 效果不受上限裁剪

**预估工作量：** 1-2 小时

---

### 阶段 2：伤害管线集成（P0 - 必须）

**状态：** 📋 待开始

**目标：** 将 Buff 效果应用到伤害计算流程。

**任务清单：**

- [ ] 2.1 修改 SkillResolver
  - [ ] 在创建 DamageContext 前调用 BuffStatApplier
  - [ ] 移除旧的 ApplyBuffEffects 调用
  - [ ] 确保 Buff 效果影响所有管线层级

- [ ] 2.2 验证伤害计算
  - [ ] 基础层（AttackFinal）
  - [ ] 主体层（AttackPercent, SpecialAttackPercent）
  - [ ] 暴击层（CritChancePercent, CritDamageBonusPercent）
  - [ ] 追击层（ChasePercent, KenChasePercent, ChaseFlat）
  - [ ] 减伤层（DamageReductionPercent）

- [ ] 2.3 更新 MultiBattleInstance
  - [ ] 急速 Buff 效果（HastePercent）
  - [ ] 其他战斗属性 Buff 效果

**验收标准：**
- [ ] 攻击% Buff 正确影响主体层伤害
- [ ] 暴击伤害 Buff 正确影响暴击倍率
- [ ] 所有属性 Buff 正确生效

**预估工作量：** 2-3 小时

---

### 阶段 3：配置更新与兼容（P1 - 重要）

**状态：** 📋 待开始

**目标：** 更新 Buff 配置文件使用新属性名，同时保持兼容。

**任务清单：**

- [ ] 3.1 更新 Buff 配置文件
  - [ ] warrior.json
  - [ ] mage.json
  - [ ] rogue.json
  - [ ] ranger.json
  - [ ] common.json
  - [ ] consumable.json

- [ ] 3.2 添加新属性的 Buff 示例
  - [ ] AttackPercent Buff 示例
  - [ ] SpecialAttackPercent Buff 示例
  - [ ] ChasePercent Buff 示例

- [ ] 3.3 更新 BuffRepository 验证
  - [ ] 验证新属性名有效性
  - [ ] 警告使用旧属性名的配置

**验收标准：**
- [ ] 旧配置继续工作（向后兼容）
- [ ] 新配置使用新属性名
- [ ] 启动时有兼容性警告

**预估工作量：** 1-2 小时

---

### 阶段 4：测试与验证（P0 - 必须）

**状态：** 📋 待开始

**目标：** 确保 Buff 系统正确工作。

**任务清单：**

- [ ] 4.1 单元测试
  - [ ] BuffStatMapper 测试
  - [ ] BuffStatApplier 测试
  - [ ] 各属性效果测试

- [ ] 4.2 集成测试
  - [ ] Buff + 伤害管线集成测试
  - [ ] 超过上限的 Buff 效果测试
  - [ ] 多 Buff 叠加测试

- [ ] 4.3 回归测试
  - [ ] 旧 Buff 配置兼容性测试
  - [ ] 现有战斗逻辑不受影响

**验收标准：**
- [ ] 所有测试通过
- [ ] 无回归问题

**预估工作量：** 2-3 小时

---

## 📊 总体进度

| 阶段 | 状态 | 预估 | 实际 |
|------|------|------|------|
| Phase 1: 属性映射与兼容层 | 📋 待开始 | 1-2h | - |
| Phase 2: 伤害管线集成 | 📋 待开始 | 2-3h | - |
| Phase 3: 配置更新与兼容 | 📋 待开始 | 1-2h | - |
| Phase 4: 测试与验证 | 📋 待开始 | 2-3h | - |
| **总计** | **0/4 (0%)** | **6-10h** | **-** |

---

## 🔍 示例：Buff 效果计算

### 场景：玩家装备 108% 攻击加成 + 获得 20% 攻击 Buff

```
装备属性汇总：
- 职业初始 AttackPercent = 0%
- 装备词条 AttackPercent = 108%
- 汇总后 AttackPercent = 108%
- 上限裁剪后 AttackPercent = 100%（上限为 100%）

获得 Buff：
- 攻击力 +20% Buff（StatAdditive, target: "AttackPercent", value: 20）

最终属性：
- AttackPercent = 100%（装备部分，已裁剪）+ 20%（Buff，不裁剪）= 120%

伤害计算（主体层）：
- AfterMain = AfterVariance × (1 + 120%/100) × ...
- 即 AfterMain = AfterVariance × 2.2 × ...
```

### 场景：多个 Buff 叠加

```
基础属性（装备裁剪后）：
- AttackPercent = 80%
- CritDamageBonusPercent = 40%

Buff 1: 攻击力 +15%（StatAdditive, AttackPercent, 15）
Buff 2: 攻击力 ×1.1（StatMultiplier, AttackPercent, 0.1）
Buff 3: 暴击伤害 +20%（StatAdditive, CritDamageBonusPercent, 20）

应用顺序（按 AppliedAtMs 排序）：
1. AttackPercent = 80 + 15 = 95
2. AttackPercent = 95 × 1.1 = 104.5
3. CritDamageBonusPercent = 40 + 20 = 60

最终属性：
- AttackPercent = 104.5%（超过上限但有效）
- CritDamageBonusPercent = 60%（超过上限但有效）
```

---

## 📝 Buff 配置迁移指南

### 旧配置（兼容但不推荐）

```json
{
  "effects": [
    {
      "type": "StatMultiplier",
      "target": "DamagePerAttack",  // 旧属性名
      "value": 0.15
    }
  ]
}
```

### 新配置（推荐）

```json
{
  "effects": [
    {
      "type": "StatMultiplier",
      "target": "AttackFinal",  // 新属性名
      "value": 0.15
    }
  ]
}
```

### 新属性示例

```json
// 攻击%加成 Buff
{
  "type": "StatAdditive",
  "target": "AttackPercent",
  "value": 20.0
}

// 特攻%加成 Buff
{
  "type": "StatAdditive",
  "target": "SpecialAttackPercent",
  "value": 15.0
}

// 追击%加成 Buff
{
  "type": "StatAdditive",
  "target": "ChasePercent",
  "value": 10.0
}

// 暴击伤害加成 Buff
{
  "type": "StatAdditive",
  "target": "CritDamageBonusPercent",
  "value": 25.0
}
```

---

## ⚠️ 注意事项

### 上限规则澄清

1. **装备属性**：受 `CombatCapsConfig` 上限裁剪
2. **Buff 效果**：**不受**上限裁剪，直接叠加
3. **天赋效果**（未来）：规划中，可能也不受上限裁剪

### 向后兼容

1. 旧的 `DamagePerAttack` 等属性名继续工作
2. 旧的 `CritMultiplier` 值（倍率格式）会自动转换为百分比
3. 建议逐步迁移到新属性名

### 效果叠加顺序

1. Buff 按 `AppliedAtMs`（应用时间）排序
2. 同一 Buff 的多层按层数循环应用
3. 乘法效果使用 `(1 + value)` 形式，避免负值问题

---

## 📝 变更日志

### 2025-12-02 v1.0
- 初始版本
- 基于需求分析创建设计文档
- 规划 4 个实施阶段
- 预估总工时：6-10 小时

---

**设计待确认，确认后开始实施。**
