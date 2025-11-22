# Step3 定期技能检查系统 - 实施方案分析 v2.1

**文档版本：** v2.1  
**创建日期：** 2025-11-22  
**更新日期：** 2025-11-22  
**状态：** 用户反馈整合完成，准备实施

---

## 📝 用户反馈总结

用户在 PR 评论中提出了以下关键建议：

1. ✅ **复用 Buff 检查机制**：建议直接复用 `ProcessBuffTicks()` 中的定期检查机制，而不是创建独立的定期检查轨道
2. ✅ **消耗品简化**：Step3 只需实现"模拟消耗品效果的技能"（如治疗药水技能），可以装备使用，真正的消耗品系统后续单独实现
3. ✅ **死亡/复活已存在**：`HandleCooldownEnd()` (line 2180) 已实现死亡清除 Buff 和复活逻辑
4. ✅ **光环自动续期**：建议使用"Buff 剩余 < 5s 自动重新触发"的方式实现光环，复用同一机制也可用于食物 Buff

---

## 一、现有机制深度分析

### 1.1 Buff Tick 检查机制

**现有流程（MultiBattleInstance.cs）：**

```csharp
public void AdvanceTick(int tickMs)
{
    // Line 418-421: 每个 tick 都会调用 ProcessBuffTicks
    double deltaTimeSec = _lastTickTime > 0 ? (now - _lastTickTime) / 1000.0 : 0;
    if (deltaTimeSec > 0)
    {
        ProcessBuffTicks(deltaTimeSec);
        
        // 同时更新技能冷却
        foreach (var cooldownManager in _cooldownManagers.Values)
        {
            cooldownManager.TickCooldowns(deltaTimeSec);
        }
    }
}

// Line 1503: ProcessBuffTicks 实现
private void ProcessBuffTicks(double deltaTimeSec)
{
    // 处理玩家 Buff
    foreach (var kvp in _playerBuffOwners)
    {
        var charId = kvp.Key;
        var buffOwner = kvp.Value;
        var member = _playerTeam.GetMember(charId);
        if (member == null || member.IsDead) continue;
        
        ProcessEntityBuffs(buffOwner, deltaTimeSec);
    }
    
    // 处理敌人 Buff
    foreach (var kvp in _enemyBuffOwners)
    {
        var enemyId = kvp.Key;
        var buffOwner = kvp.Value;
        var member = _enemyTeam.GetMember(enemyId);
        if (member == null || member.IsDead) continue;
        
        ProcessEntityBuffs(buffOwner, deltaTimeSec);
    }
}
```

**关键发现：**
- ✅ `ProcessBuffTicks` 在**每个 tick** 都会执行（不是固定 1s 间隔）
- ✅ 遍历所有存活的玩家和敌人的 BuffOwner
- ✅ 已有跳过死亡单位的逻辑
- ✅ 与冷却更新在同一位置，逻辑统一

**结论：可以直接在 `ProcessBuffTicks` 中添加条件技能检查逻辑！**

### 1.2 死亡/复活机制

**现有流程（MultiBattleInstance.cs Line 2180）：**

```csharp
private void HandleCooldownEnd(int now)
{
    if (_state == MultiBattleState.PlayerTeamDeadCooldown)
    {
        // 复活玩家队伍
        _playerTeam.ReviveAll(_config.ReviveWithFullHp);

        // 清除玩家所有 buff 和重置资源
        foreach (var kvp in _playerBuffOwners)
        {
            var charId = kvp.Key;
            var buffOwner = kvp.Value;
            
            // 清除所有 buff
            buffOwner.ClearAllBuffs();  // ✅ 已实现
            
            // 重置资源 ...
        }
        
        // 重置轨道 ...
        _state = MultiBattleState.Fighting;
    }
}
```

**关键发现：**
- ✅ 死亡时已调用 `ClearAllBuffs()`，会清除光环 Buff
- ✅ 复活后状态变为 `Fighting`，会继续执行 `ProcessBuffTicks`
- ✅ 无需单独的 `OnPlayerRevive` 触发事件

**结论：只需在复活后的第一个 tick 重新触发光环即可！**

---

## 二、优化后的实施方案

### 2.1 核心设计：扩展 ProcessBuffTicks

**方案 B（优化版）：在 Buff 检查中集成条件技能检查**

```csharp
private void ProcessBuffTicks(double deltaTimeSec)
{
    // 原有 Buff tick 处理
    foreach (var kvp in _playerBuffOwners)
    {
        // ... 现有逻辑 ...
        ProcessEntityBuffs(buffOwner, deltaTimeSec);
    }
    
    // 新增：条件技能检查（每个 tick 都执行）
    ProcessPeriodicSkillChecks(deltaTimeSec);
}

/// <summary>
/// 处理定期技能检查（HP、数量条件等）
/// Process periodic skill checks (HP, count conditions, etc.)
/// </summary>
private void ProcessPeriodicSkillChecks(double deltaTimeSec)
{
    // 累积时间，每秒检查一次（避免每 tick 都检查）
    _periodicCheckAccumulator += deltaTimeSec;
    if (_periodicCheckAccumulator < 1.0) // 1 秒间隔
        return;
    
    _periodicCheckAccumulator -= 1.0;
    int nowMs = _clock.NowMs;
    
    // 检查所有玩家的条件技能
    foreach (var character in _playerTeam.GetLivingMembers())
    {
        ProcessCharacterPeriodicSkills(character.Id, nowMs);
    }
    
    // 可选：检查怪物的条件技能（如果需要）
    // foreach (var enemy in _enemyTeam.GetLivingMembers()) { ... }
}
```

