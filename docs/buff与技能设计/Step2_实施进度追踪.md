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

**状态：** ✅ 已完成

**目标：** 建立技能配置文件加载、验证和管理的基础设施。

**任务清单：**

- [x] 1.1 扩展 SkillDef 数据模型 ✅
  - ✅ 添加 type: "active" | "passive"
  - ✅ 添加 slotType: "active" | "passive"
  - ✅ 添加 fixed: bool（固定技能标识）
  - ✅ 添加 releaseType: "instant" | "cast"
  - ✅ 添加 castTimeSec?: number
  - ✅ 添加 isGcd: bool（窗口互斥标识）
  - ✅ 添加 allowCoTriggerAfterCast?: bool
  - ✅ 添加 targetPolicy: string（目标选择策略）
  - ✅ 添加 unlock?: UnlockConfig（解锁条件）
  - ✅ 添加 allowedProfessions?: string[]（职业限制）
  - ✅ 添加 conditions?: SkillConditions（施放条件）
  - ✅ 添加 triggers?: TriggerDef[]（触发器定义）
  - ✅ 额外添加：name, description, damage (DamageDef), costs/gains (List格式)

- [x] 1.2 创建技能配置支持类型 ✅
  - ✅ 创建 UnlockConfig 类（minLevel, accountFlags, requiresProfessionLevel）
  - ✅ 创建 SkillConditions 类（hpBelowPct, hpAbovePct, requireBuffId, forbidBuffId, requireResource）
  - ✅ 创建 TriggerDef 类（when, procChance, fireSkillId, priority, conditions, ignoreRequirements）
  - ✅ 创建 TargetPolicy 枚举（CurrentTarget, EnemiesAll, AlliesLowestHpPct, Self, AlliesAll）
  - ✅ 额外创建：DamageDef, ResourceCost, ResourceGain

- [x] 1.3 创建 skills.json 配置文件 ✅
  - ✅ 位置：BlazorIdle.Shared/Config/skills.json
  - ✅ 包含战士固定技能（warrior_attack_basic, warrior_special_pulse, warrior_slam）
  - ✅ 包含战士可配置技能（warrior_mortal_strike, warrior_rend, warrior_thunderclap, warrior_battle_shout, warrior_cleave, warrior_desperate_strike, warrior_check_stance）
  - ✅ 包含战士被动触发（warrior_proc_passive, warrior_proc_strike）
  - ✅ 包含共享技能（adrenaline_rush）
  - ✅ 包含法师测试技能（mage_pyroblast）
  - ✅ 额外包含：完整的法师技能库（10个），盗贼技能库（11个），游侠技能库（10个）
  - ✅ **总计：44个技能定义**

- [x] 1.4 扩展 SkillRepository 配置加载 ✅
  - ✅ 实现 LoadFromJson(string json) 方法
  - ✅ 实现 TryLoadFromEmbeddedJson() 方法（从嵌入资源自动加载）
  - ✅ 添加配置验证（ValidateSkillConfigurations）
  - ✅ 验证必填字段（Id, Name）
  - ✅ 验证 fireSkillId 引用存在
  - ✅ 验证逻辑约束（cooldownSec >= 0, castTimeSec >= 0）
  - ✅ 额外实现：GetSkillsByProfession(), GetSkillById()

- [x] 1.5 服务端 API 扩展 ⚠️ **不需要**
  - ⚠️ 架构决策：使用嵌入资源模式（与BuffRepository一致）
  - ⚠️ SkillRepository在构造时自动从嵌入的skills.json加载
  - ⚠️ 无需通过API传输，客户端和服务端共享同一assembly
  - ⚠️ 此任务标记为**不适用**（架构上不需要）

- [x] 1.6 客户端 GameConfigService 扩展 ⚠️ **不需要**
  - ⚠️ 架构决策：客户端直接使用SkillRepository
  - ⚠️ 与BuffRepository模式一致，无需通过GameConfigService
  - ⚠️ 此任务标记为**不适用**（架构上不需要）

- [x] 1.7 单元测试（15 个）✅
  - ✅ SkillDef 构造测试（3 个）
  - ✅ UnlockConfig/SkillConditions/TriggerDef/DamageDef 测试（4 个）
  - ✅ SkillRepository 加载测试（4 个）
  - ✅ 配置验证测试（4 个）
  - ✅ **测试结果：378个测试全部通过（363原有 + 15新增）**

