namespace BlazorIdle.Game.Skills
{
    /// <summary>
    /// 技能槽位配置 (Step 2 Phase 2)
    /// Skill slot configuration
    /// </summary>
    public sealed class SkillSlotConfig
    {
        /// <summary>
        /// 槽位ID（如 "active_1", "active_2", "active_3", "passive_1"）
        /// Slot ID (e.g., "active_1", "active_2", "active_3", "passive_1")
        /// </summary>
        public string SlotId { get; set; } = "";

        /// <summary>
        /// 槽位类型：active 或 passive
        /// Slot type: active or passive
        /// </summary>
        public string SlotType { get; set; } = "active";

        /// <summary>
        /// 装备的技能ID（null 表示槽位为空）
        /// Equipped skill ID (null means slot is empty)
        /// </summary>
        public string? SkillId { get; set; }

        /// <summary>
        /// 是否为固定槽位（固定槽位不可修改，自动装配固定技能）
        /// Whether this is a fixed slot (fixed slots cannot be modified, auto-equip fixed skills)
        /// </summary>
        public bool IsFixed { get; set; }
    }
}
