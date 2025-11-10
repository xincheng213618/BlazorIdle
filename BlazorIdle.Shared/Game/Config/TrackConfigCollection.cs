using System.Collections.Generic;
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
    }
}
