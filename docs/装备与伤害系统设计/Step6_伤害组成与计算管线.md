# Step6 伤害组成与计算管线（与装备词条/元素/技能系统联动）详细设计

版本：v1.2  
日期：2025-11-29  
作者：@copilot  
本次更新：与 v1.1 相比，明确双攻击系（Attack% / SpecialAttack%）、新增 HP% 词条（不入伤害乘区，仅影响最大生命）、暴击基础倍率 1.2 + 额外暴击伤害加成%，删去旧“技能专精”描述，统一术语（CritDamageBonusPercent、KenChasePercent、ChasePercent）。确认盛体 / 背水互斥区间无需额外选择逻辑。

--------------------------------
更新摘要（v1.2 必要变更）
--------------------------------
1. 主体加成层：现在包含 (1 + Attack%) × (1 + SpecialAttack%) × (1 + Stance%)。  
2. 词条“mastery / skillDamage%”相关描述删除；所有伤害统一视为技能，不区分普通攻击与技能输出乘区。  
3. 暴击层：基础暴击倍率 baseMultiplier=1.2；终结词条提供 CritDamageBonusPercent，乘区变为 (1 + CritDamageBonus%/100)。最大合计 50% → 最终暴击倍率 ≤ 1.2 × 1.5 = 1.8。  
4. HPPercent 不进入伤害乘区，只用于最大生命值计算与生存系统。  
5. 追击层术语统一：ChasePercent、KenChasePercent（克制条件成立时）、ChaseFlat。  
6. 删除元素加成重复；克制追击仅在元素倍率=优势(1.5)时生效。  
7. 明确盛体 / 背水互斥触发区间：≥75% 与 ≤50% 不重叠。  

--------------------------------
目录
--------------------------------
1. 目标与原则  
2. 核心字段与数据来源  
3. 上限（Caps）引用  
4. 伤害管线层级顺序（更新）  
5. 各层详细公式  
6. 盛体与背水态势映射（互斥）  
7. 元素与克制追击条件  
8. 完整公式汇总（更新版）  
9. 示例计算（更新版）  
10. 配置结构示例  
11. 伪代码（更新版）  
12. DoT / HoT / AOE 适配建议  
13. 测试矩阵（更新版）  
14. 平衡杠杆与调参点  
15. 风险与缓解策略  
16. 未来扩展预留  
17. 实施与集成步骤  
18. 总结

--------------------------------
1. 目标与原则
--------------------------------
- 多来源伤害加成统一到简洁乘区，减少爆炸可能。
- 词条与强化变化“所见即所得” → 所有百分比先裁剪再入管线。
- 乘区限制：主体 / 暴击 / 元素；追击在尾部加法。
- 盛体 / 背水互斥；元素克制与克制追击分层避免重复。

--------------------------------
2. 核心字段与数据来源
--------------------------------
角色：
- AttackFinal：基础攻击（等级/职业/装备基础）已经含 AttackFlat 等基础面板值。
- AttackPercent（词条汇总后裁剪）
- SpecialAttackPercent（词条汇总后裁剪）
- HPPercent（仅面板最大生命提升）
- CritChancePercent、CritDamageBonusPercent
- FortifyMaxPct、BackwaterMaxPct（态势上限）
- ChasePercent、KenChasePercent、ChaseFlat
- DamageReductionPercent
- Armor

技能：
- SkillCoef、SkillFlat、VarianceMin/VarianceMax、element（可选）

元素：
- 攻击方元素（技能元素优先，无则主手）
- 防守方元素（怪物配置）

--------------------------------
3. 上限（Caps）引用
--------------------------------
与 Step5 文档一致：
AttackPct 100 / SpecialAttackPct 80 / HPPercent 100 / CritChancePct 80 / CritDamageBonusPct 50 / FortifyMaxPct 20 / BackwaterMaxPct 20 / ChasePct 30 / KenChasePct 20 / DamageReductionPct 90 / ChaseFlatCap 按阶段定。

--------------------------------
4. 伤害管线层级顺序（更新）
--------------------------------
1. 基础层（Base）  
2. 主体加成层（Attack% / SpecialAttack% / Stance%）  
3. 暴击层（Crit）  
4. 元素层（ElementMult）  
5. 追击层（Chase / KenChase / FlatChase）  
6. 减伤层（ArmorCurve / DamageReduction%）  
7. 收尾层（封底、记录）

--------------------------------
5. 各层详细公式
--------------------------------
BaseComponent = AttackFinal × SkillCoef + SkillFlat  
AfterVariance = BaseComponent × Variance

主体加成层（裁剪后）：
AfterMain = AfterVariance × (1 + AttackPct/100) × (1 + SpecialAttackPct/100) × (1 + StancePct/100)

