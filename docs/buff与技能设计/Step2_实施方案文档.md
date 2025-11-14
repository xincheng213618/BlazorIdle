# Step2 技能系统 - 详细实施方案

## 📋 文档说明

本文档是 Step2 技能系统的详细实施方案，基于 [step2_技能系统-总体设计.md](./step2_技能系统-总体设计.md) 设计文档，按照可执行的步骤细化实施计划。

**版本：** v1.0  
**创建日期：** 2025-11-14  
**状态：** 待审核

---

## 一、总体目标与范围

### 1.1 核心目标

在 Step 0（Track 抽象与 SkillCast 统一）和 Step 1（资源/Buff/Special 脉冲）的基础上，实现完整的技能系统：

1. **技能类型支持**
   - 主动技能（Active）：玩家可触发的技能
   - 被动技能（Passive）：常驻效果或触发器

2. **释放类型支持**
   - 瞬发技能（Instant）：立即生效
   - 施法技能（Cast）：需要施法时间，显示施法条

3. **目标选择系统**
   - 5 种目标策略：current_target, enemies_all, allies_lowest_hp_pct, self, allies_all
   - 支持单体和 AoE 技能

4. **资源与冷却管理**
   - 资源消耗（costs）
   - 冷却时间（cooldownSec）
   - 条件判定（conditions）

5. **Window-GCD 机制**
   - 3 个触发窗口：PreAttack, PostAttack, PostCast
   - Window-GCD 互斥：同窗口 isGcd 技能只触发一个
   - 瞬发共触发：非 GCD 技能可以同窗多个触发

6. **触发类技能（Trigger）**
   - 概率触发机制（procChance）
   - 优先级排序（priority）
   - 4 种触发时机：OnAttackHit, OnAttackCrit, OnPostAttackWindow, OnPostCastWindow

7. **配置化管理**
   - skills.json 集中管理所有技能
   - 职业限制（allowedProfessions）
   - 解锁条件（unlock）

### 1.2 不包含的内容

- ❌ 手动释放/快捷键输入（预留）
- ❌ 时间型 GCD（global cooldown ms，预留字段）
- ❌ 命中率/闪避/格挡完整判定（暂用 AlwaysHits=true）
- ❌ 技能等级成长/多段引导/通道技能（预留字段）

---

## 二、技术架构

### 2.1 核心组件

```
┌─────────────────────────────────────────────────────────┐
│                   技能系统架构图                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────┐      ┌──────────────┐               │
│  │ skills.json  │─────>│ SkillRepository│              │
│  └──────────────┘      └──────────────┘               │
│                              │                          │
│                              v                          │
│  ┌──────────────┐      ┌──────────────┐               │
│  │  Character   │─────>│ SkillSlots   │               │
│  │              │      │  (槽位管理)   │               │
│  └──────────────┘      └──────────────┘               │
│                              │                          │
│                              v                          │
│  ┌──────────────┐      ┌──────────────┐               │
│  │AutoCastEngine│─────>│WindowExecutor│               │
│  │  (协调器)     │      │  (窗口执行)   │               │
│  └──────────────┘      └──────────────┘               │
│         │                     │                         │
│         v                     v                         │
│  ┌──────────────┐      ┌──────────────┐               │
│  │ConditionCheck│      │TargetSelector│               │
│  │  (条件判定)   │      │  (目标选择)   │               │
│  └──────────────┘      └──────────────┘               │
│         │                     │                         │
│         └──────┬──────────────┘                        │
│                v                                        │
│         ┌──────────────┐                               │
│         │SkillResolver │                               │
│         │  (技能执行)   │                               │
│         └──────────────┘                               │
│                │                                        │
│                v                                        │
│         ┌──────────────┐                               │
│         │MultiBattleInstance│                          │
│         │  (结果应用)   │                               │
│         └──────────────┘                               │
└─────────────────────────────────────────────────────────┘
```

### 2.2 数据流

