# Step2 技能系统总体设计（主动/被动 + 瞬发/施法 + 目标选择 + 资源/CD + 触发类技能 + 与 Buff 配置联动）

版本：v1.2  
整理时间：2025-11-14  
适配现有框架：Step0（统一SkillCast/Track）、Step1（资源/Buff/事件/UI）

—— 目录 ——
1. 目标与范围
2. 核心概念与术语
3. 触发窗口与执行顺序（Window-GCD 与瞬发共触发）
4. 技能分类与公共属性
5. 目标选择策略
6. 资源/冷却/条件判定与结算时机
7. 触发类技能（Trigger）机制
8. 施法技能（Cast）与 CastingController
9. 与 Buff 系统联动（BuffConfigId）
10. 与现有战斗框架的集成（AutoCastEngine / SkillResolver / 事件）
11. 数据结构（skills.json）与字段说明
12. 示例技能（战士/共享/法师）
13. UI 交互与可视化
14. 事件与可观测性
15. 测试计划与验收标准
16. 性能与确定性
17. 错误处理、边界与回退策略
18. 扩展预留（后续阶段）
19. 伪代码（核心执行流程）

--------------------------------
1. 目标与范围
--------------------------------
- 技能类型：主动（Active）/ 被动（Passive）
- 释放类型：瞬发（Instant）/ 施法（Cast，带施法条）
- 目标选择：current_target / enemies_all / allies_lowest_hp_pct / self / allies_all
- 限制：资源消耗 + 冷却（CD）；窗口互斥（Window-GCD）；瞬发技能可同窗共触发（按槽位顺序）
- 触发类技能（Trigger）：被动/装备等以概率/条件触发某个“真实技能”
- 数据：JSON（skills.json）集中管理，Buff 通过 buffConfigId 与 buffs.json 联动
- 职业与解锁：限定职业、等级、账户解锁标记（共享技能）
- 与现有系统对齐：SkillResolver、CastingController、AutoCastEngine、MultiBattleInstance、Segment 事件

不在本步：
- 手动释放/快捷键输入（预留）
- 时间型 GCD（global cooldown ms 的统一时间窗，预留字段）
- 命中率/闪避/格挡完整判定（暂用 AlwaysHits=true，命中机制后续实现）
- 技能等级成长/多段引导/通道技能等高级形态（预留字段）

--------------------------------
2. 核心概念与术语
--------------------------------
- 窗口（Window）：技能检查与触发的逻辑时机。三类：
  - Pre-Attack：普攻前（用于施法技能起手）
  - Post-Attack：普攻后（用于瞬发技能与触发类技能）
  - Post-Cast：施法完成后（与 Post-Attack 同规则）
- Window-GCD（窗口互斥）：同一窗口内，共CD技能（isGcd=true）仅允许一个触发
- 瞬发共触发：同窗内非GCD瞬发技能可按槽位顺序依次触发（可多个）
- 固定技能：职业内置、不可替换；优先级高于配置技能
- 配置技能：角色在“技能设置组件”中为当前职业装配（战斗中不可改）
- 槽位（Slot）：每职业默认 3 个主动槽 + 1 个被动槽（可扩展）
- 触发类技能（Trigger）：被动/装备等定义的概率触发，触发一个真正的技能（通常非GCD瞬发）

--------------------------------
3. 触发窗口与执行顺序（Window-GCD 与瞬发共触发）
--------------------------------
执行顺序（每个 Tick）：
A) 若正在施法 → 推进 CastingController → 结束后进入 Post-Cast 窗口（见 C）
B) 若未施法，Pre-Attack 窗口（仅施法技能）：
   - 按“固定技能（Cast）→ 槽位1→2→3（Cast）”遍历
   - 满足条件（资源/CD/解锁/目标/条件）则 StartCast，暂停 AttackTrack，本次不执行普攻，进入施法中
C) Post-Attack / Post-Cast 窗口（同规则）：
   1) 主动技能（Instant）
      - GCD组：固定→槽1→槽2→槽3 找到第一个满足条件者 → 触发1个
      - 非GCD组：槽1→槽2→槽3 逐一满足即触发，可多个（按顺序）
   2) 触发类技能（Triggers）
      - 在窗口内判定（如 OnAttackHit 20%），收集命中的触发
      - 按 priority 升序执行触发技能（默认不占GCD）
      - 安全上限：每窗口最多 N（默认5）条触发（防极端）

注意：
- 同一窗口内，所有被触发技能与普攻共享一个 bundleId，事件顺序按上述流程稳定输出
- Cast 技能固定视为 isGcd=true（在本窗口互斥）

