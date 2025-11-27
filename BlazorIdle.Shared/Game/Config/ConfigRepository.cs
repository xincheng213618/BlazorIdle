using System.Text.Json;
using System.Reflection;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Config
{
    /// <summary>
    /// 配置文件仓库 - 直接从 Shared 项目的嵌入资源中读取配置
    /// Configuration repository - reads configs directly from Shared project embedded resources
    /// </summary>
    public static class ConfigRepository
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// 物品配置文件列表 - 按类型分类存储在 items 文件夹中
        /// Item config files - stored in items folder by type
        /// </summary>
        private static readonly string[] ItemConfigFiles = new[]
        {
            "items.currency.json",    // 货币
            "items.potions.json",     // 药水
            "items.food.json",        // 食物
            "items.special.json"      // 特殊物品（技能书、卷轴等）
        };

        /// <summary>
        /// 加载物品配置 - 从多个分类文件中合并加载
        /// Load items configuration - merge from multiple category files
        /// </summary>
        /// <returns>物品列表，加载失败时抛出异常</returns>
        /// <exception cref="InvalidOperationException">配置加载失败</exception>
        public static List<ItemDefinition> LoadItems()
        {
            var allItems = new List<ItemDefinition>();
            var loadedFiles = new List<string>();
            var failedFiles = new List<string>();

            foreach (var filename in ItemConfigFiles)
            {
                try
                {
                    var items = TryLoadConfig<List<ItemDefinition>>(filename);
                    if (items != null && items.Count > 0)
                    {
                        allItems.AddRange(items);
                        loadedFiles.Add(filename);
                        Console.WriteLine($"[ConfigRepository] 已加载物品分类: {filename} ({items.Count} 个物品)");
                    }
                }
                catch (Exception ex)
                {
                    failedFiles.Add($"{filename}: {ex.Message}");
                    Console.Error.WriteLine($"[ConfigRepository] 加载物品分类失败: {filename} - {ex.Message}");
                }
            }

            // 如果新的分类文件加载失败，尝试加载旧的 items.json 作为后备
            if (allItems.Count == 0)
            {
                Console.WriteLine("[ConfigRepository] 尝试从旧的 items.json 加载...");
                var legacyItems = TryLoadConfig<List<ItemDefinition>>("items.json");
                if (legacyItems != null && legacyItems.Count > 0)
                {
                    allItems.AddRange(legacyItems);
                    Console.WriteLine($"[ConfigRepository] 从 items.json 加载了 {legacyItems.Count} 个物品");
                }
            }

            if (allItems.Count == 0)
            {
                throw new InvalidOperationException(
                    $"物品配置加载失败或为空。已尝试加载: {string.Join(", ", ItemConfigFiles)}。" +
                    $"失败详情: {string.Join("; ", failedFiles)}");
            }

            Console.WriteLine($"[ConfigRepository] 成功加载物品配置，共 {allItems.Count} 个物品，来自 {loadedFiles.Count} 个文件");
            return allItems;
        }

        /// <summary>
        /// 商店配置文件列表 - 按类型分类存储在 shops 文件夹中
        /// Shop config files - stored in shops folder by type
        /// 注意：这些常量目前用于文档目的，方便了解配置文件结构
        /// Note: These constants are currently for documentation purposes
        /// </summary>
        internal static readonly string[] ShopConfigFiles = new[]
        {
            "shops.potion.json",      // 药水商店
            "shops.food.json",        // 食物商店
            "shops.consumable.json"   // 消耗品商店（技能书等）
        };

        /// <summary>
        /// 技能配置文件列表 - 按职业/类型分类存储在 skills 文件夹中
        /// Skill config files - stored in skills folder by profession/type
        /// 注意：这些常量目前用于文档目的，方便了解配置文件结构
        /// Note: These constants are currently for documentation purposes
        /// </summary>
        internal static readonly string[] SkillConfigFiles = new[]
        {
            "skills.warrior.json",    // 战士技能
            "skills.mage.json",       // 法师技能
            "skills.rogue.json",      // 盗贼技能
            "skills.ranger.json",     // 游侠技能
            "skills.common.json",     // 通用技能
            "skills.consumable.json", // 消耗品技能
            "skills.monster.json"     // 怪物技能
        };

        /// <summary>
        /// 加载消耗品商店配置 - 从分类文件加载，保留旧版兼容
        /// Load consumable shop configuration - load from categorized files with legacy fallback
        /// </summary>
        /// <returns>商店配置列表，加载失败时抛出异常</returns>
        /// <exception cref="InvalidOperationException">配置加载失败</exception>
        public static List<ShopItemConfig> LoadConsumableShop()
        {
            // 尝试从新的分类文件加载
            var result = TryLoadConfig<List<ShopItemConfig>>("shops.consumable.json");
            if (result != null && result.Count > 0)
            {
                Console.WriteLine($"[ConfigRepository] 已加载消耗品商店: shops.consumable.json ({result.Count} 个物品)");
                return result;
            }

            // 尝试旧版文件
            result = TryLoadConfig<List<ShopItemConfig>>("consumableShop.json");
            if (result != null && result.Count > 0)
            {
                Console.WriteLine($"[ConfigRepository] 从 consumableShop.json 加载了 {result.Count} 个物品");
                return result;
            }

            throw new InvalidOperationException("消耗品商店配置加载失败或为空。");
        }

        /// <summary>
        /// 加载药水商店配置 - 从分类文件加载，保留旧版兼容
        /// Load potion shop configuration - load from categorized files with legacy fallback
        /// </summary>
        /// <returns>商店配置列表，加载失败时抛出异常</returns>
        /// <exception cref="InvalidOperationException">配置加载失败</exception>
        public static List<ShopItemConfig> LoadPotionShop()
        {
            // 尝试从新的分类文件加载
            var result = TryLoadConfig<List<ShopItemConfig>>("shops.potion.json");
            if (result != null && result.Count > 0)
            {
                Console.WriteLine($"[ConfigRepository] 已加载药水商店: shops.potion.json ({result.Count} 个物品)");
                return result;
            }

            // 尝试旧版文件
            result = TryLoadConfig<List<ShopItemConfig>>("potionShop.json");
            if (result != null && result.Count > 0)
            {
                Console.WriteLine($"[ConfigRepository] 从 potionShop.json 加载了 {result.Count} 个物品");
                return result;
            }

            throw new InvalidOperationException("药水商店配置加载失败或为空。");
        }

        /// <summary>
        /// 加载食品商店配置 - 从分类文件加载，保留旧版兼容
        /// Load food shop configuration - load from categorized files with legacy fallback
        /// </summary>
        /// <returns>商店配置列表，加载失败时抛出异常</returns>
        /// <exception cref="InvalidOperationException">配置加载失败</exception>
        public static List<ShopItemConfig> LoadFoodShop()
        {
            // 尝试从新的分类文件加载
            var result = TryLoadConfig<List<ShopItemConfig>>("shops.food.json");
            if (result != null && result.Count > 0)
            {
                Console.WriteLine($"[ConfigRepository] 已加载食品商店: shops.food.json ({result.Count} 个物品)");
                return result;
            }

            // 尝试旧版文件
            result = TryLoadConfig<List<ShopItemConfig>>("foodShop.json");
            if (result != null && result.Count > 0)
            {
                Console.WriteLine($"[ConfigRepository] 从 foodShop.json 加载了 {result.Count} 个物品");
                return result;
            }

            throw new InvalidOperationException("食品商店配置加载失败或为空。");
        }

        /// <summary>
        /// 加载系统商店配置 - 从分类文件加载，保留旧版兼容
        /// Load system shop configuration - load from categorized files with legacy fallback
        /// </summary>
        /// <returns>系统商店配置，加载失败时返回默认配置</returns>
        public static SystemShopConfig LoadSystemShop()
        {
            // 尝试从新的分类文件加载
            var result = TryLoadConfig<SystemShopConfig>("shops.system.json");
            if (result != null)
            {
                Console.WriteLine("[ConfigRepository] 已加载系统商店: shops.system.json");
                return result;
            }

            // 尝试旧版文件
            result = TryLoadConfig<SystemShopConfig>("systemShop.json");
            if (result == null)
            {
                // 返回默认配置
                return new SystemShopConfig
                {
                    Items = new List<SystemShopItemConfig>
                    {
                        new SystemShopItemConfig
                        {
                            ItemId = "character_slot",
                            DisplayName = "角色槽位 +1",
                            Description = "增加一个可创建角色的槽位",
                            Price = 5000,
                            CurrencyType = "Gold",
                            Limits = new BlazorIdle.Game.Purchase.PurchaseLimit { PerAccount = 1 }
                        }
                    }
                };
            }
            return result;
        }

        /// <summary>
        /// 加载用户配置
        /// Load user configuration
        /// </summary>
        /// <returns>用户配置，加载失败时返回默认配置</returns>
        public static UserConfig LoadUserConfig()
        {
            var result = TryLoadConfig<UserConfig>("userConfig.json");
            if (result == null)
            {
                // 返回默认配置
                return new UserConfig
                {
                    DefaultCharacterSlots = 1,
                    CharacterNameMinLength = 2,
                    CharacterNameMaxLength = 20
                };
            }
            return result;
        }

        /// <summary>
        /// 安全加载配置（不抛出异常）
        /// Load configuration safely (no exception)
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="filename">文件名</param>
        /// <returns>配置对象或 null</returns>
        public static T? TryLoadConfig<T>(string filename) where T : class
        {
            try
            {
                return LoadConfig<T>(filename);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从嵌入资源加载配置文件
        /// Load configuration file from embedded resources
        /// </summary>
        /// <exception cref="FileNotFoundException">资源文件未找到</exception>
        /// <exception cref="JsonException">JSON 反序列化失败</exception>
        private static T? LoadConfig<T>(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"BlazorIdle.Shared.Config.{filename}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                var errorMsg = $"配置文件未找到: {resourceName}。请确保文件已作为嵌入资源添加到项目中。";
                Console.Error.WriteLine($"[ConfigRepository] {errorMsg}");
                throw new FileNotFoundException(errorMsg, filename);
            }

            try
            {
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidDataException($"配置文件 {filename} 为空");
                }

                var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
                if (result == null)
                {
                    throw new JsonException($"配置文件 {filename} 反序列化结果为 null");
                }

                Console.WriteLine($"[ConfigRepository] 成功加载配置: {filename}");
                return result;
            }
            catch (JsonException ex)
            {
                var errorMsg = $"配置文件 {filename} JSON 格式错误: {ex.Message}";
                Console.Error.WriteLine($"[ConfigRepository] {errorMsg}");
                throw new JsonException(errorMsg, ex);
            }
            catch (Exception ex)
            {
                var errorMsg = $"加载配置文件 {filename} 时发生错误: {ex.Message}";
                Console.Error.WriteLine($"[ConfigRepository] {errorMsg}");
                throw new InvalidOperationException(errorMsg, ex);
            }
        }
    }
}
