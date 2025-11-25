using BlazorIdle.Shared.DTOs;
using BlazorIdle.Game.Purchase;
using BlazorIdle.Configuration;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace BlazorIdle.Services
{
    /// <summary>
    /// 用户服务接口 - 管理用户级别的数据（账号购买状态等）
    /// User service interface - manages user-level data (account purchase state, etc.)
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 获取当前用户的账号购买状态
        /// Get current user's account purchase state
        /// </summary>
        Task<AccountPurchaseState?> GetAccountPurchaseStateAsync();

        /// <summary>
        /// 更新当前用户的账号购买状态
        /// Update current user's account purchase state
        /// </summary>
        Task<bool> UpdateAccountPurchaseStateAsync(AccountPurchaseState state);

        /// <summary>
        /// 获取缓存的账号购买状态（如果已加载）
        /// Get cached account purchase state (if loaded)
        /// </summary>
        AccountPurchaseState? CachedAccountPurchaseState { get; }

        /// <summary>
        /// 账号购买状态变更事件
        /// Account purchase state changed event
        /// </summary>
        event Action<AccountPurchaseState?>? AccountPurchaseStateChanged;
    }

    /// <summary>
    /// 用户服务实现 - 通过服务器API管理用户级别的数据
    /// User service implementation - manages user-level data through server API
    /// </summary>
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly ApiConfiguration _apiConfig;
        private readonly ILogger<UserService> _logger;
        
        private AccountPurchaseState? _cachedAccountPurchaseState;

        public event Action<AccountPurchaseState?>? AccountPurchaseStateChanged;

        public AccountPurchaseState? CachedAccountPurchaseState => _cachedAccountPurchaseState;

        public UserService(
            HttpClient httpClient,
            IAuthService authService,
            ApiConfiguration apiConfig,
            ILogger<UserService> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _apiConfig = apiConfig;
            _logger = logger;
        }

        /// <summary>
        /// 配置HTTP客户端的认证头
        /// Configure HTTP client authentication header
        /// </summary>
        private async Task ConfigureAuthHeaderAsync()
        {
            var token = await _authService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        /// <summary>
        /// 获取当前用户的账号购买状态
        /// Get current user's account purchase state
        /// </summary>
        public async Task<AccountPurchaseState?> GetAccountPurchaseStateAsync()
        {
            try
            {
                await ConfigureAuthHeaderAsync();
                var response = await _httpClient.GetFromJsonAsync<AccountPurchaseStateResponse>(
                    $"{_apiConfig.UserApiUrl}/purchase-state");

                if (response?.Success == true && response.AccountPurchaseState != null)
                {
                    _cachedAccountPurchaseState = response.AccountPurchaseState;
                    AccountPurchaseStateChanged?.Invoke(_cachedAccountPurchaseState);
                    return _cachedAccountPurchaseState;
                }

                // 如果没有数据，返回空状态
                _cachedAccountPurchaseState = new AccountPurchaseState();
                return _cachedAccountPurchaseState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching account purchase state");
                // 返回空状态以保持功能正常
                _cachedAccountPurchaseState = new AccountPurchaseState();
                return _cachedAccountPurchaseState;
            }
        }

        /// <summary>
        /// 更新当前用户的账号购买状态
        /// Update current user's account purchase state
        /// </summary>
        public async Task<bool> UpdateAccountPurchaseStateAsync(AccountPurchaseState state)
        {
            try
            {
                await ConfigureAuthHeaderAsync();

                var request = new UpdateAccountPurchaseStateRequest
                {
                    AccountPurchaseState = state
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"{_apiConfig.UserApiUrl}/purchase-state",
                    request);

                if (response.IsSuccessStatusCode)
                {
                    _cachedAccountPurchaseState = state;
                    AccountPurchaseStateChanged?.Invoke(_cachedAccountPurchaseState);
                    _logger.LogInformation("Successfully updated account purchase state");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to update account purchase state: {StatusCode}", 
                        response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account purchase state");
                return false;
            }
        }
    }
}
