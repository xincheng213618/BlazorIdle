using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 职业装备的技能配置 (Step 2 Phase 2.5)
    /// Equipped skills configuration for a profession
    /// </summary>
    public sealed class EquippedSkillsConfig
    {
        /// <summary>
        /// 职业ID
        /// Profession ID
        /// </summary>
        [JsonPropertyName("professionId")]
        public string ProfessionId { get; set; } = string.Empty;

        /// <summary>
        /// 主动技能槽位 (3个槽位)
        /// Active skill slots (3 slots)
        /// Key: slotId (active_1, active_2, active_3)
        /// Value: skillId
        /// </summary>
        [JsonPropertyName("activeSlots")]
        public Dictionary<string, string?> ActiveSlots { get; set; } = new Dictionary<string, string?>
        {
            { "active_1", null },
            { "active_2", null },
            { "active_3", null }
        };

        /// <summary>
        /// 被动技能槽位 (1个槽位)
        /// Passive skill slot (1 slot)
        /// </summary>
        [JsonPropertyName("passiveSlot")]
        public string? PassiveSlot { get; set; }
    }
}
