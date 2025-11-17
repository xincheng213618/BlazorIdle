using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能学习管理器 (Step 2 Phase 2.5)
    /// Skill learning manager
    /// </summary>
    public sealed class SkillLearningManager
    {
        private readonly SkillRepository _skillRepository;

        public SkillLearningManager(SkillRepository skillRepository)
        {
            _skillRepository = skillRepository;
        }

        /// <summary>
        /// 检查是否可以学习技能
        /// Check if skill can be learned
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="skillId">技能ID</param>
        /// <param name="characterLevel">角色等级</param>
        /// <param name="currentProfessionId">当前职业ID</param>
        /// <returns>是否可以学习</returns>
        public bool CanLearnSkill(CharacterData characterData, string skillId, int characterLevel, string currentProfessionId)
        {
            var skill = _skillRepository.GetSkill(skillId);
            if (skill == null)
                return false;

            // 已经学习过
            // Already learned
            if (characterData.LearnedSkills.Contains(skillId))
                return false;

            // 验证职业限制
            // Validate profession restrictions
            if (skill.AllowedProfessions != null && skill.AllowedProfessions.Count > 0)
            {
                if (!skill.AllowedProfessions.Contains(currentProfessionId))
                    return false;
            }

            // 验证解锁条件
            // Validate unlock conditions
            if (skill.Unlock != null)
            {
                // 验证等级要求
                // Validate level requirements
                if (skill.Unlock.MinLevel > 0 && characterLevel < skill.Unlock.MinLevel)
                    return false;

                // 验证账号标记要求 (Step 2 Phase 2.5+)
                // Validate account flags requirements
                if (skill.Unlock.AccountFlags != null && skill.Unlock.AccountFlags.Count > 0)
                {
                    // 检查是否拥有所有必需的账号标记
                    // Check if character has all required account flags
                    foreach (var requiredFlag in skill.Unlock.AccountFlags)
                    {
                        if (!characterData.AccountFlags.Contains(requiredFlag))
                            return false;
                    }
                }

                // 验证职业等级要求 (Step 2 Phase 2.5+)
                // Validate profession level requirements
                if (skill.Unlock.RequiresProfessionLevel != null && skill.Unlock.RequiresProfessionLevel.Count > 0)
                {
                    // 检查每个职业的等级要求
                    // Check level requirement for each profession
                    foreach (var (professionId, requiredLevel) in skill.Unlock.RequiresProfessionLevel)
                    {
                        // 获取角色在该职业的进度
                        // Get character's progress in that profession
                        if (!characterData.Professions.TryGetValue(professionId, out var professionProgress))
                            return false;

                        // 检查职业等级是否满足
                        // Check if profession level meets requirement
                        if (professionProgress.Level < requiredLevel)
                            return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 学习技能
        /// Learn skill
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="skillId">技能ID</param>
        /// <param name="characterLevel">角色等级</param>
        /// <param name="currentProfessionId">当前职业ID</param>
        /// <returns>是否学习成功</returns>
        public bool LearnSkill(CharacterData characterData, string skillId, int characterLevel, string currentProfessionId)
        {
            if (!CanLearnSkill(characterData, skillId, characterLevel, currentProfessionId))
                return false;

            characterData.LearnedSkills.Add(skillId);
            return true;
        }

        /// <summary>
        /// 获取可学习的技能列表
        /// Get learnable skills
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="characterLevel">角色等级</param>
        /// <param name="currentProfessionId">当前职业ID</param>
        /// <returns>可学习的技能列表</returns>
        public List<SkillDef> GetLearnableSkills(CharacterData characterData, int characterLevel, string currentProfessionId)
        {
            return _skillRepository.GetSkillsByProfession(currentProfessionId)
                .Where(skill => !skill.Fixed && CanLearnSkill(characterData, skill.Id, characterLevel, currentProfessionId))
                .ToList();
        }

        /// <summary>
        /// 获取已学习的技能列表
        /// Get learned skills
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <returns>已学习的技能列表</returns>
        public List<SkillDef> GetLearnedSkills(CharacterData characterData)
        {
            return characterData.LearnedSkills
                .Select(skillId => _skillRepository.GetSkill(skillId))
                .Where(skill => skill != null)
                .Cast<SkillDef>()
                .ToList();
        }
    }
}
