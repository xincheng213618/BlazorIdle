# Buff与技能设计 - Step 2 实施进度追踪

## 📋 概述

本文档用于追踪 Step 2（技能系统）的实施进度。

**目标：** 在 Step 0 和 Step 1 的基础上，实现完整的技能系统，包括主动/被动技能、瞬发/施法技能、触发类技能、目标选择、资源消耗/冷却管理、Window-GCD 机制和技能配置化管理。

**原则：**
- ✅ 基于 Step 1 的资源和 Buff 系统构建
- ✅ 配置驱动，通过 skills.json 管理所有技能
- ✅ 统一接口，使用 SkillResolver 处理所有技能施放
- ✅ 高可测试性，完整的事件系统
- ✅ 保持兼容，不破坏 Step 0 和 Step 1 的已有功能

---

## 🎯 实施阶段

### 阶段 1：技能配置基础设施（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 建立技能配置文件加载、验证和管理的基础设施。

**任务清单：**

- [ ] 1.1 扩展 SkillDef 数据模型
  - 添加 type: "active" | "passive"
  - 添加 slotType: "active" | "passive"
  - 添加 fixed: bool（固定技能标识）
  - 添加 releaseType: "instant" | "cast"
  - 添加 castTimeSec?: number
  - 添加 isGcd: bool（窗口互斥标识）
  - 添加 allowCoTriggerAfterCast?: bool
  - 添加 targetPolicy: string（目标选择策略）
  - 添加 unlock?: UnlockConfig（解锁条件）
  - 添加 allowedProfessions?: string[]（职业限制）
  - 添加 conditions?: SkillConditions（施放条件）
  - 添加 triggers?: TriggerDef[]（触发器定义）

- [ ] 1.2 创建技能配置支持类型
  - 创建 UnlockConfig 类（minLevel, accountFlags, requiresProfessionLevel）
  - 创建 SkillConditions 类（hpBelowPct, hpAbovePct, requireBuffId, forbidBuffId, requireResource）
  - 创建 TriggerDef 类（when, procChance, fireSkillId, priority, conditions, overrides）
  - 创建 TargetPolicy 枚举（CurrentTarget, EnemiesAll, AlliesLowestHpPct, Self, AlliesAll）

- [ ] 1.3 创建 skills.json 配置文件
  - 位置：Shared/Config/skills.json
  - 包含战士固定技能（warrior_attack_basic, warrior_special_pulse, warrior_slam）
  - 包含战士可配置技能（warrior_mortal_strike, warrior_rend, warrior_thunderclap）
  - 包含战士被动触发（warrior_proc_passive, warrior_proc_strike）
  - 包含共享技能（adrenaline_rush）
  - 包含法师测试技能（mage_pyroblast）

- [ ] 1.4 扩展 SkillRepository 配置加载
  - 实现 LoadFromJson(string json) 方法
  - 实现 TryLoadFromEmbeddedJson() 方法（从嵌入资源加载）
  - 添加配置验证（ValidateSkillConfigurations）
  - 验证必填字段、引用完整性（buffConfigId 存在）
  - 验证 fireSkillId 引用存在

- [ ] 1.5 服务端 API 扩展
  - GameConfigResponse 添加 Skills 字段
  - IGameConfigProvider 接口添加 Skills 属性
  - GameConfigProvider 加载并暴露 skills.json
  - GameConfigController /all endpoint 包含技能配置

- [ ] 1.6 客户端 GameConfigService 扩展
  - 添加 _skills 字段和属性
  - 从 API 响应加载技能配置
  - IGameConfigService 接口同步更新

- [ ] 1.7 单元测试（15 个）
  - SkillDef 构造测试（3 个）
  - UnlockConfig/SkillConditions/TriggerDef 测试（4 个）
  - SkillRepository 加载测试（4 个）
  - 配置验证测试（4 个）

**验收标准：**
- ✅ skills.json 配置文件格式正确
- ✅ SkillRepository 成功加载配置
- ✅ 配置验证能够捕获错误
- ✅ API 正确传递技能配置到客户端
- ✅ 15 个单元测试全部通过
- ✅ 所有 363 个原有测试继续通过

**预计工作量：** 3-4 小时

---

### 阶段 2：技能槽位系统（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现角色的技能槽位管理，支持固定技能和可配置技能。

**任务清单：**

- [ ] 2.1 创建技能槽位数据模型
  - 创建 SkillSlotConfig 类（slotId, slotType, skillId）
  - 创建 CharacterSkillSlots 类（管理角色的技能槽位）
  - 每个职业默认 3 个主动槽 + 1 个被动槽

