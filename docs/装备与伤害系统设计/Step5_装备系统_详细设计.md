# Step5 GBF式装备系统（主手+9装备栏）详细设计

版本：v1.3  
日期：2025-11-29  
作者：@copilot  
更新说明：主手与副槽的“可装备性”规则修正——同一件装备既可放入主手槽也可放入副装备槽；仅当装备放入主手槽时才受职业限制与“角色元素切换”影响。副槽不受职业限制。

--------------------------------
目录
1. 设计目标与总体思路  
2. 范围与非目标（MVP 与延伸）  
3. 装备栏结构与属性模型（简化：攻击+生命）  
4. 槽位规则修正（主手职业限制 + 主手决定角色元素；副槽自由）  
5. 元素体系（轻量方案 B 与 1.5×/0.75× 最终倍率）  
6. 技能与装备的元素归属优先级（主手决定角色元素）  
7. 词条与属性上限（伤害+100%，暴击率+80% 等）  
8. 数值计算接入管线（最终乘区）  
9. 数据与配置字段（equipment/skills/monsters/professions/elementMatrix）  
10. UI 与交互（装备面板与战斗日志）  
11. 测试矩阵（更新后）  
12. 平衡与调参建议  
13. 实施阶段与任务清单  
14. 示例配置片段  
15. 伪代码（装备校验与元素决定）  
16. 后续扩展预留

--------------------------------
1. 设计目标与总体思路
--------------------------------
- 10 槽位：1 主手 + 9 副槽，采用“所见即所得”的属性汇总（无槽权重/稀释），再套用硬上限。
- 基础属性仅保留：攻击力（Attack）与生命值（HP）；词条包含：伤害%、暴击%、急速%、技能修正等。
- 轻量元素体系：火/水/风/土/光/暗/无；固定最终伤害倍率：克制 1.5×，被克制 0.75×，无克制 1.0×。
- 槽位规则（修正）：同一件装备既可放入主手槽，也可放入任一副槽；仅当装备位于主手槽时才受职业武器类型限制，并决定角色当前元素。副槽不受职业限制。

--------------------------------
2. 范围与非目标（MVP 与延伸）
--------------------------------
MVP：
- 主手槽职业武器类型限制（allowedWeaponTypes per profession）
- 副槽自由装备（不受职业限制），遵守通用规则（等级/唯一/互斥等）
- 角色默认 Neutral；主手槽当前装备的元素决定角色元素（若主手为空或该装备无元素 → Neutral）
- 元素最终乘区（1.5×/0.75×/1.0×）接入伤害管线
- 装备面板、技能 Tooltip、战斗日志元素提示

非目标：
- 副槽职业细分限制
- 元素耐性数值化
- 随机词条/套装/强化（后续）

--------------------------------
3. 装备栏结构与属性模型（简化：攻击+生命）
--------------------------------
- 槽位索引：slotIndex 0 为主手；slotIndex 1..9 为副槽。
- 属性汇总：
  - AttackFlat、HPFlat：所有槽位装备求和。
  - DamagePercent、CritChancePercent、HastePercent、SkillDamagePercent：求和后硬上限裁剪。
  - SkillMods：按 skillId 聚合；Triggers：合并进入 TriggerProcessor。
- 无槽系数与稀释：保持直观。

--------------------------------
4. 槽位规则修正（主手职业限制 + 主手决定角色元素；副槽自由）
--------------------------------
- 同一件装备可装备到主手槽或任意副槽（数据结构不再区分“只能主手/只能副槽”）。
- 仅当装备尝试放入主手槽（slotIndex=0）时进行职业武器类型校验：
  - item.weaponType ∈ profession.allowedWeaponTypes
  - 若装备自身限定 allowedProfessions，则也需满足（可选）
  - 满足等级/解锁等通用条件
