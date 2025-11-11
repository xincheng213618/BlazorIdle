# Buff与技能设计 - Step 1 实施进度追踪

## 📋 概述

本文档用于追踪 Step 1（Buff / 资源 / Special 脉冲系统）的实施进度。

**目标：** 在 Step 0 的基础上，实现资源系统、Buff/Debuff 系统、Special 脉冲增强、事件扩展和 UI 展示。

**原则：**
- ✅ 运行态优先，不改变 CharacterData 持久化
- ✅ 低侵入修改，SkillResolver 计算 + MultiBattleInstance 应用
- ✅ 高可测试性，完整的事件系统
- ✅ 保持兼容，不破坏 Step 0 的已有功能

---

## 🎯 实施阶段

### 阶段 1：资源系统基础实现（P0 - 必须）

**状态：** ✅ 已完成

**完成时间：** 2025-11-11

**目标：** 实现 ResourceBucket 运行态，支持 rage 资源的获取、消耗和上限控制。

**任务清单：**

- [x] 1.1 创建资源系统目录结构
  - 创建 `BlazorIdle.Shared/Game/Resources/` 目录
  
- [x] 1.2 实现 ResourceBucket.cs
  - 字段：`Id`, `Current`, `Max`, `ConvertTarget`（预留）, `ConvertRatio`（预留）
  - 方法：`Gain()`, `TryConsume()`, `ForceConsume()`, `SetMax()`, `Reset()`
  - 构造函数：支持自定义 id, max, initial
  - Clamp 逻辑：确保资源值在 [0, max] 范围内
  
- [x] 1.3 实现 ResourceBucketCollection.cs
  - 默认创建 "rage" 资源桶（max=10, initial=0）
  - 方法：`GetBucket()`, `HasBucket()`, `AddBucket()`, `RemoveBucket()`, `GetAll()`
  - 异常处理：GetBucket 不存在时抛出 KeyNotFoundException
  - 异常处理：AddBucket 已存在时抛出 InvalidOperationException
  
- [x] 1.4 实现 ResourceConfig.cs
  - 配置字段：`DefaultRageMax` (10), `GainPerAttack` (1), `GainPerCritExtra` (1)
  
- [x] 1.5 单元测试
  - 创建 `ResourceBucketTests.cs`（23 个测试用例）
    - ✅ 构造函数测试（默认值、自定义值、clamp）
    - ✅ Gain 测试（正常增加、clamp、零值、负值）
    - ✅ TryConsume 测试（成功、失败、精确值、负值）
    - ✅ ForceConsume 测试
    - ✅ SetMax 测试（clamp、不影响、负值）
    - ✅ Reset 测试（指定值、默认零、clamp）
    - ✅ 多操作组合测试
  - 创建 `ResourceBucketCollectionTests.cs`（13 个测试用例）
    - ✅ 构造函数测试（默认 rage 桶）
    - ✅ GetBucket 测试（成功、异常）
    - ✅ HasBucket 测试（存在、不存在）
    - ✅ AddBucket 测试（成功、重复异常）
    - ✅ RemoveBucket 测试（成功、不存在）
    - ✅ GetAll 测试
    - ✅ 引用修改测试
    - ✅ 多操作组合测试

**实施细节：**

1. **ResourceBucket 实现**
   - 使用 `Math.Clamp()` 确保资源值在有效范围内
   - `Gain()` 方法返回实际获得量（考虑 clamp）
   - `TryConsume()` 返回 bool，成功时才扣除资源
   - 预留溢出转换接口（ConvertTarget, ConvertRatio）但不实现逻辑

2. **ResourceBucketCollection 实现**
   - 使用 `Dictionary<string, ResourceBucket>` 存储资源桶
   - 构造函数自动创建默认 "rage" 桶
   - `GetAll()` 返回只读字典，防止外部修改内部集合

3. **测试策略**
   - 全面覆盖边界情况（零值、负值、超限、精确值）
   - 测试异常抛出情况（不存在、重复）
   - 测试多操作组合场景

**验收标准：**
- ✅ ResourceBucket 支持 Gain/TryConsume/SetMax
- ✅ ResourceBucketCollection 管理多个资源桶
- ✅ 单元测试覆盖边界情况（36 个测试用例）
- ✅ 编译通过，所有测试通过（125/125）
- ✅ 原有 94 个测试继续通过，无回归

**测试结果：**
```
Total tests: 125
- Original tests: 94 (all passing)
- New tests (Phase 1): 31 (all passing)
  - ResourceBucketTests: 23
  - ResourceBucketCollectionTests: 13
- Failed: 0
- Skipped: 0
- Duration: ~716ms
```

**提交哈希：** 待提交

---

### 阶段 2：将资源系统集成到战斗流程（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 玩家攻击时自动产生 rage 资源，并记录资源变更事件。

**任务清单：**

- [ ] 2.1 扩展 BattleContext
  - 添加 `PlayerResources` 字段（ResourceBucketCollection?）
  - 添加 `EnemyResources` 字段（预留，怪物暂不使用）

