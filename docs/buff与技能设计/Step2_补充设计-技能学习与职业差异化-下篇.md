# Step2 技能系统补充设计 - 下篇：完整技能库与实施更新

## 📋 文档说明

本文档为下篇（最终篇），提供完整的1-10级技能库设计。

**内容包括：**
1. 战士完整技能库（1-10级）
2. 法师完整技能库（1-10级）
3. 盗贼完整技能库（1-10级）
4. 游侠完整技能库（1-10级）
5. Step2实施阶段更新汇总

**版本：** v1.1  
**创建日期：** 2025-11-14  

---

## 五、完整技能库设计

### 5.1 战士技能库（1-10级）

#### 等级1 - 猛击（固定主动技能）
```json
{
  "id": "warrior_slam",
  "name": "猛击",
  "description": "用力打击敌人，造成伤害并产生怒气",
  "type": "active",
  "slotType": "active",
  "fixed": true,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 10.0,
  "allowedProfessions": ["warrior"],
  "unlock": { "minLevel": 1 },
  "damage": {
    "coefAtk": 1.2,
    "flat": 20,
    "isAoe": false
  },
  "resourceGains": [
    { "bucketId": "rage", "amount": 2 }
  ]
}
```

#### 等级3 - 致死打击
```json
{
  "id": "warrior_mortal_strike",
  "name": "致死打击",
  "description": "消耗怒气进行强力一击",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 3.0,
  "allowedProfessions": ["warrior"],
  "unlock": { "minLevel": 3 },
  "costs": [
    { "bucketId": "rage", "amount": 3 }
  ],
  "damage": {
    "coefAtk": 1.5,
    "flat": 30,
    "isAoe": false
  }
}
```

#### 等级5 - 撕裂
```json
{
  "id": "warrior_rend",
  "name": "撕裂",
  "description": "撕裂敌人，造成持续流血伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 10.0,
  "allowedProfessions": ["warrior"],
  "unlock": { "minLevel": 5 },
  "costs": [
    { "bucketId": "rage", "amount": 1 }
  ],
  "damage": {
    "coefAtk": 0.3,
    "flat": 5,
    "isAoe": false
  },
  "onHitBuffs": [
    {
      "op": "apply",
      "buffConfigId": "warrior_rend_dot",
      "targetOverride": "current_target"
    }
  ]
}
```

#### 等级7 - 雷霆一击
```json
{
  "id": "warrior_thunderclap",
  "name": "雷霆一击",
  "description": "对所有敌人造成伤害，非GCD技能",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "enemies_all",
  "cooldownSec": 10.0,
  "allowedProfessions": ["warrior"],
  "unlock": { "minLevel": 7 },
  "damage": {
    "coefAtk": 0.8,
    "flat": 10,
    "isAoe": true
  }
}
```

#### 等级8 - 战吼
```json
{
  "id": "warrior_battle_shout",
  "name": "战吼",
  "description": "提升自身攻击力",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "self",
  "cooldownSec": 30.0,
  "allowedProfessions": ["warrior"],
  "unlock": { "minLevel": 8 },
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "warrior_power_boost",
      "targetOverride": "self"
    }
  ]
}
```

#### 等级9 - 顺劈斩
```json
{
  "id": "warrior_cleave",
  "name": "顺劈斩",
  "description": "对前方敌人造成AoE伤害，消耗怒气",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "enemies_all",
  "cooldownSec": 6.0,
  "allowedProfessions": ["warrior"],
  "unlock": { "minLevel": 9 },
  "costs": [
    { "bucketId": "rage", "amount": 2 }
  ],
  "damage": {
    "coefAtk": 1.0,
    "flat": 15,
    "isAoe": true
  }
}
```

#### 等级10 - 破釜沉舟
```json
{
  "id": "warrior_desperate_strike",
  "name": "破釜沉舟",
  "description": "血量低于50%时可用，必定暴击的强力一击",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 15.0,
  "allowedProfessions": ["warrior"],
  "unlock": { "minLevel": 10 },
  "conditions": {
    "hpBelowPct": 50
  },
  "costs": [
    { "bucketId": "rage", "amount": 5 }
  ],
  "damage": {
    "coefAtk": 2.5,
    "flat": 50,
    "isAoe": false
  },
  "canCrit": true,
  "alwaysHits": true
}
```

