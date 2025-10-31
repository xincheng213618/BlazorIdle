# 离线战斗系统设计文档 - 第三部分：后端API设计与实现

## 1. API端点设计

### 1.1 GameState API

#### 1.1.1 保存游戏状态
```
POST /api/gamestate/save
Authorization: Bearer {token}
Content-Type: application/json

Request Body:
{
  "characterId": "char_001",
  "stateType": "Battle",
  "stateData": {
    // BattleStateData JSON
  }
}

Response: 200 OK
{
  "success": true,
  "message": "游戏状态已保存",
  "stateId": "550e8400-e29b-41d4-a716-446655440000",
  "savedAt": "2025-10-31T03:45:41.218Z"
}
```

#### 1.1.2 获取最新游戏状态
```
GET /api/gamestate/{characterId}/{stateType}
Authorization: Bearer {token}

Response: 200 OK
{
  "success": true,
  "hasState": true,
  "state": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "characterId": "char_001",
    "stateType": "Battle",
    "stateData": { /* JSON */ },
    "lastHeartbeatAt": "2025-10-31T03:45:41.218Z",
    "version": "1.0.0",
    "schemaVersion": 1
  }
}

Response: 404 Not Found (无保存状态)
{
  "success": false,
  "hasState": false,
  "message": "未找到游戏状态"
}
```

#### 1.1.3 模拟离线战斗
```
POST /api/gamestate/simulate-offline
Authorization: Bearer {token}
Content-Type: application/json

Request Body:
{
  "characterId": "char_001",
  "stateId": "550e8400-e29b-41d4-a716-446655440000",
  "offlineDurationSeconds": 3600
}

Response: 200 OK
{
  "success": true,
  "simulatedTimeSeconds": 3600,
  "newState": {
    // 模拟后的新状态
  },
  "summary": {
    "totalKills": 45,
    "totalDeaths": 3,
    "lootGained": {
      "gold": 500,
      "wood": 120
    },
    "wavesCompleted": 8,
    "dungeonsCompleted": 1
  }
}
```

#### 1.1.4 删除游戏状态
```
DELETE /api/gamestate/{stateId}
Authorization: Bearer {token}

Response: 200 OK
{
  "success": true,
  "message": "游戏状态已删除"
}
```

### 1.2 Session API

#### 1.2.1 创建会话（登录时）
```
POST /api/session/create
Authorization: Bearer {token}
Content-Type: application/json

Request Body:
{
  "deviceInfo": "Chrome 120 / Windows 10",
  "ipAddress": "192.168.1.100"
}

Response: 200 OK
{
  "success": true,
  "sessionId": "660e8400-e29b-41d4-a716-446655440001",
  "sessionToken": "eyJhbGc...",
  "expiresAt": "2025-11-01T03:45:41.218Z",
  "previousSessionTerminated": true,
  "message": "会话已创建，之前的会话已终止"
}
```

#### 1.2.2 会话心跳
```
POST /api/session/{sessionId}/heartbeat
Authorization: Bearer {token}

Response: 200 OK
{
  "success": true,
  "isActive": true,
  "lastActivityAt": "2025-10-31T03:45:41.218Z"
}

Response: 410 Gone (会话已失效)
{
  "success": false,
  "isActive": false,
  "terminatedAt": "2025-10-31T03:44:41.218Z",
  "terminationReason": "被新会话顶掉",
  "message": "会话已失效，请重新登录"
}
```

#### 1.2.3 获取活跃会话
```
GET /api/session/active
Authorization: Bearer {token}

Response: 200 OK
{
  "success": true,
  "sessions": [
    {
      "sessionId": "660e8400-e29b-41d4-a716-446655440001",
      "deviceInfo": "Chrome 120 / Windows 10",
      "ipAddress": "192.168.1.100",
      "loginAt": "2025-10-31T03:00:00.000Z",
      "lastActivityAt": "2025-10-31T03:45:41.218Z",
      "isCurrentSession": true
    }
  ]
}
```

