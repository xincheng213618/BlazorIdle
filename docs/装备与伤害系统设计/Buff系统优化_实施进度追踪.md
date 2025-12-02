# Buff系统优化 - 实施进度追踪

## 📋 概述

本文档用于追踪 Buff 系统优化的实施进度。

**目标：** 使 Buff 系统能够正确影响新伤害管线的所有属性，且 Buff 效果不受属性上限裁剪。

**核心公式：**
```
最终属性 = 职业属性 + Clamp(装备属性) + Buff效果
```

**原则：**
- ✅ Buff 效果支持所有 `CombatStats` 属性
- ✅ **Buff 效果不受上限裁剪**
- ✅ **职业属性不受上限裁剪**
- ✅ **仅装备属性受上限裁剪**（单独裁剪）
- ✅ 保持向后兼容，支持旧的属性名称
- ✅ 建立清晰的 Buff 效果应用流程

**设计文档：**
- [Buff系统优化_设计提案.md](./Buff系统优化_设计提案.md)
- [Step6_伤害组成与计算管线.md](./Step6_伤害组成与计算管线.md) (v1.3)
- [角色属性系统_设计提案.md](./角色属性系统_设计提案.md)

---

## 📁 代码位置规划

### 新增文件

| 文件路径 | 说明 | 阶段 |
|----------|------|------|
| `Game/Buffs/BuffStatMapper.cs` | 属性名称映射（新增） | Phase 1 |
| `Game/Buffs/BuffStatApplier.cs` | Buff 效果应用（新增） | Phase 1 |

### 修改文件

| 文件路径 | 说明 | 阶段 |
|----------|------|------|
| `Game/Combat/CharacterStatsCalculator.cs` | 分离装备属性裁剪 | Phase 2 |
| `Game/Skills/SkillResolver.cs` | 集成 Buff 应用 | Phase 2 |
| `Game/MultiBattleInstance.cs` | 急速等属性 Buff 应用 | Phase 2 |
| `Config/buffs/*.json` | 更新 Buff 配置使用新属性名 | Phase 3 |

---

## 🎯 实施阶段

### 阶段 1：属性映射与兼容层（P0 - 必须）

**状态：** ✅ 已完成

**目标：** 建立属性名称映射，保证旧 Buff 配置兼容。

**任务清单：**

- [x] 1.1 创建 `BuffStatMapper` 类
  - [x] 位置：BlazorIdle.Shared/Game/Buffs/BuffStatMapper.cs
  - [x] 旧属性名映射表
    - `DamagePerAttack` → `AttackFinal`
    - `DamageReduction` → `DamageReductionPercent`
    - `CritMultiplier` → `CritDamageBonusPercent`（需值转换）
    - `SpecialDamage` → `AttackFinal`（旧特殊技能伤害）
  - [x] `NormalizeStatName(string)` 方法
  - [x] `NeedsValueConversion(string, out Func<double, double>)` 方法
  - [x] `IsValidStatName(string)` 验证方法
  - [x] 单元测试（17个测试）

- [x] 1.2 创建 `BuffStatApplier` 类
  - [x] 位置：BlazorIdle.Shared/Game/Buffs/BuffStatApplier.cs
  - [x] `ApplyBuffsToCombatStats(CombatStats, IBuffOwner)` 方法
  - [x] 支持所有 CombatStats 属性：
    - `AttackFinal` (int)
    - `AttackPercent` (double)
    - `SpecialAttackPercent` (double)
    - `HPPercent` (double)
    - `HastePercent` (double)
    - `CritChancePercent` (double)
    - `CritDamageBonusPercent` (double)
    - `FortifyMaxPercent` (double)
    - `BackwaterMaxPercent` (double)
    - `ChasePercent` (double)
    - `KenChasePercent` (double)
    - `ChaseFlat` (int)
    - `DamageReductionPercent` (double)
  - [x] 实现三种效果类型
    - `StatMultiplier`: `value × (1 + effectValue)`
    - `StatAdditive`: `value + effectValue`
    - `StatReduction`: `value × (1 - effectValue)`
  - [x] 支持层数叠加（按 Stacks 循环应用）
  - [x] 按 `AppliedAtMs` 排序应用
  - [x] 单元测试（30个测试）

