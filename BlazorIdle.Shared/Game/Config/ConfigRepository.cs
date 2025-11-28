using System.Text.Json;
using System.Reflection;
using System.Collections.Concurrent;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Game.Config
{
    /// <summary>
    /// 配置文件仓库 - 直接从 Shared 项目的嵌入资源中读取配置
    /// Configuration repository - reads configs directly from Shared project embedded resources
    /// 实现内存缓存机制，首次加载后数据缓存在内存中，后续调用直接从缓存获取
    /// Implements memory caching - data is cached after first load, subsequent calls retrieve from cache
    /// </summary>
    public static class ConfigRepository
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        #region 内存缓存 / Memory Cache

        /// <summary>
        /// 通用配置缓存 - 按文件名缓存已加载的配置对象
        /// Generic config cache - caches loaded config objects by filename
        /// </summary>
        private static readonly ConcurrentDictionary<string, object> _configCache = new();

        /// <summary>
        /// 合并配置缓存 - 缓存需要合并多个文件的配置（如物品、技能等）
        /// Merged config cache - caches configs that merge multiple files (items, skills, etc.)
        /// </summary>
        private static readonly ConcurrentDictionary<string, object> _mergedConfigCache = new();

        /// <summary>
        /// 缓存锁 - 确保并发安全
        /// Cache lock - ensures thread safety
        /// </summary>
        private static readonly object _cacheLock = new();

        /// <summary>
        /// 缓存统计 - 用于调试和性能分析
        /// Cache statistics - for debugging and performance analysis
        /// </summary>
        private static int _cacheHits;
        private static int _cacheMisses;

        /// <summary>
        /// 获取缓存命中次数
        /// Get cache hit count
        /// </summary>
        public static int CacheHits => _cacheHits;

        /// <summary>
        /// 获取缓存未命中次数
        /// Get cache miss count
        /// </summary>
        public static int CacheMisses => _cacheMisses;

        /// <summary>
        /// 清除所有缓存 - 主要用于测试或热重载
        /// Clear all caches - mainly for testing or hot reload
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _configCache.Clear();
                _mergedConfigCache.Clear();
                _cacheHits = 0;
                _cacheMisses = 0;
                Console.WriteLine("[ConfigRepository] 已清除所有配置缓存");
            }
        }

        /// <summary>
        /// 从缓存获取或加载配置
        /// Get config from cache or load if not cached
        /// </summary>
        private static T GetOrLoadConfig<T>(string cacheKey, Func<T> loader) where T : class
        {
            if (_configCache.TryGetValue(cacheKey, out var cached))
            {
                Interlocked.Increment(ref _cacheHits);
                return (T)cached;
            }

            lock (_cacheLock)
            {
                // Double-check after acquiring lock
                if (_configCache.TryGetValue(cacheKey, out cached))
                {
                    Interlocked.Increment(ref _cacheHits);
                    return (T)cached;
                }

                Interlocked.Increment(ref _cacheMisses);
                var result = loader();
                _configCache[cacheKey] = result;
                Console.WriteLine($"[ConfigRepository] 配置已缓存: {cacheKey}");
                return result;
            }
        }

        /// <summary>
        /// 从合并缓存获取或加载配置
        /// Get merged config from cache or load if not cached
        /// </summary>
        private static T GetOrLoadMergedConfig<T>(string cacheKey, Func<T> loader) where T : class
        {
            if (_mergedConfigCache.TryGetValue(cacheKey, out var cached))
            {
                Interlocked.Increment(ref _cacheHits);
                return (T)cached;
            }

            lock (_cacheLock)
            {
                // Double-check after acquiring lock
                if (_mergedConfigCache.TryGetValue(cacheKey, out cached))
                {
                    Interlocked.Increment(ref _cacheHits);
                    return (T)cached;
                }

                Interlocked.Increment(ref _cacheMisses);
                var result = loader();
                _mergedConfigCache[cacheKey] = result;
                Console.WriteLine($"[ConfigRepository] 合并配置已缓存: {cacheKey}");
                return result;
            }
        }

        #endregion

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
        /// 加载物品配置 - 从多个分类文件中合并加载（带缓存）
        /// Load items configuration - merge from multiple category files (with caching)
        /// </summary>
        /// <returns>物品列表，加载失败时抛出异常</returns>
        /// <exception cref="InvalidOperationException">配置加载失败</exception>
        public static List<ItemDefinition> LoadItems()
        {
            return GetOrLoadMergedConfig("items", LoadItemsInternal);
        }

        /// <summary>
        /// 内部加载物品配置（无缓存）
        /// Internal load items (no cache)
        /// </summary>
        private static List<ItemDefinition> LoadItemsInternal()
        {
            var allItems = new List<ItemDefinition>();
            var loadedFiles = new List<string>();
            var failedFiles = new List<string>();

            foreach (var filename in ItemConfigFiles)
            {
                try
                {
                    var items = LoadConfigInternal<List<ItemDefinition>>(filename);
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
        /// 加载消耗品商店配置 - 从分类文件加载（带缓存）
        /// Load consumable shop configuration - load from categorized files (with caching)
        /// </summary>
        /// <returns>商店配置列表，加载失败时抛出异常</returns>
        /// <exception cref="InvalidOperationException">配置加载失败</exception>
        public static List<ShopItemConfig> LoadConsumableShop()
        {
            return GetOrLoadConfig("shops.consumable", () =>
            {
                var result = LoadConfigInternal<List<ShopItemConfig>>("shops.consumable.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载消耗品商店: shops.consumable.json ({result.Count} 个物品)");
                    return result;
                }
                throw new InvalidOperationException("消耗品商店配置加载失败或为空。请检查 Config/shops/consumable.json 是否正确配置。");
            });
        }

        /// <summary>
        /// 加载药水商店配置 - 从分类文件加载（带缓存）
        /// Load potion shop configuration - load from categorized files (with caching)
        /// </summary>
        /// <returns>商店配置列表，加载失败时抛出异常</returns>
        /// <exception cref="InvalidOperationException">配置加载失败</exception>
        public static List<ShopItemConfig> LoadPotionShop()
        {
            return GetOrLoadConfig("shops.potion", () =>
            {
                var result = LoadConfigInternal<List<ShopItemConfig>>("shops.potion.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载药水商店: shops.potion.json ({result.Count} 个物品)");
                    return result;
                }
                throw new InvalidOperationException("药水商店配置加载失败或为空。请检查 Config/shops/potion.json 是否正确配置。");
            });
        }

        /// <summary>
        /// 加载食品商店配置 - 从分类文件加载（带缓存）
        /// Load food shop configuration - load from categorized files (with caching)
        /// </summary>
        /// <returns>商店配置列表，加载失败时抛出异常</returns>
        /// <exception cref="InvalidOperationException">配置加载失败</exception>
        public static List<ShopItemConfig> LoadFoodShop()
        {
            return GetOrLoadConfig("shops.food", () =>
            {
                var result = LoadConfigInternal<List<ShopItemConfig>>("shops.food.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载食品商店: shops.food.json ({result.Count} 个物品)");
                    return result;
                }
                throw new InvalidOperationException("食品商店配置加载失败或为空。请检查 Config/shops/food.json 是否正确配置。");
            });
        }

        /// <summary>
        /// 加载系统商店配置 - 从分类文件加载（带缓存）
        /// Load system shop configuration - load from categorized files (with caching)
        /// </summary>
        /// <returns>系统商店配置，加载失败时返回默认配置</returns>
        public static SystemShopConfig LoadSystemShop()
        {
            return GetOrLoadConfig("shops.system", () =>
            {
                var result = LoadConfigInternal<SystemShopConfig>("shops.system.json");
                if (result != null)
                {
                    Console.WriteLine("[ConfigRepository] 已加载系统商店: shops.system.json");
                    return result;
                }

                // 返回默认配置
                Console.WriteLine("[ConfigRepository] 系统商店配置未找到，使用默认配置");
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
            });
        }

        /// <summary>
        /// 加载用户配置（带缓存）
        /// Load user configuration (with caching)
        /// </summary>
        /// <returns>用户配置，加载失败时返回默认配置</returns>
        public static UserConfig LoadUserConfig()
        {
            return GetOrLoadConfig("userConfig", () =>
            {
                var result = LoadConfigInternal<UserConfig>("userConfig.json");
                if (result == null)
                {
                    // 返回默认配置
                    Console.WriteLine("[ConfigRepository] 用户配置未找到，使用默认配置");
                    return new UserConfig
                    {
                        DefaultCharacterSlots = 1,
                        CharacterNameMinLength = 2,
                        CharacterNameMaxLength = 20
                    };
                }
                return result;
            });
        }

        #region 战斗配置加载 / Battle Config Loading

        /// <summary>
        /// 加载战斗配置列表（带缓存）
        /// Load battle configurations list (with caching)
        /// </summary>
        /// <returns>战斗配置列表，加载失败时返回空列表</returns>
        public static List<BattleConfigDef> LoadBattleConfigs()
        {
            return GetOrLoadConfig("battles.battleConfigs", () =>
            {
                var result = LoadConfigInternal<List<BattleConfigDef>>("battles.battleConfigs.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载战斗配置: battles.battleConfigs.json ({result.Count} 个配置)");
                    return result;
                }
                Console.WriteLine("[ConfigRepository] 战斗配置未找到或为空，使用空列表");
                return new List<BattleConfigDef>();
            });
        }

        /// <summary>
        /// 加载战斗场景列表（带缓存）
        /// Load battle scenarios list (with caching)
        /// </summary>
        /// <returns>战斗场景列表，加载失败时返回空列表</returns>
        public static List<BattleScenarioDef> LoadBattleScenarios()
        {
            return GetOrLoadConfig("battles.battleScenarios", () =>
            {
                var result = LoadConfigInternal<List<BattleScenarioDef>>("battles.battleScenarios.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载战斗场景: battles.battleScenarios.json ({result.Count} 个场景)");
                    return result;
                }
                Console.WriteLine("[ConfigRepository] 战斗场景未找到或为空，使用空列表");
                return new List<BattleScenarioDef>();
            });
        }

        #endregion

        #region 怪物配置加载 / Monster Config Loading

        /// <summary>
        /// 加载怪物配置列表（带缓存）
        /// Load monster definitions list (with caching)
        /// </summary>
        /// <returns>怪物列表，加载失败时返回默认列表</returns>
        public static List<MonsterDef> LoadMonsters()
        {
            return GetOrLoadConfig("monsters.monsters", () =>
            {
                var result = LoadConfigInternal<List<MonsterDef>>("monsters.monsters.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载怪物配置: monsters.monsters.json ({result.Count} 个怪物)");
                    return result;
                }
                Console.WriteLine("[ConfigRepository] 怪物配置未找到或为空，使用默认怪物配置");
                return DefaultGameConfig.DefaultMonsters();
            });
        }

        #endregion

        #region 副本配置加载 / Dungeon Config Loading

        /// <summary>
        /// 加载副本配置列表（带缓存）
        /// Load dungeon definitions list (with caching)
        /// </summary>
        /// <returns>副本列表，加载失败时返回空列表</returns>
        public static List<DungeonDef> LoadDungeons()
        {
            return GetOrLoadConfig("dungeons.dungeons", () =>
            {
                var result = LoadConfigInternal<List<DungeonDef>>("dungeons.dungeons.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载副本配置: dungeons.dungeons.json ({result.Count} 个副本)");
                    return result;
                }
                Console.WriteLine("[ConfigRepository] 副本配置未找到或为空，使用空列表");
                return new List<DungeonDef>();
            });
        }

        #endregion

        #region 职业配置加载 / Profession Config Loading

        /// <summary>
        /// 加载职业定义列表（带缓存）
        /// Load profession definitions list (with caching)
        /// </summary>
        /// <returns>职业列表，加载失败时返回默认列表</returns>
        public static List<ProfessionDef> LoadProfessions()
        {
            return GetOrLoadConfig("professions.professions", () =>
            {
                var result = LoadConfigInternal<List<ProfessionDef>>("professions.professions.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载职业配置: professions.professions.json ({result.Count} 个职业)");
                    return result;
                }
                Console.WriteLine("[ConfigRepository] 职业配置未找到或为空，使用默认职业配置");
                return DefaultGameConfig.DefaultProfessions();
            });
        }

        /// <summary>
        /// 加载职业属性配置（带缓存）
        /// Load profession attribute configurations (with caching)
        /// </summary>
        /// <returns>职业属性配置字典，加载失败时返回空字典</returns>
        public static Dictionary<string, ProfessionAttributeConfig> LoadProfessionAttributes()
        {
            return GetOrLoadConfig("professions.attributes", () =>
            {
                var result = LoadConfigInternal<Dictionary<string, ProfessionAttributeConfig>>("professions.attributes.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载职业属性配置: professions.attributes.json ({result.Count} 个职业)");
                    return result;
                }
                Console.WriteLine("[ConfigRepository] 职业属性配置未找到或为空，使用空字典");
                return new Dictionary<string, ProfessionAttributeConfig>();
            });
        }

        /// <summary>
        /// 加载经验曲线配置（带缓存）
        /// Load experience curve configuration (with caching)
        /// </summary>
        /// <returns>经验曲线列表，加载失败时返回默认曲线</returns>
        public static List<LevelExperienceRequirement> LoadExperienceCurve()
        {
            return GetOrLoadConfig("professions.experience", () =>
            {
                var result = LoadConfigInternal<List<LevelExperienceRequirement>>("professions.experience.json");
                if (result != null && result.Count > 0)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载经验曲线: professions.experience.json ({result.Count} 个等级)");
                    return result;
                }
                Console.WriteLine("[ConfigRepository] 经验曲线未找到或为空，使用默认经验曲线");
                return CreateDefaultExperienceCurve();
            });
        }

        /// <summary>
        /// 加载职业限制配置（最大等级等）（带缓存）
        /// Load profession limits configuration (max level etc.) (with caching)
        /// </summary>
        /// <returns>经验配置，加载失败时返回默认配置</returns>
        public static ExperienceConfig LoadProfessionLimits()
        {
            return GetOrLoadConfig("professions.limits", () =>
            {
                var result = LoadConfigInternal<ExperienceConfig>("professions.limits.json");
                if (result != null)
                {
                    Console.WriteLine($"[ConfigRepository] 已加载职业限制配置: professions.limits.json (最大等级: {result.MaxProfessionLevel})");
                    return result;
                }
                Console.WriteLine("[ConfigRepository] 职业限制配置未找到，使用默认配置 (最大等级: 100)");
                return new ExperienceConfig { MaxProfessionLevel = 100 };
            });
        }

        /// <summary>
        /// 创建默认经验曲线
        /// Create default experience curve
        /// </summary>
        private static List<LevelExperienceRequirement> CreateDefaultExperienceCurve()
        {
            var curve = new List<LevelExperienceRequirement>();
            for (int level = 1; level <= 20; level++)
            {
                long expRequired = level == 1 ? 0 : (long)(100 * Math.Pow(1.5, level - 2));
                curve.Add(new LevelExperienceRequirement { Level = level, ExperienceRequired = expRequired });
            }
            return curve;
        }

        #endregion

        /// <summary>
        /// 安全加载配置（不抛出异常）- 保留用于向后兼容
        /// Load configuration safely (no exception) - kept for backward compatibility
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        /// <param name="filename">文件名</param>
        /// <returns>配置对象或 null</returns>
        public static T? TryLoadConfig<T>(string filename) where T : class
        {
            try
            {
                return LoadConfigInternal<T>(filename);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 内部加载配置文件（无缓存）- 直接从嵌入资源读取
        /// Internal load config (no cache) - reads directly from embedded resources
        /// </summary>
        /// <exception cref="FileNotFoundException">资源文件未找到</exception>
        /// <exception cref="JsonException">JSON 反序列化失败</exception>
        private static T? LoadConfigInternal<T>(string filename)
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