**验收标准：**
- ✅ skills.json 配置文件格式正确
- ✅ SkillRepository 成功加载配置（从嵌入资源）
- ✅ 配置验证能够捕获错误
- ✅ 技能配置在客户端和服务端可用（通过嵌入资源，无需API）
- ✅ 15 个单元测试全部通过
- ✅ 所有 363 个原有测试继续通过

**实际工作量：** 3-4 小时

**实施说明：**
- 采用嵌入资源加载模式，与现有BuffRepository架构保持一致
- SkillRepository在构造函数中自动调用TryLoadFromEmbeddedJson()加载skills.json
- 无需单独的API传输层，简化架构，提高性能
- 保持向后兼容性：支持Dictionary和List两种资源格式
- 完成日期：2025-11-15

---

### 阶段 2：技能槽位系统（P0 - 必须）

**状态：** ✅ 已完成

**目标：** 实现角色的技能槽位管理，支持固定技能和可配置技能。

**任务清单：**

- [x] 2.1 创建技能槽位数据模型 ✅
  - ✅ 创建 SkillSlotConfig 类（slotId, slotType, skillId, isFixed）
  - ✅ 创建 CharacterSkillSlots 类（管理角色的技能槽位）
  - ✅ 每个职业默认 3 个主动槽 + 1 个被动槽

- [x] 2.2 扩展 Character 实体 ✅
  - ✅ CharacterSkillSlots 提供完整运行态管理（注：Character 实体集成将在后续阶段完成）
  - ✅ 实现 Initialize(profession) 方法（创建默认槽位）
  - ✅ 实现 AutoEquipFixedSkills() 方法（自动装配固定技能）

- [x] 2.3 技能装配验证 ✅
  - ✅ 验证职业限制（allowedProfessions）
  - ✅ 验证解锁条件（unlock - MinLevel）
  - ✅ 验证槽位类型匹配（active 技能到 active 槽）
  - ✅ 防止重复装配同一技能
  - ✅ 固定槽位保护（IsFixed 标记，不可修改）

- [x] 2.4 战斗中技能查询 ✅
  - ✅ 实现 GetActiveSkills() 方法（按槽位顺序返回）
  - ✅ 实现 GetPassiveSkills() 方法
  - ✅ 实现 GetFixedSkills() 方法
  - ✅ 实现 GetConfigurableSkills() 方法
  - ✅ 额外实现：GetSlot(), GetAllSlots() 辅助方法

- [x] 2.5 单元测试（14 个）✅
  - ✅ SkillSlotConfig 测试（2 个）
  - ✅ CharacterSkillSlots 测试（4 个）
  - ✅ 技能装配验证测试（6 个：职业、槽位类型、重复、固定槽位、解锁条件×2）
  - ✅ 技能查询测试（2 个）
  - ✅ **测试结果：392个测试全部通过（378原有 + 14新增）**

**验收标准：**
- ✅ 角色能够管理技能槽位
- ✅ 固定技能自动装配
- ✅ 可配置技能可以装配/卸载
- ✅ 装配验证正确工作
- ✅ 14 个单元测试全部通过（超出要求）
- ✅ 所有原有测试继续通过

**实际工作量：** 2-3 小时

**实施说明：**
- SkillSlotConfig 和 CharacterSkillSlots 提供完整的槽位管理功能
- 固定技能通过 IsFixed 标记保护，无法卸载
- 装配验证包括职业限制、解锁条件（MinLevel）、槽位类型、重复检查
- 查询方法按槽位顺序返回，便于战斗系统集成
- EquipSkill 支持可选的 characterLevel 参数用于等级验证
- 完成日期：2025-11-15

**代码审查结果：**
- ✅ 所有设计文档要求已实现
- ✅ 代码质量：A级（优秀）
- ⚠️ 改进建议：考虑在后续阶段优化 characterLevel 传递方式
- ⚠️ Phase 2.5 待实现：AccountFlags 和 RequiresProfessionLevel 验证

---

### 阶段 2.5：技能学习与装备系统（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现技能学习和装备的持久化管理。

**任务清单：**

- [ ] 2.5.1 扩展 CharacterData 数据模型
  - 添加 LearnedSkills 字段（HashSet<string>）
  - 添加 EquippedSkillsByProfession 字段（Dictionary<string, EquippedSkillsConfig>）

