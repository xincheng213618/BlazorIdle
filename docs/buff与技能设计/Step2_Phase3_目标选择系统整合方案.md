# Step 2 Phase 3+ 目标选择系统整合方案

## 文档说明

本文档描述如何将Phase 3的目标选择系统完整整合到战斗系统中，使其从临时测试状态转变为生产就绪状态。

## 当前状态 (Current State)

### 已完成 ✅
1. **TargetSelector系统** - 完整实现并测试通过
   - 支持5种目标策略：CurrentTarget, EnemiesAll, AlliesLowestHpPct, Self, AlliesAll
   - 单元测试覆盖全面（Step2Phase3Tests.cs）

2. **SkillResolver集成** - TargetSelector已集成到SkillResolver
   - SkillDef.TargetPolicy字段定义目标策略
   - SkillCastResult.TargetIds存储解析的目标列表
   - 集成测试通过（Step2Phase3IntegrationTests.cs）

3. **临时测试机制** - MultiBattleInstance方法支持临时参数
   - `ProcessCharacterAttackViaSkillResolver(charId, character, targetPolicyOverride)`
   - `ProcessCharacterSpecialViaSkillResolver(charId, character, targetPolicyOverride)`
   - 可通过传入如"EnemiesAll"测试AOE效果

### 临时状态 ⚠️
- 方法参数`targetPolicyOverride`是临时的测试参数
- 仍然使用硬编码的`SkillIds.AttackBasic`和`SkillIds.SpecialPulse`
- 没有从职业配置读取固定技能

### 已移除 ❌
- `MultiBattleConfig.AttackTargetPolicy` - 已删除
- `MultiBattleConfig.SpecialTargetPolicy` - 已删除

## 整合目标 (Integration Goals)

将目标选择系统从"测试状态"升级为"生产状态"：

1. **职业固定技能配置** - 在professionAttributes.json中定义每个职业的固定技能
2. **动态技能加载** - MultiBattleInstance从职业配置读取技能ID
3. **完全使用SkillDef** - 目标策略完全来自skills.json的targetPolicy字段
4. **移除临时参数** - 删除targetPolicyOverride测试参数

## 整合方案 (Integration Plan)

### 第一步：扩展职业配置

**文件：** `BlazorIdle.Server/Config/professionAttributes.json`

为每个职业添加`fixedSkills`配置：

```json
{
  "warrior": {
    "id": "warrior",
    "name": "战士",
    // ... 现有配置 ...
    "fixedSkills": {
      "normalAttack": "warrior_attack_basic",
      "specialAttack": "warrior_special_pulse"
    }
  },
  "mage": {
    "id": "mage",
    "name": "法师",
    // ... 现有配置 ...
    "fixedSkills": {
      "normalAttack": "mage_attack_basic",
      "specialAttack": "mage_special_pulse"
    }
  },
  "ranger": {
    "id": "ranger",
    "name": "游侠",
    // ... 现有配置 ...
    "fixedSkills": {
      "normalAttack": "ranger_attack_basic",
      "specialAttack": "ranger_special_pulse"
    }
  }
}
```

**新增类型定义：**

```csharp
// BlazorIdle.Shared/Models/ProfessionAttributeConfig.cs
public class ProfessionFixedSkills
{
    public string NormalAttack { get; set; } = "";
    public string SpecialAttack { get; set; } = "";
}

// 在ProfessionAttributeConfig中添加
public ProfessionFixedSkills? FixedSkills { get; set; }
```

### 第二步：修改MultiBattleInstance

**文件：** `BlazorIdle.Shared/Game/MultiBattleInstance.cs`

#### 2.1 添加职业技能映射缓存

```csharp
private readonly Dictionary<string, (string normalAttack, string specialAttack)> _professionSkills = new();

// 在构造函数中初始化
public MultiBattleInstance(/* 现有参数 */, IGameConfigProvider? gameConfig = null)
{
    // ... 现有初始化 ...
    
    // 加载职业固定技能配置
    if (gameConfig != null)
    {
        LoadProfessionSkills(gameConfig);
    }
}

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
```

#### 2.2 修改攻击方法使用动态技能ID

