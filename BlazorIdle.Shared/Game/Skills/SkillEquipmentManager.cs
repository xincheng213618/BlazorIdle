using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能装备管理器 (Step 2 Phase 2.5)
    /// Skill equipment manager
    /// </summary>
    public sealed class SkillEquipmentManager
    {
        private readonly SkillRepository _skillRepository;

        public SkillEquipmentManager(SkillRepository skillRepository)
        {
            _skillRepository = skillRepository;
        }

        /// <summary>
        /// 装备技能到指定槽位
        /// Equip skill to specified slot
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="professionId">职业ID</param>
        /// <param name="slotId">槽位ID</param>
        /// <param name="skillId">技能ID</param>
        /// <returns>是否装备成功</returns>
        public bool EquipSkill(CharacterData characterData, string professionId, string slotId, string skillId)
        {
            // 确保职业配置存在
            // Ensure profession configuration exists
            EnsureProfessionConfigExists(characterData, professionId);

            var config = characterData.EquippedSkillsByProfession[professionId];
            var skill = _skillRepository.GetSkill(skillId);

            if (skill == null)
                return false;

            // 验证职业限制
            // Validate profession restrictions
            if (skill.AllowedProfessions != null && skill.AllowedProfessions.Count > 0)
            {
                if (!skill.AllowedProfessions.Contains(professionId))
                    return false;
            }

            // 验证技能是否已学习（固定技能除外）
            // Validate skill is learned (except fixed skills)
            if (!skill.Fixed && !characterData.LearnedSkills.Contains(skillId))
                return false;

            // 验证槽位类型匹配
            // Validate slot type matching
            if (slotId == "passive_1")
            {
                if (skill.SlotType != "passive")
                    return false;

                config.PassiveSlot = skillId;
                return true;
            }
            else if (slotId.StartsWith("active_"))
            {
                if (skill.SlotType != "active")
                    return false;

                // 检查是否已装备在其他槽位（防止重复）
                // Check if already equipped in another slot (prevent duplication)
                if (config.ActiveSlots.Values.Any(s => s == skillId) || config.PassiveSlot == skillId)
                    return false;

                config.ActiveSlots[slotId] = skillId;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 卸载指定槽位的技能
        /// Unequip skill from specified slot
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="professionId">职业ID</param>
        /// <param name="slotId">槽位ID</param>
        /// <returns>是否卸载成功</returns>
        public bool UnequipSkill(CharacterData characterData, string professionId, string slotId)
        {
            // 确保职业配置存在
            // Ensure profession configuration exists
            EnsureProfessionConfigExists(characterData, professionId);

            var config = characterData.EquippedSkillsByProfession[professionId];

            // 固定技能不能卸载
            // Fixed skills cannot be unequipped
            string? currentSkillId = null;
            if (slotId == "passive_1")
            {
                currentSkillId = config.PassiveSlot;
            }
            else if (config.ActiveSlots.TryGetValue(slotId, out var skillId))
            {
                currentSkillId = skillId;
            }

            if (currentSkillId != null)
            {
                var skill = _skillRepository.GetSkill(currentSkillId);
                if (skill != null && skill.Fixed)
                    return false;
            }

            // 卸载技能
            // Unequip skill
            if (slotId == "passive_1")
            {
                config.PassiveSlot = null;
                return true;
            }
            else if (config.ActiveSlots.ContainsKey(slotId))
            {
                config.ActiveSlots[slotId] = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取装备的技能
        /// Get equipped skills
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="professionId">职业ID</param>
        /// <returns>装备的技能列表</returns>
        public List<SkillDef> GetEquippedSkills(CharacterData characterData, string professionId)
        {
            // 确保职业配置存在
            // Ensure profession configuration exists
            EnsureProfessionConfigExists(characterData, professionId);

            var config = characterData.EquippedSkillsByProfession[professionId];
            var skills = new List<SkillDef>();

            // 添加主动技能
            // Add active skills
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

            // 添加被动技能
            // Add passive skill
            if (!string.IsNullOrEmpty(config.PassiveSlot))
            {
                var skill = _skillRepository.GetSkill(config.PassiveSlot);
                if (skill != null)
                    skills.Add(skill);
            }

            return skills;
        }

        /// <summary>
        /// 初始化固定技能（防御式设计）
        /// Initialize fixed skills (defensive design)
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="professionId">职业ID</param>
        public void InitializeFixedSkills(CharacterData characterData, string professionId)
        {
            // 确保职业配置存在
            // Ensure profession configuration exists
            if (!characterData.EquippedSkillsByProfession.ContainsKey(professionId))
            {
                characterData.EquippedSkillsByProfession[professionId] = new EquippedSkillsConfig
                {
                    ProfessionId = professionId
                };
            }

            var config = characterData.EquippedSkillsByProfession[professionId];

            // 获取该职业的固定技能
            // Get fixed skills for the profession
            var fixedSkills = _skillRepository.GetSkillsByProfession(professionId)
                .Where(s => s.Fixed)
                .ToList();

            // 自动装配固定技能到空槽位
            // Auto-equip fixed skills to empty slots
            foreach (var skill in fixedSkills)
            {
                if (skill.SlotType == "active")
                {
                    // 找到第一个空的主动槽位
                    // Find first empty active slot
                    for (int i = 1; i <= 3; i++)
                    {
                        var slotId = $"active_{i}";
                        if (string.IsNullOrEmpty(config.ActiveSlots[slotId]))
                        {
                            config.ActiveSlots[slotId] = skill.Id;
                            break;
                        }
                    }
                }
                else if (skill.SlotType == "passive")
                {
                    // 装配到被动槽位
                    // Equip to passive slot
                    if (string.IsNullOrEmpty(config.PassiveSlot))
                    {
                        config.PassiveSlot = skill.Id;
                    }
                }
            }
        }

        /// <summary>
        /// 确保职业配置存在（防御式设计）
        /// Ensure profession configuration exists (defensive design)
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="professionId">职业ID</param>
        private void EnsureProfessionConfigExists(CharacterData characterData, string professionId)
        {
            if (!characterData.EquippedSkillsByProfession.ContainsKey(professionId) ||
                characterData.EquippedSkillsByProfession[professionId] == null)
            {
                InitializeFixedSkills(characterData, professionId);
            }
        }
    }
}