**验收标准：**
- [x] 旧 Buff 配置（使用 DamagePerAttack）仍能正常工作
- [x] 新属性名（AttackPercent 等）正确映射
- [x] Buff 效果不受上限裁剪

**预估工作量：** 1-2 小时

---

### 阶段 2：伤害管线集成（P0 - 必须）

**状态：** 📋 待开始

**目标：** 将 Buff 效果应用到伤害计算流程。

**任务清单：**

- [ ] 2.1 修改 SkillResolver
  - [ ] 位置：BlazorIdle.Shared/Game/Skills/SkillResolver.cs
  - [ ] 在 `Cast()` 方法中：
    - [ ] 获取基础 CombatStats（已过装备上限裁剪）
    - [ ] 调用 `BuffStatApplier.ApplyBuffsToCombatStats()` 应用 Buff
    - [ ] 使用 buffed stats 创建 DamageContext
  - [ ] 移除旧的 `ApplyBuffEffects()` 调用（或保留兼容）
  - [ ] 确保 Buff 效果影响所有管线层级

- [ ] 2.2 验证伤害计算各层
  - [ ] 基础层：`AttackFinal` Buff 正确影响基础伤害
  - [ ] 主体层：`AttackPercent`, `SpecialAttackPercent` Buff 正确乘入
  - [ ] 暴击层：`CritChancePercent`, `CritDamageBonusPercent` Buff 正确影响暴击
  - [ ] 追击层：`ChasePercent`, `KenChasePercent`, `ChaseFlat` Buff 正确影响追击
  - [ ] 减伤层：`DamageReductionPercent` Buff 正确影响减伤

- [ ] 2.3 更新 MultiBattleInstance
  - [ ] 位置：BlazorIdle.Shared/Game/MultiBattleInstance.cs
  - [ ] 急速 Buff 效果（`HastePercent`）正确影响攻速
  - [ ] 生命 Buff 效果（`HPPercent`）正确影响最大生命

- [ ] 2.4 更新 CharacterStatsCalculator（如需要）
  - [ ] 位置：BlazorIdle.Shared/Game/Combat/CharacterStatsCalculator.cs
  - [ ] 确保 Buff 效果与属性计算流程协调

**验收标准：**
- [ ] 攻击% Buff 正确影响主体层伤害
- [ ] 暴击伤害 Buff 正确影响暴击倍率
- [ ] 所有属性 Buff 正确生效
- [ ] Buff 效果不受上限裁剪（超过上限仍然有效）

**预估工作量：** 2-3 小时

---

### 阶段 3：配置更新与兼容（P1 - 重要）

**状态：** 📋 待开始

**目标：** 更新 Buff 配置文件使用新属性名，同时保持兼容。

**任务清单：**

- [ ] 3.1 更新 Buff 配置文件
  - [ ] Config/buffs/warrior.json
    - [ ] `DamagePerAttack` → `AttackFinal`
    - [ ] 添加新属性 Buff 示例
  - [ ] Config/buffs/mage.json
  - [ ] Config/buffs/rogue.json
  - [ ] Config/buffs/ranger.json
  - [ ] Config/buffs/common.json
    - [ ] `DamageReduction` → `DamageReductionPercent`
    - [ ] `CritMultiplier` → `CritDamageBonusPercent`
  - [ ] Config/buffs/consumable.json
  - [ ] Config/buffs/debuffs.json
  - [ ] Config/buffs/monster.json

- [ ] 3.2 添加新属性的 Buff 示例
  - [ ] `AttackPercent` Buff 示例（主体层加成）
  - [ ] `SpecialAttackPercent` Buff 示例（特攻加成）
  - [ ] `ChasePercent` Buff 示例（追击加成）
  - [ ] `FortifyMaxPercent` / `BackwaterMaxPercent` Buff 示例（态势加成）

