using System.Net.Http.Headers;
using System.Net.Http.Json;
using BlazorIdle.Shared.DTOs;
using BlazorIdle.Configuration;
using Blazored.LocalStorage;

namespace BlazorIdle.Services;

public interface IAuthService
{
    Task<AuthResponse> Login(LoginRequest request);
    Task<AuthResponse> Register(RegisterRequest request);
    Task Logout();
    Task<string?> GetToken();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ApiConfiguration _apiConfig;
    private readonly CustomAuthStateProvider _authStateProvider;
    private const string TokenKey = "authToken";
    private const string UsernameKey = "username";

    public AuthService(HttpClient httpClient, ILocalStorageService localStorage, ApiConfiguration apiConfig, CustomAuthStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _apiConfig = apiConfig;
        _authStateProvider = authStateProvider;
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiConfig.AuthApiUrl}/login", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null && result.Success && !string.IsNullOrEmpty(result.Token))
                {
                    await _localStorage.SetItemAsync(TokenKey, result.Token);
                    await _localStorage.SetItemAsync(UsernameKey, result.Username);

                    // 立刻生效：设置默认鉴权头并通知状态变更
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                    _authStateProvider.NotifyUserAuthentication(result.Token);
                }
                return result ?? new AuthResponse { Success = false, Message = "Invalid response from server" };
            }
            else
            {
                var errorResult = await response.Content.ReadFromJsonAsync<AuthResponse>();
                return errorResult ?? new AuthResponse { Success = false, Message = "Login failed" };
            }
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task<AuthResponse> Register(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiConfig.AuthApiUrl}/register", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null && result.Success && !string.IsNullOrEmpty(result.Token))
                {
                    await _localStorage.SetItemAsync(TokenKey, result.Token);
                    await _localStorage.SetItemAsync(UsernameKey, result.Username);
                }
                return result ?? new AuthResponse { Success = false, Message = "Invalid response from server" };
            }
            else
            {
                var errorResult = await response.Content.ReadFromJsonAsync<AuthResponse>();
                return errorResult ?? new AuthResponse { Success = false, Message = "Registration failed" };
            }
        }
        catch (Exception ex)
        {
            return new AuthResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        await _localStorage.RemoveItemAsync(UsernameKey);
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _authStateProvider.NotifyUserLogout();
    }

    public async Task<string?> GetToken()
    {
        return await _localStorage.GetItemAsync<string>(TokenKey);
    }
}
