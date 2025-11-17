# Step2 Phase3+ 整合实施计划

## 📋 文档概述

**创建日期：** 2025-11-17  
**更新日期：** 2025-11-17  
**创建者：** GitHub Copilot  
**状态：** ✅ **已完成！所有P3.1-P3.7任务全部完成**  
**目标：** 将 MultiBattleInstance 的临时技能逻辑迁移到正式的技能系统

## 🎉 Phase3+ 整合已完成！

**完成日期：** 2025-11-17  
**实际工时：** 约5-6小时  
**测试结果：** 467个测试全部通过（+28新增）  
**核心成就：** 成功将旧技能系统迁移到新系统，消除硬编码，建立可扩展架构

---

## 🎯 整合目标（已根据用户反馈调整）

### 背景说明

**当前状态：**
- `MultiBattleInstance` 中有两个进度条（普通攻击和特殊攻击）
- 这两个事件使用的是技能系统设计前的**临时技能逻辑**
- 虽然调用了 `SkillResolver`，但使用的是硬编码的 `SkillIds.AttackBasic` 和 `SkillIds.SpecialPulse`
- 包含临时测试参数 `targetPolicyOverride`

**目标：**
将这两个事件正式迁移到 `skills.json` 中定义的技能系统

### 核心整合内容

#### 1. **Phase3+ 目标选择系统整合（生产就绪）** - 调整后的方案

**关键变更（根据用户反馈）：**

原方案是从 `professionAttributes.json` 实时读取技能ID，现调整为：
- ✅ 在 `professionAttributes.json` 中配置每个职业的默认固定技能ID
- ✅ **角色创建时**从配置读取并保存到 `CharacterData` 
- ✅ 战斗时从 `CharacterData` 读取技能ID（而非每次从配置读取）
- ✅ 这样可以支持未来的"技能升级"、"技能替换"等功能
- ✅ 移除临时测试参数 `targetPolicyOverride`
- ✅ 确保目标策略完全来自 `skills.json` 的 `targetPolicy` 字段

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

**核心设计思路（根据用户反馈调整）：**
1. ✅ 在 `professionAttributes.json` 中配置默认固定技能ID
2. ✅ 角色创建时从配置读取并保存到 `CharacterData.FixedSkills`（类似心跳保存机制）
3. ✅ 战斗时从 `Character` 实体读取技能ID（不再从配置实时读取）
4. ✅ 支持未来技能升级/替换功能

#### 任务 P3.1：扩展 CharacterData 存储固定技能

**目标：** 在 `CharacterData` 中添加字段存储固定技能ID

**文件修改：**
1. `BlazorIdle.Shared/Models/CharacterData.cs`
   ```csharp
   /// <summary>
   /// 固定技能配置 - 每个职业的普通攻击和特殊攻击技能
   /// Fixed skills configuration - normal attack and special attack per profession
   /// Key: professionId, Value: 固定技能ID配置
   /// </summary>
   [JsonPropertyName("fixedSkillsByProfession")]
   public Dictionary<string, ProfessionFixedSkills> FixedSkillsByProfession { get; set; } = new();
   ```

2. `BlazorIdle.Shared/Models/ProfessionFixedSkills.cs` (新建文件)
   ```csharp
   using System.Text.Json.Serialization;
   
   namespace BlazorIdle.Shared.Models
   {
       /// <summary>
       /// 职业固定技能配置 - 存储在角色数据中
       /// Profession fixed skills - stored in character data
       /// </summary>
       public class ProfessionFixedSkills
       {
           [JsonPropertyName("normalAttack")]
           public string NormalAttack { get; set; } = "";
           
           [JsonPropertyName("specialAttack")]
           public string SpecialAttack { get; set; } = "";
       }
   }
   ```

**测试：** 3个单元测试
- CharacterData 序列化测试
- FixedSkillsByProfession 默认值测试

**工作量：** 0.5小时

---

#### 任务 P3.2：扩展职业配置添加默认技能

**目标：** 在 `professionAttributes.json` 中定义每个职业的默认固定技能

