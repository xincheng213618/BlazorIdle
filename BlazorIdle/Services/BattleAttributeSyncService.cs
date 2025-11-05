using BlazorIdle.Shared.Events;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Services
{
    /// <summary>
    /// 战斗属性同步服务 - 负责在战斗中同步角色属性变化
    /// Battle Attribute Sync Service - Syncs character attribute changes during combat
    /// </summary>
    public interface IBattleAttributeSyncService
    {
        /// <summary>
        /// 战斗中角色升级事件
        /// Character leveled up during combat event
        /// </summary>
        event EventHandler<CharacterLeveledUpInCombatEventArgs>? CharacterLeveledUpInCombat;

        /// <summary>
        /// 触发战斗中升级事件
        /// Trigger level-up in combat event
        /// </summary>
        void TriggerCombatLevelUp(CharacterData character, string professionId, int oldLevel, int newLevel, long expGained);
    }

    public class BattleAttributeSyncService : IBattleAttributeSyncService
    {
        private readonly ILogger<BattleAttributeSyncService> _logger;

        public event EventHandler<CharacterLeveledUpInCombatEventArgs>? CharacterLeveledUpInCombat;

        public BattleAttributeSyncService(ILogger<BattleAttributeSyncService> logger)
        {
            _logger = logger;
        }

        public void TriggerCombatLevelUp(CharacterData character, string professionId, int oldLevel, int newLevel, long expGained)
        {
            _logger.LogInformation(
                "Character {CharacterId} leveled up from {OldLevel} to {NewLevel} during combat (Profession: {ProfessionId})",
                character.Id, oldLevel, newLevel, professionId);

            CharacterLeveledUpInCombat?.Invoke(this, new CharacterLeveledUpInCombatEventArgs
            {
                Character = character,
                ProfessionId = professionId,
                OldLevel = oldLevel,
                NewLevel = newLevel,
                ExperienceGained = expGained
            });
        }
    }
}
