using BlazorIdle.Server.Services;
using BlazorIdle.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlazorIdle.Server.Controllers
{
    [ApiController]
    [Route("api/game-config")]
    public class GameConfigController : ControllerBase
    {
        private readonly IGameConfigProvider _provider;

        public GameConfigController(IGameConfigProvider provider)
        {
            _provider = provider;
        }

        // 若配置无敏感信息，可允许匿名访问
        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<ActionResult<GameConfigResponse>> GetAll(CancellationToken ct)
        {
            await _provider.EnsureLoadedAsync(ct);

            var dto = new GameConfigResponse
            {
                Version = _provider.Version,
                Professions = _provider.Professions.ToList(),
                Monsters = _provider.Monsters.ToList(),
                Items = _provider.Items.ToList() // 添加物品配置到响应
            };
            return Ok(dto);
        }
    }
}