using BlazorIdle;
using BlazorIdle.Services;
using BlazorIdle.Configuration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using BlazorIdle.Game.Config;
using Microsoft.Extensions.Configuration; // 新增

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// 使用内置配置（自动加载 appsettings.json + appsettings.{Environment}.json）
var apiConfiguration = builder.Configuration.GetSection("ApiSettings").Get<ApiConfiguration>()
    ?? new ApiConfiguration { BaseUrl = "https://localhost:7056" };
builder.Services.AddSingleton(apiConfiguration);

// 心跳配置
var heartbeatConfiguration = builder.Configuration.GetSection("HeartbeatConfig").Get<HeartbeatConfiguration>()
    ?? new HeartbeatConfiguration
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

// 心跳服务
builder.Services.AddScoped<IHeartbeatService, HeartbeatService>();

// 游戏配置服务
builder.Services.AddScoped<IGameConfigService, GameConfigService>();

// 职业属性配置服务
builder.Services.AddScoped<IProfessionAttributeService, ProfessionAttributeService>();

// 角色属性计算服务
builder.Services.AddScoped<ICharacterAttributeService, CharacterAttributeService>();

await builder.Build().RunAsync();