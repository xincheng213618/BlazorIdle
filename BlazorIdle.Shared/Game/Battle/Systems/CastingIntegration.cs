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
    /// - 尝试开始施法 (TryStartCasting)
    /// - 处理施法完成 (HandleCastComplete)
    /// - 处理施法中断 (HandleCastInterrupt)
    /// - 管理玩家和怪物的施法流程
    /// </summary>
    public class CastingIntegration
    {
        private readonly IGameClock _clock;
        private readonly CastingController _castingController;
        private readonly SkillRepository _skillRepository;
        private readonly WindowExecutor _windowExecutor;
        private readonly AutoCastEngine _autoCastEngine;

        /// <summary>
        /// 施法开始事件
        /// Cast started event
        /// </summary>
        public event Action<int, string, string, double, bool>? OnCastStarted;

        /// <summary>
        /// 施法完成事件
        /// Cast completed event
        /// </summary>
        public event Action<int, string, string, double>? OnCastCompleted;

        /// <summary>
        /// 施法中断事件
        /// Cast interrupted event
        /// </summary>
        public event Action<int, string, string, string, double>? OnCastInterrupted;

        /// <summary>
        /// 技能执行请求事件
        /// Skill execution request event
        /// </summary>
        public event Action<string, string, bool, EventSource>? OnExecuteSkillRequested;

        /// <summary>
        /// 轨道暂停请求事件
        /// Track pause request event
        /// </summary>
        public event Action<string, int>? OnTrackPauseRequested;

        /// <summary>
        /// 轨道恢复请求事件
        /// Track resume request event
        /// </summary>
        public event Action<string, int>? OnTrackResumeRequested;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public CastingIntegration(
            IGameClock clock,
            CastingController castingController,
            SkillRepository skillRepository,
            WindowExecutor windowExecutor,
            AutoCastEngine autoCastEngine)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _castingController = castingController ?? throw new ArgumentNullException(nameof(castingController));
            _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
            _windowExecutor = windowExecutor ?? throw new ArgumentNullException(nameof(windowExecutor));
            _autoCastEngine = autoCastEngine ?? throw new ArgumentNullException(nameof(autoCastEngine));
        }

        /// <summary>
        /// 检查指定施法者是否正在施法
        /// Check if specified caster is currently casting
        /// </summary>
        public bool IsCasting(string casterId) => _castingController.IsCastingForCaster(casterId);

        /// <summary>
        /// 尝试开始玩家施法
        /// Try to start player casting
        /// </summary>
        public bool TryStartCasting(
            string charId,
            int now,
            Character character,
            CharacterData? characterData,
            BattleContext context,
            IBuffOwner? buffOwner)
        {
            // 如果已经在施法，返回 false
            if (_castingController.IsCastingForCaster(charId))
                return false;

            if (characterData == null)
                return false;

            // 检查是否有施法技能可用
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
                // 获取急速加成
                double hastePercent = CalculateHastePercent(character, buffOwner);

                // 开始施法
                bool castStarted = _castingController.StartCast(
                    charId,
                    castSkill.Id,
                    castSkill.CastTimeSec,
                    hastePercent,
                    pauseAttackTrack: true);

                if (castStarted)
                {
                    // 请求暂停攻击轨道
                    OnTrackPauseRequested?.Invoke(charId, now);

                    // 记录施法开始事件
                    var actualCastTime = castSkill.CastTimeSec / (1.0 + hastePercent / 100.0);
                    OnCastStarted?.Invoke(now, charId, castSkill.Id, actualCastTime, true);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试开始怪物施法
        /// Try to start monster casting
        /// </summary>
        public bool TryStartMonsterCasting(
            string enemyId,
            int now,
            Enemy enemy,
            BattleContext context)
        {
            // 如果已经在施法，返回 false
            if (_castingController.IsCastingForCaster(enemyId))
                return false;

            // 尝试选择一个施法技能
            var castSkill = _autoCastEngine.SelectMonsterCastSkill(enemyId, enemy, enemyId, context);
            if (castSkill != null && castSkill.CastTimeSec > 0)
            {
                // 怪物暂时没有急速
                double haste = 0.0;
                bool castStarted = _castingController.StartCast(
                    enemyId,
                    castSkill.Id,
                    castSkill.CastTimeSec,
                    haste,
                    pauseAttackTrack: true);

                if (castStarted)
                {
                    // 请求暂停攻击轨道
                    OnTrackPauseRequested?.Invoke(enemyId, now);

                    // 记录施法开始事件
                    OnCastStarted?.Invoke(now, enemyId, castSkill.Id, castSkill.CastTimeSec, true);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 处理玩家施法完成
        /// Handle player cast complete
        /// </summary>
        public void HandlePlayerCastComplete(
            string casterId,
            string skillId,
            int now,
            Character character,
            CharacterData? characterData,
            BattleContext context,
            IBuffOwner? buffOwner)
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
            }

            // 记录施法完成事件
            var activeCast = _castingController.GetActiveCast(casterId);
            OnCastCompleted?.Invoke(now, casterId, skillId, activeCast?.ElapsedSec ?? 0);

            // 施法完成后，尝试开始下一个施法
            bool startedNewCast = TryStartCasting(casterId, now, character, characterData, context, buffOwner);

            // 如果没有开始新的施法，恢复攻击轨道
            if (!startedNewCast)
            {
                OnTrackResumeRequested?.Invoke(casterId, now);
            }
        }

        /// <summary>
        /// 处理怪物施法完成
        /// Handle monster cast complete
        /// </summary>
        public void HandleMonsterCastComplete(
            string monsterId,
            string skillId,
            int now,
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

            // 记录施法完成事件
            var activeCast = _castingController.GetActiveCast(monsterId);
            OnCastCompleted?.Invoke(now, monsterId, skillId, activeCast?.ElapsedSec ?? 0);

            // 施法完成后，尝试开始下一次施法
            bool startedNewCast = TryStartMonsterCasting(monsterId, now, enemy, context);

            // 如果没有开始新的施法，恢复攻击轨道
            if (!startedNewCast)
            {
                OnTrackResumeRequested?.Invoke(monsterId, now);
            }
        }

        /// <summary>
        /// 处理施法中断
        /// Handle cast interrupt
        /// </summary>
        public void HandleCastInterrupt(string casterId, string skillId, string reason, int now)
        {
            // 恢复攻击轨道
            OnTrackResumeRequested?.Invoke(casterId, now);

            // 记录施法中断事件
            var activeCast = _castingController.GetActiveCast(casterId);
            OnCastInterrupted?.Invoke(now, casterId, skillId, reason, activeCast?.ElapsedSec ?? 0);
        }

        /// <summary>
        /// 检查并中断施法（所有敌人死亡时）
        /// Check and interrupt casting (when all enemies are dead)
        /// </summary>
        public void CheckAndInterruptCastingWhenAllEnemiesDead(IEnumerable<string> aliveEnemyIds)
        {
            // 只有当所有敌人都死亡时才中断施法
            if (aliveEnemyIds.Any())
                return;

            // 所有敌人死亡，中断所有正在施法的玩家
            var pausedTracks = _castingController.GetPausedTracks();
            foreach (var casterId in pausedTracks)
            {
                if (_castingController.IsCastingForCaster(casterId))
                {
                    var activeCast = _castingController.GetActiveCast(casterId);
                    if (activeCast != null)
                    {
                        _castingController.CancelCast(casterId, "all_enemies_dead");
                        // HandleCastInterrupt will be called by the event handler
                    }
                }
            }
        }

        /// <summary>
        /// 计算急速百分比（包含 Buff 效果）
        /// Calculate haste percentage (including buff effects)
        /// </summary>
        private double CalculateHastePercent(Character character, IBuffOwner? buffOwner)
        {
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
        /// 更新施法控制器的 Tick
        /// Update casting controller tick
        /// </summary>
        public void Tick(double deltaTimeSec)
        {
            _castingController.Tick(deltaTimeSec);
        }

        /// <summary>
        /// 获取活跃的施法
        /// Get active cast
        /// </summary>
        public ActiveCast? GetActiveCast(string casterId)
        {
            return _castingController.GetActiveCast(casterId);
        }
    }
}