#### 被动技能 - 附加打击触发器
```json
{
  "id": "warrior_proc_passive",
  "name": "战士被动触发",
  "description": "普攻命中有20%概率附加打击",
  "type": "passive",
  "slotType": "passive",
  "fixed": false,
  "releaseType": "instant",
  "allowedProfessions": ["warrior"],
  "unlock": { "minLevel": 1 },
  "triggers": [
    {
      "when": "OnAttackHit",
      "procChance": 0.20,
      "fireSkillId": "warrior_proc_strike",
      "priority": 15
    }
  ]
}
```

---

### 5.2 法师技能库（1-10级）

#### 等级1 - 寒冰箭
```json
{
  "id": "mage_frostbolt",
  "name": "寒冰箭",
  "description": "射出寒冰箭，减缓敌人速度",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 4.0,
  "allowedProfessions": ["mage"],
  "unlock": { "minLevel": 1 },
  "costs": [
    { "bucketId": "mana", "amount": 2 }
  ],
  "damage": {
    "coefAtk": 1.1,
    "flat": 15,
    "isAoe": false
  },
  "onHitBuffs": [
    {
      "op": "apply",
      "buffConfigId": "mage_frost_slow",
      "targetOverride": "current_target"
    }
  ]
}
```

#### 等级3 - 奥术飞弹
```json
{
  "id": "mage_arcane_missiles",
  "name": "奥术飞弹",
  "description": "快速施放奥术飞弹",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 3.0,
  "allowedProfessions": ["mage"],
  "unlock": { "minLevel": 3 },
  "costs": [
    { "bucketId": "mana", "amount": 3 }
  ],
  "damage": {
    "coefAtk": 1.3,
    "flat": 20,
    "isAoe": false
  }
}
```

#### 等级5 - 炎爆术
```json
{
  "id": "mage_pyroblast",
  "name": "炎爆术",
  "description": "施法2.5秒，造成大量火焰伤害并施加燃烧",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "cast",
  "castTimeSec": 2.5,
  "isGcd": true,
  "allowCoTriggerAfterCast": true,
  "targetPolicy": "current_target",
  "cooldownSec": 8.0,
  "allowedProfessions": ["mage"],
  "unlock": { "minLevel": 5 },
  "costs": [
    { "bucketId": "mana", "amount": 4 }
  ],
  "damage": {
    "coefAtk": 2.0,
    "flat": 50,
    "isAoe": false
  },
  "onHitBuffs": [
    {
      "op": "apply",
      "buffConfigId": "burning",
      "targetOverride": "current_target"
    }
  ]
}
```

#### 等级6 - 暴风雪
```json
{
  "id": "mage_blizzard",
  "name": "暴风雪",
  "description": "召唤暴风雪，对所有敌人造成持续伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "enemies_all",
  "cooldownSec": 12.0,
  "allowedProfessions": ["mage"],
  "unlock": { "minLevel": 6 },
  "costs": [
    { "bucketId": "mana", "amount": 5 }
  ],
  "damage": {
    "coefAtk": 0.7,
    "flat": 10,
    "isAoe": true
  },
  "onHitBuffs": [
    {
      "op": "apply",
      "buffConfigId": "mage_blizzard_dot",
      "targetOverride": "enemies_all"
    }
  ]
}
```

#### 等级7 - 奥术冲击
```json
{
  "id": "mage_arcane_blast",
  "name": "奥术冲击",
  "description": "快速奥术攻击，非GCD",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "current_target",
  "cooldownSec": 5.0,
  "allowedProfessions": ["mage"],
  "unlock": { "minLevel": 7 },
  "costs": [
    { "bucketId": "mana", "amount": 2 }
  ],
  "damage": {
    "coefAtk": 0.9,
    "flat": 12,
    "isAoe": false
  }
}
```

