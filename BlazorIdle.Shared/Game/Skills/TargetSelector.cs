using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 目标选择器 (Step 2 Phase 3)
    /// Target selector
    /// </summary>
    public sealed class TargetSelector
    {
        /// <summary>
        /// 解析目标选择策略，返回目标ID列表
        /// Resolve target selection policy, returns list of target IDs
        /// </summary>
        /// <param name="policy">目标选择策略</param>
        /// <param name="context">战斗上下文</param>
        /// <param name="casterId">施法者ID（用于Self策略）</param>
        /// <param name="currentTargetId">当前普攻目标ID（用于CurrentTarget策略）</param>
        /// <returns>目标ID列表</returns>
        public List<string> ResolveTargets(
            TargetPolicy policy,
            BattleContext context,
            string? casterId = null,
            string? currentTargetId = null)
        {
            return policy switch
            {
                TargetPolicy.CurrentTarget => ResolveCurrentTarget(currentTargetId),
                TargetPolicy.EnemiesAll => ResolveEnemiesAll(context),
                TargetPolicy.AlliesLowestHpPct => ResolveAlliesLowestHpPct(context),
                TargetPolicy.Self => ResolveSelf(casterId),
                TargetPolicy.AlliesAll => ResolveAlliesAll(context),
                _ => throw new ArgumentException($"Unknown target policy: {policy}", nameof(policy))
            };
        }

        /// <summary>
        /// 当前目标策略 - 返回当前普攻目标
        /// Current target policy - returns current basic attack target
        /// </summary>
        private List<string> ResolveCurrentTarget(string? currentTargetId)
        {
            if (string.IsNullOrEmpty(currentTargetId))
            {
                // 没有当前目标，返回空列表
                // No current target, return empty list
                return new List<string>();
            }

            return new List<string> { currentTargetId };
        }

        /// <summary>
        /// 所有敌人策略 - 返回所有存活的敌人
        /// All enemies policy - returns all alive enemies
        /// </summary>
        private List<string> ResolveEnemiesAll(BattleContext context)
        {
            if (context.EnemyTeam == null)
            {
                // 没有敌人队伍，返回空列表
                // No enemy team, return empty list
                return new List<string>();
            }

            return context.EnemyTeam.GetAliveMemberIds();
        }

        /// <summary>
        /// HP百分比最低的友方策略 - 返回HP百分比最低的友方单位
        /// Lowest HP percentage ally policy - returns ally with lowest HP percentage
        /// </summary>
        private List<string> ResolveAlliesLowestHpPct(BattleContext context)
        {
            if (context.PlayerTeam == null)
            {
                // 没有友方队伍，返回空列表
                // No player team, return empty list
                return new List<string>();
            }

            var targetId = context.PlayerTeam.GetLowestHpPercentMemberId();
            if (targetId == null)
            {
                // 没有存活的友方，返回空列表
                // No alive allies, return empty list
                return new List<string>();
            }

            return new List<string> { targetId };
        }

        /// <summary>
        /// 自身策略 - 返回施法者自己
        /// Self policy - returns the caster itself
        /// </summary>
        private List<string> ResolveSelf(string? casterId)
        {
            if (string.IsNullOrEmpty(casterId))
            {
                // 没有施法者ID，返回空列表
                // No caster ID, return empty list
                return new List<string>();
            }

            return new List<string> { casterId };
        }

        /// <summary>
        /// 所有友方策略 - 返回所有存活的友方
        /// All allies policy - returns all alive allies
        /// </summary>
        private List<string> ResolveAlliesAll(BattleContext context)
        {
            if (context.PlayerTeam == null)
            {
                // 没有友方队伍，返回空列表
                // No player team, return empty list
                return new List<string>();
            }

            return context.PlayerTeam.GetAliveMemberIds();
        }
    }
}