- [ ] 2.5.2 创建 EquippedSkillsConfig 类
  - ActiveSlots 字典（3个主动槽位：active_1, active_2, active_3）
  - PassiveSlot 字符串（1个被动槽位）
  - ProfessionId 字符串

- [ ] 2.5.3 实现 SkillLearningManager
  - CanLearnSkill() 方法（检查等级、职业、已学习）
  - LearnSkill() 方法
  - GetLearnableSkills() 方法
  - GetLearnedSkills() 方法

- [ ] 2.5.4 实现 SkillEquipmentManager
  - EquipSkill() 方法（验证槽位类型、职业限制）
  - UnequipSkill() 方法
  - GetEquippedSkills() 方法
  - InitializeFixedSkills() 方法（自动装配固定技能）
  - **固定技能初始化策略（防御式设计）：**
    * 在任何访问技能槽位的方法中，先检查当前职业配置是否存在
    * 如果不存在或为空，自动调用 InitializeFixedSkills() 初始化
    * 这样确保无论在何种情况下（创建、切换、加载），固定技能都能正确初始化
    * 建议在以下方法开头调用检查：
      - GetEquippedSkills() - 获取装备技能时检查
      - EquipSkill() - 装备技能时检查
      - Character 构造函数 - 创建时检查
      - ChangeProfession() - 切换职业时检查

- [ ] 2.5.5 数据持久化
  - 确保 CharacterData 序列化包含新字段
  - 测试存档加载和保存

- [ ] 2.5.6 单元测试（18 个）
  - SkillLearningManager 测试（8 个）
  - SkillEquipmentManager 测试（8 个）
  - 持久化测试（2 个）

**验收标准：**
- ✅ CharacterData 正确序列化技能数据
- ✅ 技能学习条件正确判定（等级、职业）
- ✅ 技能装备验证正确工作
- ✅ 固定技能自动初始化
- ✅ 18 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 3-4 小时

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

### 阶段 4：技能条件判定（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现技能施放前的条件检查系统。

**任务清单：**

- [ ] 4.1 实现 ConditionChecker 类
  - CheckConditions(skill, context) 方法
  - CheckHpCondition(hpBelowPct, hpAbovePct) 检查
  - CheckBuffCondition(requireBuffId, forbidBuffId) 检查
  - CheckResourceCondition(requireResource) 检查

- [ ] 4.2 HP 百分比条件
  - hpBelowPct: 当前 HP% < 指定值时可用
  - hpAbovePct: 当前 HP% > 指定值时可用
  - 支持同时检查上下限

- [ ] 4.3 Buff 条件
  - requireBuffId: 必须有指定 buff
  - forbidBuffId: 禁止有指定 buff
  - 支持多个 buff 条件（AND 逻辑）

- [ ] 4.4 资源条件
  - requireResource: 检查资源桶是否满足要求
  - 支持多个资源条件

- [ ] 4.5 集成到技能系统
  - AutoCastEngine 施放前检查条件
  - 条件不满足时跳过技能
  - 记录条件检查失败的诊断信息

- [ ] 4.6 单元测试（18 个）
  - HP 条件测试（6 个）
  - Buff 条件测试（6 个）
  - 资源条件测试（4 个）
  - 综合条件测试（2 个）

**验收标准：**
- ✅ HP 百分比条件正确判定
- ✅ Buff 条件正确判定
- ✅ 资源条件正确判定
- ✅ 多个条件 AND 逻辑正确
- ✅ 18 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 3-4 小时

---

### 阶段 5：资源消耗与冷却（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现技能资源消耗和冷却时间管理，包括 InstantHeal 修复。

**任务清单：**

- [ ] 5.1 实现 CooldownManager 类
  - 管理所有技能的冷却状态
  - IsReady(skillId) 方法
  - StartCooldown(skillId, duration) 方法
  - TickCooldowns(deltaTime) 方法

- [ ] 5.2 资源消耗验证
  - CheckResourceCost(skill, context) 方法
  - 支持多个资源消耗（costs 数组）
  - 瞬发技能：施放时消耗资源
  - 施法技能：施法开始时消耗资源

