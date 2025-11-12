using BlazorIdle.Server.Services;
using BlazorIdle.Shared.DTOs;
using BlazorIdle.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BlazorIdle.Server.Controllers
{
    [ApiController]
    [Route("api/game-config")]
    public class GameConfigController : ControllerBase
    {
        private readonly IGameConfigProvider _provider;
        private readonly ILogger<GameConfigController> _logger;
        private readonly IHostEnvironment _env;

        public GameConfigController(
            IGameConfigProvider provider,
            ILogger<GameConfigController> logger,
            IHostEnvironment env)
        {
            _provider = provider;
            _logger = logger;
            _env = env;
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
                Items = _provider.Items.ToList(),
                Dungeons = _provider.Dungeons.ToList(),
                BattleScenarios = _provider.BattleScenarios.ToList(),
                BattleConfigs = _provider.BattleConfigs.ToList(), // 添加战斗配置到响应（内部配置）
                ProfessionAttributes = new Dictionary<string, ProfessionAttributeConfig>(_provider.ProfessionAttributes), // Phase 2.7: 添加职业属性配置
                MaxProfessionLevel = _provider.MaxProfessionLevel
            };
            return Ok(dto);
        }

        /// <summary>
        /// 获取职业属性配置
        /// Get profession attribute configurations
        /// </summary>
        [HttpGet("profession-attributes")]
        [AllowAnonymous]
        public async Task<ActionResult<Dictionary<string, ProfessionAttributeConfig>>> GetProfessionAttributes(CancellationToken ct)
        {
            await _provider.EnsureLoadedAsync(ct);
            return Ok(new Dictionary<string, ProfessionAttributeConfig>(_provider.ProfessionAttributes));
        }
    }
}