- [ ] 3.3 更新 BuffRepository 验证
  - [ ] 位置：BlazorIdle.Shared/Game/Buffs/BuffRepository.cs
  - [ ] 在 `ValidateBuffConfigurations()` 中添加属性名有效性检查
  - [ ] 对使用旧属性名的配置输出警告（不阻止加载）

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
    - [ ] 旧属性名映射测试
    - [ ] 值转换测试（CritMultiplier）
    - [ ] 无效属性名处理测试
  - [ ] BuffStatApplier 测试
    - [ ] StatMultiplier 效果测试
    - [ ] StatAdditive 效果测试
    - [ ] StatReduction 效果测试
    - [ ] 多 Buff 叠加测试
    - [ ] 层数叠加测试
    - [ ] 时间排序测试

- [ ] 4.2 集成测试
  - [ ] Buff + 伤害管线集成测试
    - [ ] AttackFinal Buff 影响基础层
    - [ ] AttackPercent Buff 影响主体层
    - [ ] CritDamageBonusPercent Buff 影响暴击层
    - [ ] ChasePercent Buff 影响追击层
    - [ ] DamageReductionPercent Buff 影响减伤层
  - [ ] 超过上限的 Buff 效果测试
    - [ ] 装备 100% + Buff 20% = 有效 120%
  - [ ] 多 Buff 叠加战斗测试

- [ ] 4.3 回归测试
  - [ ] 旧 Buff 配置兼容性测试
  - [ ] 现有战斗逻辑不受影响
  - [ ] 现有测试套件通过

**验收标准：**
- [ ] 所有新增测试通过
- [ ] 现有测试套件通过
- [ ] 无回归问题

**预估工作量：** 2-3 小时

---

## 📊 总体进度

| 阶段 | 状态 | 预估 | 实际 |
|------|------|------|------|
| Phase 1: 属性映射与兼容层 | ✅ 已完成 | 1-2h | 1h |
| Phase 2: 伤害管线集成 | 📋 待开始 | 2-3h | - |
| Phase 3: 配置更新与兼容 | 📋 待开始 | 1-2h | - |
| Phase 4: 测试与验证 | 📋 待开始 | 2-3h | - |
| **总计** | **1/4 (25%)** | **6-10h** | **1h** |

---

## 🔧 技术要点

### 属性上限规则

```
最终属性 = 职业属性 + Clamp(装备属性) + Buff效果

关键规则：
- 职业属性不受上限裁剪
- 仅装备部分的属性受上限裁剪（单独裁剪）
- Buff 效果不受上限裁剪，直接叠加
```

### 属性映射表

| 旧属性名 | 新属性名 | 值转换 |
|---------|---------|--------|
| `DamagePerAttack` | `AttackFinal` | 无 |
| `SpecialDamage` | `AttackFinal` | 无 |
| `DamageReduction` | `DamageReductionPercent` | 无 |
| `CritMultiplier` | `CritDamageBonusPercent` | `(v-1)*100` |

### 效果叠加公式

| 效果类型 | 公式 | 示例 |
|---------|------|------|
| StatMultiplier | `base × (1 + value)` | 100 × 1.15 = 115 |
| StatAdditive | `base + value` | 100 + 15 = 115 |
| StatReduction | `base × (1 - value)` | 100 × 0.85 = 85 |

### 应用顺序

1. Buff 按 `AppliedAtMs`（应用时间）排序
2. 同一 Buff 的多层按 Stacks 循环应用
3. 先乘法后加法（如果有多种效果类型）

---

## 📝 变更日志

### 2025-12-02 v1.1
- 修正属性上限公式：`最终属性 = 职业属性 + Clamp(装备属性) + Buff效果`
- 明确仅装备属性受上限裁剪，职业属性不受影响
- 添加 CharacterStatsCalculator 修改到 Phase 2

### 2025-12-02 v1.0
- 初始版本
- 基于设计提案创建实施文档
- 规划 4 个实施阶段
- 预估总工时：6-10 小时

---

**最后更新：** 2025-12-02 v1.0  
**维护者：** @copilot  
**状态：** 📋 待开始
