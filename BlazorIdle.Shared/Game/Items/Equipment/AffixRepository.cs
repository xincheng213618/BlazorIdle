using System.Reflection;
using System.Text.Json;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 词条仓库服务 - 加载和管理词条定义与规则
    /// Affix repository service - loads and manages affix definitions and rules
    /// </summary>
    public sealed class AffixRepository
    {
        private static readonly Lazy<AffixRepository> _shared = new(() => new AffixRepository());
        
        /// <summary>
        /// 共享实例（单例）
        /// Shared instance (singleton)
        /// </summary>
        public static AffixRepository Shared => _shared.Value;

        private Dictionary<string, AffixDef>? _affixDefinitions;
        private AffixRules? _affixRules;
        private readonly object _lockObject = new();

        /// <summary>
        /// 获取所有词条定义
        /// Get all affix definitions
        /// </summary>
        public IReadOnlyDictionary<string, AffixDef> AffixDefinitions
        {
            get
            {
                EnsureLoaded();
                return _affixDefinitions ?? new Dictionary<string, AffixDef>();
            }
        }

        /// <summary>
        /// 获取词条规则
        /// Get affix rules
        /// </summary>
        public AffixRules Rules
        {
            get
            {
                EnsureLoaded();
                return _affixRules ?? AffixRules.CreateEmpty();
            }
        }

        /// <summary>
        /// 确保配置已加载
        /// Ensure configuration is loaded
        /// </summary>
        private void EnsureLoaded()
        {
            if (_affixDefinitions != null && _affixRules != null)
                return;

            lock (_lockObject)
            {
                if (_affixDefinitions != null && _affixRules != null)
                    return;

                LoadFromEmbeddedResources();
            }
        }

        /// <summary>
        /// 从嵌入资源加载配置
        /// Load configuration from embedded resources
        /// </summary>
        private void LoadFromEmbeddedResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Load definitions
            _affixDefinitions = LoadDefinitions(assembly);
            
            // Load rules
            _affixRules = LoadRules(assembly);
        }

        /// <summary>
        /// 加载词条定义
        /// Load affix definitions
        /// </summary>
        private Dictionary<string, AffixDef> LoadDefinitions(Assembly assembly)
        {
            var resourceName = "BlazorIdle.Shared.Config.affixes.definitions.json";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            
            if (stream == null)
            {
                // Return empty if resource not found (for testing scenarios)
                return new Dictionary<string, AffixDef>();
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var definitions = JsonSerializer.Deserialize<List<AffixDef>>(json, options) 
                ?? new List<AffixDef>();

            return definitions.ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 加载词条规则
        /// Load affix rules
        /// </summary>
        private AffixRules LoadRules(Assembly assembly)
        {
            var resourceName = "BlazorIdle.Shared.Config.affixes.rules.json";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            
            if (stream == null)
            {
                // Return empty if resource not found
                return AffixRules.CreateEmpty();
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            return JsonSerializer.Deserialize<AffixRules>(json, options) ?? AffixRules.CreateEmpty();
        }

        /// <summary>
        /// 通过ID获取词条定义
        /// Get affix definition by ID
        /// </summary>
        public AffixDef? GetAffixById(string affixId)
        {
            EnsureLoaded();
            var definitions = _affixDefinitions ?? new Dictionary<string, AffixDef>();
            return definitions.TryGetValue(affixId, out var def) ? def : null;
        }

        /// <summary>
        /// 获取所有词条定义列表
        /// Get all affix definitions as list
        /// </summary>
        public IReadOnlyList<AffixDef> GetAllAffixes()
        {
            EnsureLoaded();
            var definitions = _affixDefinitions ?? new Dictionary<string, AffixDef>();
            return definitions.Values.ToList();
        }

        /// <summary>
        /// 按标签获取词条定义
        /// Get affix definitions by tag
        /// </summary>
        public IReadOnlyList<AffixDef> GetAffixesByTag(string tag)
        {
            EnsureLoaded();
            var definitions = _affixDefinitions ?? new Dictionary<string, AffixDef>();
            return definitions.Values
                .Where(d => d.HasTag(tag))
                .ToList();
        }

        /// <summary>
        /// 按作用域获取词条定义
        /// Get affix definitions by scope
        /// </summary>
        public IReadOnlyList<AffixDef> GetAffixesByScope(AffixScope scope)
        {
            EnsureLoaded();
            var definitions = _affixDefinitions ?? new Dictionary<string, AffixDef>();
            return definitions.Values
                .Where(d => d.Scope == scope)
                .ToList();
        }

        /// <summary>
        /// 创建词条实例（带定义引用）
        /// Create affix instance with definition reference
        /// </summary>
        public AffixInstance CreateInstance(string affixId, int level = 1)
        {
            var definition = GetAffixById(affixId);
            return AffixInstance.Create(affixId, level, definition);
        }

        /// <summary>
        /// 填充词条实例的定义引用
        /// Fill affix instance definition reference
        /// </summary>
        public void FillDefinition(AffixInstance instance)
        {
            if (instance.Definition == null)
            {
                instance.Definition = GetAffixById(instance.AffixId);
            }
        }

        /// <summary>
        /// 检查两个词条是否互斥
        /// Check if two affixes are mutually exclusive
        /// </summary>
        public bool AreMutuallyExclusive(string affixId1, string affixId2)
        {
            return Rules.AreMutuallyExclusive(affixId1, affixId2);
        }

        /// <summary>
        /// 获取词条的装备数量限制
        /// Get equipment limit for an affix
        /// </summary>
        public int? GetEquipLimit(string affixId)
        {
            // First check if defined in rules
            var rulesLimit = Rules.GetEquipLimit(affixId);
            if (rulesLimit.HasValue)
                return rulesLimit;

            // Then check if defined in affix definition
            var def = GetAffixById(affixId);
            return def?.EquipLimit;
        }

        /// <summary>
        /// 重新加载配置（用于测试或热更新）
        /// Reload configuration (for testing or hot reload)
        /// </summary>
        public void Reload()
        {
            lock (_lockObject)
            {
                _affixDefinitions = null;
                _affixRules = null;
            }
        }

        /// <summary>
        /// 使用外部配置初始化（用于测试）
        /// Initialize with external configuration (for testing)
        /// </summary>
        public void InitializeForTesting(List<AffixDef> definitions, AffixRules rules)
        {
            lock (_lockObject)
            {
                _affixDefinitions = definitions.ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);
                _affixRules = rules;
            }
        }
    }
}
