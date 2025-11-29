# 初期装备技能词条（Affix）设计方案 v1.4（仅百分比化；两种攻击力加成并行；Lv1收益最大）

更新要点（相对 v1.3）
- 明确“特殊攻击（SpecialAttack%）”与“普通攻击（Attack%）”无技能概念绑定。两者仅作为两条独立的“伤害加成”词条，属于同一主体乘区中的两个独立系数：Attack% 与 SpecialAttack%。普通攻击、技能伤害都走统一伤害管线，SpecialAttack%并非“技能专用”，只是与Attack%区分的第二类攻击加成来源，用于构筑差异化。
- 继续保持“仅百分比化”（除固定追击外）、“Lv1收益最大、后续递减”的成长曲线。
- 全局上限（Caps）不变：AttackPct 100、SpecialAttackPct 80、CritChancePct 80、CritDamageBonus 50（基础暴击倍率1.2）、HPPercent 100、FortifyMaxPct 20、BackwaterMaxPct 20、ChasePct 30、KenChasePct 20、DamageReductionPct 90。

暴击基础倍率（combat 配置回顾）
```json
{
  "combat": {
    "crit": {
      "baseMultiplier": 1.2,
      "critDamageBonusCap": 50.0
    }
  }
}
```

词条列表（两种攻击力加成并行；Lv1最大、递减）
- 说明：Attack% 与 SpecialAttack% 在主体加成层以乘法分别叠加：(1 + Attack%) × (1 + SpecialAttack%)。两者的数值与上限分开，便于设计不同路线和互斥关系。

| 词条ID | 名称 | 类型 | 等级数值 (Lv1→Lv4，Lv1最大) | 10件Lv4总和 | 上限比对 | 约束建议 | 说明 |
|---|---|---|---|---|---|---|---|
| attack | 攻击 | 输出 | 4% / 6% / 7% / 8% | 80% | <100% | 与 assault 可并存 | 普通攻击力加成（第一类攻击系数） |
| special_attack | 特攻 | 输出 | 3% / 4.5% / 5.5% / 6% | 60% | <80% | 与 assault 可并存；与“曾提到的 mastery（若未来出现）”互斥 | 特殊攻击力加成（第二类攻击系数），与技能无特定绑定 |
| hp | 生命 | 生存 | 6% / 8% / 9% / 10% | 100% | =Cap | — | 最大生命百分比 |
| vital_force | 盛能（攻+生） | 混合 | 攻击%:2/3/3.5/4 + 生命%:4/6/7/8 | Atk 40%、HP 80% | 安全 | 与 assault 互斥 | 双百分比混合路线 |
| crit | 会心 | 暴击率 | 4% / 6% / 7% / 8% | 80% | =Cap | — | 暴击率来源 |
| crit_damage_bonus | 终结 | 暴击伤害加成 | 3% / 4% / 4.5% / 5% | 50% | =Cap | 与 assault 互斥 | 叠加在基础1.2倍上 |
| assault | 神击 | 综合输出 | 攻击%:2/3/3.5/4 + 暴击率%:2/3/3.5/4 | Atk 40%、Crit 40% | 安全 | mutuallyExclusive: [vital_force, crit_damage_bonus] | 综合路线，避免爆发或生存直叠 |
| fortify_boost | 盛体增幅 | 态势 | FortifyMaxPct: 10 / 14 / 18 / 20 | 单件20% | =Cap | uniqueGroup: fortify_unique=1 | 高血姿态，最多1件 |
| backwater_boost | 背水意志 | 态势 | BackwaterMaxPct: 10 / 14 / 18 / 20 | 单件20% | =Cap | uniqueGroup: backwater_unique=1 | 低血姿态，最多1件 |
| chase_pct | 追击 | 追击 | 1.8% / 2.4% / 2.8% / 3.2% | 8件=25.6% | <30% | equipLimit: 8；与 chase_flat 同件互斥 | 尾部乘加，限制件数 |
| chase_flat | 固定追击 | 追击(固定) | 30 / 45 / 55 / 60 | 600（10件） | 需监控占比 | perItem: 与 chase_pct 互斥 | 唯一固定值词条；Lv1给大头 |
| ken_chase | 克制追击 | 追击(克制) | 1.2% / 1.8% / 2.4% / 3.0% | 6件=18% | <20% | equipLimit: 6 | 仅元素克制成立时生效 |
| mitigation | 坚韧 | 生存 | DamageReduction%: 2 / 3 / 3.6 / 4 | 10件=40% | <90% | — | 稳定减伤，Lv1明显 |
| element_resonance | 元素共鸣 | 元素混合 | 元素匹配：攻%: 2 / 3 / 3.5 / 4 + 克制追击%: 1 / 1.5 / 1.75 / 2 | Atk 40%、Ken 20% | 受总Cap | 与 ken_chase 累加受 KenChasePctCap | 鼓励统一元素构筑 |