**文件修改：**
1. `BlazorIdle.Shared/Models/ProfessionAttributeConfig.cs`
   ```csharp
   /// <summary>
   /// 默认固定技能配置 - 用于角色创建时初始化
   /// Default fixed skills - used for character creation initialization
   /// </summary>
   public class ProfessionDefaultFixedSkills
   {
       public string NormalAttack { get; set; } = "";
       public string SpecialAttack { get; set; } = "";
   }
   
   // 在 ProfessionAttributeConfig 中添加
   [JsonPropertyName("defaultFixedSkills")]
   public ProfessionDefaultFixedSkills? DefaultFixedSkills { get; set; }
   ```

2. `BlazorIdle.Server/Config/professionAttributes.json`
   ```json
   {
     "warrior": {
       "defaultFixedSkills": {
         "normalAttack": "warrior_attack_basic",
         "specialAttack": "warrior_special_pulse"
       }
     },
     "mage": {
       "defaultFixedSkills": {
         "normalAttack": "mage_attack_basic",
         "specialAttack": "mage_special_pulse"
       }
     },
     "ranger": {
       "defaultFixedSkills": {
         "normalAttack": "ranger_attack_basic",
         "specialAttack": "ranger_special_pulse"
       }
     },
     "rogue": {
       "defaultFixedSkills": {
         "normalAttack": "rogue_attack_basic",
         "specialAttack": "rogue_special_pulse"
       }
     }
   }
   ```

**测试：** 3个单元测试
- 职业配置加载测试
- DefaultFixedSkills 序列化测试

**工作量：** 0.5小时

---

#### 任务 P3.3：角色创建时初始化固定技能

**目标：** 在角色创建时从职业配置读取默认固定技能并保存到 CharacterData

**文件修改：**
1. 查找角色创建逻辑位置（可能在 CharacterService 或相关服务中）
2. 在创建角色时：
   ```csharp
   // 伪代码示例
   var profession = GetProfessionConfig(professionId);
   if (profession?.DefaultFixedSkills != null)
   {
       characterData.FixedSkillsByProfession[professionId] = new ProfessionFixedSkills
       {
           NormalAttack = profession.DefaultFixedSkills.NormalAttack,
           SpecialAttack = profession.DefaultFixedSkills.SpecialAttack
       };
   }
   ```

3. 为所有职业初始化固定技能（因为角色同时拥有所有职业）
   ```csharp
   // 为所有职业初始化固定技能
   foreach (var profession in allProfessions)
   {
       if (profession.DefaultFixedSkills != null)
       {
           characterData.FixedSkillsByProfession[profession.Id] = new ProfessionFixedSkills
           {
               NormalAttack = profession.DefaultFixedSkills.NormalAttack,
               SpecialAttack = profession.DefaultFixedSkills.SpecialAttack
           };
       }
   }
   ```

**测试：** 5个单元测试
- 角色创建时固定技能初始化测试
- 多职业初始化测试
- 配置缺失时的回退测试

**工作量：** 1-1.5小时

---

#### 任务 P3.4：修改 Character 实体添加固定技能访问

**目标：** 在 Character 实体中添加方法获取当前职业的固定技能

**文件修改：**
1. `BlazorIdle.Shared/Game/Character.cs`
   ```csharp
   /// <summary>
   /// 获取当前激活职业的普通攻击技能ID
   /// </summary>
   public string GetNormalAttackSkillId()
   {
       if (_data.FixedSkillsByProfession.TryGetValue(_data.ActiveCombatProfessionId, out var skills))
       {
           return skills.NormalAttack;
       }
       return SkillIds.AttackBasic; // 回退到默认
   }
   
   /// <summary>
   /// 获取当前激活职业的特殊攻击技能ID
   /// </summary>
   public string GetSpecialAttackSkillId()
   {
       if (_data.FixedSkillsByProfession.TryGetValue(_data.ActiveCombatProfessionId, out var skills))
       {
           return skills.SpecialAttack;
       }
       return SkillIds.SpecialPulse; // 回退到默认
   }
   ```