```
1. 配置加载阶段：
   skills.json → SkillRepository → 验证 → 内存缓存

2. 战斗初始化阶段：
   Character → InitializeSkillSlots → 装配固定技能 + 配置技能

3. 战斗执行阶段（每个 Tick）：
   AutoCastEngine.Tick(dt)
     → CastingController.Tick(dt)  // 推进施法
     → PreAttack 窗口               // 检查施法技能
     → 普攻执行（如果未施法）
     → PostAttack 窗口              // 主动技能 + 触发类技能
     → PostCast 窗口（施法完成后）   // 追加瞬发技能

4. 技能执行流程：
   WindowExecutor.ExecuteWindow(window, context)
     → ConditionChecker.CheckConditions(skill, context)
     → ResourceChecker.CheckCost(skill, context)
     → CooldownManager.IsReady(skillId)
     → TargetSelector.ResolveTargets(targetPolicy, context)
     → SkillResolver.Cast(skill, targets, context)
     → MultiBattleInstance.ApplyResults(result)
     → EventRecording (SkillCastEvent, ResourceGainEvent, etc.)
```

---

## 三、详细实施步骤

### 阶段 1：技能配置基础设施（3-4 小时）

#### 1.1 扩展 SkillDef 数据模型

**文件：** `BlazorIdle.Shared/Game/Skills/SkillDef.cs`

**新增字段：**
```csharp
public string Type { get; set; } = "active";  // "active" | "passive"
public string SlotType { get; set; } = "active";  // "active" | "passive"
public bool Fixed { get; set; } = false;  // 固定技能标识
public string ReleaseType { get; set; } = "instant";  // "instant" | "cast"
public double CastTimeSec { get; set; } = 0.0;
public bool IsGcd { get; set; } = true;  // 窗口互斥标识
public bool AllowCoTriggerAfterCast { get; set; } = false;
public string TargetPolicy { get; set; } = "current_target";
public UnlockConfig? Unlock { get; set; }
public List<string>? AllowedProfessions { get; set; }
public SkillConditions? Conditions { get; set; }
public List<TriggerDef>? Triggers { get; set; }
```

#### 1.2 创建支持类型

**文件：** `BlazorIdle.Shared/Game/Skills/UnlockConfig.cs`
```csharp
public class UnlockConfig
{
    public int? MinLevel { get; set; }
    public List<string>? AccountFlags { get; set; }
    public int? RequiresProfessionLevel { get; set; }
}
```

**文件：** `BlazorIdle.Shared/Game/Skills/SkillConditions.cs`
```csharp
public class SkillConditions
{
    public double? HpBelowPct { get; set; }
    public double? HpAbovePct { get; set; }
    public string? RequireBuffId { get; set; }
    public string? ForbidBuffId { get; set; }
    public Dictionary<string, int>? RequireResource { get; set; }
}
```

**文件：** `BlazorIdle.Shared/Game/Skills/TriggerDef.cs`
```csharp
public class TriggerDef
{
    public string When { get; set; } = "";  // "OnAttackHit" | "OnAttackCrit" | ...
    public double ProcChance { get; set; } = 0.0;  // 0-1 概率
    public string FireSkillId { get; set; } = "";
    public int Priority { get; set; } = 10;
    public SkillConditions? Conditions { get; set; }
    public Dictionary<string, object>? Overrides { get; set; }  // 预留
}
```

**文件：** `BlazorIdle.Shared/Game/Skills/TargetPolicy.cs`
```csharp
public enum TargetPolicy
{
    CurrentTarget,
    EnemiesAll,
    AlliesLowestHpPct,
    Self,
    AlliesAll
}
```

#### 1.3 创建 skills.json 配置文件

**文件：** `BlazorIdle.Shared/Config/skills.json`

参考 [step2_Shared_Config_skills.example.json](./step2_Shared_Config_skills.example.json)，包含：
- 战士固定技能（3 个）
- 战士可配置技能（3 个）
- 战士被动触发（2 个）
- 共享技能（1 个）
- 法师测试技能（1 个）

#### 1.4 扩展 SkillRepository

**文件：** `BlazorIdle.Shared/Game/Skills/SkillRepository.cs`

**新增方法：**
```csharp
// JSON 加载
public void LoadFromJson(string json)
{
    var skillsData = JsonSerializer.Deserialize<SkillsData>(json);
    // 验证并注册技能
}

// 从嵌入资源加载
public bool TryLoadFromEmbeddedJson()
{
    // 类似 BuffRepository 的实现
}

// 配置验证
private void ValidateSkillConfigurations()
{
    // 1. 验证必填字段
    // 2. 验证 buffConfigId 引用存在
    // 3. 验证 fireSkillId 引用存在
    // 4. 验证 targetPolicy 有效
}
```

#### 1.5 API 集成

