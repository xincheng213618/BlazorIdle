using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 触发处理器 - Phase 7: 触发类技能系统
    /// Trigger Processor - Phase 7: Trigger Skill System
    /// 
    /// 处理技能触发逻辑，支持玩家和怪物技能触发
    /// Handles skill trigger logic, supports both player and monster skill triggers
    /// </summary>
    public sealed class TriggerProcessor
    {
        private readonly SkillRepository _skillRepository;
        private readonly ConditionChecker _conditionChecker;
        private readonly CooldownManager _cooldownManager;
        private readonly ResourceManager _resourceManager;
        
        // 安全机制：防止递归触发死循环
        // Safety mechanism: prevent infinite recursion loops
        private const int MaxTriggersPerWindow = 5;
        private const int MaxRecursionDepth = 3;
        
        private int _currentTriggerCount = 0;
        private int _currentRecursionDepth = 0;

        /// <summary>
        /// 触发器执行事件
        /// Trigger execution event
        /// </summary>
        public event Action<TriggerExecutionEvent>? TriggerExecuted;

        public TriggerProcessor(
            SkillRepository skillRepository,
            ConditionChecker conditionChecker,
            CooldownManager cooldownManager,
            ResourceManager resourceManager)
        {
            _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
            _conditionChecker = conditionChecker ?? throw new ArgumentNullException(nameof(conditionChecker));
            _cooldownManager = cooldownManager ?? throw new ArgumentNullException(nameof(cooldownManager));
            _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        }

        /// <summary>
        /// 处理触发器 - 主入口方法
        /// Process triggers - main entry point
        /// </summary>
        /// <param name="when">触发时机 (OnAttackHit, OnAttackCrit, OnPostAttackWindow, OnPostCastWindow)</param>
        /// <param name="sourceSkill">触发源技能（可能为 null，如普通攻击）</param>
        /// <param name="context">战斗上下文</param>
        /// <param name="isCasterPlayer">施法者是否为玩家</param>
        /// <param name="casterCharacterData">玩家角色数据（仅玩家触发时需要）</param>
        /// <param name="casterProfessionId">玩家职业ID（仅玩家触发时需要）</param>
        /// <param name="wasCrit">是否为暴击（用于 OnAttackCrit 判定）</param>
        /// <returns>触发并应执行的技能列表</returns>
        public List<SkillDef> ProcessTriggers(
            string when,
            SkillDef? sourceSkill,
            BattleContext context,
            bool isCasterPlayer,
            CharacterData? casterCharacterData = null,
            string? casterProfessionId = null,
            bool wasCrit = false)
        {
            var triggeredSkills = new List<SkillDef>();

            // 重置窗口触发计数
            if (when == "OnPostAttackWindow" || when == "OnPostCastWindow")
            {
                _currentTriggerCount = 0;
                _currentRecursionDepth = 0;
            }

            // 安全检查
            if (_currentTriggerCount >= MaxTriggersPerWindow)
            {
                RecordTriggerEvent(when, sourceSkill?.Id, null, "Safety", "Max triggers per window reached");
                return triggeredSkills;
            }

            if (_currentRecursionDepth >= MaxRecursionDepth)
            {
                RecordTriggerEvent(when, sourceSkill?.Id, null, "Safety", "Max recursion depth reached");
                return triggeredSkills;
            }

            // 获取所有可能触发的技能
            List<TriggerDef> candidateTriggers = GetCandidateTriggers(
                when,
                sourceSkill,
                isCasterPlayer,
                casterCharacterData,
                casterProfessionId,
                wasCrit);

            // 按优先级排序
            candidateTriggers = candidateTriggers.OrderByDescending(t => t.Priority).ToList();

            // 处理每个触发器
            foreach (var trigger in candidateTriggers)
            {
                // 安全检查
                if (_currentTriggerCount >= MaxTriggersPerWindow)
                    break;

                // 概率判定
                if (trigger.ProcChance < 1.0)
                {
                    var random = new Random();
                    if (random.NextDouble() > trigger.ProcChance)
                    {
                        RecordTriggerEvent(when, sourceSkill?.Id, trigger.FireSkillId, "ProcFailed", $"Chance: {trigger.ProcChance:P0}");
                        continue;
                    }
                }

                // 获取触发技能定义
                var triggeredSkill = _skillRepository.GetSkill(trigger.FireSkillId);
                if (triggeredSkill == null)
                {
                    RecordTriggerEvent(when, sourceSkill?.Id, trigger.FireSkillId, "SkillNotFound", "Triggered skill not found");
                    continue;
                }

                // 检查触发条件
                bool conditionsMet = CheckTriggerConditions(trigger, triggeredSkill, context, isCasterPlayer);
                if (!conditionsMet)
                {
                    RecordTriggerEvent(when, sourceSkill?.Id, trigger.FireSkillId, "ConditionsFailed", "Trigger conditions not met");
                    continue;
                }

                // 成功触发
                triggeredSkills.Add(triggeredSkill);
                _currentTriggerCount++;
                RecordTriggerEvent(when, sourceSkill?.Id, trigger.FireSkillId, "Success", $"Priority: {trigger.Priority}");
            }

            return triggeredSkills;
        }

        /// <summary>
        /// 获取候选触发器列表
        /// Get candidate trigger list
        /// </summary>
        private List<TriggerDef> GetCandidateTriggers(
            string when,
            SkillDef? sourceSkill,
            bool isCasterPlayer,
            CharacterData? casterCharacterData,
            string? casterProfessionId,
            bool wasCrit)
        {
            var triggers = new List<TriggerDef>();

            // 首先从源技能中收集触发器（无论玩家还是怪物）
            // First collect triggers from the source skill (for both player and monster)
            if (sourceSkill != null)
            {
                triggers.AddRange(GetSkillTriggers(sourceSkill, when));
            }

            // 如果是玩家，还要从装备技能中收集触发器
            // If player, also collect triggers from equipped skills
            if (isCasterPlayer && casterCharacterData != null && !string.IsNullOrEmpty(casterProfessionId))
            {
                triggers.AddRange(GetPlayerEquippedTriggers(casterCharacterData, casterProfessionId, when));
            }

            // 特殊处理：OnAttackCrit 只有在实际暴击时才触发
            // Special handling: OnAttackCrit only triggers when actually crit
            if (when == "OnAttackCrit" && !wasCrit)
            {
                return new List<TriggerDef>();
            }

            return triggers;
        }

        /// <summary>
        /// 获取玩家装备技能的触发器
        /// Get triggers from player's equipped skills
        /// </summary>
        private List<TriggerDef> GetPlayerEquippedTriggers(CharacterData characterData, string professionId, string when)
        {
            var triggers = new List<TriggerDef>();

            if (!characterData.EquippedSkillsByProfession.TryGetValue(professionId, out var config))
                return triggers;

            // 收集所有装备技能的触发器
            var equippedSkillIds = new List<string>();
            
            // 主动技能
            foreach (var kvp in config.ActiveSlots)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                    equippedSkillIds.Add(kvp.Value);
            }
            
            // 被动技能
            if (!string.IsNullOrEmpty(config.PassiveSlot))
                equippedSkillIds.Add(config.PassiveSlot);

            // 获取所有触发器
            foreach (var skillId in equippedSkillIds)
            {
                var skill = _skillRepository.GetSkill(skillId);
                if (skill != null)
                {
                    triggers.AddRange(GetSkillTriggers(skill, when));
                }
            }

            return triggers;
        }

        /// <summary>
        /// 获取技能的触发器
        /// Get triggers from a skill
        /// </summary>
        private List<TriggerDef> GetSkillTriggers(SkillDef skill, string when)
        {
            if (skill.Triggers == null || skill.Triggers.Count == 0)
                return new List<TriggerDef>();

            return skill.Triggers.Where(t => t.When == when).ToList();
        }

        /// <summary>
        /// 检查触发条件
        /// Check trigger conditions
        /// </summary>
        private bool CheckTriggerConditions(TriggerDef trigger, SkillDef skill, BattleContext context, bool isCasterPlayer)
        {
            // 如果触发器设置了 IgnoreRequirements，跳过所有检查
            if (trigger.IgnoreRequirements)
                return true;

            // 检查冷却
            if (!_cooldownManager.IsReady(skill.Id))
                return false;

            // 检查资源
            if (!_resourceManager.CheckResourceCost(skill, context))
                return false;

            // 检查条件（如果触发器有自己的条件，使用触发器的；否则使用技能的）
            var conditionsToCheck = trigger.Conditions ?? skill.Conditions;
            if (conditionsToCheck != null)
            {
                if (!_conditionChecker.CheckConditions(skill, context, isCasterPlayer))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 记录触发事件
        /// Record trigger event
        /// </summary>
        private void RecordTriggerEvent(string when, string? sourceSkillId, string? triggeredSkillId, string status, string reason)
        {
            TriggerExecuted?.Invoke(new TriggerExecutionEvent
            {
                When = when,
                SourceSkillId = sourceSkillId,
                TriggeredSkillId = triggeredSkillId,
                Status = status,
                Reason = reason
            });
        }

        /// <summary>
        /// 重置触发计数器（用于测试或战斗重置）
        /// Reset trigger counters (for testing or battle reset)
        /// </summary>
        public void ResetCounters()
        {
            _currentTriggerCount = 0;
            _currentRecursionDepth = 0;
        }
    }

    /// <summary>
    /// 触发执行事件
    /// Trigger execution event
    /// </summary>
    public sealed class TriggerExecutionEvent
    {
        public string When { get; set; } = "";
        public string? SourceSkillId { get; set; }
        public string? TriggeredSkillId { get; set; }
        public string Status { get; set; } = "";
        public string Reason { get; set; } = "";
    }
}
