using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Tracks;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Battle.Systems
{
    /// <summary>
    /// 施法系统集成模块 - 从 MultiBattleInstance 提取的施法相关逻辑
    /// Casting system integration module - Casting-related logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 计算急速加成 (CalculateHastePercent)
    /// - 处理玩家施法完成后的窗口技能 (ProcessPostCastWindow)
    /// - 处理怪物施法完成后的窗口技能 (ProcessMonsterPostCastWindow)
    /// - 选择玩家的施法技能 (SelectCastSkill)
    /// - 选择怪物的施法技能 (SelectMonsterCastSkill)
    /// 
    /// 注意：轨道暂停/恢复和施法控制器管理保留在 MultiBattleInstance 中，
    /// 因为这些需要直接访问轨道字典和施法控制器事件。
    /// </summary>
    public class CastingIntegration
    {
        private readonly IGameClock _clock;
        private readonly SkillRepository _skillRepository;
        private readonly WindowExecutor _windowExecutor;
        private readonly AutoCastEngine _autoCastEngine;

        /// <summary>
        /// 技能执行请求事件 (casterId, skillId, isCasterPlayer, eventSource)
        /// Skill execution request event
        /// </summary>
        public event Action<string, string, bool, EventSource>? OnExecuteSkillRequested;

        /// <summary>
        /// 窗口触发器请求事件 (casterId, windowType, sourceSkillId, isCasterPlayer)
        /// Window trigger request event
        /// </summary>
        public event Action<string, string, string, bool>? OnWindowTriggerRequested;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public CastingIntegration(
            IGameClock clock,
            SkillRepository skillRepository,
            WindowExecutor windowExecutor,
            AutoCastEngine autoCastEngine)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
            _windowExecutor = windowExecutor ?? throw new ArgumentNullException(nameof(windowExecutor));
            _autoCastEngine = autoCastEngine ?? throw new ArgumentNullException(nameof(autoCastEngine));
        }

        /// <summary>
        /// 选择玩家的施法技能
        /// Select a cast skill for player
        /// </summary>
        /// <returns>可用的施法技能，如果没有则返回 null</returns>
        public SkillDef? SelectCastSkill(
            string charId,
            Character character,
            CharacterData? characterData,
            BattleContext context)
        {
            if (characterData == null)
                return null;

            var castSkills = _windowExecutor.ExecuteWindow(
                WindowType.PreAttack,
                charId,
                characterData,
                character.ActiveCombatProfessionId,
                context,
                gcdAlreadyUsed: false);

            var castSkill = castSkills.FirstOrDefault();
            if (castSkill != null && castSkill.CastTimeSec > 0)
            {
                return castSkill;
            }

            return null;
        }

        /// <summary>
        /// 选择怪物的施法技能
        /// Select a cast skill for monster
        /// </summary>
        /// <returns>可用的施法技能，如果没有则返回 null</returns>
        public SkillDef? SelectMonsterCastSkill(
            string enemyId,
            Enemy enemy,
            BattleContext context)
        {
            var castSkill = _autoCastEngine.SelectMonsterCastSkill(enemyId, enemy, enemyId, context);
            if (castSkill != null && castSkill.CastTimeSec > 0)
            {
                return castSkill;
            }

            return null;
        }

        /// <summary>
        /// 计算急速百分比（包含 Buff 效果）
        /// Calculate haste percentage (including buff effects)
        /// </summary>
        public double CalculateHastePercent(Character character, IBuffOwner? buffOwner)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
                
            double hastePercent = character.HastePercent;

            if (buffOwner is CharacterBuffOwner charBuffOwner)
            {
                // 按应用时间排序 Buff
                var sortedBuffs = charBuffOwner.Buffs.Values
                    .OrderBy(b => b.AppliedAtMs)
                    .ToList();

                foreach (var buff in sortedBuffs)
                {
                    foreach (var effect in buff.Effects)
                    {
                        if (effect.Target != "HastePercent") continue;

                        switch (effect.Type)
                        {
                            case BuffEffectType.StatMultiplier:
                                hastePercent *= (1.0 + effect.Value);
                                break;
                            case BuffEffectType.StatAdditive:
                                hastePercent += effect.Value;
                                break;
                            case BuffEffectType.StatReduction:
                                hastePercent *= (1.0 - effect.Value);
                                break;
                        }
                    }
                }
            }

            return hastePercent;
        }

        /// <summary>
        /// 处理玩家施法完成后的窗口技能
        /// Process player's post-cast window skills
        /// </summary>
        public void ProcessPostCastWindow(
            string casterId,
            string skillId,
            Character character,
            CharacterData? characterData,
            BattleContext context)
        {
            var skillDef = _skillRepository.GetSkill(skillId);
            if (skillDef == null) return;

            // 执行施法技能效果
            OnExecuteSkillRequested?.Invoke(casterId, skillId, true, EventSource.Cast);

            // PostCast 窗口：施法完成后执行瞬发技能
            if (characterData != null)
            {
                bool castSkillIsGcd = skillDef.IsGcd;
                var postCastSkills = _windowExecutor.ExecuteWindow(
                    WindowType.PostCast,
                    casterId,
                    characterData,
                    character.ActiveCombatProfessionId,
                    context,
                    castSkillIsGcd);

                foreach (var skill in postCastSkills)
                {
                    OnExecuteSkillRequested?.Invoke(casterId, skill.Id, true, EventSource.PostCast);
                }

                // PostCast 窗口触发器
                OnWindowTriggerRequested?.Invoke(casterId, "OnPostCastWindow", skillId, true);
            }
        }

        /// <summary>
        /// 处理怪物施法完成后的窗口技能
        /// Process monster's post-cast window skills
        /// </summary>
        public void ProcessMonsterPostCastWindow(
            string monsterId,
            string skillId,
            Enemy enemy,
            BattleContext context)
        {
            var skillDef = _skillRepository.GetSkill(skillId);
            if (skillDef == null) return;

            // 执行施法技能效果
            OnExecuteSkillRequested?.Invoke(monsterId, skillId, false, EventSource.Cast);

            // PostCast 窗口：怪物施法完成后执行瞬发技能
            bool castSkillIsGcd = skillDef.IsGcd;
            var postCastSkills = _autoCastEngine.ExecuteMonsterWindow(enemy, monsterId, context, castSkillIsGcd, "PostCast");

            foreach (var skill in postCastSkills)
            {
                OnExecuteSkillRequested?.Invoke(monsterId, skill.Id, false, EventSource.PostCast);
            }

            // PostCast 窗口触发器
            OnWindowTriggerRequested?.Invoke(monsterId, "OnPostCastWindow", skillId, false);
        }
    }
}
