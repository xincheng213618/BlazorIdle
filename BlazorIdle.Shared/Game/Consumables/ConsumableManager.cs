using System;
using System.Collections.Generic;
using System.Linq;
using BlazorIdle.Game.Config;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Consumables
{
    /// <summary>
    /// 消耗品管理操作结果
    /// Consumable management operation result
    /// </summary>
    public enum ConsumableOperationResult
    {
        /// <summary>成功 / Success</summary>
        Success,
        
        /// <summary>物品不存在 / Item not found</summary>
        ItemNotFound,
        
        /// <summary>物品不是消耗品 / Item is not a consumable</summary>
        NotConsumable,
        
        /// <summary>物品类别与槽位不匹配 / Item category doesn't match slot type</summary>
        CategoryMismatch,
        
        /// <summary>物品已装备在其他槽位 / Item already equipped in another slot</summary>
        AlreadyEquipped,
        
        /// <summary>背包库存不足 / Insufficient inventory</summary>
        InsufficientInventory,
        
        /// <summary>槽位ID无效 / Invalid slot ID</summary>
        InvalidSlotId,
        
        /// <summary>战斗中禁止修改 / Modification locked during battle</summary>
        LockedInBattle,
        
        /// <summary>角色数据无效 / Invalid character data</summary>
        InvalidCharacterData
    }

    /// <summary>
    /// 消耗品管理器 - 处理消耗品的装备和卸载逻辑
    /// Consumable Manager - handles consumable equipment and unequipment logic
    /// 
    /// 设计说明：
    /// - 槽位只保存物品ID引用（引用模式）
    /// - 战斗中每次使用实时从背包扣除
    /// - 战斗中禁止修改配置
    /// - 同一物品不能装备多个槽位
    /// 
    /// Design notes:
    /// - Slots only store item ID reference (reference mode)
    /// - Each battle use deducts from inventory in real-time
    /// - Configuration is locked during battle
    /// - Same item cannot be equipped in multiple slots
    /// </summary>
    public sealed class ConsumableManager
    {
        private readonly IGameConfigService _configService;

        /// <summary>
        /// 有效的药水槽位ID列表 - 引用自 ConsumableEquipmentConfig
        /// Valid potion slot IDs - referenced from ConsumableEquipmentConfig
        /// </summary>
        public static string[] PotionSlotIds => ConsumableEquipmentConfig.PotionSlotIds;

        /// <summary>
        /// 有效的食物槽位ID列表 - 引用自 ConsumableEquipmentConfig
        /// Valid food slot IDs - referenced from ConsumableEquipmentConfig
        /// </summary>
        public static string[] FoodSlotIds => ConsumableEquipmentConfig.FoodSlotIds;

        /// <summary>
        /// 所有有效的槽位ID列表 - 缓存以避免重复分配
        /// All valid slot IDs - cached to avoid repeated allocations
        /// </summary>
        private static readonly string[] _allSlotIds = 
            ConsumableEquipmentConfig.PotionSlotIds
                .Concat(ConsumableEquipmentConfig.FoodSlotIds)
                .ToArray();

        /// <summary>
        /// 所有有效的槽位ID列表
        /// All valid slot IDs
        /// </summary>
        public static string[] AllSlotIds => _allSlotIds;

        /// <summary>
        /// 构造函数
        /// Constructor
        /// </summary>
        /// <param name="configService">游戏配置服务</param>
        public ConsumableManager(IGameConfigService configService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        #region 装备/卸载操作

        /// <summary>
        /// 装备消耗品到指定槽位
        /// Equip a consumable to a specified slot
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="slotId">槽位ID (potion_1, potion_2, food_1, food_2)</param>
        /// <param name="itemId">物品ID</param>
        /// <param name="isInBattle">是否在战斗中</param>
        /// <returns>操作结果</returns>
        public ConsumableOperationResult EquipConsumable(
            CharacterData characterData,
            string slotId,
            string itemId,
            bool isInBattle = false)
        {
            // 战斗中禁止修改
            if (isInBattle)
                return ConsumableOperationResult.LockedInBattle;

            // 验证角色数据
            if (characterData == null)
                return ConsumableOperationResult.InvalidCharacterData;

            // 验证槽位ID
            if (!IsValidSlotId(slotId))
                return ConsumableOperationResult.InvalidSlotId;

            // 获取物品配置
            var itemDef = _configService.GetItem(itemId);
            if (itemDef == null)
                return ConsumableOperationResult.ItemNotFound;

            // 验证物品是消耗品
            if (itemDef.Type != ItemType.Consumable || itemDef.ConsumableConfig == null)
                return ConsumableOperationResult.NotConsumable;

            // 验证物品类别与槽位类型匹配
            if (!IsCategoryMatchingSlot(itemDef.ConsumableConfig.Category, slotId))
                return ConsumableOperationResult.CategoryMismatch;

            // 确保角色有消耗品配置
            if (characterData.EquippedConsumables == null)
                characterData.EquippedConsumables = new ConsumableEquipmentConfig();

            // 验证物品没有装备在其他槽位
            var existingSlot = characterData.EquippedConsumables.GetEquippedSlotId(itemId);
            if (existingSlot != null && existingSlot != slotId)
                return ConsumableOperationResult.AlreadyEquipped;

            // 验证背包有库存
            int quantity = characterData.Inventory.GetItemQuantity(itemId);
            if (quantity <= 0)
                return ConsumableOperationResult.InsufficientInventory;

            // 获取槽位并更新
            var slot = characterData.EquippedConsumables.GetSlot(slotId);
            if (slot == null)
                return ConsumableOperationResult.InvalidSlotId;

            slot.ItemId = itemId;
            slot.SkillId = itemDef.ConsumableConfig.SkillId;

            return ConsumableOperationResult.Success;
        }

        /// <summary>
        /// 卸载指定槽位的消耗品
        /// Unequip the consumable from a specified slot
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="slotId">槽位ID</param>
        /// <param name="isInBattle">是否在战斗中</param>
        /// <returns>操作结果</returns>
        public ConsumableOperationResult UnequipConsumable(
            CharacterData characterData,
            string slotId,
            bool isInBattle = false)
        {
            // 战斗中禁止修改
            if (isInBattle)
                return ConsumableOperationResult.LockedInBattle;

            // 验证角色数据
            if (characterData == null)
                return ConsumableOperationResult.InvalidCharacterData;

            // 验证槽位ID
            if (!IsValidSlotId(slotId))
                return ConsumableOperationResult.InvalidSlotId;

            // 确保角色有消耗品配置
            if (characterData.EquippedConsumables == null)
                return ConsumableOperationResult.Success; // 没有配置，视为已卸载

            // 获取槽位并清空
            var slot = characterData.EquippedConsumables.GetSlot(slotId);
            if (slot == null)
                return ConsumableOperationResult.InvalidSlotId;

            slot.Clear();

            return ConsumableOperationResult.Success;
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取指定槽位的装备信息
        /// Get equipped consumable info for a slot
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="slotId">槽位ID</param>
        /// <returns>槽位数据，如果不存在返回null</returns>
        public ConsumableSlotData? GetEquippedConsumable(CharacterData characterData, string slotId)
        {
            if (characterData?.EquippedConsumables == null)
                return null;

            return characterData.EquippedConsumables.GetSlot(slotId);
        }

        /// <summary>
        /// 获取指定类别可用于装备的消耗品列表
        /// Get available consumables for a specific category
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="category">类别 (potion/food)</param>
        /// <returns>可装备的消耗品列表（包含物品定义和库存数量）</returns>
        public List<(ItemDefinition Item, int Quantity)> GetAvailableConsumables(
            CharacterData characterData,
            string category)
        {
            var result = new List<(ItemDefinition, int)>();

            if (characterData == null)
                return result;

            // 获取所有消耗品类物品
            foreach (var itemDef in _configService.Items)
            {
                if (itemDef.Type != ItemType.Consumable || itemDef.ConsumableConfig == null)
                    continue;

                // 检查类别匹配
                if (!string.Equals(itemDef.ConsumableConfig.Category, category, StringComparison.OrdinalIgnoreCase))
                    continue;

                // 获取库存数量
                int quantity = characterData.Inventory.GetItemQuantity(itemDef.Id);
                
                // 只返回有库存或已装备的物品
                bool isEquipped = characterData.EquippedConsumables?.IsItemEquipped(itemDef.Id) ?? false;
                if (quantity > 0 || isEquipped)
                {
                    result.Add((itemDef, quantity));
                }
            }

            return result;
        }

        /// <summary>
        /// 检查槽位是否为空
        /// Check if a slot is empty
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="slotId">槽位ID</param>
        /// <returns>如果为空返回true</returns>
        public bool IsSlotEmpty(CharacterData characterData, string slotId)
        {
            var slot = GetEquippedConsumable(characterData, slotId);
            return slot == null || !slot.IsEquipped;
        }

        /// <summary>
        /// 获取装备物品的当前库存数量
        /// Get current inventory quantity of an equipped item
        /// </summary>
        /// <param name="characterData">角色数据</param>
        /// <param name="slotId">槽位ID</param>
        /// <returns>库存数量，如果槽位为空返回0</returns>
        public int GetEquippedItemQuantity(CharacterData characterData, string slotId)
        {
            var slot = GetEquippedConsumable(characterData, slotId);
            if (slot == null || string.IsNullOrEmpty(slot.ItemId))
                return 0;

            return characterData.Inventory.GetItemQuantity(slot.ItemId);
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 检查是否可以修改消耗品配置
        /// Check if consumable configuration can be modified
        /// </summary>
        /// <param name="isInBattle">是否在战斗中</param>
        /// <returns>如果可以修改返回true</returns>
        public static bool CanModifyConsumables(bool isInBattle)
        {
            return !isInBattle;
        }

        /// <summary>
        /// 检查槽位ID是否有效
        /// Check if slot ID is valid
        /// </summary>
        /// <param name="slotId">槽位ID</param>
        /// <returns>如果有效返回true</returns>
        public static bool IsValidSlotId(string slotId)
        {
            return AllSlotIds.Contains(slotId);
        }

        /// <summary>
        /// 检查槽位是否为药水槽位
        /// Check if slot is a potion slot
        /// </summary>
        /// <param name="slotId">槽位ID</param>
        /// <returns>如果是药水槽位返回true</returns>
        public static bool IsPotionSlot(string slotId)
        {
            return PotionSlotIds.Contains(slotId);
        }

        /// <summary>
        /// 检查槽位是否为食物槽位
        /// Check if slot is a food slot
        /// </summary>
        /// <param name="slotId">槽位ID</param>
        /// <returns>如果是食物槽位返回true</returns>
        public static bool IsFoodSlot(string slotId)
        {
            return FoodSlotIds.Contains(slotId);
        }

        /// <summary>
        /// 检查物品类别是否与槽位匹配
        /// Check if item category matches slot type
        /// </summary>
        /// <param name="category">物品类别 (potion/food)</param>
        /// <param name="slotId">槽位ID</param>
        /// <returns>如果匹配返回true</returns>
        public static bool IsCategoryMatchingSlot(string category, string slotId)
        {
            bool isPotion = string.Equals(category, "potion", StringComparison.OrdinalIgnoreCase);
            bool isFood = string.Equals(category, "food", StringComparison.OrdinalIgnoreCase);

            if (isPotion)
                return IsPotionSlot(slotId);
            if (isFood)
                return IsFoodSlot(slotId);

            return false;
        }

        /// <summary>
        /// 获取槽位的期望类别
        /// Get the expected category for a slot
        /// </summary>
        /// <param name="slotId">槽位ID</param>
        /// <returns>期望的类别，如果槽位无效返回null</returns>
        public static string? GetSlotCategory(string slotId)
        {
            if (IsPotionSlot(slotId))
                return "potion";
            if (IsFoodSlot(slotId))
                return "food";
            return null;
        }

        #endregion
    }
}