**测试：** 4个单元测试
- GetNormalAttackSkillId 正常情况测试
- GetSpecialAttackSkillId 正常情况测试
- 职业切换后技能ID变化测试
- 配置缺失时回退测试

**工作量：** 0.5小时

---

#### 任务 P3.5：修改 MultiBattleInstance 从 Character 读取技能ID

**目标：** 修改战斗逻辑从 Character 实体读取技能ID，移除临时测试参数

**文件修改：**
1. `BlazorIdle.Shared/Game/MultiBattleInstance.cs`
   - 修改 `ProcessCharacterAttackViaSkillResolver()` 
     * 从 `character.GetNormalAttackSkillId()` 读取技能ID
     * **移除** `targetPolicyOverride` 临时参数
   - 修改 `ProcessCharacterSpecialViaSkillResolver()`
     * 从 `character.GetSpecialAttackSkillId()` 读取技能ID
     * **移除** `targetPolicyOverride` 临时参数

**核心代码变更：**
```csharp
// 修改前（移除 targetPolicyOverride 参数）
private void ProcessCharacterAttackViaSkillResolver(string charId, Character character)
{
    var member = _playerTeam.GetMember(charId);
    if (member == null) return;

    // 从角色实体读取技能ID（不再硬编码）
    string skillId = character.GetNormalAttackSkillId();

    // 创建战斗上下文
    var ctx = new BattleContext { /* ... */ };
    var opts = new SkillCastOptions 
    { 
        SourceTrack = "attack",
        CasterId = charId
    };
    
    // 使用角色的固定技能
    var result = _skillResolver.Cast(skillId, ctx, opts);
    
    // 直接使用 result.TargetIds（来自技能的 targetPolicy）
    List<string> targetIds = result.TargetIds?.Count > 0 ? result.TargetIds : 
        (defaultTargetId != null ? new List<string> { defaultTargetId } : new List<string>());
    
    // 应用伤害到所有目标
    foreach (var targetId in targetIds)
    {
        // ... 应用伤害逻辑 ...
    }
}

private void ProcessCharacterSpecialViaSkillResolver(string charId, Character character)
{
    var member = _playerTeam.GetMember(charId);
    if (member == null) return;

    // 从角色实体读取技能ID（不再硬编码）
    string skillId = character.GetSpecialAttackSkillId();

    // ... 类似的处理 ...
}
```

**测试：** 8个单元测试
- 战士普通攻击使用正确技能ID测试
- 战士特殊攻击使用正确技能ID测试
- 法师普通攻击使用正确技能ID测试
- 法师特殊攻击使用正确技能ID测试
- 目标选择来自 targetPolicy 测试（4个）

**工作量：** 1-2小时

---

#### 任务 P3.6：验证 skills.json 配置完整性

**目标：** 确保所有职业固定技能都有正确的 `targetPolicy` 配置

**检查清单：**
- [ ] warrior_attack_basic - targetPolicy: "current_target"
- [ ] warrior_special_pulse - targetPolicy: "self"
- [ ] mage_attack_basic - targetPolicy: "current_target"
- [ ] mage_special_pulse - targetPolicy: "self"
- [ ] ranger_attack_basic - targetPolicy: "current_target"
- [ ] ranger_special_pulse - targetPolicy: "self"
- [ ] rogue_attack_basic - targetPolicy: "current_target"
- [ ] rogue_special_pulse - targetPolicy: "self"

**测试：** 8个集成测试
- 每个职业的普攻和特殊技能目标选择验证（2个 × 4个职业 = 8个）

**工作量：** 1小时

---

#### 任务 P3.7：更新现有存档兼容性

**目标：** 为现有角色数据添加固定技能配置（迁移逻辑）

