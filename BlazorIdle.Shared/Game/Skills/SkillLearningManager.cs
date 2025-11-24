using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Shared.Models;
using BlazorIdle.Game.Purchase;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能学习管理器 (Step 2 Phase 2.5, Step 4 扩展)
    /// Skill learning manager
    /// </summary>
    public sealed class SkillLearningManager
    {
        private readonly SkillRepository _skillRepository;
        private PurchaseService? _purchaseService;

        public SkillLearningManager(SkillRepository skillRepository)
        {
            _skillRepository = skillRepository;
        }

        /// <summary>
        /// 初始化购买服务（可选，用于付费学习）
        /// Initialize purchase service (optional, for paid learning)
        /// </summary>
        public void InitializePurchaseService(PurchaseState purchaseState)
        {
            _purchaseService = new PurchaseService(purchaseState);
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

            // 固定技能或没有unlock属性的技能不能学习（如触发技能）
            // Fixed skills or skills without unlock property cannot be learned (e.g., trigger skills)
            if (skill.Fixed || skill.Unlock == null)
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
                .Where(skill => !skill.Fixed && skill.Unlock != null && CanLearnSkill(characterData, skill.Id, characterLevel, currentProfessionId))
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

        /// <summary>
        /// 学习技能（支持付费购买）- Step 4
        /// Learn skill (with purchase support)
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="skillId">技能ID</param>
        /// <param name="characterLevel">角色等级</param>
        /// <param name="currentProfessionId">当前职业ID</param>
        /// <returns>学习结果</returns>
        public (bool Success, string Message) LearnSkillWithPurchase(
            CharacterData characterData,
            string skillId,
            int characterLevel,
            string currentProfessionId)
        {
            var skill = _skillRepository.GetSkill(skillId);
            if (skill == null)
                return (false, "技能不存在");

            // 已学习检查
            if (characterData.LearnedSkills.Contains(skillId))
                return (false, "已经学会此技能");

            // 如果技能配置了购买要求且启用了购买
            if (skill.Purchase?.Enabled == true)
            {
                if (_purchaseService == null)
                    return (false, "暂时无法购买此技能");

                // 检查是否可以在商店直接购买
                if (!skill.Purchase.CanBuyInShop)
                    return (false, "此技能只能通过技能书学习");

                // 解析货币类型
                var currencyType = ParseCurrencyType(skill.Purchase.CurrencyType);

                // 构建购买配置
                var purchaseConfig = new PurchasableConfig
                {
                    Id = skillId,
                    DisplayName = skill.Name,
                    CurrencyType = currencyType,
                    BasePrice = skill.Purchase.BasePrice,
                    DiscountPercent = skill.Purchase.DiscountPercent,
                    Limits = skill.Purchase.Limits ?? new PurchaseLimit(),
                    Unlock = ConvertUnlockConfig(skill.Unlock, skill)
                };

                // 先检查是否可以购买
                var checkResult = _purchaseService.CanPurchase(
                    purchaseConfig,
                    characterData,
                    currentProfessionId,
                    characterLevel);

                if (!checkResult.CanPurchase)
                    return (false, checkResult.Reason ?? "无法购买");

                // 执行购买
                var purchaseResult = _purchaseService.Purchase(
                    purchaseConfig,
                    characterData,
                    currentProfessionId,
                    characterLevel,
                    (c) => c.LearnedSkills.Add(skillId)
                );

                if (!purchaseResult.Success)
                    return (false, purchaseResult.Message);

                string currencyName = CurrencyHelper.GetCurrencyName(currencyType);
                return (true, $"成功学习技能（消耗 {purchaseResult.SpentAmount} {currencyName}）");
            }
            else
            {
                // 免费学习（使用原有逻辑）
                if (!CanLearnSkill(characterData, skillId, characterLevel, currentProfessionId))
                    return (false, "不满足学习条件");

                characterData.LearnedSkills.Add(skillId);
                return (true, "成功学习技能");
            }
        }

        /// <summary>
        /// 检查是否可以负担技能价格
        /// Check if can afford skill price
        /// </summary>
        public bool CanAffordSkill(CharacterData characterData, SkillDef skill)
        {
            if (skill.Purchase?.Enabled != true)
                return true; // 免费技能总是可负担

            var currencyType = ParseCurrencyType(skill.Purchase.CurrencyType);
            int finalPrice = PurchaseService.CalculatePrice(
                skill.Purchase.BasePrice,
                skill.Purchase.DiscountPercent);

            return CurrencyHelper.HasEnough(characterData.Inventory, currencyType, finalPrice);
        }

        /// <summary>
        /// 获取技能最终价格
        /// Get skill final price
        /// </summary>
        public int GetSkillFinalPrice(SkillDef skill)
        {
            if (skill.Purchase?.Enabled != true)
                return 0;

            return PurchaseService.CalculatePrice(
                skill.Purchase.BasePrice,
                skill.Purchase.DiscountPercent);
        }

        /// <summary>
        /// 获取技能剩余可购买次数
        /// Get remaining purchases for skill
        /// </summary>
        public int GetRemainingPurchases(CharacterData characterData, string skillId)
        {
            var skill = _skillRepository.GetSkill(skillId);
            if (skill == null || skill.Purchase?.Enabled != true || _purchaseService == null)
                return -1; // -1 表示无限制或不适用

            return _purchaseService.GetRemainingPurchases(
                skillId,
                characterData.Id,
                characterData.UserId.ToString(),
                skill.Purchase.Limits ?? new PurchaseLimit());
        }

        /// <summary>
        /// 解析货币类型
        /// Parse currency type
        /// </summary>
        private CurrencyType ParseCurrencyType(string currencyTypeStr)
        {
            return currencyTypeStr?.ToLower() switch
            {
                "gold" => CurrencyType.Gold,
                "gem" => CurrencyType.Gem,
                "token" => CurrencyType.Token,
                _ => CurrencyType.Gold
            };
        }

        /// <summary>
        /// 转换解锁配置到购买解锁条件
        /// Convert unlock config to purchase unlock condition
        /// </summary>
        private UnlockCondition? ConvertUnlockConfig(UnlockConfig? unlockConfig, SkillDef skill)
        {
            if (unlockConfig == null && skill.AllowedProfessions == null)
                return null;

            return new UnlockCondition
            {
                MinLevel = unlockConfig?.MinLevel ?? 0,
                AllowedProfessions = skill.AllowedProfessions?.ToList(),
                RequireAccountFlags = unlockConfig?.AccountFlags?.ToList(),
                RequireLearnedSkills = null, // 技能前置条件可以从 UnlockConfig 扩展
                Message = null // 可以从 unlockConfig 扩展添加自定义消息
            };
        }
    }
}
