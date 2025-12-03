using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Battle.Systems
{
    /// <summary>
    /// 触发器系统集成模块 - 从 MultiBattleInstance 提取的触发器相关逻辑
    /// Trigger system integration module - Trigger-related logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 处理攻击触发器 (OnAttackHit, OnAttackCrit)
    /// - 处理窗口触发器 (OnPostAttackWindow, OnPostCastWindow)
    /// </summary>
    public class TriggerIntegration
    {
        private readonly SkillRepository _skillRepository;
        private readonly TriggerProcessor _triggerProcessor;

        /// <summary>
        /// 触发技能执行请求事件
        /// Trigger skill execution request event
        /// </summary>
        public event Action<string, string, bool, EventSource>? OnExecuteSkillRequested;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public TriggerIntegration(SkillRepository skillRepository, TriggerProcessor triggerProcessor)
        {
            _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
            _triggerProcessor = triggerProcessor ?? throw new ArgumentNullException(nameof(triggerProcessor));
        }

        /// <summary>
        /// 处理攻击触发器 (OnAttackHit, OnAttackCrit)
        /// Process attack triggers (OnAttackHit, OnAttackCrit)
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="skillId">触发技能ID / Triggering skill ID</param>
        /// <param name="isCrit">是否暴击 / Whether it's a crit</param>
        /// <param name="isCasterPlayer">施法者是否为玩家 / Whether caster is player</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        /// <param name="characterData">角色数据（玩家专用）/ Character data (player only)</param>
        /// <param name="professionId">职业ID / Profession ID</param>
        public void ProcessAttackTriggers(
            string casterId,
            string? skillId,
            bool isCrit,
            bool isCasterPlayer,
            BattleContext context,
            CharacterData? characterData = null,
            string? professionId = null)
        {
            // 获取源技能定义（如果有）/ Get source skill definition (if any)
            SkillDef? sourceSkill = skillId != null ? _skillRepository.GetSkill(skillId) : null;

            // 处理 OnAttackHit 触发 / Process OnAttackHit triggers
            var hitTriggers = _triggerProcessor.ProcessTriggers(
                "OnAttackHit",
                casterId,
                sourceSkill,
                context,
                isCasterPlayer,
                characterData,
                professionId,
                wasCrit: isCrit);

            // 如果是暴击，处理 OnAttackCrit 触发 / If crit, process OnAttackCrit triggers
            List<SkillDef> critTriggers = new List<SkillDef>();
            if (isCrit)
            {
                critTriggers = _triggerProcessor.ProcessTriggers(
                    "OnAttackCrit",
                    casterId,
                    sourceSkill,
                    context,
                    isCasterPlayer,
                    characterData,
                    professionId,
                    wasCrit: true);
            }

            // 执行所有触发的技能 / Execute all triggered skills
            foreach (var triggeredSkill in hitTriggers.Concat(critTriggers))
            {
                OnExecuteSkillRequested?.Invoke(casterId, triggeredSkill.Id, isCasterPlayer, EventSource.Trigger);
            }
        }

        /// <summary>
        /// 处理窗口触发器 (OnPostAttackWindow, OnPostCastWindow)
        /// Process window triggers (OnPostAttackWindow, OnPostCastWindow)
        /// </summary>
        /// <param name="casterId">施法者ID / Caster ID</param>
        /// <param name="windowType">窗口类型 / Window type</param>
        /// <param name="sourceSkillId">源技能ID / Source skill ID</param>
        /// <param name="isCasterPlayer">施法者是否为玩家 / Whether caster is player</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        /// <param name="characterData">角色数据（玩家专用）/ Character data (player only)</param>
        /// <param name="professionId">职业ID / Profession ID</param>
        public void ProcessWindowTriggers(
            string casterId,
            string windowType,
            string? sourceSkillId,
            bool isCasterPlayer,
            BattleContext context,
            CharacterData? characterData = null,
            string? professionId = null)
        {
            // 怪物目前不使用窗口触发 / Monsters don't currently use window triggers
            if (!isCasterPlayer)
                return;

            // 获取源技能定义（如果有）/ Get source skill definition (if any)
            SkillDef? sourceSkill = sourceSkillId != null ? _skillRepository.GetSkill(sourceSkillId) : null;

            // 处理窗口触发 / Process window triggers
            var triggers = _triggerProcessor.ProcessTriggers(
                windowType,
                casterId,
                sourceSkill,
                context,
                isCasterPlayer,
                characterData,
                professionId,
                wasCrit: false);

            // 执行所有触发的技能 / Execute all triggered skills
            foreach (var triggeredSkill in triggers)
            {
                EventSource eventSource = windowType == "OnPostAttackWindow" 
                    ? EventSource.PostAttack 
                    : EventSource.PostCast;
                OnExecuteSkillRequested?.Invoke(casterId, triggeredSkill.Id, isCasterPlayer, eventSource);
            }
        }
    }
}
