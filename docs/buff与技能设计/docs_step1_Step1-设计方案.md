# Step1 设计文档 — Buff / 资源 / Special 脉冲（整体方案）

版本：v1.0  
整理：copilot（基于讨论）  
日期：2025-11-11

---

## 一、目的与范围（高层）

目标
- 在战斗运行态（BattleContext / MultiBattleInstance）实现：
  - 资源桶（默认 rage，Max = 10，Clamp，不做溢出转换）
  - Buff / Debuff 通道（支持 Stat 增减、Multiplier、DoT、HoT、Instant Heal、Toggle/ForceCrit）
  - Special 脉冲（SpecialTrack）：支持两种增长策略 `presence` / `encounter`；脉冲可施加 buff/debuff/DoT/HoT
- 暴露怪物同等的 buff/debuff 接口（本步只实现接口与通路，怪物技能实现在技能阶段）
- 在战斗 UI 中为玩家与怪物面板展示 Buff/Debuff（图标、堆栈数、剩余时间、tooltip）
- 不改动持久化数据（CharacterData 不变），不实现资源溢出转换（保留接口，后续扩展）

不包含
- 装备/掉落/词条的复杂交互
- 完整施法条（CastingController 已预留接口，实际施法逻辑留到技能阶段）
- 资源溢出转换实现（仅保留接口）

---

## 二、设计原则

- 运行态优先：所有 resource / buff / DoT / HoT 都为战斗运行态数据（不持久化到 CharacterData）。
- 低侵入：SkillResolver 负责计算并返回变更指令；MultiBattleInstance 负责应用这些指令并写入 Segment。
- 可测试：新增事件（ResourceGainEvent、BuffApplyEvent、BuffTickEvent、BuffExpireEvent）便于回放与断言。
- 可扩展：为未来功能（溢出转换、驱散、优先级等）预留字段与 policy。

---

## 三、运行时模型（核心类型）

（以下类型均为运行态，在 BattleContext / MultiBattleInstance 中存在）

### 1) IBuffOwner（通用持有者接口）
- 属性
  - Id: string
  - IsPlayer: bool
  - CurrentHp, MaxHp: int
  - Buckets: ResourceBucketCollection
  - Buffs: Dictionary<string, BuffInstance>
- 行为
  - ApplyBuff(BuffInstance)
  - RemoveBuff(string buffId, string reason)
  - ReceiveDamage(int amount, DamageMeta meta)
  - ReceiveHeal(int amount, HealMeta meta)

### 2) ResourceBucket（资源桶）
- 字段
  - id: string（如 "rage"）
  - current: int
  - max: int（默认 10）
- 方法
  - int Gain(int amount, string reason) — 返回实际获得量（已 clamp）
  - bool TryConsume(int amount, string reason)
- 说明：暂不实现 overflow convert；接口包含 ConvertTarget 字段以便后续扩展

### 3) BuffInstance（Buff 运行态）
- 字段
  - id: string
  - ownerId: string
  - kind: "buff" | "debuff"
  - effects: List<Effect>
  - stackingPolicy: "Refresh" | "Stack" | "Ignore"
  - stacks: int
  - durationSec?: double（剩余秒数，可选）
  - tickIntervalSec?: double（DoT/HoT 周期）
  - tickAccum: double（内部累积计时）
- 方法
  - Apply(BattleContext ctx) — 将 Buff 附加到 owner
  - Tick(double dt, BattleContext ctx) — 触发 DoT/HoT 并递减 duration
  - IsExpired(): bool
  - OnExpire(BattleContext ctx)

### 4) Effect（效果类型）
- StatMultiplier { target, value }  // 乘法
- StatAdditive { target, value }    // 绝对或百分点加成
- ForceCrit                         // 下次技能强制暴击
- DamageOverTime { amountPerTick: int }
- HealOverTime { amountPerTick: int }
- InstantHeal { amount: int }
- StatReduction { target, value }   // 减益

### 5) SpecialTrack（Special 轨运行态）
- 字段
  - pulseIntervalSec (来源于 CharacterData.specialIntervalSec)
  - progressPolicy: Presence | Encounter
  - hastePassthrough: double [0..1]
  - fillProgress: double（fractional preserved）
  - onPulseActions: list of configured ops（apply buff to self / apply buff to enemies / inline ops）
- 方法
  - Tick(double dt, BattleContext ctx) — 累积并在满格时执行 onPulseActions

---

## 四、SkillResolver 与变更通路

职责分工
- SkillResolver.Cast(skillId, ctx, opts)
  - 计算伤害、暴击、浮动等
  - 返回 SkillCastResult（DamageDealt、IsCrit、可选 ResourceDeltas、BuffOps、BundleId、SkillId）
  - 不直接修改 Buckets/Buffs（只返回指令）
