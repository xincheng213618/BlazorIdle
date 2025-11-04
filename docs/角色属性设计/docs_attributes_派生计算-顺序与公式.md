# 派生计算（第一步）— 顺序与公式

输入
- level：角色等级
- professionId：职业
- 职业模板（配置）：
  - baseline: baseDamage, baseSpecialDamage, baseAPS, baseHp, baseSpecialIntervalSec, critBase, variance, reviveSec
  - weights: a,b,c,d,e 系数与 caps（详见“职业基线与权重 · 示例”）
  - mainStat: base, perLevel
- 装备主属性加成（预留，MVP 可为 0）

步骤
1) 计算主属性
   - MainStatTotal = mainStat.base + mainStat.perLevel × (level - 1) + equipMainStat
2) 线性派生（MVP）
   - DamagePerAttack = baseDamage + a × MainStatTotal
   - SpecialDamage = baseSpecialDamage + b × MainStatTotal
   - HastePercent = clamp(c × MainStatTotal, 0, hasteCap)
   - CritChancePercent = clamp(critBase + d × MainStatTotal, 0, critCap=0.60)
   - MaxHp = baseHp + e × MainStatTotal
3) 其它字段
   - AttackRateAPS = baseAPS
     - 若当前引擎未对 APS 乘以 Haste，可用“兼容模式”：AttackRateAPS *= (1 + HastePercent)，并暂将 HastePercent 置 0
   - SpecialIntervalSec = baseSpecialIntervalSec（本步不吃常规急速）
   - CritMultiplier = 2.0
   - VariancePct = variance（建议 0.05）
   - ReviveSec = reviveSec（MVP 可不变）
4) 写回 `CharacterData`

期望计算（用于校准）
- 单击期望伤害：E_hit = DamagePerAttack × [1 + CritChancePercent × (CritMultiplier - 1)]
- 生效攻速：APS_effective = AttackRateAPS × (1 + HastePercent)
- 普攻DPS：DPS_basic = APS_effective × E_hit
- 技能DPS：DPS_skill = SpecialDamage / SpecialIntervalSec（若暂不启用技能，可忽略）
- 总DPS：DPS_total = DPS_basic + DPS_skill

上下限/下限
- CritChancePercent ≤ 60%
- HastePercent ≤ 40%（建议初值）
- GCD/技能间隔下限占位：0.5s（战斗步再落实）

触发与幂等
- 在加载/升级/切职业时重复上述过程，结果覆盖写入；幂等、可重复调用