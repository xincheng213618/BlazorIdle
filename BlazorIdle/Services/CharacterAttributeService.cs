using BlazorIdle.Shared.Game;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Services
{
    /// <summary>
    /// 角色属性服务 - 负责计算和更新角色属性
    /// </summary>
    public interface ICharacterAttributeService
    {
        Task<CalculatedAttributes?> CalculateAttributesAsync(CharacterData character);
        Task RecalculateAndApplyAsync(CharacterData character);
    }

    public class CharacterAttributeService : ICharacterAttributeService
    {
        private readonly IProfessionAttributeService _professionAttributeService;
        private readonly ILogger<CharacterAttributeService> _logger;

        public CharacterAttributeService(
            IProfessionAttributeService professionAttributeService,
            ILogger<CharacterAttributeService> logger)
        {
            _professionAttributeService = professionAttributeService;
            _logger = logger;
        }

        /// <summary>
        /// 计算角色属性（不应用到角色）
        /// </summary>
        public async Task<CalculatedAttributes?> CalculateAttributesAsync(CharacterData character)
        {
            try
            {
                // 获取当前激活职业的配置
                var professionId = character.ActiveCombatProfessionId;
                var config = await _professionAttributeService.GetConfigAsync(professionId);

                if (config == null)
                {
                    _logger.LogWarning(
                        "Profession config not found for profession {ProfessionId}", 
                        professionId);
                    return null;
                }

                // 获取当前职业的等级
                var level = GetProfessionLevel(character, professionId);

                // 执行计算 (MVP阶段装备加成为null)
                var result = AttributeCalculator.Calculate(level, config, equipBonuses: null);

                _logger.LogDebug(
                    "Calculated attributes for character {CharacterId}, profession {ProfessionId}, level {Level}: " +
                    "MainStat={MainStat}, Damage={Damage}, HP={HP}",
                    character.Id, professionId, level, 
                    result.MainStatTotal, result.DamagePerAttack, result.MaxHp);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error calculating attributes for character {CharacterId}", 
                    character.Id);
                return null;
            }
        }

        /// <summary>
        /// 重新计算并应用属性到角色
        /// </summary>
        public async Task RecalculateAndApplyAsync(CharacterData character)
        {
            var calculatedAttrs = await CalculateAttributesAsync(character);

            if (calculatedAttrs != null)
            {
                calculatedAttrs.ApplyToCharacterData(character);
                
                _logger.LogInformation(
                    "Applied calculated attributes to character {CharacterId}: " +
                    "Damage={Damage}, HP={HP}, Crit={Crit:P2}, Haste={Haste:P2}",
                    character.Id,
                    character.DamagePerAttack,
                    character.MaxHp,
                    character.CritChancePercent,
                    character.HastePercent);
            }
        }

        /// <summary>
        /// 获取指定职业的等级
        /// </summary>
        private int GetProfessionLevel(CharacterData character, string professionId)
        {
            if (character.Professions.TryGetValue(professionId, out var progress))
            {
                return progress.Level;
            }

            _logger.LogWarning(
                "Profession progress not found for {ProfessionId}, using level 1", 
                professionId);
            return 1;
        }
    }
}
