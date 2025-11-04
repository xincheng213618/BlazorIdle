# 角色属性系统（第一步）总览 — MVP

目标
- 让“升级”带来真实数值提升（不再只有等级数字变化）。
- 不改现有数据结构，仅通过配置与计算口径把“主属性→次级→现有字段”落到 `CharacterData`。
- 采用方案B：每职业仅一个“主属性”，由该主属性派生少量次级属性（Power/Haste/Crit/HP）。

范围（本步要实现）
- 职业基线与主属性成长（level→MainStat）
- 主属性到次级属性的线性派生（MVP 简化）
- 将派生结果写入现有字段以被战斗核使用
- 上下限口径（暴击上限、急速上限/GCD 下限占位）
- 触发时机：加载、升级、切换职业时重算

暂不包含（下一步或以后）
- Buff/资源/叠层：`SpecialIntervalSec` 改为“Special 脉冲 + 层数/资源”（下一步 Buff）
- 场景压制/越级惩罚：做成“场景 Buff”（Buff 步实现）
- 命中/偏斜/闪避：不在 MVP
- Rating→% 曲线：MVP 先用线性，后续（V2）再切曲线
- 技能自动施放、资源消耗：在“技能”步实现
- 战斗确定性仿真/Segment/Digest：在“战斗”步实现

玩家可见属性（建议）
- 主属性（MainStat）：随职业命名（力量/法强/敏捷）
- 次级属性（展示值）：攻击强度（Power，以实际伤害变化体现）、急速（Haste%）、暴击率（Crit%）、生命（HP）
- 固定/默认：暴击倍数=2.0、伤害浮动=5%

与现有字段的映射
- DamagePerAttack ← 主属性线性加成
- AttackRateAPS ← 职业基线；实际生效攻速=APS×(1+Haste%)（若引擎暂未乘以急速，可临时在计算阶段把 APS 直接乘上 Haste）
- HastePercent ← 主属性线性加成（封顶）
- CritChancePercent ← 主属性线性加成+基线（封顶）
- CritMultiplier ← 固定 2.0
- SpecialIntervalSec ← 职业“技能节奏基线”（本步不吃常规急速）
- SpecialDamage ← 主属性线性加成（法系高）
- VariancePct ← 固定 0.05（职业可微调）
- MaxHp ← 主属性线性加成
- ReviveSec ← 保持模板值或略减，MVP 可不动

实施触点
- OnCharacterLoaded（角色加载）
- OnLevelUp（升级）
- OnActiveProfessionChanged（切换职业）
以上三个时机均调用“派生计算”并写回 `CharacterData`。

配套文档
- 派生计算 · 顺序与公式（见本目录）
- 职业基线与权重 · 示例（见本目录）
- 配置示例 professions.example.json（见本目录/config）
- 测试校准检查清单（见本目录）