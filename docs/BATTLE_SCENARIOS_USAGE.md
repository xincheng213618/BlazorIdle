# 战斗场景使用指南 (Battle Scenarios Usage Guide)

## 如何添加新的战斗场景 (How to Add New Battle Scenarios)

### 步骤 1: 编辑配置文件

打开 `BlazorIdle.Server/Config/battleScenarios.json`，添加新的场景定义：

```json
{
  "id": "my_custom_battle",
  "name": "我的自定义战斗",
  "description": "这是一个自定义的战斗场景示例",
  "monsterGroups": [
    {
      "monsterId": "slime",
      "count": 5,
      "spawnDelaySec": 0,
      "hpMultiplier": 1.2,
      "damageMultiplier": 1.1,
      "attackSpeedMultiplier": 1.0,
      "isElite": false,
      "isBoss": false
    }
  ]
}
```

### 步骤 2: 参数说明

#### 场景基本信息
- `id` (必需): 场景唯一标识符，使用小写字母和下划线
- `name` (必需): 场景显示名称，会在下拉菜单中显示
- `description` (可选): 场景描述，提示玩家这个场景的特点

#### MonsterGroup 参数详解

##### 基础参数
- `monsterId` (必需): 引用 `monsters.json` 中的怪物ID
- `count` (必需): 该类型怪物的数量（1-10推荐）
- `spawnDelaySec` (必需): 延迟出现时间（秒），0表示立即出现

##### 属性倍率（用于调整难度）
- `hpMultiplier` (必需): 生命值倍率，默认1.0
  - 0.5 = 50%生命
  - 1.0 = 100%生命（正常）
  - 2.0 = 200%生命（加强）
- `damageMultiplier` (必需): 伤害倍率，默认1.0
- `attackSpeedMultiplier` (必需): 攻速倍率，默认1.0
  - 注意：数值越大攻击越快（与攻击间隔成反比）

##### 特殊标记
- `isElite` (必需): 是否为精英怪物，用于标识和潜在的特殊机制
- `isBoss` (必需): 是否为Boss，用于标识和潜在的特殊机制

##### 特殊掉落（可选）
- `specialDrops` (可选): 覆盖怪物默认掉落配置
```json
"specialDrops": [
  {
    "itemId": "gold_coin",
    "minQuantity": 100,
    "maxQuantity": 200,
    "dropChance": 1.0
  }
]
```

### 步骤 3: 重启服务器

修改配置文件后，需要重启服务器以加载新配置：
```bash
# 如果正在运行，先停止
# 然后重新启动
dotnet run --project BlazorIdle.Server
```

### 步骤 4: 测试

1. 启动游戏客户端
2. 选择或创建角色
3. 在 BattleDemo 的下拉菜单中选择新场景
4. 点击"开始战斗"进行测试

## 战斗场景设计建议 (Battle Scenario Design Tips)

### 1. 难度梯度设计

#### 简单场景
```json
{
  "id": "easy_training",
  "name": "简单训练",
  "monsterGroups": [
    {
      "monsterId": "slime",
      "count": 1,
      "hpMultiplier": 0.7,
      "damageMultiplier": 0.8,
      "attackSpeedMultiplier": 0.9
    }
  ]
}
```

#### 中等场景
```json
{
  "id": "medium_challenge",
  "name": "中等挑战",
  "monsterGroups": [
    {
      "monsterId": "wolf",
      "count": 2,
      "hpMultiplier": 1.0,
      "damageMultiplier": 1.0,
      "attackSpeedMultiplier": 1.0
    }
  ]
}
```

#### 困难场景
```json
{
  "id": "hard_battle",
  "name": "困难战斗",
  "monsterGroups": [
    {
      "monsterId": "ogre",
      "count": 2,
      "hpMultiplier": 1.5,
      "damageMultiplier": 1.3,
      "attackSpeedMultiplier": 1.2,
      "isElite": true
    }
  ]
}
```

### 2. 多样性设计