--------------------------------
4. 技能分类与公共属性
--------------------------------
- 主动技能（Active）：
  - releaseType：instant | cast（castTimeSec>0）
  - isGcd：是否参与窗口互斥（Cast 固定 true；Instant 可 true/false）
  - cooldownSec / costs[] / conditions / damage（coefAtk + flat + isAoe）
  - onCastBuffs / onHitBuffs（BuffOperation，引用 buffConfigId）
- 被动技能（Passive）：
  - 常驻光环/Aura（onCastBuffs 施加到 self/allies_all 等）
  - 或仅注册 triggers（触发类技能定义）
- 固定 vs 可配置：
  - fixed=true：固定技能，优先级高，默认常驻
  - fixed=false：可配置技能，装入槽位后生效

--------------------------------
5. 目标选择策略
--------------------------------
- current_target：当前普攻锁定目标（若无则失败）
- enemies_all：所有存活敌人
- allies_lowest_hp_pct：友方中 HP% 最低者
- self：自身
- allies_all：所有友方
备注：后续可扩展 targetCount、筛选条件（如仅Boss、仅濒血）

--------------------------------
6. 资源/冷却/条件判定与结算时机
--------------------------------
- 资源（costs）：
  - Instant：在施放瞬间扣除，失败不扣
  - Cast：StartCast 时扣除，失败起手（资源不足）则不进入施法
- 冷却（CD）：
  - 施放成功立即进入 CD（Cast 在完成时进入或在起手时进入，二选一；本期建议“完成时进入”，避免被打断损失 CD）
- 条件（conditions）：
  - hpBelowPct / hpAbovePct / requireBuffId / forbidBuffId / requireResource
  - unlock：{minLevel, accountFlags[], requiresProfessionLevel?}
  - allowedProfessions：职业白名单
- 命中判定：
  - 本期默认 AlwaysHits=true；OnHitBuff 视为命中即生效
  - 命中率机制后续实现（保留 SkillDef.canCrit/alwaysHits 字段）

--------------------------------
7. 触发类技能（Trigger）机制
--------------------------------
- 定义位置：通常定义在“被动技能”或“装备被动”里
- 字段：
  - when: "OnAttackHit" | "OnAttackCrit" | "OnPostAttackWindow" | "OnPostCastWindow"（本期至少支持 OnAttackHit）
  - procChance: 0~1 概率
  - fireSkillId: 触发的“真实技能”（需在 skills.json 中有定义）
  - priority: 数值越小优先执行（如 15 < 20）
  - conditions: 触发条件（可选）
  - overrides: 可选覆盖 fireSkill 的 isGcd/targetPolicy 等（本期默认不覆盖）
- 行为：
  - 触发技能默认非GCD瞬发，可与主动技能同窗共存
  - 可多条同时命中，按 priority 升序依次触发
  - RNG 消耗顺序：按 triggers 声明顺序保证确定性
  - 防递归：触发技能不会在同一窗口再次触发“OnAttackHit”类型的触发器

--------------------------------
8. 施法技能（Cast）与 CastingController
--------------------------------
- StartCast（Pre-Attack 判定时）：
  - 校验：CD/资源/解锁/条件/目标可用
  - 扣资源（本期在起手扣或完成扣二选一；建议“起手扣”更直观，若中断不返还）
  - 暂停 AttackTrack（SpecialTrack 是否暂停可由技能定义或默认策略）
  - 记录 CastStartEvent（可选）
- Casting：
  - 由 CastingController.Tick 推进，达到 castTimeSec 完成
- Complete：
  - 释放技能效果（伤害/Heal/Buff），进入冷却（建议在完成时）
  - 进入 Post-Cast 窗口：可追加瞬发技能（按“GCD一个 + 非GCD多个”的顺序）
  - 记录 CastCompleteEvent（可选）

--------------------------------
9. 与 Buff 系统联动（BuffConfigId）
--------------------------------
- 技能只引用 buffConfigId（来自 buffs.json），由 BuffRepository 解析生成 BuffInstance
- onCastBuffs：在施法起手（或瞬发施放时）施加；通常用于 Aura/自强化/即时治疗
- onHitBuffs：在伤害结算后施加；通常用于 DoT/减益/处置效果
- 共享技能示例：肾上腺素 → onCastBuffs=instant_heal_20pct（自身）

--------------------------------
10. 与现有战斗框架的集成（AutoCastEngine / SkillResolver / 事件）
--------------------------------
- AutoCastEngine（新增/改）：
  - 执行窗口：Pre-Attack / Post-Attack / Post-Cast
  - 窗口内依次执行：主动技能（GCD→非GCD）→ 触发技能
  - 产出技能队列（带 bundleId），交给 SkillResolver 执行