**修改文件：**
- `BlazorIdle.Shared/DTOs/GameConfigResponse.cs` - 添加 Skills 字段
- `BlazorIdle.Server/Services/IGameConfigProvider.cs` - 添加 Skills 属性
- `BlazorIdle.Server/Services/GameConfigProvider.cs` - 加载 skills.json
- `BlazorIdle.Server/Controllers/GameConfigController.cs` - 返回技能配置
- `BlazorIdle/Game/Config/GameConfigService.cs` - 客户端接收

#### 1.6 单元测试

**文件：** `BlazorIdle.Tests/Phase1SkillConfigTests.cs`

**测试用例（15 个）：**
1. SkillDef_Constructor_SetsDefaultValues
2. SkillDef_WithAllFields_DeserializesCorrectly
3. SkillDef_MissingRequiredFields_ThrowsException
4. UnlockConfig_Constructor_SetsFields
5. SkillConditions_HpConditions_ValidatesCorrectly
6. TriggerDef_Constructor_SetsDefaults
7. TriggerDef_Priority_SortsCorrectly
8. SkillRepository_LoadFromJson_Success
9. SkillRepository_LoadFromJson_InvalidFormat_Throws
10. SkillRepository_ValidateConfigurations_CatchesMissingBuffId
11. SkillRepository_ValidateConfigurations_CatchesMissingFireSkillId
12. SkillRepository_GetSkillById_ReturnsCorrect
13. SkillRepository_GetSkillsByProfession_FiltersCorrectly
14. SkillRepository_EmbeddedJson_LoadsSuccessfully
15. SkillRepository_DuplicateSkillId_Logs Warning

**验收标准：**
- ✅ 所有 15 个新测试通过
- ✅ 所有 363 个原有测试继续通过
- ✅ skills.json 成功加载
- ✅ 配置验证能够捕获错误

---

### 阶段 2：技能槽位系统（2-3 小时）

#### 2.1 创建槽位数据模型

**文件：** `BlazorIdle.Shared/Game/Skills/SkillSlotConfig.cs`
```csharp
public class SkillSlotConfig
{
    public string SlotId { get; set; } = "";  // "active_1", "active_2", "active_3", "passive_1"
    public string SlotType { get; set; } = "";  // "active" | "passive"
    public string? SkillId { get; set; }  // 装配的技能ID（可以为空）
}
```

**文件：** `BlazorIdle.Shared/Game/Skills/CharacterSkillSlots.cs`
```csharp
public class CharacterSkillSlots
{
    private Dictionary<string, SkillSlotConfig> _slots = new();
    
    public CharacterSkillSlots(string profession)
    {
        // 初始化默认槽位：3个主动 + 1个被动
        _slots["active_1"] = new SkillSlotConfig { SlotId = "active_1", SlotType = "active" };
        _slots["active_2"] = new SkillSlotConfig { SlotId = "active_2", SlotType = "active" };
        _slots["active_3"] = new SkillSlotConfig { SlotId = "active_3", SlotType = "active" };
        _slots["passive_1"] = new SkillSlotConfig { SlotId = "passive_1", SlotType = "passive" };
    }
    
    public bool TryEquipSkill(string slotId, string skillId, SkillDef skillDef)
    {
        // 验证槽位存在
        // 验证槽位类型匹配
        // 验证技能未重复装配
        // 装配技能
    }
    
    public void UnequipSkill(string slotId) { }
    
    public List<SkillDef> GetActiveSkills() { }  // 按槽位顺序
    public List<SkillDef> GetPassiveSkills() { }
    public List<SkillDef> GetFixedSkills() { }
    public List<SkillDef> GetConfigurableSkills() { }
}
```

#### 2.2 扩展 Character 实体

**文件：** `BlazorIdle.Shared/Entities/Character.cs`

**新增字段：**
```csharp
// 技能槽位（运行态）
public CharacterSkillSlots? EquippedSkills { get; set; }
```

**新增方法：**
```csharp
public void InitializeSkillSlots(SkillRepository skillRepo)
{
    EquippedSkills = new CharacterSkillSlots(ActiveCombatProfessionId);
    
    // 自动装配固定技能
    var fixedSkills = skillRepo.GetSkillsByProfession(ActiveCombatProfessionId)
        .Where(s => s.Fixed)
        .ToList();
    
    foreach (var skill in fixedSkills)
    {
        // 根据 SlotType 找到合适的槽位装配
    }
}
```

#### 2.3 装配验证

