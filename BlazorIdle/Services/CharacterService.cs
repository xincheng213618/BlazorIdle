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
        Task<CharacterResponse?> SwitchProfessionAsync(string characterId, string professionId);

        // Task 3.5: 强制重算和批量重算
        Task ForceRecalculateAttributesAsync(CharacterData character, bool saveImmediately = false);
        Task RecalculateAllCharactersAsync(List<CharacterData> characters);

        // Task 4.2: 立即保存
        Task<bool> SaveImmediatelyAsync(CharacterData character, string reason);

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
        private readonly ICharacterAttributeService _attributeService;
        private readonly ILogger<CharacterService> _logger;
        private CharacterListResponse? _cachedCharacters;

        // 事件实现
        public event Action<CharacterData?>? SelectedCharacterChanged;

        public CharacterService(
            HttpClient httpClient, 
            IAuthService authService,
            Blazored.LocalStorage.ILocalStorageService localStorage,
            ApiConfiguration apiConfig,
            ICharacterAttributeService attributeService,
            ILogger<CharacterService> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _localStorage = localStorage;
            _apiConfig = apiConfig;
            _attributeService = attributeService;
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
                
                if (response?.Character != null)
                {
                    // Task 3.1: 加载角色后立即计算属性
                    // Calculate attributes immediately after loading character
                    await _attributeService.RecalculateAndApplyAsync(response.Character);
                    _logger.LogInformation("Loaded and calculated attributes for character {CharacterId}", id);
                }
                
                return response?.Character;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching character {id}: {ex.Message}");
                _logger.LogError(ex, "Error fetching character {CharacterId}", id);
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
                    
                    if (result?.Character != null)
                    {
                        // Task 3.1: 创建角色后初始化属性
                        // Initialize attributes after creating character
                        await _attributeService.RecalculateAndApplyAsync(result.Character);
                        _logger.LogInformation("Created and initialized attributes for character {CharacterId}", result.Character.Id);
                        
                        // 保存初始化后的属性
                        // Save initialized attributes
                        await UpdateCharacterAsync(result.Character.Id, result.Character);
                    }
                    
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
                _logger.LogError(ex, "Error creating character");
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
                    ActiveCombatProfessionId = character.ActiveCombatProfessionId,
                    // 技能数据 (Step 2 Phase 2.5)
                    // Skill data
                    LearnedSkills = character.LearnedSkills,
                    EquippedSkillsByProfession = character.EquippedSkillsByProfession
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

        /// <summary>
        /// 切换战斗职业
        /// Switch combat profession
        /// </summary>
        /// <param name="characterId">角色ID</param>
        /// <param name="professionId">要切换到的职业ID</param>
        /// <returns>切换结果</returns>
        public async Task<CharacterResponse?> SwitchProfessionAsync(string characterId, string professionId)
        {
            try
            {
                await ConfigureAuthHeaderAsync();

                var request = new SwitchProfessionRequest
                {
                    ProfessionId = professionId
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiConfig.CharacterApiUrl}/{characterId}/switch-profession",
                    request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CharacterResponse>();
                    
                    // 清除缓存并触发更新事件
                    // Clear cache and trigger update event
                    _cachedCharacters = null;
                    if (result?.Character != null)
                    {
                        // Task 3.3: 切换职业后重新计算属性
                        // Recalculate attributes after profession switch
                        await _attributeService.RecalculateAndApplyAsync(result.Character);
                        _logger.LogInformation(
                            "Switched profession to {ProfessionId} and recalculated attributes for character {CharacterId}",
                            professionId, characterId);
                        
                        // 立即保存更新后的属性
                        // Immediately save updated attributes
                        await UpdateCharacterAsync(characterId, result.Character);
                        
                        SelectedCharacterChanged?.Invoke(result.Character);
                    }
                    
                    return result;
                }
                else
                {
                    var errorResult = await response.Content.ReadFromJsonAsync<CharacterResponse>();
                    Console.WriteLine($"Error switching profession: {errorResult?.Message ?? "Unknown error"}");
                    _logger.LogError("Error switching profession: {Message}", errorResult?.Message ?? "Unknown error");
                    return errorResult ?? new CharacterResponse
                    {
                        Success = false,
                        Message = "切换职业失败"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error switching profession for character {characterId}: {ex.Message}");
                _logger.LogError(ex, "Error switching profession for character {CharacterId}", characterId);
                return new CharacterResponse
                {
                    Success = false,
                    Message = $"切换职业失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Task 3.5: 强制重新计算并应用角色属性
        /// Force recalculate and apply character attributes
        /// </summary>
        /// <param name="character">要重算的角色</param>
        /// <param name="saveImmediately">是否立即保存到服务器</param>
        public async Task ForceRecalculateAttributesAsync(CharacterData character, bool saveImmediately = false)
        {
            try
            {
                _logger.LogInformation("Force recalculating attributes for character {CharacterId}", character.Id);

                // 重新计算属性
                await _attributeService.RecalculateAndApplyAsync(character);

                // 如果需要立即保存
                if (saveImmediately)
                {
                    await UpdateCharacterAsync(character.Id, character);
                    _logger.LogInformation("Saved recalculated attributes for character {CharacterId}", character.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force recalculating attributes for character {CharacterId}", character.Id);
            }
        }

        /// <summary>
        /// Task 3.5: 批量重算所有角色的属性（用于配置更新后）
        /// Batch recalculate attributes for all characters (used after config update)
        /// </summary>
        public async Task RecalculateAllCharactersAsync(List<CharacterData> characters)
        {
            _logger.LogInformation("Recalculating attributes for {Count} characters", characters.Count);

            foreach (var character in characters)
            {
                await ForceRecalculateAttributesAsync(character, saveImmediately: false);
            }

            _logger.LogInformation("Completed recalculation for all characters");
        }

        /// <summary>
        /// Task 4.2: 立即保存角色数据到服务器（用于关键操作后）
        /// Immediately save character data to server (for critical operations)
        /// </summary>
        /// <param name="character">要保存的角色</param>
        /// <param name="reason">保存原因（用于日志）</param>
        /// <returns>是否保存成功</returns>
        public async Task<bool> SaveImmediatelyAsync(CharacterData character, string reason)
        {
            try
            {
                _logger.LogInformation(
                    "Immediate save triggered for character {CharacterId}, reason: {Reason}",
                    character.Id, reason);

                var result = await UpdateCharacterAsync(character.Id, character);

                if (result != null && result.Success)
                {
                    _logger.LogInformation(
                        "Successfully saved character {CharacterId}",
                        character.Id);
                    return true;
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to save character {CharacterId}: {Message}",
                        character.Id, result?.Message ?? "Unknown error");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during immediate save for character {CharacterId}",
                    character.Id);
                return false;
            }
        }
    }
}