- 当装备位于主手槽时，角色元素 = 该装备的 element（缺失或空 → Neutral）。
- 当装备移出主手槽或主手槽为空 → 角色元素重置为 Neutral（除非技能本身定义元素，见优先级）。
- 副槽装备不受职业武器类型限制，仅遵守通用规则（唯一/互斥/等级）。

--------------------------------
5. 元素体系（轻量方案 B 与 1.5×/0.75× 最终倍率）
--------------------------------
- 元素枚举：Fire/Water/Wind/Earth/Light/Dark/Neutral。
- 克制矩阵（循环链）：
  - Fire → Wind: 1.5×；Wind → Fire: 0.75×
  - Wind → Earth: 1.5×；Earth → Wind: 0.75×
  - Earth → Water: 1.5×；Water → Earth: 0.75×
  - Water → Fire: 1.5×；Fire → Water: 0.75×
- 光/暗玩家优势：
  - 玩家 Light → 怪物 Dark：输出 1.5×，受击 0.75×
  - 玩家 Dark → 怪物 Light：输出 1.5×，受击 0.75×
  - 同元素对战：1.0×
- Neutral：无加减 → 1.0×。

--------------------------------
6. 技能与装备的元素归属优先级（主手决定角色元素）
--------------------------------
- 角色元素：
  - 默认 Neutral；
  - 若主手槽装备存在元素 → 使用该元素；否则 Neutral。
- 技能元素优先级：
  1) 若 skill.element 存在 → 使用技能元素；
  2) 否则使用角色元素（由主手槽决定）。
- 怪物元素来自其配置，缺失则 Neutral。

--------------------------------
7. 词条与属性上限（伤害+100%，暴击率+80% 等）
--------------------------------
- attributeCaps（硬上限）：
```json
"attributeCaps": {
  "DamagePercent": 100.0,
  "CritChancePercent": 80.0,
  "HastePercent": 50.0,
  "SkillDamagePercent": 120.0
}
```
- 元素倍率不参与该上限，位于最终乘区。

--------------------------------
8. 数值计算接入管线（最终乘区）
--------------------------------
伤害顺序：
1) 基础伤害（AttackFlat × 技能系数）
2) 词条/Buff 加成（Damage%、SkillDamage%、Haste 等）
3) 暴击修正（若命中）
4) 元素最终乘区（根据攻击方元素与防守方元素）
5) 写 CombatEvent（保存 elementMultiplier 与说明）

--------------------------------
9. 数据与配置字段（equipment/skills/monsters/professions/elementMatrix）
--------------------------------
- equipment.json：
  - 通用：id, name, weaponType?, element?, baseStats, allowedProfessions?
  - 不再固定“slot: main/sub”，而是可装备到任意槽；“主手槽”由装备操作位置决定（slotIndex=0）。
- skills.json：
  - element（可选；缺失则使用角色元素）。
- monsters.json：
  - element（可选；缺失则 Neutral）。
- professions.json：
  - { id, name, allowedWeaponTypes: ["sword","axe"] }
- elementMatrix.json：
```json
{
  "elementMatrix": {
    "Fire":  { "Wind": 1.5 },
    "Wind":  { "Earth": 1.5 },
    "Earth": { "Water": 1.5 },
    "Water": { "Fire": 1.5 },
    "Light": { "Dark": 1.5 },
    "Dark":  { "Light": 1.5 }
  },
  "reverseMultiplier": 0.75,
  "defaultMultiplier": 1.0
}
```

--------------------------------
10. UI 与交互（装备面板与战斗日志）
--------------------------------
- 装备面板：
  - 主手槽（slotIndex=0）显著标识；当试图将装备放入主手槽时，提示职业可用武器类型并校验。
  - 副槽自由放置，不显示职业限制提示。
  - 面板摘要显示当前角色元素（来源：主手槽装备）。
- 战斗日志：
  - 显示元素克制提示：“克制 +50% / 被克制 -25%”；Neutral 不提示。