#### 1.2.4 终止会话
```
POST /api/session/{sessionId}/terminate
Authorization: Bearer {token}
Content-Type: application/json

Request Body:
{
  "reason": "用户主动登出"
}

Response: 200 OK
{
  "success": true,
  "message": "会话已终止"
}
```

## 2. 服务层实现

### 2.1 GameStateService

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BlazorIdle.Server.Data;
using BlazorIdle.Shared.Models;
using BlazorIdle.Shared.DTOs;

namespace BlazorIdle.Server.Services
{
    public interface IGameStateService
    {
        Task<GameStateSaveResponse> SaveStateAsync(string characterId, string stateType, object stateData);
        Task<GameStateLoadResponse> LoadStateAsync(string characterId, string stateType);
        Task<bool> DeleteStateAsync(Guid stateId, int userId);
        Task CleanupExpiredStatesAsync();
        Task<bool> ValidateStateVersionAsync(string version);
    }

    public class GameStateService : IGameStateService
    {
        private readonly GameDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GameStateService> _logger;
        private readonly string _currentVersion;

        public GameStateService(
            GameDbContext context,
            IConfiguration configuration,
            ILogger<GameStateService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _currentVersion = configuration["GameVersion"] ?? "1.0.0";
        }

        public async Task<GameStateSaveResponse> SaveStateAsync(
            string characterId, 
            string stateType, 
            object stateData)
        {
            try
            {
                // 序列化状态数据
                var stateJson = JsonSerializer.Serialize(stateData);

                // 查找是否已存在该类型的状态
                var existingState = await _context.GameStates
                    .FirstOrDefaultAsync(gs => 
                        gs.CharacterId == characterId && 
                        gs.StateType == stateType);

                var now = DateTime.UtcNow;
                var retentionDays = _configuration.GetValue<int>("GameStateCleanup:RetentionDays", 7);

                if (existingState != null)
                {
                    // 更新现有状态
                    existingState.StateData = stateJson;
                    existingState.Version = _currentVersion;
                    existingState.UpdatedAt = now;
                    existingState.LastHeartbeatAt = now;
                    existingState.ExpiresAt = now.AddDays(retentionDays);
                }
                else
                {
                    // 创建新状态
                    var newState = new GameState
                    {
                        CharacterId = characterId,
                        StateType = stateType,
                        StateData = stateJson,
                        Version = _currentVersion,
                        SchemaVersion = 1,
                        CreatedAt = now,
                        UpdatedAt = now,
                        LastHeartbeatAt = now,
                        ExpiresAt = now.AddDays(retentionDays)
                    };
                    _context.GameStates.Add(newState);
                    existingState = newState;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Saved game state for character {CharacterId}, type {StateType}", 
                    characterId, stateType);

                return new GameStateSaveResponse
                {
                    Success = true,
                    Message = "游戏状态已保存",
                    StateId = existingState.Id,
                    SavedAt = now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save game state for character {CharacterId}", characterId);
                return new GameStateSaveResponse
                {
                    Success = false,
                    Message = "保存游戏状态失败：" + ex.Message
                };
            }
        }

        public async Task<GameStateLoadResponse> LoadStateAsync(string characterId, string stateType)
        {
            try
            {
                var state = await _context.GameStates
                    .Where(gs => gs.CharacterId == characterId && gs.StateType == stateType)
                    .OrderByDescending(gs => gs.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (state == null)
                {
                    return new GameStateLoadResponse
                    {
                        Success = false,
                        HasState = false,
                        Message = "未找到游戏状态"
                    };
                }

                // 检查版本兼容性
                if (!await ValidateStateVersionAsync(state.Version))
                {
                    // 版本不兼容，删除旧状态
                    _context.GameStates.Remove(state);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning(
                        "Removed incompatible state for character {CharacterId}, version {Version}", 
                        characterId, state.Version);

                    return new GameStateLoadResponse
                    {
                        Success = false,
                        HasState = false,
                        Message = "游戏状态版本不兼容，已清除"
                    };
                }

                return new GameStateLoadResponse
                {
                    Success = true,
                    HasState = true,
                    State = state
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load game state for character {CharacterId}", characterId);
                return new GameStateLoadResponse
                {
                    Success = false,
                    HasState = false,
                    Message = "加载游戏状态失败：" + ex.Message
                };
            }
        }

        public async Task<bool> DeleteStateAsync(Guid stateId, int userId)
        {
            try
            {
                var state = await _context.GameStates
                    .Include(gs => gs.Character)
                    .FirstOrDefaultAsync(gs => gs.Id == stateId);

                if (state == null || state.Character?.UserId != userId)
                {
                    return false;
                }

                _context.GameStates.Remove(state);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted game state {StateId}", stateId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete game state {StateId}", stateId);
                return false;
            }
        }

        public async Task CleanupExpiredStatesAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredStates = await _context.GameStates
                    .Where(gs => gs.ExpiresAt != null && gs.ExpiresAt < now)
                    .ToListAsync();

                if (expiredStates.Count > 0)
                {
                    _context.GameStates.RemoveRange(expiredStates);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} expired game states", expiredStates.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired game states");
            }
        }

