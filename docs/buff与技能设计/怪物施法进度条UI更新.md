# 怪物施法进度条 UI 更新
# Monster Casting Progress Bar UI Update

**日期 / Date:** 2025-11-20  
**更新者 / Updated by:** @copilot  
**类型 / Type:** UI Enhancement  

---

## 更新概述 / Update Overview

为 `EnemyTeamPanel.razor` 组件添加怪物施法进度条显示功能，使其与 `CharacterPanel.razor` 的行为保持一致。

---

## 功能对比 / Feature Comparison

### 修改前 / Before

**EnemyTeamPanel（怪物面板）：**
- ✅ 显示怪物 HP 条
- ✅ 显示 Buff/Debuff 图标
- ✅ 显示攻击进度条（红色）
- ❌ **不显示施法进度条**

**CharacterPanel（玩家面板）：**
- ✅ 显示玩家 HP 条
- ✅ 显示资源条（法力/怒气等）
- ✅ 显示 Buff/Debuff 图标
- ✅ 显示攻击进度条（蓝色）
- ✅ **显示施法进度条（黄色）**

### 修改后 / After

**EnemyTeamPanel（怪物面板）：**
- ✅ 显示怪物 HP 条
- ✅ 显示 Buff/Debuff 图标
- ✅ 显示攻击进度条（红色）
- ✅ **显示施法进度条（黄色）** ⭐ **新增**

**一致性：** 现在怪物和玩家的 UI 行为完全一致！

---

## UI 显示逻辑 / UI Display Logic

### 怪物施法时 / When Monster is Casting

```
┌─────────────────────────────────┐
│ Enemy Name               HP Bar │
│ ┌─────────────────────────────┐ │
│ │ Buffs/Debuffs              │ │
│ └─────────────────────────────┘ │
│ 施法中 Casting          75%     │
│ ┌─────────────────────────────┐ │
│ │████████████████░░░░░░░░░░░░│ │  ← 黄色施法条
│ │    火球术 Fireball          │ │  ← 技能名称
│ └─────────────────────────────┘ │
│          剩余：450 ms           │
└─────────────────────────────────┘
```

### 怪物未施法时 / When Monster is Not Casting

```
┌─────────────────────────────────┐
│ Enemy Name               HP Bar │
│ ┌─────────────────────────────┐ │
│ │ Buffs/Debuffs              │ │
│ └─────────────────────────────┘ │
│ 攻击进度                        │
│ ┌─────────────────────────────┐ │
│ │████████████████████░░░░░░░░│ │  ← 红色攻击条
│ └─────────────────────────────┘ │
│       下次攻击：1200 ms         │
└─────────────────────────────────┘
```

---

## 实现细节 / Implementation Details

### 1. HTML 结构 / HTML Structure

**修改前（仅攻击进度）：**
```razor
@if (!m.IsDead)
{
    <div class="small">
        <div class="text-muted mb-1">攻击进度</div>
        <div class="progress">
            <div class="progress-bar bg-danger" ...></div>
        </div>
        <div class="text-end text-muted mt-1">
            下次攻击：@(...) ms
        </div>
    </div>
}
```

**修改后（条件显示施法或攻击）：**
```razor
@if (!m.IsDead)
{
    <div class="small">
        @if (IsEnemyCasting(m.Id))
        {
            <!-- 施法进度条 -->
            <div class="d-flex justify-content-between text-muted mb-1">
                <span>施法中 Casting</span>
                <span>@($"{GetEnemyCastingProgress(m.Id) * 100:0}%")</span>
            </div>
            <div class="progress">
                <div class="progress-bar bg-warning" ...>
                    <small class="casting-skill-name">
                        @GetEnemyCastingSkillName(m.Id)
                    </small>
                </div>
            </div>
            <div class="text-end text-muted mt-1">
                剩余：@(...) ms
            </div>
        }
        else
        {
            <!-- 攻击进度条（原有逻辑） -->
            <div class="text-muted mb-1">攻击进度</div>
            <div class="progress">
                <div class="progress-bar bg-danger" ...></div>
            </div>
            <div class="text-end text-muted mt-1">
                下次攻击：@(...) ms
            </div>
        }
    </div>
}
```

### 2. C# 辅助方法 / C# Helper Methods

```csharp
// Phase 8: 怪物施法相关方法 / Monster casting related methods
private bool IsEnemyCasting(string enemyId)
    => Battle?.IsCastingForCharacter(enemyId) ?? false;

private double GetEnemyCastingProgress(string enemyId)
    => Battle?.GetCastingProgress(enemyId) ?? 0.0;

private double GetEnemyCastingRemainMs(string enemyId)
    => Battle?.GetCastingTimeRemaining(enemyId) ?? 0.0;

private string? GetEnemyCastingSkillName(string enemyId)
{
    var skillId = Battle?.GetCastingSkillId(enemyId);
    if (string.IsNullOrEmpty(skillId))
        return null;

    var skillRepo = Battle?.GetSkillRepository();
    var skill = skillRepo?.GetSkill(skillId);
    return skill?.Name ?? skillId;
}
```