- [ ] 5.3 资源获得处理
  - ApplyResourceGains(skill, context) 方法
  - 支持多个资源获得（gains 数组）
  - 技能命中后获得资源

- [ ] 5.4 **InstantHeal 修复**
  - 移除 BuffEffectType.InstantHeal 枚举值
  - 移除 BuffEffect.InstantHeal() 工厂方法
  - 移除 BuffInstance.HasInstantHeal() 方法
  - 移除 BuffInstance.GetInstantHealAmount() 方法
  - 确认 SkillDef.InstantHeal 字段保留（作为技能直接效果）
  - 确认 ApplyInstantHeal() 方法正确（直接治疗，不创建buff）
  - 更新相关单元测试

- [ ] 5.5 集成到技能系统
  - 技能施放前检查资源和冷却
  - 技能施放后扣除资源、启动冷却
  - 技能命中后应用资源获得

- [ ] 5.6 单元测试（15 个）
  - CooldownManager 测试（4 个）
  - 资源消耗测试（4 个）
  - 资源获得测试（3 个）
  - InstantHeal 修复测试（4 个）

**验收标准：**
- ✅ 冷却时间正确管理
- ✅ 资源消耗时机正确（瞬发 vs 施法）
- ✅ 资源获得正确应用
- ✅ InstantHeal 作为技能直接效果，不创建buff
- ✅ HoT 继续作为buff效果正确工作
- ✅ 15 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 4-5 小时（包含 InstantHeal 修复 1-2h）

---

### 阶段 6：Window-GCD 机制（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现 Window-GCD 窗口互斥机制，支持 3 个触发窗口。

**任务清单：**

- [ ] 6.1 定义窗口类型
  - PreAttack 窗口：普攻前，用于施法技能
  - PostAttack 窗口：普攻后，用于瞬发技能
  - PostCast 窗口：施法完成后，用于追加瞬发技能

- [ ] 6.2 实现 WindowExecutor 类
  - ExecuteWindow(window, context) 方法
  - 获取窗口可用技能列表
  - 按优先级排序（槽位顺序）
  - 执行第一个满足条件的 GCD 技能
  - 执行所有满足条件的非 GCD 技能

- [ ] 6.3 Window-GCD 规则
  - 施法技能：固定 isGcd=true
  - 瞬发技能：可以是 GCD 或非 GCD
  - 同窗口 GCD 技能互斥（只触发一个）
  - 非 GCD 技能可以共触发

- [ ] 6.4 allowCoTriggerAfterCast 支持
  - 施法完成后允许触发瞬发技能
  - PostCast 窗口特殊处理

- [ ] 6.5 集成到战斗循环
  - Tick() 方法调用窗口执行
  - 记录窗口执行事件
  - 处理窗口执行失败

- [ ] 6.6 单元测试（20 个）
  - PreAttack 窗口测试（4 个）
  - PostAttack 窗口测试（6 个）
  - PostCast 窗口测试（4 个）
  - GCD 互斥测试（4 个）
  - 非 GCD 共触发测试（2 个）

**验收标准：**
- ✅ 3 个窗口正确触发
- ✅ GCD 技能互斥正确工作
- ✅ 非 GCD 技能可以共触发
- ✅ allowCoTriggerAfterCast 正确处理
- ✅ 20 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 5-6 小时

---

### 阶段 7：触发类技能系统（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现被动技能的概率触发机制。

**任务清单：**

- [ ] 7.1 定义触发时机
  - OnAttackHit: 普攻命中时
  - OnAttackCrit: 普攻暴击时
  - OnPostAttackWindow: PostAttack 窗口时
  - OnPostCastWindow: PostCast 窗口时

- [ ] 7.2 实现 TriggerProcessor 类
  - ProcessTriggers(when, context) 方法
  - 获取所有匹配时机的触发器
  - 按优先级排序
  - 概率判定（procChance）
  - 触发技能施放

- [ ] 7.3 触发安全机制
  - 每窗口最多 5 条触发（防止死循环）
  - 递归深度限制
  - 触发失败记录和诊断

- [ ] 7.4 条件覆盖（overrides）
  - 触发器可以覆盖技能条件
  - 支持强制触发（忽略冷却/资源）

- [ ] 7.5 集成到战斗循环
  - 各触发时机正确调用
  - 触发器与窗口系统协同工作

