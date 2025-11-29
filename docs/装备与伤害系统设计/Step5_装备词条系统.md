# Step5 装备词条系统（GBF / 怪猎风格“技能化词条”）详细设计

版本：v2.1  
日期：2025-11-29  
作者：@copilot  
本次更新：与 v2.0 相比，按后续讨论的 v1.4 初期词条设计做规范化调整（双攻击系 Attack% / SpecialAttack%，新增单独 HP% 词条，移除“技能输出专属”概念、明确暴击基础倍率、统一“Lv1收益最大”成长模型、精简互斥/限量规则、保留唯一固定追击）。

--------------------------------
更新摘要（v2.1 相对 v2.0 必要改动）
--------------------------------
需更新原因：
1. 原 v2.0 文档中还保留了“mastery / special 技能专精”概念，现在所有伤害都走技能管线，不再需要“技能专精”独立乘区；改为纯第二攻击系（SpecialAttack%）。
2. 新增单独“hp（生命%）”词条；原“神威/valor”平加形式已被替换为百分比混合词条“vital_force”（Attack%+HP%）。
3. 明确暴击基础倍率为 1.2；“终结(crit_damage_bonus)”仅提供额外暴击伤害加成%，总额受 CritDamageBonusCap（50%）限制 → 最大暴击乘区 ~1.8。
4. 成长曲线统一为：Lv1 最大、后续递减（之前有部分线性或均匀增长的描述需修正）。
5. 强化 +n 只提升词条等级（Lv1→Lv4），不再提及词条随机等级与其他路径。
6. 互斥与限制规则更新： assault 与 vital_force / crit_damage_bonus 互斥；fortify_boost / backwater_boost 各唯一；chase_pct / ken_chase 有 equipLimit；同件限制仅一个追击类型（百分比或固定）。
7. 删除“mastery”与“skillDamage%”在词条层的说明；伤害管线主体乘区现在是 Attack% × SpecialAttack% × Stance%。
8. 文档内所有示例与伪代码需替换为新的字段名（CritDamageBonusPercent、SpecialAttackPercent、HPPercent、KenChasePercent、ChasePercent、ChaseFlat 等）。
9. Caps 列表更新并与 Step6 文档一致。

--------------------------------
目录
--------------------------------
1. 设计目标与定位  
2. 词条体系核心原则（更新）  
3. 属性与全局上限（Caps）  
4. 词条分类与当前初始清单（v1.4 规范）  
5. 等级成长模型（Lv1 最大递减）  
6. 强化机制与词条同步提升 (+3)  
7. 互斥 / 限量 / 唯一规则（affixRules）  
8. 词条详细定义（示例 JSON）  
9. 与伤害管线的映射关系  
10. 构筑与平衡：堆叠安全性分析  
11. UI 展示与提示（上限 / 递减 / 无效）  
12. 数据校验与自动化测试建议  
13. 上线阶段与迭代计划  
14. 未来扩展预留（分支 / 真伤 / 穿透 / 洗练）  
15. 变更风险与缓解  
16. 总结

--------------------------------
1. 设计目标与定位
--------------------------------
在“主手+9副槽”装备框架内，以固定词条序列（橙品质完整，低品质截取前 N 条）+ 小幅强化(+3) 构筑早期数值生态：
- 低心智：无随机词条池选择，玩家只判断留/拆 → 强化 → 替换可全额回收。
- 多路线：双攻击系（Attack% / SpecialAttack%）、暴击（Crit% / CritDamageBonus%）、混合（vital_force / assault）、态势（盛体 / 背水）、追击（百分比 / 克制 / 固定）、元素共鸣、生存（HP% / DamageReduction%）。
- 可控上限：全部百分比属性进入统一 Caps；固定追击单独监控。
- 强化线性：词条等级同步 +1，不额外乘区，防止爆炸。
- Lv1 大头：提升第一步显著，后续递减鼓励“先获得装备”再优化而非无限刷极端强化。