### 3. CSS 样式 / CSS Styles

**新增样式：**
```css
/* Phase 8: 施法条样式 / Casting bar styles */
.enemy-team .progress-bar.bg-warning {
    background-color: #ffc107 !important;
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
}

.enemy-team .casting-skill-name {
    color: #000;
    font-weight: 600;
    font-size: 10px;
    position: absolute;
    white-space: nowrap;
    text-shadow: 0 0 2px rgba(255, 255, 255, 0.8);
}
```

---

## 技术说明 / Technical Notes

### 方法复用 / Method Reuse

现有的施法相关方法对玩家和怪物通用：

| 方法 | 参数 | 说明 |
|------|------|------|
| `IsCastingForCharacter` | casterId | 检查是否正在施法（玩家或怪物） |
| `GetCastingProgress` | casterId | 获取施法进度 0.0-1.0 |
| `GetCastingTimeRemaining` | casterId | 获取剩余时间（毫秒） |
| `GetCastingSkillId` | casterId | 获取施法技能 ID |

**关键点：** 这些方法在 `MultiBattleInstance` 中使用 `CastingController`，而 `CastingController` 是通用的（不区分玩家和怪物）。

### 设计一致性 / Design Consistency

**颜色方案：**
- 🔵 **蓝色** - 玩家攻击进度条
- 🔴 **红色** - 怪物攻击进度条
- 🟡 **黄色** - 施法进度条（玩家和怪物通用）

**信息显示：**
- **左侧** - 进度类型（"施法中 Casting" 或 "攻击进度"）
- **右侧** - 百分比（施法）或无（攻击）
- **进度条中心** - 技能名称（仅施法时）
- **进度条下方** - 剩余时间或下次触发时间

---

## 测试验证 / Testing Verification

### 单元测试 / Unit Tests

```
Passed!  - Failed: 0, Passed: 632, Skipped: 0, Total: 632
```

✅ 所有测试通过，无破坏性变更

### 手动测试场景 / Manual Test Scenarios

1. **怪物未施法时：**
   - 应显示红色攻击进度条
   - 显示"攻击进度"标签
   - 显示下次攻击时间

2. **怪物开始施法时：**
   - 攻击进度条应切换为黄色施法条
   - 显示"施法中 Casting"标签
   - 显示施法百分比
   - 显示技能名称
   - 显示剩余时间

3. **怪物施法完成后：**
   - 施法条应切换回攻击进度条
   - 恢复显示攻击相关信息

4. **多个怪物同时施法：**
   - 每个怪物应独立显示各自的施法状态
   - 不应互相干扰

---

## 用户体验提升 / UX Improvements

### 修改前的问题 / Issues Before

1. **信息不完整：** 怪物施法时，UI 仍显示攻击进度条，无法看到施法状态
2. **行为不一致：** 玩家有施法进度显示，但怪物没有
3. **难以追踪：** 无法通过 UI 判断怪物何时施法、施放什么技能

### 修改后的改进 / Improvements After

1. **信息完整：** 怪物施法时清晰显示施法进度和技能名称
2. **行为一致：** 玩家和怪物的 UI 表现完全一致
3. **易于追踪：** 可以实时看到怪物的施法状态和进度

---

## 后续可能的增强 / Possible Future Enhancements

1. **施法条颜色差异：** 可以根据技能类型（火焰/冰霜/奥术等）显示不同颜色
2. **施法打断指示：** 当施法被打断时显示特殊动画
3. **施法队列显示：** 显示怪物接下来可能施放的技能
4. **施法范围指示：** 对 AOE 技能显示范围提示

---

## 文件变更清单 / File Changes

| 文件 | 类型 | 变更内容 |
|------|------|---------|
| `EnemyTeamPanel.razor` | 修改 | 添加施法进度条显示逻辑 |
| `EnemyTeamPanel.razor.css` | 修改 | 添加施法条样式 |

**代码统计：**
- 新增行数：+79
- 删除行数：-14
- 净增加：+65 行

---

## 总结 / Summary

### 实现的功能 / Implemented Features

- ✅ 怪物施法进度条显示
- ✅ 施法技能名称显示
- ✅ 施法百分比和剩余时间
- ✅ 与 CharacterPanel 行为一致

### 技术亮点 / Technical Highlights

- 📦 复用现有 API（无需新增后端方法）
- 🎨 保持 UI 设计一致性
- 🧪 所有测试通过
- 🔧 代码简洁，易于维护

### 用户价值 / User Value

- 🎯 更清晰的战斗信息展示
- 🔍 更好的怪物行为可见性
- 📊 更完整的战斗状态监控

---

**更新完成 / Update Completed:** 2025-11-20  
**测试状态 / Test Status:** ✅ 通过 / Passed  
**部署状态 / Deployment Status:** ⏳ 待用户验证 / Pending User Verification