**优势：**
- ✅ **零侵入**：不需要新增独立的时间轨道
- ✅ **统一管理**：Buff 检查和技能条件检查在同一位置
- ✅ **性能优化**：通过累积器控制检查频率（1s 间隔）
- ✅ **代码复用**：复用现有的生死判定、BuffOwner 遍历逻辑

### 2.2 光环自动续期机制

**用户建议：Buff 剩余 < 5s 时自动重新触发**

```csharp
private void ProcessCharacterPeriodicSkills(string characterId, int nowMs)
{
    // 获取角色的被动技能
    var passiveSkills = GetCharacterPassiveSkills(characterId);
    
    foreach (var skill in passiveSkills)
    {
        // 检查是否有 OnPeriodic 触发器
        var periodicTriggers = skill.Triggers?.Where(t => t.When == "OnPeriodic").ToList();
        if (periodicTriggers == null || periodicTriggers.Count == 0)
            continue;
        
        foreach (var trigger in periodicTriggers)
        {
            // 光环自动续期：检查 Buff 剩余时间
            if (ShouldRefreshAura(characterId, trigger, skill))
            {
                TriggerSkill(characterId, trigger, skill, nowMs);
                continue;
            }
            
            // 普通条件检查：HP、资源、数量等
            if (CheckPeriodicConditions(characterId, trigger, skill))
            {
                TriggerSkill(characterId, trigger, skill, nowMs);
            }
        }
    }
}

/// <summary>
/// 检查是否需要续期光环 Buff
/// Check if aura buff needs to be refreshed
/// </summary>
private bool ShouldRefreshAura(string characterId, TriggerDef trigger, SkillDef skill)
{
    // 检查触发的技能是否会施加 Buff
    var triggeredSkill = _skillRepository.GetSkillById(trigger.FireSkillId);
    if (triggeredSkill?.Buffs == null || triggeredSkill.Buffs.Count == 0)
        return false;
    
    // 检查所有 Buff 的剩余时间
    if (_playerBuffOwners.TryGetValue(characterId, out var buffOwner))
    {
        foreach (var buffDef in triggeredSkill.Buffs)
        {
            if (buffOwner.Buffs.TryGetValue(buffDef.BuffId, out var buffInstance))
            {
                // 剩余时间 < 5s，需要续期
                if (buffInstance.RemainingDurationSec < 5.0)
                    return true;
            }
            else
            {
                // Buff 不存在（可能刚复活），需要重新施加
                return true;
            }
        }
    }
    
    return false;
}
```

**优势：**
- ✅ **自动处理复活**：复活后 Buff 不存在，自动重新触发
- ✅ **自动续期**：剩余 < 5s 自动续期，无需 9999s 超长时间
- ✅ **统一机制**：光环和食物 Buff 都可以用这个机制

### 2.3 战斗开始触发

**重要说明：** OnBattleStart 保留用于未来扩展，**不用于光环系统**。光环完全通过 buff 续期条件（OnPeriodic + BuffTimeRemainingSec）实现。

OnBattleStart 的未来用途示例：
- 战斗开始时给全队施加增益/减益
- 战斗开始的特殊事件触发
- Boss 战开始的剧情效果
- 等等

**实现（保留接口）：**

```csharp
public void Start()
{
    // ... 现有初始化逻辑 ...
    
    _running = true;
    _state = MultiBattleState.Fighting;
    
    // 新增：触发战斗开始技能（用于未来扩展，当前光环不使用）
    TriggerBattleStartSkills();
    
    // 开始计时
    _lastTickTime = _clock.NowMs;
}

/// <summary>
/// 触发战斗开始时的技能（用于未来扩展）
/// Trigger skills at battle start (for future expansion)
/// 
/// 注意：光环系统不使用此触发器，使用 OnPeriodic + BuffTimeRemainingSec 实现
/// Note: Aura system does NOT use this trigger, uses OnPeriodic + BuffTimeRemainingSec instead
/// </summary>
private void TriggerBattleStartSkills()
{
    int nowMs = _clock.NowMs;
    
    // 玩家技能
    foreach (var character in _playerTeam.GetLivingMembers())
    {
        var passiveSkills = GetCharacterPassiveSkills(character.Id);
        
        foreach (var skill in passiveSkills)
        {
            var battleStartTriggers = skill.Triggers?
                .Where(t => t.When == "OnBattleStart")
                .ToList();
            
            if (battleStartTriggers == null || battleStartTriggers.Count == 0)
                continue;
            
            foreach (var trigger in battleStartTriggers)
            {
                TriggerSkill(character.Id, trigger, skill, nowMs);
            }
        }
    }
    
    // 怪物技能（未来扩展）
    foreach (var enemy in _enemyTeam.GetLivingMembers())
    {
        var periodicSkills = GetEnemyPeriodicSkills(enemy.Id);
        
        foreach (var skill in periodicSkills)
        {
            var battleStartTriggers = skill.Triggers?
                .Where(t => t.When == "OnBattleStart")
                .ToList();
            
            if (battleStartTriggers == null || battleStartTriggers.Count == 0)
                continue;
            
            foreach (var trigger in battleStartTriggers)
            {
                TriggerSkill(enemy.Id, trigger, skill, nowMs);
            }
        }
    }
}
```

