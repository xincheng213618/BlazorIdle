# 多人战斗系统设计 - 第二部分：数据库设计与API

## 1. 数据库设计

### 1.1 Parties 表（队伍信息）

```sql
CREATE TABLE Parties (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PartyName NVARCHAR(100) NOT NULL,
    LeaderId INT NOT NULL,                    -- 队长用户ID
    DungeonId NVARCHAR(50),                   -- 当前副本ID
    MaxMembers INT NOT NULL DEFAULT 5,        -- 最大成员数
    CurrentMembers INT NOT NULL DEFAULT 1,    -- 当前成员数
    Status VARCHAR(50) NOT NULL,              -- Created, Fighting, Completed, Disbanded
    IsHosted BIT NOT NULL DEFAULT 0,          -- 是否服务器托管中
    CreatedAt DATETIME2 NOT NULL,
    StartedAt DATETIME2,                      -- 战斗开始时间
    LastSyncAt DATETIME2,                     -- 最后同步时间（队长心跳）
    CompletedAt DATETIME2,
    DisbandedAt DATETIME2,
    FOREIGN KEY (LeaderId) REFERENCES Users(Id)
);

CREATE INDEX IX_Parties_LeaderId ON Parties(LeaderId);
CREATE INDEX IX_Parties_Status ON Parties(Status);
CREATE INDEX IX_Parties_LastSyncAt ON Parties(LastSyncAt) WHERE Status = 'Fighting';
```

### 1.2 PartyMembers 表（队伍成员）

```sql
CREATE TABLE PartyMembers (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PartyId UNIQUEIDENTIFIER NOT NULL,
    UserId INT NOT NULL,
    CharacterId NVARCHAR(50) NOT NULL,        -- 使用的角色ID
    Role VARCHAR(20) NOT NULL,                -- Leader, Member
    Status VARCHAR(20) NOT NULL,              -- Active, Offline, Left
    JoinedAt DATETIME2 NOT NULL,
    LastSeenAt DATETIME2 NOT NULL,            -- 最后在线时间
    LeftAt DATETIME2,
    FOREIGN KEY (PartyId) REFERENCES Parties(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (CharacterId) REFERENCES Characters(Id)
);

CREATE INDEX IX_PartyMembers_PartyId ON PartyMembers(PartyId);
CREATE INDEX IX_PartyMembers_UserId ON PartyMembers(UserId);
CREATE INDEX IX_PartyMembers_CharacterId ON PartyMembers(CharacterId);
CREATE UNIQUE INDEX IX_PartyMembers_PartyUser ON PartyMembers(PartyId, UserId);
```

### 1.3 PartyBattleSnapshots 表（战斗快照）

```sql
CREATE TABLE PartyBattleSnapshots (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PartyId UNIQUEIDENTIFIER NOT NULL,
    SnapshotData NVARCHAR(MAX) NOT NULL,      -- JSON格式的战斗快照
    ElapsedMs INT NOT NULL,                   -- 战斗已进行时间（毫秒）
    CurrentWave INT NOT NULL,                 -- 当前波次
    TotalWaves INT NOT NULL,                  -- 总波次
    CompletionCount INT NOT NULL DEFAULT 0,   -- 完成次数
    IsVerified BIT NOT NULL DEFAULT 0,        -- 是否已验证
    UploadedBy INT NOT NULL,                  -- 上传者（队长）
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (PartyId) REFERENCES Parties(Id) ON DELETE CASCADE,
    FOREIGN KEY (UploadedBy) REFERENCES Users(Id)
);

CREATE INDEX IX_PartyBattleSnapshots_PartyId ON PartyBattleSnapshots(PartyId);
CREATE INDEX IX_PartyBattleSnapshots_CreatedAt ON PartyBattleSnapshots(PartyId, CreatedAt DESC);
```

### 1.4 PartyLootRecords 表（掉落记录）

```sql
CREATE TABLE PartyLootRecords (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PartyId UNIQUEIDENTIFIER NOT NULL,
    SnapshotId UNIQUEIDENTIFIER NOT NULL,     -- 关联的快照ID
    ItemId NVARCHAR(50) NOT NULL,
    Quantity INT NOT NULL,
    DroppedAt DATETIME2 NOT NULL,
    DistributedAt DATETIME2,                  -- 分配时间
    FOREIGN KEY (PartyId) REFERENCES Parties(Id) ON DELETE CASCADE,
    FOREIGN KEY (SnapshotId) REFERENCES PartyBattleSnapshots(Id)
);

CREATE INDEX IX_PartyLootRecords_PartyId ON PartyLootRecords(PartyId);
CREATE INDEX IX_PartyLootRecords_SnapshotId ON PartyLootRecords(SnapshotId);
```

