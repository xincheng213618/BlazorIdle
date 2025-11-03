using BlazorIdle.Shared.Models;
using BlazorIdle.Game.Config;
using BlazorIdle.Shared.DTOs;
using BlazorIdle.Configuration;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace BlazorIdle.Services
{
    /// <summary>
    /// 角色管理服务 - 通过服务器API进行角色数据的持久化管理
    /// Character management service - manages character data persistence through server API
    /// </summary>
    public interface ICharacterService
    {
        Task<CharacterListResponse?> GetCharactersAsync();
        Task<CharacterData?> GetCharacterAsync(string id);
        Task<CharacterResponse?> CreateCharacterAsync(string name, ProfessionDef profession);
        Task<CharacterResponse?> DeleteCharacterAsync(string id);
        Task<CharacterData?> GetSelectedCharacterAsync();
        Task SetSelectedCharacterAsync(string? characterId);
        Task<CharacterResponse?> UpdateCharacterAsync(string characterId, CharacterData character);

        // 新增：选中角色变更事件
        event Action<CharacterData?>? SelectedCharacterChanged;
    }

    public class CharacterService : ICharacterService
    {
        private const string SelectedCharacterKey = "blazoridle_selected_character";
        
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private readonly ApiConfiguration _apiConfig;
        private CharacterListResponse? _cachedCharacters;

        // 事件实现
        public event Action<CharacterData?>? SelectedCharacterChanged;

        public CharacterService(
            HttpClient httpClient, 
            IAuthService authService,
            Blazored.LocalStorage.ILocalStorageService localStorage,
            ApiConfiguration apiConfig)
        {
            _httpClient = httpClient;
            _authService = authService;
            _localStorage = localStorage;
            _apiConfig = apiConfig;
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
        /// 获取当前用户的所有角色
        /// Get all characters for the current user
        /// </summary>
        public async Task<CharacterListResponse?> GetCharactersAsync()
        {
            try
            {
                await ConfigureAuthHeaderAsync();
                var response = await _httpClient.GetFromJsonAsync<CharacterListResponse>(_apiConfig.CharacterApiUrl);
                _cachedCharacters = response;
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching characters: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据ID获取指定角色
        /// Get a specific character by ID
        /// </summary>
        public async Task<CharacterData?> GetCharacterAsync(string id)
        {
            try
            {
                await ConfigureAuthHeaderAsync();
                var response = await _httpClient.GetFromJsonAsync<CharacterResponse>($"{_apiConfig.CharacterApiUrl}/{id}");
                return response?.Character;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching character {id}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建新角色
        /// Create a new character
        /// </summary>
        public async Task<CharacterResponse?> CreateCharacterAsync(string name, ProfessionDef profession)
        {
            try
            {
                await ConfigureAuthHeaderAsync();
                
                var request = new CreateCharacterRequest
                {
                    Name = name,
                    ProfessionId = profession.Id
                };
                
                var response = await _httpClient.PostAsJsonAsync(_apiConfig.CharacterApiUrl, request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CharacterResponse>();
                    _cachedCharacters = null; // 清除缓存
                    return result;
                }
                else
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<CharacterResponse>();
                    return errorResult ?? new CharacterResponse
                    {
                        Success = false,
                        Message = "创建角色失败"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating character: {ex.Message}");
                return new CharacterResponse
                {
                    Success = false,
                    Message = $"创建角色失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 删除角色
        /// Delete a character
        /// </summary>
        public async Task<CharacterResponse?> DeleteCharacterAsync(string id)
        {
            try
            {
                await ConfigureAuthHeaderAsync();
                
                var response = await _httpClient.DeleteAsync($"{_apiConfig.CharacterApiUrl}/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CharacterResponse>();
                    _cachedCharacters = null; // 清除缓存
                                        
                    // 如果删除的是当前选中的角色，清除选中状态并通知
                    var selectedId = await _localStorage.GetItemAsync<string>(SelectedCharacterKey);
                    if (selectedId == id)
                    {
                        await _localStorage.RemoveItemAsync(SelectedCharacterKey);
                        SelectedCharacterChanged?.Invoke(null);
                    }
                    
                    return result;
                }
                else
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<CharacterResponse>();
                    return errorResult ?? new CharacterResponse
                    {
                        Success = false,
                        Message = "删除角色失败"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting character: {ex.Message}");
                return new CharacterResponse
                {
                    Success = false,
                    Message = $"删除角色失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取当前选中的角色
        /// Get the currently selected character
        /// </summary>
        public async Task<CharacterData?> GetSelectedCharacterAsync()
        {
            try
            {
                var selectedId = await _localStorage.GetItemAsync<string>(SelectedCharacterKey);
                if (string.IsNullOrWhiteSpace(selectedId))
                {
                    return null;
                }

                return await GetCharacterAsync(selectedId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting selected character: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 设置当前选中的角色
        /// Set the currently selected character
        /// </summary>
        public async Task SetSelectedCharacterAsync(string? characterId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(characterId))
                {
                    await _localStorage.RemoveItemAsync(SelectedCharacterKey);
                    SelectedCharacterChanged?.Invoke(null);
                }
                else
                {
                    await _localStorage.SetItemAsync(SelectedCharacterKey, characterId);
                    // 触发事件时提供完整的角色信息，方便UI显示名称
                    var character = await GetCharacterAsync(characterId);
                    SelectedCharacterChanged?.Invoke(character);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting selected character: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新角色数据 - 用于心跳保存和手动更新
        /// Update character data - for heartbeat save and manual updates
        /// </summary>
        /// <param name="characterId">角色ID</param>
        /// <param name="character">要更新的角色数据</param>
        /// <returns>更新结果</returns>
        public async Task<CharacterResponse?> UpdateCharacterAsync(string characterId, CharacterData character)
        {
            try
            {
                await ConfigureAuthHeaderAsync();

                // 构建更新请求，包含所有可更新的字段
                // Build update request with all updatable fields
                var request = new UpdateCharacterRequest
                {
                    // 角色属性
                    // Character stats
                    MaxHp = character.MaxHp,
                    AttackRateAPS = character.AttackRateAPS,
                    DamagePerAttack = character.DamagePerAttack,
                    HastePercent = character.HastePercent,
                    SpecialIntervalSec = character.SpecialIntervalSec,
                    SpecialDamage = character.SpecialDamage,
                    CritChancePercent = character.CritChancePercent,
                    CritMultiplier = character.CritMultiplier,
                    VariancePct = character.VariancePct,
                    ReviveSec = character.ReviveSec,
                    // 库存数据
                    // Inventory data
                    Inventory = character.Inventory,
                    // 职业数据
                    // Profession data
                    Professions = character.Professions,
                    ActiveCombatProfessionId = character.ActiveCombatProfessionId
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"{_apiConfig.CharacterApiUrl}/{characterId}", 
                    request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CharacterResponse>();
                    return result;
                }
                else
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<CharacterResponse>();
                    Console.WriteLine($"Error updating character: {errorResult?.Message ?? "Unknown error"}");
                    return errorResult ?? new CharacterResponse
                    {
                        Success = false,
                        Message = "更新角色失败"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating character {characterId}: {ex.Message}");
                return new CharacterResponse
                {
                    Success = false,
                    Message = $"更新角色失败: {ex.Message}"
                };
            }
        }
    }
}