--------------------------------
2. 词条体系核心原则（更新）
--------------------------------
- 仅百分比（固定追击除外）：简化公式与展示。
- 两攻击系并行：Attack% 与 SpecialAttack% 独立乘入主体层，构筑差异。
- 暴击基础保障：没有暴击伤害词条时仍有基础 1.2× 暴击倍率。
- 盛体 / 背水互斥区间：触发血量不重叠，不需 max 判定逻辑。
- 追击尾部加法：所有追击（包括克制追击）在元素层之后加成，防止乘区放大。
- 上限裁剪：所有百分比在汇总后裁剪，再进入伤害管线。
- 互斥 / 限量：预防多个爆发类或态势类词条堆满导致失衡。
- 强化无损回收：降低尝试成本，提升替换频率与流通。

--------------------------------
3. 属性与全局上限（Caps）
--------------------------------
```json
{
  "caps": {
    "AttackPct": 100.0,
    "SpecialAttackPct": 80.0,
    "HPPercent": 100.0,
    "CritChancePct": 80.0,
    "CritDamageBonusPct": 50.0,
    "FortifyMaxPct": 20.0,
    "BackwaterMaxPct": 20.0,
    "ChasePct": 30.0,
    "KenChasePct": 20.0,
    "DamageReductionPct": 90.0,
    "ChaseFlatCap": 9999
  },
  "crit": {
    "baseMultiplier": 1.2
  }
}
```

--------------------------------
4. 词条分类与当前初始清单
--------------------------------
输出：attack / special_attack / crit / crit_damage_bonus / assault  
混合：vital_force / element_resonance  
生存：hp / mitigation  
态势：fortify_boost / backwater_boost  
追击：chase_pct / chase_flat / ken_chase  
元素：element_resonance（条件触发攻% + 克制追击%）

--------------------------------
5. 等级成长模型（Lv1 最大递减）
--------------------------------
模式举例（单一百分比型）：
- Lv1：基线（显著）
- Lv2：基线增幅中等（+50%~+60% 相对 Lv1 增量）
- Lv3：小幅增量
- Lv4：收尾增量（微提升）

示例 attack：4 / 6 / 7 / 8（Lv1→Lv2 +2，后续 +1 / +1）  
特殊攻击：3 / 4.5 / 5.5 / 6（Lv1→Lv2 +1.5，后续 +1 / +0.5）  
态势类 FortifyMaxPct：10 / 14 / 18 / 20（后两级减小）

--------------------------------
6. 强化机制与词条同步提升 (+3)
--------------------------------
- 强化等级 0→+3：把装备内所有词条等级从基准 Lv1 提升到 Lv4。
- 成本与材料参考强化系统文档（仅通用材料消耗，分层 Tier）。
- 分解返还全部强化投入材料：无强化损耗，无需额外“锁位”逻辑。

--------------------------------
7. 互斥 / 限量 / 唯一规则（affixRules）
--------------------------------
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

