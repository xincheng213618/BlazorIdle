using System;
using System.Collections.Generic;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;

namespace BlazorIdle.Game.Battle.Systems
{
    /// <summary>
    /// 定期技能处理器 - 从 MultiBattleInstance 提取的定期技能检查逻辑
    /// Periodic skill processor - Periodic skill check logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 处理玩家角色的定期技能检查 (ProcessCharacterPeriodicSkills)
    /// - 处理怪物的定期技能检查 (ProcessEnemyPeriodicSkills)
    /// - 检查 OnPeriodic 触发器并触发相应技能
    /// </summary>
    public class PeriodicSkillProcessor
    {
        private readonly SkillRepository _skillRepository;
        private readonly ConditionChecker _conditionChecker;
        private readonly RngContext _rng;
        private readonly Func<string, CooldownManager> _getCooldownManager;

        /// <summary>
        /// 执行技能请求事件
        /// Execute skill request event
        /// </summary>
        public event Action<string, string, string, bool, EventSource>? OnExecuteSkillRequested;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public PeriodicSkillProcessor(
            SkillRepository skillRepository,
            ConditionChecker conditionChecker,
            RngContext rng,
            Func<string, CooldownManager> getCooldownManager)
        {
            _skillRepository = skillRepository ?? throw new ArgumentNullException(nameof(skillRepository));
            _conditionChecker = conditionChecker ?? throw new ArgumentNullException(nameof(conditionChecker));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _getCooldownManager = getCooldownManager ?? throw new ArgumentNullException(nameof(getCooldownManager));
        }

        /// <summary>
        /// 处理玩家角色的定期技能检查
        /// Process character's periodic skill checks
        /// </summary>
        /// <param name="characterId">角色ID / Character ID</param>
        /// <param name="passiveSkillId">被动技能槽位的技能ID / Passive slot skill ID</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        public void ProcessCharacterPeriodicSkills(
            string characterId,
            string? passiveSkillId,
            BattleContext context)
        {
            // 快速路径 - 检查被动技能槽位是否有技能
            // Fast path - check if passive slot has a skill
            if (string.IsNullOrEmpty(passiveSkillId))
                return;

            var passiveSkill = _skillRepository.GetSkillById(passiveSkillId);
            if (passiveSkill == null || passiveSkill.Triggers == null || passiveSkill.Triggers.Count == 0)
                return;

            // 检查是否有 OnPeriodic 触发器
            // Check if there are OnPeriodic triggers
            foreach (var trigger in passiveSkill.Triggers)
            {
                if (trigger.When != "OnPeriodic")
                    continue;

                // 检查触发条件（使用trigger的conditions或技能的conditions）
                // Check trigger conditions (use trigger's conditions or skill's conditions)
                var conditionsToCheck = trigger.Conditions ?? passiveSkill.Conditions;
                if (conditionsToCheck != null)
                {
                    // 创建临时技能定义用于条件检查
                    // Create temporary skill definition for condition checking
                    var tempSkill = new SkillDef
                    {
                        Id = passiveSkill.Id,
                        Conditions = conditionsToCheck
                    };

                    if (!_conditionChecker.CheckConditions(tempSkill, context, isCasterPlayer: true, characterId))
                        continue;
                }

                // 检查概率触发
                // Check proc chance
                if (trigger.ProcChance < 1.0)
                {
                    double roll = _rng.NextDouble();
                    if (roll > trigger.ProcChance)
                        continue;
                }

                // 获取要触发的技能
                // Get the skill to trigger
                if (string.IsNullOrEmpty(trigger.FireSkillId))
                    continue;

                var skillToFire = _skillRepository.GetSkillById(trigger.FireSkillId);
                if (skillToFire == null)
                    continue;

                // 检查冷却（除非ignoreRequirements为true）
                // Check cooldown (unless ignoreRequirements is true)
                if (!trigger.IgnoreRequirements)
                {
                    var cooldownManager = _getCooldownManager(characterId);
                    if (!cooldownManager.IsReady(skillToFire.Id))
                        continue;
                }

                // 执行触发的技能
                // Execute the triggered skill
                OnExecuteSkillRequested?.Invoke(characterId, skillToFire.Id, "periodic_trigger", true, EventSource.Trigger);
            }
        }

        /// <summary>
        /// 处理怪物的定期技能检查
        /// Process enemy's periodic skill checks
        /// </summary>
        /// <param name="enemyId">怪物ID / Enemy ID</param>
        /// <param name="periodicSkillIds">定期技能ID列表 / Periodic skill IDs</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        public void ProcessEnemyPeriodicSkills(
            string enemyId,
            IList<string>? periodicSkillIds,
            BattleContext context)
        {
            // 快速路径 - 检查是否有定期技能
            // Fast path - check if there are periodic skills
            if (periodicSkillIds == null || periodicSkillIds.Count == 0)
                return;

            // 遍历怪物的定期技能
            // Iterate through monster's periodic skills
            foreach (var skillId in periodicSkillIds)
            {
                var skill = _skillRepository.GetSkillById(skillId);
                if (skill == null || skill.Triggers == null || skill.Triggers.Count == 0)
                    continue;

                // 检查是否有 OnPeriodic 触发器
                // Check if there are OnPeriodic triggers
                foreach (var trigger in skill.Triggers)
                {
                    if (trigger.When != "OnPeriodic")
                        continue;

                    // 检查触发条件（使用trigger的conditions或技能的conditions）
                    // Check trigger conditions (use trigger's conditions or skill's conditions)
                    var conditionsToCheck = trigger.Conditions ?? skill.Conditions;
                    if (conditionsToCheck != null)
                    {
                        // 创建临时技能定义用于条件检查
                        // Create temporary skill definition for condition checking
                        var tempSkill = new SkillDef
                        {
                            Id = skill.Id,
                            Conditions = conditionsToCheck
                        };

                        if (!_conditionChecker.CheckConditions(tempSkill, context, isCasterPlayer: false, enemyId))
                            continue;
                    }

                    // 检查概率触发
                    // Check proc chance
                    if (trigger.ProcChance < 1.0)
                    {
                        double roll = _rng.NextDouble();
                        if (roll > trigger.ProcChance)
                            continue;
                    }

                    // 获取要触发的技能
                    // Get the skill to trigger
                    if (string.IsNullOrEmpty(trigger.FireSkillId))
                        continue;

                    var skillToFire = _skillRepository.GetSkillById(trigger.FireSkillId);
                    if (skillToFire == null)
                        continue;

                    // 检查冷却（除非ignoreRequirements为true）
                    // Check cooldown (unless ignoreRequirements is true)
                    if (!trigger.IgnoreRequirements)
                    {
                        var cooldownManager = _getCooldownManager(enemyId);
                        if (!cooldownManager.IsReady(skillToFire.Id))
                            continue;
                    }

                    // 执行触发的技能
                    // Execute the triggered skill
                    OnExecuteSkillRequested?.Invoke(enemyId, skillToFire.Id, "periodic_trigger", false, EventSource.Trigger);
                }
            }
        }
    }
}
