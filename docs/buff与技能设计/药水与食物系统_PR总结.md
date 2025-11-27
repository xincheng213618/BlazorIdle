# 药水与食物系统扩展 - PR 总结

**PR 标题：** Implement potion and food system expansion  
**完成日期：** 2025-11-26  
**实际工时：** 8.5 小时（预估 13.5 小时）  
**测试结果：** 817 个测试全部通过  

---

## 📋 功能概述

本 PR 实现了独立的药水和食物消耗品栏位系统，支持战斗中自动触发、物品真实消耗、配置化管理。

### 核心功能

1. **新增 4 个消耗品栏位**
   - 2 个药水槽位（potion_1, potion_2）- 提供增益效果
   - 2 个食物槽位（food_1, food_2）- 提供恢复效果

2. **引用模式装备**
   - 槽位只保存物品 ID 引用
   - 每次使用实时从背包扣除物品
   - 补充库存后自动恢复可用，无需重新配置

3. **战斗自动触发**
   - 基于触发条件自动使用（如 HP < 50%）
   - 触发顺序：先药水后食物，按槽位顺序
   - 冷却按物品 ID 追踪（同类物品共享冷却）

4. **UI 交互**
   - 点击槽位显示可装备物品列表
   - 实时显示库存数量
   - 库存耗尽显示特殊样式
   - 战斗中锁定配置

---

## 📁 新增文件清单

### 数据模型

| 文件 | 说明 |
|------|------|
| `BlazorIdle.Shared/Models/ConsumableSlotData.cs` | 消耗品槽位数据（ItemId, SkillId） |
| `BlazorIdle.Shared/Models/ConsumableEquipmentConfig.cs` | 消耗品装备配置（2药水+2食物槽位） |
| `BlazorIdle.Shared/Models/ConsumableConfig.cs` | 消耗品配置（category, skillId, triggerConditions, cooldownSec） |

### 业务逻辑

| 文件 | 说明 |
|------|------|
| `BlazorIdle.Shared/Game/Consumables/ConsumableManager.cs` | 消耗品管理器（装备/卸载/查询） |
| `BlazorIdle.Shared/Config/consumableSkills.json` | 8 个消耗品技能配置 |

### UI 组件

| 文件 | 说明 |
|------|------|
| `BlazorIdle/Components/ConsumableSlotPanel.razor` | 消耗品装备面板（4槽位+选择弹窗） |
| `BlazorIdle/Components/ConsumableIcon.razor` | 消耗品图标组件（冷却显示+小巧样式） |

### 测试文件

| 文件 | 测试数量 |
|------|----------|
| `BlazorIdle.Tests/ConsumableSystemPhase1Tests.cs` | 24 个测试 |
| `BlazorIdle.Tests/ConsumableSystemPhase2Tests.cs` | 12 个测试 |
| `BlazorIdle.Tests/ConsumableSystemPhase3Tests.cs` | 10 个测试 |
| `BlazorIdle.Tests/ConsumableSystemPhase4Tests.cs` | 51 个测试 |
| `BlazorIdle.Tests/ConsumableSystemPhase5Tests.cs` | 23 个测试 |

---

## 📝 修改文件清单

### 数据模型扩展

| 文件 | 变更内容 |
|------|----------|
| `BlazorIdle.Shared/Models/CharacterData.cs` | 添加 `EquippedConsumables` 字段 |
| `BlazorIdle.Shared/Models/Item.cs` | 添加 `ConsumableConfig` 字段到 `ItemDefinition` |

### 战斗逻辑

| 文件 | 变更内容 |
|------|----------|
| `BlazorIdle.Shared/Game/MultiBattleInstance.cs` | 添加消耗品处理逻辑：`ProcessCharacterConsumableChecks`、`ProcessConsumableSlot`、冷却查询方法 |
| `BlazorIdle.Shared/Game/CombatModels.cs` | 添加 `EventSource.Consumable`、`ConsumableUsedEvent`、`ConsumableOutOfStockEvent` |
| `BlazorIdle.Shared/Game/DungeonManager.cs` | 传递 `gameConfig` 到 `MultiBattleInstance` 以支持副本中消耗品 |

