# Buff与技能设计 - Step 1 详细实施方案文档

## 📋 文档概述

**文档版本：** v1.0  
**创建日期：** 2025-11-11  
**目标阶段：** Step 1 - Buff / 资源 / Special 脉冲系统  
**预计工作量：** 40-50 小时（5-7 个工作日）

---

## 🎯 一、Step 1 总体目标

### 1.1 核心目标

基于 Step 0 已完成的统一技能施放架构，Step 1 将实现战斗系统的核心增强功能：

1. **资源系统**
   - 实现 rage 资源桶（默认上限 10）
   - 玩家攻击命中 +1 rage，暴击额外 +1 rage
   - 支持资源的获取、消耗和上限控制
   - 预留资源溢出转换接口（本步不实现）

2. **Buff/Debuff 系统**
   - 支持多种效果类型：StatMultiplier（乘法加成）、StatAdditive（加法加成）、ForceCrit（强制暴击）、DamageOverTime（持续伤害）、HealOverTime（持续治疗）、InstantHeal（瞬间治疗）
   - 堆叠策略：Refresh（刷新）、Stack（堆叠）、Ignore（忽略）
   - 完整的生命周期管理：施加、Tick、过期、移除
   - 玩家和怪物都能拥有 Buff/Debuff

3. **Special 脉冲增强**
   - 支持两种进度增长策略：presence（仅有敌人时增长）、encounter（遭遇期间持续增长）
   - 脉冲触发时可施加 Buff/Debuff
   - 支持两种目标模式：self（自身）、allEnemies（所有敌人）
   - 测试配置：Warrior（自身增益）、Mage（敌人 DoT）

4. **事件系统扩展**
   - 新增 ResourceGainEvent（资源获得）
   - 新增 BuffApplyEvent（Buff 施加）
   - 新增 BuffRemoveEvent（Buff 移除）
   - 新增 BuffTickEvent（Buff Tick）
   - 新增 HealEvent（治疗）

5. **UI 展示**
   - 玩家头像旁显示 Buff 图标列表
   - 敌人头像下显示 Debuff 图标列表
   - 显示堆栈数、剩余时间
   - 鼠标悬停显示 Tooltip（名称、效果、来源）
   - 视觉区分：Buff 绿色/蓝色边框，Debuff 红色边框

### 1.2 设计原则

- ✅ **运行态优先**：所有 Buff/资源数据为战斗运行态，不持久化到 CharacterData
- ✅ **低侵入修改**：SkillResolver 负责计算并返回变更指令，MultiBattleInstance 负责应用变更
- ✅ **高可测试性**：完整的事件系统支持回放和自动化验证
- ✅ **面向扩展**：为未来功能预留接口（溢出转换、驱散、优先级等）
- ✅ **保持兼容**：不破坏 Step 0 的已有功能，所有 104 个测试继续通过

### 1.3 不包含的功能

以下功能在 Step 1 **不实现**，仅预留接口：

- ❌ 资源溢出转换的实际逻辑
- ❌ Buff 驱散（Dispel）机制
- ❌ Buff 优先级与互斥规则
- ❌ 复杂的目标选择策略（random、lowest_hp 等）
- ❌ 技能系统（skill cast、cooldown、cost 等，留到后续步骤）
- ❌ 数据持久化（CharacterData 不变）

---

## 📦 二、实施阶段详细规划

### 阶段 1：资源系统基础实现（P0 - 必须）

#### 目标
实现 ResourceBucket 运行态，支持 rage 资源的获取、消耗和上限控制。

#### 任务清单

**1.1 创建资源系统目录结构**
```
BlazorIdle.Shared/Game/Resources/
├── ResourceBucket.cs
├── ResourceBucketCollection.cs
└── ResourceConfig.cs
```

**1.2 实现 ResourceBucket.cs**

核心类，管理单个资源桶的状态：

```csharp
namespace BlazorIdle.Game.Resources;

/// <summary>
/// 资源桶，管理单一类型资源（如 rage）
/// </summary>
public class ResourceBucket
{
    /// <summary>
    /// 资源 ID（如 "rage"）
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// 当前资源值
    /// </summary>
    public int Current { get; private set; }
    
    /// <summary>
    /// 资源上限
    /// </summary>
    public int Max { get; private set; }
    
    /// <summary>
    /// 预留：溢出转换目标资源 ID（Step 1 不实现）
    /// </summary>
    public string? ConvertTarget { get; set; }
    
    /// <summary>
    /// 预留：溢出转换比例（Step 1 不实现）
    /// </summary>
    public double ConvertRatio { get; set; } = 0.0;

    public ResourceBucket(string id, int max = 10, int initial = 0)
    {
        Id = id;
        Max = max;
        Current = Math.Clamp(initial, 0, max);
    }

    /// <summary>
    /// 获得资源，自动 clamp 到上限
    /// </summary>
    /// <param name="amount">获得的数量</param>
    /// <param name="reason">获得原因（用于日志）</param>
    /// <returns>实际获得的数量</returns>
    public int Gain(int amount, string reason)
    {
        if (amount <= 0) return 0;
        
        int oldValue = Current;
        Current = Math.Min(Current + amount, Max);
        int actualGain = Current - oldValue;
        
        // 预留：溢出转换（Step 1 不实现）
        // if (Current == Max && amount > actualGain && !string.IsNullOrEmpty(ConvertTarget))
        // {
        //     int overflow = amount - actualGain;
        //     OnOverflow?.Invoke(ConvertTarget, (int)(overflow * ConvertRatio), reason);
        // }
        
        return actualGain;
    }

    /// <summary>
    /// 尝试消耗资源
    /// </summary>
    /// <param name="amount">消耗的数量</param>
    /// <param name="reason">消耗原因（用于日志）</param>
    /// <returns>是否成功消耗</returns>
    public bool TryConsume(int amount, string reason)
    {
        if (amount < 0) return false;
        if (Current < amount) return false;
        
        Current -= amount;
        return true;
    }

    /// <summary>
    /// 强制消耗资源（允许负数，用于特殊情况）
    /// </summary>
    public void ForceConsume(int amount, string reason)
    {
        Current = Math.Max(0, Current - amount);
    }

    /// <summary>
    /// 设置资源上限
    /// </summary>
    public void SetMax(int newMax)
    {
        if (newMax < 0) newMax = 0;
        Max = newMax;
        Current = Math.Min(Current, Max);
    }

    /// <summary>
    /// 重置资源到初始值
    /// </summary>
    public void Reset(int value = 0)
    {
        Current = Math.Clamp(value, 0, Max);
    }
}
```