**文件修改：**
1. 在角色加载时检查是否有 `FixedSkillsByProfession`
2. 如果为空，根据当前职业配置初始化
   ```csharp
   // 伪代码 - 在加载角色数据后
   if (characterData.FixedSkillsByProfession.Count == 0)
   {
       // 为所有职业初始化固定技能
       foreach (var profession in allProfessions)
       {
           if (profession.DefaultFixedSkills != null)
           {
               characterData.FixedSkillsByProfession[profession.Id] = new ProfessionFixedSkills
               {
                   NormalAttack = profession.DefaultFixedSkills.NormalAttack,
                   SpecialAttack = profession.DefaultFixedSkills.SpecialAttack
               };
           }
       }
   }
   ```

**测试：** 3个单元测试
- 旧存档加载时自动初始化测试
- 新存档正常加载测试

**工作量：** 0.5-1小时

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

## 📈 整合进度甘特图（已更新）

```
Week 1:
├─ Phase3+ 目标选择系统整合 (5-7h) ⭐ 聚焦核心迁移
│  ├─ P3.1: 扩展CharacterData存储 (0.5h) ██
│  ├─ P3.2: 扩展职业配置 (0.5h) ██
│  ├─ P3.3: 角色创建初始化 (1-1.5h) ████
│  ├─ P3.4: Character实体访问方法 (0.5h) ██
│  ├─ P3.5: MultiBattleInstance迁移 (1-2h) ████
│  ├─ P3.6: 验证skills.json配置 (1h) ██
│  └─ P3.7: 存档兼容性 (0.5-1h) ██

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

Total: 11-17 hours (已优化，聚焦核心迁移)
```

**关键差异说明：**
- ✅ 旧方案：每次战斗从配置读取 → 新方案：从角色数据读取
- ✅ 优势：支持未来技能升级、角色个性化、更灵活
- ✅ 类似设计：心跳保存机制的数据持久化模式

---

## ✅ 验收标准（已更新）

### Phase3+ 目标选择系统整合
- [ ] CharacterData 中添加 `FixedSkillsByProfession` 字段
- [ ] professionAttributes.json 中配置所有职业的 `defaultFixedSkills`
- [ ] 角色创建时正确初始化固定技能（保存到 CharacterData）
- [ ] Character 实体提供 `GetNormalAttackSkillId()` 和 `GetSpecialAttackSkillId()` 方法
- [ ] MultiBattleInstance 从 Character 读取技能ID（不再硬编码）
- [ ] 移除所有临时测试参数 `targetPolicyOverride`
- [ ] 目标选择完全基于 `SkillDef.targetPolicy`
- [ ] 现有存档兼容性处理正确
- [ ] 所有 439+ 测试通过
- [ ] 新增 34+ 测试通过（7个任务 × 平均5个测试）

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

## 🎯 实施顺序建议（已更新）

### 第一阶段：Phase3+ 核心迁移（Week 1，5-7小时）
**优先级：P0 - 最高**
- 🎯 核心目标：将硬编码技能迁移到正式技能系统
- ✅ 数据模型扩展（CharacterData + ProfessionAttributeConfig）
- ✅ 角色创建时初始化固定技能
- ✅ 战斗时从角色数据读取技能ID
- ✅ 移除临时测试代码
- 📌 **本阶段聚焦：完成旧技能逻辑到新系统的迁移**

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

---

## 🐉 Phase3+ 扩展：怪物技能系统基础整合

### 📋 概述

**实施日期：** 2025-11-17  
**状态：** ✅ **已完成**  
**工作量：** 2-3 小时  
**测试结果：** 473 个测试全部通过  
**PR链接：** [Complete monster skill system: monsterskills.json integration (P3 pattern)](创建于 2025-11-17)

### 🎯 目标

将怪物攻击系统迁移到新的技能系统，采用与 Phase3+ 玩家攻击集成完全一致的架构模式。

**核心原则：**
- ✅ 复用玩家技能系统架构（P3 模式）
- ✅ 只实现固定普通攻击（触发技能延后到阶段7）
- ✅ 独立配置文件管理（monsterskills.json vs skills.json）
- ✅ 统一技能执行函数（ExecuteSkill 支持玩家和怪物）

### 📝 实施内容

#### P3+.1 Enemy 实体扩展 ✅

