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
    /// - Battle Refactor Phase 6.3: 准备施法数据
    ///   - PreparePlayerCasting() - 准备玩家施法数据（技能选择、急速计算、实际施法时间）
    ///   - PrepareMonsterCasting() - 准备怪物施法数据
    ///   - GetSkillDef() - 获取技能定义
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

        #region Battle Refactor Phase 6.3: 施法开始流程

        /// <summary>
        /// Battle Refactor Phase 6.3: 尝试为玩家开始施法的数据准备
        /// Battle Refactor Phase 6.3: Prepare data for starting player casting
        /// </summary>
        /// <param name="charId">角色ID / Character ID</param>
        /// <param name="character">角色 / Character</param>
        /// <param name="characterData">角色数据 / Character data</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        /// <param name="buffOwner">Buff所有者 / Buff owner</param>
        /// <returns>
        /// 施法数据元组：(成功, 技能定义, 急速百分比, 实际施法时间)
        /// Casting data tuple: (success, skill definition, haste percent, actual cast time)
        /// </returns>
        public (bool Success, SkillDef? Skill, double HastePercent, double ActualCastTime) PreparePlayerCasting(
            string charId,
            Character character,
            CharacterData? characterData,
            BattleContext context,
            IBuffOwner? buffOwner)
        {
            if (characterData == null)
                return (false, null, 0, 0);

            // 选择施法技能
            // Select cast skill
            var castSkill = SelectCastSkill(charId, character, characterData, context);
            if (castSkill == null)
                return (false, null, 0, 0);

            // 计算急速
            // Calculate haste
            double hastePercent = CalculateHastePercent(character, buffOwner);

            // 计算实际施法时间
            // Calculate actual cast time
            double actualCastTime = castSkill.CastTimeSec / (1.0 + hastePercent / 100.0);

            return (true, castSkill, hastePercent, actualCastTime);
        }

        /// <summary>
        /// Battle Refactor Phase 6.3: 尝试为怪物开始施法的数据准备
        /// Battle Refactor Phase 6.3: Prepare data for starting monster casting
        /// </summary>
        /// <param name="enemyId">怪物ID / Enemy ID</param>
        /// <param name="enemy">怪物 / Enemy</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        /// <returns>
        /// 施法数据元组：(成功, 技能定义, 实际施法时间)
        /// Casting data tuple: (success, skill definition, actual cast time)
        /// </returns>
        public (bool Success, SkillDef? Skill, double ActualCastTime) PrepareMonsterCasting(
            string enemyId,
            Enemy enemy,
            BattleContext context)
        {
            // 选择施法技能
            // Select cast skill
            var castSkill = SelectMonsterCastSkill(enemyId, enemy, context);
            if (castSkill == null)
                return (false, null, 0);

            // 怪物目前不支持急速，施法时间就是基础施法时间
            // Monster doesn't support haste yet, cast time is base cast time
            double actualCastTime = castSkill.CastTimeSec;

            return (true, castSkill, actualCastTime);
        }

        /// <summary>
        /// Battle Refactor Phase 6.3: 获取技能定义
        /// Battle Refactor Phase 6.3: Get skill definition
        /// </summary>
        public SkillDef? GetSkillDef(string skillId)
        {
            return _skillRepository.GetSkill(skillId);
        }

        #endregion

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
        /// 
        /// Phase 8 优化：复用 HasteCalculator，消除代码重复
        /// Phase 8 optimization: Reuse HasteCalculator to eliminate code duplication
        /// </summary>
        public double CalculateHastePercent(Character character, IBuffOwner? buffOwner)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
                
            double baseHastePercent = character.HastePercent;

            // Phase 8: 复用 HasteCalculator 的泛型方法
            // Phase 8: Reuse HasteCalculator's generic method
            if (buffOwner is CharacterBuffOwner charBuffOwner)
            {
                return HasteCalculator.CalculateHastePercent(baseHastePercent, charBuffOwner);
            }

            return baseHastePercent;
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
