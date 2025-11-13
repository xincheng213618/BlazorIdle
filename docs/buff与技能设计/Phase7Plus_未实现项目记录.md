# Phase 7+ 未实现项目记录

## 文档信息

**创建时间**: 2025-11-13  
**状态**: 待后续实施  
**优先级**: P1 (中等优先级)

---

## 未实现项目清单

### 7.7 ApplyInstantHeal 目标选择灵活性

**当前状态**: 仅支持施法者自疗

**问题描述**:
- 当前 `ApplyInstantHeal` 方法只能治疗施法者自己
- 缺少对其他目标（如队友）的治疗支持
- 治疗术（Healing Spell）等技能无法实现

**建议实现方案**:
1. 添加 `HealTarget` 枚举类型（类似 `BuffTarget`）
   - `Self`: 自己
   - `Target`: 主要目标
   - `AllAllies`: 所有队友
   - `LowestHpAlly`: 血量最低队友
   - `RandomAlly`: 随机队友

2. 修改 `SkillDef` 添加 `HealTarget` 字段
   ```csharp
   public class SkillDef
   {
       // 现有字段...
       public HealTarget HealTarget { get; set; } = HealTarget.Self;
   }
   ```

3. 更新 `ApplyInstantHeal` 方法
   ```csharp
   private void ApplyInstantHeal(
       SkillDef skillDef,
       string casterId,
       string? targetId,
       bool isCasterPlayer)
   {
       var healTargets = ResolveHealTargets(
           skillDef.HealTarget, 
           casterId, 
           targetId, 
           isCasterPlayer);
       
       foreach (var target in healTargets)
       {
           // 应用治疗...
       }
   }
   ```

**预计工作量**: 3-4 小时

**依赖项**: 无

**风险评估**: 低
- 需要添加新的枚举和目标解析逻辑
- 与现有 `ResolveBuffTargets` 模式类似，实现难度较低

**测试需求**:
- 自疗测试
- 目标治疗测试
- 多目标治疗测试（AOE 治疗）
- 智能目标选择测试（血量最低）

---

### 7.8 BuffTemplate 可变性问题

**当前状态**: BuffTemplate 是可变对象，多次使用同一 SkillDef 会共享实例

**问题描述**:
```csharp
var skillDef = new SkillDef
{
    Id = "test_skill",
    OnHitBuffs = new List<BuffOperation>
    {
        new BuffOperation
        {
            BuffTemplate = new BuffInstance(...) // 可变对象
        }
    }
};

// 问题：多次调用 Cast 会修改同一个 BuffTemplate 实例
resolver.Cast("test_skill", ctx);  // 第一次
resolver.Cast("test_skill", ctx);  // 第二次 - 可能会有问题
```

**已知影响**:
1. `BuffTemplate.RemainingDurationSec` 可能被意外修改
2. `BuffTemplate.Stacks` 可能不正确
3. 多线程环境下可能出现竞态条件

**建议实现方案**:

**方案 A: 深拷贝 BuffTemplate（推荐）**
```csharp
private void ApplyBuffOperation(...)
{
    // 创建 BuffTemplate 的副本
    var buffToApply = new BuffInstance(
        id: operation.BuffTemplate.Id,
        ownerId: target.Id,
        kind: operation.BuffTemplate.Kind,
        effects: new List<BuffEffect>(operation.BuffTemplate.Effects),
        stackingPolicy: operation.BuffTemplate.StackingPolicy,
        durationSec: operation.BuffTemplate.RemainingDurationSec, // 使用原始值
        tickIntervalSec: operation.BuffTemplate.TickIntervalSec,
        maxStacks: operation.BuffTemplate.MaxStacks
    );
    
    target.ApplyBuff(buffToApply);
}
```

**方案 B: 不可变设计（需要大重构）**
```csharp
public class BuffTemplate
{
    // 所有属性改为只读
    public string Id { get; init; }
    public BuffKind Kind { get; init; }
    public IReadOnlyList<BuffEffect> Effects { get; init; }
    // ...
}
```

**预计工作量**: 
- 方案 A: 2-3 小时
- 方案 B: 8-10 小时（需要重构大量代码）

**依赖项**: 无

**风险评估**: 中等
- 方案 A 风险低，但需要确保所有嵌套对象也被正确拷贝
- 方案 B 风险高，需要修改大量现有代码

**测试需求**:
- 同一技能多次施放测试
- 并发技能施放测试
- BuffTemplate 独立性测试

---

### 7.12 命中率实现 (AlwaysHits=false)

**当前状态**: 所有技能总是命中 (`AlwaysHits` 默认为 `true`)

**问题描述**:
- 当前 `SkillResolver.Cast` 中有 TODO 注释，但命中率判定尚未实现
- `AlwaysHits=false` 时应该进行命中率判定
- `OnHitBuffs` 应该只在命中时应用