### 1.5 PartyMemberRewards 表（成员待领取收益）

```sql
CREATE TABLE PartyMemberRewards (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PartyId UNIQUEIDENTIFIER NOT NULL,
    MemberId UNIQUEIDENTIFIER NOT NULL,       -- PartyMembers.Id
    UserId INT NOT NULL,
    CharacterId NVARCHAR(50) NOT NULL,
    ItemId NVARCHAR(50) NOT NULL,
    Quantity INT NOT NULL,
    EarnedAt DATETIME2 NOT NULL,              -- 获得时间
    ClaimedAt DATETIME2,                      -- 领取时间
    FOREIGN KEY (PartyId) REFERENCES Parties(Id) ON DELETE CASCADE,
    FOREIGN KEY (MemberId) REFERENCES PartyMembers(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (CharacterId) REFERENCES Characters(Id)
);

CREATE INDEX IX_PartyMemberRewards_UserId ON PartyMemberRewards(UserId, ClaimedAt);
CREATE INDEX IX_PartyMemberRewards_PartyMember ON PartyMemberRewards(PartyId, MemberId);
CREATE INDEX IX_PartyMemberRewards_Unclaimed ON PartyMemberRewards(UserId) 
    WHERE ClaimedAt IS NULL;
```

### 1.6 PartyInvitations 表（队伍邀请）

```sql
CREATE TABLE PartyInvitations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PartyId UNIQUEIDENTIFIER NOT NULL,
    InviterId INT NOT NULL,                   -- 邀请者ID
    InviteeId INT NOT NULL,                   -- 被邀请者ID
    Status VARCHAR(20) NOT NULL,              -- Pending, Accepted, Declined, Expired
    CreatedAt DATETIME2 NOT NULL,
    RespondedAt DATETIME2,
    ExpiresAt DATETIME2 NOT NULL,
    FOREIGN KEY (PartyId) REFERENCES Parties(Id) ON DELETE CASCADE,
    FOREIGN KEY (InviterId) REFERENCES Users(Id),
    FOREIGN KEY (InviteeId) REFERENCES Users(Id)
);

CREATE INDEX IX_PartyInvitations_InviteeId ON PartyInvitations(InviteeId, Status);
CREATE INDEX IX_PartyInvitations_PartyId ON PartyInvitations(PartyId);
```

## 2. 核心实体类定义

### 2.1 Party 实体

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlazorIdle.Shared.Models
{
    public enum PartyStatus
    {
        Created,      // 已创建，等待成员
        Fighting,     // 战斗中
        Completed,    // 已完成
        Disbanded     // 已解散
    }

    public class Party
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string PartyName { get; set; } = string.Empty;

        [Required]
        public int LeaderId { get; set; }

        [MaxLength(50)]
        public string? DungeonId { get; set; }

        public int MaxMembers { get; set; } = 5;
        public int CurrentMembers { get; set; } = 1;

        [Required]
        public PartyStatus Status { get; set; } = PartyStatus.Created;

        public bool IsHosted { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? DisbandedAt { get; set; }

        // 导航属性
        public virtual User? Leader { get; set; }
        public virtual ICollection<PartyMember> Members { get; set; } = new List<PartyMember>();
    }
}
```

### 2.2 PartyMember 实体

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorIdle.Shared.Models
{
    public enum MemberRole
    {
        Leader,
        Member
    }

    public enum MemberStatus
    {
        Active,    // 在线活跃
        Offline,   // 离线
        Left       // 已离队
    }

    public class PartyMember
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PartyId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string CharacterId { get; set; } = string.Empty;

        [Required]
        public MemberRole Role { get; set; } = MemberRole.Member;

        [Required]
        public MemberStatus Status { get; set; } = MemberStatus.Active;

        [Required]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

        public DateTime? LeftAt { get; set; }

        // 导航属性
        public virtual Party? Party { get; set; }
        public virtual User? User { get; set; }
        public virtual CharacterData? Character { get; set; }
    }
}
```