        public Task<bool> ValidateStateVersionAsync(string version)
        {
            // 简单的版本兼容性检查
            // 主版本必须相同
            var currentParts = _currentVersion.Split('.');
            var stateParts = version.Split('.');

            if (currentParts.Length < 1 || stateParts.Length < 1)
            {
                return Task.FromResult(false);
            }

            // 主版本不同则不兼容
            if (currentParts[0] != stateParts[0])
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
```

### 2.2 SessionService

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BlazorIdle.Server.Data;
using BlazorIdle.Shared.Models;
using BlazorIdle.Shared.DTOs;

namespace BlazorIdle.Server.Services
{
    public interface ISessionService
    {
        Task<SessionCreateResponse> CreateSessionAsync(int userId, string? deviceInfo, string? ipAddress);
        Task<SessionHeartbeatResponse> HeartbeatAsync(Guid sessionId);
        Task<bool> TerminateSessionAsync(Guid sessionId, string reason);
        Task<List<UserSession>> GetActiveSessionsAsync(int userId);
        Task CleanupExpiredSessionsAsync();
    }

    public class SessionService : ISessionService
    {
        private readonly GameDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SessionService> _logger;

        public SessionService(
            GameDbContext context,
            IConfiguration configuration,
            ILogger<SessionService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<SessionCreateResponse> CreateSessionAsync(
            int userId, 
            string? deviceInfo, 
            string? ipAddress)
        {
            try
            {
                var now = DateTime.UtcNow;
                var sessionTimeout = _configuration.GetValue<int>("Session:TimeoutHours", 24);

                // 查找该用户的所有活跃会话
                var activeSessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && s.IsActive)
                    .ToListAsync();

                bool previousSessionTerminated = false;

                // 终止所有现有活跃会话（实现顶号功能）
                if (activeSessions.Count > 0)
                {
                    foreach (var session in activeSessions)
                    {
                        session.IsActive = false;
                        session.TerminatedAt = now;
                        session.TerminationReason = "被新会话顶掉";
                    }
                    previousSessionTerminated = true;

                    _logger.LogInformation(
                        "Terminated {Count} active sessions for user {UserId}", 
                        activeSessions.Count, userId);
                }

                // 创建新会话
                var newSession = new UserSession
                {
                    UserId = userId,
                    SessionToken = Guid.NewGuid().ToString(),
                    DeviceInfo = deviceInfo,
                    IpAddress = ipAddress,
                    LoginAt = now,
                    LastActivityAt = now,
                    ExpiresAt = now.AddHours(sessionTimeout),
                    IsActive = true
                };

                _context.UserSessions.Add(newSession);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Created new session {SessionId} for user {UserId}", 
                    newSession.Id, userId);

                return new SessionCreateResponse
                {
                    Success = true,
                    SessionId = newSession.Id,
                    SessionToken = newSession.SessionToken,
                    ExpiresAt = newSession.ExpiresAt,
                    PreviousSessionTerminated = previousSessionTerminated,
                    Message = previousSessionTerminated 
                        ? "会话已创建，之前的会话已终止" 
                        : "会话已创建"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create session for user {UserId}", userId);
                return new SessionCreateResponse
                {
                    Success = false,
                    Message = "创建会话失败：" + ex.Message
                };
            }
        }

        public async Task<SessionHeartbeatResponse> HeartbeatAsync(Guid sessionId)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                {
                    return new SessionHeartbeatResponse
                    {
                        Success = false,
                        IsActive = false,
                        Message = "会话不存在"
                    };
                }

                // 检查会话是否已失效
                if (!session.IsActive)
                {
                    return new SessionHeartbeatResponse
                    {
                        Success = false,
                        IsActive = false,
                        TerminatedAt = session.TerminatedAt,
                        TerminationReason = session.TerminationReason,
                        Message = "会话已失效，请重新登录"
                    };
                }

                // 更新最后活动时间
                session.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new SessionHeartbeatResponse
                {
                    Success = true,
                    IsActive = true,
                    LastActivityAt = session.LastActivityAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process heartbeat for session {SessionId}", sessionId);
                return new SessionHeartbeatResponse
                {
                    Success = false,
                    IsActive = false,
                    Message = "会话心跳失败：" + ex.Message
                };
            }
        }

        public async Task<bool> TerminateSessionAsync(Guid sessionId, string reason)
        {
            try
            {
                var session = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null || !session.IsActive)
                {
                    return false;
                }

                session.IsActive = false;
                session.TerminatedAt = DateTime.UtcNow;
                session.TerminationReason = reason;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Terminated session {SessionId}, reason: {Reason}", 
                    sessionId, reason);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to terminate session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<List<UserSession>> GetActiveSessionsAsync(int userId)
        {
            return await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var inactiveTimeout = _configuration.GetValue<int>("SessionCleanup:InactiveSessionTimeoutHours", 24);

                var expiredSessions = await _context.UserSessions
                    .Where(s => s.ExpiresAt < now || 
                                (s.IsActive && s.LastActivityAt < now.AddHours(-inactiveTimeout)))
                    .ToListAsync();

                if (expiredSessions.Count > 0)
                {
                    foreach (var session in expiredSessions)
                    {
                        session.IsActive = false;
                        session.TerminatedAt = now;
                        session.TerminationReason = session.TerminationReason ?? "Session expired";
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired sessions");
            }
        }
    }
}
```

### 2.3 OfflineBattleSimulator

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BlazorIdle.Game;
using BlazorIdle.Game.Config;
using BlazorIdle.Shared.DTOs;

namespace BlazorIdle.Server.Services
{
    public interface IOfflineBattleSimulator
    {
        Task<OfflineSimulationResult> SimulateOfflineBattleAsync(
            BattleStateData savedState, 
            int offlineDurationSeconds);
    }

    public class OfflineBattleSimulator : IOfflineBattleSimulator
    {
        private readonly IGameConfigService _gameConfig;
        private readonly ILogger<OfflineBattleSimulator> _logger;
        private const int MaxSimulationSeconds = 86400; // 24小时
        private const int SimulationTickMs = 100; // 模拟时间步长

        public OfflineBattleSimulator(
            IGameConfigService gameConfig,
            ILogger<OfflineBattleSimulator> logger)
        {
            _gameConfig = gameConfig;
            _logger = logger;
        }

        public async Task<OfflineSimulationResult> SimulateOfflineBattleAsync(
            BattleStateData savedState, 
            int offlineDurationSeconds)
        {
            try
            {
                // 限制最大模拟时长
                var simulationSeconds = Math.Min(offlineDurationSeconds, MaxSimulationSeconds);
                var simulationMs = simulationSeconds * 1000;

                _logger.LogInformation(
                    "Starting offline simulation for {Seconds} seconds", 
                    simulationSeconds);

                // 1. 重建战斗状态
                var (dungeonManager, playerCharacter) = await RebuildBattleStateAsync(savedState);

                if (dungeonManager == null)
                {
                    throw new InvalidOperationException("Failed to rebuild battle state");
                }

                // 2. 快速推进战斗
                var summary = new SimulationSummary();
                var totalTicks = simulationMs / SimulationTickMs;

                // 订阅事件以收集统计
                dungeonManager.LootDropped += (loot) =>
                {
                    if (!summary.LootGained.ContainsKey(loot.ItemId))
                        summary.LootGained[loot.ItemId] = 0;
                    summary.LootGained[loot.ItemId] += loot.Quantity;
                };

                dungeonManager.DungeonCompleted += (ev) =>
                {
                    if (ev.Success)
                    {
                        summary.DungeonsCompleted++;
                        summary.WavesCompleted += ev.DungeonName != null ? 
                            savedState.TotalWaves : 0;
                    }
                    summary.TotalKills += ev.TotalKills;
                    summary.TotalDeaths += ev.TotalDeaths;
                };

                // 执行模拟
                for (int i = 0; i < totalTicks; i++)
                {
                    dungeonManager.AdvanceTick(SimulationTickMs);
                }

                // 3. 提取最终状态
                var finalSnapshot = dungeonManager.GetSnapshot();
                var newState = ConvertToStateData(finalSnapshot, playerCharacter, savedState);

                _logger.LogInformation(
                    "Simulation completed: {Kills} kills, {Loot} items looted", 
                    summary.TotalKills, 
                    summary.LootGained.Count);

                return new OfflineSimulationResult
                {
                    Success = true,
                    SimulatedTimeSeconds = simulationSeconds,
                    NewState = newState,
                    Summary = summary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to simulate offline battle");
                return new OfflineSimulationResult
                {
                    Success = false,
                    Message = "离线模拟失败：" + ex.Message
                };
            }
        }

        private async Task<(DungeonManager?, Character?)> RebuildBattleStateAsync(
            BattleStateData savedState)
        {
            // 实现战斗状态重建逻辑
            // 这里需要根据savedState重新创建DungeonManager和Character
            // 具体实现取决于现有的战斗系统架构
            
            // 伪代码示例：
            // 1. 从配置获取副本定义
            // 2. 创建角色实例
            // 3. 创建DungeonManager
            // 4. 恢复状态（波次、血量、时钟等）
            
            await Task.CompletedTask; // 占位
            return (null, null);
        }

        private BattleStateData ConvertToStateData(
            DungeonSnapshot snapshot, 
            Character character, 
            BattleStateData originalState)
        {
            // 实现快照转换为状态数据的逻辑
            return originalState; // 占位
        }
    }

    public class SimulationSummary
    {
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public Dictionary<string, int> LootGained { get; set; } = new();
        public int WavesCompleted { get; set; }
        public int DungeonsCompleted { get; set; }
    }
}
```

## 3. Controller实现

### 3.1 GameStateController

```csharp
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BlazorIdle.Server.Services;
using BlazorIdle.Shared.DTOs;

namespace BlazorIdle.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameStateController : ControllerBase
    {
        private readonly IGameStateService _gameStateService;
        private readonly IOfflineBattleSimulator _simulator;
        private readonly ILogger<GameStateController> _logger;

        public GameStateController(
            IGameStateService gameStateService,
            IOfflineBattleSimulator simulator,
            ILogger<GameStateController> logger)
        {
            _gameStateService = gameStateService;
            _simulator = simulator;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        [HttpPost("save")]
        public async Task<ActionResult<GameStateSaveResponse>> SaveState(
            [FromBody] GameStateSaveRequest request)
        {
            var response = await _gameStateService.SaveStateAsync(
                request.CharacterId,
                request.StateType,
                request.StateData);

            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("{characterId}/{stateType}")]
        public async Task<ActionResult<GameStateLoadResponse>> LoadState(
            string characterId, 
            string stateType)
        {
            var response = await _gameStateService.LoadStateAsync(characterId, stateType);

            return response.Success && response.HasState 
                ? Ok(response) 
                : NotFound(response);
        }

        [HttpPost("simulate-offline")]
        public async Task<ActionResult<OfflineSimulationResult>> SimulateOffline(
            [FromBody] OfflineSimulationRequest request)
        {
            // 1. 加载保存的状态
            var loadResponse = await _gameStateService.LoadStateAsync(
                request.CharacterId, 
                "Battle");

            if (!loadResponse.Success || !loadResponse.HasState)
            {
                return NotFound(new { message = "未找到保存的战斗状态" });
            }

            // 2. 解析状态数据
            var stateData = System.Text.Json.JsonSerializer.Deserialize<BattleStateData>(
                loadResponse.State!.StateData);

            if (stateData == null)
            {
                return BadRequest(new { message = "状态数据解析失败" });
            }

            // 3. 执行离线模拟
            var result = await _simulator.SimulateOfflineBattleAsync(
                stateData, 
                request.OfflineDurationSeconds);

            return result.Success ? Ok(result) : StatusCode(500, result);
        }

        [HttpDelete("{stateId}")]
        public async Task<ActionResult> DeleteState(Guid stateId)
        {
            var userId = GetCurrentUserId();
            var success = await _gameStateService.DeleteStateAsync(stateId, userId);

            return success 
                ? Ok(new { success = true, message = "游戏状态已删除" })
                : NotFound(new { success = false, message = "游戏状态不存在或无权删除" });
        }
    }
}
```

### 3.2 SessionController

```csharp
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BlazorIdle.Server.Services;
using BlazorIdle.Shared.DTOs;

namespace BlazorIdle.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly ILogger<SessionController> _logger;

        public SessionController(
            ISessionService sessionService,
            ILogger<SessionController> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim ?? "0");
        }

        [HttpPost("create")]
        public async Task<ActionResult<SessionCreateResponse>> CreateSession(
            [FromBody] SessionCreateRequest request)
        {
            var userId = GetCurrentUserId();
            var response = await _sessionService.CreateSessionAsync(
                userId, 
                request.DeviceInfo, 
                request.IpAddress);

            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPost("{sessionId}/heartbeat")]
        public async Task<ActionResult<SessionHeartbeatResponse>> Heartbeat(Guid sessionId)
        {
            var response = await _sessionService.HeartbeatAsync(sessionId);

            if (!response.Success && !response.IsActive)
            {
                return StatusCode(410, response); // 410 Gone
            }

            return Ok(response);
        }

        [HttpGet("active")]
        public async Task<ActionResult> GetActiveSessions()
        {
            var userId = GetCurrentUserId();
            var sessions = await _sessionService.GetActiveSessionsAsync(userId);

            return Ok(new { success = true, sessions });
        }

        [HttpPost("{sessionId}/terminate")]
        public async Task<ActionResult> TerminateSession(
            Guid sessionId,
            [FromBody] SessionTerminateRequest request)
        {
            var success = await _sessionService.TerminateSessionAsync(
                sessionId, 
                request.Reason ?? "用户主动登出");

            return success
                ? Ok(new { success = true, message = "会话已终止" })
                : NotFound(new { success = false, message = "会话不存在或已失效" });
        }
    }
}
```

## 4. DTO定义

所有请求和响应DTO的定义，详见代码注释。

## 5. 后台服务配置

### 5.1 定时清理服务

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BlazorIdle.Server.Services;

namespace BlazorIdle.Server.BackgroundServices
{
    public class CleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

        public CleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<CleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cleanup background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var gameStateService = scope.ServiceProvider.GetRequiredService<IGameStateService>();
                    var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

                    await gameStateService.CleanupExpiredStatesAsync();
                    await sessionService.CleanupExpiredSessionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cleanup");
                }
            }

            _logger.LogInformation("Cleanup background service stopped");
        }
    }
}
```

### 5.2 在Program.cs中注册服务

```csharp
// 注册服务
builder.Services.AddScoped<IGameStateService, GameStateService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IOfflineBattleSimulator, OfflineBattleSimulator>();

// 注册后台服务
builder.Services.AddHostedService<CleanupBackgroundService>();

// 配置游戏版本
builder.Configuration["GameVersion"] = "1.0.0";
```

## 下一步

继续查看：
- 第四部分：前端集成与状态管理
- 第五部分：测试方案与部署指南