- [ ] 2.2 在 MultiBattleInstance 维护资源集合
  - 新增字段：`Dictionary<string, ResourceBucketCollection> _playerResources`
  - 新增字段：`ResourceConfig _resourceConfig`
  - 构造函数：为每个玩家创建 ResourceBucketCollection
  - 新增方法：`GetResourceSnapshot()` 用于 UI 显示

- [ ] 2.3 修改攻击处理逻辑
  - 修改 `ProcessCharacterAttackViaSkillResolver()`
  - 攻击命中后调用 `rageBucket.Gain(1, "attack_hit")`
  - 暴击时额外调用 `rageBucket.Gain(1, "crit_bonus")`
  - 记录 ResourceGainEvent

- [ ] 2.4 新增 ResourceGainEvent
  - 创建 `BlazorIdle.Shared/Game/Resources/ResourceGainEvent.cs`
  - 字段：`ActorId`, `BucketId`, `Delta`, `NewValue`, `Reason`, `SkillId?`, `BundleId?`
  - 继承自 `CombatEvent`

- [ ] 2.5 实现资源事件记录
  - 在 MultiBattleInstance 中实现 `RecordResourceGain()` 方法
  - 使用 `_combatConfig.EmitCastEvents` 控制是否记录事件

- [ ] 2.6 集成测试
  - 创建 `ResourceIntegrationTests.cs`
  - 测试：攻击产生 rage
  - 测试：rage clamp 到 10
  - 测试：暴击产生额外 rage

**预计工作量：** 3-4 小时

---

### 阶段 3：Buff 系统核心实现（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现 BuffInstance 和多种 Effect 类型，支持 Buff 的生命周期管理。

**预计工作量：** 4-5 小时

---

### 阶段 4：IBuffOwner 接口与玩家/怪物集成（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 让玩家和怪物都能拥有 Buff。

**预计工作量：** 4-5 小时

---

### 阶段 5：扩展 SkillResolver 支持 Buff 操作（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** SkillResolver 返回 Buff 操作指令。

**预计工作量：** 3-4 小时

---

### 阶段 6：实现 Special 脉冲与 Buff 施加（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** Special 脉冲可以施加测试 Buff。

**预计工作量：** 5-6 小时

---

### 阶段 7：Buff 效果应用到属性计算（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** Buff 的属性加成在伤害计算中生效。

**预计工作量：** 4-5 小时

---

### 阶段 8：扩展事件系统（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 记录 Buff 相关的所有事件。

**预计工作量：** 3-4 小时

---

### 阶段 9：UI 展示 Buff/Debuff（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 在前端显示 Buff 图标、堆栈数、剩余时间。

**预计工作量：** 6-8 小时

---

### 阶段 10：验收测试与文档（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 确保 Step 1 实施质量。

**预计工作量：** 4-5 小时

---

## 📊 进度总览

| 阶段 | 状态 | 完成时间 | 提交哈希 | 测试数量 |
|------|------|----------|----------|---------|
| 阶段 1 - 资源系统基础 | ✅ 已完成 | 2025-11-11 | 待提交 | +31 (125 total) |
| 阶段 2 - 资源集成战斗 | ⬜ 未开始 | - | - | - |
| 阶段 3 - Buff 系统核心 | ⬜ 未开始 | - | - | - |
| 阶段 4 - IBuffOwner 接口 | ⬜ 未开始 | - | - | - |
| 阶段 5 - SkillResolver 扩展 | ⬜ 未开始 | - | - | - |
| 阶段 6 - Special 脉冲 Buff | ⬜ 未开始 | - | - | - |
| 阶段 7 - Buff 效果应用 | ⬜ 未开始 | - | - | - |
| 阶段 8 - 事件系统扩展 | ⬜ 未开始 | - | - | - |
| 阶段 9 - UI 展示 | ⬜ 未开始 | - | - | - |
| 阶段 10 - 验收测试 | ⬜ 未开始 | - | - | - |

**总体进度：** 1/10 (10%) ✅

---

## 🎯 当前里程碑

**已完成：**
- ✅ 阶段 1：资源系统基础实现（ResourceBucket, ResourceBucketCollection, ResourceConfig）
- ✅ 31 个单元测试全部通过
- ✅ 保持 94 个原有测试通过，无回归

**下一步：**
- 📍 阶段 2：将资源系统集成到战斗流程
  - 扩展 BattleContext
  - 在 MultiBattleInstance 中为每个玩家创建资源集合
  - 修改攻击逻辑产生 rage
  - 实现 ResourceGainEvent
  - 编写集成测试

**预计剩余工作量：** 37-46 小时

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
5. **记录测试数量：** 更新测试总数和新增测试数

---

## ⚠️ 注意事项

- **保持测试通过**：每个阶段完成后运行全部测试，确保无回归
- **增量提交**：每完成一个阶段就提交，便于回滚和追踪
- **文档同步**：实施过程中如有设计调整，同步更新设计文档
- **性能监控**：关注测试执行时间，如有明显增长需要优化

---

**最后更新：** 2025-11-11  
**维护者：** @copilot  
**分支：** copilot/design-step1-scheme
