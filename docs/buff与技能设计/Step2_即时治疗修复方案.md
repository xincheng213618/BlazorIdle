# Step2 即时治疗（InstantHeal）修复方案

## 📋 问题描述

**当前问题：** InstantHeal 作为 Buff 效果实现，会创建一个buff，治疗后消失。这种实现不合理，因为：
1. 治疗术应该像造成伤害一样，是即时生效的技能效果
2. 不应该创建一个短暂存在的buff
3. HoT（Heal Over Time）才应该作为buff存在并按tick治疗

**正确的设计：**
- **InstantHeal（即时治疗）** → 技能的直接效果，立即治疗，不创建buff
- **HealOverTime（持续治疗）** → Buff效果，创建buff，按tick治疗

---

## 🎯 修复方案

### 1. 数据模型调整

#### 1.1 移除 BuffEffectType.InstantHeal

**文件：** `BlazorIdle.Shared/Game/Buffs/BuffEffectType.cs`

```csharp
public enum BuffEffectType
{
    StatMultiplier,
    StatAdditive,
    ForceCrit,
    DamageOverTime,
    HealOverTime,
    // InstantHeal,  // ❌ 移除：不应该作为Buff效果
    StatReduction,
    Toggle
}
```

**原因：** InstantHeal不应该是Buff的效果类型，因为它不需要持续存在。

#### 1.2 保留 SkillDef.InstantHeal

**文件：** `BlazorIdle.Shared/Game/Skills/SkillDef.cs`

```csharp
/// <summary>
/// 即时治疗量（0 = 无治疗）
/// Instant heal amount (0 = no healing)
/// 注意：这是技能的直接效果，不会创建buff
/// Note: This is a direct skill effect, does not create a buff
/// </summary>
public int InstantHeal { get; set; }
```

**保留原因：** InstantHeal作为技能的直接属性是正确的，就像DamageMultiplier一样。

#### 1.3 移除 BuffEffect.InstantHeal 工厂方法

**文件：** `BlazorIdle.Shared/Game/Buffs/BuffEffect.cs`

```csharp
// ❌ 移除以下方法
// public static BuffEffect InstantHeal(int amount)
// {
//     return new BuffEffect(BuffEffectType.InstantHeal, value: amount);
// }
```

---

### 2. 执行流程调整

#### 2.1 在 SkillResolver 中计算即时治疗

**文件：** `BlazorIdle.Shared/Game/Skills/SkillResolver.cs`

```csharp
public SkillCastResult Cast(string skillId, BattleContext ctx, SkillCastOptions? opts = null)
{
    var skill = _repository.GetSkillById(skillId);
    if (skill == null)
    {
        return new SkillCastResult { SkillId = skillId, BundleId = opts?.BundleId };
    }

    // ... existing damage calculation ...

    return new SkillCastResult
    {
        DamageDealt = finalDamage,
        IsCrit = isCrit,
        InstantHeal = skill.InstantHeal, // ✅ 直接从技能定义获取
        BuffOperations = buffOps,
        ResourceChanges = resourceChanges,
        SkillId = skillId,
        BundleId = opts?.BundleId
    };
}
```

#### 2.2 在 MultiBattleInstance 中应用即时治疗

**文件：** `BlazorIdle.Shared/Game/MultiBattleInstance.cs`

**当前实现（有问题）：**
```csharp
private void ApplyInstantHeal(
    SkillCastResult result,
    string casterId,
    string? targetId,
    bool isCasterPlayer,
    string? skillId = null)
{
    if (result.InstantHeal <= 0)
        return;

    // 获取目标（通常是施法者自己）
    Buffs.IBuffOwner? target = null;
    if (isCasterPlayer)
    {
        if (_playerBuffOwners.TryGetValue(casterId, out var playerOwner))
            target = playerOwner;
    }
    else
    {
        if (_enemyBuffOwners.TryGetValue(casterId, out var enemyOwner))
            target = enemyOwner;
    }

    if (target != null)
    {
        // ✅ 直接应用治疗，不创建buff
        var healMeta = new Buffs.HealMeta("instant_heal", "skill_cast");
        int healAmount = result.InstantHeal;
        target.ReceiveHeal(healAmount, healMeta);
        
        // ✅ 记录治疗事件
        RecordHeal(
            ownerId: target.Id,
            amount: healAmount,
            resultingHp: target.CurrentHp,
            healSource: skillId ?? "unknown_skill",
            bundleId: result.BundleId
        );
    }
}
```

