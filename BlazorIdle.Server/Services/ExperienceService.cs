using BlazorIdle.Shared.Models;
using BlazorIdle.Shared.Events;

namespace BlazorIdle.Server.Services
{
    /// <summary>
    /// 经验服务 - 处理经验计算和职业升级逻辑
    /// Experience service - handles experience calculation and profession level-up logic
    /// </summary>
    public interface IExperienceService
    {
        /// <summary>
        /// 职业升级事件 - Task 3.2
        /// Profession level-up event
        /// </summary>
        event EventHandler<ProfessionLevelUpEventArgs>? ProfessionLeveledUp;

        /// <summary>
        /// 计算实际获得的经验值（应用增益系数）
        /// Calculate actual experience gained (apply multiplier)
        /// </summary>
        /// <param name="baseExperience">基础经验值</param>
        /// <param name="multiplier">增益系数（默认0，预留给buff系统）</param>
        /// <returns>实际经验值</returns>
        long CalculateActualExperience(long baseExperience, double multiplier = 0.0);

        /// <summary>
        /// 为职业增加经验并处理升级
        /// Add experience to profession and handle level-up
        /// </summary>
        /// <param name="character">角色数据（用于事件）</param>
        /// <param name="progress">职业进度数据</param>
        /// <param name="experience">要增加的经验值</param>
        /// <returns>是否发生了升级</returns>
        bool AddExperience(CharacterData character, ProfessionProgress progress, long experience);

        /// <summary>
        /// 处理单次升级
        /// Process a single level-up
        /// </summary>
        /// <param name="progress">职业进度数据</param>
        void ProcessLevelUp(ProfessionProgress progress);
    }

    public class ExperienceService : IExperienceService
    {
        private readonly IGameConfigProvider _gameConfig;
        private readonly ILogger<ExperienceService> _logger;

        /// <summary>
        /// Task 3.2: 职业升级事件
        /// Profession level-up event
        /// </summary>
        public event EventHandler<ProfessionLevelUpEventArgs>? ProfessionLeveledUp;

        public ExperienceService(IGameConfigProvider gameConfig, ILogger<ExperienceService> logger)
        {
            _gameConfig = gameConfig;
            _logger = logger;
        }

        public long CalculateActualExperience(long baseExperience, double multiplier = 0.0)
        {
            // 实际经验 = 基础经验 × (1 + 增益系数)
            // Actual experience = base experience × (1 + multiplier)
            
            // TODO: 未来从buff系统获取增益系数
            // TODO: Future - get multiplier from buff system
            
            double actualExp = baseExperience * (1.0 + multiplier);
            return (long)Math.Max(0, actualExp);
        }

        public bool AddExperience(CharacterData character, ProfessionProgress progress, long experience)
        {
            if (experience <= 0) return false;

            progress.Experience += experience;
            bool leveledUp = false;
            int oldLevel = progress.Level;

            // 检查是否可以升级（可能连续升级多次）
            // Check if can level up (may level up multiple times)
            while (progress.Experience >= progress.ExperienceToNext && progress.Level < 100) // 最大等级100
            {
                ProcessLevelUp(progress);
                leveledUp = true;
            }

            // Task 3.2: 如果发生了升级，触发事件
            // If leveled up, trigger event
            if (leveledUp)
            {
                ProfessionLeveledUp?.Invoke(this, new ProfessionLevelUpEventArgs
                {
                    Character = character,
                    ProfessionId = progress.ProfessionId,
                    OldLevel = oldLevel,
                    NewLevel = progress.Level
                });

                _logger.LogInformation(
                    "Character {CharacterId} profession {ProfessionId} leveled up from {OldLevel} to {NewLevel}",
                    character.Id, progress.ProfessionId, oldLevel, progress.Level);
            }

            return leveledUp;
        }

        public void ProcessLevelUp(ProfessionProgress progress)
        {
            // 1. 增加等级
            // 1. Increase level
            progress.Level++;

            // 2. 扣除经验，保留溢出部分
            // 2. Deduct experience, keep overflow
            progress.Experience -= progress.ExperienceToNext;

            // 3. 查表更新下一级所需经验
            // 3. Update experience required for next level from config
            progress.ExperienceToNext = _gameConfig.GetExperienceRequired(progress.Level + 1);

            _logger.LogInformation("Profession {ProfessionId} leveled up to {Level}", 
                progress.ProfessionId, progress.Level);

            // 注意：简化设计，不增加属性，不解锁技能
            // Note: Simplified design - no stat growth, no skill unlocking
        }
    }
}