**1.3 实现 ResourceBucketCollection.cs**

管理一个实体的多个资源桶：

```csharp
namespace BlazorIdle.Game.Resources;

/// <summary>
/// 资源桶集合，管理一个实体的所有资源
/// </summary>
public class ResourceBucketCollection
{
    private readonly Dictionary<string, ResourceBucket> _buckets = new();

    /// <summary>
    /// 创建集合并初始化默认资源桶（rage）
    /// </summary>
    public ResourceBucketCollection()
    {
        // 默认创建 rage 资源桶，上限 10
        _buckets["rage"] = new ResourceBucket("rage", max: 10, initial: 0);
    }

    /// <summary>
    /// 获取指定 ID 的资源桶
    /// </summary>
    public ResourceBucket GetBucket(string id)
    {
        if (!_buckets.TryGetValue(id, out var bucket))
        {
            throw new KeyNotFoundException($"Resource bucket '{id}' not found.");
        }
        return bucket;
    }

    /// <summary>
    /// 检查是否存在指定 ID 的资源桶
    /// </summary>
    public bool HasBucket(string id) => _buckets.ContainsKey(id);

    /// <summary>
    /// 添加新的资源桶
    /// </summary>
    public void AddBucket(string id, int max = 10, int initial = 0)
    {
        if (_buckets.ContainsKey(id))
        {
            throw new InvalidOperationException($"Resource bucket '{id}' already exists.");
        }
        _buckets[id] = new ResourceBucket(id, max, initial);
    }

    /// <summary>
    /// 移除资源桶
    /// </summary>
    public bool RemoveBucket(string id) => _buckets.Remove(id);

    /// <summary>
    /// 获取所有资源桶的只读视图
    /// </summary>
    public IReadOnlyDictionary<string, ResourceBucket> GetAll() => _buckets;
}
```

**1.4 实现 ResourceConfig.cs**

全局资源配置：

```csharp
namespace BlazorIdle.Game.Resources;

/// <summary>
/// 全局资源配置
/// </summary>
public class ResourceConfig
{
    /// <summary>
    /// 默认 rage 上限
    /// </summary>
    public int DefaultRageMax { get; set; } = 10;

    /// <summary>
    /// 每次普通攻击命中获得的 rage
    /// </summary>
    public int GainPerAttack { get; set; } = 1;

    /// <summary>
    /// 暴击时额外获得的 rage
    /// </summary>
    public int GainPerCritExtra { get; set; } = 1;
}
```

**1.5 单元测试**

创建 `BlazorIdle.Tests/Resources/ResourceBucketTests.cs`：

```csharp
public class ResourceBucketTests
{
    [Fact]
    public void ResourceBucket_Gain_ClampsToMax()
    {
        var bucket = new ResourceBucket("rage", max: 10, initial: 8);
        
        int gained = bucket.Gain(5, "test");
        
        Assert.Equal(2, gained);  // 只能获得 2（8 + 2 = 10）
        Assert.Equal(10, bucket.Current);
    }

    [Fact]
    public void ResourceBucket_TryConsume_SucceedsWhenEnough()
    {
        var bucket = new ResourceBucket("rage", max: 10, initial: 5);
        
        bool success = bucket.TryConsume(3, "test");
        
        Assert.True(success);
        Assert.Equal(2, bucket.Current);
    }

    [Fact]
    public void ResourceBucket_TryConsume_FailsWhenInsufficient()
    {
        var bucket = new ResourceBucket("rage", max: 10, initial: 2);
        
        bool success = bucket.TryConsume(5, "test");
        
        Assert.False(success);
        Assert.Equal(2, bucket.Current);  // 未变化
    }

    [Fact]
    public void ResourceBucket_SetMax_ClampsCurrentValue()
    {
        var bucket = new ResourceBucket("rage", max: 10, initial: 8);
        
        bucket.SetMax(5);
        
        Assert.Equal(5, bucket.Max);
        Assert.Equal(5, bucket.Current);  // 被 clamp 到新上限
    }

    [Fact]
    public void ResourceBucketCollection_GetBucket_ReturnsDefaultRage()
    {
        var collection = new ResourceBucketCollection();
        
        var rage = collection.GetBucket("rage");
        
        Assert.NotNull(rage);
        Assert.Equal("rage", rage.Id);
        Assert.Equal(10, rage.Max);
        Assert.Equal(0, rage.Current);
    }

    [Fact]
    public void ResourceBucketCollection_AddBucket_CreatesNewBucket()
    {
        var collection = new ResourceBucketCollection();
        
        collection.AddBucket("energy", max: 100, initial: 50);
        
        Assert.True(collection.HasBucket("energy"));
        var energy = collection.GetBucket("energy");
        Assert.Equal(100, energy.Max);
        Assert.Equal(50, energy.Current);
    }
}
```

