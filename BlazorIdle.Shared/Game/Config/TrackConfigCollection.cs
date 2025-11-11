using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game.Tracks;

namespace BlazorIdle.Game.Config
{
    /// <summary>
    /// Track 配置集合
    /// Track configuration collection
    /// </summary>
    public sealed class TrackConfigCollection
    {
        /// <summary>
        /// Track 配置字典
        /// Track configuration dictionary
        /// Key: Track ID (如 "attack", "special", "enemy_attack")
        /// Value: Track 配置
        /// </summary>
        public Dictionary<string, TrackConfig> Tracks { get; set; } = new()
        {
            ["attack"] = new TrackConfig
            {
                BoundSkills = new List<string> { "attack_basic" },
                OnFireTriggers = new List<string>(),
                ProgressPolicy = ProgressPolicy.Presence
            },
            ["special"] = new TrackConfig
            {
                BoundSkills = new List<string> { "special_pulse" },
                OnFireTriggers = new List<string>(),
                ProgressPolicy = ProgressPolicy.Presence
            },
            ["enemy_attack"] = new TrackConfig
            {
                BoundSkills = new List<string> { "enemy_attack_basic" },
                OnFireTriggers = new List<string>(),
                ProgressPolicy = ProgressPolicy.Presence
            }
        };
        
        /// <summary>
        /// 验证配置的完整性
        /// Validate configuration integrity
        /// </summary>
        /// <param name="skillDefs">技能定义集合用于验证引用 / Skill definitions for reference validation</param>
        /// <returns>验证错误列表，为空则验证通过 / List of validation errors, empty if valid</returns>
        public List<string> Validate(SkillDefCollection? skillDefs = null)
        {
            var errors = new List<string>();
            
            // 检查是否有空的 Track ID
            // Check for empty track IDs
            if (Tracks.Any(kvp => string.IsNullOrWhiteSpace(kvp.Key)))
            {
                errors.Add("TrackConfigCollection contains empty track ID");
            }
            
            // 检查每个 Track 配置
            // Check each track configuration
            foreach (var (trackId, config) in Tracks)
            {
                if (config == null)
                {
                    errors.Add($"Track '{trackId}' has null configuration");
                    continue;
                }
                
                // 检查 BoundSkills 是否为空
                // Check if BoundSkills is null
                if (config.BoundSkills == null)
                {
                    errors.Add($"Track '{trackId}' has null BoundSkills");
                    continue;
                }
                
                // 如果提供了技能定义，验证技能引用
                // If skill definitions provided, validate skill references
                if (skillDefs != null)
                {
                    foreach (var skillId in config.BoundSkills)
                    {
                        if (!skillDefs.Skills.ContainsKey(skillId))
                        {
                            errors.Add($"Track '{trackId}' references non-existent skill '{skillId}'");
                        }
                    }
                    
                    // 验证 OnFireTriggers
                    // Validate OnFireTriggers
                    if (config.OnFireTriggers != null)
                    {
                        foreach (var skillId in config.OnFireTriggers)
                        {
                            if (!skillDefs.Skills.ContainsKey(skillId))
                            {
                                errors.Add($"Track '{trackId}' OnFireTriggers references non-existent skill '{skillId}'");
                            }
                        }
                    }
                }
            }
            
            return errors;
        }
    }
}