**文件：** `BlazorIdle.Shared/Game/Skills/SkillEquipValidator.cs`
```csharp
public class SkillEquipValidator
{
    public static bool CanEquip(SkillDef skill, Character character, SkillRepository skillRepo)
    {
        // 1. 验证职业限制
        if (skill.AllowedProfessions != null && 
            !skill.AllowedProfessions.Contains(character.ActiveCombatProfessionId))
            return false;
        
        // 2. 验证解锁条件
        if (!CheckUnlock(skill.Unlock, character))
            return false;
        
        // 3. 验证槽位类型匹配
        // ...
        
        return true;
    }
    
    private static bool CheckUnlock(UnlockConfig? unlock, Character character)
    {
        // 检查等级、账户标记等
    }
}
```

#### 2.4 单元测试

**文件：** `BlazorIdle.Tests/Phase2SkillSlotTests.cs`

**测试用例（12 个）：**
1. SkillSlotConfig_Constructor_InitializesCorrectly
2. SkillSlotConfig_EquipSkill_UpdatesSkillId
3. CharacterSkillSlots_Constructor_Creates4Slots
4. CharacterSkillSlots_TryEquipSkill_Success
5. CharacterSkillSlots_TryEquipSkill_WrongSlotType_Fails
6. CharacterSkillSlots_GetActiveSkills_ReturnsInSlotOrder
7. SkillEquipValidator_ProfessionMismatch_ReturnsFalse
8. SkillEquipValidator_LevelTooLow_ReturnsFalse
9. SkillEquipValidator_MissingAccountFlag_ReturnsFalse
10. Character_InitializeSkillSlots_EquipsFixedSkills
11. Character_EquippedSkills_PreventsDuplicateSkill
12. CharacterSkillSlots_UnequipSkill_RemovesSkill

**验收标准：**
- ✅ 所有 12 个新测试通过
- ✅ 固定技能自动装配
- ✅ 装配验证正确工作

---

### 阶段 3：目标选择系统（3-4 小时）

#### 3.1 实现目标选择核心

**文件：** `BlazorIdle.Shared/Game/Skills/TargetSelector.cs`
```csharp
public class TargetSelector
{
    public static List<IBuffOwner> ResolveTargets(
        TargetPolicy policy,
        IBuffOwner caster,
        IBuffOwner? currentTarget,
        List<IBuffOwner> allAllies,
        List<IBuffOwner> allEnemies)
    {
        switch (policy)
        {
            case TargetPolicy.CurrentTarget:
                return currentTarget != null ? new List<IBuffOwner> { currentTarget } : new();
            
            case TargetPolicy.EnemiesAll:
                return allEnemies.Where(e => e.CurrentHp > 0).ToList();
            
            case TargetPolicy.AlliesLowestHpPct:
                var lowestAlly = allAllies
                    .Where(a => a.CurrentHp > 0)
                    .OrderBy(a => (double)a.CurrentHp / a.MaxHp)
                    .FirstOrDefault();
                return lowestAlly != null ? new List<IBuffOwner> { lowestAlly } : new();
            
            case TargetPolicy.Self:
                return new List<IBuffOwner> { caster };
            
            case TargetPolicy.AlliesAll:
                return allAllies.Where(a => a.CurrentHp > 0).ToList();
            
            default:
                LogTargetResolutionFailure(policy, caster);
                return new();
        }
    }
    
    private static void LogTargetResolutionFailure(TargetPolicy policy, IBuffOwner caster)
    {
        Debug.WriteLine($"[TargetSelector] Failed to resolve targets: policy={policy}, caster={caster.Id}");
    }
}
```

#### 3.2 集成到 SkillResolver

**文件：** `BlazorIdle.Shared/Game/Skills/SkillResolver.cs`

**修改 Cast 方法：**
```csharp
public SkillCastResult Cast(string skillId, BattleContext ctx, SkillCastOptions? opts = null)
{
    var skill = _repository.GetSkillById(skillId);
    if (skill == null) return EmptyResult();
    
    // 解析目标
    var targets = TargetSelector.ResolveTargets(
        ParseTargetPolicy(skill.TargetPolicy),
        ctx.PlayerBuffOwner,
        ctx.EnemyBuffOwners?.Values.FirstOrDefault(),  // 当前目标（简化）
        GetAllAllies(ctx),
        GetAllEnemies(ctx)
    );
    
    if (targets.Count == 0)
    {
        // 目标缺失，记录并跳过
        return EmptyResult();
    }
    
    // 对每个目标计算伤害
    var results = new List<SkillCastResult>();
    foreach (var target in targets)
    {
        var result = CalculateDamageForTarget(skill, target, ctx, opts);
        results.Add(result);
    }
    
    // 合并结果（AoE 技能）
    return MergeResults(results);
}
```