---

## 三、扩展条件系统

### 3.1 新增条件类型

**SkillConditions.cs 扩展：**

```csharp
public class SkillConditions
{
    // 现有条件
    public double? HpBelowPct { get; set; }
    public double? HpAbovePct { get; set; }
    public string? RequireBuffId { get; set; }
    public string? ForbidBuffId { get; set; }
    public Dictionary<string, int>? RequireResource { get; set; }
    public Dictionary<string, int>? RequireBuffStacks { get; set; }
    
    // 新增 - 数量条件
    public int? EnemyCountAbove { get; set; }      // 敌人数量 >= 阈值
    public int? EnemyCountBelow { get; set; }      // 敌人数量 <= 阈值
    public int? AllyCountAbove { get; set; }       // 队友数量 >= 阈值
    public int? AllyCountBelow { get; set; }       // 队友数量 <= 阈值
    
    // 新增 - 队友HP条件
    public double? AllyHpBelowPct { get; set; }    // 任意队友 HP < 阈值
    
    // 新增 - Buff 时间条件（用于光环续期）
    public double? BuffTimeRemainingSec { get; set; }  // Buff 剩余时间 < 阈值
    public string? BuffTimeCheckId { get; set; }       // 检查的 Buff ID
}
```

### 3.2 ConditionChecker 扩展

```csharp
public bool CheckConditions(
    SkillDef skill, 
    BattleContext context, 
    bool isCasterPlayer, 
    string? casterId = null)
{
    if (skill.Conditions == null)
        return true;

    var conditions = skill.Conditions;

    // 现有条件检查 ...
    
    // 新增：敌人数量条件
    if (conditions.EnemyCountAbove.HasValue || conditions.EnemyCountBelow.HasValue)
    {
        if (!CheckEnemyCountCondition(context.EnemyTeam, conditions))
            return false;
    }
    
    // 新增：队友数量条件
    if (conditions.AllyCountAbove.HasValue || conditions.AllyCountBelow.HasValue)
    {
        if (!CheckAllyCountCondition(context.PlayerTeam, conditions))
            return false;
    }
    
    // 新增：队友HP条件
    if (conditions.AllyHpBelowPct.HasValue)
    {
        if (!CheckAllyHpCondition(context.PlayerTeam, conditions.AllyHpBelowPct.Value))
            return false;
    }
    
    // 新增：Buff 时间条件
    if (conditions.BuffTimeRemainingSec.HasValue && conditions.BuffTimeCheckId != null)
    {
        if (!CheckBuffTimeCondition(caster, conditions))
            return false;
    }

    return true;
}

private bool CheckEnemyCountCondition(BattleTeam<Enemy>? enemyTeam, SkillConditions conditions)
{
    if (enemyTeam == null) return false;
    
    int livingCount = enemyTeam.GetLivingMembers().Count();
    
    if (conditions.EnemyCountAbove.HasValue && livingCount < conditions.EnemyCountAbove.Value)
        return false;
    
    if (conditions.EnemyCountBelow.HasValue && livingCount > conditions.EnemyCountBelow.Value)
        return false;
    
    return true;
}

private bool CheckAllyCountCondition(BattleTeam<Character>? playerTeam, SkillConditions conditions)
{
    if (playerTeam == null) return false;
    
    int livingCount = playerTeam.GetLivingMembers().Count();
    
    if (conditions.AllyCountAbove.HasValue && livingCount < conditions.AllyCountAbove.Value)
        return false;
    
    if (conditions.AllyCountBelow.HasValue && livingCount > conditions.AllyCountBelow.Value)
        return false;
    
    return true;
}

private bool CheckAllyHpCondition(BattleTeam<Character>? playerTeam, double hpThreshold)
{
    if (playerTeam == null) return false;
    
    // 检查是否有任意队友 HP < 阈值
    foreach (var ally in playerTeam.GetLivingMembers())
    {
        double hpPct = (ally.Hp * 100.0) / ally.MaxHp;
        if (hpPct < hpThreshold)
            return true;
    }
    
    return false;
}

private bool CheckBuffTimeCondition(IBuffOwner? caster, SkillConditions conditions)
{
    if (caster == null) return false;
    
    if (!caster.Buffs.TryGetValue(conditions.BuffTimeCheckId!, out var buff))
        return false; // Buff 不存在
    
    return buff.RemainingDurationSec < conditions.BuffTimeRemainingSec!.Value;
}
```