- [ ] 7.6 单元测试（18 个）
  - OnAttackHit 触发测试（4 个）
  - OnAttackCrit 触发测试（4 个）
  - OnPostAttackWindow 触发测试（3 个）
  - OnPostCastWindow 触发测试（3 个）
  - 概率触发测试（2 个）
  - 触发安全机制测试（2 个）

**验收标准：**
- ✅ 所有触发时机正确工作
- ✅ 概率触发正确判定
- ✅ 优先级排序正确
- ✅ 触发安全机制有效
- ✅ 18 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 4-5 小时

---

### 阶段 8：施法技能集成（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现施法技能（Cast）和施法条显示。

**任务清单：**

- [ ] 8.1 扩展 CastingController
  - 支持技能 castTimeSec
  - StartCast(skillId, castTime) 方法
  - TickCasting(deltaTime) 方法
  - CancelCast() 方法

- [ ] 8.2 施法中断处理
  - 受到伤害时中断（可选）
  - 手动取消
  - 目标死亡时中断

- [ ] 8.3 施法完成处理
  - OnCastComplete 事件
  - 触发 PostCast 窗口
  - 执行技能效果

- [ ] 8.4 Track 暂停
  - 施法中暂停 Attack Track
  - 施法完成后恢复

- [ ] 8.5 施法条 UI
  - 显示施法进度
  - 显示技能名称
  - 显示剩余时间

- [ ] 8.6 单元测试（15 个）
  - 施法开始测试（3 个）
  - 施法进度测试（3 个）
  - 施法完成测试（3 个）
  - 施法中断测试（4 个）
  - Track 暂停测试（2 个）

**验收标准：**
- ✅ 施法技能正确执行
- ✅ 施法时间准确
- ✅ 施法中断正确处理
- ✅ Track 暂停/恢复正确
- ✅ 15 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 4-5 小时

---

### 阶段 9：AutoCastEngine（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现自动施放引擎，协调所有技能系统组件。

**任务清单：**

- [ ] 9.1 实现 AutoCastEngine 类
  - Tick(deltaTime) 主循环
  - 协调 CastingController
  - 协调 WindowExecutor
  - 协调 TriggerProcessor
  - 协调 CooldownManager

- [ ] 9.2 技能选择策略
  - 按槽位优先级选择
  - 检查条件、资源、冷却
  - 选择第一个可用技能

- [ ] 9.3 执行流程
  - PreAttack 窗口 → 选择施法技能
  - 如果无施法技能 → 执行普攻
  - PostAttack 窗口 → 执行瞬发技能
  - PostCast 窗口 → 追加瞬发技能

- [ ] 9.4 事件记录
  - 记录技能选择决策
  - 记录技能施放事件
  - 记录失败原因（条件/资源/冷却）

- [ ] 9.5 性能优化
  - 缓存技能列表
  - 避免重复查询
  - 最小化对象创建

- [ ] 9.6 单元测试（20 个）
  - 技能选择测试（6 个）
  - 窗口协调测试（6 个）
  - 执行流程测试（4 个）
  - 事件记录测试（2 个）
  - 性能测试（2 个）

**验收标准：**
- ✅ 自动施放正确工作
- ✅ 所有组件正确协同
- ✅ 技能选择策略正确
- ✅ 执行流程符合设计
- ✅ 20 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 5-6 小时

---

### 阶段 9.5：职业固定技能差异化（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现四个职业的独特固定技能机制。

**任务清单：**

- [ ] 9.5.1 战士架势系统
  - warrior_special_pulse 获得 warrior_stance buff（可叠加3层）
  - warrior_check_stance 检查层数
  - 满3层时自动获得 warrior_guaranteed_crit buff（必暴）
  - 必暴buff消耗后移除架势层数

- [ ] 9.5.2 法师奥术充能系统
  - mage_special_pulse 获得 mage_arcane_power buff（可叠加5层）
  - 每层提升 8% DamagePerAttack、12% SpecialDamage
  - 持续 8 秒，刷新时重置时间

- [ ] 9.5.3 游侠集中系统
  - ranger_special_pulse 获得 ranger_focus buff（可叠加3层）
  - 每层提升 5% CritChancePercent、3% HastePercent
  - 持续 6 秒

