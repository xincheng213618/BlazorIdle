using BlazorIdle.Shared.Models;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Items
{
    /// <summary>
    /// 物品使用辅助类 - 处理消耗品和技能书的使用逻辑
    /// Item usage helper - handles consumable and skill book usage logic
    /// </summary>
    public class ItemUsageHelper
    {
        private readonly SkillRepository _skillRepository;
        private readonly SkillLearningManager _learningManager;

        public ItemUsageHelper(SkillRepository skillRepository, SkillLearningManager learningManager)
        {
            _skillRepository = skillRepository;
            _learningManager = learningManager;
        }

        /// <summary>
        /// 检查物品是否可以使用
        /// Check if item can be used
        /// </summary>
        public (bool CanUse, string Reason) CanUseItem(
            ItemDefinition itemDef,
            CharacterData character,
            string currentProfessionId,
            int characterLevel)
        {
            if (itemDef.Type == ItemType.SkillBook)
            {
                return CanUseSkillBook(itemDef, character, currentProfessionId, characterLevel);
            }

            // 其他消耗品类型可以在这里扩展
            // Other consumable types can be extended here
            if (itemDef.Type == ItemType.Consumable)
            {
                return (true, string.Empty);
            }

            return (false, "该物品不可使用");
        }

        /// <summary>
        /// 使用物品
        /// Use item
        /// </summary>
        public (bool Success, string Message) UseItem(
            ItemDefinition itemDef,
            CharacterData character,
            string currentProfessionId,
            int characterLevel)
        {
            if (itemDef.Type == ItemType.SkillBook)
            {
                return UseSkillBook(itemDef, character, currentProfessionId, characterLevel);
            }

            // 其他消耗品类型可以在这里扩展
            // Other consumable types can be extended here
            if (itemDef.Type == ItemType.Consumable)
            {
                // 基础消耗品逻辑（如药水、卷轴等）
                return (true, $"使用了 {itemDef.Name}");
            }

            return (false, "该物品不可使用");
        }

        /// <summary>
        /// 检查技能书是否可以使用
        /// Check if skill book can be used
        /// </summary>
        private (bool CanUse, string Reason) CanUseSkillBook(
            ItemDefinition itemDef,
            CharacterData character,
            string currentProfessionId,
            int characterLevel)
        {
            if (itemDef.Metadata?.SkillId == null)
            {
                return (false, "技能书配置错误");
            }

            var skill = _skillRepository.GetSkill(itemDef.Metadata.SkillId);
            if (skill == null)
            {
                return (false, "技能不存在");
            }

            // 检查是否已学习
            if (character.LearnedSkills.Contains(itemDef.Metadata.SkillId))
            {
                return (false, "已经学会该技能");
            }

            // 检查职业限制
            if (skill.AllowedProfessions != null && skill.AllowedProfessions.Count > 0)
            {
                if (!skill.AllowedProfessions.Contains(currentProfessionId))
                {
                    var professionNames = string.Join("、", skill.AllowedProfessions.Select(GetProfessionName));
                    return (false, $"该技能仅限 {professionNames} 使用");
                }
            }

            // 检查等级要求
            if (skill.Unlock != null && skill.Unlock.MinLevel > 0)
            {
                if (characterLevel < skill.Unlock.MinLevel)
                {
                    return (false, $"需要等级 {skill.Unlock.MinLevel}（当前 Lv.{characterLevel}）");
                }
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 使用技能书
        /// Use skill book
        /// </summary>
        private (bool Success, string Message) UseSkillBook(
            ItemDefinition itemDef,
            CharacterData character,
            string currentProfessionId,
            int characterLevel)
        {
            var (canUse, reason) = CanUseSkillBook(itemDef, character, currentProfessionId, characterLevel);
            if (!canUse)
            {
                return (false, reason);
            }

            var skillId = itemDef.Metadata!.SkillId!;
            var skill = _skillRepository.GetSkill(skillId);

            // 学习技能
            character.LearnedSkills.Add(skillId);

            // 设置账户标记（如果配置了）
            if (!string.IsNullOrEmpty(itemDef.Metadata.AccountFlag))
            {
                character.AccountFlags.Add(itemDef.Metadata.AccountFlag);
            }

            return (true, $"成功学习技能：{skill?.Name ?? skillId}");
        }

        /// <summary>
        /// 获取职业中文名称
        /// Get profession Chinese name
        /// </summary>
        private string GetProfessionName(string professionId)
        {
            return professionId switch
            {
                "warrior" => "战士",
                "mage" => "法师",
                "ranger" => "游侠",
                "rogue" => "盗贼",
                _ => professionId
            };
        }
    }
}
