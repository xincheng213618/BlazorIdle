# Step2 Phase3+ 整合实施计划

## 📋 文档概述

**创建日期：** 2025-11-17  
**创建者：** GitHub Copilot  
**状态：** 待用户确认  
**目标：** 根据 Step2 补充设计文档和 Phase3 目标选择系统整合方案，制定完整的整合实施计划

---

## 🎯 整合目标

基于对以下文档的深入分析：
1. **Step2_补充设计文档**（上篇、中篇、下篇）- 技能学习、职业差异化、完整技能库
2. **Step2_Phase3_目标选择系统整合方案** - 目标选择系统从测试态到生产态
3. **Step2_实施进度追踪** - 当前实施进度（已完成阶段1-3）

我们需要完成以下核心整合任务：

### 核心整合内容

#### 1. **Phase3+ 目标选择系统整合（生产就绪）**
   - 扩展 `professionAttributes.json` 添加 `fixedSkills` 配置
   - 修改 `MultiBattleInstance` 从职业配置动态加载技能ID
   - 移除临时测试参数 `targetPolicyOverride`
   - 确保目标策略完全来自 `skills.json` 的 `targetPolicy` 字段

#### 2. **职业固定技能差异化（阶段9.5）**
   - 战士架势系统（Stance System）
   - 法师奥术充能系统（Arcane Power）
   - 游侠集中系统（Focus）
   - 盗贼连击系统（Combo Points）

#### 3. **技能学习与装备UI（阶段10扩展）**
   - 技能学习界面（SkillLearningPanel）
   - 技能装备界面（SkillEquipmentPanel）

---

## 📊 当前项目状态

### 已完成的阶段 ✅

根据 `Step2_实施进度追踪.md` 的记录：

| 阶段 | 状态 | 测试数 | 完成日期 |
|------|------|--------|---------|
| **阶段1** - 技能配置基础设施 | ✅ 已完成 | +15 | 2025-11-15 |
| **阶段2** - 技能槽位系统 | ✅ 已完成 | +14 | 2025-11-15 |
| **阶段2.5** - 技能学习与装备系统 | ✅ 已完成 | +18 | 2025-11-15 |
| **阶段3** - 目标选择系统 | ✅ 已完成 | +15 | 2025-11-15 |

**当前测试基线：** 425 个测试全部通过（包括439个总测试）

### 待实施的阶段 ⬜

| 阶段 | 优先级 | 预计工时 | 依赖 |
|------|--------|---------|------|
| **Phase 3+** - 目标选择系统整合 | P0 | 6-9h | 阶段3 |
| **阶段4** - 技能条件判定 | P0 | 3-4h | 阶段3 |
| **阶段5** - 资源消耗与冷却 | P0 | 4-5h | 阶段4 |
| **阶段6** - Window-GCD 机制 | P0 | 5-6h | 阶段5 |
| **阶段7** - 触发类技能系统 | P0 | 4-5h | 阶段6 |
| **阶段8** - 施法技能集成 | P0 | 4-5h | 阶段7 |
| **阶段9** - AutoCastEngine | P0 | 5-6h | 阶段8 |
| **阶段9.5** - 职业固定技能差异化 | P0 | 4-5h | 阶段9 |
| **阶段10** - UI技能显示 | P0 | 6-8h | 阶段9.5 |
| **阶段11** - 集成测试验收 | P0 | 6-8h | 阶段10 |
| **阶段12** - 文档与交付 | P0 | 4-5h | 阶段11 |

---

## 🚀 整合实施计划

### 第一部分：Phase3+ 目标选择系统整合（优先级最高）

#### 任务 P3.1：扩展职业配置模型

**目标：** 在 `professionAttributes.json` 中定义每个职业的固定技能

**文件修改：**
1. `BlazorIdle.Shared/Models/ProfessionAttributeConfig.cs`
   ```csharp
   public class ProfessionFixedSkills
   {
       public string NormalAttack { get; set; } = "";
       public string SpecialAttack { get; set; } = "";
   }
   
   // 在 ProfessionAttributeConfig 中添加
   public ProfessionFixedSkills? FixedSkills { get; set; }
   ```

