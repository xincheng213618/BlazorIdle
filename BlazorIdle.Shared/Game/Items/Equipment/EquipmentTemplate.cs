using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Items.Equipment
{
    /// <summary>
    /// 装备模板模型 - 从 Config/equipment/templates.json 加载
    /// Equipment template model - loaded from Config/equipment/templates.json
    /// </summary>
    public sealed class EquipmentTemplate
    {
        /// <summary>
        /// 模板唯一标识符
        /// Template unique identifier
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 装备显示名称
        /// Equipment display name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 武器类型 (sword/dagger/spear/axe/staff/gun/melee/bow/harp/katana)
        /// Weapon type (GBF-style)
        /// </summary>
        [JsonPropertyName("weaponType")]
        public string WeaponType { get; set; } = "sword";

        /// <summary>
        /// 装备元素 (fire/water/wind/earth/light/dark/neutral)
        /// Equipment element
        /// </summary>
        [JsonPropertyName("element")]
        public string Element { get; set; } = "neutral";

        /// <summary>
        /// 基础品质
        /// Base quality
        /// </summary>
        [JsonPropertyName("baseQuality")]
        public string BaseQuality { get; set; } = "white";

        /// <summary>
        /// 可用词条池
        /// Available affix pool
        /// </summary>
        [JsonPropertyName("affixPool")]
        public List<string> AffixPool { get; set; } = new();

        /// <summary>
        /// 装备层级（1-5，影响强化材料）
        /// Equipment tier (1-5, affects reinforcement materials)
        /// </summary>
        [JsonPropertyName("tier")]
        public int Tier { get; set; } = 1;

        /// <summary>
        /// 基础攻击力
        /// Base attack value
        /// </summary>
        [JsonPropertyName("baseAttack")]
        public double BaseAttack { get; set; } = 100;

        /// <summary>
        /// 基础生命值（可选）
        /// Base HP value (optional)
        /// </summary>
        [JsonPropertyName("baseHp")]
        public double BaseHp { get; set; } = 0;
    }

    /// <summary>
    /// 武器类型常量 - 仿照GBF的武器类型设计
    /// Weapon type constants - GBF-style weapon types
    /// </summary>
    public static class WeaponTypes
    {
        public const string Sword = "sword";     // 剑
        public const string Dagger = "dagger";   // 匕首
        public const string Spear = "spear";     // 枪
        public const string Axe = "axe";         // 斧
        public const string Staff = "staff";     // 杖
        public const string Gun = "gun";         // 铳
        public const string Melee = "melee";     // 拳套
        public const string Bow = "bow";         // 弓
        public const string Harp = "harp";       // 乐器
        public const string Katana = "katana";   // 刀

        /// <summary>
        /// 所有武器类型列表
        /// All weapon types list
        /// </summary>
        public static readonly IReadOnlyList<string> All = new[]
        {
            Sword, Dagger, Spear, Axe, Staff, Gun, Melee, Bow, Harp, Katana
        };

        /// <summary>
        /// 获取武器类型的中文名称
        /// Get Chinese name for weapon type
        /// </summary>
        public static string GetDisplayName(string weaponType) => weaponType.ToLowerInvariant() switch
        {
            "sword" => "剑",
            "dagger" => "匕首",
            "spear" => "枪",
            "axe" => "斧",
            "staff" => "杖",
            "gun" => "铳",
            "melee" => "拳套",
            "bow" => "弓",
            "harp" => "乐器",
            "katana" => "刀",
            _ => weaponType
        };

        /// <summary>
        /// 检查是否为有效的武器类型
        /// Check if weapon type is valid
        /// </summary>
        public static bool IsValid(string weaponType) =>
            All.Contains(weaponType, StringComparer.OrdinalIgnoreCase);
    }
}
