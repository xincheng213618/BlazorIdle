# 第0步设计方案（整合版）— Track 抽象与 SkillCast 统一入口

目的
- 在不改变现有玩法与数值表现的前提下，为后续“充能条→释放技能、施法条、Buff/资源/Special 脉冲、Special 双模式、普攻+瞬发同发”等能力预留最小抽象与接口。
- 将“Attack/Special 的触发时机（轨道）”与“技能效果处理”解耦：现在继续沿用旧频率与效果，但所有触发统一走 SkillCast 管道。
- 引入 Track 抽象（Legacy 适配器），为后续切换到 ChargeTrack（进度条满→施放技能）提供平滑迁移。

范围（本步生效）
- 保持当前 Attack 频率、Special 触发节奏与数值不变（与改造前等价）。
- 新增统一的 SkillCast 接口；现有普攻/特殊触发改为调用 SkillCast（或 CastBundle）。
- 新增 Track 抽象与 Legacy 实现（AttackTrackLegacy / SpecialTrackLegacy），仅负责“何时触发”；“如何结算”由 SkillCast 负责。
- 预留 Casting（施法）与 Track 暂停（suspend/resume）接口，但不启用施法流程。
- 预留 Special 轨“双模式”配置项（presence/encounter），默认走与当前一致的 presence 行为，不强制启用新模式。

不在本步
- 不切换 ChargeTrack 充能实现（后续阶段开关）。
- 不引入新 Buff/资源逻辑（下一步接入）；Special 的占位效果保持现状。
- 不启用施法条（castTime>0），仅预留字段与事件类型。
- 不改存档结构与数据接口。

一、统一入口：SkillCast 与成组施放（CastBundle）
- 目标：所有“普攻/脉冲/将来的技能释放”走同一条处理链，负责伤害、资源、Buff（预留）、事件记录。
- 支持“同一时刻多个技能同发”（普攻+瞬发技能）：通过 CastBundle 顺序施放并共享 bundleId。
- 暴击与急速的口径不变：
  - E_hit = DamagePerAttack × [1 + Crit% × (CritMultiplier − 1)]
  - APS_effective = AttackRateAPS × (1 + HastePercent)

接口草案（最小）
```csharp
public sealed class SkillCastOptions {
    public bool ForceCrit { get; set; } = false;     // 预留：必暴（由后续 Buff/脉冲触发）
    public string SourceTrack { get; set; } = "";    // "attack" | "special" | "manual"
    public string? BundleId { get; set; }            // 由 CastBundle 统一下发
}

public sealed class SkillCastResult {
    public int DamageDealt { get; set; }
    public bool IsCrit { get; set; }
    public Dictionary<string,int> ResourceChanges { get; set; } = new();
    public List<string> BuffChanges { get; set; } = new(); // 预留
}

public interface ISkillResolver {
    SkillCastResult Cast(string skillId, BattleContext ctx, SkillCastOptions? opts = null);

    // 新增：成组施放（普攻+瞬发）
    IReadOnlyList<SkillCastResult> CastBundle(IReadOnlyList<string> skillIds, BattleContext ctx, SkillCastOptions opts);
}
```

CastBundle 规则（第0步默认）
- 顺序：主技能（索引0）→跟随技能（按索引依次）。
- 校验：主技能必施放（普攻 cost=0），随技能独立检查（资源/冷却不满足则跳过，不影响主技能）。
- 可见性：后发技能可见前面技能带来的资源/状态变化（例如主技能命中产怒气，随技能可立即消耗）。
- 防递归：一次 CastBundle 内不允许再次触发“同类型 on-attack 同发”以防环。
- 上限：每 Tick 至多 N 次施放（含主+随），默认 20。

二、Track 抽象与 Legacy 适配
- ITrack 只负责“何时触发”；触发时调用 SkillResolver（Cast 或 CastBundle）。
- 本步提供两个 Legacy 实现（不改变数值）：
  - AttackTrackLegacy：按 APS_effective 定时触发；到点时 CastBundle(["attack_basic", ...onAttackInstantSkills]).
  - SpecialTrackLegacy：按 SpecialIntervalSec 定时触发；到点时 CastBundle(["special_pulse"] + onSpecialInstantSkills).

接口草案
```csharp
public interface ITrack {
    string Id { get; }                   // "attack" | "special"
    bool IsSuspended { get; }            // 预留：施法/控制暂停
    void Suspend(string reason);         // 预留
    void Resume(string reason);          // 预留
    void Tick(double dt, BattleContext ctx);
}
```

三、Special 轨“双模式”预留（默认不变）
- progressPolicy
  - "presence"：仅“有敌人时”增长（与 Attack 一致，默认等同当前实现）
  - "encounter"：在遭遇期间持续增长（即使无敌或 Attack 暂停），用于部分职业风格
- 第0步仅预留配置判断与门控函数，默认保持 presence 行为；启动“encounter”将作为后续开关。

门控伪码（占位）
```pseudo
ShouldAdvance_Special(ctx) =
  if ctx.Config.Special.ProgressPolicy == "presence":
     return EncounterActive && EnemiesAlive && !special.IsSuspended
  else:
     return EncounterActive && !special.IsSuspended
```