暴击层：
AfterCrit = AfterMain × ( isCrit ? (1 + CritDamageBonusPct/100) × baseCritMultiplier : 1 )

元素层：
AfterElement = AfterCrit × ElementMult  (1.5 / 1.0 / 0.75)

追击层：
AfterChase = AfterElement
           + AfterElement × (ChasePct/100)
           + AfterElement × (KenChasePct/100, 若克制成立)
           + ChaseFlat

减伤层：
AfterDefense = AfterChase × (1 - ArmorCurve(Armor)) × (1 - DamageReductionPct/100)

收尾：
FinalDamage = max(1, round(AfterDefense))

--------------------------------
6. 盛体与背水态势映射（互斥）
--------------------------------
Fortify：
- 条件：HPRatio ≥ 0.75
- FortifyRaw = (HPRatio - 0.75)/(0.25) ∈ [0,1]
- StanceFortifyPct = FortifyRaw × FortifyMaxPct

Backwater：
- 条件：HPRatio ≤ 0.50
- BackwaterRaw = (0.50 - HPRatio)/(0.49) ∈ [0,1]
- StanceBackwaterPct = BackwaterRaw × BackwaterMaxPct

StancePct = StanceFortifyPct + StanceBackwaterPct（互斥区间 → 实际只会有一个非零）

--------------------------------
7. 元素与克制追击条件
--------------------------------
- ElementMult = 1.5（克制前向） / 0.75（被克制） / 1.0（其他或 Neutral）。
- KenChasePct 仅在 ElementMult=1.5 时启用；不在元素层重复加成。
- element_resonance 提供 Attack% + KenChase%（仍受总 KenChase 上限裁剪）。

--------------------------------
8. 完整公式汇总（更新版）
--------------------------------
Let:
A = AttackFinal; SC = SkillCoef; SF = SkillFlat; V = Variance  
Atk% = AttackPct; Sp% = SpecialAttackPct; St% = StancePct  
CritDmgBonus% = CritDamageBonusPct; CritMultBase = 1.2  
ElemM = ElementMult  
Ch% = ChasePct; KenCh% = KenChasePct; ChFlat = ChaseFlat  
AR = ArmorCurve(Armor); DR% = DamageReductionPct  

Base = A × SC + SF  
Var = Base × V  
Main = Var × (1+Atk%/100) × (1+Sp%/100) × (1+St%/100)  
Crit = Main × (isCrit ? CritMultBase × (1+CritDmgBonus%/100) : 1)  
Elem = Crit × ElemM  
Chase = Elem + Elem×(Ch%/100) + Elem×(KenCh%/100) + ChFlat  
Defense = Chase × (1-AR) × (1-DR%/100)  
Final = round( max(1, Defense) )

--------------------------------
9. 示例计算（更新版）
--------------------------------
示例输入：
- A=1000, SC=1.2, SF=80, V=1.03
- Atk%=80, Sp%=40, St%（高血）=10
- CritChance=25% 命中；CritDamageBonus%=40
- ElemM=1.5（克制）
- Ch%=15, KenCh%=10, ChFlat=50
- AR=0.10, DR%=5

过程：
Base = 1000×1.2 + 80 = 1280  
Var ≈ 1280×1.03 = 1318.4  
Main = 1318.4 ×1.8 ×1.4 ×1.1 ≈ 3651.0  
Crit = 3651.0 × (1.2 × (1+0.40)) = 3651.0 × 1.68 ≈ 6137.7  
Elem = 6137.7 ×1.5 ≈ 9206.6  
Chase = 9206.6 + 9206.6×0.15 + 9206.6×0.10 + 50 ≈ 9206.6 + 1380.99 + 920.66 + 50 = 11558.25  
Defense = 11558.25 ×0.9 ×0.95 ≈ 9899.3  
FinalDamage ≈ 9899

--------------------------------
10. 配置结构示例
--------------------------------
```json
{
  "combat": {
    "variance": { "defaultMin": 0.95, "defaultMax": 1.05 },
    "crit": { "baseMultiplier": 1.2, "critDamageBonusCap": 50.0 },
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
    "element": { "advantageMultiplier": 1.5, "reverseMultiplier": 0.75, "defaultMultiplier": 1.0 },
    "armorCurve": { "k": 50.0 }
  }
}
```