#### 预计工作量
2-3 小时

#### 验收标准
- ✅ ResourceBucket 支持 Gain/TryConsume/SetMax
- ✅ ResourceBucketCollection 管理多个资源桶
- ✅ 单元测试覆盖边界情况（clamp、不足消耗等）
- ✅ 编译通过，所有测试通过

---

### 阶段 2：将资源系统集成到战斗流程（P0 - 必须）

#### 目标
玩家攻击时自动产生 rage 资源，并记录资源变更事件。

#### 任务清单

**2.1 扩展 BattleContext**

在 `BlazorIdle.Shared/Game/Skills/BattleContext.cs` 添加资源引用：

```csharp
public sealed class BattleContext
{
    // 现有字段...
    public Character? Player { get; init; }
    public Enemy? Enemy { get; init; }
    public BattleTeam<Character>? PlayerTeam { get; init; }
    public BattleTeam<Enemy>? EnemyTeam { get; init; }
    public RngContext Rng { get; init; }
    public IGameClock Clock { get; init; }
    
    // 新增：资源集合引用
    public ResourceBucketCollection? PlayerResources { get; init; }
    public ResourceBucketCollection? EnemyResources { get; init; }  // 预留，怪物暂不使用
}
```

**2.2 在 MultiBattleInstance 维护资源集合**

修改 `BlazorIdle.Shared/Game/MultiBattleInstance.cs`：

```csharp
public class MultiBattleInstance
{
    // 现有字段...
    private readonly Dictionary<string, ResourceBucketCollection> _playerResources = new();
    private readonly ResourceConfig _resourceConfig = new();

    public MultiBattleInstance(/* 现有参数 */)
    {
        // 现有初始化...
        
        // 为每个玩家创建资源集合
        foreach (var member in _playerTeam.Members)
        {
            _playerResources[member.Id] = new ResourceBucketCollection();
        }
    }

    // 新增：获取玩家资源快照（用于 UI 显示）
    public Dictionary<string, Dictionary<string, int>> GetResourceSnapshot()
    {
        var snapshot = new Dictionary<string, Dictionary<string, int>>();
        
        foreach (var (playerId, resources) in _playerResources)
        {
            snapshot[playerId] = new Dictionary<string, int>();
            foreach (var (bucketId, bucket) in resources.GetAll())
            {
                snapshot[playerId][bucketId] = bucket.Current;
            }
        }
        
        return snapshot;
    }
}
```

**2.3 修改攻击处理逻辑**

在 `ProcessCharacterAttackViaSkillResolver` 中添加资源获得逻辑：

```csharp
private void ProcessCharacterAttackViaSkillResolver(string charId, Character character)
{
    var targetId = SelectEnemyTarget(_config.PlayerTargetStrategy);
    if (targetId == null) return;
    
    var member = _playerTeam.GetMember(charId);
    var target = _enemyTeam.GetMember(targetId);
    if (member == null || target == null) return;
    
    // 创建战斗上下文
    var ctx = new BattleContext
    {
        Player = character,
        Enemy = target.Entity,
        PlayerTeam = _playerTeam,
        EnemyTeam = _enemyTeam,
        Rng = _rng,
        Clock = _clock,
        PlayerResources = _playerResources.GetValueOrDefault(charId)  // 新增
    };
    
    // 使用 SkillResolver 计算伤害
    var opts = new SkillCastOptions { SourceTrack = "attack" };
    var result = _skillResolver.Cast(SkillIds.AttackBasic, ctx, opts);
    
    // 应用伤害
    ApplyDamageToEnemy(charId, member, targetId, target, result.DamageDealt, 
                       EventSource.Attack, isAoe: false, isCrit: result.IsCrit,
                       skillId: SkillIds.AttackBasic, bundleId: opts.BundleId);
    
    // 新增：产生 rage 资源
    if (_playerResources.TryGetValue(charId, out var resources))
    {
        var rageBucket = resources.GetBucket("rage");
        
        // 命中 +1 rage
        int gained = rageBucket.Gain(_resourceConfig.GainPerAttack, "attack_hit");
        if (gained > 0)
        {
            RecordResourceGain(charId, "rage", gained, rageBucket.Current, "attack_hit");
        }
        
        // 暴击额外 +1 rage
        if (result.IsCrit)
        {
            int critGain = rageBucket.Gain(_resourceConfig.GainPerCritExtra, "crit_bonus");
            if (critGain > 0)
            {
                RecordResourceGain(charId, "rage", critGain, rageBucket.Current, "crit_bonus");
            }
        }
    }
}
```

**2.4 新增 ResourceGainEvent**

创建 `BlazorIdle.Shared/Game/Resources/ResourceGainEvent.cs`：

```csharp
namespace BlazorIdle.Game.Resources;

/// <summary>
/// 资源获得事件
/// </summary>
public class ResourceGainEvent : CombatEvent
{
    /// <summary>
    /// 获得资源的实体 ID
    /// </summary>
    public string ActorId { get; set; } = "";

    /// <summary>
    /// 资源桶 ID（如 "rage"）
    /// </summary>
    public string BucketId { get; set; } = "";

    /// <summary>
    /// 获得的数量（可能因 clamp 而小于请求数量）
    /// </summary>
    public int Delta { get; set; }

    /// <summary>
    /// 获得后的新值
    /// </summary>
    public int NewValue { get; set; }

    /// <summary>
    /// 获得原因
    /// </summary>
    public string Reason { get; set; } = "";

    /// <summary>
    /// 可选：关联的技能 ID
    /// </summary>
    public string? SkillId { get; set; }

    /// <summary>
    /// 可选：关联的 Bundle ID
    /// </summary>
    public string? BundleId { get; set; }
}
```