- MultiBattleInstance（调用方）
  - 调用 SkillResolver.Cast(...)
  - ApplyDamage(...)
  - Apply ResourceDeltas：调用 Bucket.Gain(...)
  - Apply BuffOps：创建 BuffInstance 并调用 Apply
  - 记录并写入 Segment 事件（SkillCastEvent、ResourceGainEvent、BuffApplyEvent 等）

SkillCastResult 的最小结构
- int DamageDealt
- bool IsCrit
- Dictionary<string,int> ResourceDeltas
- List<BuffOp> BuffOps (BuffOp { op: "apply"|"remove"|"refresh", buffId or inlineBuffDef })
- string SkillId, string BundleId

实现约定
- 普攻资源增益优先由 MultiBattleInstance 根据配置实现（attackHit +1, crit +1），SkillResolver 可以选返回 ResourceDeltas 但不是必需的。

---

## 五、Special 脉冲策略（presence / encounter）

模式说明
- presence：仅在 EncounterActive 且 EnemiesAlive 时增长（与 Attack 绑定）
- encounter：在遭遇全程增长（即便当前波已清空）

填充速率
- baseRate = 1 / max(pulseIntervalSec, minInterval)
- effectiveRate = baseRate * (1 + hastePassthrough * HastePercent)
- fillProgress += effectiveRate * dt
- 当 fillProgress >= 1 时触发一次脉冲（fillProgress -= 1），执行 onPulseActions

默认行为（Step1）
- 脉冲触发内联测试 Buff（可对自家或敌方施加 buff/debuff/DoT/HoT）
- 默认不消耗资源

---

## 六、Buff / Debuff / DoT / HoT / Heal 语义

应用语义
- Apply: MultiBattleInstance 为目标创建 BuffInstance 并加入 owner.Buffs
- StackingPolicy:
  - Refresh：如果已存在则重置持续时间（不堆叠）
  - Stack：增加 stacks（可能有 maxStacks）
  - Ignore：若存在则忽略
- Tick: MultiBattleInstance 在每帧循环中对所有 active Buff 调用 Tick(dt)：
  - 若为 DoT：在 tickInterval 到达时调用 owner.ReceiveDamage(amount)
  - 若为 HoT：调用 owner.ReceiveHeal(amount)
  - 记录 BuffTickEvent
- 过期：remainingDurationSec <= 0 时触发 OnExpire 与 BuffRemoveEvent

治疗
- InstantHeal 在 Apply 时立即生效（不超过 MaxHp），记录 HealEvent

减益
- StatReduction / StatMultiplier 在计算伤害/属性时被查询并作用。Step1 简化：在 ApplyDamage 时读取当前 BuffList 的临时加成并应用（不修改持久化属性）

---

## 七、怪物（Enemy）支持

- Enemy 实现 IBuffOwner（或通过包装在 BattleContext 提供同样接口）
- MultiBattleInstance 可以对怪物 ApplyBuff / RemoveBuff
- 本步只实现接口与通路；怪物技能在技能阶段实现（但可以在集成测试中对敌人手动注入 Buff 以验证）

---

## 八、事件 / Segment（schema）

新增或扩展事件（均包含 timeMs, actorId, 可选 bundleId）
- SkillCastEvent { timeMs, bundleId, skillId, casterId, targetIds, damage, isCrit }
- ResourceGainEvent { timeMs, bundleId, actorId, bucketId, delta, newValue, reason }
- BuffApplyEvent { timeMs, bundleId, ownerId, buffId, kind, durationSec, effectsSummary, sourceSkillId }
- BuffRemoveEvent { timeMs, ownerId, buffId, reason }
- BuffTickEvent { timeMs, ownerId, buffId, tickType: DoT|HoT, amount, resultingHp }
- HealEvent { timeMs, ownerId, amount, resultingHp, source }

兼容性
- 新事件为可选字段，既有消费者可忽略未知事件

---

## 九、UI 显示（MVP 要求）

位置与要素
- 玩家面板：头像右上/右侧一行图标
- 敌人面板：头像下方或侧边一行图标
图标元素
- 图标（由 buffId -> icon 映射）
- 堆栈数（若 stacks>1）
- 剩余时间（秒，向下取整）
- Tooltip：显示 名称 / 种类（Buff/Debuff）/ 简短效果 / 剩余时间 / 来源
视觉约定
- Buff（增益）：绿色/蓝色边框
- Debuff（减益）：红色边框
实现接口
- 前端订阅 Segment 流或每帧读取 BattleContext.Buffs 快照，更新 UI
- MVP 先实现静态图标与剩余秒数文本，动画可后续优化

---

## 十、配置示例（JSON）

资源全局默认
```json
{
  "resources": {
    "defaultRageMax": 10,
    "gainPerAttack": 1,
    "gainPerCritExtra": 1
  }
}
```