2. `BlazorIdle.Server/Config/professionAttributes.json`
   ```json
   {
     "warrior": {
       "fixedSkills": {
         "normalAttack": "warrior_attack_basic",
         "specialAttack": "warrior_special_pulse"
       }
     },
     "mage": {
       "fixedSkills": {
         "normalAttack": "mage_attack_basic",
         "specialAttack": "mage_special_pulse"
       }
     },
     "ranger": {
       "fixedSkills": {
         "normalAttack": "ranger_attack_basic",
         "specialAttack": "ranger_special_pulse"
       }
     },
     "rogue": {
       "fixedSkills": {
         "normalAttack": "rogue_attack_basic",
         "specialAttack": "rogue_special_pulse"
       }
     }
   }
   ```

**测试：** 5个单元测试
- 职业配置加载测试
- FixedSkills 序列化测试
- 默认值回退测试

**工作量：** 1-2小时

---

#### 任务 P3.2：修改 MultiBattleInstance 动态加载技能

**目标：** 从职业配置读取固定技能ID，替代硬编码

**文件修改：**
1. `BlazorIdle.Shared/Game/MultiBattleInstance.cs`
   - 添加 `_professionSkills` 缓存
   - 实现 `LoadProfessionSkills(IGameConfigProvider)` 方法
   - 修改 `ProcessCharacterAttackViaSkillResolver()` 使用动态技能ID
   - 修改 `ProcessCharacterSpecialViaSkillResolver()` 使用动态技能ID
   - **移除** `targetPolicyOverride` 临时参数

**核心代码变更：**
```csharp
private readonly Dictionary<string, (string normalAttack, string specialAttack)> _professionSkills = new();

private void LoadProfessionSkills(IGameConfigProvider gameConfig)
{
    foreach (var profession in gameConfig.Professions)
    {
        if (profession.FixedSkills != null)
        {
            _professionSkills[profession.Id] = (
                profession.FixedSkills.NormalAttack,
                profession.FixedSkills.SpecialAttack
            );
        }
        else
        {
            // 回退到默认技能
            _professionSkills[profession.Id] = (
                SkillIds.AttackBasic,
                SkillIds.SpecialPulse
            );
        }
    }
}

private void ProcessCharacterAttackViaSkillResolver(string charId, Character character)
{
    var member = _playerTeam.GetMember(charId);
    if (member == null) return;

    // 获取角色职业的固定技能ID
    string skillId = SkillIds.AttackBasic; // 默认
    if (_professionSkills.TryGetValue(member.Entity.ActiveCombatProfessionId, out var skills))
    {
        skillId = skills.normalAttack;
    }

    var result = _skillResolver.Cast(skillId, ctx, opts);
    
    // 直接使用 result.TargetIds（来自技能的 targetPolicy）
    List<string> targetIds = result.TargetIds?.Count > 0 ? result.TargetIds : 
        (defaultTargetId != null ? new List<string> { defaultTargetId } : new List<string>());
    
    // ... 应用伤害到所有目标 ...
}
```

**测试：** 10个单元测试
- 职业技能映射加载测试（4个）
- 动态技能ID使用测试（4个）
- 向后兼容测试（2个）

**工作量：** 2-3小时

---

#### 任务 P3.3：验证 skills.json 配置完整性

**目标：** 确保所有职业固定技能都有正确的 `targetPolicy` 配置

**检查清单：**
- [ ] warrior_attack_basic - targetPolicy: "CurrentTarget"
- [ ] warrior_special_pulse - targetPolicy: "Self"
- [ ] mage_attack_basic - targetPolicy: "CurrentTarget"
- [ ] mage_special_pulse - targetPolicy: "Self"
- [ ] ranger_attack_basic - targetPolicy: "CurrentTarget"
- [ ] ranger_special_pulse - targetPolicy: "Self"
- [ ] rogue_attack_basic - targetPolicy: "CurrentTarget"
- [ ] rogue_special_pulse - targetPolicy: "Self"

**测试：** 8个集成测试
- 每个职业的普攻和特殊技能目标选择验证（2个职业 × 4个职业 = 8个）

