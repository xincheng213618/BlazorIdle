using System;
using System.Collections.Generic;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Battle.Systems
{
    /// <summary>
    /// 资源系统集成模块 - 从 MultiBattleInstance 提取的资源相关逻辑
    /// Resource system integration module - Resource-related logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 应用技能的资源消耗/获得
    /// - 记录资源变化事件
    /// </summary>
    public class ResourceIntegration
    {
        private readonly Dictionary<string, ResourceBucketCollection> _playerResources;

        /// <summary>
        /// 资源变化事件
        /// Resource change event
        /// </summary>
        public event Action<string, string, int, int, string, string?, string?>? OnResourceChange;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public ResourceIntegration(Dictionary<string, ResourceBucketCollection> playerResources)
        {
            _playerResources = playerResources ?? throw new ArgumentNullException(nameof(playerResources));
        }

        /// <summary>
        /// 应用技能结果中的资源变化
        /// Apply resource changes from skill result
        /// </summary>
        /// <param name="result">技能施放结果 / Skill cast result</param>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="isCasterPlayer">施法者是否为玩家 / Whether caster is player</param>
        /// <param name="skillId">技能ID / Skill ID</param>
        public void ApplyResourceChanges(
            SkillCastResult result,
            string casterId,
            bool isCasterPlayer,
            string? skillId = null)
        {
            if (result.ResourceChanges == null || result.ResourceChanges.Count == 0)
                return;

            // 只应用给施法者（敌人暂不支持资源系统）
            // Only apply to caster (enemies don't have resource system yet)
            if (!isCasterPlayer)
                return;

            if (!_playerResources.TryGetValue(casterId, out var resources))
                return;

            foreach (var kvp in result.ResourceChanges)
            {
                string resourceId = kvp.Key;
                int amount = kvp.Value;

                if (!resources.HasBucket(resourceId))
                    continue;

                var bucket = resources.GetBucket(resourceId);

                // 应用资源变化（正数为增加，负数为消耗）
                // Apply resource change (positive = gain, negative = cost)
                string reason;
                int actualChange;

                if (amount > 0)
                {
                    actualChange = bucket.Gain(amount, "skill_resource_gain");
                    reason = "skill_resource_gain";
                }
                else if (amount < 0)
                {
                    bucket.ForceConsume(-amount, "skill_resource_cost");
                    actualChange = amount; // Negative for cost
                    reason = "skill_resource_cost";
                }
                else
                {
                    continue; // Skip zero changes
                }

                // 触发资源变化事件
                // Trigger resource change event
                OnResourceChange?.Invoke(
                    casterId,
                    resourceId,
                    actualChange,
                    bucket.Current,
                    reason,
                    skillId,
                    result.BundleId);
            }
        }

        /// <summary>
        /// 记录普通攻击的资源获得（后备逻辑）
        /// Record resource gain from normal attack (fallback logic)
        /// </summary>
        public void RecordAttackResourceGain(
            string casterId,
            string resourceId,
            int gainPerAttack,
            int gainPerCritExtra,
            bool isCrit,
            string? skillId = null,
            string? bundleId = null)
        {
            if (!_playerResources.TryGetValue(casterId, out var resources))
                return;

            if (!resources.HasBucket(resourceId))
                return;

            var bucket = resources.GetBucket(resourceId);

            // 命中产生资源 / Hit generates resources
            int gained = bucket.Gain(gainPerAttack, "attack_hit");
            if (gained > 0)
            {
                OnResourceChange?.Invoke(
                    casterId,
                    resourceId,
                    gained,
                    bucket.Current,
                    "attack_hit",
                    skillId,
                    bundleId);
            }

            // 暴击额外产生资源 / Crit generates extra resources
            if (isCrit)
            {
                int critGain = bucket.Gain(gainPerCritExtra, "crit_bonus");
                if (critGain > 0)
                {
                    OnResourceChange?.Invoke(
                        casterId,
                        resourceId,
                        critGain,
                        bucket.Current,
                        "crit_bonus",
                        skillId,
                        bundleId);
                }
            }
        }

        /// <summary>
        /// 获取玩家资源集合
        /// Get player resource collection
        /// </summary>
        public ResourceBucketCollection? GetPlayerResources(string playerId)
        {
            return _playerResources.TryGetValue(playerId, out var resources) ? resources : null;
        }

        /// <summary>
        /// 检查玩家是否有足够资源
        /// Check if player has enough resources
        /// </summary>
        public bool HasEnoughResources(string playerId, string resourceId, int amount)
        {
            if (!_playerResources.TryGetValue(playerId, out var resources))
                return false;

            if (!resources.HasBucket(resourceId))
                return false;

            return resources.GetBucket(resourceId).Current >= amount;
        }
    }
}