#### 3.3 单元测试

**文件：** `BlazorIdle.Tests/Phase3TargetSelectionTests.cs`

**测试用例（15 个）：**
1. TargetSelector_CurrentTarget_WithTarget_ReturnsTarget
2. TargetSelector_CurrentTarget_NoTarget_ReturnsEmpty
3. TargetSelector_CurrentTarget_DeadTarget_ReturnsEmpty
4. TargetSelector_EnemiesAll_Returns3Enemies
5. TargetSelector_EnemiesAll_NoEnemies_ReturnsEmpty
6. TargetSelector_EnemiesAll_FiltersDeadEnemies
7. TargetSelector_AlliesLowestHpPct_ReturnsLowest
8. TargetSelector_AlliesLowestHpPct_UsesPercentage_NotAbsolute
9. TargetSelector_AlliesLowestHpPct_NoAllies_ReturnsEmpty
10. TargetSelector_Self_ReturnsCaster
11. TargetSelector_AlliesAll_ReturnsAllLiving
12. TargetSelector_AlliesAll_FiltersDeadAllies
13. SkillResolver_AoeSkill_HitsAllEnemies
14. SkillResolver_SingleTarget_HitsOnlyOne
15. TargetSelector_InvalidPolicy_LogsAndReturnsEmpty

**验收标准：**
- ✅ 所有 15 个新测试通过
- ✅ AlliesLowestHpPct 使用 HP 百分比
- ✅ 目标缺失优雅处理
- ✅ AoE 技能正确作用

---

### 阶段 4：技能条件判定系统（3-4 小时）

#### 4.1 实现条件判定

**文件：** `BlazorIdle.Shared/Game/Skills/ConditionChecker.cs`
```csharp
public class ConditionChecker
{
    public static bool CheckConditions(SkillConditions? conditions, IBuffOwner caster)
    {
        if (conditions == null) return true;
        
        // HP 条件
        if (conditions.HpBelowPct.HasValue)
        {
            double hpPct = (double)caster.CurrentHp / caster.MaxHp * 100.0;
            if (hpPct >= conditions.HpBelowPct.Value)
                return false;
        }
        
        if (conditions.HpAbovePct.HasValue)
        {
            double hpPct = (double)caster.CurrentHp / caster.MaxHp * 100.0;
            if (hpPct <= conditions.HpAbovePct.Value)
                return false;
        }
        
        // Buff 条件
        if (!string.IsNullOrEmpty(conditions.RequireBuffId))
        {
            if (!caster.Buffs.ContainsKey(conditions.RequireBuffId))
                return false;
        }
        
        if (!string.IsNullOrEmpty(conditions.ForbidBuffId))
        {
            if (caster.Buffs.ContainsKey(conditions.ForbidBuffId))
                return false;
        }
        
        // 资源条件
        if (conditions.RequireResource != null)
        {
            foreach (var (bucketId, amount) in conditions.RequireResource)
            {
                var bucket = caster.Buckets?.GetBucket(bucketId);
                if (bucket == null || bucket.Current < amount)
                    return false;
            }
        }
        
        return true;
    }
    
    public static bool CheckUnlock(UnlockConfig? unlock, Character character)
    {
        if (unlock == null) return true;
        
        // 等级条件
        if (unlock.MinLevel.HasValue && character.Level < unlock.MinLevel.Value)
            return false;
        
        // 账户标记条件
        if (unlock.AccountFlags != null && unlock.AccountFlags.Count > 0)
        {
            // TODO: 实现账户标记检查（需要账户系统支持）
            // 暂时返回 true
        }
        
        // 职业等级条件
        if (unlock.RequiresProfessionLevel.HasValue)
        {
            // TODO: 实现职业等级检查
        }
        
        return true;
    }
}
```

#### 4.2 集成到技能执行流程

在 WindowExecutor 中，技能触发前检查条件：
```csharp
// 1. 检查解锁
if (!ConditionChecker.CheckUnlock(skill.Unlock, character))
{
    LogSkipReason(skill.Id, "not_unlocked");
    continue;
}

// 2. 检查职业限制
if (!IsAllowedProfession(skill, character))
{
    LogSkipReason(skill.Id, "profession_mismatch");
    continue;
}

// 3. 检查条件
if (!ConditionChecker.CheckConditions(skill.Conditions, caster))
{
    LogSkipReason(skill.Id, "conditions_not_met");
    continue;
}

// 4. 检查资源和冷却
// ...
```

