using System.Text.Json.Serialization;

namespace BlazorIdle.Shared.Models
{
    /// <summary>
    /// 用户配置
    /// User configuration
    /// </summary>
    public class UserConfig
    {
        /// <summary>
        /// 新用户默认角色槽位数
        /// Default character slots for new users
        /// </summary>
        [JsonPropertyName("defaultCharacterSlots")]
        public int DefaultCharacterSlots { get; set; } = 1;

        /// <summary>
        /// 角色名称最小长度
        /// Character name minimum length
        /// </summary>
        [JsonPropertyName("characterNameMinLength")]
        public int CharacterNameMinLength { get; set; } = 2;

        /// <summary>
        /// 角色名称最大长度
        /// Character name maximum length
        /// </summary>
        [JsonPropertyName("characterNameMaxLength")]
        public int CharacterNameMaxLength { get; set; } = 20;
    }
}