**工作量：** 1-2小时

---

#### 任务 P3.4：清理临时测试代码

**目标：** 移除 `targetPolicyOverride` 等临时测试参数

**文件修改：**
- 移除 `ProcessCharacterAttackViaSkillResolver()` 的 `targetPolicyOverride` 参数
- 移除 `ProcessCharacterSpecialViaSkillResolver()` 的 `targetPolicyOverride` 参数
- 更新所有调用处

**测试：** 确保所有 439+ 测试继续通过

**工作量：** 1小时

---

### 第二部分：职业固定技能差异化（阶段9.5）

#### 任务 9.5.1：战士架势系统

**目标：** 实现战士的架势叠加和必暴机制

**新增 Buff 配置（buffs.json）：**
```json
{
  "id": "warrior_stance",
  "name": "战斗架势",
  "type": "buff",
  "maxStacks": 3,
  "durationSec": 15.0,
  "effects": []
}
{
  "id": "warrior_guaranteed_crit",
  "name": "必定暴击",
  "type": "buff",
  "maxStacks": 1,
  "durationSec": 0.1,
  "effects": [
    {
      "type": "OverrideCritChancePercent",
      "value": 100.0
    }
  ]
}
{
  "id": "warrior_check_stance",
  "name": "架势检查",
  "type": "trigger_skill",
  "triggers": [
    {
      "when": "OnPostAttackWindow",
      "conditions": {
        "requireBuffId": "warrior_stance",
        "requireBuffStacks": 3
      },
      "fireSkillId": "warrior_apply_guaranteed_crit"
    }
  ]
}
```

**新增技能配置（skills.json）：**
```json
{
  "id": "warrior_apply_guaranteed_crit",
  "name": "满架势触发",
  "targetPolicy": "Self",
  "onHitBuffs": [
    {
      "op": "apply",
      "buffId": "warrior_guaranteed_crit",
      "stacks": 1
    },
    {
      "op": "removeStacks",
      "buffId": "warrior_stance",
      "stacks": 3
    }
  ]
}
```

**测试：** 5个单元测试
- 架势叠加测试
- 满3层触发必暴测试
- 必暴消耗后移除架势测试

**工作量：** 1.5小时

---

#### 任务 9.5.2：法师奥术充能系统

**目标：** 实现法师的奥术充能叠加和伤害提升

**新增 Buff 配置：**
```json
{
  "id": "mage_arcane_power",
  "name": "奥术充能",
  "type": "buff",
  "maxStacks": 5,
  "durationSec": 8.0,
  "effects": [
    {
      "type": "AddPercentDamagePerAttack",
      "flatValuePerStack": 8.0
    },
    {
      "type": "AddPercentSpecialDamage",
      "flatValuePerStack": 12.0
    }
  ]
}
```

**更新技能配置：**
```json
{
  "id": "mage_special_pulse",
  "onHitBuffs": [
    {
      "op": "apply",
      "buffId": "mage_arcane_power",
      "stacks": 1
    }
  ]
}
```

**测试：** 5个单元测试
- 奥术充能叠加测试
- 伤害提升测试（8%普攻、12%特殊）
- 持续时间刷新测试

**工作量：** 1小时

---

#### 任务 9.5.3：游侠集中系统

**目标：** 实现游侠的集中状态和属性提升

**新增 Buff 配置：**
```json
{
  "id": "ranger_focus",
  "name": "集中射击",
  "type": "buff",
  "maxStacks": 3,
  "durationSec": 6.0,
  "effects": [
    {
      "type": "AddPercentCritChance",
      "flatValuePerStack": 5.0
    },
    {
      "type": "AddPercentHaste",
      "flatValuePerStack": 3.0
    }
  ]
}
```

**测试：** 5个单元测试
- 集中叠加测试
- 暴击率提升测试（5%/层）
- 急速提升测试（3%/层）

**工作量：** 1小时

---

#### 任务 9.5.4：盗贼连击系统

**目标：** 实现盗贼的连击点积累和消耗