#### 混合类型（测试多目标能力）
```json
{
  "monsterGroups": [
    {
      "monsterId": "slime",
      "count": 3,
      "spawnDelaySec": 0,
      "hpMultiplier": 0.8,
      "damageMultiplier": 0.8,
      "attackSpeedMultiplier": 1.0
    },
    {
      "monsterId": "wolf",
      "count": 1,
      "spawnDelaySec": 2.0,
      "hpMultiplier": 1.2,
      "damageMultiplier": 1.2,
      "attackSpeedMultiplier": 1.0
    }
  ]
}
```

#### 波次出现（模拟支援）
```json
{
  "monsterGroups": [
    {
      "monsterId": "wolf",
      "count": 2,
      "spawnDelaySec": 0
    },
    {
      "monsterId": "wolf",
      "count": 1,
      "spawnDelaySec": 5.0,
      "hpMultiplier": 1.3
    },
    {
      "monsterId": "ogre",
      "count": 1,
      "spawnDelaySec": 10.0,
      "isBoss": true,
      "hpMultiplier": 2.0
    }
  ]
}
```

### 3. Boss战设计

```json
{
  "id": "epic_boss",
  "name": "史诗Boss战",
  "description": "挑战强大的Boss和它的护卫",
  "monsterGroups": [
    {
      "monsterId": "slime",
      "count": 2,
      "spawnDelaySec": 0,
      "hpMultiplier": 1.0,
      "damageMultiplier": 1.0,
      "attackSpeedMultiplier": 1.0,
      "isElite": true
    },
    {
      "monsterId": "ogre",
      "count": 1,
      "spawnDelaySec": 3.0,
      "hpMultiplier": 3.0,
      "damageMultiplier": 1.5,
      "attackSpeedMultiplier": 0.8,
      "isBoss": true,
      "specialDrops": [
        {
          "itemId": "gold_coin",
          "minQuantity": 500,
          "maxQuantity": 1000,
          "dropChance": 1.0
        }
      ]
    }
  ]
}
```

## 常见问题 (FAQ)

### Q: 如何平衡多个弱敌人和少数强敌人？

A: 使用倍率参数。一般规则：
- 数量多时降低倍率（0.7-0.9）
- 数量少时提高倍率（1.2-2.0）
- Boss单位使用高倍率（2.0-3.0）+ 特殊掉落

### Q: spawnDelaySec 如何工作？

A: 
- 0 表示战斗开始时立即出现
- 大于0表示延迟该秒数后出现
- 可用于创建波次效果或支援机制

### Q: AOE技能何时启用？

A: 
- 系统会自动检测敌人数量
- 当 `enemyTeam.TotalCount > 1` 时自动启用AOE
- AOE伤害倍率默认为0.7（在MultiBattleConfig中配置）

### Q: 如何测试倍率效果？

A: 
1. 创建两个相似场景，仅倍率不同
2. 用同一角色分别测试
3. 观察战斗时长和伤害日志
4. 根据需要调整倍率

### Q: 可以引用不存在的怪物ID吗？

A: 
不可以。`monsterId` 必须引用 `monsters.json` 中已定义的怪物。
如果引用不存在的ID，该怪物组将被跳过。

### Q: 场景数量有限制吗？

A: 
没有硬性限制，但建议：
- 保持场景数量在50个以内以便管理
- 使用清晰的命名和描述
- 按难度或类型分组组织

## 最佳实践 (Best Practices)

1. **命名规范**
   - 使用描述性的ID：`boss_ogre_hard` 而不是 `battle_1`
   - 名称简洁明了
   - 描述包含关键信息（难度、特点）

2. **难度平衡**
   - 从简单场景开始设计
   - 逐步增加复杂度
   - 测试不同角色和装备配置

3. **奖励设计**
   - 困难场景给予更好的奖励
   - Boss战使用specialDrops
   - 平衡游戏经济

4. **测试流程**
   - 使用不同等级的角色测试
   - 记录战斗时长和难度感受
   - 收集玩家反馈进行调整

5. **维护性**
   - 添加注释（虽然JSON不支持，但可以在文档中说明）
   - 保持配置文件整洁
   - 定期审查和优化场景设计