**文件：** `BlazorIdle.Shared/Game/Actors.cs`

**变更：**
```csharp
public sealed class Enemy
{
    // 新增：普通攻击技能ID
    public string? NormalAttackSkillId { get; set; }
    
    // 新增：获取普通攻击技能ID（默认回退）
    public string GetNormalAttackSkillId()
    {
        return NormalAttackSkillId ?? "monster_attack_basic";
    }
}
```

**说明：**
- 镜像 Character 实体的设计
- 支持向后兼容（未配置时使用默认值）
- 为特殊怪物自定义技能预留扩展性

#### P3+.2 创建 monsterskills.json 配置文件 ✅

**文件：** `BlazorIdle.Shared/Config/monsterskills.json`

**内容：**
```json
[
  {
    "id": "monster_attack_basic",
    "name": "怪物普通攻击",
    "description": "通用的怪物普通攻击，造成1倍伤害",
    "type": "passive",
    "slotType": "passive",
    "fixed": true,
    "releaseType": "instant",
    "targetPolicy": "random_player",
    "cooldownSec": 0,
    "allowedProfessions": [],
    "damage": {
      "coefAtk": 1.0,
      "flat": 0,
      "isAoe": false
    }
  }
]

// ⚠️ 重要：怪物技能命名约定
// Monster Skill Naming Convention:
// - 所有怪物技能的 "id" 必须以 "monster_" 或 "enemy_" 开头
// - All monster skill IDs MUST start with "monster_" or "enemy_" prefix
// - 这是 SkillResolver 用于识别怪物技能的关键依据
```

**关键特性：**
- 独立于 skills.json（职责分离）
- 使用相同的 SkillDef 数据结构
- 添加命名约定注释（防止配置错误）

#### P3+.3 扩展 monsters.json 配置 ✅

**文件：** `BlazorIdle.Server/Config/monsters.json`

**变更：**
```json
{
  "id": "slime",
  "name": "Green Slime",
  "normalAttackSkillId": "monster_attack_basic",  // 新增
  ...
}
```

**应用范围：**
- slime → `monster_attack_basic`
- wolf → `monster_attack_basic`
- ogre → `monster_attack_basic`

**设计优势：**
- 保留为特殊怪物自定义技能的灵活性
- 便于未来添加 BOSS 特殊技能

#### P3+.4 SkillRepository 双文件加载 ✅

**文件：** `BlazorIdle.Shared/Game/Skills/SkillRepository.cs`

**变更：**
```csharp
public SkillRepository()
{
    InitializeDefaultSkills();
    
    // 加载玩家技能
    bool playerSkillsLoaded = TryLoadFromEmbeddedJson("skills.json");
    
    // 加载怪物技能（新增）
    bool monsterSkillsLoaded = TryLoadFromEmbeddedJson("monsterskills.json");
    
    if (playerSkillsLoaded || monsterSkillsLoaded)
    {
        ValidateSkillConfigurations();
    }
}
```

**优势：**
- 所有技能（玩家+怪物）在同一仓库中查询
- 简化战斗系统集成
- 保持配置文件分离（职责清晰）

#### P3+.5 SkillResolver 怪物技能支持 ✅

**文件：** `BlazorIdle.Shared/Game/Skills/SkillResolver.cs`

**问题：** 原有代码只检查 `skillId.StartsWith("enemy_")`，导致 `monster_*` 技能使用错误的伤害源

**修复（4处）：**

1. **伤害计算：**
```csharp
// 修改前
int attackPower = skillId.StartsWith("enemy_")
    ? (ctx.Enemy?.DamagePerHit ?? 0)
    : (ctx.Player?.DamagePerAttack ?? 0);

// 修改后
bool isMonsterSkill = skillId.StartsWith("enemy_") || skillId.StartsWith("monster_");
int attackPower = isMonsterSkill
    ? (ctx.Enemy?.DamagePerHit ?? 0)
    : (ctx.Player?.DamagePerAttack ?? 0);
```

