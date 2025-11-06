using BlazorIdle.Shared.Models;
using BlazorIdle.Configuration;
using System.Net.Http.Json;

namespace BlazorIdle.Services
{
    /// <summary>
    /// 职业属性配置服务 - 负责加载和缓存职业属性配置
    /// </summary>
    public interface IProfessionAttributeService
    {
        Task<Dictionary<string, ProfessionAttributeConfig>> GetAllConfigsAsync();
        Task<ProfessionAttributeConfig?> GetConfigAsync(string professionId);
        Task ReloadConfigsAsync();
    }

    public class ProfessionAttributeService : IProfessionAttributeService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProfessionAttributeService> _logger;
        private readonly ApiConfiguration _apiConfig;
        private Dictionary<string, ProfessionAttributeConfig>? _cachedConfigs;

        public ProfessionAttributeService(
            HttpClient httpClient,
            ILogger<ProfessionAttributeService> logger,
            ApiConfiguration apiConfig)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiConfig = apiConfig;
        }

        /// <summary>
        /// 获取所有职业属性配置
        /// </summary>
        public async Task<Dictionary<string, ProfessionAttributeConfig>> GetAllConfigsAsync()
        {
            if (_cachedConfigs != null)
            {
                return _cachedConfigs;
            }

            return await ReloadConfigsInternalAsync();
        }

        /// <summary>
        /// 获取指定职业的属性配置
        /// </summary>
        public async Task<ProfessionAttributeConfig?> GetConfigAsync(string professionId)
        {
            var configs = await GetAllConfigsAsync();
            return configs.TryGetValue(professionId, out var config) ? config : null;
        }

        /// <summary>
        /// 重新加载配置（清除缓存）
        /// </summary>
        public async Task ReloadConfigsAsync()
        {
            _cachedConfigs = null;
            await ReloadConfigsInternalAsync();
        }

        private async Task<Dictionary<string, ProfessionAttributeConfig>> ReloadConfigsInternalAsync()
        {
            try
            {
                _logger.LogInformation("Loading profession attribute configs from server");
                
                var url = $"{_apiConfig.GameConfigApiUrl}/profession-attributes";
                _logger.LogDebug("Fetching profession attributes from {Url}", url);
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var configs = await response.Content.ReadFromJsonAsync<Dictionary<string, ProfessionAttributeConfig>>();
                
                if (configs == null || configs.Count == 0)
                {
                    _logger.LogWarning("Received empty profession attribute configs");
                    configs = new Dictionary<string, ProfessionAttributeConfig>();
                }
                else
                {
                    _logger.LogInformation("Loaded {Count} profession attribute configs", configs.Count);
                }
                
                _cachedConfigs = configs;
                return configs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load profession attribute configs");
                
                // 返回空配置而不是抛出异常，保证应用不会崩溃
                _cachedConfigs = new Dictionary<string, ProfessionAttributeConfig>();
                return _cachedConfigs;
            }
        }
    }
}