--------------------------------
8. 词条详细定义（核心示例 JSON）
--------------------------------
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
    },
    {
      "id": "hp",
      "name": "生命",
      "levels": [
        { "HPPercent": 6.0 },
        { "HPPercent": 8.0 },
        { "HPPercent": 9.0 },
        { "HPPercent": 10.0 }
      ],
      "tags": ["defense","core"]
    },
    {
      "id": "vital_force",
      "name": "盛能",
      "levels": [
        { "AttackPercent": 2.0, "HPPercent": 4.0 },
        { "AttackPercent": 3.0, "HPPercent": 6.0 },
        { "AttackPercent": 3.5, "HPPercent": 7.0 },
        { "AttackPercent": 4.0, "HPPercent": 8.0 }
      ],
      "tags": ["hybrid","core"]
    },
    {
      "id": "crit",
      "name": "会心",
      "levels": [
        { "CritChancePercent": 4.0 },
        { "CritChancePercent": 6.0 },
        { "CritChancePercent": 7.0 },
        { "CritChancePercent": 8.0 }
      ],
      "tags": ["offense"]
    },
    {
      "id": "crit_damage_bonus",
      "name": "终结",
      "levels": [
        { "CritDamageBonusPercent": 3.0 },
        { "CritDamageBonusPercent": 4.0 },
        { "CritDamageBonusPercent": 4.5 },
        { "CritDamageBonusPercent": 5.0 }
      ],
      "tags": ["offense","burst"]
    },
    {
      "id": "assault",
      "name": "神击",
      "levels": [
        { "AttackPercent": 2.0, "CritChancePercent": 2.0 },
        { "AttackPercent": 3.0, "CritChancePercent": 3.0 },
        { "AttackPercent": 3.5, "CritChancePercent": 3.5 },
        { "AttackPercent": 4.0, "CritChancePercent": 4.0 }
      ],
      "tags": ["hybrid","burst"]
    },
    {
      "id": "fortify_boost",
      "name": "盛体增幅",
      "levels": [
        { "FortifyMaxPct": 10.0 },
        { "FortifyMaxPct": 14.0 },
        { "FortifyMaxPct": 18.0 },
        { "FortifyMaxPct": 20.0 }
      ],
      "tags": ["stance"],
      "uniqueGroup": "fortify_unique"
    },
    {
      "id": "backwater_boost",
      "name": "背水意志",
      "levels": [
        { "BackwaterMaxPct": 10.0 },
        { "BackwaterMaxPct": 14.0 },
        { "BackwaterMaxPct": 18.0 },
        { "BackwaterMaxPct": 20.0 }
      ],
      "tags": ["stance"],
      "uniqueGroup": "backwater_unique"
    },
    {
      "id": "chase_pct",
      "name": "追击",
      "levels": [
        { "ChasePercent": 1.8 },
        { "ChasePercent": 2.4 },
        { "ChasePercent": 2.8 },
        { "ChasePercent": 3.2 }
      ],
      "tags": ["chase"]
    },
    {
      "id": "chase_flat",
      "name": "固定追击",
      "levels": [
        { "ChaseFlat": 30 },
        { "ChaseFlat": 45 },
        { "ChaseFlat": 55 },
        { "ChaseFlat": 60 }
      ],
      "tags": ["chase"]
    },
    {
      "id": "ken_chase",
      "name": "克制追击",
      "levels": [
        { "KenChasePercent": 1.2 },
        { "KenChasePercent": 1.8 },
        { "KenChasePercent": 2.4 },
        { "KenChasePercent": 3.0 }
      ],
      "tags": ["chase","element"]
    },
    {
      "id": "mitigation",
      "name": "坚韧",
      "levels": [
        { "DamageReductionPercent": 2.0 },
        { "DamageReductionPercent": 3.0 },
        { "DamageReductionPercent": 3.6 },
        { "DamageReductionPercent": 4.0 }
      ],
      "tags": ["defense"]
    },
    {
      "id": "element_resonance",
      "name": "元素共鸣",
      "levels": [
        { "AttackPercent": 2.0, "KenChasePercent": 1.0 },
        { "AttackPercent": 3.0, "KenChasePercent": 1.5 },
        { "AttackPercent": 3.5, "KenChasePercent": 1.75 },
        { "AttackPercent": 4.0, "KenChasePercent": 2.0 }
      ],
      "tags": ["element","hybrid"]
    }
  ]
}
```

--------------------------------
9. 与伤害管线的映射关系
--------------------------------
主体乘区：AttackPercent, SpecialAttackPercent, StancePct(Fortify/Backwater映射)  
暴击层：CritChancePercent, CritDamageBonusPercent（作用于基础 1.2×）  
元素层：ElementMult（1.5/1.0/0.75）  
追击层：ChasePercent, KenChasePercent(仅克制), ChaseFlat, ElementResonance(附带 KenChase%)  
减伤层：DamageReductionPercent（与 ArmorCurve）  
生命面板：HPPercent 仅影响最大生命，不入伤害乘区。

--------------------------------
10. 构筑与平衡：堆叠安全性分析
--------------------------------
极端满配（10 件 Lv4）：
- Attack% = 80% (<100)  
- SpecialAttack% = 60% (<80)  
- HP% = 100% (=Cap)  
- CritChance% = 80% (=Cap)  
- CritDamageBonus% = 50% (=Cap) → 最大暴击倍率 1.8  
- ChasePct（限制8件）= 25.6% (<30)  
- KenChasePct（限制6件 + 元素共鸣）最大 ≈ 18% + 2% = 20% (=Cap)  
- Fortify / Backwater = 20% (=Cap, 不共存)  
- DamageReduction% = 40% (<90)  
→ 全部在安全范围，有余量给临时 Buff / 活动增益。

--------------------------------
11. UI 展示与提示
--------------------------------
- 词条行：名称 / 等级 / 当前百分比 / 下一等级增量（递减标识）  
- 属性达到上限：显示“已达上限”徽记 + 阴影数值（不再提升）  
- 强化预览：整件装备 Lv1→Lv4 聚合变化  
- 追击占比统计：战斗日志末尾显示“追击贡献比”以监控平衡

--------------------------------
12. 数据校验与自动化测试建议
--------------------------------
自动校验脚本：
1. 随机生成 10 件橙装（全 4 词条）  
2. 模拟强化 +3（全部 Lv4）  
3. 汇总各属性 → 检查是否超过 Caps 或违反 equipLimit / uniqueGroup。  
4. 输出“超限字段”列表（若为空则通过）。

关键测试：
- Lv1→Lv4 每级增量衰减性断言  
- 互斥词条同一装备组合拒绝  
- 同件 chase_pct 与 chase_flat 互斥  
- fortify / backwater 各最多 1 件  
- 克制场景下 ken_chase 与 element_resonance 叠加不突破上限  
- 强化回收正确（词条等级回退不保留，重新获取新装备后再强化）

--------------------------------
13. 上线阶段与迭代计划
--------------------------------
Phase A：落地基础词条（attack / special_attack / hp / crit / fortify / backwater / chase_pct / mitigation / crit_damage_bonus）  
Phase B：开放混合/元素（vital_force / assault / ken_chase / element_resonance / chase_flat）  
Phase C：数据采样与平衡微调（若某属性平均利用率 >80% 长期）  
Phase D：扩展分支词条 / 词条替换系统 / 真伤或穿透

--------------------------------
14. 未来扩展预留
--------------------------------
- 分支：元素共鸣可二选一（攻% 或 防御穿透%）  
- 真伤追击（TrueChasePercent）独立 Cap（≤5%）  
- ArmorPenetrationPercent（穿透词条）进入防御层前修正  
- 词条洗练：消耗材料替换第4词条  
- 强化继承：新装备耗特殊材料继承旧强化等级（旧装备不返还材料）

--------------------------------
15. 变更风险与缓解
--------------------------------
| 风险 | 缓解 |
|------|------|
| 攻% 与 特攻% 路线差异度不足 | 后期加入分支（特攻对特定标签技能增幅）保持初期简单 |
| 生命% 满配过高影响生存 | 活动/副本伤害按 HP% 规模设定，或下调 Lv4 增量 |
| 追击伤害占比过高 | 降 chase_pct Lv4 值或收紧 equipLimit |
| 暴击过度稳定 | 引入暴击期望衰减（溢出部分转化为精准/压制） |

--------------------------------
16. 总结
--------------------------------
v2.1 对原 v2.0 装备词条系统进行了必要修订以对齐最新讨论结果：双攻击系、纯百分比化、HP% 独立、暴击基础倍率明确、Lv1 最大收益、强化线性无爆炸、互斥限量控制安全堆叠。该版本与 Step6 伤害管线保持一致，可直接用于实现与测试。后续若需自动校验脚本或代码骨架，请继续指示。  