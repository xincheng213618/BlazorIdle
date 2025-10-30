# 战斗场景重构说明 (Battle Scenarios Refactor)

## 概述 (Overview)

本次重构将 `BattleDemo` 组件从直接选择单个怪物改为使用 `monsterGroups` 概念，统一了战斗系统的怪物管理方式。

This refactor updates the `BattleDemo` component from directly selecting a single monster to using the `monsterGroups` concept, unifying monster management across the battle system.

## 主要变更 (Key Changes)

### 1. 新增配置文件 (New Configuration File)

**文件路径**: `BlazorIdle.Server/Config/battleScenarios.json`

新增战斗场景配置文件，定义了9种不同的战斗场景：
- `single_slime` - 单个史莱姆
- `three_slimes` - 三个史莱姆
- `single_wolf` - 单个野狼
- `wolf_pack` - 狼群（2只）
- `single_ogre` - 单个食人魔
- `mixed_encounter` - 混合遭遇（史莱姆+野狼）
- `elite_ogre` - 精英食人魔（带倍率增强）
- `boss_ogre` - Boss食人魔（带特殊掉落）
- `horde` - 怪物群（多种类大量敌人）

### 2. 新增数据结构 (New Data Structures)

**文件**: `BlazorIdle.Shared/Game/Config/DungeonConfig.cs`

新增 `BattleScenarioDef` 类：
```csharp
public class BattleScenarioDef
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public List<MonsterGroup> MonsterGroups { get; set; }
}
```

### 3. 配置服务更新 (Configuration Service Updates)

更新了以下文件以支持 `battleScenarios` 加载：
- `BlazorIdle.Server/Services/IGameConfigProvider.cs`
- `BlazorIdle.Server/Services/GameConfigProvider.cs`
- `BlazorIdle.Server/Controllers/GameConfigController.cs`
- `BlazorIdle.Shared/DTOs/GameConfigResponse.cs`
- `BlazorIdle/Game/Config/GameConfigService.cs`

新增方法：
- `IGameConfigService.GetBattleScenario(string id)`
- `IReadOnlyList<BattleScenarioDef> BattleScenarios` 属性

### 4. BattleDemo 组件重构 (BattleDemo Component Refactor)

**文件**: `BlazorIdle/Components/BattleDemo.razor`

#### UI 变更
- 将"怪物选择"改为"战斗场景选择"
- 从显示单个怪物面板改为显示敌人队伍面板（支持多个敌人）
- 每个敌人显示独立的生命条和状态

#### 逻辑变更
- `BuildBattle()` 方法完全重写，现在支持从 `monsterGroups` 创建敌人队伍
- 支持以下 `MonsterGroup` 参数：
  - `count` - 怪物数量
  - `hpMultiplier` - 生命值倍率
  - `damageMultiplier` - 伤害倍率
  - `attackSpeedMultiplier` - 攻速倍率
  - `specialDrops` - 特殊掉落（覆盖默认掉落）
- 根据敌人数量自动切换特殊技能的 AOE 模式
- 敌人 ID 格式：`{monsterId}_{index}`，例如 `slime_1`, `slime_2`

#### 战斗配置优化
```csharp
PlayerTargetStrategy = TargetStrategy.LowestHp,  // 优先攻击低血量敌人
EnemyTargetStrategy = TargetStrategy.Random,     // 敌人随机选择目标
SpecialIsAoe = enemyTeam.TotalCount > 1,        // 多敌人时自动启用AOE
```

### 5. 代码清理 (Code Cleanup)

**文件**: `BlazorIdle/Game/DungeonManager.cs`

移除未使用的字段：
- `_waveStartDelayMs` 
- `_waveEndDelayMs`

这些字段已经被 `_nextActionAtMs` 替代。

## 参数可配置性 (Parameter Configurability)

所有战斗参数都通过配置文件控制，无硬编码：

### battleScenarios.json 中的可配置参数
```json
{
  "id": "场景ID",
  "name": "场景名称",
  "description": "场景描述",
  "monsterGroups": [
    {
      "monsterId": "怪物ID（引用monsters.json）",
      "count": 数量,
      "spawnDelaySec": 延迟出现时间（秒）,
      "hpMultiplier": 生命值倍率,
      "damageMultiplier": 伤害倍率,
      "attackSpeedMultiplier": 攻速倍率,
      "isElite": 是否精英,
      "isBoss": 是否Boss,
      "specialDrops": [
        {
          "itemId": "物品ID",
          "minQuantity": 最小数量,
          "maxQuantity": 最大数量,
          "dropChance": 掉落概率（0.0-1.0）
        }
      ]
    }
  ]
}
```

### 战斗配置参数（在代码中可按需调整）
- `PlayerTargetStrategy` - 玩家目标选择策略
- `EnemyTargetStrategy` - 敌人目标选择策略
- `SpecialIsAoe` - 特殊技能是否AOE
- `AoeDamageMultiplier` - AOE伤害倍率
- `AllowPlayerRevive` - 是否允许玩家复活
- `AllowEnemyRespawn` - 是否允许敌人刷新
- `ReviveWithFullHp` - 复活时是否满血

## 兼容性 (Compatibility)

### 向后兼容
- `MultiBattleTest.razor` - 无影响，继续使用手动创建的队伍
- `DungeonBattle.razor` - 无影响，已经使用 `monsterGroups`
- `dungeons.json` - 已经使用 `monsterGroups` 结构，无需修改

### 统一性
现在所有战斗场景（单次战斗、副本、测试）都使用相同的 `MonsterGroup` 概念进行怪物配置，提高了代码一致性和可维护性。

## 测试建议 (Testing Recommendations)

1. **功能测试**
   - 启动应用，选择角色
   - 在 BattleDemo 中测试所有9种战斗场景
   - 验证多敌人显示和战斗逻辑
   - 验证 AOE 技能在多敌人时自动启用

2. **配置测试**
   - 修改 `battleScenarios.json` 中的倍率参数
   - 添加新的战斗场景
   - 验证特殊掉落配置

3. **兼容性测试**
   - 测试 DungeonBattle 功能是否正常
   - 测试 MultiBattleTest 功能是否正常

## 未来扩展 (Future Extensions)

1. **更多场景类型**
   - Boss 战（多阶段）
   - 生存模式（无限刷新）
   - 时间限制战斗

2. **更多配置选项**
   - 怪物行为 AI 配置
   - 特殊机制（如环境效果）
   - 难度等级系统

3. **UI 增强**
   - 敌人状态图标（精英、Boss标记）
   - 战斗统计面板
   - 伤害统计图表

## 总结 (Summary)

本次重构成功实现了以下目标：
- ✅ 统一了战斗系统的怪物管理方式
- ✅ 实现了可配置的战斗场景系统
- ✅ 支持一打多的战斗效果
- ✅ 所有参数都可通过配置文件控制
- ✅ 保持了代码风格的一致性
- ✅ 添加了详细的中文注释
- ✅ 清理了废弃代码
- ✅ 构建无警告无错误
