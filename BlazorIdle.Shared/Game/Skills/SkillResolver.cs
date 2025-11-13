using System;
using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能解析器基础实现 - 统一处理技能效果
    /// Basic skill resolver implementation - unified skill effect processing
    /// </summary>
    public sealed class SkillResolver : ISkillResolver
    {
        private readonly Config.CombatConfig? _config;
        private readonly SkillRepository _skillRepository;
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
        public SkillResolver(Config.CombatConfig? config = null, SkillRepository? skillRepository = null)
        {
            _config = config;
            _skillRepository = skillRepository ?? new SkillRepository();
        }

        /// <summary>
        /// 施放单个技能（Phase 5: 支持 Buff 操作，Phase 8: 应用 Buff 效果到属性计算）
        /// Cast a single skill (Phase 5: Supports buff operations, Phase 8: Apply buff effects to stat calculations)
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
            Buffs.IBuffOwner? casterBuffOwner = null;
            if (!skillId.StartsWith("enemy_") && ctx.PlayerBuffOwner != null)
            {
                casterBuffOwner = ctx.PlayerBuffOwner;
            }

            // 根据技能类型确定基础伤害
            // Determine base damage based on skill type
            int baseDamage = skillId switch
            {
                SkillIds.AttackBasic => ctx.Player?.DamagePerAttack ?? 0,
                SkillIds.SpecialPulse => ctx.Player?.SpecialDamage ?? 0,
                SkillIds.EnemyAttackBasic => ctx.Enemy?.DamagePerHit ?? 0,
                _ => 0
            };

            // Phase 8: 应用 Buff 效果到基础伤害
            // Phase 8: Apply buff effects to base damage
            if (casterBuffOwner != null && skillId == SkillIds.AttackBasic)
            {
                baseDamage = ApplyBuffEffects(baseDamage, "DamagePerAttack", casterBuffOwner);
            }
            else if (casterBuffOwner != null && skillId == SkillIds.SpecialPulse)
            {
                baseDamage = ApplyBuffEffects(baseDamage, "SpecialDamage", casterBuffOwner);
            }

            // Phase 5: 应用技能的伤害倍率
            // Phase 5: Apply skill damage multiplier
            if (skillDef != null)
            {
                baseDamage = (int)(baseDamage * skillDef.DamageMultiplier);
            }

            // 应用浮动
            // Apply variance
            double variancePct = skillId.StartsWith("enemy_")
                ? ctx.Enemy?.VariancePct ?? 0.0
                : ctx.Player?.VariancePct ?? 0.0;
            double dmg = Math.Floor(ctx.Rng.Jitter(baseDamage, variancePct));
            if (dmg < 1) dmg = 1;

            // Phase 8: 检查是否有 ForceCrit 效果
            // Phase 8: Check for ForceCrit effect
            bool hasForceCrit = casterBuffOwner != null && HasForceCritEffect(casterBuffOwner);

            // 检查暴击（仅玩家攻击有暴击）
            // Check for critical hit (only player attacks can crit)
            bool isCrit = false;
            bool canCrit = skillDef?.CanCrit ?? true;
            if (!skillId.StartsWith("enemy_") && ctx.Player != null && canCrit)
            {
                // Phase 8: ForceCrit 优先级最高
                // Phase 8: ForceCrit has highest priority
                if (hasForceCrit)
                {
                    isCrit = true;
                    // 消耗 ForceCrit buff（在下一次攻击后会被移除）
                    // Consume ForceCrit buff (will be removed after next attack)
                }
                else
                {
                    // Phase 8: 应用 Buff 效果到暴击率
                    // Phase 8: Apply buff effects to crit chance
                    double critChance = ctx.Player.CritChancePercent;
                    if (casterBuffOwner != null)
                    {
                        critChance = ApplyBuffEffectsToDouble(critChance, "CritChancePercent", casterBuffOwner);
                    }
                    
                    isCrit = opts.ForceCrit || ctx.Rng.NextDouble() < (critChance / 100.0);
                }
                
                if (isCrit)
                {
                    // Phase 8: 应用 Buff 效果到暴击倍率
                    // Phase 8: Apply buff effects to crit multiplier
                    double critMultiplier = ctx.Player.CritMultiplier;
                    if (casterBuffOwner != null)
                    {
                        critMultiplier = ApplyBuffEffectsToDouble(critMultiplier, "CritMultiplier", casterBuffOwner);
                    }
                    
                    dmg = Math.Floor(dmg * Math.Max(1.0, critMultiplier));
                }
            }

            // Phase 5: 创建结果并添加 buff 操作
            // Phase 5: Create result and add buff operations
            var result = new SkillCastResult
            {
                DamageDealt = (int)dmg,
                IsCrit = isCrit,
                BundleId = opts.BundleId,
                InstantHeal = skillDef?.InstantHeal ?? 0
            };

            // Phase 5: 添加 OnCast buff 操作
            // Phase 5: Add OnCast buff operations
            if (skillDef != null)
            {
                result.BuffOperations.AddRange(skillDef.OnCastBuffs);

                // 添加 OnHit buff 操作
                // Add OnHit buff operations
                // 注意：当前 Step 0 设计中技能总是命中，AlwaysHits=true 表示必定命中
                // Note: In current Step 0 design, skills always hit, AlwaysHits=true means guaranteed hit
                // TODO Phase 6: 实现命中率检查，当 AlwaysHits=false 时需要滚动命中判定
                // TODO Phase 6: Implement hit chance check when AlwaysHits=false
                bool skillHits = skillDef.AlwaysHits || true; // Currently always hits in Step 0
                if (skillHits)
                {
                    result.BuffOperations.AddRange(skillDef.OnHitBuffs);
                }

                // 添加 OnCrit buff 操作
                // Add OnCrit buff operations
                if (isCrit)
                {
                    result.BuffOperations.AddRange(skillDef.OnCritBuffs);
                }

                // Phase 5: 添加资源消耗和获得到结果中
                // Phase 5: Add resource costs and gains to result
                foreach (var (resId, cost) in skillDef.ResourceCosts)
                {
                    result.ResourceChanges[resId] = -cost; // 负数表示消耗 / negative means cost
                }
                foreach (var (resId, gain) in skillDef.ResourceGains)
                {
                    // 如果已经有消耗，则累加；否则直接设置
                    // If already has cost, accumulate; otherwise set directly
                    if (result.ResourceChanges.ContainsKey(resId))
                    {
                        result.ResourceChanges[resId] += gain;
                    }
                    else
                    {
                        result.ResourceChanges[resId] = gain;
                    }
                }
            }

            return result;
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
                    BundleId = bundleId
                };

                // 施放技能
                // Cast skill
                var result = Cast(skillId, ctx, optsWithBundle);
                results.Add(result);
                _currentTickCasts++;
            }

            return results;
        }

        /// <summary>
        /// Phase 8: 应用 Buff 效果到整数属性
        /// Phase 8: Apply buff effects to integer attributes
        /// </summary>
        private int ApplyBuffEffects(int baseValue, string statName, Buffs.IBuffOwner buffOwner)
        {
            double modifiedValue = baseValue;

            foreach (var buff in buffOwner.Buffs.Values)
            {
                foreach (var effect in buff.Effects)
                {
                    // 只处理影响指定属性的效果
                    // Only process effects targeting the specified stat
                    if (effect.Target != statName)
                        continue;

                    switch (effect.Type)
                    {
                        case Buffs.BuffEffectType.StatMultiplier:
                            // 倍率效果：基础值 * (1 + value)
                            // Multiplier effect: base * (1 + value)
                            // value 为 0.15 表示 +15%
                            // value of 0.15 means +15%
                            modifiedValue *= (1.0 + effect.Value);
                            break;

                        case Buffs.BuffEffectType.StatAdditive:
                            // 加法效果：直接加上数值
                            // Additive effect: directly add value
                            modifiedValue += effect.Value;
                            break;

                        case Buffs.BuffEffectType.StatReduction:
                            // 减益效果：基础值 * (1 - value)
                            // Reduction effect: base * (1 - value)
                            // value 为 0.10 表示 -10%
                            // value of 0.10 means -10%
                            modifiedValue *= (1.0 - effect.Value);
                            break;
                    }
                }
            }

            return (int)Math.Floor(modifiedValue);
        }

        /// <summary>
        /// Phase 8: 应用 Buff 效果到浮点数属性
        /// Phase 8: Apply buff effects to double attributes
        /// </summary>
        private double ApplyBuffEffectsToDouble(double baseValue, string statName, Buffs.IBuffOwner buffOwner)
        {
            double modifiedValue = baseValue;

            foreach (var buff in buffOwner.Buffs.Values)
            {
                foreach (var effect in buff.Effects)
                {
                    // 只处理影响指定属性的效果
                    // Only process effects targeting the specified stat
                    if (effect.Target != statName)
                        continue;

                    switch (effect.Type)
                    {
                        case Buffs.BuffEffectType.StatMultiplier:
                            // 倍率效果：基础值 * (1 + value)
                            // Multiplier effect: base * (1 + value)
                            modifiedValue *= (1.0 + effect.Value);
                            break;

                        case Buffs.BuffEffectType.StatAdditive:
                            // 加法效果：直接加上数值
                            // Additive effect: directly add value
                            modifiedValue += effect.Value;
                            break;

                        case Buffs.BuffEffectType.StatReduction:
                            // 减益效果：基础值 * (1 - value)
                            // Reduction effect: base * (1 - value)
                            modifiedValue *= (1.0 - effect.Value);
                            break;
                    }
                }
            }

            return modifiedValue;
        }

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
    }
}
