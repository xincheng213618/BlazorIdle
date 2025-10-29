using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;

namespace BlazorIdle.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // 设置默认鉴权头，后续请求会自动带 Bearer
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch
        {
            // 解析失败时降级为匿名用户，避免抛异常影响页面
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public void NotifyUserAuthentication(string token)
    {
        // 刷新默认鉴权头
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLogout()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();

        // JWT: header.payload.signature，取 payload
        var parts = jwt.Split('.');
        if (parts.Length < 2) return claims;

        var payload = parts[1];
        var jsonBytes = DecodeBase64Url(payload);

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        if (keyValuePairs is null) return claims;

        // 常见声明兼容
        if (keyValuePairs.TryGetValue(ClaimTypes.Name, out var name) && name is not null)
            claims.Add(new Claim(ClaimTypes.Name, name.ToString()!));

        if (keyValuePairs.TryGetValue(ClaimTypes.NameIdentifier, out var userId) && userId is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()!));

        if (keyValuePairs.TryGetValue("sub", out var sub) && sub is not null)
            claims.Add(new Claim("sub", sub.ToString()!));

        // 可根据需要映射其他声明

        return claims;
    }

    // 正确的 Base64Url 解码：替换字符并补齐填充
    private static byte[] DecodeBase64Url(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        var pad = input.Length % 4;
        if (pad == 2) input += "==";
        else if (pad == 3) input += "=";
        else if (pad != 0 && pad != 2 && pad != 3)
            throw new FormatException("Invalid Base64Url string length.");
        return Convert.FromBase64String(input);
    }
}