**2.5 实现资源事件记录**

在 `MultiBattleInstance` 中：

```csharp
private void RecordResourceGain(string actorId, string bucketId, int delta, int newValue, string reason)
{
    if (_combatConfig.EmitCastEvents)  // 复用现有开关
    {
        var evt = new ResourceGainEvent
        {
            TimeMs = _clock.NowMs,
            ActorId = actorId,
            BucketId = bucketId,
            Delta = delta,
            NewValue = newValue,
            Reason = reason
        };
        
        var flushed = _aggregator.AddEvent(evt);
        if (flushed != null) _segments.Add(flushed);
    }
}
```

**2.6 集成测试**

创建 `BlazorIdle.Tests/Integration/ResourceIntegrationTests.cs`：

```csharp
public class ResourceIntegrationTests
{
    [Fact]
    public void MultiBattle_PlayerAttack_GainsRage()
    {
        // 创建 3v2 战斗
        var battle = CreateTestBattle(playerCount: 3, enemyCount: 2);
        
        // 执行 100 ticks
        for (int i = 0; i < 100; i++)
        {
            battle.AdvanceTick(100);
        }
        
        // 验证玩家获得了 rage
        var resources = battle.GetResourceSnapshot();
        foreach (var (playerId, buckets) in resources)
        {
            Assert.True(buckets.ContainsKey("rage"));
            Assert.InRange(buckets["rage"], 1, 10);  // rage 应该在 1-10 之间
        }
    }

    [Fact]
    public void MultiBattle_RageClampsAt10()
    {
        var battle = CreateTestBattle(playerCount: 1, enemyCount: 1);
        
        // 执行很多 ticks，确保触发足够多攻击
        for (int i = 0; i < 500; i++)
        {
            battle.AdvanceTick(100);
        }
        
        // 验证 rage 不超过 10
        var resources = battle.GetResourceSnapshot();
        foreach (var buckets in resources.Values)
        {
            Assert.InRange(buckets["rage"], 0, 10);
        }
    }

    [Fact]
    public void MultiBattle_CritAttackGainsExtraRage()
    {
        // 创建 100% 暴击率的角色
        var character = CreateTestCharacter(critChance: 100.0);
        var battle = CreateTestBattle(characters: new[] { character }, enemyCount: 1);
        
        // 记录初始 rage
        var resources = battle.GetResourceSnapshot();
        int initialRage = resources[character.Id]["rage"];
        
        // 触发一次攻击
        battle.AdvanceTick(1000);  // 足够长的时间确保触发攻击
        
        // 验证获得了 2 点 rage（命中 +1，暴击 +1）
        resources = battle.GetResourceSnapshot();
        Assert.Equal(initialRage + 2, resources[character.Id]["rage"]);
    }
}
```

#### 预计工作量
3-4 小时

#### 验收标准
- ✅ 玩家攻击命中 +1 rage，暴击额外 +1 rage
- ✅ Rage 不超过 10（clamp 生效）
- ✅ ResourceGainEvent 正确记录到 Segment
- ✅ 集成测试验证资源获得逻辑
- ✅ 原有 104 个测试继续通过

---

### 阶段 3：Buff 系统核心实现（P0 - 必须）

#### 目标
实现 BuffInstance 和多种 Effect 类型，支持 Buff 的生命周期管理。

#### 任务清单

**3.1 创建 Buff 系统目录结构**

```
BlazorIdle.Shared/Game/Buffs/
├── BuffInstance.cs
├── BuffKind.cs
├── StackingPolicy.cs
├── Effects/
│   ├── Effect.cs (基类)
│   ├── StatMultiplierEffect.cs
│   ├── StatAdditiveEffect.cs
│   ├── ForceCritEffect.cs
│   ├── DamageOverTimeEffect.cs
│   ├── HealOverTimeEffect.cs
│   └── InstantHealEffect.cs
└── IBuffOwner.cs
```

**3.2 实现 BuffKind 和 StackingPolicy**

```csharp
// BuffKind.cs
namespace BlazorIdle.Game.Buffs;

public enum BuffKind
{
    Buff,     // 增益
    Debuff    // 减益
}

// StackingPolicy.cs
namespace BlazorIdle.Game.Buffs;

public enum StackingPolicy
{
    Refresh,   // 重置持续时间，不堆叠
    Stack,     // 堆叠层数
    Ignore     // 若已存在则忽略新的
}
```

**3.3 实现 Effect 类型体系**

```csharp
// Effect.cs（抽象基类）
namespace BlazorIdle.Game.Buffs.Effects;

public abstract class Effect
{
    public abstract string EffectType { get; }
}

// StatMultiplierEffect.cs
public class StatMultiplierEffect : Effect
{
    public override string EffectType => "StatMultiplier";
    
    /// <summary>
    /// 目标属性名称（如 "DamagePerAttack"、"HastePercent"）
    /// </summary>
    public string Target { get; set; } = "";
    
    /// <summary>
    /// 乘法系数（0.15 表示 +15%）
    /// </summary>
    public double Value { get; set; }
}

// StatAdditiveEffect.cs
public class StatAdditiveEffect : Effect
{
    public override string EffectType => "StatAdditive";
    
    public string Target { get; set; } = "";
    
    /// <summary>
    /// 加法值（可以是百分点或绝对值，取决于 Target）
    /// </summary>
    public double Value { get; set; }
}

// ForceCritEffect.cs
public class ForceCritEffect : Effect
{
    public override string EffectType => "ForceCrit";
    
    /// <summary>
    /// 剩余触发次数（0 表示无限次，直到 Buff 过期）
    /// </summary>
    public int RemainingTriggers { get; set; } = 1;
}

// DamageOverTimeEffect.cs
public class DamageOverTimeEffect : Effect
{
    public override string EffectType => "DamageOverTime";
    
    /// <summary>
    /// 每次 Tick 造成的伤害
    /// </summary>
    public int AmountPerTick { get; set; }
}

// HealOverTimeEffect.cs
public class HealOverTimeEffect : Effect
{
    public override string EffectType => "HealOverTime";
    
    /// <summary>
    /// 每次 Tick 治疗的数量
    /// </summary>
    public int AmountPerTick { get; set; }
}

// InstantHealEffect.cs
public class InstantHealEffect : Effect
{
    public override string EffectType => "InstantHeal";
    
    /// <summary>
    /// 瞬间治疗的数量
    /// </summary>
    public int Amount { get; set; }
}
```

