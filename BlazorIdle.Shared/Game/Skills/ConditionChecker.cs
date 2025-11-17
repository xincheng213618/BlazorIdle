using BlazorIdle.Game.Buffs;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能条件检查器 - 用于判定技能是否满足施放条件
    /// Skill condition checker - determines if skill conditions are met
    /// 
    /// Step 2 Phase 4: Skill Condition System
    /// </summary>
    public sealed class ConditionChecker
    {
        /// <summary>
        /// 检查技能的所有施放条件是否满足
        /// Check if all casting conditions for the skill are met
        /// </summary>
        /// <param name="skill">技能定义 / Skill definition</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        /// <param name="isCasterPlayer">施法者是否为玩家 / Whether the caster is a player</param>
        /// <returns>是否满足所有条件 / Whether all conditions are met</returns>
        public bool CheckConditions(SkillDef skill, BattleContext context, bool isCasterPlayer)
        {
            // 如果没有定义条件，默认满足
            // If no conditions are defined, default to satisfied
            if (skill.Conditions == null)
                return true;

            var conditions = skill.Conditions;

            // 获取施法者的 BuffOwner
            // Get caster's BuffOwner
            IBuffOwner? caster = isCasterPlayer 
                ? (IBuffOwner?)context.PlayerBuffOwner 
                : (context.Enemy != null && context.EnemyBuffOwners != null && context.Enemy.MonsterId != null
                    ? context.EnemyBuffOwners.GetValueOrDefault(context.Enemy.MonsterId)
                    : null);

            if (caster == null)
                return false; // 无法获取施法者信息，条件检查失败

            // 检查 HP 条件
            // Check HP conditions
            if (!CheckHpCondition(caster, conditions.HpBelowPct, conditions.HpAbovePct))
                return false;

            // 检查 Buff 条件
            // Check Buff conditions
            if (!CheckBuffCondition(caster, conditions.RequireBuffId, conditions.ForbidBuffId))
                return false;

            // 检查资源条件
            // Check Resource conditions
            if (!CheckResourceCondition(caster, conditions.RequireResource))
                return false;

            return true;
        }

        /// <summary>
        /// 检查 HP 百分比条件
        /// Check HP percentage conditions
        /// </summary>
        /// <param name="caster">施法者 / Caster</param>
        /// <param name="hpBelowPct">HP 必须低于此百分比（可选）/ HP must be below this percentage (optional)</param>
        /// <param name="hpAbovePct">HP 必须高于此百分比（可选）/ HP must be above this percentage (optional)</param>
        /// <returns>是否满足 HP 条件 / Whether HP conditions are met</returns>
        private bool CheckHpCondition(IBuffOwner caster, double? hpBelowPct, double? hpAbovePct)
        {
            // 计算当前 HP 百分比
            // Calculate current HP percentage
            double currentHpPct = caster.MaxHp > 0 
                ? (caster.CurrentHp * 100.0 / caster.MaxHp) 
                : 0.0;

            // 检查 HP 上限条件
            // Check HP upper bound condition
            if (hpBelowPct.HasValue && currentHpPct >= hpBelowPct.Value)
                return false;

            // 检查 HP 下限条件
            // Check HP lower bound condition
            if (hpAbovePct.HasValue && currentHpPct <= hpAbovePct.Value)
                return false;

            return true;
        }

        /// <summary>
        /// 检查 Buff 条件
        /// Check Buff conditions
        /// </summary>
        /// <param name="caster">施法者 / Caster</param>
        /// <param name="requireBuffId">必须拥有的 Buff ID（可选）/ Required buff ID (optional)</param>
        /// <param name="forbidBuffId">禁止拥有的 Buff ID（可选）/ Forbidden buff ID (optional)</param>
        /// <returns>是否满足 Buff 条件 / Whether Buff conditions are met</returns>
        private bool CheckBuffCondition(IBuffOwner caster, string? requireBuffId, string? forbidBuffId)
        {
            // 检查必须拥有的 Buff
            // Check required buff
            if (!string.IsNullOrEmpty(requireBuffId))
            {
                if (!caster.Buffs.ContainsKey(requireBuffId))
                    return false;
            }

            // 检查禁止拥有的 Buff
            // Check forbidden buff
            if (!string.IsNullOrEmpty(forbidBuffId))
            {
                if (caster.Buffs.ContainsKey(forbidBuffId))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 检查资源条件
        /// Check Resource conditions
        /// </summary>
        /// <param name="caster">施法者 / Caster</param>
        /// <param name="requireResource">资源要求字典 (bucketId -> minAmount)（可选）/ Resource requirements dictionary (optional)</param>
        /// <returns>是否满足资源条件 / Whether Resource conditions are met</returns>
        private bool CheckResourceCondition(IBuffOwner caster, Dictionary<string, int>? requireResource)
        {
            // 如果没有资源要求，直接通过
            // If no resource requirements, pass directly
            if (requireResource == null || requireResource.Count == 0)
                return true;

            // 如果施法者没有资源系统，无法满足条件
            // If caster has no resource system, cannot satisfy conditions
            if (caster.Buckets == null)
                return false;

            // 检查每个资源要求
            // Check each resource requirement
            foreach (var (bucketId, minAmount) in requireResource)
            {
                var bucket = caster.Buckets.GetBucket(bucketId);
                if (bucket == null)
                    return false; // 资源桶不存在

                if (bucket.Current < minAmount)
                    return false; // 资源不足
            }

            return true;
        }
    }
}