- SkillResolver：
  - 计算伤害/暴击/浮动，产生 BuffOperations/ResourceChanges/Heal
  - AoE/单体一致通过 targetPolicy/团队枚举分发
- MultiBattleInstance：
  - 统一应用伤害/治愈/资源/Buff，写入事件（SkillCastEvent、ResourceGainEvent、Buff*）
  - 生成 bundleId（同窗共享）
- Segment 事件：
  - 继续记录 SkillCastEvent / CastStart/Complete（可选）/ ResourceGain / BuffApply/Remove/Tick / Heal

--------------------------------
11. 数据结构（skills.json）与字段说明
--------------------------------
skills: SkillDef[]
- id, name
- type: "active" | "passive"
- slotType: "active" | "passive"
- fixed: bool（固定技能）
- releaseType: "instant" | "cast"
- castTimeSec?: number（>0 为施法）
- isGcd: bool（Cast 固定 true；Instant 可控）
- allowCoTriggerAfterCast?: bool（施法完成后是否允许追加瞬发）
- targetPolicy: "current_target" | "enemies_all" | "allies_lowest_hp_pct" | "self" | "allies_all"
- cooldownSec: number
- timeGcdMs?: number（预留，暂不启用）
- costs?: [{ bucketId, amount }]
- damage?: { coefAtk: number, flat: number, isAoe: bool }
- onCastBuffs?: [{ op, buffConfigId, targetOverride? }]
- onHitBuffs?: [{ op, buffConfigId, targetOverride? }]
- resourceGains?: [{ bucketId, amount }]
- conditions?: { hpBelowPct?, hpAbovePct?, requireBuffId?, forbidBuffId?, requireResource? }
- allowedProfessions?: string[]
- unlock?: { minLevel?: number, accountFlags?: string[], requiresProfessionLevel?: number }
- canCrit?: bool（默认 true）
- alwaysHits?: bool（默认 true）
- triggers?: TriggerDef[]（仅被动/装备需要）
TriggerDef：
- when, procChance, fireSkillId, priority, conditions?, overrides?

--------------------------------
12. 示例技能（战士/共享/法师）
--------------------------------
- 战士固定被动：warrior_attack_basic / warrior_special_pulse（脉冲赋 Buff）
- 战士固定主动：warrior_slam（10s CD, 1.2×Atk+20，+2 怒气，GCD）
- 战士可配置：
  - 致死打击：rage×3，CD 3s，1.5×Atk+30，GCD（≥3级）
  - 撕裂：rage×1，CD 10s，OnHit 施加 8s DoT（总伤 2×Atk），GCD（≥5级）
  - 雷霆一击：CD 10s，AoE 0.8×Atk+10，非GCD（≥7级）
- 战士被动触发：
  - 被动：普攻命中 20% → fire warrior_proc_strike（非GCD，0.5×Atk），priority=15
- 共享技能（账户解锁）：
  - 肾上腺素：CD 20s，hp<50%，即时治疗 20% MaxHP，isGcd=true；解锁条件=“warrior_level_10_unlocked”
- 法师施法技能（测试用）：
  - 炎爆术：cast 2.5s，CD 8s，mana×30，2.0×Atk+50，OnHit 施加 burning DoT，allowCoTriggerAfterCast=true

（详见 skills.example.json 示例）

--------------------------------
13. UI 交互与可视化
--------------------------------
- 技能设置组件（职业）：
  - 3 主动 + 1 被动栏，显示解锁、职业限制、条件
  - 战斗中不可修改
- 战斗 HUD：
  - 技能图标：CD 转圈、置灰（资源不足/未解锁/条件不符）
  - 施法条：显示技能名称、剩余施法时间；提示“施法中暂停普攻”
  - GCD 提示（开发模式）：被跳过原因（同窗已有GCD触发/资源不足/未就绪）
  - 触发技能高亮（可选）
- 可访问性：鼠标悬浮 tooltip 展示数值与效果摘要

--------------------------------
14. 事件与可观测性
--------------------------------
- SkillCastEvent：skillId、bundleId、casterId、targets、damage、isCrit、timeMs
- CastStartEvent/CastCompleteEvent（可选）
- ResourceGainEvent、BuffApply/Remove/Tick、HealEvent
- 诊断日志（开发模式）：
  - 窗口检查顺序、GCD 选择、被跳过原因（CD/资源/条件/解锁/职业不符）
  - 触发器命中与否、RNG 索引消费顺序

