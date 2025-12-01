using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 元素克制关系定义
    /// Element advantage relation definition
    /// </summary>
    public sealed class ElementRelation
    {
        /// <summary>
        /// 攻击方元素ID
        /// Attacker element ID
        /// </summary>
        [JsonPropertyName("attacker")]
        public string Attacker { get; set; } = "";

        /// <summary>
        /// 防守方元素ID
        /// Defender element ID
        /// </summary>
        [JsonPropertyName("defender")]
        public string Defender { get; set; } = "";

        /// <summary>
        /// 克制结果（advantage = 克制）
        /// Advantage result (advantage = attacker has advantage)
        /// </summary>
        [JsonPropertyName("result")]
        public string Result { get; set; } = "neutral";
    }

    /// <summary>
    /// 元素克制矩阵配置 - 从 Config/elements/matrix.json 加载
    /// Element advantage matrix configuration - loaded from Config/elements/matrix.json
    /// </summary>
    public sealed class ElementMatrixConfig
    {
        /// <summary>
        /// 克制时的伤害倍率
        /// Damage multiplier when attacker has advantage
        /// </summary>
        [JsonPropertyName("advantageMultiplier")]
        public double AdvantageMultiplier { get; set; } = 1.5;

        /// <summary>
        /// 被克制时的伤害倍率
        /// Damage multiplier when attacker has disadvantage
        /// </summary>
        [JsonPropertyName("disadvantageMultiplier")]
        public double DisadvantageMultiplier { get; set; } = 0.75;

        /// <summary>
        /// 无克制关系时的伤害倍率
        /// Damage multiplier when neutral
        /// </summary>
        [JsonPropertyName("neutralMultiplier")]
        public double NeutralMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 元素克制关系列表
        /// Element advantage relations list
        /// </summary>
        [JsonPropertyName("relations")]
        public List<ElementRelation> Relations { get; set; } = new();
    }

    /// <summary>
    /// 元素克制矩阵服务 - 提供元素克制计算
    /// Element advantage matrix service - provides element advantage calculations
    /// </summary>
    public sealed class ElementMatrix
    {
        private readonly ElementMatrixConfig _config;
        private readonly Dictionary<(string attacker, string defender), string> _relationLookup;

        /// <summary>
        /// 创建元素矩阵服务
        /// Create element matrix service
        /// </summary>
        public ElementMatrix(ElementMatrixConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            // 构建查找表
            // Build lookup table
            _relationLookup = new Dictionary<(string, string), string>();
            foreach (var relation in _config.Relations)
            {
                _relationLookup[(relation.Attacker, relation.Defender)] = relation.Result;
            }
        }

        /// <summary>
        /// 获取元素克制关系
        /// Get element advantage relation
        /// </summary>
        /// <param name="attacker">攻击方元素ID / Attacker element ID</param>
        /// <param name="defender">防守方元素ID / Defender element ID</param>
        /// <returns>克制关系（advantage, disadvantage, neutral）</returns>
        public string GetRelation(string attacker, string defender)
        {
            // 无元素或相同元素 = 中立
            // No element or same element = neutral
            if (string.IsNullOrEmpty(attacker) || string.IsNullOrEmpty(defender))
                return "neutral";
            if (attacker == defender)
                return "neutral";
            if (attacker == ElementIds.Neutral || defender == ElementIds.Neutral)
                return "neutral";

            // 查找攻击方对防守方的克制关系
            // Look up attacker's advantage over defender
            if (_relationLookup.TryGetValue((attacker, defender), out var relation))
            {
                return relation;
            }

            // 查找防守方对攻击方的克制关系（反向）
            // Look up defender's advantage over attacker (reverse)
            if (_relationLookup.TryGetValue((defender, attacker), out var reverseRelation))
            {
                // 如果防守方克制攻击方，则攻击方处于劣势
                // If defender has advantage, attacker has disadvantage
                if (reverseRelation == "advantage")
                    return "disadvantage";
            }

            return "neutral";
        }

        /// <summary>
        /// 获取元素克制伤害倍率
        /// Get element advantage damage multiplier
        /// </summary>
        /// <param name="attacker">攻击方元素ID / Attacker element ID</param>
        /// <param name="defender">防守方元素ID / Defender element ID</param>
        /// <returns>伤害倍率 / Damage multiplier</returns>
        public double GetMultiplier(string attacker, string defender)
        {
            var relation = GetRelation(attacker, defender);
            return relation switch
            {
                "advantage" => _config.AdvantageMultiplier,
                "disadvantage" => _config.DisadvantageMultiplier,
                _ => _config.NeutralMultiplier
            };
        }

        /// <summary>
        /// 检查是否为克制关系
        /// Check if attacker has advantage
        /// </summary>
        public bool HasAdvantage(string attacker, string defender)
        {
            return GetRelation(attacker, defender) == "advantage";
        }

        /// <summary>
        /// 检查是否为被克制关系
        /// Check if attacker has disadvantage
        /// </summary>
        public bool HasDisadvantage(string attacker, string defender)
        {
            return GetRelation(attacker, defender) == "disadvantage";
        }

        /// <summary>
        /// 获取配置
        /// Get configuration
        /// </summary>
        public ElementMatrixConfig Config => _config;

        /// <summary>
        /// 创建默认元素矩阵
        /// Create default element matrix
        /// </summary>
        public static ElementMatrix CreateDefault()
        {
            var config = new ElementMatrixConfig
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
            return new ElementMatrix(config);
        }
    }
}