- [ ] 9.5.4 盗贼连击系统
  - rogue_attack_basic 40% 概率触发获得连击点
  - rogue_special_pulse 必定获得连击点
  - rogue_combo buff 可叠加 5 层
  - rogue_eviscerate 消耗连击点造成高伤害

- [ ] 9.5.5 更新 buffs.json
  - 添加所有职业特色 buff 配置（20+ 个）
  - warrior_stance, warrior_guaranteed_crit
  - mage_arcane_power
  - ranger_focus
  - rogue_combo

- [ ] 9.5.6 更新固定技能配置
  - 战士：warrior_attack_basic, warrior_special_pulse
  - 法师：mage_attack_basic, mage_special_pulse
  - 游侠：ranger_attack_basic, ranger_special_pulse
  - 盗贼：rogue_attack_basic, rogue_special_pulse

- [ ] 9.5.7 单元测试（20 个）
  - 战士架势系统测试（5 个）
  - 法师奥术充能测试（5 个）
  - 游侠集中系统测试（5 个）
  - 盗贼连击系统测试（5 个）

**验收标准：**
- ✅ 战士架势系统正确工作（满3层必暴）
- ✅ 法师奥术充能正确叠加伤害
- ✅ 游侠集中系统正确提升属性
- ✅ 盗贼连击系统正确积累和消耗
- ✅ 所有 buff 配置正确
- ✅ 20 个单元测试全部通过
- ✅ 所有原有测试继续通过

**预计工作量：** 4-5 小时

---

### 阶段 10：UI 技能显示（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 实现技能学习、装备和战斗显示的 UI 组件。

**任务清单：**

- [ ] 10.1 技能学习界面（SkillLearningPanel）
  - 显示所有职业可学技能（包括未满足条件的）
  - 可学习/已学习分类显示
  - 等级条件判定和视觉反馈（锁定/解锁）
  - 技能详情展示（名称、描述、消耗、冷却）
  - 学习按钮和状态反馈

- [ ] 10.2 技能装备界面（SkillEquipmentPanel）
  - 4个槽位显示（3主动 + 1被动）
  - 槽位选择和技能装备
  - 已装备技能显示
  - 可装备技能列表（已学习的技能）
  - 卸载技能功能
  - 技能详情和消耗显示

- [ ] 10.3 战斗中技能图标
  - 技能图标显示（4个槽位）
  - 冷却时间倒计时
  - 资源不足提示
  - 条件不满足提示（灰度显示）
  - 技能触发动画

- [ ] 10.4 施法条组件
  - 显示施法进度条
  - 显示技能名称和图标
  - 显示剩余时间（秒）
  - 取消按钮（可选）
  - 施法完成动画

- [ ] 10.5 响应式设计
  - 移动端适配
  - 触摸操作支持
  - 不同分辨率适配

- [ ] 10.6 完整 CSS 样式
  - SkillLearningPanel.razor.css
  - SkillEquipmentPanel.razor.css
  - SkillIcon.razor.css
  - CastingBar.razor.css

- [ ] 10.7 战斗日志详细记录（P0 - 必须）
  - 在战斗 UI 的 log 中详细记录技能事件
  - 记录技能施放（SkillCast）、技能命中、技能触发
  - 记录攻击事件（普攻、暴击）
  - 记录伤害/治疗数值和目标
  - 记录 Buff 应用和移除
  - 为后续战斗 UI 美化提供数据基础
  - 注：动画和音效在后续迭代中根据这些事件实现

**验收标准：**
- ✅ 技能学习界面功能完整
- ✅ 技能装备界面功能完整
- ✅ 技能图标正确显示状态
- ✅ 施法条正确显示进度
- ✅ 战斗日志详细记录所有技能和攻击事件
- ✅ UI 响应式设计良好
- ✅ 手动测试通过

**预计工作量：** 6-8 小时

---

### 阶段 11：集成测试验收（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 进行完整的集成测试，验证所有功能正确协同工作。

**任务清单：**

- [ ] 11.1 完整战斗场景测试
  - 多职业战斗测试
  - 技能轮换测试
  - 资源管理测试
  - Buff 叠加测试

- [ ] 11.2 职业特色测试
  - 战士架势系统完整流程
  - 法师奥术充能完整流程
  - 游侠集中系统完整流程
  - 盗贼连击系统完整流程

- [ ] 11.3 边界情况测试
  - 资源耗尽
  - 冷却等待
  - 目标缺失
  - 条件不满足