#### 4.3 单元测试

**文件：** `BlazorIdle.Tests/Phase4ConditionTests.cs`

**测试用例（18 个）：**
1. ConditionChecker_HpBelowPct_50_AtHp49_ReturnsTrue
2. ConditionChecker_HpBelowPct_50_AtHp51_ReturnsFalse
3. ConditionChecker_HpAbovePct_50_AtHp51_ReturnsTrue
4. ConditionChecker_HpAbovePct_50_AtHp49_ReturnsFalse
5. ConditionChecker_RequireBuffId_BuffPresent_ReturnsTrue
6. ConditionChecker_RequireBuffId_BuffAbsent_ReturnsFalse
7. ConditionChecker_ForbidBuffId_BuffPresent_ReturnsFalse
8. ConditionChecker_ForbidBuffId_BuffAbsent_ReturnsTrue
9. ConditionChecker_RequireResource_Sufficient_ReturnsTrue
10. ConditionChecker_RequireResource_Insufficient_ReturnsFalse
11. ConditionChecker_MultipleConditions_AllMet_ReturnsTrue
12. ConditionChecker_CheckUnlock_MinLevel_Met_ReturnsTrue
13. ConditionChecker_CheckUnlock_MinLevel_NotMet_ReturnsFalse
14. ConditionChecker_CheckUnlock_AccountFlag_Present_ReturnsTrue
15. ConditionChecker_CheckUnlock_AccountFlag_Absent_ReturnsFalse
16. ConditionChecker_ProfessionRestriction_Match_ReturnsTrue
17. ConditionChecker_ProfessionRestriction_Mismatch_ReturnsFalse
18. ConditionChecker_NoConditions_AlwaysReturnsTrue

**验收标准：**
- ✅ 所有 18 个新测试通过
- ✅ 条件不满足不消耗资源/CD
- ✅ 诊断日志记录跳过原因

---

## 四、测试策略

### 4.1 单元测试

- 每个阶段至少 12-20 个单元测试
- 覆盖正常流程和边界情况
- 使用 xUnit + Moq（如需 mock）

### 4.2 集成测试

- 战士完整技能循环测试
- 法师施法技能测试
- 多玩家战斗测试
- 确定性测试（同 RNG 种子）

### 4.3 性能测试

- 1000 tick、3v5 场景 < 50ms
- 内存占用合理
- 无性能回退

---

## 五、风险与缓解

### 5.1 已识别风险

1. **复杂度风险**：12 个阶段，48-62 小时工作量
   - 缓解：严格按阶段实施，每阶段验收后再继续

2. **兼容性风险**：可能影响 Step 0/1 功能
   - 缓解：每阶段运行所有原有测试，确保无回归

3. **性能风险**：技能系统可能引入性能问题
   - 缓解：每阶段进行性能测试，及时优化

4. **UI 风险**：前端集成可能遇到问题
   - 缓解：UI 阶段独立，可以最后实施

### 5.2 回滚策略

- 每个阶段独立提交
- 保留功能开关（如 enable_skill_system）
- 可以回退到任意阶段

---

## 六、交付标准

### 6.1 代码质量

- ✅ 所有新代码通过 CodeQL 扫描
- ✅ 代码风格一致
- ✅ 关键逻辑有注释
- ✅ 公共 API 有 XML 文档注释

### 6.2 测试覆盖

- ✅ 新增 ~178 个单元测试
- ✅ 测试通过率 100%
- ✅ 集成测试覆盖主要场景

### 6.3 文档完整

- ✅ 技能配置指南
- ✅ API 文档
- ✅ 使用示例
- ✅ 常见问题解答

### 6.4 性能达标

- ✅ 1000 tick 战斗 < 50ms
- ✅ 内存占用 < 100MB
- ✅ 无明显性能回退

---

## 七、下一步行动

1. **用户审核**：审查本实施方案，提出修改意见
2. **确认开始**：确认后开始阶段 1 实施
3. **定期汇报**：每个阶段完成后汇报进度

---

**文档版本：** v1.0  
**最后更新：** 2025-11-14  
**维护者：** @copilot  
**状态：** 待审核