### 2.3 PartyBattleSnapshot 实体

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace BlazorIdle.Shared.Models
{
    public class PartyBattleSnapshot
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PartyId { get; set; }

        [Required]
        public string SnapshotData { get; set; } = "{}";

        public int ElapsedMs { get; set; }
        public int CurrentWave { get; set; }
        public int TotalWaves { get; set; }
        public int CompletionCount { get; set; } = 0;
        public bool IsVerified { get; set; } = false;

        [Required]
        public int UploadedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 导航属性
        public virtual Party? Party { get; set; }
        public virtual User? Uploader { get; set; }
    }
}
```

## 3. 战斗快照数据结构（JSON）

### 3.1 PartyBattleSnapshotData

```csharp
namespace BlazorIdle.Shared.DTOs
{
    /// <summary>
    /// 队伍战斗快照数据
    /// </summary>
    public class PartyBattleSnapshotData
    {
        // 基本信息
        public Guid PartyId { get; set; }
        public string DungeonId { get; set; } = string.Empty;
        public string DungeonName { get; set; } = string.Empty;

        // 副本进度
        public int CurrentWaveIndex { get; set; }
        public int TotalWaves { get; set; }
        public string DungeonState { get; set; } = "Fighting";
        public int DungeonElapsedMs { get; set; }
        public int CompletionCount { get; set; }
        public bool AutoRepeatEnabled { get; set; }

        // 战斗状态
        public int ClockTimeMs { get; set; }
        public int RngSeed { get; set; }
        public int RngIndex { get; set; }

        // 队伍状态（所有玩家角色）
        public List<PartyCharacterState> Characters { get; set; } = new();

        // 敌人状态
        public List<PartyEnemyState> Enemies { get; set; } = new();

        // 统计数据
        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public Dictionary<string, int> TotalLoot { get; set; } = new();

        // 时间戳
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string GameVersion { get; set; } = "1.0.0";
    }

    /// <summary>
    /// 队伍中角色状态
    /// </summary>
    public class PartyCharacterState
    {
        public string CharacterId { get; set; } = string.Empty;
        public string CharacterName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public bool IsAlive { get; set; }
        public bool IsOnline { get; set; }  // 玩家是否在线

        // 攻击轨道状态
        public TrackStateData? AttackTrack { get; set; }
        public TrackStateData? SpecialTrack { get; set; }

        // 战斗统计
        public int DamageDealt { get; set; }
        public int DamageTaken { get; set; }
        public int Kills { get; set; }
    }

    /// <summary>
    /// 敌人状态
    /// </summary>
    public class PartyEnemyState
    {
        public string EnemyId { get; set; } = string.Empty;
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public bool IsAlive { get; set; }
        public TrackStateData? AttackTrack { get; set; }
    }

    /// <summary>
    /// 轨道状态数据
    /// </summary>
    public class TrackStateData
    {
        public int LastTriggerMs { get; set; }
        public double IntervalMs { get; set; }
        public double HasteMultiplier { get; set; } = 1.0;
    }
}
```

## 4. API端点设计

### 4.1 队伍管理 API

#### 4.1.1 创建队伍
```
POST /api/party/create
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "partyName": "萌新小队",
  "maxMembers": 5
}

Response: 200 OK
{
  "success": true,
  "partyId": "guid",
  "partyName": "萌新小队",
  "leaderId": 123,
  "message": "队伍创建成功"
}
```

#### 4.1.2 邀请玩家
```
POST /api/party/{partyId}/invite
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "inviteeUsername": "player123"
}

Response: 200 OK
{
  "success": true,
  "invitationId": "guid",
  "expiresAt": "2025-11-01T12:00:00Z",
  "message": "邀请已发送"
}
```

#### 4.1.3 接受邀请
```
POST /api/party/invitation/{invitationId}/accept
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "characterId": "char_001"
}

Response: 200 OK
{
  "success": true,
  "partyId": "guid",
  "message": "已加入队伍"
}
```

#### 4.1.4 开始副本战斗
```
POST /api/party/{partyId}/start-dungeon
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "dungeonId": "dungeon_forest_1"
}

Response: 200 OK
{
  "success": true,
  "battleData": {
    "partyId": "guid",
    "dungeonId": "dungeon_forest_1",
    "characters": [
      {
        "characterId": "char_001",
        "userId": 123,
        "characterData": { /* CharacterData */ }
      },
      // ... 其他队员
    ],
    "rngSeed": 123456
  },
  "message": "战斗已开始，您是队长，请运行战斗"
}
```

### 4.2 战斗同步 API

#### 4.2.1 上传战斗快照（队长）
```
POST /api/party/{partyId}/sync-battle
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "snapshotData": { /* PartyBattleSnapshotData */ },
  "newLoots": [
    {
      "itemId": "gold",
      "quantity": 150
    }
  ]
}

