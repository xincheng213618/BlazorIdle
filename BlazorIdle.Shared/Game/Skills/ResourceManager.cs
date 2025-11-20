using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game;
using BlazorIdle.Game.Buffs;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 管理技能的资源消耗和获得（Phase 5: 资源消耗与冷却）
    /// Manages skill resource costs and gains (Phase 5: Resource consumption and cooldowns)
    /// </summary>
    public sealed class ResourceManager
    {
        /// <summary>
        /// 检查角色是否有足够资源施放技能
        /// Check if character has enough resources to cast skill
        /// </summary>
        /// <param name="skill">技能定义 / Skill definition</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        /// <returns>如果资源足够返回 true，否则返回 false / True if resources are sufficient, false otherwise</returns>
        public bool CheckResourceCost(SkillDef skill, BattleContext context)
        {
            if (skill == null || context?.PlayerBuffOwner == null)
                return false;

            var buckets = context.PlayerBuffOwner.Buckets;
            if (buckets == null)
                return true; // No resource system, all skills available

            // 检查新格式的资源消耗（Costs 列表）
            // Check new format resource costs (Costs list)
            if (skill.Costs != null && skill.Costs.Count > 0)
            {
                foreach (var cost in skill.Costs)
                {
                    if (!HasSufficientResource(buckets, cost.BucketId, cost.Amount))
                        return false;
                }
                return true;
            }

            // 兼容旧格式的资源消耗（ResourceCosts 字典）
            // Compatible with old format resource costs (ResourceCosts dictionary)
            if (skill.ResourceCosts != null && skill.ResourceCosts.Count > 0)
            {
                foreach (var kvp in skill.ResourceCosts)
                {
                    if (!HasSufficientResource(buckets, kvp.Key, kvp.Value))
                        return false;
                }
                return true;
            }

            // 没有资源消耗要求，视为满足
            // No resource cost requirement, considered satisfied
            return true;
        }

        /// <summary>
        /// Phase 9: 检查是否有足够资源施放技能（支持怪物）
        /// Phase 9: Check if there are enough resources to cast skill (supports monsters)
        /// </summary>
        /// <param name="skill">技能定义 / Skill definition</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        /// <param name="isCasterPlayer">施法者是否为玩家 / Is caster a player</param>
        /// <param name="casterId">施法者ID（用于怪物）/ Caster ID (for monsters)</param>
        /// <returns>如果资源足够返回 true，否则返回 false / True if resources are sufficient, false otherwise</returns>
        public bool CheckResourceCost(SkillDef skill, BattleContext context, bool isCasterPlayer, string? casterId = null)
        {
            if (skill == null || context == null)
                return false;

            // 获取施法者的 BuffOwner
            // Get caster's BuffOwner
            IBuffOwner? caster = null;
            if (isCasterPlayer)
            {
                caster = context.PlayerBuffOwner;
            }
            else
            {
                // 对于怪物，从 EnemyBuffOwners 字典中查找
                // For monsters, find from EnemyBuffOwners dictionary
                if (casterId != null && context.EnemyBuffOwners != null)
                {
                    caster = context.EnemyBuffOwners.GetValueOrDefault(casterId);
                }
            }

            if (caster == null)
                return true; // No caster found, assume no resource requirements

            var buckets = caster.Buckets;
            if (buckets == null)
                return true; // No resource system, all skills available

            // 检查新格式的资源消耗（Costs 列表）
            // Check new format resource costs (Costs list)
            if (skill.Costs != null && skill.Costs.Count > 0)
            {
                foreach (var cost in skill.Costs)
                {
                    if (!HasSufficientResource(buckets, cost.BucketId, cost.Amount))
                        return false;
                }
                return true;
            }

            // 兼容旧格式的资源消耗（ResourceCosts 字典）
            // Compatible with old format resource costs (ResourceCosts dictionary)
            if (skill.ResourceCosts != null && skill.ResourceCosts.Count > 0)
            {
                foreach (var kvp in skill.ResourceCosts)
                {
                    if (!HasSufficientResource(buckets, kvp.Key, kvp.Value))
                        return false;
                }
                return true;
            }

            // 没有资源消耗要求，视为满足
            // No resource cost requirement, considered satisfied
            return true;
        }

        /// <summary>
        /// 消耗技能所需的资源
        /// Consume resources required by the skill
        /// </summary>
        /// <param name="skill">技能定义 / Skill definition</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        /// <returns>如果成功消耗返回 true，否则返回 false / True if resources were consumed, false otherwise</returns>
        public bool ConsumeResourceCost(SkillDef skill, BattleContext context)
        {
            if (skill == null || context?.PlayerBuffOwner == null)
                return false;

            // 先检查是否有足够资源
            // First check if there are enough resources
            if (!CheckResourceCost(skill, context))
                return false;

            var buckets = context.PlayerBuffOwner.Buckets;
            if (buckets == null)
                return true; // No resource system, no consumption needed

            // 消耗新格式的资源（Costs 列表）
            // Consume new format resources (Costs list)
            if (skill.Costs != null && skill.Costs.Count > 0)
            {
                foreach (var cost in skill.Costs)
                {
                    ConsumeResource(buckets, cost.BucketId, cost.Amount);
                }
                return true;
            }

            // 消耗旧格式的资源（ResourceCosts 字典）
            // Consume old format resources (ResourceCosts dictionary)
            if (skill.ResourceCosts != null && skill.ResourceCosts.Count > 0)
            {
                foreach (var kvp in skill.ResourceCosts)
                {
                    ConsumeResource(buckets, kvp.Key, kvp.Value);
                }
                return true;
            }

            return true;
        }

        /// <summary>
        /// 应用技能的资源获得
        /// Apply resource gains from the skill
        /// </summary>
        /// <param name="skill">技能定义 / Skill definition</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        public void ApplyResourceGains(SkillDef skill, BattleContext context)
        {
            if (skill == null || context?.PlayerBuffOwner == null)
                return;

            var buckets = context.PlayerBuffOwner.Buckets;
            if (buckets == null)
                return; // No resource system, no gains

            // 应用新格式的资源获得（Gains 列表）
            // Apply new format resource gains (Gains list)
            if (skill.Gains != null && skill.Gains.Count > 0)
            {
                foreach (var gain in skill.Gains)
                {
                    GainResource(buckets, gain.BucketId, gain.Amount);
                }
            }

            // 应用旧格式的资源获得（ResourceGains 字典）
            // Apply old format resource gains (ResourceGains dictionary)
            if (skill.ResourceGains != null && skill.ResourceGains.Count > 0)
            {
                foreach (var kvp in skill.ResourceGains)
                {
                    GainResource(buckets, kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// 检查是否有足够的指定资源
        /// Check if there is sufficient specified resource
        /// </summary>
        private bool HasSufficientResource(Resources.ResourceBucketCollection buckets, string bucketId, int amount)
        {
            var bucket = buckets?.GetBucket(bucketId);
            if (bucket == null)
                return false;

            return bucket.Current >= amount;
        }

        /// <summary>
        /// 消耗指定资源
        /// Consume specified resource
        /// </summary>
        private void ConsumeResource(Resources.ResourceBucketCollection buckets, string bucketId, int amount)
        {
            var bucket = buckets?.GetBucket(bucketId);
            if (bucket == null)
                return;

            bucket.ForceConsume(amount, "skill_resource_cost");
        }

        /// <summary>
        /// 增加指定资源
        /// Add specified resource
        /// </summary>
        private void GainResource(Resources.ResourceBucketCollection buckets, string bucketId, int amount)
        {
            var bucket = buckets?.GetBucket(bucketId);
            if (bucket == null)
                return;

            bucket.Gain(amount, "skill_resource_gain");
        }
    }
}
