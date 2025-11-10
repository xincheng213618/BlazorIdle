using System.Collections.Generic;

namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能解析器接口 - 负责技能效果的计算和应用
    /// Skill resolver interface - responsible for calculating and applying skill effects
    /// </summary>
    public interface ISkillResolver
    {
        /// <summary>
        /// 施放单个技能
        /// Cast a single skill
        /// </summary>
        /// <param name="skillId">技能ID（如 "attack_basic", "special_pulse"）</param>
        /// <param name="ctx">战斗上下文</param>
        /// <param name="opts">施放选项（可选）</param>
        /// <returns>技能施放结果</returns>
        SkillCastResult Cast(string skillId, BattleContext ctx, SkillCastOptions? opts = null);

        /// <summary>
        /// 成组施放多个技能（Bundle Cast）
        /// Cast multiple skills as a bundle
        /// </summary>
        /// <param name="skillIds">技能ID列表（索引0为主技能，其余为跟随技能）</param>
        /// <param name="ctx">战斗上下文</param>
        /// <param name="opts">施放选项</param>
        /// <returns>每个技能的施放结果列表</returns>
        IReadOnlyList<SkillCastResult> CastBundle(IReadOnlyList<string> skillIds, BattleContext ctx, SkillCastOptions opts);
    }
}
