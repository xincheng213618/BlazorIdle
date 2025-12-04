using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Config;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Battle.Systems
{
    /// <summary>
    /// 消耗品处理器 - 从 MultiBattleInstance 提取的消耗品检查逻辑
    /// Consumable processor - Consumable check logic extracted from MultiBattleInstance
    /// 
    /// 职责:
    /// - 处理角色的消耗品检查 (ProcessCharacterConsumableChecks)
    /// - 处理单个消耗品槽位 (ProcessConsumableSlot)
    /// - 检查触发条件、冷却、库存
    /// - 触发消耗品使用和库存耗尽事件
    /// </summary>
    public class ConsumableProcessor
    {
        private readonly ConditionChecker _conditionChecker;
        private readonly IGameConfigService _gameConfigService;
        private readonly Func<string, CooldownManager> _getCooldownManager;
        private readonly IGameClock _clock;

        // 耗尽通知缓存，避免重复触发库存耗尽事件
        // Out of stock notification cache to avoid duplicate events
        private readonly Dictionary<string, HashSet<string>> _outOfStockNotified = new();

        /// <summary>
        /// 执行技能请求事件
        /// Execute skill request event
        /// </summary>
        public event Action<string, string, string, bool, EventSource>? OnExecuteSkillRequested;

        /// <summary>
        /// 消耗品使用事件
        /// Consumable used event
        /// </summary>
        public event Action<ConsumableUsedEvent>? OnConsumableUsed;

        /// <summary>
        /// 消耗品耗尽事件
        /// Consumable out of stock event
        /// </summary>
        public event Action<ConsumableOutOfStockEvent>? OnConsumableOutOfStock;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        public ConsumableProcessor(
            ConditionChecker conditionChecker,
            IGameConfigService gameConfigService,
            Func<string, CooldownManager> getCooldownManager,
            IGameClock clock)
        {
            _conditionChecker = conditionChecker ?? throw new ArgumentNullException(nameof(conditionChecker));
            _gameConfigService = gameConfigService ?? throw new ArgumentNullException(nameof(gameConfigService));
            _getCooldownManager = getCooldownManager ?? throw new ArgumentNullException(nameof(getCooldownManager));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// 处理角色的消耗品检查
        /// Process character's consumable checks
        /// 
        /// 触发优先级: 先药水后食物，按槽位顺序 (potion_1 → potion_2 → food_1 → food_2)
        /// Trigger priority: Potions first, then food, in slot order
        /// </summary>
        /// <param name="characterId">角色ID / Character ID</param>
        /// <param name="characterData">角色数据 / Character data</param>
        /// <param name="context">战斗上下文 / Battle context</param>
        public void ProcessCharacterConsumableChecks(
            string characterId,
            CharacterData characterData,
            BattleContext context)
        {
            // 获取角色当前职业的消耗品配置
            // Get character's consumable configuration for current profession
            var consumableConfig = characterData.GetConsumablesForProfession(characterData.ActiveCombatProfessionId);
            if (consumableConfig == null)
                return;

            // 处理药水槽位（优先触发，提供增益效果）
            // Process potion slots (higher priority, provides buff effects)
            foreach (var kvp in consumableConfig.PotionSlots.OrderBy(x => x.Key))
            {
                ProcessConsumableSlot(characterId, kvp.Key, kvp.Value, context, characterData);
            }

            // 处理食物槽位（其次触发，提供恢复效果）
            // Process food slots (lower priority, provides recovery effects)
            foreach (var kvp in consumableConfig.FoodSlots.OrderBy(x => x.Key))
            {
                ProcessConsumableSlot(characterId, kvp.Key, kvp.Value, context, characterData);
            }
        }

        /// <summary>
        /// 处理单个消耗品槽位
        /// Process a single consumable slot
        /// </summary>
        public void ProcessConsumableSlot(
            string characterId,
            string slotId,
            ConsumableSlotData slotData,
            BattleContext context,
            CharacterData characterData)
        {
            int nowMs = _clock.NowMs;

            // 检查槽位是否装备
            // Check if slot is equipped
            if (string.IsNullOrEmpty(slotData.ItemId) || string.IsNullOrEmpty(slotData.SkillId))
                return;

            // 获取物品配置
            // Get item configuration
            var itemConfig = _gameConfigService.GetItem(slotData.ItemId);
            if (itemConfig?.ConsumableConfig == null)
                return;

            // 检查触发条件（优先使用自定义条件，否则使用物品默认条件）
            // Check trigger conditions (prefer custom conditions, otherwise use item default)
            var effectiveConditions = slotData.GetEffectiveTriggerConditions(itemConfig.ConsumableConfig.TriggerConditions);
            if (effectiveConditions != null)
            {
                var tempSkill = new SkillDef { Conditions = effectiveConditions };
                if (!_conditionChecker.CheckConditions(tempSkill, context, isCasterPlayer: true, characterId))
                    return;
            }

            // 检查冷却（按物品ID追踪，同类物品共享冷却）
            // Check cooldown (tracked per item ID, same item type shares cooldown)
            var cooldownManager = _getCooldownManager(characterId);
            string cooldownKey = $"consumable_{slotData.ItemId}";
            if (!cooldownManager.IsReady(cooldownKey))
                return;

            // 检查背包库存（不自动卸载，保留配置等待玩家补充）
            // Check inventory (no auto-unequip, preserve config waiting for player to replenish)
            int currentCount = characterData.Inventory.GetItemQuantity(slotData.ItemId);
            if (currentCount <= 0)
            {
                // 检查是否已通知耗尽（防止重复通知）- 使用 GetOrAdd 模式避免双重查找
                // Check if already notified as out of stock (prevent duplicate notifications) - use GetOrAdd pattern to avoid double lookup
                if (!_outOfStockNotified.TryGetValue(characterId, out var notifiedItems))
                {
                    notifiedItems = new HashSet<string>();
                    _outOfStockNotified[characterId] = notifiedItems;
                }

                // 只在首次耗尽时触发事件
                // Only trigger event on first out of stock occurrence
                if (notifiedItems.Add(slotData.ItemId))
                {
                    // 库存不足，触发库存耗尽事件（UI可据此显示特殊样式）
                    // Out of stock, trigger event (UI can show special style)
                    OnConsumableOutOfStock?.Invoke(new ConsumableOutOfStockEvent
                    {
                        TimeMs = nowMs,
                        CharacterId = characterId,
                        ItemId = slotData.ItemId,
                        SlotId = slotId
                    });
                }
                return;
            }

            // 如果库存已补充，清除耗尽通知标记
            // If inventory is replenished, clear the out of stock notification flag
            if (_outOfStockNotified.TryGetValue(characterId, out var notified))
            {
                notified.Remove(slotData.ItemId);
            }

            // 扣除背包库存
            // Deduct from inventory
            if (!characterData.Inventory.TryConsumeItem(slotData.ItemId, 1))
                return;

            // 执行消耗品技能
            // Execute consumable skill
            OnExecuteSkillRequested?.Invoke(characterId, slotData.SkillId, $"consumable_{slotId}", true, EventSource.Consumable);

            // 启动冷却（使用物品配置的冷却时间）
            // Start cooldown (using item's cooldown time)
            double cooldownSec = itemConfig.ConsumableConfig.CooldownSec;
            if (cooldownSec > 0)
            {
                cooldownManager.StartCooldown(cooldownKey, cooldownSec);
            }

            // 触发消耗品使用事件
            // Trigger consumable used event
            OnConsumableUsed?.Invoke(new ConsumableUsedEvent
            {
                TimeMs = nowMs,
                CharacterId = characterId,
                ItemId = slotData.ItemId,
                SkillId = slotData.SkillId,
                SlotId = slotId,
                RemainingCount = characterData.Inventory.GetItemQuantity(slotData.ItemId)
            });
        }

        /// <summary>
        /// 清除耗尽通知缓存
        /// Clear out of stock notification cache
        /// </summary>
        public void ClearOutOfStockNotifications()
        {
            _outOfStockNotified.Clear();
        }

        /// <summary>
        /// 清除指定角色的耗尽通知缓存
        /// Clear out of stock notification cache for specific character
        /// </summary>
        public void ClearOutOfStockNotifications(string characterId)
        {
            _outOfStockNotified.Remove(characterId);
        }
    }
}