--------------------------------
15. 测试计划与验收标准
--------------------------------
单元测试：
- Window-GCD：同窗 2 个 isGcd 技能就绪，仅首个按优先序触发
- 瞬发共触发：3 个非GCD瞬发均满足，按槽位顺序全部触发
- 触发类：OnAttackHit 20% 在固定种子下命中序稳定；priority=15 先于 20
- 施法：Pre-Attack 起手暂停普攻；完成后进入 Post-Cast 并可追加瞬发
- 资源与CD：起手/完成扣费策略一致；资源不足/未就绪不触发且不进入 CD
- 目标选择：5 种策略命中正确（含 allies_lowest_hp_pct）
- 共享技能：账户解锁后跨职业可用；未解锁不可用
- Buff 联动：onCast / onHit 引用 buffConfigId 正确应用

集成测试：
- 战士循环：普攻后→“猛击(GCD)”优先→“雷霆（非GCD）”继续→撕裂 DoT 生效→触发类附加打击顺序正确
- 施法链：法师炎爆→完成后追加瞬发（若有），事件顺序与伤害统计正确
- 确定性：同 RNG 种子下事件序列完全一致
- 性能：1000 tick、3v5、多技能共触发场景 < 50ms

验收：
- 示例 skills.json 可加载并通过校验
- 所有新增测试通过；无回归
- HUD 显示与交互符合预期；施法条与 GCD 提示正确

--------------------------------
16. 性能与确定性
--------------------------------
- 同窗 bundle 聚合，事件写入顺序固定化
- RNG 消费顺序稳定（技能检查 → 触发器判定）
- 可选属性快照缓存（后续）：Buff 改变时重算属性，减少每次遍历成本
- 限流：combat.maxTriggeredPerWindow（默认5）；非GCD瞬发上限可配置

--------------------------------
17. 错误处理、边界与回退策略
--------------------------------
- 配置校验：字段必填/范围/引用存在性（buffConfigId、fireSkillId）
- 行为回退：目标缺失/无可用敌人 → 技能跳过并记录 reason
- 施法中断（预留）：被眩晕/沉默 → 打断施法、不进入 CD（或策略化）
- 错误日志：Console + Debug 双路输出，含上下文（caster/skill/target/window）

--------------------------------
18. 扩展预留（后续阶段）
--------------------------------
- Time-GCD（如 1.5s 全局 GCD）
- 手动释放/快捷键与输入缓冲
- 技能等级/成长/多段/引导/通道
- 复合成本（多资源/消耗 Buff 层数）
- 命中/闪避/格挡/抗性（与 AlwaysHits/canCrit 完整化）
- Charges/每目标冷却/连携/条件 DSL

--------------------------------
19. 伪代码（核心执行流程）
--------------------------------
PreAttackWindow():
  for skill in (fixed_cast → slot1_cast → slot2_cast → slot3_cast):
    if ReadyAndEligible(skill) and IsCast(skill):
      if TryConsumeCost(skill):
        StartCast(skill) // suspend AttackTrack
        return true
  return false

PostWindow(kind): // kind = Post-Attack or Post-Cast
  gcdFired = false
  // 主动技能：GCD 组
  for skill in (fixed_instant_gcd → slot1_gcd → slot2_gcd → slot3_gcd):
    if ReadyAndEligible(skill):
      if TryConsumeCost(skill):
        Fire(skill); gcdFired = true; break
  // 主动技能：非GCD 组
  for skill in (slot1_nonGcd → slot2_nonGcd → slot3_nonGcd):
    if ReadyAndEligible(skill):
      if TryConsumeCost(skill): Fire(skill)
  // 触发类技能
  procs = []
  for trigger in TriggersOfOwner():
    if trigger.when matches kind or OnAttackHit (for Post-Attack):
      if RNG() < trigger.procChance and TriggerConditionsOk(trigger):
        procs.add(trigger)
  procs.sortBy(priority asc, stable)
  fired = 0
  for t in procs:
    if fired >= maxTriggeredPerWindow: break
    skill = GetSkill(t.fireSkillId)
    if ReadyAndEligible(skill): // 通常非GCD
      Fire(skill); fired++

Fire(skill):
  result = SkillResolver.Cast(skill, ctx)
  ApplyDamage/Heal/Resource/Buff from result
  Record events with bundleId(transient-per-window)

ReadyAndEligible(skill):
  return CDReady(skill) && UnlockOk(skill) && ProfessionOk(skill) && ConditionsOk(skill) && TargetAvailable(skill)