四、施法占位（CastingController）
- 预留但不启用：支持未来“施法条暂停 Attack、可选暂停 Special”的设计。
- 字段：IsCasting、ActiveCast{skillId, castTimeSec, pauseAttackTrack, pauseSpecialTrack}。
- Tick 顺序（占位，不改变现有执行流）：
  - casting.Tick(dt) → 根据 IsCasting 决定是否让某轨道 Suspend/Resume
  - attackTrack.Tick(dt) → specialTrack.Tick(dt) → Segment.Append()

五、Segment 事件与审计
- 事件：SkillCastEvent（每条技能一条记录），包含 {bundleId, skillId, sourceTrack, timestamp, damage, isCrit, resourceDelta, buffDelta}。
- 预留：CastStart / CastComplete 事件类型（施法启用后使用）。
- 第0步可通过调试开关 emit_cast_events 控制是否输出详细事件日志。

六、配置与开关（占位，默认不改变行为）
```json
{
  "combat": {
    "use_charge_tracks": false,      // 后续切换为 ChargeTrack 时启用
    "emit_cast_events": true,        // 第0步可开：便于观测
    "max_triggers_per_tick": 20      // 防极端
  },
  "tracks": {
    "attack": {
      "boundSkills": ["attack_basic"],
      "onFireTriggers": []           // 将来填入瞬发技能
    },
    "special": {
      "progressPolicy": "presence",  // 或 "encounter"（后续再启用）
      "boundSkills": ["special_pulse"],
      "onFireTriggers": []
    }
  },
  "skills": {
    "attack_basic":   { "castTimeSec": 0 },
    "special_pulse":  { "castTimeSec": 0 }
  }
}
```

七、伪代码（整合示例）
```pseudo
tick(dt, now):
  casting.Tick(dt) // 第0步为空，不改变逻辑

  // Attack legacy timing
  attackAccum += APS_effective * dt
  while attackAccum >= 1 and fires < maxPerTick:
    attackAccum -= 1
    skills = ["attack_basic"] + resolveOnAttackInstantSkills(ctx)
    resolver.CastBundle(skills, ctx, { sourceTrack: "attack" })
    fires++

  // Special legacy timing + 双模式门控（presence 默认与现状一致）
  if ShouldAdvance_Special(ctx):
    specialAccum += dt / max(SpecialIntervalSec, 1e-4)
    while specialAccum >= 1 and fires < maxPerTick:
      specialAccum -= 1
      skills = ["special_pulse"] + resolveOnSpecialInstantSkills(ctx)
      resolver.CastBundle(skills, ctx, { sourceTrack: "special" })
      fires++

  segment.FlushIfNeeded()
```

八、字段与数据（不改存档）
- 仍使用：AttackRateAPS, DamagePerAttack, HastePercent, CritChancePercent, CritMultiplier, SpecialIntervalSec, SpecialDamage（占位）, VariancePct, MaxHp, ReviveSec。
- 运行态新增（内存内）：BattleContext（Tracks、SkillResolver、CastingController 占位）、Segment 聚合器。

九、验收标准
- 功能与数值（A/B 对比改造前后，允差＜1%）
  - 普攻触发频率、总伤害、暴击分布、DPS 基本一致
  - Special 触发次数与节奏一致
- 记录/可观测性
  - Segment 中出现 “attack_basic” 与 “special_pulse” 的 SkillCastEvent（携带 bundleId）
- 性能
  - 1000 tick 内轨道触发 + SkillCast 处理 < 50ms（参考）
  - max_triggers_per_tick 生效，无死循环
- 回滚
  - 通过函数级 flag 或配置开关快速回退到旧触发路径（若需要）

十、实现清单（任务拆分）
- 新建 ISkillResolver + 实现（支持 Cast / CastBundle；接入 attack_basic / special_pulse）
- 新建 ITrack 接口 + AttackTrackLegacy / SpecialTrackLegacy（从旧逻辑抽出触发调用）
- 新建 CastingController 与 ActiveCast 占位（仅状态字段与空 Tick）
- 改造：普攻与 Special 的触发改为调用 SkillResolver（由单 Cast 升级为 CastBundle）
- 预留：Special 双模式门控函数（默认 presence，与现状等价）
- 新建：SkillCastEvent 记录到 Segment（日志可控）
- 配置：combat.emit_cast_events / use_charge_tracks / max_triggers_per_tick / tracks.* 占位项

十一、后续衔接（展望）
- 阶段1（Buff/资源/Special 脉冲）：
  - 在 attack_basic/special_pulse 的技能效果中添加“产资源/叠层/一次性必暴开关”等
  - 开放 Special.progressPolicy="encounter" 给特定职业
- 阶段2（技能/AutoCast）：
  - 扩展 SkillDef（cost/cooldown/effects），AutoCast 使用 SkillResolver；施法 castTime>0 走 CastingController
- 阶段3（ChargeTrack 切换）：
  - 用 ChargeTrack 替换 Legacy Track 的 Tick 实现；其他保持不变（靠开关 use_charge_tracks 控制）