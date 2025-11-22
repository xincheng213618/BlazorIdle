using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Purchase
{
    /// <summary>
    /// 解锁条件检查器 - 统一验证职业、等级、账户标记、前置技能等条件
    /// Unlock condition checker - unified validation for profession, level, account flags, prerequisite skills, etc.
    /// </summary>
    public class UnlockConditionChecker
    {
        /// <summary>
        /// 检查解锁条件
        /// Check unlock conditions
        /// </summary>
        /// <param name="condition">解锁条件</param>
        /// <param name="character">角色数据</param>
        /// <param name="currentProfessionId">当前职业ID</param>
        /// <param name="characterLevel">角色等级</param>
        /// <returns>解锁检查结果</returns>
        public UnlockCheckResult Check(
            UnlockCondition? condition,
            CharacterData character,
            string currentProfessionId,
            int characterLevel)
        {
            // 如果没有解锁条件，直接通过
            if (condition == null)
                return UnlockCheckResult.Unlocked();

            // 1. 职业检查
            if (condition.AllowedProfessions?.Count > 0)
            {
                if (!condition.AllowedProfessions.Contains(currentProfessionId))
                {
                    string professionNames = string.Join("、", condition.AllowedProfessions.Select(GetProfessionName));
                    return UnlockCheckResult.Locked(
                        condition.Message ?? $"该商品仅限 {professionNames} 购买");
                }
            }

            // 2. 等级检查
            if (condition.MinLevel > 0 && characterLevel < condition.MinLevel)
            {
                return UnlockCheckResult.Locked(
                    condition.Message ?? $"需要等级 {condition.MinLevel}（当前 Lv.{characterLevel}）");
            }

            // 3. 账户Flag检查
            if (condition.RequireAccountFlags?.Count > 0)
            {
                foreach (var flag in condition.RequireAccountFlags)
                {
                    if (!character.AccountFlags.Contains(flag))
                    {
                        return UnlockCheckResult.Locked(
                            condition.Message ?? "未满足账户条件");
                    }
                }
            }

            // 4. 前置技能检查
            if (condition.RequireLearnedSkills?.Count > 0)
            {
                foreach (var skillId in condition.RequireLearnedSkills)
                {
                    if (!character.LearnedSkills.Contains(skillId))
                    {
                        return UnlockCheckResult.Locked(
                            condition.Message ?? $"需要已掌握前置技能");
                    }
                }
            }

            return UnlockCheckResult.Unlocked();
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
