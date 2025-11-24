using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 角色库存 - 管理角色拥有的所有物品
    /// Character inventory - manages all items owned by a character
    /// </summary>
    public class Inventory
    {
        /// <summary>
        /// 库存唯一ID
        /// Unique inventory ID
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 所属角色ID
        /// Owner character ID
        /// </summary>
        [JsonPropertyName("characterId")]
        public string CharacterId { get; set; } = string.Empty;

        /// <summary>
        /// 物品列表
        /// List of items
        /// </summary>
        [JsonPropertyName("items")]
        public List<ItemInstance> Items { get; set; } = new();

        /// <summary>
        /// 最后更新时间
        /// Last updated time
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public event Action? Changed;  // 新增

        /// <summary>
        /// 添加物品到库存
        /// Add item to inventory
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="quantity">数量</param>
        public void AddItem(string itemId, int quantity)
        {
            if (quantity <= 0) return;

            // 查找是否已存在该物品
            var existingItem = Items.FirstOrDefault(i => i.ItemId == itemId);
            
            if (existingItem != null)
            {
                // 已存在，增加数量
                existingItem.Quantity += quantity;
            }
            else
            {
                // 不存在，创建新物品实例
                Items.Add(new ItemInstance
                {
                    ItemId = itemId,
                    Quantity = quantity,
                    AcquiredAt = DateTime.UtcNow
                });
            }

            LastUpdated = DateTime.UtcNow;
            Changed?.Invoke(); // 通知
        }

        /// <summary>
        /// 移除物品从库存
        /// Remove item from inventory
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="quantity">数量</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveItem(string itemId, int quantity)
        {
            if (quantity <= 0) return false;

            var existingItem = Items.FirstOrDefault(i => i.ItemId == itemId);
            
            if (existingItem == null || existingItem.Quantity < quantity)
            {
                return false; // 物品不存在或数量不足
            }

            existingItem.Quantity -= quantity;
            
            // 如果数量为0，从列表中移除
            if (existingItem.Quantity <= 0)
            {
                Items.Remove(existingItem);
            }

            LastUpdated = DateTime.UtcNow;
            Changed?.Invoke(); // 通知
            return true;
        }

        /// <summary>
        /// 获取指定物品的数量
        /// Get quantity of a specific item
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <returns>物品数量</returns>
        public int GetItemQuantity(string itemId)
        {
            var item = Items.FirstOrDefault(i => i.ItemId == itemId);
            return item?.Quantity ?? 0;
        }

        /// <summary>
        /// 原子性地消费物品（检查并移除）
        /// Atomically consume item (check and remove)
        /// </summary>
        /// <param name="itemId">物品ID</param>
        /// <param name="quantity">数量</param>
        /// <returns>是否成功消费</returns>
        public bool TryConsumeItem(string itemId, int quantity)
        {
            if (quantity <= 0) return false;

            var existingItem = Items.FirstOrDefault(i => i.ItemId == itemId);
            
            // 原子性检查：必须有足够的数量才能消费
            if (existingItem == null || existingItem.Quantity < quantity)
            {
                return false;
            }

            // 执行消费
            existingItem.Quantity -= quantity;
            
            // 如果数量为0，从列表中移除
            if (existingItem.Quantity <= 0)
            {
                Items.Remove(existingItem);
            }

            LastUpdated = DateTime.UtcNow;
            Changed?.Invoke();
            return true;
        }
    }
}
