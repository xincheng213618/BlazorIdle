using BlazorIdle.Shared.Models;

namespace BlazorIdle.Shared.Events
{
    /// <summary>
    /// 职业升级事件参数
    /// Profession level-up event arguments
    /// </summary>
    public class ProfessionLevelUpEventArgs : EventArgs
    {
        public CharacterData Character { get; set; } = null!;
        public string ProfessionId { get; set; } = string.Empty;
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
    }

    /// <summary>
    /// 战斗中角色升级事件参数
    /// Character leveled up during combat event arguments
    /// </summary>
    public class CharacterLeveledUpInCombatEventArgs : EventArgs
    {
        public CharacterData Character { get; set; } = null!;
        public string ProfessionId { get; set; } = string.Empty;
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
        public long ExperienceGained { get; set; }
    }
}
