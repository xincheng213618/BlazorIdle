# Step5 装备词条系统（GBF / 怪猎风格“技能化词条”）详细设计

版本：v2.2  
日期：2025-11-30  
作者：@copilot

本次增量（基于 v2.1）：补充“元素限定生效”与“作用域（scope）”机制，明确输出类词条仅在装备元素与主元素一致时激活；保留 v2.1 的双攻击系（Attack%/SpecialAttack%）、HP%、暴击基础倍率（1.2）与加成%、Lv1收益最大的成长模型、互斥/限量规则、与伤害管线的映射关系。

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
8. 元素与作用域（scope）机制（新增）  
9. 主元素确定与匹配规则（新增）  
10. 生效矩阵（新增）  
11. 聚合与过滤流程（新增伪代码）  
12. 备用池（reserve）与主手切换（新增）  
13. 词条详细定义（示例 JSON，含 scope）  
14. 与伤害管线的映射关系  
15. 构筑与平衡：堆叠安全性分析  
16. UI 展示与提示（上限 / 递减 / 未激活标识）  
17. 数据校验与自动化测试建议  
18. 上线阶段与迭代计划  
19. 未来扩展预留（分支 / 真伤 / 穿透 / 洗练 / 错配折损）  
20. 变更风险与缓解  
21. 总结

--------------------------------
1. 设计目标与定位
--------------------------------
在“主手+9副槽”的装备框架内，采用固定词条序列（橙品质完整、低品质截取前 N 条）+ 小幅强化（+3）构筑早期数值生态。新增“元素限定生效”使输出类词条的收益聚焦于玩家当前主元素，促进元素一致的构筑选择，同时保留通用生存与暴击类词条的全局有效性。

