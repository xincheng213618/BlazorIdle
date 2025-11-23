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
        public static List<ItemDefinition> LoadItems()
        {
            return LoadConfig<List<ItemDefinition>>("items.json") ?? new List<ItemDefinition>();
        }

        /// <summary>
        /// 加载消耗品商店配置
        /// Load consumable shop configuration
        /// </summary>
        public static List<ShopItemConfig> LoadConsumableShop()
        {
            return LoadConfig<List<ShopItemConfig>>("consumableShop.json") ?? new List<ShopItemConfig>();
        }

        /// <summary>
        /// 从嵌入资源加载配置文件
        /// Load configuration file from embedded resources
        /// </summary>
        private static T? LoadConfig<T>(string filename)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"BlazorIdle.Shared.Config.{filename}";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Console.WriteLine($"[ConfigRepository] Resource not found: {resourceName}");
                    return default;
                }

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConfigRepository] Error loading {filename}: {ex.Message}");
                return default;
            }
        }
    }
}
