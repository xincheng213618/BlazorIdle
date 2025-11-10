using System.Collections.Generic;
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
            ["attack_basic"] = new SkillDef
            {
                Id = "attack_basic",
                CastTimeSec = 0,
                IsAoe = false
            },
            ["special_pulse"] = new SkillDef
            {
                Id = "special_pulse",
                CastTimeSec = 0,
                IsAoe = true  // 特殊技能可能是 AOE，可以打击所有敌人
                              // Special skill may be AOE, can hit all enemies
            },
            ["enemy_attack_basic"] = new SkillDef
            {
                Id = "enemy_attack_basic",
                CastTimeSec = 0,
                IsAoe = false
            }
        };
    }
}