**新增 Buff 配置：**
```json
{
  "id": "rogue_combo",
  "name": "连击点",
  "type": "buff",
  "maxStacks": 5,
  "durationSec": 30.0,
  "effects": []
}
```

**新增技能配置：**
```json
{
  "id": "rogue_eviscerate",
  "name": "剔骨",
  "conditions": {
    "requireBuffId": "rogue_combo",
    "minStacks": 1
  },
  "damage": {
    "coefAtk": "1.0 + 0.4 * combo_stacks",
    "flat": 50
  },
  "onHitBuffs": [
    {
      "op": "removeAll",
      "buffId": "rogue_combo"
    }
  ]
}
```

**更新技能配置：**
```json
{
  "id": "rogue_attack_basic",
  "triggers": [
    {
      "when": "OnAttackHit",
      "procChance": 0.4,
      "fireSkillId": "rogue_gain_combo"
    }
  ]
}
{
  "id": "rogue_special_pulse",
  "onHitBuffs": [
    {
      "op": "apply",
      "buffId": "rogue_combo",
      "stacks": 1
    }
  ]
}
```

**测试：** 5个单元测试
- 普攻40%触发连击点测试
- 特殊技能必定获得连击点测试
- 终结技消耗连击点测试

**工作量：** 1.5小时

---

### 第三部分：技能学习与装备UI（阶段10扩展）

#### 任务 10.1：技能学习界面

**目标：** 实现 `SkillLearningPanel` 组件

**新增文件：**
- `BlazorIdle/Components/SkillLearningPanel.razor`
- `BlazorIdle/Components/SkillLearningPanel.razor.cs`
- `BlazorIdle/Components/SkillLearningPanel.razor.css`

**核心功能：**
- 显示所有职业可学技能（包括未满足条件的）
- 可学习/已学习分类显示
- 等级条件判定和视觉反馈（锁定/解锁）
- 技能详情展示
- 学习按钮和状态反馈

**测试：** 手动UI测试

**工作量：** 2-3小时

---

#### 任务 10.2：技能装备界面

**目标：** 实现 `SkillEquipmentPanel` 组件

**新增文件：**
- `BlazorIdle/Components/SkillEquipmentPanel.razor`
- `BlazorIdle/Components/SkillEquipmentPanel.razor.cs`
- `BlazorIdle/Components/SkillEquipmentPanel.razor.css`

**核心功能：**
- 4个槽位显示（3主动 + 1被动）
- 槽位选择和技能装备
- 已装备技能显示
- 可装备技能列表（已学习的技能）
- 卸载技能功能

**测试：** 手动UI测试

**工作量：** 2-3小时

---

#### 任务 10.3：战斗日志详细记录

**目标：** 在战斗UI的log中详细记录技能事件

**文件修改：**
- `BlazorIdle/Components/BattleDemo.razor.cs`
- 或相关战斗UI组件

**记录内容：**
- 技能施放（SkillCast）
- 技能命中
- 技能触发
- 攻击事件（普攻、暴击）
- 伤害/治疗数值和目标
- Buff应用和移除

**测试：** 手动UI测试

**工作量：** 1-2小时

---

## 📈 整合进度甘特图

```
Week 1:
├─ Phase3+ 目标选择系统整合 (6-9h)
│  ├─ P3.1: 扩展职业配置模型 (1-2h) ████
│  ├─ P3.2: 修改MultiBattleInstance (2-3h) ████████
│  ├─ P3.3: 验证skills.json配置 (1-2h) ████
│  └─ P3.4: 清理临时测试代码 (1h) ██

Week 2:
├─ 阶段9.5: 职业固定技能差异化 (4-5h)
│  ├─ 9.5.1: 战士架势系统 (1.5h) ████
│  ├─ 9.5.2: 法师奥术充能 (1h) ██
│  ├─ 9.5.3: 游侠集中系统 (1h) ██
│  └─ 9.5.4: 盗贼连击系统 (1.5h) ████

Week 3:
├─ 阶段10: UI技能显示 (6-8h)
│  ├─ 10.1: 技能学习界面 (2-3h) ██████
│  ├─ 10.2: 技能装备界面 (2-3h) ██████
│  └─ 10.3: 战斗日志记录 (1-2h) ████

Total: 16-22 hours
```

