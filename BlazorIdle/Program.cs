using BlazorIdle;
using BlazorIdle.Services;
using BlazorIdle.Configuration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using BlazorIdle.Game.Config;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Load API configuration from appsettings.json
var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var apiConfig = await http.GetFromJsonAsync<Dictionary<string, ApiConfiguration>>("appsettings.json");
var apiConfiguration = apiConfig?["ApiSettings"] ?? new ApiConfiguration { BaseUrl = "https://localhost:7056" };
builder.Services.AddSingleton(apiConfiguration);

// 加载心跳配置
// Load heartbeat configuration
var heartbeatConfig = await http.GetFromJsonAsync<Dictionary<string, HeartbeatConfiguration>>("appsettings.json");
var heartbeatConfiguration = heartbeatConfig?["HeartbeatConfig"] ?? new HeartbeatConfiguration 
{ 
    SaveIntervalSeconds = 30,
    EnableAutoSave = true 
};
builder.Services.AddSingleton(heartbeatConfiguration);

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Authentication services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthorizationCore();

// Add Character service
builder.Services.AddScoped<ICharacterService, CharacterService>();

// 添加心跳服务 - Scoped生命周期，每个用户会话独立
// Add Heartbeat service - Scoped lifetime, independent per user session
builder.Services.AddScoped<IHeartbeatService, HeartbeatService>();

// Game config service (scoped依赖 HttpClient)
builder.Services.AddScoped<IGameConfigService, GameConfigService>();

await builder.Build().RunAsync();