--------------------------------
11. 测试矩阵（更新后）
--------------------------------
- 装备可在主手/副槽互换（同 itemId），属性汇总一致（2）
- 主手槽职业武器类型校验：允许/拒绝（4）
- 主手决定元素：空主手→Neutral；有元素主手→角色元素切换（3）
- 技能元素优先级：技能元素覆盖主手元素 / 无技能元素使用主手元素（3）
- 元素克制矩阵：克制/被克制/无克制正确（4）
- 光/暗玩家优势：输出 1.5× / 受击 0.75×（3）
- 副槽不受职业限制但遵守唯一/等级等通用规则（3）

--------------------------------
12. 平衡与调参建议
--------------------------------
- 若职业主手限制过严导致卡构筑，可提供 Neutral 主手（高基础 Attack/HP）作为通用解。
- 监控元素使用与加成分布，必要时调整 1.5/0.75 为 1.3/0.8。
- 唯一/互斥标签用于抑制“同词条横向堆满”。

--------------------------------
13. 实施阶段与任务清单
--------------------------------
Phase E1（P0）
- professions.json 引入 allowedWeaponTypes；主手槽装备时进行职业校验
- 装备数据不区分 main/sub；UI/逻辑按槽位位置区分
- SkillResolver 元素层与优先级接入
- 单元/集成测试补齐

Phase E2（P1）
- UI 提示与交互完善：主手槽校验反馈、角色元素展示、日志标注
- 文档与示例配置完善

--------------------------------
14. 示例配置片段
--------------------------------
profession：
```json
{ "id": "warrior", "allowedWeaponTypes": ["sword","axe"] }
```

装备（同一个定义，可装主手或副槽）：
```json
{
  "id": "flame_blade",
  "name": "烈焰刀",
  "weaponType": "sword",
  "element": "Fire",
  "baseStats": { "AttackFlat": 120, "HPFlat": 60, "DamagePercent": 10 }
}
```

技能：
```json
{ "id": "flame_slash", "name": "烈焰斩", "element": "Fire", "type": "active", "releaseType": "instant", "damage": { "coef": 1.2 } }
```

怪物：
```json
{ "id": "wind_spirit", "name": "风之精灵", "element": "Wind", "hp": 500 }
```

--------------------------------
15. 伪代码（装备校验与元素决定）
--------------------------------
```csharp
bool CanEquipToSlot(Character c, EquipmentItem item, int slotIndex) {
    if (slotIndex == 0) {
        // 主手槽：职业武器类型限制
        var allowed = c.Profession.AllowedWeaponTypes?.Contains(item.WeaponType) ?? true;
        if (!allowed) return false;
        // 可选：装备自身的 allowedProfessions
        if (item.AllowedProfessions != null && !item.AllowedProfessions.Contains(c.Profession.Id))
            return false;
    }
    // 副槽：不做职业类型限制
    // 通用条件检查（等级/唯一/互斥等）
    return CheckGenericRules(c, item);
}

string ResolveCharacterElement(Character c) {
    var mainItem = c.Equipment.Slots[0];
    return string.IsNullOrEmpty(mainItem?.Element) ? "Neutral" : mainItem.Element;
}

string ResolveSkillElement(SkillDef skill, Character c) {
    return string.IsNullOrEmpty(skill.Element) ? ResolveCharacterElement(c) : skill.Element;
}
```

--------------------------------
16. 后续扩展预留
--------------------------------
- 主手“皮肤”与外观（无数值变化）
- 元素套装：同元素达到 N 件触发额外增益（受软上限控制）
- Neutral 专精主手：通用构筑入口
- 元素伤害%词条（独立上限）与元素耐性系统（未来）

--------------------------------
总结
v1.3 修正了“主手与副槽的可装备性”设计：同一装备可放主手或副槽；仅主手受职业武器类型限制且决定角色元素。副槽保持自由度，整体仍遵循“所见即所得 + 硬上限 + 轻量元素”的设计，简洁直观且易于实施与平衡。  
下一步：按该规则调整装备/职业/技能配置，落地主手校验与元素层接入，并更新 UI 与测试。  