--------------------------------
11. 伪代码（更新版）
--------------------------------
```csharp
double CalcDamage(HitCtx ctx) {
    double baseComp = ctx.AttackFinal * ctx.Skill.Coef + ctx.Skill.Flat;
    double variance = RngBetween(ctx.Skill.VarMin ?? 0.95, ctx.Skill.VarMax ?? 1.05);
    double afterVariance = baseComp * variance;

    double atkPct = Clamp(ctx.Stats.AttackPct, 0, Caps.AttackPct);
    double spPct  = Clamp(ctx.Stats.SpecialAttackPct, 0, Caps.SpecialAttackPct);
    double stancePct = CalcStancePct(ctx.Attacker.HPRatio, Caps.FortifyMaxPct, Caps.BackwaterMaxPct);

    double afterMain = afterVariance * (1 + atkPct/100) * (1 + spPct/100) * (1 + stancePct/100);

    double critChance = Clamp(ctx.Stats.CritChancePct, 0, Caps.CritChancePct);
    bool crit = RngRoll(critChance);
    double critBonusPct = Clamp(ctx.Stats.CritDamageBonusPct, 0, Caps.CritDamageBonusPct);
    double afterCrit = afterMain * (crit ? CritBaseMult * (1 + critBonusPct/100) : 1);

    double elemMult = ResolveElementMultiplier(ctx);
    double afterElem = afterCrit * elemMult;

    double chasePct = Clamp(ctx.Stats.ChasePct, 0, Caps.ChasePct);
    double kenChasePct = (elemMult > 1.0) ? Clamp(ctx.Stats.KenChasePct, 0, Caps.KenChasePct) : 0;
    double chaseFlat = Clamp(ctx.Stats.ChaseFlat, 0, Caps.ChaseFlatCap);
    double afterChase = afterElem + afterElem * (chasePct/100) + afterElem * (kenChasePct/100) + chaseFlat;

    double armorReduce = ArmorCurve(ctx.Defender.Armor, ArmorCurveK);
    double drPct = Clamp(ctx.Defender.DamageReductionPct + ctx.Buffs.MitigationPct, 0, Caps.DamageReductionPct);

    double afterDefense = afterChase * (1 - armorReduce) * (1 - drPct/100);
    double final = Math.Max(1, Math.Round(afterDefense));
    EmitCombatEvent(ctx, final);
    return final;
}
```

--------------------------------
12. DoT / HoT / AOE 适配建议
--------------------------------
- DoT：快照 Attack% / SpecialAttack% / Crit（可选） / ElementMult；追击与克制追击仅在初次应用或全部禁用（避免多层放大）。
- HoT：不使用元素与追击；可选暴击（低频）。
- AOE：共享前四层结果，对每目标单独应用减伤层。

--------------------------------
13. 测试矩阵（更新版）
--------------------------------
单元：
- AttackPct / SpecialAttackPct / CritChancePct / CritDamageBonusPct Caps 裁剪
- 盛体 / 背水区间边界 (0.50,0.75)
- 元素克制与被克制乘区
- KenChasePct 仅在 elemMult=1.5 时生效
- 固定追击受减伤
- 暴击期望值统计（大量样本）
集成：
- 构筑差异：全 Attack% vs Attack%+SpecialAttack%
- 追击与克制占比采样
- 替换装备强化回收后重新计算一致性
- 高血/低血场景伤害对比

--------------------------------
14. 平衡杠杆与调参点
--------------------------------
- 攻击双系比值（Attack% vs SpecialAttack%）调节构筑偏好
- CritDamageBonusCap 控制暴击上限
- ChasePct / KenChasePct equipLimit 控制尾部加成
- StanceMaxPct 调节高血 / 低血差异
- ArmorCurve k 调整怪物防御收益

--------------------------------
15. 风险与缓解策略
--------------------------------
| 风险 | 缓解 |
|------|------|
| 双攻击系同时堆满输出过高 | 降低 SpecialAttackPct 上限或调整其增长曲线 |
| 追击尾部占比过大 | 下调 chasePct Lv4 或 equipLimit |
| 暴击强度过度稳定 | 引入暴击溢出转化机制（未来） |
| 高血/低血利用率失衡 | 调整阈值或 MaxPct，或添加轻度衰减曲线 |

--------------------------------
16. 未来扩展预留
--------------------------------
- 真伤追击（不受减伤层）
- 穿透（ArmorPenetrationPercent）前置于减伤层
- 暴击期望溢出机制（精准伤害）
- Stance 过渡带（70%~55% 区间渐变）
- 元素耐性与穿透

--------------------------------
17. 实施与集成步骤
--------------------------------
Phase C1：配置与数据字段校验  
Phase C2：服务层改写（双攻击系、暴击基础倍率）  
Phase C3：词条与强化接入（与 Step5 v2.1 一致）  
Phase C4：测试与采样脚本执行  
Phase C5：平衡调参与日志可视化

--------------------------------
18. 总结
--------------------------------
v1.2 伤害管线已与最新词条系统 (Step5 v2.1) 对齐：双攻击系、基础暴击倍率、态势互斥、追击尾部加法、克制追击条件化、HP% 不直接增伤。结构紧凑、易控、可扩展，满足初期放置游戏数值稳定与构筑差异化需要。  
后续若需自动化采样 / 回归脚本或性能基准，请继续指示。  