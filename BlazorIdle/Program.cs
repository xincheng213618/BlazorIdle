using BlazorIdle;
using BlazorIdle.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;
using BlazorIdle.Game.Config;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Authentication services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthorizationCore();

// Add Character service
builder.Services.AddScoped<ICharacterService, CharacterService>();

// Game config service (scoped������ HttpClient)
builder.Services.AddScoped<IGameConfigService, GameConfigService>();

await builder.Build().RunAsync();