2. **Buff系统识别、伤害浮动、暴击检查** - 应用相同逻辑

**影响：**
- ✅ 怪物使用正确的 DamagePerHit
- ✅ 怪物使用正确的 VariancePct
- ✅ 怪物不触发 Buff 效果
- ✅ 怪物攻击不会暴击

#### P3+.6 ExecuteSkill 统一重构 ✅

**文件：** `BlazorIdle.Shared/Game/MultiBattleInstance.cs`

**重构前：**
- `ExecuteSkill(charId, character, skillId, ...)` - 专用于玩家（120行）
- `ProcessEnemyAttackViaSkillResolver(enemyId, enemy)` - 怪物逻辑（80行）
- **问题：** 大量重复代码（约70行）

**重构后：**
```csharp
// 统一函数签名
private void ExecuteSkill(
    string casterId,           // 施法者ID（玩家或怪物）
    string skillId, 
    string sourceTrack,
    bool isCasterPlayer,       // 标识施法者类型
    EventSource eventSource = EventSource.Attack)
{
    if (isCasterPlayer)
    {
        // 玩家分支：目标选择、伤害应用、资源管理
    }
    else
    {
        // 怪物分支：目标选择、伤害应用、无资源管理
    }
}

// 简化后的怪物攻击（仅6行）
private void ProcessEnemyAttackViaSkillResolver(string enemyId, Enemy enemy)
{
    string skillId = enemy.GetNormalAttackSkillId();
    ExecuteSkill(enemyId, skillId, "enemy_attack", isCasterPlayer: false);
}
```

**优势：**
- ✅ 消除约 70 行重复代码
- ✅ 单一职责：ExecuteSkill 专注于技能执行
- ✅ 易于维护：未来逻辑改动只需修改一处
- ✅ 扩展性强：添加 NPC/宠物只需新增分支

#### P3+.7 文档更新 ✅

**Step2_实施进度追踪.md 更新：**
- 新增阶段 P3+：怪物技能系统基础整合
- 更新阶段 7：明确支持玩家+怪物触发技能
- 进度总览：5/15 阶段完成（33%）
- 更新日志 v3.0

**monsterskills.json 注释：**
- 添加关键命名约定说明
- 提供正确/错误示例
- 防止未来配置错误

### ✅ 验收标准

- [x] monsterskills.json 配置正确加载
- [x] 怪物使用新的技能系统攻击
- [x] 怪物伤害计算正确（使用 DamagePerHit）
- [x] ExecuteSkill 统一支持玩家和怪物
- [x] 向后兼容（未配置技能ID时使用默认值）
- [x] 473 个测试全部通过
- [x] 手动测试验证功能正常
- [x] 文档完整更新

### 📊 代码变更统计

| 文件 | 变更类型 | 行数变化 |
|------|---------|---------|
| Actors.cs | 新增 | +10 行 |
| monsterskills.json | 新建 | +50 行 |
| monsters.json | 修改 | +3 行 |
| SkillRepository.cs | 修改 | +15 行 |
| SkillResolver.cs | 修改 | +10 行 |
| MultiBattleInstance.cs | 重构 | -70 行 |
| Step2_实施进度追踪.md | 更新 | +127 行 |
| **总计** | | **-60 行净减少** |

### 🎯 架构优势

#### 1. 配置分离
```
skills.json          → 玩家职业技能（44个）
monsterskills.json   → 怪物技能（1个通用 + 未来扩展）
```

#### 2. 代码复用
```
SkillResolver    → 统一处理玩家和怪物技能
ExecuteSkill     → 统一执行逻辑（isCasterPlayer 分支）
TargetSelector   → 共享目标选择策略
```

#### 3. 易于扩展

**当前阶段：**
- ✅ 怪物固定普通攻击

**阶段 7（触发技能）：**
- ✅ 怪物触发技能复用 TriggerProcessor
- ✅ 无需单独实现怪物触发逻辑
- ✅ 直接添加触发器配置即可