#### 等级8 - 法力护盾
```json
{
  "id": "mage_mana_shield",
  "name": "法力护盾",
  "description": "为自己施加护盾，吸收伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "self",
  "cooldownSec": 20.0,
  "allowedProfessions": ["mage"],
  "unlock": { "minLevel": 8 },
  "costs": [
    { "bucketId": "mana", "amount": 3 }
  ],
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "mage_shield",
      "targetOverride": "self"
    }
  ]
}
```

#### 等级9 - 火焰冲击
```json
{
  "id": "mage_fire_blast",
  "name": "火焰冲击",
  "description": "瞬发火焰伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 6.0,
  "allowedProfessions": ["mage"],
  "unlock": { "minLevel": 9 },
  "costs": [
    { "bucketId": "mana", "amount": 3 }
  ],
  "damage": {
    "coefAtk": 1.4,
    "flat": 25,
    "isAoe": false
  }
}
```

#### 等级10 - 时间扭曲
```json
{
  "id": "mage_time_warp",
  "name": "时间扭曲",
  "description": "大幅提升急速，持续10秒",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "self",
  "cooldownSec": 40.0,
  "allowedProfessions": ["mage"],
  "unlock": { "minLevel": 10 },
  "costs": [
    { "bucketId": "mana", "amount": 8 }
  ],
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "mage_haste_buff",
      "targetOverride": "self"
    }
  ]
}
```

---

### 5.3 盗贼技能库（1-10级）

#### 等级1 - 背刺
```json
{
  "id": "rogue_backstab",
  "name": "背刺",
  "description": "高暴击率的背后攻击",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 5.0,
  "allowedProfessions": ["rogue"],
  "unlock": { "minLevel": 1 },
  "costs": [
    { "bucketId": "energy", "amount": 2 }
  ],
  "damage": {
    "coefAtk": 1.3,
    "flat": 18,
    "isAoe": false
  },
  "canCrit": true
}
```

#### 等级3 - 剔骨
```json
{
  "id": "rogue_eviscerate",
  "name": "剔骨",
  "description": "消耗连击点数造成伤害，连击点越多伤害越高",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 0.0,
  "allowedProfessions": ["rogue"],
  "unlock": { "minLevel": 3 },
  "conditions": {
    "requireBuffId": "rogue_combo"
  },
  "costs": [
    { "bucketId": "energy", "amount": 3 }
  ],
  "damage": {
    "coefAtk": 1.8,
    "flat": 30,
    "isAoe": false
  },
  "onCastBuffs": [
    {
      "op": "remove",
      "buffIdToRemove": "rogue_combo"
    }
  ]
}
```

#### 等级5 - 毒刃
```json
{
  "id": "rogue_envenom",
  "name": "毒刃",
  "description": "涂毒的刀刃，造成持续毒素伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 8.0,
  "allowedProfessions": ["rogue"],
  "unlock": { "minLevel": 5 },
  "costs": [
    { "bucketId": "energy", "amount": 2 }
  ],
  "damage": {
    "coefAtk": 0.8,
    "flat": 10,
    "isAoe": false
  },
  "onHitBuffs": [
    {
      "op": "apply",
      "buffConfigId": "rogue_poison_dot",
      "targetOverride": "current_target"
    }
  ]
}
```

#### 等级6 - 刀扇
```json
{
  "id": "rogue_fan_of_knives",
  "name": "刀扇",
  "description": "向所有敌人投掷飞刀",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "enemies_all",
  "cooldownSec": 10.0,
  "allowedProfessions": ["rogue"],
  "unlock": { "minLevel": 6 },
  "costs": [
    { "bucketId": "energy", "amount": 3 }
  ],
  "damage": {
    "coefAtk": 0.9,
    "flat": 12,
    "isAoe": true
  }
}
```

