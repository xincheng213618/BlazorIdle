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

            // 验证等级要求
            // Validate level requirements
            if (skill.Unlock != null && skill.Unlock.MinLevel > 0)
            {
                if (characterLevel < skill.Unlock.MinLevel)
                    return false;
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