互斥 / 限制规则（建议）
```json
{
  "affixRules": {
    "mutuallyExclusive": {
      "assault": ["vital_force", "crit_damage_bonus"]
    },
    "uniqueGroup": {
      "fortify_unique": 1,
      "backwater_unique": 1
    },
    "equipLimit": {
      "chase_pct": 8,
      "ken_chase": 6
    },
    "perItemConstraint": {
      "maxChaseTypePerItem": 1
    }
  }
}
```

示例 JSON 片段（两种攻击加成并行；Lv1最大、递减）
```json
{
  "affixes": [
    {
      "id": "attack",
      "name": "攻击",
      "levels": [
        { "AttackPercent": 4.0 },
        { "AttackPercent": 6.0 },
        { "AttackPercent": 7.0 },
        { "AttackPercent": 8.0 }
      ],
      "tags": ["offense"]
    },
    {
      "id": "special_attack",
      "name": "特攻",
      "levels": [
        { "SpecialAttackPercent": 3.0 },
        { "SpecialAttackPercent": 4.5 },
        { "SpecialAttackPercent": 5.5 },
        { "SpecialAttackPercent": 6.0 }
      ],
      "tags": ["offense"]
    }
  ]
}
```

与伤害管线的对应关系（关键说明）
- 主体加成层：AfterMain = AfterVariance × (1 + Attack%/100) × (1 + SpecialAttack%/100) × (1 + Stance%/100)。Attack% 与 SpecialAttack% 为两条独立加成来源；不绑定技能类型。
- 暴击层：基础暴击倍率 1.2；终结词条提供“额外暴击伤害%”在上限 50% 内提升。
- 态势层：盛体/背水仅修改上限（FortifyMaxPct/BackwaterMaxPct），互斥区间，不会同时触发。
- 追击层：追击%、克制追击%、固定追击在元素层之后做尾部加法。
- 减伤层：护甲曲线与最终减伤统一控制。

数值安全性快速校验
- attack Lv4×10 = 80%（<100%）
- special_attack Lv4×10 = 60%（<80%）
- hp Lv4×10 = 100%（=Cap）
- crit Lv4×10 = 80%（=Cap）
- crit_damage_bonus Lv4×10 = 50%（=Cap）→ 暴击倍率上限约 1.2 × 1.5 = 1.8
- chase_pct Lv4×8 = 25.6%（<30%）
- ken_chase Lv4×6 = 18%（<20%）
- fortify/backwater 单件Lv4 = 20%（=Cap）
- mitigation Lv4×10 = 40%（<<90%）

等级曲线与强化说明
- 所有百分比词条遵循“Lv1收益最大、后续递减”，强化（+3）同步将装备上所有词条从基础 Lv1 提升至 Lv4。
- 接近上限时在 UI 提示“收益递减或已达上限”。

总结
v1.4 明确了“特攻%”与“攻%”为两条并行的伤害加成路线，无需与技能概念绑定；整体继续保持仅百分比化（固定追击除外）、Lv1最大收益、上限安全与互斥/限量控制。这样玩家在初期就能通过“攻% vs 特攻%”做出有辨识度的构筑选择，同时不破坏伤害管线的简洁性与可控性。  
如需，我可输出完整 affixes.json 初版和自动校验脚本（检查 10 件满配 +3 强化后的属性是否超出 Caps）。  