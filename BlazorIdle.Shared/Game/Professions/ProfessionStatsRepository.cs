using System.Reflection;
using System.Text.Json;

namespace BlazorIdle.Game.Professions
{
    /// <summary>
    /// 职业属性仓库 - 加载和管理职业基础属性配置
    /// Profession stats repository - loads and manages profession base stats configuration
    /// </summary>
    public sealed class ProfessionStatsRepository
    {
        private static readonly Lazy<ProfessionStatsRepository> _instance = 
            new Lazy<ProfessionStatsRepository>(() => new ProfessionStatsRepository());

        /// <summary>
        /// 共享单例实例
        /// Shared singleton instance
        /// </summary>
        public static ProfessionStatsRepository Shared => _instance.Value;

        private readonly Dictionary<string, ProfessionConfig> _professions;
        private readonly bool _isLoaded;

        /// <summary>
        /// 私有构造函数 - 从嵌入资源加载配置
        /// Private constructor - loads configuration from embedded resource
        /// </summary>
        private ProfessionStatsRepository()
        {
            _professions = new Dictionary<string, ProfessionConfig>(StringComparer.OrdinalIgnoreCase);
            _isLoaded = LoadFromEmbeddedResource();
        }

        /// <summary>
        /// 是否已加载
        /// Whether the repository is loaded
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// 职业数量
        /// Number of professions
        /// </summary>
        public int Count => _professions.Count;

        /// <summary>
        /// 从嵌入资源加载职业配置
        /// Load profession configuration from embedded resource
        /// </summary>
        private bool LoadFromEmbeddedResource()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "BlazorIdle.Shared.Config.professions.base_stats.json";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Console.WriteLine($"[ProfessionStatsRepository] Resource not found: {resourceName}");
                    return false;
                }

                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                // JSON 文件包含注释，需要跳过
                // JSON file contains comments, need to skip them
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var professionDict = JsonSerializer.Deserialize<Dictionary<string, ProfessionConfig>>(json, options);
                if (professionDict != null)
                {
                    foreach (var kvp in professionDict)
                    {
                        _professions[kvp.Key] = kvp.Value;
                    }
                }

                Console.WriteLine($"[ProfessionStatsRepository] Loaded {_professions.Count} professions");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProfessionStatsRepository] Load failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取职业配置
        /// Get profession configuration
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <returns>职业配置，如果不存在则返回null / Profession config, or null if not found</returns>
        public ProfessionConfig? GetProfession(string professionId)
        {
            if (string.IsNullOrEmpty(professionId))
                return null;

            _professions.TryGetValue(professionId, out var config);
            return config;
        }

        /// <summary>
        /// 获取所有职业配置
        /// Get all profession configurations
        /// </summary>
        /// <returns>所有职业配置的只读列表 / Read-only list of all profession configs</returns>
        public IReadOnlyList<ProfessionConfig> GetAllProfessions()
        {
            return _professions.Values.ToList();
        }

        /// <summary>
        /// 获取所有职业ID
        /// Get all profession IDs
        /// </summary>
        /// <returns>所有职业ID的只读列表 / Read-only list of all profession IDs</returns>
        public IReadOnlyList<string> GetAllProfessionIds()
        {
            return _professions.Keys.ToList();
        }

        /// <summary>
        /// 获取职业基础面板属性
        /// Get profession base panel stats
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <returns>基础属性，如果不存在则返回默认值 / Base stats, or default if not found</returns>
        public ProfessionBaseStats GetBaseStats(string professionId)
        {
            var config = GetProfession(professionId);
            return config?.BaseStats ?? ProfessionBaseStats.CreateDefault();
        }

        /// <summary>
        /// 获取职业初始战斗属性
        /// Get profession initial combat stats
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <returns>初始战斗属性，如果不存在则返回默认值 / Initial combat stats, or default if not found</returns>
        public InitialCombatStats GetInitialCombatStats(string professionId)
        {
            var config = GetProfession(professionId);
            return config?.InitialCombatStats ?? InitialCombatStats.CreateDefault();
        }

        /// <summary>
        /// 获取职业资源配置
        /// Get profession resource configuration
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <returns>资源配置，如果不存在则返回null / Resource config, or null if not found</returns>
        public ProfessionResource? GetResource(string professionId)
        {
            var config = GetProfession(professionId);
            return config?.Resource;
        }

        /// <summary>
        /// 获取职业默认技能配置
        /// Get profession default skills configuration
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <returns>默认技能配置，如果不存在则返回null / Default skills config, or null if not found</returns>
        public ProfessionDefaultSkills? GetDefaultSkills(string professionId)
        {
            var config = GetProfession(professionId);
            return config?.DefaultSkills;
        }

        /// <summary>
        /// 检查职业是否存在
        /// Check if profession exists
        /// </summary>
        /// <param name="professionId">职业ID / Profession ID</param>
        /// <returns>是否存在 / Whether exists</returns>
        public bool HasProfession(string professionId)
        {
            return !string.IsNullOrEmpty(professionId) && _professions.ContainsKey(professionId);
        }

        /// <summary>
        /// 清除缓存（用于测试）
        /// Clear cache (for testing)
        /// </summary>
        public static void ClearCache()
        {
            // 由于使用 Lazy<T>，无法直接清除缓存
            // 这里提供一个警告，说明在生产环境中不应该调用此方法
            // Since Lazy<T> is used, cache cannot be cleared directly
            // This provides a warning that this method should not be called in production
            Console.WriteLine("[ProfessionStatsRepository] Warning: ClearCache called, but Lazy<T> singleton cannot be reset");
        }
    }
}
