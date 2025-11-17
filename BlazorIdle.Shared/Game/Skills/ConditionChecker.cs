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
        /// <param name="casterId">施法者ID（可选，用于查找 BuffOwner）/ Caster ID (optional, for finding BuffOwner)</param>
        /// <returns>是否满足所有条件 / Whether all conditions are met</returns>
        public bool CheckConditions(SkillDef skill, BattleContext context, bool isCasterPlayer, string? casterId = null)
        {
            // 如果没有定义条件，默认满足
            // If no conditions are defined, default to satisfied
            if (skill.Conditions == null)
                return true;

            var conditions = skill.Conditions;

            // 检查 HP 条件（可以直接从 Character/Enemy 获取，不需要 BuffOwner）
            // Check HP conditions (can get directly from Character/Enemy, don't need BuffOwner)
            if (!CheckHpConditionFromContext(context, isCasterPlayer, conditions.HpBelowPct, conditions.HpAbovePct))
                return false;

            // 获取施法者的 BuffOwner（用于 Buff 和资源条件）
            // Get caster's BuffOwner (for Buff and resource conditions)
            IBuffOwner? caster = null;
            if (isCasterPlayer)
            {
                caster = context.PlayerBuffOwner;
            }
            else
            {
                // 对于怪物，优先使用 casterId 查找 BuffOwner
                // For monsters, prefer using casterId to find BuffOwner
                if (casterId != null && context.EnemyBuffOwners != null)
                {
                    caster = context.EnemyBuffOwners.GetValueOrDefault(casterId);
                }
                // 后备：尝试使用 Enemy.MonsterId
                // Fallback: try using Enemy.MonsterId
                else if (context.Enemy != null && context.EnemyBuffOwners != null && context.Enemy.MonsterId != null)
                {
                    caster = context.EnemyBuffOwners.GetValueOrDefault(context.Enemy.MonsterId);
                }
            }

            // 检查 Buff 条件（需要 BuffOwner）
            // Check Buff conditions (need BuffOwner)
            if (conditions.RequireBuffId != null || conditions.ForbidBuffId != null)
            {
                if (caster == null)
                    return false; // 需要 BuffOwner 但无法获取
                
                if (!CheckBuffCondition(caster, conditions.RequireBuffId, conditions.ForbidBuffId))
                    return false;
            }

            // 检查资源条件（需要 BuffOwner）
            // Check Resource conditions (need BuffOwner)
            if (conditions.RequireResource != null && conditions.RequireResource.Count > 0)
            {
                if (caster == null)
                    return false; // 需要 BuffOwner 但无法获取
                
                if (!CheckResourceCondition(caster, conditions.RequireResource))
                    return false;
            }

            // 检查 Buff 层数条件（需要 BuffOwner）
            // Check Buff stack conditions (need BuffOwner)
            if (conditions.RequireBuffStacks != null && conditions.RequireBuffStacks.Count > 0)
            {
                if (caster == null)
                    return false; // 需要 BuffOwner 但无法获取
                
                if (!CheckBuffStackCondition(caster, conditions.RequireBuffStacks))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 从战斗上下文检查 HP 百分比条件
        /// Check HP percentage conditions from battle context
        /// </summary>
        /// <param name="context">战斗上下文 / Battle context</param>
        /// <param name="isCasterPlayer">施法者是否为玩家 / Whether the caster is a player</param>
        /// <param name="hpBelowPct">HP 必须低于此百分比（可选）/ HP must be below this percentage (optional)</param>
        /// <param name="hpAbovePct">HP 必须高于此百分比（可选）/ HP must be above this percentage (optional)</param>
        /// <returns>是否满足 HP 条件 / Whether HP conditions are met</returns>
        private bool CheckHpConditionFromContext(BattleContext context, bool isCasterPlayer, double? hpBelowPct, double? hpAbovePct)
        {
            // 如果没有 HP 条件，直接通过
            // If no HP conditions, pass directly
            if (!hpBelowPct.HasValue && !hpAbovePct.HasValue)
                return true;

            // 获取施法者的当前 HP 和最大 HP
            // Get caster's current HP and max HP
            int currentHp, maxHp;
            if (isCasterPlayer)
            {
                if (context.Player == null)
                    return false;
                currentHp = context.Player.Hp;
                maxHp = context.Player.MaxHp;
            }
            else
            {
                if (context.Enemy == null)
                    return false;
                currentHp = context.Enemy.Hp;
                maxHp = context.Enemy.MaxHp;
            }

            // 计算当前 HP 百分比
            // Calculate current HP percentage
            double currentHpPct = maxHp > 0 
                ? (currentHp * 100.0 / maxHp) 
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

        /// <summary>
        /// 检查 Buff 层数条件
        /// Check Buff stack conditions
        /// </summary>
        /// <param name="caster">施法者 / Caster</param>
        /// <param name="requireBuffStacks">Buff层数要求字典 (buffId -> minStacks)（可选）/ Required buff stacks dictionary (optional)</param>
        /// <returns>是否满足 Buff 层数条件 / Whether Buff stack conditions are met</returns>
        private bool CheckBuffStackCondition(IBuffOwner caster, Dictionary<string, int>? requireBuffStacks)
        {
            // 如果没有 Buff 层数要求，直接通过
            // If no buff stack requirements, pass directly
            if (requireBuffStacks == null || requireBuffStacks.Count == 0)
                return true;

            // 检查每个 Buff 层数要求
            // Check each buff stack requirement
            foreach (var (buffId, minStacks) in requireBuffStacks)
            {
                // 检查 Buff 是否存在
                // Check if buff exists
                if (!caster.Buffs.TryGetValue(buffId, out var buffInstance))
                    return false; // Buff 不存在

                // 检查层数是否满足要求
                // Check if stack count meets requirement
                if (buffInstance.Stacks < minStacks)
                    return false; // 层数不足
            }

            return true;
        }
    }
}