--------------------------------
2. 词条体系核心原则（更新）
--------------------------------
- 仅百分比（固定追击除外）：简化公式与展示。
- 双攻击系并行：Attack% 与 SpecialAttack% 独立乘入主体层（管线中的 Attack 与 SpecialAttack 乘区）。
- 暴击基础保障：无暴击伤害词条时，暴击倍率=1.2；词条仅提供额外暴击伤害加成%（总上限 50%）。
- 盛体 / 背水互斥区间：HP≥75% 触发盛体；HP≤50% 触发背水；区间不重叠。
- 追击尾部加法：追击%、克制追击%、固定追击在元素层后以加法形式叠加。
- 上限裁剪：所有百分比在汇总后裁剪至 Caps，再进入伤害管线。
- 互斥 / 限量：防止爆发或态势类词条堆满导致失衡。
- 元素限定生效（新增）：输出类词条仅在“装备元素 == 主元素”时激活；通用生存/暴击类词条不受元素限制。
- 强化无损回收：分解返还全部强化材料，降低尝试成本。

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
  "crit": { "baseMultiplier": 1.2 }
}
```

--------------------------------
4. 词条分类与当前初始清单（v1.4 规范）
--------------------------------
输出：attack / special_attack / crit / crit_damage_bonus / assault  
混合：vital_force / element_resonance  
生存：hp / mitigation  
态势：fortify_boost / backwater_boost  
追击：chase_pct / chase_flat / ken_chase  
元素：element_resonance（元素匹配时提供 Attack% 与 KenChase%）

--------------------------------
5. 等级成长模型（Lv1 最大递减）
--------------------------------
- Lv1：显著提升（大头）
- Lv2：中等提升（约为 Lv1 增量的 50~60%）
- Lv3：小幅提升
- Lv4：收尾微增

示例：  
- attack：4 / 6 / 7 / 8  
- special_attack：3 / 4.5 / 5.5 / 6  
- fortify_max：10 / 14 / 18 / 20

--------------------------------
6. 强化机制与词条同步提升 (+3)
--------------------------------
- 强化 0→+3：将装备内所有词条等级从 Lv1 提升至 Lv4。
- 仅消耗分层通用材料；分解返还全部强化投入材料（不含基础分解收益）。

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
8. 元素与作用域（scope）机制（新增）
--------------------------------
- scope 类型：
  - global：不做元素匹配，始终生效（HP%、减伤%、暴击率%、暴击伤害加成%、盛体/背水上限、固定追击）。
  - element：仅在“装备元素 == 主元素”时生效（Attack%、SpecialAttack%、Chase%、KenChase%、ElementResonance 的 Attack/Ken 部分）。
  - hybrid：同一词条内字段分属不同作用域（例如 assault：Attack%→element；Crit%→global）。
- 设计意图：鼓励统一元素构筑，同时避免通用生存/暴击属性在错配时完全失效。

--------------------------------
9. 主元素确定与匹配规则（新增）
--------------------------------
- 主元素 = 主手（slot 0）装备的元素；若主手为空或元素为 Neutral → 主元素=Neutral。
- 激活条件（element-scope）：equip.element == 主元素。
- Neutral 主元素：
  - 激活 Neutral 元素的 element-scope 字段与所有 global 字段；
  - 非 Neutral 的 element-scope 字段不激活（进入 reserve）。

--------------------------------
10. 生效矩阵（新增）
--------------------------------
| 字段 | 作用域 | 生效条件 |
|------|--------|----------|
| AttackPercent | element | 装备元素==主元素 |
| SpecialAttackPercent | element | 装备元素==主元素 |
| ChasePercent | element | 装备元素==主元素 |
| KenChasePercent | element | 装备元素==主元素 且元素克制成立 |
| HPPercent | global | 始终生效 |
| DamageReductionPercent | global | 始终生效 |
| CritChancePercent | global | 始终生效 |
| CritDamageBonusPercent | global | 始终生效（叠加到基础 1.2 倍） |
| FortifyMaxPct / BackwaterMaxPct | global | 始终生效 |
| ChaseFlat | global | 始终生效 |
| ElementResonance.AttackPercent | element | 装备元素==主元素 |
| ElementResonance.KenChasePercent | element | 装备元素==主元素 且克制成立 |

--------------------------------
11. 聚合与过滤流程（新增伪代码）
--------------------------------
```csharp
AggregatedStats Aggregate(List<Equipment> equips, Element main) {
  var active = new StatBag();
  var reserve = new StatBag();

  foreach (var eq in equips) {
    foreach (var affix in eq.affixes) {
      var lvl = GetLevelData(affix.id, affix.level);
      foreach (var (field, value) in lvl.Fields) {
        var fieldScope = ResolveScope(affix.id, field); // global / element
        bool isActive = fieldScope == "global" || eq.element == main;
        (isActive ? active : reserve).Add(field, value);
      }
    }
  }

  // Caps 仅对 active 生效
  active["AttackPercent"] = Clamp(active["AttackPercent"], 0, Caps.AttackPct);
  active["SpecialAttackPercent"] = Clamp(active["SpecialAttackPercent"], 0, Caps.SpecialAttackPct);
  active["CritChancePercent"] = Clamp(active["CritChancePercent"], 0, Caps.CritChancePct);
  active["CritDamageBonusPercent"] = Clamp(active["CritDamageBonusPercent"], 0, Caps.CritDamageBonusPct);
  active["ChasePercent"] = Clamp(active["ChasePercent"], 0, Caps.ChasePct);
  active["KenChasePercent"] = Clamp(active["KenChasePercent"], 0, Caps.KenChasePct);
  active["DamageReductionPercent"] = Clamp(active["DamageReductionPercent"], 0, Caps.DamageReductionPct);
  // HPPercent、Fortify/Backwater 上限等同理

  return new AggregatedStats { Active = active, Reserve = reserve };
}
```

--------------------------------
12. 备用池（reserve）与主手切换（新增）
--------------------------------
- reserve：保存未激活的元素限定字段（用于预览与构筑参考）。
- 主手切换：重新聚合；部分 reserve 值迁入 active。
- UI：展示“未激活（因元素不匹配）”的数值与“切换主手后可激活”的提示。

--------------------------------
13. 词条详细定义（示例 JSON，含 scope）
--------------------------------
attack（元素限定）：
```json
{
  "id": "attack",
  "name": "攻击",
  "scope": "element",
  "levels": [
    { "AttackPercent": 4.0 },
    { "AttackPercent": 6.0 },
    { "AttackPercent": 7.0 },
    { "AttackPercent": 8.0 }
  ],
  "tags": ["offense"]
}
```

special_attack（元素限定）：
```json
{
  "id": "special_attack",
  "name": "特攻",
  "scope": "element",
  "levels": [
    { "SpecialAttackPercent": 3.0 },
    { "SpecialAttackPercent": 4.5 },
    { "SpecialAttackPercent": 5.5 },
    { "SpecialAttackPercent": 6.0 }
  ],
  "tags": ["offense"]
}
```

hp（全局）：
```json
{
  "id": "hp",
  "name": "生命",
  "scope": "global",
  "levels": [
    { "HPPercent": 6.0 },
    { "HPPercent": 8.0 },
    { "HPPercent": 9.0 },
    { "HPPercent": 10.0 }
  ],
  "tags": ["defense","core"]
}
```

assault（混合：Attack% 元素限定 / Crit% 全局）：
```json
{
  "id": "assault",
  "name": "神击",
  "scope": "hybrid",
  "perFieldScope": {
    "AttackPercent": "element",
    "CritChancePercent": "global"
  },
  "levels": [
    { "AttackPercent": 2.0, "CritChancePercent": 2.0 },
    { "AttackPercent": 3.0, "CritChancePercent": 3.0 },
    { "AttackPercent": 3.5, "CritChancePercent": 3.5 },
    { "AttackPercent": 4.0, "CritChancePercent": 4.0 }
  ],
  "tags": ["hybrid","burst"]
}
```

element_resonance（元素共鸣，元素限定）：
```json
{
  "id": "element_resonance",
  "name": "元素共鸣",
  "scope": "element",
  "levels": [
    { "AttackPercent": 2.0, "KenChasePercent": 1.0 },
    { "AttackPercent": 3.0, "KenChasePercent": 1.5 },
    { "AttackPercent": 3.5, "KenChasePercent": 1.75 },
    { "AttackPercent": 4.0, "KenChasePercent": 2.0 }
  ],
  "tags": ["element","hybrid"]
}
```

其余词条（crit、crit_damage_bonus、fortify_boost、backwater_boost、mitigation、chase_pct、ken_chase、chase_flat）按 v2.1 定义，scope 根据“生效矩阵”设置为 global 或 element。

--------------------------------
14. 与伤害管线的映射关系
--------------------------------
- 主体乘区：AfterMain = AfterVariance × (1 + AttackPctActive) × (1 + SpecialAttackPctActive) × (1 + StancePct)  
  - AttackPctActive / SpecialAttackPctActive：由聚合器过滤后输出的“已激活”值。  
- 暴击层：基础 1.2 × (1 + CritDamageBonus%Active)，暴击率使用 CritChance%Active。  
- 元素层：克制 1.5 / 被克制 0.75 / 其他 1.0。  
- 追击层：Chase%Active、KenChase%Active（仅克制时）、ChaseFlatActive（固定值）。  
- 减伤层：DamageReduction%Active 与 ArmorCurve。  
- HPPercentActive：仅影响最大生命面板，不入伤害乘区。

--------------------------------
15. 构筑与平衡：堆叠安全性分析
--------------------------------
极端满配（10 件 Lv4，元素匹配下）：
- Attack%Active = 80%（<100）  
- SpecialAttack%Active = 60%（<80）  
- HP%Active = 100%（=Cap）  
- CritChance%Active = 80%（=Cap）  
- CritDamageBonus%Active = 50%（=Cap） → 最大暴击倍率 1.8  
- ChasePctActive（限制8件）= 25.6%（<30）  
- KenChasePctActive（限制6件 + 元素共鸣）最大 ≈ 18% + 2% = 20%（=Cap）  
- Fortify/Backwater = 20%（=Cap，互斥）  
- DamageReduction%Active = 40%（<<90）

--------------------------------
16. UI 展示与提示（上限 / 递减 / 未激活标识）
--------------------------------
- 词条行：显示当前等级、百分比值与下一等级增量（标注“Lv1收益最大”）。  
- 上限提示：达上限时显示徽记与“提升无效”说明。  
- 元素未激活：以元素图标与灰显标示，并提示“当前主元素：Water（装备元素：Fire），此词条未激活”。  
- 主手切换预览：快速预览不同主元素下的“Active/Reserve”合计与伤害预估差异。

--------------------------------
17. 数据校验与自动化测试建议
--------------------------------
- Caps 校验：Active 值不越过上限；Reserve 不裁剪，仅展示。  
- 互斥/限量：assault 与 vital_force/crit_damage_bonus 互斥；chase_pct/ken_chase equipLimit；同件最多一个追击类型。  
- 元素过滤：主元素 Fire → Fire 的 Attack%Active 生效、Water 的进入 Reserve；主元素切换后迁移正确。  
- 态势互斥：HPRatio 边界（0.50、0.75）映射正确。  
- 强化与分解：+3 等级同步提升；分解返还材料与激活状态无关。

--------------------------------
18. 上线阶段与迭代计划
--------------------------------
Phase A：实现聚合器（scope 过滤、Active/Reserve 统计、Caps 裁剪）  
Phase B：接入伤害管线（读取 Active 值）；UI 未激活标识与主手预览  
Phase C：数据采样与平衡微调（掉落与词条出现频率）  
Phase D：扩展分支词条 / 洗练 / 错配折损模式（可选）

--------------------------------
19. 未来扩展预留（分支 / 真伤 / 穿透 / 洗练 / 错配折损）
--------------------------------
- 错配折损：element-scope 在不匹配下按 25% 生效（配置开关）。  
- 真伤追击（TrueChase%）：不受减伤层，严格 Cap（≤5%）。  
- 穿透（ArmorPenetration%）：进入防御层前修正。  
- 分支选择：元素共鸣可在 Attack% 与 防穿透% 中二选一。  
- 词条洗练：消耗材料替换第 4 词条或变更 perFieldScope。

--------------------------------
20. 变更风险与缓解
--------------------------------
| 风险 | 缓解 |
|------|------|
| 多元素副槽输出失效挫败 | 提供 Reserve 展示与前期提升 global 词条占比 |
| 玩家不理解元素限定 | 明确 UI 标识与教学弹窗 |
| 构筑过度单一 | 后期加入错配折损/双主元素机制 |
| 数值爆炸 | 严格 Caps + 互斥/限量 + 追击尾部加法 |

--------------------------------
21. 总结
--------------------------------
v2.2 在 v2.1 的基础上补齐“元素限定生效”机制，通过 scope（global/element/hybrid）与 Active/Reserve 聚合过滤，使输出类词条仅在元素匹配时激活，通用生存与暴击类词条始终有效。该更新不改变伤害管线结构，只影响聚合输入，便于实现与测试，并进一步鼓励元素一致的构筑路线。  
如需，我可以继续输出 ElementScopedAggregator 的代码骨架与测试用例模板。  