#### 等级7 - 消失
```json
{
  "id": "rogue_vanish",
  "name": "消失",
  "description": "进入隐身，下次攻击必定暴击",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "self",
  "cooldownSec": 25.0,
  "allowedProfessions": ["rogue"],
  "unlock": { "minLevel": 7 },
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "rogue_stealth_crit",
      "targetOverride": "self"
    }
  ]
}
```

#### 等级8 - 致命药膏
```json
{
  "id": "rogue_deadly_poison",
  "name": "致命药膏",
  "description": "为武器涂毒，攻击附带额外毒素伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "self",
  "cooldownSec": 30.0,
  "allowedProfessions": ["rogue"],
  "unlock": { "minLevel": 8 },
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "rogue_deadly_poison_buff",
      "targetOverride": "self"
    }
  ]
}
```

#### 等级9 - 疾跑
```json
{
  "id": "rogue_sprint",
  "name": "疾跑",
  "description": "大幅提升攻击速度",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": false,
  "targetPolicy": "self",
  "cooldownSec": 20.0,
  "allowedProfessions": ["rogue"],
  "unlock": { "minLevel": 9 },
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "rogue_sprint_buff",
      "targetOverride": "self"
    }
  ]
}
```

#### 等级10 - 暗影之舞
```json
{
  "id": "rogue_shadow_dance",
  "name": "暗影之舞",
  "description": "进入暗影形态，大幅提升暴击率和暴击伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "self",
  "cooldownSec": 40.0,
  "allowedProfessions": ["rogue"],
  "unlock": { "minLevel": 10 },
  "costs": [
    { "bucketId": "energy", "amount": 4 }
  ],
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "rogue_shadow_dance_buff",
      "targetOverride": "self"
    }
  ]
}
```

---

### 5.4 游侠技能库（1-10级）

#### 等级1 - 奥术射击
```json
{
  "id": "ranger_arcane_shot",
  "name": "奥术射击",
  "description": "稳定的远程伤害技能",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 4.0,
  "allowedProfessions": ["ranger"],
  "unlock": { "minLevel": 1 },
  "costs": [
    { "bucketId": "energy", "amount": 2 }
  ],
  "damage": {
    "coefAtk": 1.2,
    "flat": 15,
    "isAoe": false
  }
}
```

#### 等级3 - 多重射击
```json
{
  "id": "ranger_multi_shot",
  "name": "多重射击",
  "description": "对多个敌人造成伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "enemies_all",
  "cooldownSec": 8.0,
  "allowedProfessions": ["ranger"],
  "unlock": { "minLevel": 3 },
  "costs": [
    { "bucketId": "energy", "amount": 3 }
  ],
  "damage": {
    "coefAtk": 0.9,
    "flat": 10,
    "isAoe": true
  }
}
```

#### 等级5 - 毒蛇陷阱
```json
{
  "id": "ranger_snake_trap",
  "name": "毒蛇陷阱",
  "description": "放置陷阱，对敌人造成持续毒素伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "enemies_all",
  "cooldownSec": 15.0,
  "allowedProfessions": ["ranger"],
  "unlock": { "minLevel": 5 },
  "onHitBuffs": [
    {
      "op": "apply",
      "buffConfigId": "ranger_poison_dot",
      "targetOverride": "enemies_all"
    }
  ]
}
```

#### 等级6 - 瞄准射击
```json
{
  "id": "ranger_aimed_shot",
  "name": "瞄准射击",
  "description": "施法1.5秒，造成巨大伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "cast",
  "castTimeSec": 1.5,
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 10.0,
  "allowedProfessions": ["ranger"],
  "unlock": { "minLevel": 6 },
  "costs": [
    { "bucketId": "energy", "amount": 3 }
  ],
  "damage": {
    "coefAtk": 2.2,
    "flat": 40,
    "isAoe": false
  }
}
```

#### 等级7 - 假死
```json
{
  "id": "ranger_feign_death",
  "name": "假死",
  "description": "短暂无敌，恢复生命值",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "self",
  "cooldownSec": 30.0,
  "allowedProfessions": ["ranger"],
  "unlock": { "minLevel": 7 },
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "ranger_heal_over_time",
      "targetOverride": "self"
    }
  ]
}
```

