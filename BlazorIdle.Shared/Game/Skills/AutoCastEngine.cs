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
    /// 
    /// Phase 9 优化 (Optimizations):
    /// - 技能列表缓存 / Skill list caching
    /// - LINQ 优化 / LINQ optimization
    /// - 移除过时代码 / Obsolete code removed
    /// </summary>
    public sealed class AutoCastEngine
    {
        private readonly SkillRepository _skillRepository;
        private readonly ConditionChecker _conditionChecker;
        private readonly CooldownManager _cooldownManager;
        private readonly ResourceManager _resourceManager;

        // Phase 9: 技能列表缓存 / Skill list cache
        private readonly Dictionary<string, SkillCacheEntry> _skillCache = new Dictionary<string, SkillCacheEntry>();

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
        /// Phase 9: 使缓存失效 / Invalidate cache
        /// 当角色装备技能变更时调用 / Call when character's equipped skills change
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        public void InvalidateCache(string professionId)
        {
            _skillCache.Remove(professionId);
        }

        /// <summary>
        /// Phase 9: 清空所有缓存 / Clear all cache
        /// </summary>
        public void ClearCache()
        {
            _skillCache.Clear();
        }



        /// <summary>
        /// PreAttack 窗口：选择施法技能
        /// PreAttack window: Select cast skill
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="professionId">当前职业ID</param>
        /// <param name="context">战斗上下文</param>
        /// <returns>选中的施法技能，如果没有返回 null</returns>
        public SkillDef? SelectCastSkill(CharacterData characterData, string professionId, BattleContext context)
        {
            if (characterData == null || context == null)
                return null;

            // Phase 9: 使用缓存获取技能列表 / Use cache to get skill list
            var cacheEntry = GetOrCreateCacheEntry(characterData, professionId);
            if (cacheEntry.CastSkills.Count == 0)
                return null;

            // Phase 9: 优化 - 直接遍历，避免 LINQ / Optimized - direct iteration, avoid LINQ
            for (int i = 0; i < cacheEntry.CastSkills.Count; i++)
            {
                var skill = cacheEntry.CastSkills[i];
                if (IsSkillAvailable(skill, context))
                {
                    RecordSkillSelection(skill.Id, "PreAttack-Cast");
                    RecordSkillCastAttempt(skill.Id);
                    return skill;
                }
            }

            return null;
        }

        /// <summary>
        /// PostAttack/PostCast 窗口：执行瞬发技能（支持 Window-GCD 互斥）
        /// PostAttack/PostCast window: Execute instant skills (supports Window-GCD exclusion)
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="professionId">当前职业ID</param>
        /// <param name="context">战斗上下文</param>
        /// <param name="gcdAlreadyUsed">GCD 槽位是否已被占用（例如普通攻击或施法技能占用）</param>
        /// <param name="windowName">窗口名称（用于日志）</param>
        /// <returns>可以释放的技能列表</returns>
        public List<SkillDef> ExecuteWindow(CharacterData characterData, string professionId, BattleContext context, bool gcdAlreadyUsed, string windowName = "PostAttack")
        {
            var results = new List<SkillDef>();

            if (characterData == null || context == null)
                return results;

            // Phase 9: 使用缓存获取技能列表 / Use cache to get skill list
            var cacheEntry = GetOrCreateCacheEntry(characterData, professionId);
            if (cacheEntry.InstantSkills.Count == 0)
                return results;

            // Phase 9: 优化 - 直接遍历，避免 LINQ / Optimized - direct iteration, avoid LINQ
            for (int i = 0; i < cacheEntry.InstantSkills.Count; i++)
            {
                var skill = cacheEntry.InstantSkills[i];
                if (!IsSkillAvailable(skill, context))
                    continue;

                if (skill.IsGcd)
                {
                    // GCD 技能：只有当 GCD 槽位未被占用时才能释放
                    if (gcdAlreadyUsed)
                    {
                        RecordSkillFailure(skill.Id, "GCD", $"GCD slot already used in {windowName} window");
                        continue;
                    }

                    results.Add(skill);
                    RecordSkillSelection(skill.Id, $"{windowName}-GCD");
                    RecordSkillCastAttempt(skill.Id);
                    gcdAlreadyUsed = true; // 占用 GCD 槽位
                }
                else
                {
                    // 非 GCD 技能：可以多个同时释放
                    results.Add(skill);
                    RecordSkillSelection(skill.Id, $"{windowName}-NonGCD");
                    RecordSkillCastAttempt(skill.Id);
                }
            }

            return results;
        }

        /// <summary>
        /// Phase 9: 获取或创建缓存条目 / Get or create cache entry
        /// </summary>
        private SkillCacheEntry GetOrCreateCacheEntry(CharacterData characterData, string professionId)
        {
            if (_skillCache.TryGetValue(professionId, out var cached))
                return cached;

            // 创建新的缓存条目 / Create new cache entry
            var allSkills = GetEquippedSkills(characterData, professionId);
            var entry = new SkillCacheEntry
            {
                AllSkills = allSkills,
                CastSkills = new List<SkillDef>(),
                InstantSkills = new List<SkillDef>()
            };

            // 分类技能以优化后续查询 / Categorize skills for optimized queries
            for (int i = 0; i < allSkills.Count; i++)
            {
                var skill = allSkills[i];
                if (skill.ReleaseType == "cast")
                    entry.CastSkills.Add(skill);
                else if (skill.ReleaseType == "instant")
                    entry.InstantSkills.Add(skill);
            }

            _skillCache[professionId] = entry;
            return entry;
        }

        /// <summary>
        /// 检查技能是否可用（冷却、条件、资源）
        /// Check if skill is available (cooldown, conditions, resources)
        /// </summary>
        private bool IsSkillAvailable(SkillDef skill, BattleContext context)
        {
            // 检查冷却
            if (!_cooldownManager.IsReady(skill.Id))
            {
                RecordSkillFailure(skill.Id, "Cooldown", $"Remaining: {_cooldownManager.GetRemainingCooldown(skill.Id):F1}s");
                return false;
            }

            // 检查条件
            if (skill.Conditions != null && !_conditionChecker.CheckConditions(skill, context, isCasterPlayer: true))
            {
                RecordSkillFailure(skill.Id, "Condition", "Skill conditions not met");
                return false;
            }

            // 检查资源
            if (!_resourceManager.CheckResourceCost(skill, context))
            {
                RecordSkillFailure(skill.Id, "Resource", "Insufficient resources");
                return false;
            }

            return true;
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

    /// <summary>
    /// Phase 9: 技能缓存条目 / Skill cache entry
    /// 缓存角色已装备的技能列表，按类型分类以提高查询性能
    /// Cache equipped skills, categorized by type for improved query performance
    /// </summary>
    internal class SkillCacheEntry
    {
        /// <summary>
        /// 所有已装备技能 / All equipped skills
        /// </summary>
        public List<SkillDef> AllSkills { get; set; } = new List<SkillDef>();

        /// <summary>
        /// 施法技能列表 / Cast skills list
        /// </summary>
        public List<SkillDef> CastSkills { get; set; } = new List<SkillDef>();

        /// <summary>
        /// 瞬发技能列表 / Instant skills list
        /// </summary>
        public List<SkillDef> InstantSkills { get; set; } = new List<SkillDef>();
    }
}
