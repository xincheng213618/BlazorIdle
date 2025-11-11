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
        public SkillResolver(Config.CombatConfig? config = null)
        {
            _config = config;
        }

        /// <summary>
        /// 施放单个技能
        /// Cast a single skill
        /// </summary>
        public SkillCastResult Cast(string skillId, BattleContext ctx, SkillCastOptions? opts = null)
        {
            opts ??= new SkillCastOptions();

            // 根据技能类型确定基础伤害
            // Determine base damage based on skill type
            int baseDamage = skillId switch
            {
                "attack_basic" => ctx.Player?.DamagePerAttack ?? 0,
                "special_pulse" => ctx.Player?.SpecialDamage ?? 0,
                "enemy_attack_basic" => ctx.Enemy?.DamagePerHit ?? 0,
                _ => 0
            };

            // 应用浮动
            // Apply variance
            double variancePct = skillId.StartsWith("enemy_")
                ? ctx.Enemy?.VariancePct ?? 0.0
                : ctx.Player?.VariancePct ?? 0.0;
            double dmg = Math.Floor(ctx.Rng.Jitter(baseDamage, variancePct));
            if (dmg < 1) dmg = 1;

            // 检查暴击（仅玩家攻击有暴击）
            // Check for critical hit (only player attacks can crit)
            bool isCrit = false;
            if (!skillId.StartsWith("enemy_") && ctx.Player != null)
            {
                isCrit = opts.ForceCrit || ctx.Rng.NextDouble() < (ctx.Player.CritChancePercent / 100.0);
                if (isCrit)
                {
                    dmg = Math.Floor(dmg * Math.Max(1.0, ctx.Player.CritMultiplier));
                }
            }

            return new SkillCastResult
            {
                DamageDealt = (int)dmg,
                IsCrit = isCrit,
                BundleId = opts.BundleId
            };
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
    }
}
