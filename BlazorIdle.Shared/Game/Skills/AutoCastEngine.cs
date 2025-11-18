using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// AutoCastEngine 核心框架 (Step 2 Phase 9 - 先行实施)
    /// AutoCastEngine core framework (Step 2 Phase 9 - Priority Implementation)
    /// 
    /// 提供统一的技能调度和协调框架，为后续 Window-GCD 和触发系统提供基础
    /// Provides unified skill scheduling and coordination framework for future Window-GCD and trigger systems
    /// </summary>
    public sealed class AutoCastEngine
    {
        private readonly SkillRepository _skillRepository;
        private readonly ConditionChecker _conditionChecker;
        private readonly CooldownManager _cooldownManager;
        private readonly ResourceManager _resourceManager;

        /// <summary>
        /// 技能选择决策事件
        /// Skill selection decision event
        /// </summary>
        public event Action<SkillSelectionEvent>? SkillSelectionDecision;

        /// <summary>
        /// 技能施放事件
        /// Skill cast event
        /// </summary>
        public event Action<SkillCastAttemptEvent>? SkillCastAttempt;

        /// <summary>
        /// 技能失败事件
        /// Skill failure event
        /// </summary>
        public event Action<SkillFailureEvent>? SkillFailure;

        public AutoCastEngine(
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
        /// 主循环 - 每帧调用，负责技能选择和调度
        /// Main loop - called each frame, handles skill selection and scheduling
        /// </summary>
        /// <param name="deltaTime">距离上一帧的时间间隔（秒）</param>
        /// <param name="characterData">角色数据</param>
        /// <param name="professionId">当前职业ID</param>
        /// <param name="context">战斗上下文</param>
        /// <returns>选中要释放的技能ID，如果没有可用技能返回 null</returns>
        public string? Tick(double deltaTime, CharacterData characterData, string professionId, BattleContext context)
        {
            if (characterData == null || context == null)
                return null;

            // 获取角色的技能槽位
            var equippedSkills = GetEquippedSkills(characterData, professionId);
            if (equippedSkills.Count == 0)
                return null;

            // 按优先级排序（槽位顺序）
            var sortedSkills = SortByPriority(equippedSkills);

            // 选择第一个可用的技能
            foreach (var skill in sortedSkills)
            {
                // 检查冷却
                if (!_cooldownManager.IsReady(skill.Id))
                {
                    RecordSkillFailure(skill.Id, "Cooldown", $"Remaining: {_cooldownManager.GetRemainingCooldown(skill.Id):F1}s");
                    continue;
                }

                // 检查条件
                if (skill.Conditions != null && !_conditionChecker.CheckConditions(skill, context, isCasterPlayer: true))
                {
                    RecordSkillFailure(skill.Id, "Condition", "Skill conditions not met");
                    continue;
                }

                // 检查资源
                if (!_resourceManager.CheckResourceCost(skill, context))
                {
                    RecordSkillFailure(skill.Id, "Resource", "Insufficient resources");
                    continue;
                }

                // 找到第一个可用技能
                RecordSkillSelection(skill.Id, "Available");
                RecordSkillCastAttempt(skill.Id);
                return skill.Id;
            }

            // 没有可用技能
            return null;
        }

        /// <summary>
        /// 获取角色已装备的技能列表
        /// Get character's equipped skills
        /// </summary>
        private List<SkillDef> GetEquippedSkills(CharacterData characterData, string professionId)
        {
            var skills = new List<SkillDef>();

            if (!characterData.EquippedSkillsByProfession.TryGetValue(professionId, out var config))
                return skills;

            // 获取主动技能（按槽位顺序）
            for (int i = 1; i <= 3; i++)
            {
                var slotId = $"active_{i}";
                if (config.ActiveSlots.TryGetValue(slotId, out var skillId) && !string.IsNullOrEmpty(skillId))
                {
                    var skill = _skillRepository.GetSkill(skillId);
                    if (skill != null)
                        skills.Add(skill);
                }
            }

            // 获取被动技能
            if (!string.IsNullOrEmpty(config.PassiveSlot))
            {
                var skill = _skillRepository.GetSkill(config.PassiveSlot);
                if (skill != null)
                    skills.Add(skill);
            }

            return skills;
        }

        /// <summary>
        /// 按优先级排序技能（槽位顺序）
        /// Sort skills by priority (slot order)
        /// </summary>
        private List<SkillDef> SortByPriority(List<SkillDef> skills)
        {
            // 当前简单实现：保持槽位顺序（已经是正确的优先级）
            // Current simple implementation: keep slot order (already correct priority)
            // 未来可以根据技能类型、条件等进行更复杂的排序
            // Future: can add more complex sorting based on skill type, conditions, etc.
            return skills;
        }

        /// <summary>
        /// 记录技能选择决策
        /// Record skill selection decision
        /// </summary>
        private void RecordSkillSelection(string skillId, string reason)
        {
            SkillSelectionDecision?.Invoke(new SkillSelectionEvent
            {
                SkillId = skillId,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 记录技能施放尝试
        /// Record skill cast attempt
        /// </summary>
        private void RecordSkillCastAttempt(string skillId)
        {
            SkillCastAttempt?.Invoke(new SkillCastAttemptEvent
            {
                SkillId = skillId,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 记录技能失败原因
        /// Record skill failure reason
        /// </summary>
        private void RecordSkillFailure(string skillId, string reason, string details)
        {
            SkillFailure?.Invoke(new SkillFailureEvent
            {
                SkillId = skillId,
                Reason = reason,
                Details = details,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 技能选择事件
    /// Skill selection event
    /// </summary>
    public class SkillSelectionEvent
    {
        public string SkillId { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 技能施放尝试事件
    /// Skill cast attempt event
    /// </summary>
    public class SkillCastAttemptEvent
    {
        public string SkillId { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 技能失败事件
    /// Skill failure event
    /// </summary>
    public class SkillFailureEvent
    {
        public string SkillId { get; set; } = "";
        public string Reason { get; set; } = "";
        public string Details { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}