**未来扩展：**
- ✅ BOSS 特殊技能（修改 monsters.json）
- ✅ 怪物施法技能（添加 castTimeSec）
- ✅ NPC/宠物系统（扩展 ExecuteSkill 分支）

### 🔗 与 Phase3+ 的关系

**Phase3+ 玩家集成：**
- Character → NormalAttackSkillId → skills.json
- ExecuteSkill(player branch)

**Phase3+ 怪物集成：**
- Enemy → NormalAttackSkillId → monsterskills.json
- ExecuteSkill(monster branch)

**一致性：**
- ✅ 相同的数据模型（SkillDef）
- ✅ 相同的执行流程（ExecuteSkill）
- ✅ 相同的架构模式（配置驱动）
- ✅ 相同的扩展路径（触发技能在阶段7）

### 💡 关键设计决策

#### 1. 前缀约定 vs Type参数

**当前方案：** 使用 `monster_*` 或 `enemy_*` 前缀识别

**优势：**
- 零侵入性（无需修改数据结构）
- 向后兼容
- 简单直观

**未来改进：** 当添加第3种施法者（NPC）时，考虑重构为从 BattleContext 推断类型

#### 2. 独立配置文件 vs 合并

**当前方案：** monsterskills.json 独立于 skills.json

**优势：**
- 职责分离（玩家技能 vs 怪物技能）
- 易于管理（各自维护）
- 清晰的命名空间

#### 3. 通用技能 vs 专用技能

**当前方案：** 所有普通怪物共享 `monster_attack_basic`

**优势：**
- 简化配置
- 减少冗余
- 符合怪物攻击同质化特性

**扩展性：** 特殊怪物可配置独立技能ID

### 🚀 后续工作

**立即可用：**
- ✅ 怪物使用新技能系统攻击
- ✅ 支持目标选择策略（random_player）
- ✅ 正确的伤害计算

**阶段 7 扩展（触发技能）：**
- [ ] 怪物触发技能配置（在 monsterskills.json）
- [ ] 触发器定义（triggers 数组）
- [ ] 概率判定（procChance）
- [ ] 复用 TriggerProcessor（无需额外代码）

**未来增强：**
- [ ] BOSS 特殊技能
- [ ] 怪物施法技能
- [ ] 怪物 AoE 攻击
- [ ] 怪物 Buff 系统（如果需要）

---

## 💡 备注

- 本计划基于充分的文档分析和代码审查
- **已根据用户反馈调整核心设计思路**
- 所有任务都有明确的验收标准
- 保持向后兼容性和测试覆盖率
- 遵循最小化修改原则
- 优先完成 Phase3+ 核心迁移，为后续功能奠定基础
- **Phase3+ 扩展已完成怪物技能系统基础整合**

## 🔄 关键设计变更总结

### 原方案 vs 新方案

| 方面 | 原方案 | 新方案（用户建议） |
|------|--------|-------------------|
| 技能ID存储 | 实时从 professionAttributes.json 读取 | 角色创建时保存到 CharacterData |
| 战斗时读取 | 从配置缓存读取 | 从 Character 实体读取 |
| 扩展性 | 有限（依赖配置文件） | 强（支持技能升级、个性化） |
| 实现复杂度 | 中等（需要缓存管理） | 简单（直接从实体读取） |
| 数据持久化 | 不需要 | 需要（类似心跳保存） |
| 未来功能支持 | 不支持技能升级 | ✅ 支持技能升级/替换 |

### 设计优势
1. ✅ **扩展性强**：支持未来的"普通攻击升级"等功能
2. ✅ **数据一致性**：技能ID与角色数据一起保存
3. ✅ **简化战斗逻辑**：直接从 Character 读取，无需配置查询
4. ✅ **个性化支持**：每个角色可以有不同的固定技能（未来）
5. ✅ **参考现有模式**：与心跳保存机制一致的设计模式

---

**文档版本：** 2.0  
**创建日期：** 2025-11-17  
**更新日期：** 2025-11-17  
**作者：** GitHub Copilot  
**状态：** ✅ 已根据用户反馈更新，待确认开始实施