**当前代码**:
```csharp
// TODO Phase 6: 实现命中率检查，当 AlwaysHits=false 时需要滚动命中判定
bool skillHits = skillDef.AlwaysHits || true; // Currently always hits in Step 0
```

**建议实现方案**:

1. 在 `Character` 和 `Enemy` 中添加命中率属性
   ```csharp
   public class Character
   {
       // 现有属性...
       public double HitChancePercent { get; set; } = 95.0; // 默认 95% 命中率
       public double EvadeChancePercent { get; set; } = 5.0; // 默认 5% 闪避率
   }
   
   public class Enemy
   {
       // 现有属性...
       public double EvadeChancePercent { get; set; } = 5.0;
   }
   ```

2. 实现命中率判定逻辑
   ```csharp
   // 在 SkillResolver.Cast 中
   bool skillHits = skillDef.AlwaysHits;
   
   if (!skillHits && !skillId.StartsWith("enemy_") && ctx.Player != null)
   {
       // 玩家攻击，计算命中率
       double hitChance = ctx.Player.HitChancePercent;
       
       // 如果有目标，考虑目标的闪避率
       if (ctx.Enemy != null)
       {
           double evadeChance = ctx.Enemy.EvadeChancePercent;
           double finalHitChance = Math.Max(0, hitChance - evadeChance);
           skillHits = ctx.Rng.NextDouble() < (finalHitChance / 100.0);
       }
       else
       {
           skillHits = ctx.Rng.NextDouble() < (hitChance / 100.0);
       }
   }
   else if (!skillHits && skillId.StartsWith("enemy_") && ctx.Enemy != null)
   {
       // 敌人攻击逻辑类似
       skillHits = true; // 简化：敌人攻击总是命中，或者实现类似逻辑
   }
   
   // OnHitBuffs 只在命中时应用
   if (skillHits)
   {
       result.BuffOperations.AddRange(skillDef.OnHitBuffs);
   }
   ```

3. 添加未命中事件记录
   ```csharp
   if (!skillHits)
   {
       // 记录 MissEvent
       RecordMiss(casterId, targetId, skillId);
   }
   ```

**需求不明确的地方**:
- 命中率和闪避率如何计算？直接相减？还是更复杂的公式？
- 是否需要考虑等级差异？
- 魔法攻击是否也有命中率？还是魔法攻击总是命中？
- 是否需要 "格挡"、"招架" 等其他防御机制？

**预计工作量**: 4-6 小时（取决于需求明确程度）

**依赖项**: 
- 需要明确游戏设计文档中的命中率计算规则
- 可能需要调整角色和敌人的属性系统

**风险评估**: 中等
- 需求不明确可能导致返工
- 影响战斗平衡性，需要仔细调优

**测试需求**:
- 命中率计算测试
- 闪避测试
- OnHitBuffs 只在命中时应用的测试
- 边界情况测试（0% 命中率，100% 命中率）

---

## 实施建议

### 优先级排序

1. **7.8 BuffTemplate 可变性问题** - 高优先级
   - 虽然标记为 P1，但这是一个潜在的 bug
   - 建议尽快实施方案 A（深拷贝）
   - 可能影响生产环境的稳定性

2. **7.7 ApplyInstantHeal 目标灵活性** - 中优先级
   - 功能性改进，不是 bug
   - 当需要实现治疗术等技能时再实施
   - 实现难度低，风险小

3. **7.12 命中率实现** - 低优先级
   - 需求不明确
   - 需要游戏设计文档支持
   - 影响游戏平衡，需要仔细测试

### 何时实施

- **7.8**: 建议在 Phase 9 之前实施，避免在 UI 展示时出现问题
- **7.7**: 当需要实现治疗术技能时实施
- **7.12**: 当游戏设计文档明确命中率机制后实施

### 技术债务评估

当前这三个未实现项目构成的技术债务：

1. **7.8 BuffTemplate 可变性**: 🔴 高风险技术债务
   - 可能导致 bug
   - 应尽快解决

2. **7.7 ApplyInstantHeal 目标**: 🟡 中等技术债务
   - 限制功能扩展
   - 不影响现有功能

3. **7.12 命中率**: 🟢 低技术债务
   - 功能缺失，不是 bug
   - 可以延后实施

---

## 版本记录

| 版本 | 日期 | 变更说明 |
|------|------|---------|
| 1.0 | 2025-11-13 | 初始版本，记录 3 个未实现的 Phase 7+ 项目 |

---

## 参考文档

- [Step1_实施进度追踪.md](./Step1_实施进度追踪.md) - Phase 7 完整实施记录
- [docs_step1_Step1-设计方案.md](./docs_step1_Step1-设计方案.md) - Step 1 设计方案
