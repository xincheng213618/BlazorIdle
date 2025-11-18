using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// Window 类型枚举
    /// Window type enumeration
    /// </summary>
    public enum WindowType
    {
        /// <summary>
        /// 普攻前窗口：用于选择施法技能
        /// PreAttack window: for selecting cast skills
        /// </summary>
        PreAttack,

        /// <summary>
        /// 普攻后窗口：用于执行瞬发技能
        /// PostAttack window: for executing instant skills
        /// </summary>
        PostAttack,

        /// <summary>
        /// 施法完成后窗口：用于追加瞬发技能
        /// PostCast window: for executing additional instant skills after casting
        /// </summary>
        PostCast
    }

    /// <summary>
    /// WindowExecutor - Phase 6: Window-GCD 机制的核心执行器
    /// WindowExecutor - Phase 6: Core executor for Window-GCD mechanism
    /// 
    /// 负责在不同窗口中执行技能选择和 GCD 互斥逻辑
    /// Responsible for skill selection and GCD exclusion logic in different windows
    /// </summary>
    public sealed class WindowExecutor
    {
        private readonly SkillRepository _skillRepository;
        private readonly ConditionChecker _conditionChecker;
        private readonly CooldownManager _cooldownManager;
        private readonly ResourceManager _resourceManager;

        /// <summary>
        /// 窗口执行事件
        /// Window execution event
        /// </summary>
        public event Action<WindowExecutionEvent>? WindowExecuted;

        public WindowExecutor(
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
        /// 执行窗口：根据窗口类型和 GCD 状态选择并返回可执行的技能
        /// Execute window: Select and return executable skills based on window type and GCD status
        /// </summary>
        /// <param name="window">窗口类型</param>
        /// <param name="characterData">角色数据</param>
        /// <param name="professionId">职业ID</param>
        /// <param name="context">战斗上下文</param>
        /// <param name="gcdAlreadyUsed">GCD 槽位是否已被占用</param>
        /// <returns>可执行的技能列表</returns>
        public List<SkillDef> ExecuteWindow(
            WindowType window,
            CharacterData characterData,
            string professionId,
            BattleContext context,
            bool gcdAlreadyUsed)
        {
            var results = new List<SkillDef>();

            if (characterData == null || context == null)
                return results;

            // 获取窗口可用的技能
            var availableSkills = GetWindowSkills(window, characterData, professionId);
            if (availableSkills.Count == 0)
            {
                RecordWindowExecution(window, 0, 0, gcdAlreadyUsed);
                return results;
            }

            // 按优先级排序（槽位顺序）
            var sortedSkills = SortByPriority(availableSkills);

            // 执行技能选择（遵守 Window-GCD 规则）
            int consideredCount = 0;
            foreach (var skill in sortedSkills)
            {
                consideredCount++;

                // 检查技能是否可用（冷却、条件、资源）
                if (!IsSkillAvailable(skill, context))
                    continue;

                // Window-GCD 规则判定
                if (skill.IsGcd)
                {
                    // GCD 技能：只有当 GCD 槽位未被占用时才能释放
                    if (gcdAlreadyUsed)
                        continue;

                    results.Add(skill);
                    gcdAlreadyUsed = true; // 占用 GCD 槽位
                }
                else
                {
                    // 非 GCD 技能：可以多个同时释放
                    results.Add(skill);
                }
            }

            RecordWindowExecution(window, consideredCount, results.Count, gcdAlreadyUsed);
            return results;
        }

        /// <summary>
        /// 获取指定窗口可用的技能列表
        /// Get available skills for the specified window
        /// </summary>
        private List<SkillDef> GetWindowSkills(WindowType window, CharacterData characterData, string professionId)
        {
            var skills = new List<SkillDef>();

            if (!characterData.EquippedSkillsByProfession.TryGetValue(professionId, out var config))
                return skills;

            switch (window)
            {
                case WindowType.PreAttack:
                    // PreAttack 窗口：只选择施法技能
                    // PreAttack window: Select only cast skills
                    skills = GetEquippedSkills(config).Where(s => s.ReleaseType == "cast").ToList();
                    break;

                case WindowType.PostAttack:
                case WindowType.PostCast:
                    // PostAttack/PostCast 窗口：只选择瞬发技能
                    // PostAttack/PostCast window: Select only instant skills
                    // 排除职业固定的被动技能（Type=passive，如 attack_basic/special_pulse）
                    // Exclude profession-fixed passive skills (Type=passive, like attack_basic/special_pulse)
                    skills = GetEquippedSkills(config)
                        .Where(s => s.ReleaseType == "instant" && s.Type != "passive")
                        .ToList();
                    
                    // PostCast 窗口额外检查 allowCoTriggerAfterCast
                    // PostCast window additionally checks allowCoTriggerAfterCast
                    if (window == WindowType.PostCast)
                    {
                        skills = skills.Where(s => s.AllowCoTriggerAfterCast).ToList();
                    }
                    break;
            }

            return skills;
        }

        /// <summary>
        /// 获取角色已装备的所有技能
        /// Get all equipped skills for a character
        /// </summary>
        private List<SkillDef> GetEquippedSkills(EquippedSkillsConfig config)
        {
            var skills = new List<SkillDef>();

            // 获取主动技能（按槽位顺序：active_1 → active_2 → active_3）
            // Get active skills (in slot order: active_1 → active_2 → active_3)
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

            // 获取被动技能槽位（passive_1）
            // Get passive skill slot (passive_1)
            // 注意：被动技能槽位通常用于装备主动触发的技能（如 active 类型）
            // Note: Passive skill slot is typically used for actively triggered skills (type=active)
            // 如果是职业固定技能（type=passive，如 attack_basic），应该由 GetWindowSkills 的 ReleaseType 过滤掉
            // If it's a profession-fixed skill (type=passive, like attack_basic), it should be filtered by ReleaseType in GetWindowSkills
            if (!string.IsNullOrEmpty(config.PassiveSlot))
            {
                var skill = _skillRepository.GetSkill(config.PassiveSlot);
                if (skill != null)
                    skills.Add(skill);
            }

            return skills;
        }

        /// <summary>
        /// 检查技能是否可用（冷却、条件、资源）
        /// Check if skill is available (cooldown, conditions, resources)
        /// </summary>
        private bool IsSkillAvailable(SkillDef skill, BattleContext context)
        {
            // 检查冷却
            if (!_cooldownManager.IsReady(skill.Id))
                return false;

            // 检查条件
            if (skill.Conditions != null && !_conditionChecker.CheckConditions(skill, context, isCasterPlayer: true))
                return false;

            // 检查资源
            if (!_resourceManager.CheckResourceCost(skill, context))
                return false;

            return true;
        }

        /// <summary>
        /// 按优先级排序技能（槽位顺序）
        /// Sort skills by priority (slot order)
        /// </summary>
        private List<SkillDef> SortByPriority(List<SkillDef> skills)
        {
            // 当前实现：保持槽位顺序（已经是正确的优先级）
            // Current implementation: keep slot order (already correct priority)
            // 未来可以根据技能属性（伤害、冷却等）进行更复杂的排序
            // Future: can add more complex sorting based on skill properties (damage, cooldown, etc.)
            return skills;
        }

        /// <summary>
        /// 记录窗口执行结果
        /// Record window execution result
        /// </summary>
        private void RecordWindowExecution(WindowType window, int consideredCount, int executedCount, bool gcdUsed)
        {
            WindowExecuted?.Invoke(new WindowExecutionEvent
            {
                Window = window,
                ConsideredSkillCount = consideredCount,
                ExecutedSkillCount = executedCount,
                GcdUsed = gcdUsed,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 窗口执行事件
    /// Window execution event
    /// </summary>
    public class WindowExecutionEvent
    {
        public WindowType Window { get; set; }
        public int ConsideredSkillCount { get; set; }
        public int ExecutedSkillCount { get; set; }
        public bool GcdUsed { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
