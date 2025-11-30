using System.Text.Json;
using System.Reflection;
using System.Collections.Concurrent;

namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 战斗配置仓库 - 加载战斗相关配置文件
    /// Combat configuration repository - loads combat related config files
    /// </summary>
    public static class CombatConfigRepository
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// 配置缓存
        /// Configuration cache
        /// </summary>
        private static readonly ConcurrentDictionary<string, object> _cache = new();

        /// <summary>
        /// 清除缓存（用于测试）
        /// Clear cache (for testing)
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }

        #region Combat Caps Config

        /// <summary>
        /// 加载战斗属性上限配置（带缓存）
        /// Load combat caps configuration (with caching)
        /// </summary>
        public static CombatCapsConfig LoadCombatCaps()
        {
            return GetOrLoad("combat.caps", () =>
            {
                var result = LoadConfigInternal<CombatCapsConfig>("combat.caps.json");
                if (result != null)
                {
                    Console.WriteLine("[CombatConfigRepository] 已加载战斗属性上限配置: combat.caps.json");
                    return result;
                }
                Console.WriteLine("[CombatConfigRepository] 战斗属性上限配置未找到，使用默认配置");
                return CombatCapsConfig.CreateDefault();
            });
        }

        #endregion

        #region Crit Config

        /// <summary>
        /// 加载暴击系统配置（带缓存）
        /// Load critical hit configuration (with caching)
        /// </summary>
        public static CritConfig LoadCritConfig()
        {
            return GetOrLoad("combat.crit", () =>
            {
                var result = LoadConfigInternal<CritConfig>("combat.crit.json");
                if (result != null)
                {
                    Console.WriteLine($"[CombatConfigRepository] 已加载暴击配置: combat.crit.json (基础倍率: {result.BaseMultiplier})");
                    return result;
                }
                Console.WriteLine("[CombatConfigRepository] 暴击配置未找到，使用默认配置");
                return CritConfig.CreateDefault();
            });
        }

        #endregion

        #region Element Types

        /// <summary>
        /// 加载元素类型定义（带缓存）
        /// Load element type definitions (with caching)
        /// </summary>
        public static List<ElementTypeDef> LoadElementTypes()
        {
            return GetOrLoad("elements.types", () =>
            {
                var result = LoadConfigInternal<List<ElementTypeDef>>("elements.types.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[CombatConfigRepository] 已加载元素类型: elements.types.json ({result.Count} 个元素)");
                    return result;
                }
                Console.WriteLine("[CombatConfigRepository] 元素类型配置未找到，使用默认配置");
                return CreateDefaultElementTypes();
            });
        }

        private static List<ElementTypeDef> CreateDefaultElementTypes()
        {
            return new List<ElementTypeDef>
            {
                new() { Id = "fire", Name = "火", Icon = "🔥" },
                new() { Id = "water", Name = "水", Icon = "💧" },
                new() { Id = "wind", Name = "风", Icon = "🌪️" },
                new() { Id = "earth", Name = "地", Icon = "🪨" },
                new() { Id = "light", Name = "光", Icon = "☀️" },
                new() { Id = "dark", Name = "暗", Icon = "🌙" },
                new() { Id = "neutral", Name = "无", Icon = "⚪" }
            };
        }

        #endregion

        #region Element Matrix

        /// <summary>
        /// 加载元素克制矩阵配置（带缓存）
        /// Load element matrix configuration (with caching)
        /// </summary>
        public static ElementMatrixConfig LoadElementMatrixConfig()
        {
            return GetOrLoad("elements.matrix", () =>
            {
                var result = LoadConfigInternal<ElementMatrixConfig>("elements.matrix.json");
                if (result != null)
                {
                    Console.WriteLine($"[CombatConfigRepository] 已加载元素矩阵: elements.matrix.json ({result.Relations.Count} 个关系)");
                    return result;
                }
                Console.WriteLine("[CombatConfigRepository] 元素矩阵配置未找到，使用默认配置");
                return CreateDefaultElementMatrixConfig();
            });
        }

        /// <summary>
        /// 加载并创建元素矩阵服务（带缓存）
        /// Load and create element matrix service (with caching)
        /// </summary>
        public static ElementMatrix LoadElementMatrix()
        {
            return GetOrLoad("elements.matrix.service", () =>
            {
                var config = LoadElementMatrixConfig();
                return new ElementMatrix(config);
            });
        }

        private static ElementMatrixConfig CreateDefaultElementMatrixConfig()
        {
            return new ElementMatrixConfig
            {
                AdvantageMultiplier = 1.5,
                DisadvantageMultiplier = 0.75,
                NeutralMultiplier = 1.0,
                Relations = new List<ElementRelation>
                {
                    new() { Attacker = "fire", Defender = "wind", Result = "advantage" },
                    new() { Attacker = "wind", Defender = "earth", Result = "advantage" },
                    new() { Attacker = "earth", Defender = "water", Result = "advantage" },
                    new() { Attacker = "water", Defender = "fire", Result = "advantage" },
                    new() { Attacker = "light", Defender = "dark", Result = "advantage" },
                    new() { Attacker = "dark", Defender = "light", Result = "advantage" }
                }
            };
        }

        #endregion

        #region Cache Helpers

        private static T GetOrLoad<T>(string cacheKey, Func<T> loader) where T : class
        {
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return (T)cached;
            }

            var result = loader();
            _cache[cacheKey] = result;
            return result;
        }

        #endregion

        #region Internal Loading

        private static T? LoadConfigInternal<T>(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"BlazorIdle.Shared.Config.{filename}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Console.Error.WriteLine($"[CombatConfigRepository] 配置文件未找到: {resourceName}");
                return default;
            }

            try
            {
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.Error.WriteLine($"[CombatConfigRepository] 配置文件为空: {filename}");
                    return default;
                }

                var result = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return result;
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[CombatConfigRepository] JSON 解析错误 {filename}: {ex.Message}");
                return default;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CombatConfigRepository] 加载配置错误 {filename}: {ex.Message}");
                return default;
            }
        }

        #endregion
    }
}