- [ ] 11.4 性能测试
  - 长时间战斗稳定性
  - 内存使用情况
  - 帧率影响测试

- [ ] 11.5 集成测试（30 个）
  - 完整战斗流程测试（10 个）
  - 职业特色集成测试（8 个）
  - 边界情况测试（8 个）
  - 性能测试（4 个）

**验收标准：**
- ✅ 所有功能正确协同
- ✅ 职业特色完整工作
- ✅ 边界情况正确处理
- ✅ 性能满足要求
- ✅ 30 个集成测试全部通过
- ✅ 所有 ~579 个测试（363 原有 + 216 新增）全部通过

**预计工作量：** 6-8 小时

---

### 阶段 12：文档与交付（P0 - 必须）

**状态：** ⬜ 未开始

**目标：** 完善文档，准备交付。

**任务清单：**

- [ ] 12.1 代码文档
  - 所有公共 API 添加 XML 注释
  - 复杂逻辑添加代码注释
  - README 更新

- [ ] 12.2 使用文档
  - 技能配置指南
  - 职业特色说明
  - UI 使用说明

- [ ] 12.3 测试文档
  - 测试覆盖率报告
  - 测试用例文档
  - 已知问题列表

- [ ] 12.4 发布准备
  - 版本号更新
  - CHANGELOG 编写
  - 发布说明

**验收标准：**
- ✅ 代码文档完整
- ✅ 使用文档清晰
- ✅ 测试文档完整
- ✅ 准备好发布

**预计工作量：** 4-5 小时

---

## 📊 进度总览

| 阶段 | 状态 | 预计工时 | 测试增量 |
|------|------|---------|---------|
| 阶段 1 - 技能配置基础设施 | ⬜ 未开始 | 3-4h | +15 |
| 阶段 2 - 技能槽位系统 | ⬜ 未开始 | 2-3h | +12 |
| **阶段 2.5 - 技能学习与装备系统** | ⬜ 未开始 | 3-4h | +18 |
| 阶段 3 - 目标选择系统 | ⬜ 未开始 | 3-4h | +15 |
| 阶段 4 - 技能条件判定 | ⬜ 未开始 | 3-4h | +18 |
| 阶段 5 - 资源消耗与冷却（含 InstantHeal 修复） | ⬜ 未开始 | 4-5h | +15 |
| 阶段 6 - Window-GCD 机制 | ⬜ 未开始 | 5-6h | +20 |
| 阶段 7 - 触发类技能系统 | ⬜ 未开始 | 4-5h | +18 |
| 阶段 8 - 施法技能集成 | ⬜ 未开始 | 4-5h | +15 |
| 阶段 9 - AutoCastEngine | ⬜ 未开始 | 5-6h | +20 |
| **阶段 9.5 - 职业固定技能差异化** | ⬜ 未开始 | 4-5h | +20 |
| 阶段 10 - UI 技能显示（扩展） | ⬜ 未开始 | 6-8h | - |
| 阶段 11 - 集成测试验收 | ⬜ 未开始 | 6-8h | +30 |
| 阶段 12 - 文档与交付 | ⬜ 未开始 | 4-5h | - |

**总体进度：** 0/14 (0%) ⬜  
**预计总工时：** 57-72 小时  
**预计新增测试：** ~216 个  
**当前测试基线：** 363 个（Step1 完成后）  
**完成后预计总测试：** ~579 个

---

## 📝 更新日志

### 2025-11-14 v2.0
- ✅ 新增阶段 2.5：技能学习与装备系统（3-4h，+18 测试）
- ✅ 新增阶段 9.5：职业固定技能差异化（4-5h，+20 测试）
- ✅ 扩展阶段 5：增加 InstantHeal 修复（+1h）
- ✅ 扩展阶段 10：增加技能学习和装备 UI
- ✅ 补充完整 14 个阶段的详细任务清单
- ✅ 更新总工时：57-72 小时（原 48-62h）
- ✅ 更新总测试：~216 个（原 ~178 个）
- ✅ 总阶段数：14 个（原 12 个）

### 2025-11-14 v1.0
- 初始版本，12 个阶段

---

**最后更新：** 2025-11-14 v2.0  
**维护者：** @copilot  
**状态：** 已更新，待用户确认