```csharp
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

    // ... 其余代码保持不变，使用skillId而不是SkillIds.AttackBasic ...
    var result = _skillResolver.Cast(skillId, ctx, opts);
    
    // 直接使用result.TargetIds（来自技能的targetPolicy）
    List<string> targetIds = result.TargetIds?.Count > 0 ? result.TargetIds : 
        (defaultTargetId != null ? new List<string> { defaultTargetId } : new List<string>());
    
    // ... 应用伤害到所有目标 ...
}
```

#### 2.3 修改特殊技能方法

```csharp
private void ProcessCharacterSpecialViaSkillResolver(string charId, Character character)
{
    var member = _playerTeam.GetMember(charId);
    if (member == null) return;

    // 获取角色职业的固定技能ID
    string skillId = SkillIds.SpecialPulse; // 默认
    if (_professionSkills.TryGetValue(member.Entity.ActiveCombatProfessionId, out var skills))
    {
        skillId = skills.specialAttack;
    }

    // ... 其余代码保持不变，使用skillId而不是SkillIds.SpecialPulse ...
    var result = _skillResolver.Cast(skillId, ctx, opts);
    
    // 优先使用result.TargetIds，然后是SpecialIsAoe（向后兼容）
    List<string> targetIds;
    if (result.TargetIds?.Count > 0)
    {
        targetIds = result.TargetIds;
    }
    else if (_config.SpecialIsAoe)
    {
        // 向后兼容
        targetIds = _enemyTeam.GetAliveMemberIds();
    }
    else
    {
        targetIds = defaultTargetId != null ? new List<string> { defaultTargetId } : new List<string>();
    }
    
    // ... 应用效果到所有目标 ...
}
```

### 第三步：验证skills.json配置

确保skills.json中所有职业技能都有正确的targetPolicy：

```json
{
  "id": "warrior_attack_basic",
  "targetPolicy": "current_target",  // ✅ 单体攻击
  // ...
}
{
  "id": "warrior_special_pulse",
  "targetPolicy": "self",  // ✅ 自我buff
  // ...
}
{
  "id": "mage_attack_basic",
  "targetPolicy": "current_target",  // ✅ 单体攻击
  // ...
}
{
  "id": "mage_fireball",
  "targetPolicy": "enemies_all",  // ✅ AOE技能
  // ...
}
```

### 第四步：更新SkillRepository默认技能

**文件：** `BlazorIdle.Shared/Game/Skills/SkillRepository.cs`

保持默认技能作为回退，但确保它们有targetPolicy：

```csharp
private void InitializeDefaultSkills()
{
    RegisterSkill(new SkillDef
    {
        Id = SkillIds.AttackBasic,
        DamageMultiplier = 1.0,
        CanCrit = true,
        AlwaysHits = true,
        TargetPolicy = "CurrentTarget"  // ✅ 已设置
    });

    RegisterSkill(new SkillDef
    {
        Id = SkillIds.SpecialPulse,
        DamageMultiplier = 1.0,
        CanCrit = true,
        AlwaysHits = true,
        IsAoe = true,
        TargetPolicy = "EnemiesAll",  // ✅ 已设置
        // ...
    });
    
    // ...
}
```

## 实施步骤 (Implementation Steps)

### 阶段1：配置准备（无代码修改）
1. ✅ 设计professionAttributes.json的fixedSkills结构
2. ✅ 审查skills.json确保所有技能都有targetPolicy
3. ✅ 编写本整合方案文档

### 阶段2：模型扩展
1. 添加`ProfessionFixedSkills`类
2. 在`ProfessionAttributeConfig`中添加`FixedSkills`属性
3. 更新professionAttributes.json添加fixedSkills配置
4. 测试配置加载

### 阶段3：MultiBattleInstance整合
1. 添加`_professionSkills`缓存和`LoadProfessionSkills`方法
2. 修改构造函数接受`IGameConfigProvider`
3. 更新`ProcessCharacterAttackViaSkillResolver`使用动态技能ID
4. 更新`ProcessCharacterSpecialViaSkillResolver`使用动态技能ID
5. 移除`targetPolicyOverride`临时参数

### 阶段4：测试验证
1. 单元测试：职业配置加载
2. 集成测试：不同职业使用各自的技能
3. 功能测试：目标选择在实际战斗中工作
4. 回归测试：确保现有功能不受影响

### 阶段5：清理和文档
1. 移除所有临时测试代码
2. 更新API文档
3. 更新设计文档
4. 代码审查

## 测试用例 (Test Cases)

### 单元测试

