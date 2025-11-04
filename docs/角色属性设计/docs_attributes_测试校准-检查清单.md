# 测试与校准检查清单（第一步：属性生效）

功能性
- [ ] 升级后（L→L+1）触发派生计算，`CharacterData` 对应字段更新
- [ ] 切换职业时按新职业模板重算
- [ ] 首次加载时按职业/等级正确计算

数值趋势
- [ ] 同职业 L1→L10 的 DPS/HP 曲线单调、平滑
- [ ] 不同职业在同级基线表现符合风味：战士普攻强、法师技能强、游侠攻速/暴击高
- [ ] CritChance ≤ 60%，Haste ≤ 40%

表现验证
- [ ] 攻速：APS_effective ≈ baseAPS × (1 + Haste%)（或兼容模式下 AttackRateAPS 已含急速）
- [ ] 伤害：E_hit ≈ DamagePerAttack × [1 + Crit% × (2.0 - 1)]
- [ ] 技能：SpecialDamage 与 SpecialIntervalSec 生效（若暂未接入技能可跳过）

边界与回归
- [ ] Level=1、Level 极高下不溢出、不负数
- [ ] Variance 固定 5% 生效
- [ ] 复活时间保持模板值或期望值

调参建议
- 以“同级精英 10~12s 击杀”为 TTK 锚点倒推各职业参数
- 先调 baseDamage/baseAPS/MaxHp 定位白板基线，再微调 a~e 系数