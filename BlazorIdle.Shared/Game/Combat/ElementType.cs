using System.Text.Json.Serialization;

namespace BlazorIdle.Game.Combat
{
    /// <summary>
    /// 元素类型定义 - 从 Config/elements/types.json 加载
    /// Element type definition - loaded from Config/elements/types.json
    /// </summary>
    public sealed class ElementTypeDef
    {
        /// <summary>
        /// 元素ID（唯一标识符）
        /// Element ID (unique identifier)
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "neutral";

        /// <summary>
        /// 元素显示名称
        /// Element display name
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "无";

        /// <summary>
        /// 元素图标（emoji）
        /// Element icon (emoji)
        /// </summary>
        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "⚪";
    }

    /// <summary>
    /// 元素常量定义
    /// Element constants
    /// </summary>
    public static class ElementIds
    {
        public const string Fire = "fire";
        public const string Water = "water";
        public const string Wind = "wind";
        public const string Earth = "earth";
        public const string Light = "light";
        public const string Dark = "dark";
        public const string Neutral = "neutral";
    }
}