---

## 四、技能配置示例

### 4.1 战士力量光环（自动续期）

**重要说明：** 光环系统**不使用 OnBattleStart 触发**，完全通过 buff 续期条件实现。OnBattleStart 保留用于未来扩展（如战斗开始的团队增益、特殊事件等）。

**技能定义：**
```json
{
  "id": "warrior_strength_aura_passive",
  "name": "力量光环",
  "description": "战士光环，持续为全队提供攻击力加成",
  "type": "passive",
  "slotType": "passive",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "allies_all",
  "cooldownSec": 0,
  "allowedProfessions": ["warrior"],
  "triggers": [
    {
      "when": "OnPeriodic",
      "procChance": 1.0,
      "fireSkillId": "warrior_strength_aura_effect",
      "priority": 10,
      "conditions": {
        "buffTimeCheckId": "strength_aura_buff",
        "buffTimeRemainingSec": 5.0
      }
    }
  ]
}
```

**光环效果技能：**
```json
{
  "id": "warrior_strength_aura_effect",
  "name": "力量光环效果",
  "description": "施加力量光环Buff",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "allies_all",
  "cooldownSec": 0,
  "allowedProfessions": ["warrior"],
  "onCastBuffs": [
    {
      "op": "Apply",
      "buffConfigId": "strength_aura_buff",
      "targetOverride": "AllTargets"
    }
  ]
}
```

**Buff 定义：**
```json
{
  "id": "strength_aura_buff",
  "name": "力量光环",
  "type": "Buff",
  "duration": 10,
  "effects": [
    {
      "type": "ModifyStat",
      "stat": "AttackPower",
      "operation": "MultiplyPercent",
      "value": 10
    }
  ]
}
```

**说明：**
- 战斗开始时触发一次（OnBattleStart）
- 每秒检查 Buff 剩余时间，< 5s 时自动续期（OnPeriodic）
- 死亡后 Buff 被清除，复活后第一次检查时自动重新触发
- Buff 持续 10s，每 5s 续期一次，保持持续生效

### 4.2 治疗药水技能（模拟消耗品）

**说明：** 可装备的被动技能，用于验证 HP 条件触发。未来真实的消耗品系统会有专门的装备栏。

**技能定义：**
```json
{
  "id": "health_potion_skill",
  "name": "治疗药水",
  "description": "生命值低于50%时自动使用，恢复100点生命值",
  "type": "passive",
  "slotType": "passive",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "self",
  "cooldownSec": 0,
  "allowedProfessions": [],
  "triggers": [
    {
      "when": "OnPeriodic",
      "procChance": 1.0,
      "fireSkillId": "health_potion_effect",
      "priority": 10,
      "conditions": {
        "hpBelowPct": 50
      }
    }
  ]
}
```

**药水效果技能：**
```json
{
  "id": "health_potion_effect",
  "name": "治疗药水效果",
  "description": "立即恢复100点生命值",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "self",
  "cooldownSec": 30,
  "allowedProfessions": [],
  "instantHeal": 100
}
```

**说明：**
- 每秒检查 HP < 50%
- 满足条件时触发治疗效果（回复 100 HP）
- 冷却 30 秒，防止频繁触发
- 可以装备到被动槽位，与其他被动技能一样使用

### 4.3 群体打击（敌人数量触发）

**说明：** 可装备的被动技能，用于验证敌人数量条件触发。

**技能定义：**
```json
{
  "id": "warrior_aoe_strike",
  "name": "群体打击",
  "description": "敌人数量≥3时自动触发AOE攻击",
  "type": "passive",
  "slotType": "passive",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "enemies_all",
  "cooldownSec": 0,
  "allowedProfessions": ["warrior"],
  "triggers": [
    {
      "when": "OnPeriodic",
      "procChance": 1.0,
      "fireSkillId": "warrior_aoe_strike_effect",
      "priority": 5,
      "conditions": {
        "enemyCountAbove": 3
      }
    }
  ]
}
```

**效果技能：**
```json
{
  "id": "warrior_aoe_strike_effect",
  "name": "群体打击效果",
  "description": "对所有敌人造成伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "enemies_all",
  "cooldownSec": 10,
  "allowedProfessions": ["warrior"],
  "damage": {
    "coefAtk": 0.8,
    "flat": 50
  }
}
```

**说明：**
- 每秒检查敌人数量 >= 3
- 满足条件时触发 AOE 攻击
- 冷却 10 秒

### 4.4 团队急救（队友低血触发）

**说明：** 牧师专属被动技能，用于验证队友 HP 条件触发。