**3.4 实现 IBuffOwner 接口**

```csharp
namespace BlazorIdle.Game.Buffs;

/// <summary>
/// Buff 持有者接口（玩家和怪物都可实现）
/// </summary>
public interface IBuffOwner
{
    /// <summary>
    /// 实体 ID
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// 是否是玩家
    /// </summary>
    bool IsPlayer { get; }
    
    /// <summary>
    /// 当前生命值
    /// </summary>
    int CurrentHp { get; }
    
    /// <summary>
    /// 最大生命值
    /// </summary>
    int MaxHp { get; }
    
    /// <summary>
    /// 资源桶集合
    /// </summary>
    ResourceBucketCollection Buckets { get; }
    
    /// <summary>
    /// 当前拥有的所有 Buff
    /// </summary>
    Dictionary<string, BuffInstance> Buffs { get; }
    
    /// <summary>
    /// 施加 Buff
    /// </summary>
    void ApplyBuff(BuffInstance buff);
    
    /// <summary>
    /// 移除 Buff
    /// </summary>
    void RemoveBuff(string buffId, string reason);
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    void ReceiveDamage(int amount, string source);
    
    /// <summary>
    /// 受到治疗
    /// </summary>
    void ReceiveHeal(int amount, string source);
}
```

**3.5 实现 BuffInstance**

```csharp
namespace BlazorIdle.Game.Buffs;

public class BuffInstance
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public BuffKind Kind { get; set; }
    public List<Effect> Effects { get; set; } = new();
    public StackingPolicy StackingPolicy { get; set; }
    public int Stacks { get; set; } = 1;
    public int MaxStacks { get; set; } = 99;
    
    /// <summary>
    /// 剩余持续时间（秒），null 表示永久
    /// </summary>
    public double? DurationSec { get; set; }
    
    /// <summary>
    /// Tick 间隔（用于 DoT/HoT），null 表示不 Tick
    /// </summary>
    public double? TickIntervalSec { get; set; }
    
    /// <summary>
    /// Tick 累积时间（内部使用）
    /// </summary>
    public double TickAccum { get; set; } = 0;
    
    /// <summary>
    /// 来源技能 ID（可选）
    /// </summary>
    public string? SourceSkillId { get; set; }
    
    /// <summary>
    /// 名称（用于 UI 显示）
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 描述（用于 Tooltip）
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 图标 URL（用于 UI）
    /// </summary>
    public string IconUrl { get; set; } = "";

    /// <summary>
    /// Buff Tick 处理
    /// </summary>
    public void Tick(double dt, IBuffOwner owner, Action<BuffTickInfo> onTick)
    {
        // 递减持续时间
        if (DurationSec.HasValue)
        {
            DurationSec -= dt;
        }
        
        // 处理 DoT/HoT Tick
        if (TickIntervalSec.HasValue && TickIntervalSec.Value > 0)
        {
            TickAccum += dt;
            
            while (TickAccum >= TickIntervalSec.Value)
            {
                TickAccum -= TickIntervalSec.Value;
                ProcessTick(owner, onTick);
            }
        }
    }

    private void ProcessTick(IBuffOwner owner, Action<BuffTickInfo> onTick)
    {
        foreach (var effect in Effects)
        {
            if (effect is DamageOverTimeEffect dot)
            {
                owner.ReceiveDamage(dot.AmountPerTick, $"dot_{Id}");
                onTick?.Invoke(new BuffTickInfo
                {
                    BuffId = Id,
                    OwnerId = owner.Id,
                    TickType = BuffTickType.DamageOverTime,
                    Amount = dot.AmountPerTick,
                    ResultingHp = owner.CurrentHp
                });
            }
            else if (effect is HealOverTimeEffect hot)
            {
                owner.ReceiveHeal(hot.AmountPerTick, $"hot_{Id}");
                onTick?.Invoke(new BuffTickInfo
                {
                    BuffId = Id,
                    OwnerId = owner.Id,
                    TickType = BuffTickType.HealOverTime,
                    Amount = hot.AmountPerTick,
                    ResultingHp = owner.CurrentHp
                });
            }
        }
    }

    /// <summary>
    /// 检查是否已过期
    /// </summary>
    public bool IsExpired()
    {
        return DurationSec.HasValue && DurationSec.Value <= 0;
    }

    /// <summary>
    /// 生成效果摘要（用于事件记录）
    /// </summary>
    public string GetEffectsSummary()
    {
        return string.Join(", ", Effects.Select(e => e.EffectType));
    }
}

public enum BuffTickType
{
    DamageOverTime,
    HealOverTime
}

public class BuffTickInfo
{
    public string BuffId { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public BuffTickType TickType { get; set; }
    public int Amount { get; set; }
    public int ResultingHp { get; set; }
}
```

**3.6 单元测试**

创建 `BlazorIdle.Tests/Buffs/BuffInstanceTests.cs`：