Response: 200 OK
{
  "success": true,
  "snapshotId": "guid",
  "isVerified": true,
  "message": "战斗快照已验证"
}

Response: 400 Bad Request (检测到异常)
{
  "success": false,
  "message": "检测到异常数据，已转服务器托管"
}
```

#### 4.2.2 获取战斗状态（队员）
```
GET /api/party/{partyId}/battle-status
Authorization: Bearer {token}

Response: 200 OK
{
  "success": true,
  "partyId": "guid",
  "status": "Fighting",
  "isHosted": false,
  "currentWave": 3,
  "totalWaves": 5,
  "elapsedMs": 180000,
  "completionCount": 2,
  "characters": [
    {
      "characterId": "char_001",
      "characterName": "勇士",
      "isOnline": true,
      "currentHp": 800,
      "maxHp": 1000,
      "damageDealt": 5000
    }
  ],
  "recentLogs": [
    "[60.5s] 勇士 攻击 哥布林#1，造成 120 伤害",
    "[61.2s] 法师 特技 命中 哥布林#2，造成 250 伤害 [暴击!]"
  ],
  "pendingRewardsCount": 5
}
```

#### 4.2.3 领取收益（所有成员）
```
POST /api/party/{partyId}/claim-rewards
Authorization: Bearer {token}

Response: 200 OK
{
  "success": true,
  "rewards": [
    {
      "itemId": "gold",
      "quantity": 150
    },
    {
      "itemId": "wood",
      "quantity": 30
    }
  ],
  "message": "收益已领取"
}
```

### 4.3 托管控制 API

#### 4.3.1 检查托管状态
```
GET /api/party/{partyId}/hosting-status
Authorization: Bearer {token}

Response: 200 OK
{
  "success": true,
  "isHosted": true,
  "hostedSince": "2025-10-31T14:00:00Z",
  "reason": "队长离线超过5分钟",
  "message": "当前由服务器托管战斗"
}
```

#### 4.3.2 队长恢复控制
```
POST /api/party/{partyId}/resume-control
Authorization: Bearer {token}

Response: 200 OK
{
  "success": true,
  "currentSnapshot": { /* PartyBattleSnapshotData */ },
  "message": "控制权已恢复，请继续运行战斗"
}
```

## 5. 服务层接口定义

### 5.1 IPartyService

```csharp
namespace BlazorIdle.Server.Services
{
    public interface IPartyService
    {
        // 队伍管理
        Task<PartyCreateResponse> CreatePartyAsync(int leaderId, string partyName, int maxMembers);
        Task<InvitationResponse> InvitePlayerAsync(Guid partyId, int inviterId, string inviteeUsername);
        Task<JoinPartyResponse> AcceptInvitationAsync(Guid invitationId, int userId, string characterId);
        Task<bool> LeavePartyAsync(Guid partyId, int userId);
        Task<bool> DisbandPartyAsync(Guid partyId, int leaderId);

        // 战斗控制
        Task<StartBattleResponse> StartDungeonAsync(Guid partyId, int leaderId, string dungeonId);
        Task<Party?> GetPartyAsync(Guid partyId);
        Task<List<Party>> GetUserPartiesAsync(int userId);
    }
}
```

### 5.2 IPartyBattleService

```csharp
namespace BlazorIdle.Server.Services
{
    public interface IPartyBattleService
    {
        // 战斗同步
        Task<SyncBattleResponse> SyncBattleSnapshotAsync(
            Guid partyId, 
            int leaderId, 
            PartyBattleSnapshotData snapshotData,
            List<LootItem> newLoots);

        // 状态查询
        Task<BattleStatusResponse> GetBattleStatusAsync(Guid partyId, int userId);
        Task<PartyBattleSnapshot?> GetLatestSnapshotAsync(Guid partyId);

        // 收益管理
        Task<ClaimRewardsResponse> ClaimRewardsAsync(Guid partyId, int userId, string characterId);
        Task<int> GetPendingRewardsCountAsync(int userId);

        // 验证
        Task<bool> ValidateSnapshotAsync(PartyBattleSnapshotData snapshot);
    }
}
```

### 5.3 IPartyHostingService

```csharp
namespace BlazorIdle.Server.Services
{
    public interface IPartyHostingService
    {
        // 托管控制
        Task StartHostingAsync(Guid partyId);
        Task StopHostingAsync(Guid partyId);
        Task<bool> IsHostedAsync(Guid partyId);

