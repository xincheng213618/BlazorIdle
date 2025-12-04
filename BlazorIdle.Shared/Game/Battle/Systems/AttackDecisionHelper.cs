using System.Collections.Generic;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Resources;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Battle.Systems
{
    /// <summary>
    /// 攻击决策辅助类 - 从 MultiBattleInstance 提取的攻击决策逻辑
    /// Attack decision helper - Attack decision logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 构建战斗上下文
    /// - 准备攻击决策所需数据
    /// </summary>
    public static class AttackDecisionHelper
    {
        /// <summary>
        /// 攻击决策数据
        /// Attack decision data
        /// </summary>
        public class AttackDecisionData
        {
            /// <summary>
            /// 角色数据（可为 null）
            /// Character data (may be null)
            /// </summary>
            public CharacterData? CharacterData { get; set; }

            /// <summary>
            /// 战斗上下文
            /// Battle context
            /// </summary>
            public BattleContext? Context { get; set; }

            /// <summary>
            /// 普攻技能 ID
            /// Normal attack skill ID
            /// </summary>
            public string? NormalAttackSkillId { get; set; }

            /// <summary>
            /// 普攻技能定义
            /// Normal attack skill definition
            /// </summary>
            public SkillDef? NormalAttackSkill { get; set; }

            /// <summary>
            /// 普攻是否为 GCD
            /// Whether normal attack is GCD
            /// </summary>
            public bool NormalAttackIsGcd { get; set; } = true;

            /// <summary>
            /// 是否有角色数据
            /// Whether has character data
            /// </summary>
            public bool HasCharacterData => CharacterData != null;
        }

        /// <summary>
        /// 准备攻击决策数据
        /// Prepare attack decision data
        /// </summary>
        /// <param name="charId">角色 ID / Character ID</param>
        /// <param name="character">角色 / Character</param>
        /// <param name="characterDataMap">角色数据映射 / Character data map</param>
        /// <param name="playerBuffOwners">玩家 Buff 拥有者 / Player buff owners</param>
        /// <param name="playerResources">玩家资源 / Player resources</param>
        /// <param name="rng">随机数上下文 / Random number context</param>
        /// <param name="clock">游戏时钟 / Game clock</param>
        /// <param name="currentTargetId">当前目标 ID / Current target ID</param>
        /// <param name="skillRepository">技能仓库 / Skill repository</param>
        /// <returns>攻击决策数据 / Attack decision data</returns>
        public static AttackDecisionData PrepareAttackDecisionData(
            string charId,
            Character character,
            Dictionary<string, CharacterData>? characterDataMap,
            Dictionary<string, CharacterBuffOwner> playerBuffOwners,
            Dictionary<string, ResourceBucketCollection> playerResources,
            RngContext rng,
            IGameClock clock,
            string? currentTargetId,
            SkillRepository skillRepository)
        {
            var data = new AttackDecisionData();

            // 获取 CharacterData（如果可用）
            // Get CharacterData (if available)
            if (characterDataMap != null && characterDataMap.TryGetValue(charId, out var charData))
            {
                data.CharacterData = charData;
            }

            // 构建战斗上下文
            // Build battle context
            data.Context = new BattleContext
            {
                Player = character,
                PlayerBuffOwner = playerBuffOwners.GetValueOrDefault(charId),
                PlayerResources = playerResources.GetValueOrDefault(charId),
                Rng = rng,
                Clock = clock,
                CurrentTargetId = currentTargetId
            };

            // 获取普攻技能
            // Get normal attack skill
            data.NormalAttackSkillId = character.GetNormalAttackSkillId();
            data.NormalAttackSkill = skillRepository.GetSkill(data.NormalAttackSkillId);
            data.NormalAttackIsGcd = data.NormalAttackSkill?.IsGcd ?? true;

            return data;
        }
    }
}