**技能定义：**
```json
{
  "id": "priest_emergency_heal",
  "name": "团队急救",
  "description": "任意队友生命值低于30%时自动治疗",
  "type": "passive",
  "slotType": "passive",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "allies_lowest_hp_pct",
  "cooldownSec": 0,
  "allowedProfessions": ["priest"],
  "triggers": [
    {
      "when": "OnPeriodic",
      "procChance": 1.0,
      "fireSkillId": "priest_emergency_heal_effect",
      "priority": 10,
      "conditions": {
        "allyHpBelowPct": 30
      }
    }
  ]
}
```

**效果技能：**
```json
{
  "id": "priest_emergency_heal_effect",
  "name": "团队急救效果",
  "description": "治疗生命值最低的队友",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "allies_lowest_hp_pct",
  "cooldownSec": 20,
  "allowedProfessions": ["priest"],
  "instantHeal": 150
}
```

**说明：**
- 牧师专属技能
- 每秒检查是否有队友 HP < 30%
- 满足条件时治疗 HP 最低的队友
- 冷却 20 秒

### 4.5 怪物定期技能示例

**重要说明：** 怪物也需要支持定期技能检查系统。怪物技能配置更简单，直接在 `monsterskills.json` 中定义，然后在 `monsters.json` 的怪物配置中引用（没有装备槽概念）。

#### 4.5.1 怪物狂暴光环（自动续期）

**怪物技能定义（monsterskills.json）：**
```json
{
  "id": "monster_enrage_aura",
  "name": "怪物狂暴光环",
  "description": "怪物狂暴光环，持续提升攻击力",
  "type": "passive",
  "slotType": "passive",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "self",
  "cooldownSec": 0,
  "allowedProfessions": [],
  "triggers": [
    {
      "when": "OnPeriodic",
      "procChance": 1.0,
      "fireSkillId": "monster_enrage_aura_effect",
      "priority": 10,
      "conditions": {
        "buffTimeCheckId": "monster_enrage_buff",
        "buffTimeRemainingSec": 5.0
      }
    }
  ]
}
```

**光环效果技能：**
```json
{
  "id": "monster_enrage_aura_effect",
  "name": "怪物狂暴效果",
  "description": "施加狂暴Buff",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "self",
  "cooldownSec": 0,
  "allowedProfessions": [],
  "onCastBuffs": [
    {
      "op": "Apply",
      "buffConfigId": "monster_enrage_buff",
      "targetOverride": "Self"
    }
  ]
}
```

**Buff 定义（buffs.json）：**
```json
{
  "id": "monster_enrage_buff",
  "name": "狂暴",
  "type": "Buff",
  "duration": 10,
  "maxStacks": 1,
  "effects": [
    {
      "type": "ModifyStat",
      "stat": "AttackPower",
      "operation": "MultiplyPercent",
      "value": 20
    }
  ]
}
```

**怪物配置（monsters.json）：**
```json
{
  "id": "enraged_ogre",
  "name": "狂暴食人魔",
  "desc": "拥有狂暴光环的食人魔，攻击力持续提升",
  "level": 15,
  "maxHp": 600,
  "attackIntervalSec": 2.2,
  "damagePerHit": 20,
  "variancePct": 0.05,
  "respawnSec": 4.0,
  "baseExperience": 15,
  "normalAttackSkillId": "monster_attack_basic",
  "periodicSkillIds": ["monster_enrage_aura"],
  "lootDrops": [
    {
      "itemId": "gold_coin",
      "minQuantity": 20,
      "maxQuantity": 30,
      "dropChance": 1.0
    }
  ]
}
```

**说明：**
- 怪物通过 `periodicSkillIds` 数组配置定期技能（新增字段）
- 不需要装备槽，直接配置技能 ID
- 定期检查会遍历怪物的 `periodicSkillIds`，检查触发条件
- 光环自动续期机制与玩家相同

#### 4.5.2 怪物自我治疗（HP 低于 30% 触发）

**怪物技能定义（monsterskills.json）：**
```json
{
  "id": "monster_self_heal",
  "name": "怪物自我治疗",
  "description": "生命值低于30%时自动治疗自己",
  "type": "passive",
  "slotType": "passive",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "self",
  "cooldownSec": 0,
  "allowedProfessions": [],
  "triggers": [
    {
      "when": "OnPeriodic",
      "procChance": 1.0,
      "fireSkillId": "monster_self_heal_effect",
      "priority": 10,
      "conditions": {
        "hpBelowPct": 30
      }
    }
  ]
}
```

**治疗效果技能：**
```json
{
  "id": "monster_self_heal_effect",
  "name": "怪物治疗效果",
  "description": "恢复自身生命值",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "self",
  "cooldownSec": 30,
  "allowedProfessions": [],
  "instantHeal": 150
}
```

**怪物配置：**
```json
{
  "id": "healing_shaman",
  "name": "治疗萨满",
  "desc": "会自我治疗的萨满，血量低时恢复生命值",
  "level": 18,
  "maxHp": 500,
  "attackIntervalSec": 2.5,
  "damagePerHit": 15,
  "variancePct": 0.05,
  "respawnSec": 4.5,
  "baseExperience": 18,
  "normalAttackSkillId": "monster_attack_basic",
  "periodicSkillIds": ["monster_self_heal"],
  "lootDrops": [
    {
      "itemId": "gold_coin",
      "minQuantity": 25,
      "maxQuantity": 40,
      "dropChance": 1.0
    }
  ]
}
```