```csharp
public class BuffInstanceTests
{
    [Fact]
    public void BuffInstance_Tick_DecrementsDuration()
    {
        var buff = new BuffInstance
        {
            Id = "test_buff",
            DurationSec = 5.0
        };
        
        buff.Tick(1.0, null, null);
        
        Assert.Equal(4.0, buff.DurationSec);
    }

    [Fact]
    public void BuffInstance_Tick_ProcessesDamageOverTime()
    {
        var owner = CreateMockBuffOwner();
        var buff = new BuffInstance
        {
            Id = "test_dot",
            TickIntervalSec = 1.0,
            Effects = new List<Effect>
            {
                new DamageOverTimeEffect { AmountPerTick = 10 }
            }
        };
        
        int tickCount = 0;
        buff.Tick(1.5, owner, info => tickCount++);
        
        Assert.Equal(1, tickCount);  // 触发了 1 次 Tick
        Assert.InRange(buff.TickAccum, 0.4, 0.6);  // 剩余 0.5 秒
    }

    [Fact]
    public void BuffInstance_IsExpired_ReturnsTrueWhenDurationZero()
    {
        var buff = new BuffInstance
        {
            Id = "test_buff",
            DurationSec = 0.0
        };
        
        Assert.True(buff.IsExpired());
    }

    [Fact]
    public void BuffInstance_IsExpired_ReturnsFalseForPermanentBuff()
    {
        var buff = new BuffInstance
        {
            Id = "test_buff",
            DurationSec = null  // 永久
        };
        
        Assert.False(buff.IsExpired());
    }
}
```

#### 预计工作量
4-5 小时

#### 验收标准
- ✅ BuffInstance 支持多种 Effect 类型
- ✅ Tick 逻辑正确处理 DoT/HoT
- ✅ 过期检测正常工作
- ✅ 单元测试覆盖所有 Effect 类型
- ✅ 编译通过，所有测试通过

---

### 阶段 4-10 概要

（由于文档长度限制，后续阶段的详细内容请参考完整实施方案）

**阶段 4**：IBuffOwner 接口与玩家/怪物集成（4-5h）  
**阶段 5**：扩展 SkillResolver 支持 Buff 操作（3-4h）  
**阶段 6**：实现 Special 脉冲与 Buff 施加（5-6h）  
**阶段 7**：Buff 效果应用到属性计算（4-5h）  
**阶段 8**：扩展事件系统（3-4h）  
**阶段 9**：UI 展示 Buff/Debuff（6-8h）  
**阶段 10**：验收测试与文档（4-5h）

---

## 📊 三、总体时间估算

| 阶段 | 任务描述 | 预计工作量 | 累计时间 |
|------|---------|-----------|---------|
| 阶段 1 | 资源系统基础实现 | 2-3h | 3h |
| 阶段 2 | 资源集成战斗流程 | 3-4h | 7h |
| 阶段 3 | Buff 系统核心实现 | 4-5h | 12h |
| 阶段 4 | IBuffOwner 接口集成 | 4-5h | 17h |
| 阶段 5 | SkillResolver 扩展 | 3-4h | 21h |
| 阶段 6 | Special 脉冲 Buff | 5-6h | 27h |
| 阶段 7 | Buff 效果应用 | 4-5h | 32h |
| 阶段 8 | 事件系统扩展 | 3-4h | 36h |
| 阶段 9 | UI 展示 | 6-8h | 44h |
| 阶段 10 | 验收测试与文档 | 4-5h | 49h |

**总计：约 40-50 小时（5-7 个工作日）**

---

## 🔧 四、技术决策说明

### 4.1 为什么使用 IBuffOwner 接口？

**理由：**
- **统一管理**：玩家和怪物使用相同的 Buff 管理逻辑，避免代码重复
- **易于扩展**：未来添加 NPC、宠物等实体时，只需实现 IBuffOwner 接口
- **职责分离**：Buff 管理独立于 Character/Enemy 的核心逻辑
- **便于测试**：可以创建 Mock IBuffOwner 进行单元测试

**替代方案：**
- 直接在 Character 和 Enemy 类中添加 Buff 字段 → 会导致代码重复和耦合
- 使用继承（如 BuffableEntity 基类）→ 不如接口灵活，且 C# 单继承限制

### 4.2 为什么 Buff 不持久化？

**理由：**
- **设计要求**：Step 1 明确不改动 CharacterData
- **运行态数据**：Buff 是战斗过程中的临时状态，战斗结束即清空
- **简化存档**：避免存储大量临时数据
- **性能考虑**：减少频繁的存档操作

**未来扩展：**
- 如需要"离线时 Buff 持续生效"，可在战斗开始时从配置重新施加

### 4.3 为什么使用 BuffOps 指令模式？

**理由：**
- **单一职责**：SkillResolver 只负责计算，MultiBattleInstance 负责应用
- **便于测试**：可以独立测试 SkillResolver 的计算逻辑
- **支持批处理**：可以先收集所有变更再统一应用，便于优化
- **清晰的数据流**：SkillResolver → BuffOps → MultiBattleInstance → IBuffOwner

**替代方案：**
- SkillResolver 直接修改 Buff → 违反单一职责，难以测试
- 使用事件/回调 → 增加复杂度，调试困难

### 4.4 为什么选择 Effect 继承体系？

**理由：**
- **类型安全**：编译时检查，避免运行时错误
- **清晰的语义**：每种 Effect 都有明确的类型和字段
- **易于扩展**：添加新 Effect 类型只需创建新子类
- **符合 C# 习惯**：OOP 继承是 C# 的主流模式

