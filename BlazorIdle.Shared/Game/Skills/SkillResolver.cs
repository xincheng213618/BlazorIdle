using System;
using System.Collections.Generic;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Buffs;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能解析器基础实现 - 统一处理技能效果
    /// Basic skill resolver implementation - unified skill effect processing
    /// 
    /// Phase 5 Enhancement: 支持新伤害系统集成
    /// Phase 5 Enhancement: Supports new damage system integration
    /// - 当 BattleContext.DamageCalculator 可用时，使用新的 7 层伤害管线
    /// - When BattleContext.DamageCalculator is available, use new 7-layer damage pipeline
    /// - 否则回退到旧的简化伤害计算
    /// - Otherwise fallback to old simplified damage calculation
    /// 
    /// Buff System Optimization: 使用 BuffStatApplier 应用 Buff 效果到 CombatStats
    /// Buff System Optimization: Use BuffStatApplier to apply buff effects to CombatStats
    /// - 公式：最终属性 = 职业属性 + Clamp(装备属性) + Buff效果
    /// - Formula: FinalStats = ProfessionStats + Clamp(EquipmentStats) + BuffEffects
    /// - Buff 效果不受属性上限裁剪
    /// - Buff effects are NOT subject to stat caps
    /// </summary>
    public sealed class SkillResolver : ISkillResolver
    {
        private readonly Config.CombatConfig? _config;
        private readonly SkillRepository _skillRepository;
        private readonly TargetSelector _targetSelector;
        private int _castCounter = 0;
        private int _currentTickCasts = 0;
        private int _lastTickTime = 0;
        
        // Counter reset threshold to prevent overflow (reset after ~1 million casts)
        // 计数器重置阈值，防止溢出（约 100 万次施放后重置）
        private const int CounterResetThreshold = 1_000_000;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="config">战斗配置（可选）/ Combat configuration (optional)</param>
        /// <param name="skillRepository">技能配置仓库（可选，Phase 5）/ Skill repository (optional, Phase 5)</param>
        /// <param name="targetSelector">目标选择器（可选，Phase 3）/ Target selector (optional, Phase 3)</param>
        public SkillResolver(Config.CombatConfig? config = null, SkillRepository? skillRepository = null, TargetSelector? targetSelector = null)
        {
            _config = config;
            _skillRepository = skillRepository ?? SkillRepository.Shared;
            _targetSelector = targetSelector ?? new TargetSelector();
        }

        /// <summary>
        /// 施放单个技能（Phase 5: 支持 Buff 操作，Phase 8: 应用 Buff 效果到属性计算）
        /// 战斗重构 Phase 1: 支持多段伤害
        /// Cast a single skill (Phase 5: Supports buff operations, Phase 8: Apply buff effects to stat calculations)
        /// Battle Refactor Phase 1: Supports multi-hit damage
        /// </summary>
        public SkillCastResult Cast(string skillId, BattleContext ctx, SkillCastOptions? opts = null)
        {
            opts ??= new SkillCastOptions();

            // Phase 5: 获取技能定义
            // Phase 5: Get skill definition
            var skillDef = _skillRepository.GetSkill(skillId);
            
            // Phase 7.10: 验证 SkillDef.Id 与 skillId 的一致性
            // Phase 7.10: Validate SkillDef.Id consistency with skillId parameter
            if (skillDef != null && skillDef.Id != skillId)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SkillResolver] Warning: SkillDef.Id mismatch! " +
                    $"Parameter skillId='{skillId}' but SkillDef.Id='{skillDef.Id}'");
            }

            // Phase 8: 获取施法者的 Buff 所有者以应用 Buff 效果
            // Phase 8: Get caster's buff owner to apply buff effects
            // Monster Skill System: 怪物技能不使用 Buff 系统
            // Monster Skill System: Monster skills don't use buff system
            Buffs.IBuffOwner? casterBuffOwner = null;
            bool isMonsterSkillForBuff = skillId.StartsWith("enemy_") || skillId.StartsWith("monster_");
            if (!isMonsterSkillForBuff && ctx.PlayerBuffOwner != null)
            {
                casterBuffOwner = ctx.PlayerBuffOwner;
            }

            bool isMonsterSkill = skillId.StartsWith("enemy_") || skillId.StartsWith("monster_");
            
            // 战斗重构 Phase 1: 检测是否为多段伤害技能
            // Battle Refactor Phase 1: Detect if this is a multi-hit skill
            bool isMultiHit = skillDef?.IsMultiHit ?? false;
            
            // 创建结果对象
            // Create result object
            var result = new SkillCastResult
            {
                BundleId = opts.BundleId,
                InstantHeal = skillDef?.InstantHeal ?? 0
            };

            if (isMultiHit && skillDef!.Hits != null)
            {
                // 战斗重构 Phase 1: 多段伤害处理
                // Battle Refactor Phase 1: Multi-hit damage processing
                result.DamageInstances = new List<DamageInstance>();
                bool? sharedCritResult = null; // 用于共享暴击判定
                
                foreach (var hit in skillDef.Hits)
                {
                    var damageInstance = CalculateDamageInstance(
                        hit, skillDef, ctx, opts, 
                        isMonsterSkill, casterBuffOwner,
                        ref sharedCritResult);
                    
                    result.DamageInstances.Add(damageInstance);
                }
                
                // 填充向后兼容的单段伤害字段（使用第一段的结果）
                // Populate backward compatible single-hit fields (using first hit's result)
                if (result.DamageInstances.Count > 0)
                {
                    var firstHit = result.DamageInstances[0];
                    result.DamageDealt = firstHit.Damage;
                    result.IsCrit = firstHit.IsCrit;
                    result.HasElementAdvantage = firstHit.HasElementAdvantage;
                    result.ElementMultiplier = firstHit.ElementMultiplier;
                    result.DetailedDamageResult = firstHit.DetailedResult;
                }
            }
            else
            {
                // 单段伤害处理（向后兼容）
                // Single-hit damage processing (backward compatible)
                double dmg = 0;
                bool isCrit = false;
                DamageResult? damageResult = null;

                // Phase 5: 优先使用新的 DamageCalculator 系统
                // Phase 5: Prefer new DamageCalculator system when available
                if (ctx.DamageCalculator != null && ctx.AttackerCombatStats != null)
                {
                    // 使用新的 7 层伤害管线
                    // Use new 7-layer damage pipeline
                    var damageDef = skillDef?.Damage;
                    double skillCoef = damageDef?.CoefAtk ?? 1.0;
                    int skillFlat = damageDef?.Flat ?? 0;
                    
                    // Buff System Optimization: 应用 Buff 效果到整个 CombatStats
                    // Buff System Optimization: Apply buff effects to entire CombatStats
                    var baseStats = ctx.AttackerCombatStats;
                    var buffedStats = (!isMonsterSkill && casterBuffOwner != null)
                        ? BuffStatApplier.ApplyBuffsToCombatStats(baseStats, casterBuffOwner)
                        : baseStats;
                    
                    int attackFinal = isMonsterSkill
                        ? (int)(ctx.Enemy?.BaseAttack ?? 0)
                        : buffedStats.AttackFinal;
                    
                    // 创建伤害上下文
                    // Create damage context
                    var damageCtx = new DamageContext
                    {
                        AttackFinal = attackFinal,
                        SkillCoef = skillCoef,
                        SkillFlat = skillFlat,
                        AttackerStats = buffedStats,
                        AttackerHPRatio = ctx.AttackerHPRatio,
                        AttackerElement = ctx.AttackerElement ?? ElementIds.Neutral,
                        DefenderElement = ctx.DefenderElement ?? ElementIds.Neutral,
                        DefenderDRPct = ctx.DefenderDamageReductionPercent,
                        Rng = new Random(ctx.Rng.NextRange(int.MinValue, int.MaxValue))
                    };
                    
                    // Phase 8: 检查 ForceCrit
                    // Phase 8: Check ForceCrit
                    bool hasForceCrit = casterBuffOwner != null && HasForceCritEffect(casterBuffOwner);
                    bool canCrit = skillDef?.CanCrit ?? true;
                    
                    if (hasForceCrit && canCrit && !isMonsterSkill)
                    {
                        damageResult = ctx.DamageCalculator.CalculateDeterministic(damageCtx, forceCrit: true);
                        ConsumeForceCritBuff(casterBuffOwner!);
                    }
                    else if (opts.ForceCrit && canCrit && !isMonsterSkill)
                    {
                        damageResult = ctx.DamageCalculator.CalculateDeterministic(damageCtx, forceCrit: true);
                    }
                    else
                    {
                        damageResult = ctx.DamageCalculator.Calculate(damageCtx);
                    }
                    
                    dmg = damageResult.FinalDamage;
                    isCrit = damageResult.IsCrit;
                }

                result.DamageDealt = (int)dmg;
                result.IsCrit = isCrit;
                result.HasElementAdvantage = damageResult?.HasElementAdvantage ?? false;
                result.ElementMultiplier = damageResult?.ElementMultiplier ?? 1.0;
                result.StancePercent = damageResult?.StancePercent ?? 0;
                result.DetailedDamageResult = damageResult;
            }

            // Phase 3: 解析目标选择策略
            // Phase 3: Resolve target selection policy
            if (skillDef != null && !string.IsNullOrEmpty(skillDef.TargetPolicy))
            {
                string policyString = ConvertSnakeCaseToPascalCase(skillDef.TargetPolicy);
                
                if (Enum.TryParse<TargetPolicy>(policyString, ignoreCase: true, out var policy))
                {
                    string? casterId = opts.CasterId;
                    string? currentTargetId = ctx.CurrentTargetId;
                    result.TargetIds = _targetSelector.ResolveTargets(policy, ctx, casterId, currentTargetId);
                }
            }

            // Phase 5: 添加 Buff 操作和资源变化
            // Phase 5: Add buff operations and resource changes
            if (skillDef != null)
            {
                result.BuffOperations.AddRange(skillDef.OnCastBuffs);

                bool skillHits = skillDef.AlwaysHits || true;
                if (skillHits)
                {
                    result.BuffOperations.AddRange(skillDef.OnHitBuffs);
                }

                if (result.IsCrit)
                {
                    result.BuffOperations.AddRange(skillDef.OnCritBuffs);
                }

                // 资源消耗
                foreach (var (resId, cost) in skillDef.ResourceCosts)
                {
                    result.ResourceChanges[resId] = -cost;
                }
                
                if (skillDef.Costs != null)
                {
                    foreach (var cost in skillDef.Costs)
                    {
                        result.ResourceChanges[cost.BucketId] = -cost.Amount;
                    }
                }

                // 资源获得
                foreach (var (resId, gain) in skillDef.ResourceGains)
                {
                    if (result.ResourceChanges.ContainsKey(resId))
                    {
                        result.ResourceChanges[resId] += gain;
                    }
                    else
                    {
                        result.ResourceChanges[resId] = gain;
                    }
                }
                
                if (skillDef.Gains != null)
                {
                    foreach (var resourceGain in skillDef.Gains)
                    {
                        if (result.ResourceChanges.ContainsKey(resourceGain.BucketId))
                        {
                            result.ResourceChanges[resourceGain.BucketId] += resourceGain.Amount;
                        }
                        else
                        {
                            result.ResourceChanges[resourceGain.BucketId] = resourceGain.Amount;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 战斗重构 Phase 1: 计算单个伤害实例
        /// Battle Refactor Phase 1: Calculate a single damage instance
        /// </summary>
        private DamageInstance CalculateDamageInstance(
            DamageHit hit,
            SkillDef skillDef,
            BattleContext ctx,
            SkillCastOptions opts,
            bool isMonsterSkill,
            Buffs.IBuffOwner? casterBuffOwner,
            ref bool? sharedCritResult)
        {
            var instance = new DamageInstance
            {
                HitIndex = hit.HitIndex,
                ApplyAtSec = hit.DelaySec,
                SkillId = skillDef.Id,
                CasterId = opts.CasterId ?? "",
                IsCasterPlayer = !isMonsterSkill,
                BundleId = opts.BundleId
            };

            if (ctx.DamageCalculator == null || ctx.AttackerCombatStats == null)
            {
                // 没有 DamageCalculator，伤害为 0
                return instance;
            }

            // 应用 Buff 效果到属性
            var baseStats = ctx.AttackerCombatStats;
            var buffedStats = (!isMonsterSkill && casterBuffOwner != null)
                ? BuffStatApplier.ApplyBuffsToCombatStats(baseStats, casterBuffOwner)
                : baseStats;

            int attackFinal = isMonsterSkill
                ? (int)(ctx.Enemy?.BaseAttack ?? 0)
                : buffedStats.AttackFinal;

            // 创建伤害上下文
            var damageCtx = new DamageContext
            {
                AttackFinal = attackFinal,
                SkillCoef = hit.CoefAtk,
                SkillFlat = hit.Flat,
                AttackerStats = buffedStats,
                AttackerHPRatio = ctx.AttackerHPRatio,
                AttackerElement = ctx.AttackerElement ?? ElementIds.Neutral,
                DefenderElement = ctx.DefenderElement ?? ElementIds.Neutral,
                DefenderDRPct = ctx.DefenderDamageReductionPercent,
                Rng = new Random(ctx.Rng.NextRange(int.MinValue, int.MaxValue))
            };

            DamageResult damageResult;
            bool canCrit = skillDef.CanCrit;

            // 判断是否使用独立暴击判定
            // Determine whether to use independent crit roll
            if (hit.IndependentCrit)
            {
                // 独立暴击判定
                bool hasForceCrit = casterBuffOwner != null && HasForceCritEffect(casterBuffOwner);
                
                if (hasForceCrit && canCrit && !isMonsterSkill)
                {
                    damageResult = ctx.DamageCalculator.CalculateDeterministic(damageCtx, forceCrit: true);
                    // 只在第一次使用时消耗 ForceCrit buff
                    if (hit.HitIndex == 0)
                    {
                        ConsumeForceCritBuff(casterBuffOwner!);
                    }
                }
                else if (opts.ForceCrit && canCrit && !isMonsterSkill)
                {
                    damageResult = ctx.DamageCalculator.CalculateDeterministic(damageCtx, forceCrit: true);
                }
                else
                {
                    damageResult = ctx.DamageCalculator.Calculate(damageCtx);
                }
            }
            else
            {
                // 共享暴击判定（使用第一段的结果）
                if (sharedCritResult == null)
                {
                    // 第一段：进行暴击判定并保存结果
                    bool hasForceCrit = casterBuffOwner != null && HasForceCritEffect(casterBuffOwner);
                    
                    if (hasForceCrit && canCrit && !isMonsterSkill)
                    {
                        damageResult = ctx.DamageCalculator.CalculateDeterministic(damageCtx, forceCrit: true);
                        ConsumeForceCritBuff(casterBuffOwner!);
                    }
                    else if (opts.ForceCrit && canCrit && !isMonsterSkill)
                    {
                        damageResult = ctx.DamageCalculator.CalculateDeterministic(damageCtx, forceCrit: true);
                    }
                    else
                    {
                        damageResult = ctx.DamageCalculator.Calculate(damageCtx);
                    }
                    sharedCritResult = damageResult.IsCrit;
                }
                else
                {
                    // 后续段：使用共享的暴击结果
                    damageResult = ctx.DamageCalculator.CalculateDeterministic(damageCtx, forceCrit: sharedCritResult.Value);
                }
            }

            instance.Damage = damageResult.FinalDamage;
            instance.IsCrit = damageResult.IsCrit;
            instance.DetailedResult = damageResult;
            instance.HasElementAdvantage = damageResult.HasElementAdvantage;
            instance.ElementMultiplier = damageResult.ElementMultiplier;

            return instance;
        }

        /// <summary>
        /// 消耗 ForceCrit buff
        /// Consume ForceCrit buff
        /// </summary>
        private void ConsumeForceCritBuff(Buffs.IBuffOwner casterBuffOwner)
        {
            var buffsToRemove = new List<string>();
            foreach (var buff in casterBuffOwner.Buffs.Values)
            {
                if (buff.Effects.Any(e => e.Type == Buffs.BuffEffectType.ForceCrit))
                {
                    buffsToRemove.Add(buff.Id);
                    break;
                }
            }
            foreach (var buffId in buffsToRemove)
            {
                casterBuffOwner.RemoveBuff(buffId, "consumed_forcecrit");
            }
        }

        /// <summary>
        /// 成组施放多个技能
        /// Cast multiple skills as a bundle
        /// </summary>
        public IReadOnlyList<SkillCastResult> CastBundle(IReadOnlyList<string> skillIds, BattleContext ctx, SkillCastOptions opts)
        {
            // 防超限：重置计数器如果进入新的 tick
            // Prevent overflow: reset counter if entering new tick
            int nowMs = ctx.Clock.NowMs;
            if (nowMs != _lastTickTime)
            {
                _lastTickTime = nowMs;
                _currentTickCasts = 0;
            }

            var results = new List<SkillCastResult>();

            // 生成唯一的 bundleId
            // Generate unique bundleId
            string bundleId = $"bundle_{nowMs}_{_castCounter++}";
            
            // Reset counter if it exceeds threshold to prevent overflow
            // 如果计数器超过阈值则重置，防止溢出
            if (_castCounter >= CounterResetThreshold)
            {
                _castCounter = 0;
            }

            // 顺序施放技能列表
            // Cast skills in sequence
            foreach (var skillId in skillIds)
            {
                // 上限控制：每 tick 最多施放配置中的次数
                // Limit control: max casts per tick from config
                int maxCastsPerTick = _config?.MaxTriggersPerTick ?? 20;
                if (_currentTickCasts >= maxCastsPerTick)
                {
                    break;
                }

                // 创建带 bundleId 的选项
                // Create options with bundleId
                var optsWithBundle = new SkillCastOptions
                {
                    ForceCrit = opts.ForceCrit,
                    SourceTrack = opts.SourceTrack,
                    BundleId = bundleId,
                    CasterId = opts.CasterId
                };

                // 施放技能
                // Cast skill
                var result = Cast(skillId, ctx, optsWithBundle);
                results.Add(result);
                _currentTickCasts++;
            }

            return results;
        }

        // 旧系统清理：移除了 ApplyBuffEffects 和 ApplyBuffEffectsToDouble 方法
        // Legacy cleanup: Removed ApplyBuffEffects and ApplyBuffEffectsToDouble methods
        // 这些方法已被 BuffStatApplier.ApplyBuffsToCombatStats() 替代
        // These methods have been replaced by BuffStatApplier.ApplyBuffsToCombatStats()

        /// <summary>
        /// Phase 8: 检查是否有强制暴击效果
        /// Phase 8: Check if there's a force crit effect
        /// </summary>
        private bool HasForceCritEffect(Buffs.IBuffOwner buffOwner)
        {
            foreach (var buff in buffOwner.Buffs.Values)
            {
                foreach (var effect in buff.Effects)
                {
                    if (effect.Type == Buffs.BuffEffectType.ForceCrit)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Phase 3+: 将 snake_case 字符串转换为 PascalCase
        /// Phase 3+: Convert snake_case string to PascalCase
        /// </summary>
        /// <param name="snakeCase">snake_case 字符串，例如 "enemies_all"</param>
        /// <returns>PascalCase 字符串，例如 "EnemiesAll"</returns>
        private string ConvertSnakeCaseToPascalCase(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase))
                return snakeCase;

            var parts = snakeCase.Split('_');
            var result = new System.Text.StringBuilder();
            
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    // 首字母大写，其余字母小写
                    // Capitalize first letter, lowercase the rest
                    result.Append(char.ToUpper(part[0]));
                    if (part.Length > 1)
                    {
                        result.Append(part.Substring(1).ToLower());
                    }
                }
            }
            
            return result.ToString();
        }
    }
}
