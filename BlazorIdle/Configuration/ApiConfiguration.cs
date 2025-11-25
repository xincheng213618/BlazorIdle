namespace BlazorIdle.Configuration
{
    /// <summary>
    /// API配置类 - 统一管理服务器API的基础URL
    /// API Configuration class - centrally manages the base URL for server APIs
    /// </summary>
    public class ApiConfiguration
    {
        /// <summary>
        /// API基础URL
        /// API base URL
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// 获取认证API的完整URL
        /// Get the full URL for the auth API
        /// </summary>
        public string AuthApiUrl => $"{BaseUrl.TrimEnd('/')}/api/auth";

        /// <summary>
        /// 获取角色API的完整URL
        /// Get the full URL for the character API
        /// </summary>
        public string CharacterApiUrl => $"{BaseUrl.TrimEnd('/')}/api/character";

        /// <summary>
        /// 获取游戏配置API的完整URL
        /// Get the full URL for the game-config API
        /// </summary>
        public string GameConfigApiUrl => $"{BaseUrl.TrimEnd('/')}/api/game-config";

        /// <summary>
        /// 获取用户API的完整URL (Step 4 Phase 2)
        /// Get the full URL for the user API
        /// </summary>
        public string UserApiUrl => $"{BaseUrl.TrimEnd('/')}/api/user";
    }
}