**替代方案：**
- Union Type（Discriminated Union）→ C# 不原生支持，需要手动实现
- Dictionary<string, object> → 失去类型安全，容易出错

---

## ⚠️ 五、风险与缓解措施

### 风险 1：Buff 计算性能问题

**场景：**  
大量 Buff 叠加时，每次伤害计算都需要遍历所有 Buff，可能造成性能问题。

**缓解措施：**
1. **缓存加成结果**：每个 Tick 开始时计算一次所有 Buff 的总加成，缓存到 BuffOwner
2. **限制 Buff 数量**：使用 `maxBuffsPerOwner` 配置项（如 20 个）
3. **性能测试**：阶段 10 中测试 1000 ticks、3v5、每个单位 3-5 个 Buff 的场景
4. **延迟计算**：只有在需要时才重新计算加成（标记为 dirty）

### 风险 2：UI 更新频率过高

**场景：**  
每个 Tick（100ms）都更新 UI，可能导致渲染卡顿。

**缓解措施：**
1. **节流（Throttle）**：UI 每 200-500ms 更新一次，而非每 Tick
2. **增量更新**：只更新变化的 Buff，而非重新渲染整个列表
3. **虚拟滚动**：如果 Buff 数量很多，使用虚拟滚动技术
4. **条件渲染**：仅在 Buff 列表实际变化时触发更新

### 风险 3：Buff 事件过多导致 Segment 膨胀

**场景：**  
DoT/HoT 每秒触发一次，长时间战斗会产生大量 BuffTickEvent。

**缓解措施：**
1. **可配置开关**：使用 `emitBuffTickEvents` 配置项，生产环境可关闭
2. **聚合事件**：记录总 DoT 伤害而非每次 Tick（如"10 秒内造成 60 点 DoT"）
3. **采样记录**：只记录部分 Tick 事件（如每 5 次记录 1 次）
4. **压缩存储**：使用更紧凑的事件格式

### 风险 4：Buff 与 Track 暂停冲突

**场景：**  
未来施法系统启用后，施法会暂停 Attack Track，但 Buff 持续时间继续递减。

**缓解措施：**
1. **Buff 也支持暂停**：在 BuffInstance 中添加 `IsPaused` 字段
2. **独立的时间轴**：Buff 使用"战斗时间"而非"真实时间"
3. **清晰的文档**：说明 Buff 持续时间的计算方式
4. **未来设计**：在 Step 2（技能系统）中详细规划

---

## 📝 六、后续扩展预留（Future）

以下功能在 Step 1 **不实现**，仅预留接口或字段：

### 6.1 资源溢出转换

**接口预留：**
```csharp
public class ResourceBucket
{
    public string? ConvertTarget { get; set; }
    public double ConvertRatio { get; set; } = 0.0;
    // public event Action<string, int, string>? OnOverflow;  // 预留
}
```

**用途：**  
Rage 满时（10/10），继续获得的 rage 转换为其他资源（如能量、护盾等）。

**实现时机：**  
Step 2 或 Step 3，根据游戏设计需求。

### 6.2 Buff 驱散（Dispel）

**接口预留：**
```csharp
public enum BuffOpType
{
    Apply,
    Remove,
    Refresh,
    Dispel  // 预留
}
```

**用途：**  
技能或特殊机制可以移除目标身上的 Buff/Debuff。

**实现时机：**  
Step 2（技能系统）中实现。

### 6.3 Buff 优先级与互斥

**接口预留：**
```csharp
public class BuffInstance
{
    public int Priority { get; set; } = 0;  // 预留
    public List<string> MutexBuffIds { get; set; } = new();  // 预留
}
```

**用途：**  
- 高优先级 Buff 可以覆盖低优先级
- 互斥 Buff 不能同时存在（如"狂暴"与"虚弱"）

**实现时机：**  
根据游戏平衡需求，可能在 Step 3 或更晚。

### 6.4 复杂 Targeting

**当前支持：**  
- `self`：施加到自身
- `allEnemies`：施加到所有敌人

**未来扩展：**
- `randomEnemy`：随机一个敌人
- `lowestHpEnemy`：生命值最低的敌人
- `highestThreatEnemy`：威胁值最高的敌人
- `allAllies`：所有队友
- `randomAlly`：随机一个队友

**实现时机：**  
Step 2（技能系统）中实现更丰富的目标选择。

### 6.5 Buff 快照（Snapshotting）

**概念：**  
Buff 施加时"快照"当前属性值，后续属性变化不影响 Buff 效果。

**示例：**  
- 施加 DoT 时，快照当前攻击力，即使攻击力后续变化，DoT 伤害不变

**实现时机：**  
根据游戏机制需求，可能在 Step 3 或更晚。

---

## ✅ 七、验收清单

在 Step 1 实施完成后，需要验证以下所有项：

### 功能验收

**资源系统：**
- [ ] 玩家攻击命中产生 +1 rage
- [ ] 玩家暴击产生额外 +1 rage（总共 +2）
- [ ] Rage 上限为 10，正确 clamp
- [ ] 可以消耗 rage（TryConsume 返回正确结果）
- [ ] ResourceGainEvent 正确记录到 Segment

**Buff 系统：**
- [ ] 可以施加 Buff 到玩家和怪物
- [ ] Buff 持续时间正确倒计时
- [ ] Buff 过期后自动移除
- [ ] StackingPolicy.Refresh 正确刷新持续时间
- [ ] StackingPolicy.Stack 正确堆叠层数
- [ ] DoT 每秒正确造成伤害
- [ ] HoT 每秒正确治疗
- [ ] InstantHeal 立即治疗
- [ ] BuffApplyEvent、BuffRemoveEvent、BuffTickEvent 正确记录