#### 等级8 - 猎豹守护
```json
{
  "id": "ranger_aspect_of_cheetah",
  "name": "猎豹守护",
  "description": "大幅提升攻击速度",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "self",
  "cooldownSec": 25.0,
  "allowedProfessions": ["ranger"],
  "unlock": { "minLevel": 8 },
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "ranger_haste_buff",
      "targetOverride": "self"
    }
  ]
}
```

#### 等级9 - 杀戮命令
```json
{
  "id": "ranger_kill_command",
  "name": "杀戮命令",
  "description": "强力攻击，对血量低于30%的敌人额外伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "current_target",
  "cooldownSec": 6.0,
  "allowedProfessions": ["ranger"],
  "unlock": { "minLevel": 9 },
  "costs": [
    { "bucketId": "energy", "amount": 3 }
  ],
  "damage": {
    "coefAtk": 1.6,
    "flat": 28,
    "isAoe": false
  }
}
```

#### 等级10 - 狂野怒火
```json
{
  "id": "ranger_bestial_wrath",
  "name": "狂野怒火",
  "description": "进入狂野状态，大幅提升所有伤害",
  "type": "active",
  "slotType": "active",
  "fixed": false,
  "releaseType": "instant",
  "isGcd": true,
  "targetPolicy": "self",
  "cooldownSec": 45.0,
  "allowedProfessions": ["ranger"],
  "unlock": { "minLevel": 10 },
  "onCastBuffs": [
    {
      "op": "apply",
      "buffConfigId": "ranger_berserk_buff",
      "targetOverride": "self"
    }
  ]
}
```

---

## 六、Step2实施阶段更新汇总

### 6.1 新增阶段：技能学习与装备系统

在原 **阶段2（技能槽位系统）** 之后插入：

**阶段 2.5：技能学习与装备系统（P0 - 必须）**

- 预计工作量：3-4 小时
- 新增测试：18 个
- 涉及文件：
  - `CharacterData.cs` - 添加持久化字段
  - `EquippedSkillsConfig.cs` - 新增类
  - `SkillLearningManager.cs` - 新增类
  - `SkillEquipmentManager.cs` - 新增类

### 6.2 新增阶段：职业固定技能差异化

在原 **阶段10（UI技能显示）** 之前插入：

**阶段 9.5：职业固定技能差异化（P0 - 必须）**

- 预计工作量：4-5 小时
- 新增测试：20 个
- 涉及内容：
  - 战士架势系统实现
  - 法师奥术充能系统实现
  - 游侠集中射击系统实现
  - 盗贼连击系统实现
  - 相关Buff配置添加到buffs.json

### 6.3 新增阶段：技能配置UI组件

在原 **阶段10（UI技能显示）** 中扩展：

**阶段 10（扩展）：技能学习与装备UI（P0 - 必须）**

新增组件：
- `SkillLearningPanel.razor` - 技能学习界面
- `SkillEquipmentPanel.razor` - 技能装备界面
- 相应的CSS样式文件

- 预计工作量：6-8 小时（总计，包含原技能图标和施法条）
- UI测试：手动测试验证

### 6.4 完整技能库添加

在原 **阶段1（技能配置基础设施）** 的 skills.json 中：

**扩展 skills.json 内容：**
- 战士技能：10个主动技能 + 2个被动/触发技能
- 法师技能：10个主动技能
- 盗贼技能：10个主动技能
- 游侠技能：10个主动技能
- 共计：40+ 个技能定义

**相应Buff配置（buffs.json）：**
- 战士相关Buff：5个
- 法师相关Buff：5个
- 盗贼相关Buff：5个
- 游侠相关Buff：5个
- 共计：20+ 个新Buff配置

### 6.5 更新后的总体进度