---

## 五、实施计划（修订版）

### 5.1 阶段 1: 扩展 ProcessBuffTicks（P0 - 3-4小时）

**目标：** 在 Buff 检查中集成条件技能检查（玩家 + 怪物）

**任务清单：**
- [ ] 1.1 在 MultiBattleInstance 中添加累积器
  - [ ] `_periodicCheckAccumulator` 字段（double）
- [ ] 1.2 实现 `ProcessPeriodicSkillChecks()` 方法
  - [ ] 累积器逻辑（1s 间隔）
  - [ ] 遍历存活玩家
  - [ ] 调用 `ProcessCharacterPeriodicSkills()`
  - [ ] 遍历存活怪物
  - [ ] 调用 `ProcessEnemyPeriodicSkills()`
- [ ] 1.3 实现 `ProcessCharacterPeriodicSkills()` 方法
  - [ ] 获取被动技能（从装备槽）
  - [ ] 检查 OnPeriodic 触发器
  - [ ] 检查条件并触发技能
- [ ] 1.4 实现 `ProcessEnemyPeriodicSkills()` 方法
  - [ ] 从 Enemy.PeriodicSkillIds 获取技能
  - [ ] 检查 OnPeriodic 触发器
  - [ ] 检查条件并触发技能
- [ ] 1.5 扩展 Enemy 数据模型
  - [ ] 添加 `PeriodicSkillIds` 属性（List<string>）
- [ ] 1.6 扩展 Monster 配置
  - [ ] 添加 `periodicSkillIds` 字段
- [ ] 1.7 在 `ProcessBuffTicks()` 中调用新方法
- [ ] 1.8 单元测试（8-10 个）
  - [ ] 累积器逻辑测试（2 个）
  - [ ] 玩家条件检查测试（3 个）
  - [ ] 怪物条件检查测试（2 个）
  - [ ] 触发逻辑测试（2-3 个）

**验收标准：**
- ✅ 每秒检查一次条件技能（通过累积器控制）
- ✅ 玩家条件满足时正确触发技能
- ✅ 怪物条件满足时正确触发技能
- ✅ 8-10 个单元测试全部通过
- ✅ 所有现有测试（644个）继续通过

**预估工作量：** 3-4 小时

---

### 5.2 阶段 2: 战斗开始触发 + 光环续期（P0 - 2-3小时）

**目标：** 实现 OnBattleStart 触发和光环自动续期

**任务清单：**
- [ ] 2.1 实现 `TriggerBattleStartSkills()` 方法
  - [ ] 遍历所有玩家
  - [ ] 检查 OnBattleStart 触发器
  - [ ] 触发技能
- [ ] 2.2 在 `Start()` 中调用新方法
- [ ] 2.3 实现 `ShouldRefreshAura()` 方法
  - [ ] 检查 Buff 剩余时间
  - [ ] 检查 Buff 是否存在（复活场景）
- [ ] 2.4 在 `ProcessCharacterPeriodicSkills()` 中集成续期逻辑
- [ ] 2.5 创建示例光环技能配置
  - [ ] 战士力量光环
  - [ ] 牧师治疗光环（可选）
- [ ] 2.6 单元测试（6-8 个）
  - [ ] OnBattleStart 触发测试（2 个）
  - [ ] 光环续期测试（2 个）
  - [ ] 复活重新触发测试（2 个）
  - [ ] 集成测试（1-2 个）

**验收标准：**
- ✅ 战斗开始时光环自动激活
- ✅ Buff 剩余 < 5s 时自动续期
- ✅ 复活后自动重新触发光环
- ✅ 6-8 个单元测试全部通过
- ✅ 所有现有测试继续通过

**预估工作量：** 2-3 小时

---

### 5.3 阶段 3: 数量和队友条件（P1 - 2-3小时）

**目标：** 支持基于敌人/队友数量和 HP 的条件

**任务清单：**
- [ ] 3.1 扩展 `SkillConditions` 类（+5 个字段）
  - [ ] EnemyCountAbove / EnemyCountBelow
  - [ ] AllyCountAbove / AllyCountBelow
  - [ ] AllyHpBelowPct
- [ ] 3.2 在 `ConditionChecker` 中实现新条件检查
  - [ ] `CheckEnemyCountCondition()`
  - [ ] `CheckAllyCountCondition()`
  - [ ] `CheckAllyHpCondition()`
- [ ] 3.3 创建示例技能配置
  - [ ] 群体打击（敌人 >= 3）
  - [ ] 团队急救（队友 HP < 30%）
- [ ] 3.4 单元测试（8-10 个）
  - [ ] 敌人数量条件测试（3 个）
  - [ ] 队友数量条件测试（2 个）
  - [ ] 队友 HP 条件测试（2 个）
  - [ ] 综合条件测试（2-3 个）