### 配置加载

| 文件 | 变更内容 |
|------|----------|
| `BlazorIdle.Shared/Game/Skills/SkillRepository.cs` | 加载 `consumableSkills.json` |
| `BlazorIdle.Shared/BlazorIdle.Shared.csproj` | 添加 `consumableSkills.json` 嵌入资源 |

### 配置文件

| 文件 | 变更内容 |
|------|----------|
| `BlazorIdle.Shared/Config/items.json` | 为消耗品添加 `consumableConfig` |
| `BlazorIdle.Shared/Config/buffs.json` | 添加 3 个消耗品 Buff（strength, speed, steak） |

### 数据库

| 文件 | 变更内容 |
|------|----------|
| `BlazorIdle.Server/Data/GameDbContext.cs` | 添加 `EquippedConsumables` JSON 列转换配置 |

### UI 页面

| 文件 | 变更内容 |
|------|----------|
| `BlazorIdle/Pages/Home.razor` | 集成 `ConsumableSlotPanel` 组件 |
| `BlazorIdle/Components/BattleDemo.razor` | 传递 `EquippedConsumables` 到 `CharacterPanel` |
| `BlazorIdle/Components/BattleDemo.razor.cs` | 添加 `GameConfig` 参数、`playerEquippedConsumables` 属性、冷却时间获取 |
| `BlazorIdle/Components/CharacterPanel.razor` | 添加消耗品显示 UI、`EquippedConsumableData` 数据类 |

---

## 🔧 核心实现细节

### 1. 数据模型

```csharp
// ConsumableSlotData - 槽位数据
public class ConsumableSlotData
{
    public string? ItemId { get; set; }    // 物品ID
    public string? SkillId { get; set; }   // 技能ID
}

// ConsumableEquipmentConfig - 装备配置
public class ConsumableEquipmentConfig
{
    public Dictionary<string, ConsumableSlotData> PotionSlots { get; set; }  // 药水槽
    public Dictionary<string, ConsumableSlotData> FoodSlots { get; set; }    // 食物槽
}

// ConsumableConfig - 物品消耗品配置（在items.json中）
public class ConsumableConfig
{
    public string Category { get; set; }           // "potion" 或 "food"
    public string SkillId { get; set; }            // 触发的技能ID
    public SkillConditions? TriggerConditions { get; set; }  // 触发条件
    public double CooldownSec { get; set; }        // 冷却时间（秒）
}
```

### 2. 战斗触发流程

```
ProcessPeriodicSkillChecks (每秒)
    └── ProcessCharacterConsumableChecks
        ├── 处理药水槽位 (potion_1 → potion_2)
        │   └── ProcessConsumableSlot
        │       ├── 检查槽位是否装备
        │       ├── 检查触发条件
        │       ├── 检查冷却 (按物品ID)
        │       ├── 检查背包库存
        │       ├── 扣除库存
        │       ├── 执行技能
        │       ├── 启动冷却
        │       └── 触发事件
        └── 处理食物槽位 (food_1 → food_2)
            └── (同上逻辑)
```

### 3. ConsumableManager 核心方法

```csharp
public class ConsumableManager
{
    // 装备消耗品
    ConsumableOperationResult EquipConsumable(CharacterData, slotId, itemId, isInBattle)
    
    // 卸载消耗品
    ConsumableOperationResult UnequipConsumable(CharacterData, slotId, isInBattle)
    
    // 获取可装备的消耗品
    List<ItemDefinition> GetAvailableConsumables(CharacterData, category)
    
    // 战斗锁定检查
    static bool CanModifyConsumables(isInBattle)
    
    // 槽位验证
    static bool IsPotionSlot(slotId)
    static bool IsFoodSlot(slotId)
    static bool IsCategoryMatchingSlot(category, slotId)
}
```

### 4. 操作结果枚举