**说明：** 当前实现已经是正确的！它直接调用 `target.ReceiveHeal()` 而不创建buff。问题在于BuffEffectType中不应该有InstantHeal。

---

### 3. 清理工作

#### 3.1 移除 BuffInstance 中的 InstantHeal 相关方法

**文件：** `BlazorIdle.Shared/Game/Buffs/BuffInstance.cs`

```csharp
// ❌ 移除以下方法
// public bool HasInstantHeal()
// {
//     return Effects.Exists(e => e.Type == BuffEffectType.InstantHeal);
// }

// public int GetInstantHealAmount()
// {
//     int total = 0;
//     foreach (var effect in Effects)
//     {
//         if (effect.Type == BuffEffectType.InstantHeal)
//         {
//             total += (int)effect.Value;
//         }
//     }
//     return total;
// }
```

**原因：** 既然InstantHeal不再是Buff效果，这些方法就没有存在的意义了。

#### 3.2 更新 BuffConfig

**文件：** `BlazorIdle.Shared/Config/buffs.json`

确保没有buff配置使用 InstantHeal 效果类型：

```json
// ❌ 错误示例（不应该这样配置）
{
  "id": "instant_heal_buff",
  "effects": [
    {
      "type": "InstantHeal",  // ❌ 不应该在buff中使用
      "value": 50
    }
  ]
}

// ✅ 正确做法：在技能中直接设置
// skills.json
{
  "id": "healing_spell",
  "instantHeal": 50,  // ✅ 作为技能属性
  "onCastBuffs": []   // 不创建buff
}
```

---

### 4. 技能配置示例

#### 4.1 治疗术（Instant Heal）

```json
{
  "id": "adrenaline_rush",
  "name": "肾上腺素",
  "description": "立即恢复20% MaxHP",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "self",
  "cooldownSec": 20.0,
  "allowedProfessions": [],
  "unlock": {
    "minLevel": 1
  },
  "conditions": {
    "hpBelowPct": 50
  },
  "instantHeal": 0,
  "damage": null,
  "onCastBuffs": [],
  "onHitBuffs": []
}
```

**注意：** InstantHeal的数值可以在技能使用时动态计算（比如基于MaxHP的百分比）。这需要在SkillResolver中实现特殊逻辑。

#### 4.2 持续治疗（HoT Buff）

```json
{
  "id": "rejuvenation",
  "name": "恢复术",
  "description": "持续恢复生命值",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "allies_lowest_hp_pct",
  "cooldownSec": 12.0,
  "instantHeal": 0,
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "rejuvenation_hot",
      "targetOverride": "target"
    }
  ]
}
```

**对应的Buff配置（buffs.json）：**
```json
{
  "id": "rejuvenation_hot",
  "name": "恢复术",
  "description": "持续恢复生命值",
  "icon": "💚",
  "kind": "buff",
  "durationSec": 12.0,
  "stackingPolicy": "refresh",
  "maxStacks": 1,
  "tickIntervalSec": 3.0,
  "effects": [
    {
      "type": "HealOverTime",  // ✅ 正确：HoT作为buff效果
      "amountPerTick": 20
    }
  ],
  "defaultTarget": "target"
}
```

---

### 5. 实施计划

#### 5.1 实施阶段

**在阶段 5（资源消耗与冷却）或阶段 6（Window-GCD机制）中实施**

**任务清单：**

- [ ] 5.1 移除 BuffEffectType.InstantHeal 枚举值
- [ ] 5.2 移除 BuffEffect.InstantHeal() 工厂方法
- [ ] 5.3 移除 BuffInstance.HasInstantHeal() 方法
- [ ] 5.4 移除 BuffInstance.GetInstantHealAmount() 方法
- [ ] 5.5 确认 SkillDef.InstantHeal 保留
- [ ] 5.6 确认 ApplyInstantHeal 方法正确（已经是直接治疗）
- [ ] 5.7 更新单元测试（移除InstantHeal buff相关测试）
- [ ] 5.8 添加InstantHeal技能效果测试
- [ ] 5.9 更新文档注释

**预计工作量：** 1-2小时

#### 5.2 测试更新

**移除的测试（BuffSystemTests.cs）：**
```csharp
// ❌ 移除
[Fact]
public void BuffInstance_GetInstantHealAmount_SumsAllInstantHeals()
{
    // 这个测试不再需要，因为InstantHeal不再是buff效果
}

[Fact]
public void BuffInstance_HasInstantHeal_ReturnsTrueWhenPresent()
{
    // 这个测试不再需要
}
```