职业示例（warrior / mage）
```json
{
  "professions": {
    "warrior": {
      "special": {
        "progressPolicy": "presence",
        "pulseIntervalSec": 6.0,
        "hastePassthrough": 0.0,
        "targeting": "self",
        "pulseBuffInline": {
          "id": "test_warrior_special",
          "kind": "buff",
          "durationSec": 6.0,
          "stackingPolicy": "Refresh",
          "effects": [
            { "type": "StatMultiplier", "target": "DamagePerAttack", "value": 0.15 },
            { "type": "StatAdditivePercent", "target": "HastePercent", "value": 0.10 },
            { "type": "StatAdditiveAbsolute", "target": "CritChancePercent", "value": 0.05 }
          ]
        }
      }
    },
    "mage": {
      "special": {
        "progressPolicy": "encounter",
        "pulseIntervalSec": 5.0,
        "targeting": "allEnemies",
        "pulseBuffInline": {
          "id": "test_mage_dot",
          "kind": "debuff",
          "durationSec": 6.0,
          "stackingPolicy": "Refresh",
          "tickIntervalSec": 1.0,
          "effects": [
            { "type": "DamageOverTime", "amountPerTick": 6 }
          ]
        }
      }
    }
  }
}
```

---

## 十一、测试与验收标准

自动化测试（建议）
- 单元测试
  - ResourceBucket.Gain / TryConsume（Clamp）
  - BuffInstance.Tick（DoT/HoT 触发）与到期逻辑
  - ApplyBuff / RemoveBuff 与 stackingPolicy（Refresh）
- 集成测试
  - 单角色短战：连续攻击产生 rage（<=10），暴击 +2，记录 ResourceGainEvent
  - special_pulse（self）：触发 BuffApplyEvent 并使后续伤害计算体现 buff 效果
  - special_pulse（allEnemies）：所有敌人收到 DoT，产生 BuffTickEvent 与损伤
- Segment 验证
  - Sample battle 的 Segment 包含 ResourceGainEvent、BuffApplyEvent、BuffTickEvent、BuffRemoveEvent，且事件顺序正确
- 性能基线
  - 场景：1000 ticks，3 players x 5 enemies，resource/buff 处理 < 50ms（目标），并启用 maxTriggersPerTick 限制

验收准则
- 在未启用测试 buff 时，Step1 不改变 Step0 基线数值（避免回归）
- UI 显示 buff 图标并能显示剩余时间与 tooltip
- Enemy IBuffOwner 接口存在并示例测试通过

---

## 十二、实施任务（高层，供你拆分为具体步骤）

P0（必须）
1. 实现 ResourceBucket 运行态（默认 max=10）并覆盖单元测试
2. 在 MultiBattleInstance 将普攻结果映射为 Bucket.Gain（attackHit +1，crit +1）
3. 实现 BuffInstance 与 Effect 基础（包含 DoT/HoT/InstantHeal/StatMultiplier/StatAdditive）
4. 扩展 SkillCastResult 支持 BuffOps 与 ResourceDeltas；由 MultiBattleInstance 应用并写入 Segment
5. 实现 SpecialTrack 的 pulse 应用 inline 测试 Buff（self / allEnemies 两种 target）
6. 实现 Segment 的 ResourceGainEvent、BuffApplyEvent、BuffTickEvent、BuffRemoveEvent
7. 在前端新增最小 Buff 列表 UI（图标、剩余时间、tooltip）

P1（次要）
1. 将 Enemy 实现 IBuffOwner（接口与测试）
2. 实现配置的 targeting 行为并测试
3. 增加 Segment 导出工具（用于回放/分析）

Future（预留）
- overflow convert、dispel、复杂优先级、UI 动画等

---

## 十三、兼容性与迁移说明

- 所有改动均为运行态：战斗结束后不影响 CharacterData 存档
- Segment schema 向后兼容：消费者忽略未知事件
- 推荐：在 CombatConfig 与 professions 配置里加入 `configVersion` 字段，便于后续回溯

---

## 十四、需要你确认的决策点（实施前）
1. 资源上限默认 10（已和你确认）
2. 暴击时额外 +1 资源（已和你确认）
3. Special pulse 默认测试 buff 数值（15% 攻击、+10% 急速、+5% 暴击、持续 6s）是否接受用于验证？（可修改）

---

## 附录：快速 API 草案（概念）

- IResourceBucket, BuffInstance, BuffOp, SkillCastResult, Segment DTO 的最小接口/类（实现者可据此直接编码）

---

结束语  
本设计文档把 Step1 的整体方案（包含你提出的扩展需求：Debuff、DoT、HoT、怪物接口与 UI）整理为一份可执行规格。你可以根据此文档把工作拆解为具体的 issue / task。我可以继续把这些任务拆成可直接下发的 issue 模板与最小代码模版（C# 接口和前端 stub），或者根据你要的优先级生成详细实现步骤。请告诉我下一步你希望我做哪一项。