```csharp
public enum ConsumableOperationResult
{
    Success,              // 成功
    ItemNotFound,         // 物品不存在
    NotConsumable,        // 不是消耗品
    CategoryMismatch,     // 类别不匹配
    AlreadyEquipped,      // 已装备在其他槽位
    InsufficientInventory, // 库存不足
    InvalidSlotId,        // 无效槽位ID
    LockedInBattle,       // 战斗中锁定
    InvalidCharacterData  // 无效角色数据
}
```

---

## 📦 配置文件结构

### consumableSkills.json 示例

```json
[
  {
    "id": "consumable_health_potion",
    "name": "生命药水效果",
    "type": "consumable",
    "releaseType": "instant",
    "targetPolicy": "self",
    "instantHeal": 50
  },
  {
    "id": "consumable_strength_potion",
    "name": "力量药水效果",
    "type": "consumable",
    "onCastBuffs": [
      { "op": "Apply", "buffConfigId": "consumable_strength_buff" }
    ]
  }
]
```

### items.json consumableConfig 示例

```json
{
  "id": "health_potion",
  "name": "生命药水",
  "consumableConfig": {
    "category": "food",
    "skillId": "consumable_health_potion",
    "triggerConditions": { "hpBelowPct": 50.0 },
    "cooldownSec": 30.0
  }
}
```

---

## 🐛 已修复的 Bug

### 1. EF Core 映射错误
- **问题**：`ConsumableEquipmentConfig.FoodSlots` (Dictionary类型) 导致 EF Core 关系映射错误
- **修复**：在 `GameDbContext.OnModelCreating` 中添加 JSON 列转换配置

### 2. 消耗品战斗未触发
- **问题**：`BattleDemo` 创建战斗实例时未传入 `GameConfig` 参数
- **修复**：在 `MultiBattleInstance` 构造函数调用中添加 `GameConfig` 参数

### 3. 战斗面板不显示消耗品
- **问题**：`CharacterPanel` 缺少消耗品显示参数和UI
- **修复**：添加 `EquippedConsumables` 参数和 `EquippedConsumableData` 数据类

### 4. 副本战斗中消耗品无法触发
- **问题**：`DungeonManager` 创建 `MultiBattleInstance` 时未传入 `gameConfig` 参数
- **修复**：在 `DungeonManager.cs` 的 `MultiBattleInstance` 构造函数调用中添加 `_gameConfig` 参数

---

## 🎯 已确认的设计决策

| 决策项 | 选择 |
|--------|------|
| 装备模式 | 引用模式（槽位存ID，使用时从背包扣除） |
| 触发优先级 | 先药水后食物，按槽位顺序 |
| 库存耗尽处理 | 保留配置，显示特殊样式，等待补充 |
| 冷却追踪 | 按物品ID（同类物品共享冷却） |
| 槽位唯一性 | 禁止同一物品装备多个槽位 |
| 战斗中修改 | 禁止配置修改，允许商店购买补充 |
| 角色配置 | 每个角色独立 |
| UI交互 | 点击选择（非拖拽） |

---

## 🔮 后续优化建议

### 1. 功能增强
- [ ] 消耗品使用日志显示
- [ ] 消耗品效果浮动提示
- [ ] 手动使用消耗品按钮
- [ ] 消耗品使用统计

### 2. UI 优化
- [ ] 消耗品冷却动画效果
- [ ] 触发时闪烁效果
- [ ] 更丰富的物品图标

### 3. 配置扩展
- [ ] 更多触发条件类型
- [ ] 条件组合（AND/OR）
- [ ] 职业专属消耗品

### 4. 性能优化
- [ ] 消耗品检查频率优化
- [ ] 缓存优化

---

## 📚 相关文档

- [药水与食物系统扩展设计.md](./药水与食物系统扩展设计.md) - 详细设计文档
- [药水与食物系统_实施进度追踪.md](./药水与食物系统_实施进度追踪.md) - 实施进度追踪

---

**文档状态：** ✅ 已完成  
**维护者：** @copilot  
**最后更新：** 2025-11-26
