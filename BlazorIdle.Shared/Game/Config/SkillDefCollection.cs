using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Config
{
    /// <summary>
    /// 技能定义集合
    /// Skill definition collection
    /// </summary>
    public sealed class SkillDefCollection
    {
        /// <summary>
        /// 技能定义字典
        /// Skill definition dictionary
        /// Key: 技能ID
        /// Value: 技能定义
        /// </summary>
        public Dictionary<string, SkillDef> Skills { get; set; } = new()
        {
            [SkillIds.AttackBasic] = new SkillDef
            {
                Id = SkillIds.AttackBasic,
                CastTimeSec = 0
            },
            [SkillIds.SpecialPulse] = new SkillDef
            {
                Id = SkillIds.SpecialPulse,
                CastTimeSec = 0
                // Note: AOE determined by targetPolicy, not IsAoe property
            },
            [SkillIds.EnemyAttackBasic] = new SkillDef
            {
                Id = SkillIds.EnemyAttackBasic,
                CastTimeSec = 0
            }
        };
        
        /// <summary>
        /// 验证技能定义的完整性
        /// Validate skill definition integrity
        /// </summary>
        /// <returns>验证错误列表，为空则验证通过 / List of validation errors, empty if valid</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();
            
            // 检查是否有空的技能 ID
            // Check for empty skill IDs
            if (Skills.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key)))
            {
                errors.Add("SkillDefCollection contains empty skill ID");
            }
            
            // 检查每个技能定义
            // Check each skill definition
            foreach (var (skillId, skillDef) in Skills)
            {
                if (skillDef == null)
                {
                    errors.Add($"Skill '{skillId}' has null definition");
                    continue;
                }
                
                // 检查技能定义的 ID 是否与字典的 key 一致
                // Check if skill definition ID matches dictionary key
                if (skillDef.Id != skillId)
                {
                    errors.Add($"Skill ID mismatch: dictionary key is '{skillId}' but SkillDef.Id is '{skillDef.Id}'");
                }
                
                // 检查施法时间是否为负数
                // Check if cast time is negative
                if (skillDef.CastTimeSec < 0)
                {
                    errors.Add($"Skill '{skillId}' has negative cast time: {skillDef.CastTimeSec}");
                }
            }
            
            return errors;
        }
    }
}