---

## ✅ 验收标准

### Phase3+ 目标选择系统整合
- [ ] 所有职业都从 `professionAttributes.json` 加载固定技能
- [ ] 目标选择完全基于 `SkillDef.TargetPolicy`
- [ ] 移除所有临时测试参数
- [ ] 所有 439+ 测试通过
- [ ] 新增 23+ 测试通过

### 职业固定技能差异化
- [ ] 战士架势系统正确工作（满3层必暴）
- [ ] 法师奥术充能正确叠加伤害
- [ ] 游侠集中系统正确提升属性
- [ ] 盗贼连击系统正确积累和消耗
- [ ] 所有 buff 配置正确
- [ ] 新增 20+ 测试通过

### UI技能显示
- [ ] 技能学习界面功能完整
- [ ] 技能装备界面功能完整
- [ ] 战斗日志详细记录所有技能和攻击事件
- [ ] UI 响应式设计良好
- [ ] 手动测试通过

---

## 🎯 实施顺序建议

### 阶段1：Phase3+ 整合（Week 1）
**优先级：P0 - 最高**
- 这是从测试态到生产态的关键步骤
- 为后续所有阶段提供正确的基础架构
- 必须先完成才能继续其他功能

### 阶段2：职业差异化（Week 2）
**优先级：P0 - 高**
- 丰富游戏体验的核心功能
- 展示技能系统的强大能力
- 为测试提供更多场景

### 阶段3：UI实现（Week 3）
**优先级：P1 - 中高**
- 用户可见的功能
- 可以并行或后续实施
- 依赖前两个阶段的数据

---

## 📝 风险和缓解措施

### 风险1：配置文件缺失或错误
**影响：** 高  
**概率：** 中  
**缓解：**
- 回退到默认技能ID
- 启动时验证配置
- 单元测试覆盖配置加载

### 风险2：技能ID不存在
**影响：** 高  
**概率：** 低  
**缓解：**
- SkillRepository.GetSkill 返回 null 时记录错误
- 使用默认技能作为回退
- 集成测试验证所有配置的技能ID存在

### 风险3：破坏现有功能
**影响：** 高  
**概率：** 低  
**缓解：**
- 保持向后兼容
- 完整的回归测试
- 代码审查

### 风险4：职业差异化系统复杂度
**影响：** 中  
**概率：** 中  
**缓解：**
- 每个职业独立实施和测试
- 充分利用现有 Buff 和触发器系统
- 渐进式实施，优先战士和法师

---

## 📚 参考文档

1. **Step2_补充设计-技能学习与职业差异化-上篇.md** - 技能学习管理系统
2. **Step2_补充设计-技能学习与职业差异化-中篇.md** - 技能装备UI与职业特色
3. **Step2_补充设计-技能学习与职业差异化-下篇.md** - 完整技能库
4. **Step2_Phase3_目标选择系统整合方案.md** - Phase3+ 整合方案
5. **Step2_实施进度追踪.md** - 当前实施进度
6. **Step2_补充设计-阅读指南.md** - 设计文档导航

---

## 🚦 下一步行动

### 立即行动（等待用户确认）
1. **Review** - 用户审查本整合计划
2. **Feedback** - 收集用户反馈和调整建议
3. **Approve** - 获得计划批准

### 确认后行动
4. **Branch** - 创建新PR分支 `feature/step2-phase3plus-integration`
5. **Implement** - 按阶段1-3实施
6. **Test** - 完整测试验证
7. **Review** - 代码审查
8. **Merge** - 合并到主分支

---

## 💡 备注

- 本计划基于充分的文档分析和代码审查
- 所有任务都有明确的验收标准
- 保持向后兼容性和测试覆盖率
- 遵循最小化修改原则
- 优先完成 Phase3+ 整合，为后续阶段奠定基础

---

**文档版本：** 1.0  
**最后更新：** 2025-11-17  
**作者：** GitHub Copilot  
**状态：** ✅ 待用户确认