        // 后台任务
        Task ProcessHostedBattlesAsync();  // 后台任务调用
        Task<HostingStatusResponse> GetHostingStatusAsync(Guid partyId);
        Task<ResumeControlResponse> ResumeLeaderControlAsync(Guid partyId, int leaderId);
    }
}
```

## 6. 响应DTO定义

### 6.1 常用响应

```csharp
namespace BlazorIdle.Shared.DTOs
{
    public class PartyCreateResponse
    {
        public bool Success { get; set; }
        public Guid PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public int LeaderId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class StartBattleResponse
    {
        public bool Success { get; set; }
        public Guid PartyId { get; set; }
        public string DungeonId { get; set; } = string.Empty;
        public List<PartyCharacterData> Characters { get; set; } = new();
        public int RngSeed { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class PartyCharacterData
    {
        public string CharacterId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public CharacterData CharacterData { get; set; } = new();
    }

    public class SyncBattleResponse
    {
        public bool Success { get; set; }
        public Guid SnapshotId { get; set; }
        public bool IsVerified { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class BattleStatusResponse
    {
        public bool Success { get; set; }
        public Guid PartyId { get; set; }
        public PartyStatus Status { get; set; }
        public bool IsHosted { get; set; }
        public int CurrentWave { get; set; }
        public int TotalWaves { get; set; }
        public int ElapsedMs { get; set; }
        public int CompletionCount { get; set; }
        public List<PartyCharacterState> Characters { get; set; } = new();
        public List<string> RecentLogs { get; set; } = new();
        public int PendingRewardsCount { get; set; }
    }

    public class ClaimRewardsResponse
    {
        public bool Success { get; set; }
        public List<LootItem> Rewards { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class LootItem
    {
        public string ItemId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
```

## 7. DbContext 更新

```csharp
using Microsoft.EntityFrameworkCore;
using BlazorIdle.Shared.Models;

namespace BlazorIdle.Server.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
        {
        }

        // 现有表
        public DbSet<User> Users { get; set; }
        public DbSet<CharacterData> Characters { get; set; }
        public DbSet<GameState> GameStates { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        
        // 多人战斗新增表
        public DbSet<Party> Parties { get; set; }
        public DbSet<PartyMember> PartyMembers { get; set; }
        public DbSet<PartyBattleSnapshot> PartyBattleSnapshots { get; set; }
        public DbSet<PartyLootRecord> PartyLootRecords { get; set; }
        public DbSet<PartyMemberReward> PartyMemberRewards { get; set; }
        public DbSet<PartyInvitation> PartyInvitations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // 配置 Party 实体
            modelBuilder.Entity<Party>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PartyName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Status).HasConversion<string>();
                
                entity.HasIndex(e => e.LeaderId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.LastSyncAt)
                    .HasFilter("[Status] = 'Fighting'");
                
                entity.HasOne(e => e.Leader)
                    .WithMany()
                    .HasForeignKey(e => e.LeaderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            // 配置 PartyMember 实体
            modelBuilder.Entity<PartyMember>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).HasConversion<string>();
                entity.Property(e => e.Status).HasConversion<string>();
                
                entity.HasIndex(e => e.PartyId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.PartyId, e.UserId }).IsUnique();
                
                entity.HasOne(e => e.Party)
                    .WithMany(p => p.Members)
                    .HasForeignKey(e => e.PartyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // 其他实体配置...
        }
    }
}
```

## 8. 数据清理策略

### 8.1 自动清理规则

```csharp
public class PartyCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            
            // 清理24小时无活动的队伍
            await CleanupInactivePartiesAsync();
            
            // 清理过期的邀请
            await CleanupExpiredInvitationsAsync();
            
            // 清理已领取的奖励记录（保留7天）
            await CleanupOldRewardsAsync();
        }
    }
    
    private async Task CleanupInactivePartiesAsync()
    {
        var threshold = DateTime.UtcNow.AddHours(-24);
        var inactiveParties = await _context.Parties
            .Where(p => p.Status == PartyStatus.Fighting 
                     && p.LastSyncAt < threshold)
            .ToListAsync();
            
        foreach (var party in inactiveParties)
        {
            party.Status = PartyStatus.Disbanded;
            party.DisbandedAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
    }
}
```

## 9. 下一步

继续查看：
- 第三部分：队长端实现指南
- 第四部分：服务器端实现指南
- 第五部分：防作弊与安全策略
