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
        /// 加载物品配置
        /// Load items configuration
        /// </summary>
        /// <returns>物品列表，加载失败时抛出异常</returns>
        /// <exception cref="InvalidOperationException">配置加载失败</exception>
        public static List<ItemDefinition> LoadItems()
        {
            var result = LoadConfig<List<ItemDefinition>>("items.json");
            if (result == null || result.Count == 0)
            {
                throw new InvalidOperationException("物品配置加载失败或为空。请检查 items.json 文件是否正确配置。");
            }
            return result;
        }

        /// <summary>
        /// 加载消耗品商店配置
        /// Load consumable shop configuration
        /// </summary>
        /// <returns>商店配置列表，加载失败时抛出异常</returns>
        /// <exception cref="InvalidOperationException">配置加载失败</exception>
        public static List<ShopItemConfig> LoadConsumableShop()
        {
            var result = LoadConfig<List<ShopItemConfig>>("consumableShop.json");
            if (result == null || result.Count == 0)
            {
                throw new InvalidOperationException("消耗品商店配置加载失败或为空。请检查 consumableShop.json 文件是否正确配置。");
            }
            return result;
        }

        /// <summary>
        /// 加载系统商店配置
        /// Load system shop configuration
        /// </summary>
        /// <returns>系统商店配置，加载失败时返回默认配置</returns>
        public static SystemShopConfig LoadSystemShop()
        {
            var result = TryLoadConfig<SystemShopConfig>("systemShop.json");
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