| 阶段 | 名称 | 工时 | 测试 |
|------|------|-----|------|
| 1 | 技能配置基础设施（扩展） | 4-5h | +15 |
| 2 | 技能槽位系统 | 2-3h | +12 |
| **2.5** | **技能学习与装备系统（新增）** | **3-4h** | **+18** |
| 3 | 目标选择系统 | 3-4h | +15 |
| 4 | 技能条件判定 | 3-4h | +18 |
| 5 | 资源消耗与冷却 | 3-4h | +15 |
| 6 | Window-GCD 机制 | 5-6h | +20 |
| 7 | 触发类技能系统 | 4-5h | +18 |
| 8 | 施法技能集成 | 4-5h | +15 |
| 9 | AutoCastEngine | 5-6h | +20 |
| **9.5** | **职业固定技能差异化（新增）** | **4-5h** | **+20** |
| 10 | UI技能显示（扩展） | 6-8h | - |
| 11 | 集成测试验收 | 6-8h | +30 |
| 12 | 文档与交付 | 4-5h | - |

**更新后总计：**
- 预计总工时：**57-72 小时**（原48-62h + 9-10h）
- 预计新增测试：**~216 个**（原178 + 38）
- 新增阶段：2个（2.5 和 9.5）
- 当前测试基线：363 个（Step1 完成后）

---

## 七、JSON文件位置说明

### 7.1 配置文件统一放置

**所有配置文件统一放置在：** `BlazorIdle.Shared/Config/`

这样做的好处：
1. ✅ 与现有 buffs.json 保持一致
2. ✅ 便于服务端战斗迁移时共享配置
3. ✅ 客户端和服务端都可以访问
4. ✅ 配置文件集中管理

**文件列表：**
```
BlazorIdle.Shared/Config/
├── buffs.json           # Buff配置（已存在）
├── skills.json          # 技能配置（新增）
└── professionAttributes.json  # 职业属性（已存在）
```

### 7.2 嵌入资源配置

在 `BlazorIdle.Shared.csproj` 中添加：

```xml
<ItemGroup>
  <EmbeddedResource Include="Config\buffs.json" />
  <EmbeddedResource Include="Config\skills.json" />
  <EmbeddedResource Include="Config\professionAttributes.json" />
</ItemGroup>
```

---

## 八、总结

本补充设计文档（上中下三篇）完整涵盖了用户要求的所有内容：

### 8.1 已添加内容

1. ✅ **技能学习与管理系统（持久化）**
   - CharacterData 扩展
   - EquippedSkillsConfig 数据模型
   - SkillLearningManager 逻辑
   - SkillEquipmentManager 逻辑

2. ✅ **技能配置UI组件**
   - SkillLearningPanel（学习界面）
   - SkillEquipmentPanel（装备界面）
   - 完整的CSS样式

3. ✅ **职业固定技能差异化**
   - 战士：架势系统（满3层必暴）
   - 法师：奥术充能系统（法力上限10，叠加伤害buff）
   - 游侠：集中射击系统（提升暴击和急速）
   - 盗贼：连击系统（积累连击点）

4. ✅ **完整技能库（1-10级）**
   - 战士：12个技能
   - 法师：10个技能
   - 盗贼：10个技能
   - 游侠：10个技能
   - 共计：42个技能

5. ✅ **JSON文件位置**
   - skills.json 放在 BlazorIdle.Shared/Config/
   - 与 buffs.json 保持一致

### 8.2 实施建议

1. **按阶段实施**：严格按照更新后的12+2个阶段执行
2. **测试优先**：每个阶段完成后运行全部测试
3. **增量提交**：每完成一个阶段就提交代码
4. **UI最后**：UI组件可以放在最后实施
5. **文档同步**：实施过程中同步更新文档

### 8.3 预期成果

完成 Step2 实施后，项目将具备：
- ✅ 完整的技能学习和装备系统
- ✅ 4个职业各有独特的战斗机制
- ✅ 42个可用技能（1-10级）
- ✅ 完善的UI界面（学习、装备、战斗显示）
- ✅ 约579个测试（363现有 + 216新增）

---

**最后更新：** 2025-11-14  
**维护者：** @copilot  
**状态：** 已完成

**三篇文档系列完成！**