**属性加成：**
- [ ] StatMultiplierEffect 正确增加伤害（如 +15%）
- [ ] StatAdditiveEffect 正确增加暴击率（如 +5%）
- [ ] 多个 Buff 的效果可以叠加
- [ ] Buff 过期后，属性恢复正常

**Special 脉冲：**
- [ ] Warrior Special 正确施加自身 Buff（+15% 伤害、+10% 急速、+5% 暴击）
- [ ] Mage Special 正确施加敌人 Debuff（DoT 6 伤害/秒）
- [ ] presence 模式下，无敌人时不增长
- [ ] encounter 模式下，无敌人时继续增长

### UI 验收

- [ ] 玩家头像旁显示 Buff 图标列表
- [ ] 敌人头像下显示 Debuff 图标列表
- [ ] 图标显示堆栈数（如果 stacks > 1）
- [ ] 图标显示剩余时间（秒，向下取整）
- [ ] 鼠标悬停显示 Tooltip
- [ ] Tooltip 包含：名称、种类、效果描述、剩余时间、来源
- [ ] Buff 使用绿色/蓝色边框
- [ ] Debuff 使用红色边框
- [ ] 剩余时间实时更新

### 技术验收

- [ ] 所有原有 104 个测试继续通过（无回归）
- [ ] 新增单元测试全部通过
- [ ] 新增集成测试全部通过
- [ ] 代码覆盖率 > 80%（核心逻辑）
- [ ] 性能测试通过：1000 ticks，3v5，3-5 buffs/unit < 100ms
- [ ] CodeQL 安全检查通过
- [ ] 构建无错误，无新增警告

### 文档验收

- [ ] `docs_step1_Step1-设计方案.md` 更新为"已完成"
- [ ] 创建 `Step1_实施进度追踪.md` 并维护进度
- [ ] 代码注释完整准确（特别是公共 API）
- [ ] README 更新（如有需要）

---

## 📈 八、实施建议

### 8.1 开发顺序

建议严格按照阶段 1 → 10 的顺序实施，原因：

1. **依赖关系**：后续阶段依赖前面阶段的成果
2. **渐进式验证**：每个阶段都有独立的验收标准
3. **风险控制**：及早发现设计问题，避免大规模返工

### 8.2 测试策略

**测试金字塔：**
- **单元测试（60%）**：ResourceBucket、BuffInstance、Effect 类型
- **集成测试（30%）**：MultiBattleInstance 与 Buff/资源的交互
- **UI 测试（10%）**：手动验证或快照测试

**TDD 建议：**
- 阶段 1-3：先写测试，再写实现（TDD）
- 阶段 4-7：测试与实现并行
- 阶段 8-9：实现后补充测试

### 8.3 代码审查要点

每个阶段完成后，应审查：

1. **接口设计**：是否符合 SOLID 原则？
2. **命名规范**：是否清晰、一致？
3. **错误处理**：是否处理了边界情况？
4. **性能考虑**：是否有明显的性能问题？
5. **文档完整性**：公共 API 是否有注释？

### 8.4 常见陷阱

**陷阱 1：过度设计**  
不要在 Step 1 实现所有可能的功能，严格遵循设计文档的范围。

**陷阱 2：忽略测试**  
不要等到最后才写测试，应该每个阶段都有对应的测试。

**陷阱 3：破坏现有功能**  
每次修改后都运行完整测试套件，确保 104 个原有测试继续通过。

**陷阱 4：UI 实现过早**  
先完成后端逻辑（阶段 1-8），再做 UI（阶段 9），避免反复修改 UI。

---

## 🎓 九、参考资料

### 相关文档

- [Step 0 设计方案](./docs_step0_第0步-设计方案.md)
- [Step 0 实施进度追踪](./Step0_实施进度追踪.md)
- [Step 1 设计文档](./docs_step1_Step1-设计方案.md)
- [战斗系统重构设计](../战斗系统重构设计/README.md)

### 技术参考

- **C# 异步编程**：虽然 Step 1 不涉及异步，但 UI 更新可能需要
- **Blazor 组件生命周期**：理解 `StateHasChanged()` 的使用时机
- **xUnit 测试框架**：熟悉 `[Fact]`、`[Theory]`、`Assert` 等
- **性能分析工具**：使用 BenchmarkDotNet 或 Visual Studio Profiler

---

## 📞 十、联系与支持

### 问题反馈

如在实施过程中遇到问题，请：

1. **检查文档**：查阅设计文档和本实施方案
2. **查看示例**：参考 Step 0 的实施方式
3. **运行测试**：确保现有测试通过
4. **提交 Issue**：在 GitHub 仓库中提交问题

### 进度更新

建议每完成一个阶段后：

1. 更新 `Step1_实施进度追踪.md`
2. 提交 Git commit（附上阶段编号）
3. 通知团队成员（如有）

---

## 🎉 结语

Step 1 的实施将为战斗系统带来质的飞跃，引入资源管理和 Buff 系统后，游戏玩法将更加丰富和有趣。

**关键成功因素：**
- ✅ 严格遵循阶段划分，不跳步
- ✅ 保持高测试覆盖率
- ✅ 频繁运行测试，及时发现问题
- ✅ 代码审查，保证质量
- ✅ 完善文档，便于维护

**预期成果：**
- ✅ 104 → 130+ 测试（+25%）
- ✅ 资源系统可用（rage）
- ✅ Buff/Debuff 系统完整
- ✅ Special 脉冲增强
- ✅ UI 展示 Buff 信息
- ✅ 为 Step 2（技能系统）打下基础

祝实施顺利！💪

---

**文档版本：** v1.0  
**最后更新：** 2025-11-11  
**维护者：** @copilot