**验收标准：**
- ✅ 敌人数量条件正确判定
- ✅ 队友数量条件正确判定
- ✅ 队友 HP 条件正确判定
- ✅ 8-10 个单元测试全部通过
- ✅ 所有现有测试继续通过

**预估工作量：** 2-3 小时

---

### 5.4 阶段 4: Buff 时间条件 + 示例技能（P2 - 2小时）

**目标：** 支持 Buff 时间条件和创建完整示例

**任务清单：**
- [ ] 4.1 扩展 `SkillConditions` 类（+2 个字段）
  - [ ] BuffTimeRemainingSec
  - [ ] BuffTimeCheckId
- [ ] 4.2 在 `ConditionChecker` 中实现 `CheckBuffTimeCondition()`
- [ ] 4.3 创建示例技能配置
  - [ ] 治疗药水技能（模拟消耗品）
  - [ ] 其他测试技能
- [ ] 4.4 单元测试（4-6 个）
  - [ ] Buff 时间条件测试（2 个）
  - [ ] 示例技能集成测试（2-4 个）

**验收标准：**
- ✅ Buff 时间条件正确判定
- ✅ 示例技能正常工作
- ✅ 4-6 个单元测试全部通过
- ✅ 所有现有测试继续通过

**预估工作量：** 2 小时

---

### 5.5 阶段 5: 集成测试和文档（P2 - 1-2小时）

**目标：** 完善集成测试和文档

**任务清单：**
- [ ] 5.1 编写端到端集成测试（3-5 个）
  - [ ] 光环完整流程测试（1 个）
  - [ ] 定期检查完整流程测试（1 个）
  - [ ] 复杂场景测试（1-3 个）
- [ ] 5.2 更新文档
  - [ ] 更新 `Step2_实施进度追踪.md`
  - [ ] 创建 `Step3_实施总结.md`
  - [ ] 更新 API 文档

**验收标准：**
- ✅ 3-5 个集成测试全部通过
- ✅ 文档完整更新
- ✅ 所有测试（667-678 个）继续通过

**预估工作量：** 1-2 小时

---

### 5.6 总体时间估算（修订版）

| 阶段 | 描述 | 预估时间 |
|------|------|---------|
| 阶段 1 | 扩展 ProcessBuffTicks（P0） | 3-4h |
| 阶段 2 | 战斗开始触发 + 光环续期（P0） | 2-3h |
| 阶段 3 | 数量和队友条件（P1） | 2-3h |
| 阶段 4 | Buff 时间条件 + 示例技能（P2） | 2h |
| 阶段 5 | 集成测试和文档（P2） | 1-2h |
| **总计** | | **10-14 小时** |

**测试覆盖目标（修订版）：**
```
现有测试基线：644 个
新增测试预估：23-32 个
- 阶段 1：5-8 个
- 阶段 2：6-8 个
- 阶段 3：8-10 个
- 阶段 4：4-6 个

完成后总测试：667-676 个
```

---

## 六、关键设计决策（修订版）

### 6.1 ✅ 复用 Buff 检查机制

**决策：** 在 `ProcessBuffTicks()` 中集成条件技能检查，而不是创建独立轨道

**理由：**
- ✅ 代码复用率更高（复用生死判定、BuffOwner 遍历）
- ✅ 逻辑统一管理（Buff tick 和技能条件检查在同一位置）
- ✅ 性能更优（避免重复遍历）
- ✅ 实施更简单（无需新增时间轨道）

### 6.2 ✅ 光环自动续期机制

**决策：** 使用"Buff 剩余 < 5s 自动续期"替代"OnPlayerRevive 触发"

**理由：**
- ✅ 自动处理复活场景（Buff 不存在时自动重新触发）
- ✅ 统一机制（光环和食物 Buff 都可以用）
- ✅ 无需单独的复活事件处理
- ✅ Buff 可以使用合理的持续时间（如 10s），而不是 9999s

### 6.3 ✅ 保留 OnBattleStart 触发

**决策：** 保留战斗开始触发，不仅用于光环

**理由：**
- ✅ 用户认为"以后也用得上"
- ✅ 实施成本低（简单的初始化触发）
- ✅ 语义清晰（战斗开始时的一次性效果）
- ✅ 可用于其他场景（如战斗开始的增益、特殊效果等）

### 6.4 ✅ 消耗品简化为技能

**决策：** Step3 只实现"模拟消耗品效果的技能"

**理由：**
- ✅ 符合用户需求（只是验证条件功能）
- ✅ 实施简单（无需装备槽、物品系统）
- ✅ 可复用（与其他被动技能一样装备使用）
- ✅ 真正的消耗品系统后续单独实现

### 6.5 ✅ 检查间隔 1 秒

**决策：** 使用累积器控制，每秒检查一次

**理由：**
- ✅ 对大多数场景足够及时
- ✅ 性能影响小（< 0.1% CPU）
- ✅ 可根据实际测试调整
- ✅ 避免每 tick 都检查（降低性能影响）