```csharp
[Fact]
public void LoadProfessionSkills_LoadsCorrectSkillIds()
{
    // Arrange
    var gameConfig = CreateMockGameConfig();
    var battle = new MultiBattleInstance(/* params */, gameConfig);
    
    // Act
    var warriorSkills = battle.GetProfessionSkills("warrior");
    
    // Assert
    Assert.Equal("warrior_attack_basic", warriorSkills.normalAttack);
    Assert.Equal("warrior_special_pulse", warriorSkills.specialAttack);
}
```

### 集成测试

```csharp
[Fact]
public void WarriorAttack_UsesCorrectTargetPolicy()
{
    // Arrange
    var warrior = CreateWarriorCharacter();
    var battle = CreateBattleWithWarrior(warrior);
    
    // Act
    battle.ProcessAttack(warrior.Id);
    var events = battle.GetCombatEvents();
    
    // Assert
    // 战士普通攻击应该是单体(current_target)
    Assert.Single(events.Where(e => e.Type == "damage"));
}

[Fact]
public void MageFireball_HitsAllEnemies()
{
    // Arrange
    var mage = CreateMageCharacter();
    var battle = CreateBattleWithMageAndThreeEnemies(mage);
    
    // Act
    battle.ProcessSpecial(mage.Id); // 假设法师特殊是AOE
    var events = battle.GetCombatEvents();
    
    // Assert
    // 火球术应该打击所有敌人(enemies_all)
    Assert.Equal(3, events.Count(e => e.Type == "damage"));
}
```

## 向后兼容性 (Backward Compatibility)

### 保留的兼容性
1. **SpecialIsAoe配置** - 保留作为回退机制
2. **默认技能** - 如果职业配置缺失，使用SkillIds.AttackBasic/SpecialPulse
3. **现有测试** - 所有现有测试应继续通过

### 迁移路径
1. 旧代码：直接使用SkillIds.AttackBasic
2. 过渡期：支持职业配置和硬编码（当前状态）
3. 新代码：完全依赖职业配置（目标状态）

## 性能考虑 (Performance Considerations)

### 缓存策略
- `_professionSkills` Dictionary缓存技能ID映射
- 避免每次攻击都查询配置
- O(1)查找性能

### 内存占用
- 每个职业2个字符串引用
- 预计3个职业 = 6个字符串引用
- 可忽略不计的内存开销

## 风险和缓解 (Risks and Mitigation)

### 风险1：配置缺失或错误
**缓解：**
- 回退到默认技能ID
- 启动时验证配置
- 单元测试覆盖配置加载

### 风险2：技能ID不存在
**缓解：**
- SkillRepository.GetSkill返回null时记录错误
- 使用默认技能作为回退
- 集成测试验证所有配置的技能ID存在

### 风险3：破坏现有功能
**缓解：**
- 保持向后兼容
- 完整的回归测试
- 代码审查

## 成功标准 (Success Criteria)

✅ **必须达成：**
1. 所有职业都从配置文件加载固定技能
2. 目标选择完全基于SkillDef.TargetPolicy
3. 移除所有临时测试参数
4. 所有现有测试通过
5. 新增测试覆盖职业技能加载

✅ **加分项：**
1. 支持动态切换职业时更新技能
2. 技能装备系统扩展预留接口
3. 性能无退化

## 时间估算 (Time Estimate)

- 阶段1（配置准备）：✅ 已完成
- 阶段2（模型扩展）：1-2小时
- 阶段3（MultiBattleInstance整合）：2-3小时
- 阶段4（测试验证）：2-3小时
- 阶段5（清理和文档）：1小时

**总计：6-9小时**

## 参考文档 (References)

- `Step2_实施进度追踪.md` - Phase 3实施状态
- `Step2_Phase1-3_PR总结.md` - Phase 1-3总结
- `Step2_补充设计-技能学习与职业差异化-上篇.md` - 技能系统设计
- `skills.json` - 技能配置文件
- `professionAttributes.json` - 职业属性配置

## 下一步行动 (Next Actions)

1. **Review** - 团队审查本整合方案
2. **Approve** - 获得方案批准
3. **Branch** - 创建新PR分支 `feature/phase3-skill-integration`
4. **Implement** - 按阶段2-5实施
5. **Test** - 完整测试验证
6. **Merge** - 合并到主分支

---

**文档版本：** 1.0  
**创建日期：** 2025-11-17  
**作者：** GitHub Copilot  
**状态：** 待审查