**新增的测试（SkillSystemTests.cs）：**
```csharp
[Fact]
public void SkillResolver_Cast_WithInstantHeal_ReturnsHealAmount()
{
    // 测试技能的instantHeal属性正确返回
    var skill = new SkillDef
    {
        Id = "healing_spell",
        InstantHeal = 50
    };
    
    var result = skillResolver.Cast("healing_spell", context);
    
    Assert.Equal(50, result.InstantHeal);
}

[Fact]
public void MultiBattleInstance_ApplyInstantHeal_IncreasesHp()
{
    // 测试即时治疗直接增加HP，不创建buff
    var result = new SkillCastResult
    {
        InstantHeal = 50
    };
    
    // 应用治疗前HP
    int hpBefore = character.CurrentHp;
    
    // 应用治疗
    battleInstance.ApplyInstantHeal(result, characterId, null, true);
    
    // 验证HP增加，但没有创建buff
    Assert.Equal(hpBefore + 50, character.CurrentHp);
    Assert.Empty(character.Buffs); // 没有buff被创建
}
```

---

### 6. 对现有代码的影响

#### 6.1 兼容性分析

**好消息：** 影响范围很小！

1. **SkillDef.InstantHeal** - 保持不变，技能配置不受影响
2. **ApplyInstantHeal方法** - 已经是正确实现，不需要修改
3. **BuffEffectType** - 移除InstantHeal，但没有技能/buff在使用它

**需要检查的地方：**
```bash
# 搜索是否有代码使用 BuffEffectType.InstantHeal
grep -r "BuffEffectType.InstantHeal" --include="*.cs"

# 搜索是否有buff配置使用 InstantHeal 效果
grep -r '"type".*"InstantHeal"' --include="*.json"
```

如果搜索结果为空或只有测试代码，说明影响很小。

#### 6.2 迁移步骤

1. **第一步：移除枚举和方法**
   - 移除 BuffEffectType.InstantHeal
   - 移除相关工厂方法和辅助方法

2. **第二步：验证现有代码**
   - 确认没有技能使用 InstantHeal buff效果
   - 确认 ApplyInstantHeal 方法正常工作

3. **第三步：更新测试**
   - 移除 BuffInstance InstantHeal 相关测试
   - 添加 SkillDef InstantHeal 相关测试

4. **第四步：更新文档**
   - 更新设计文档说明治疗机制
   - 添加技能配置示例

---

### 7. 设计原则确认

#### 7.1 技能效果 vs Buff效果

**技能直接效果（不创建buff）：**
- ✅ Damage（伤害）
- ✅ InstantHeal（即时治疗）
- ✅ ResourceGain（资源获得）
- ✅ ResourceCost（资源消耗）

**Buff效果（创建buff，持续存在）：**
- ✅ StatMultiplier（属性乘法）
- ✅ StatAdditive（属性加法）
- ✅ DamageOverTime（持续伤害）
- ✅ HealOverTime（持续治疗）
- ✅ ForceCrit（强制暴击）
- ✅ StatReduction（属性减少）

#### 7.2 为什么这样设计？

1. **简洁性**：InstantHeal是一次性效果，不需要buff的生命周期管理
2. **性能**：减少buff创建和销毁的开销
3. **清晰性**：Buff列表只显示持续性效果，不会被瞬时效果干扰
4. **一致性**：与Damage的处理方式保持一致

---

## 📊 总结

### 核心改动

1. **移除** BuffEffectType.InstantHeal
2. **保留** SkillDef.InstantHeal（作为技能属性）
3. **保持** ApplyInstantHeal 方法的当前实现（已经是正确的）
4. **移除** BuffInstance 中的 InstantHeal 相关方法

### 优势

- ✅ InstantHeal 像 Damage 一样立即生效
- ✅ 不创建临时buff，buff列表更清晰
- ✅ 性能更好（减少buff创建/销毁）
- ✅ 代码更容易理解和维护

### 实施建议

在 **阶段 5 或 阶段 6** 中实施，工作量约 1-2 小时。由于当前 ApplyInstantHeal 方法已经是正确实现，主要工作是清理不必要的 BuffEffectType 和相关方法。

---

**创建日期：** 2025-11-14  
**维护者：** @copilot  
**状态：** 待实施
