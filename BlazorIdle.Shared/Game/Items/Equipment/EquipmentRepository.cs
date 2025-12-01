using System.Reflection;
using System.Text.Json;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 装备仓库 - 加载和管理装备模板、品质、层级配置
    /// Equipment repository - loads and manages equipment templates, qualities, and tiers
    /// </summary>
    public sealed class EquipmentRepository
    {
        private static EquipmentRepository? _shared;
        private static readonly object _lock = new();

        private readonly Dictionary<string, EquipmentTemplate> _templates = new();
        private readonly Dictionary<string, QualityDef> _qualities = new();
        private readonly Dictionary<int, TierDef> _tiers = new();
        private SlotConfig? _slotConfig;
        private bool _initialized = false;

        /// <summary>
        /// 获取共享实例
        /// Get shared instance
        /// </summary>
        public static EquipmentRepository Shared
        {
            get
            {
                if (_shared == null)
                {
                    lock (_lock)
                    {
                        _shared ??= new EquipmentRepository();
                        _shared.Initialize();
                    }
                }
                return _shared;
            }
        }

        /// <summary>
        /// JSON 选项（支持注释）
        /// JSON options (supports comments)
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// 初始化仓库
        /// Initialize repository
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            LoadQualities();
            LoadTiers();
            LoadSlotConfig();
            LoadTemplates();

            _initialized = true;
        }

        /// <summary>
        /// 加载品质配置
        /// Load quality configurations
        /// </summary>
        private void LoadQualities()
        {
            var json = LoadEmbeddedResource("BlazorIdle.Shared.Config.equipment.quality.json");
            if (string.IsNullOrEmpty(json)) return;

            var qualities = JsonSerializer.Deserialize<List<QualityDef>>(json, JsonOptions);
            if (qualities == null) return;

            foreach (var q in qualities)
            {
                _qualities[q.Id] = q;
            }
        }

        /// <summary>
        /// 加载层级配置
        /// Load tier configurations
        /// </summary>
        private void LoadTiers()
        {
            var json = LoadEmbeddedResource("BlazorIdle.Shared.Config.equipment.tiers.json");
            if (string.IsNullOrEmpty(json)) return;

            var tierConfig = JsonSerializer.Deserialize<TierConfig>(json, JsonOptions);
            if (tierConfig?.Tiers == null) return;

            foreach (var t in tierConfig.Tiers)
            {
                _tiers[t.Tier] = t;
            }
        }

        /// <summary>
        /// 加载槽位配置
        /// Load slot configuration
        /// </summary>
        private void LoadSlotConfig()
        {
            var json = LoadEmbeddedResource("BlazorIdle.Shared.Config.equipment.slots.json");
            if (string.IsNullOrEmpty(json)) return;

            _slotConfig = JsonSerializer.Deserialize<SlotConfig>(json, JsonOptions);
        }

        /// <summary>
        /// 加载装备模板
        /// Load equipment templates
        /// </summary>
        private void LoadTemplates()
        {
            var elements = new[] { "fire", "water", "wind", "earth", "light", "dark", "neutral" };
            var weaponTypes = WeaponTypes.All;

            foreach (var element in elements)
            {
                foreach (var weaponType in weaponTypes)
                {
                    LoadTemplatesForCategory(element, weaponType);
                }
            }
        }

        /// <summary>
        /// 加载指定分类的模板
        /// Load templates for specific category
        /// </summary>
        private void LoadTemplatesForCategory(string element, string weaponType)
        {
            var resourceName = $"BlazorIdle.Shared.Config.equipment.weapons.{element}.{weaponType}.json";
            var json = LoadEmbeddedResource(resourceName);
            if (string.IsNullOrEmpty(json)) return;

            var templates = JsonSerializer.Deserialize<List<EquipmentTemplate>>(json, JsonOptions);
            if (templates == null) return;

            foreach (var t in templates)
            {
                _templates[t.Id] = t;
            }
        }

        /// <summary>
        /// 加载嵌入资源
        /// Load embedded resource
        /// </summary>
        private static string? LoadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return null;

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        #region Public API

        /// <summary>
        /// 获取装备模板
        /// Get equipment template by ID
        /// </summary>
        public EquipmentTemplate? GetTemplate(string templateId)
        {
            return _templates.TryGetValue(templateId, out var template) ? template : null;
        }

        /// <summary>
        /// 获取所有装备模板
        /// Get all equipment templates
        /// </summary>
        public IEnumerable<EquipmentTemplate> GetAllTemplates() => _templates.Values;

        /// <summary>
        /// 按元素获取模板
        /// Get templates by element
        /// </summary>
        public IEnumerable<EquipmentTemplate> GetTemplatesByElement(string element)
        {
            return _templates.Values.Where(t => 
                string.Equals(t.Element, element, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 按武器类型获取模板
        /// Get templates by weapon type
        /// </summary>
        public IEnumerable<EquipmentTemplate> GetTemplatesByWeaponType(string weaponType)
        {
            return _templates.Values.Where(t => 
                string.Equals(t.WeaponType, weaponType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 按层级获取模板
        /// Get templates by tier
        /// </summary>
        public IEnumerable<EquipmentTemplate> GetTemplatesByTier(int tier)
        {
            return _templates.Values.Where(t => t.Tier == tier);
        }

        /// <summary>
        /// 获取品质定义
        /// Get quality definition
        /// </summary>
        public QualityDef? GetQuality(string qualityId)
        {
            return _qualities.TryGetValue(qualityId, out var quality) ? quality : null;
        }

        /// <summary>
        /// 获取所有品质定义
        /// Get all quality definitions
        /// </summary>
        public IEnumerable<QualityDef> GetAllQualities() => _qualities.Values;

        /// <summary>
        /// 获取层级定义
        /// Get tier definition
        /// </summary>
        public TierDef? GetTier(int tier)
        {
            return _tiers.TryGetValue(tier, out var tierDef) ? tierDef : null;
        }

        /// <summary>
        /// 获取所有层级定义
        /// Get all tier definitions
        /// </summary>
        public IEnumerable<TierDef> GetAllTiers() => _tiers.Values;

        /// <summary>
        /// 获取槽位配置
        /// Get slot configuration
        /// </summary>
        public SlotConfig? GetSlotConfig() => _slotConfig;

        /// <summary>
        /// 创建装备实例
        /// Create equipment instance from template
        /// </summary>
        /// <param name="templateId">模板ID / Template ID</param>
        /// <param name="quality">品质 / Quality</param>
        /// <param name="affixRepository">词条仓库 / Affix repository</param>
        /// <returns>装备实例 / Equipment instance</returns>
        public EquipmentItem? CreateEquipment(string templateId, string quality, AffixRepository? affixRepository = null)
        {
            var template = GetTemplate(templateId);
            if (template == null) return null;

            var qualityDef = GetQuality(quality);
            var tierDef = GetTier(template.Tier);
            var affixCount = qualityDef?.AffixCount ?? 0;

            var item = EquipmentItem.Create(templateId, quality, template, qualityDef, tierDef);

            // 根据品质生成词条
            if (affixRepository != null && template.AffixSequence.Count > 0)
            {
                for (int i = 0; i < Math.Min(affixCount, template.AffixSequence.Count); i++)
                {
                    var affixId = template.AffixSequence[i];
                    var affix = affixRepository.CreateInstance(affixId, 1);
                    if (affix != null)
                    {
                        item.Affixes.Add(affix);
                    }
                }
            }

            return item;
        }

        #endregion
    }
}