- [ ] 2.2 扩展 Character 实体
  - 添加 EquippedSkills: CharacterSkillSlots（运行态）
  - 添加 InitializeSkillSlots(profession) 方法
  - 自动装配固定技能（fixed=true）

- [ ] 2.3 技能装配验证
  - 验证职业限制（allowedProfessions）
  - 验证解锁条件（unlock）
  - 验证槽位类型匹配（active 技能到 active 槽）
  - 防止重复装配同一技能

- [ ] 2.4 战斗中技能查询
  - 实现 GetActiveSkills() 方法（按槽位顺序返回）
  - 实现 GetPassiveSkills() 方法
  - 实现 GetFixedSkills() 方法
  - 实现 GetConfigurableSkills() 方法

- [ ] 2.5 单元测试（12 个）
  - SkillSlotConfig 测试（2 个）
  - CharacterSkillSlots 测试（4 个）
  - 技能装配验证测试（4 个）
  - 技能查询测试（2 个）

**验收标准：**
- ✅ 角色能够管理技能槽位
- ✅ 固定技能自动装配
- ✅ 可配置技能可以装配/卸载
- ✅ 装配验证正确工作
- ✅ 12 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 2-3 小时

---

### 阶段 3：目标选择系统（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现 5 种目标选择策略，支持单体和 AoE 技能。

**任务清单：**

- [ ] 3.1 实现目标选择核心方法
  - 创建 TargetSelector 类
  - 实现 ResolveTargets(TargetPolicy, BattleContext) 方法
  - 支持 CurrentTarget（当前普攻目标）
  - 支持 EnemiesAll（所有存活敌人）
  - 支持 AlliesLowestHpPct（HP 百分比最低的友方）
  - 支持 Self（自身）
  - 支持 AlliesAll（所有友方）

- [ ] 3.2 处理目标缺失情况
  - CurrentTarget 无目标 → 返回空列表
  - EnemiesAll 无敌人 → 返回空列表
  - AlliesLowestHpPct 使用 HP 百分比排序
  - 记录目标解析失败的诊断信息

- [ ] 3.3 集成到 SkillResolver
  - 修改 Cast() 方法支持 targetPolicy 参数
  - 根据目标列表处理单体/AoE 技能
  - AoE 技能对所有目标计算独立伤害

- [ ] 3.4 单元测试（15 个）
  - CurrentTarget 测试（3 个）
  - EnemiesAll 测试（3 个）
  - AlliesLowestHpPct 测试（3 个）
  - Self 测试（2 个）
  - AlliesAll 测试（2 个）
  - 目标缺失处理测试（2 个）

**验收标准：**
- ✅ 所有 5 种目标选择策略正确实现
- ✅ AlliesLowestHpPct 使用 HP 百分比（不是绝对值）
- ✅ 目标缺失优雅处理
- ✅ AoE 技能正确作用于所有目标
- ✅ 15 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 3-4 小时

---

## 📊 进度总览

| 阶段 | 状态 | 预计工时 | 测试增量 |
|------|------|---------|---------|
| 阶段 1 - 技能配置基础设施 | ⬜ 未开始 | 3-4h | +15 |
| 阶段 2 - 技能槽位系统 | ⬜ 未开始 | 2-3h | +12 |
| 阶段 3 - 目标选择系统 | ⬜ 未开始 | 3-4h | +15 |
| 阶段 4 - 技能条件判定 | ⬜ 未开始 | 3-4h | +18 |
| 阶段 5 - 资源消耗与冷却 | ⬜ 未开始 | 3-4h | +15 |
| 阶段 6 - Window-GCD 机制 | ⬜ 未开始 | 5-6h | +20 |
| 阶段 7 - 触发类技能系统 | ⬜ 未开始 | 4-5h | +18 |
| 阶段 8 - 施法技能集成 | ⬜ 未开始 | 4-5h | +15 |
| 阶段 9 - AutoCastEngine | ⬜ 未开始 | 5-6h | +20 |
| 阶段 10 - UI 技能显示 | ⬜ 未开始 | 6-8h | - |
| 阶段 11 - 集成测试验收 | ⬜ 未开始 | 6-8h | +30 |
| 阶段 12 - 文档与交付 | ⬜ 未开始 | 4-5h | - |

**总体进度：** 0/12 (0%) ⬜  
**预计总工时：** 48-62 小时  
**预计新增测试：** ~178 个

---

**最后更新：** 2025-11-14  
**维护者：** @copilot  
**状态：** 待用户确认