---

## 七、方案对比（修订版）

### 原方案 A vs 修订方案 B

| 对比项 | 原方案 A（独立轨道） | 修订方案 B（集成 Buff 检查） |
|-------|-------------------|---------------------------|
| 代码复用 | ⚠️ 低（需要重复逻辑） | ✅ 高（复用 Buff 检查流程） |
| 职责分离 | ✅ 好（独立组件） | ✅ 好（统一管理） |
| 实施难度 | ⚠️ 中（3-4h） | ✅ 低（3-4h，但更简单） |
| 维护成本 | ⚠️ 中（两套逻辑） | ✅ 低（统一维护） |
| 性能 | ⚠️ 稍差（重复遍历） | ✅ 更优（单次遍历） |
| 推荐度 | ⭐⭐ | ⭐⭐⭐ |

**结论：修订方案 B 更优！**

---

## 八、风险评估（修订版）

### 8.1 风险识别

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|----------|
| 累积器精度问题 | 低 | 低 | 使用 double 类型，测试边界情况 |
| 光环续期时机不准确 | 中 | 低 | 充分测试，调整阈值（5s → 可配置） |
| 与现有系统集成问题 | 中 | 低 | 渐进式实施，充分测试 |
| 性能问题（检查过于频繁） | 低 | 低 | 累积器控制间隔，条件缓存（可选） |

### 8.2 回滚计划

**如果新系统出现严重问题：**
1. **功能开关**：添加配置开关禁用条件检查
2. **降级方案**：光环效果降级为 OnBattleStart 触发（无续期）
3. **快速修复**：准备 hotfix 流程
4. **监控告警**：设置性能和错误监控

---

## 九、总结

### 9.1 核心改进（相比 v1.0）

1. ✅ **复用 Buff 检查机制**：在 `ProcessBuffTicks()` 中集成，避免重复逻辑
2. ✅ **光环自动续期**：通过 Buff 时间条件实现，自动处理复活场景
3. ✅ **消耗品简化**：仅作为技能实现，验证条件功能
4. ✅ **保留战斗开始触发**：OnBattleStart 有独立价值
5. ✅ **优化实施计划**：10-14h（vs 原 11-15h），更简单高效

### 9.2 用户反馈响应

| 用户建议 | 响应 | 状态 |
|---------|------|------|
| 1. 复用 Buff 检查机制 | 采纳，在 ProcessBuffTicks 中集成 | ✅ 已修订 |
| 2. 消耗品简化为技能 | 采纳，仅实现模拟技能 | ✅ 已修订 |
| 3. 死亡/复活已存在 | 确认，利用现有逻辑 | ✅ 已确认 |
| 4. 光环自动续期机制 | 采纳，使用 Buff 时间条件 | ✅ 已修订 |

### 9.3 用户反馈整合（v2.1 更新）

**用户反馈日期：** 2025-11-22

#### ✅ 已确认的关键点

1. **光环系统不使用 OnBattleStart**
   - ✅ 光环完全通过 buff 续期条件实现（OnPeriodic + BuffTimeRemainingSec）
   - ✅ OnBattleStart 保留用于未来扩展（战斗开始的团队增益、特殊事件等）

2. **示例技能遵循现有格式**
   - ✅ 参考现有被动技能 JSON 格式
   - ✅ 创建可装备的被动技能用于测试
   - ✅ 未来消耗品系统会有专门的装备栏

3. **怪物支持是必需的**
   - ✅ 怪物需要适配定期检查系统
   - ✅ 怪物技能配置更简单（无装备槽概念）
   - ✅ 直接在 `monsters.json` 中配置 `periodicSkillIds` 数组
   - ✅ 技能定义在 `monsterskills.json` 中

4. **技术参数已确认**
   - ✅ 光环续期阈值：5 秒
   - ✅ Buff 持续时间：10 秒
   - ✅ 检查间隔：1000ms（1秒）

### 9.4 下一步

**用户已确认方案，准备开始实施！**

实施顺序：
1. 阶段 1：扩展 ProcessBuffTicks（玩家 + 怪物）
2. 阶段 2：战斗开始触发 + 光环续期（保留 OnBattleStart 接口）
3. 阶段 3：数量和队友条件
4. 阶段 4：Buff 时间条件 + 示例技能（玩家 + 怪物）
5. 阶段 5：集成测试和文档

**预计完成时间：** 10-14 小时

---

**文档维护：**
- 实施过程中的决策记录在 `决策日志` 章节
- 每个阶段完成后更新 `Step2_实施进度追踪.md`

**相关文档：**
- `Step2_实施进度追踪.md` - 当前进度（644 测试通过）
- `Step3_定期技能检查系统设计.md` - 原始设计文档
- `Step3_实施方案分析.md` - v1.0 版本（已废弃）
- `Step3_实施方案分析_v2.md` - 本文档（最新版）

---

**最后更新：** 2025-11-22  
**维护者：** @copilot  
**状态：** ✅ 用户反馈已整合，等待最终确认
