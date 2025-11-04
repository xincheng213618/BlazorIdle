# 职业基线与权重（示例参数，MVP 起点）

说明
- 这是可用来“先跑起来”的占位数值，用于验证升级有效果与职业风味。
- 实际以目标 TTK 与怪物表校准为准，可在实现后按手感微调。

共同约定
- critBase = 5%（0.05）
- critCap = 60%（0.60）
- hasteCap = 40%（0.40）
- variance = 5%（0.05）
- reviveSec = 5.0（占位）

战士（warrior）
- 主属性：力量
- 主属性成长：base=10, perLevel=5
- 基线：
  - baseDamage=12, baseSpecialDamage=8, baseAPS=1.10, baseHp=200, baseSpecialIntervalSec=6.0
- 权重（a,b,c,d,e）：
  - a=1.2（普攻伤害/点）
  - b=0.6（技能伤害/点）
  - c=0.002（急速%/点，0.2%）
  - d=0.001（暴击%/点，0.1%）
  - e=12（生命/点）

法师（mage）
- 主属性：法强
- 主属性成长：base=12, perLevel=4
- 基线：
  - baseDamage=9, baseSpecialDamage=14, baseAPS=1.00, baseHp=160, baseSpecialIntervalSec=5.0
- 权重：
  - a=0.6, b=1.3, c=0.0015, d=0.0008, e=8

游侠（ranger）
- 主属性：敏捷
- 主属性成长：base=11, perLevel=4
- 基线：
  - baseDamage=10, baseSpecialDamage=10, baseAPS=1.20, baseHp=180, baseSpecialIntervalSec=4.5
- 权重：
  - a=0.9, b=0.7, c=0.003, d=0.0012, e=10

说明
- SpecialIntervalSec 在本步仅作“技能节奏基线”占位；下步（Buff/资源）会重定义为“Special 脉冲”。
- AttackRateAPS 与 HastePercent 的乘法关系在战斗引擎中生效；若暂未支持，可用“兼容模式”临时合并。