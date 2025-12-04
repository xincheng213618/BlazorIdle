using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Battle.Events;
using BlazorIdle.Game.Battle.State;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Config;
using BlazorIdle.Game.Tracks;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 核心战斗系统重构 Phase 4 测试 - 事件与状态管理
    /// Core Battle System Refactoring Phase 4 Tests - Event and State Management
    /// </summary>
    public class BattleRefactorPhase4Tests
    {
        #region CombatEventRecorder Tests

        [Fact]
        public void CombatEventRecorder_RecordResourceGain_FiresEvent()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var config = new CombatConfig { EmitCastEvents = true };
            var aggregator = new SegmentAggregator(new SegmentAggregatorOptions { MaxEvents = 20, MaxDurationMs = 3000 });
            var segments = new List<CombatSegment>();
            
            var recorder = new CombatEventRecorder(clock, rng, config, aggregator, segments);
            
            // Act
            recorder.RecordResourceGain("player1", "rage", 10, 20, "attack_hit", "warrior_strike");
            
            // Assert - Event should be added to aggregator (but not flushed yet since we haven't hit limits)
            Assert.Empty(segments); // No flush yet
        }

        [Fact]
        public void CombatEventRecorder_RecordBuffApply_FiresEventAndCallback()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var config = new CombatConfig { EmitCastEvents = true };
            var aggregator = new SegmentAggregator(new SegmentAggregatorOptions { MaxEvents = 20, MaxDurationMs = 3000 });
            var segments = new List<CombatSegment>();
            
            var recorder = new CombatEventRecorder(clock, rng, config, aggregator, segments);
            
            BuffApplyEvent? receivedEvent = null;
            recorder.OnBuffApplied += evt => receivedEvent = evt;
            
            // Act
            recorder.RecordBuffApply("player1", "buff_berserk", BuffKind.Buff, 10.0, 1, "AttackMultiplier=1.2", "warrior_rage");
            
            // Assert
            Assert.NotNull(receivedEvent);
            Assert.Equal("player1", receivedEvent!.OwnerId);
            Assert.Equal("buff_berserk", receivedEvent.BuffId);
            Assert.Equal(BuffKind.Buff, receivedEvent.Kind);
        }

        [Fact]
        public void CombatEventRecorder_RecordBuffRemove_FiresEventAndCallback()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var config = new CombatConfig { EmitCastEvents = true };
            var aggregator = new SegmentAggregator(new SegmentAggregatorOptions { MaxEvents = 20, MaxDurationMs = 3000 });
            var segments = new List<CombatSegment>();
            
            var recorder = new CombatEventRecorder(clock, rng, config, aggregator, segments);
            
            BuffRemoveEvent? receivedEvent = null;
            recorder.OnBuffRemoved += evt => receivedEvent = evt;
            
            // Act
            recorder.RecordBuffRemove("player1", "buff_berserk", "expired");
            
            // Assert
            Assert.NotNull(receivedEvent);
            Assert.Equal("player1", receivedEvent!.OwnerId);
            Assert.Equal("buff_berserk", receivedEvent.BuffId);
            Assert.Equal("expired", receivedEvent.Reason);
        }

        [Fact]
        public void CombatEventRecorder_RecordBuffTick_FiresEventAndCallback()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var config = new CombatConfig { EmitCastEvents = true };
            var aggregator = new SegmentAggregator(new SegmentAggregatorOptions { MaxEvents = 20, MaxDurationMs = 3000 });
            var segments = new List<CombatSegment>();
            
            var recorder = new CombatEventRecorder(clock, rng, config, aggregator, segments);
            
            BuffTickEvent? receivedEvent = null;
            recorder.OnBuffTicked += evt => receivedEvent = evt;
            
            // Act
            recorder.RecordBuffTick("player1", "buff_dot", BuffTickType.DamageOverTime, 50, 150);
            
            // Assert
            Assert.NotNull(receivedEvent);
            Assert.Equal("player1", receivedEvent!.OwnerId);
            Assert.Equal("buff_dot", receivedEvent.BuffId);
            Assert.Equal(BuffTickType.DamageOverTime, receivedEvent.TickType);
            Assert.Equal(50, receivedEvent.Amount);
        }

        [Fact]
        public void CombatEventRecorder_RecordHeal_FiresEventAndCallback()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var config = new CombatConfig { EmitCastEvents = true };
            var aggregator = new SegmentAggregator(new SegmentAggregatorOptions { MaxEvents = 20, MaxDurationMs = 3000 });
            var segments = new List<CombatSegment>();
            
            var recorder = new CombatEventRecorder(clock, rng, config, aggregator, segments);
            
            HealEvent? receivedEvent = null;
            recorder.OnHealed += evt => receivedEvent = evt;
            
            // Act
            recorder.RecordHeal("player1", 100, 200, "instant_heal");
            
            // Assert
            Assert.NotNull(receivedEvent);
            Assert.Equal("player1", receivedEvent!.OwnerId);
            Assert.Equal(100, receivedEvent.Amount);
            Assert.Equal(200, receivedEvent.ResultingHp);
        }

        [Fact]
        public void CombatEventRecorder_RecordCastStart_FiresEventCallback()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var config = new CombatConfig { EmitCastEvents = true };
            var aggregator = new SegmentAggregator(new SegmentAggregatorOptions { MaxEvents = 20, MaxDurationMs = 3000 });
            var segments = new List<CombatSegment>();
            
            var recorder = new CombatEventRecorder(clock, rng, config, aggregator, segments);
            
            CastStartEvent? receivedEvent = null;
            recorder.OnCastStarted += evt => receivedEvent = evt;
            
            // Act
            recorder.RecordCastStart("player1", "fireball", 2.5, true);
            
            // Assert
            Assert.NotNull(receivedEvent);
            Assert.Equal("player1", receivedEvent!.CasterId);
            Assert.Equal("fireball", receivedEvent.SkillId);
            Assert.Equal(2.5, receivedEvent.CastTimeSec);
            Assert.True(receivedEvent.PauseAttackTrack);
        }

        [Fact]
        public void CombatEventRecorder_RecordCastComplete_FiresEventCallback()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var config = new CombatConfig { EmitCastEvents = true };
            var aggregator = new SegmentAggregator(new SegmentAggregatorOptions { MaxEvents = 20, MaxDurationMs = 3000 });
            var segments = new List<CombatSegment>();
            
            var recorder = new CombatEventRecorder(clock, rng, config, aggregator, segments);
            
            CastCompleteEvent? receivedEvent = null;
            recorder.OnCastCompleted += evt => receivedEvent = evt;
            
            // Act
            recorder.RecordCastComplete("player1", "fireball", 2.3);
            
            // Assert
            Assert.NotNull(receivedEvent);
            Assert.Equal("player1", receivedEvent!.CasterId);
            Assert.Equal("fireball", receivedEvent.SkillId);
            Assert.Equal(2.3, receivedEvent.ActualCastTimeSec);
        }

        [Fact]
        public void CombatEventRecorder_RecordCastInterrupt_FiresEventCallback()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var config = new CombatConfig { EmitCastEvents = true };
            var aggregator = new SegmentAggregator(new SegmentAggregatorOptions { MaxEvents = 20, MaxDurationMs = 3000 });
            var segments = new List<CombatSegment>();
            
            var recorder = new CombatEventRecorder(clock, rng, config, aggregator, segments);
            
            CastInterruptEvent? receivedEvent = null;
            recorder.OnCastInterrupted += evt => receivedEvent = evt;
            
            // Act
            recorder.RecordCastInterrupt("player1", "fireball", "target_dead", 1.5);
            
            // Assert
            Assert.NotNull(receivedEvent);
            Assert.Equal("player1", receivedEvent!.CasterId);
            Assert.Equal("fireball", receivedEvent.SkillId);
            Assert.Equal("target_dead", receivedEvent.Reason);
            Assert.Equal(1.5, receivedEvent.ElapsedSec);
        }

        [Fact]
        public void CombatEventRecorder_DisabledEmitEvents_DoesNotFireCallbacks()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var config = new CombatConfig { EmitCastEvents = false };
            var aggregator = new SegmentAggregator(new SegmentAggregatorOptions { MaxEvents = 20, MaxDurationMs = 3000 });
            var segments = new List<CombatSegment>();
            
            var recorder = new CombatEventRecorder(clock, rng, config, aggregator, segments);
            
            bool eventFired = false;
            recorder.OnBuffApplied += _ => eventFired = true;
            
            // Act
            recorder.RecordBuffApply("player1", "buff_test", BuffKind.Buff, 5.0, 1, "test");
            
            // Assert
            Assert.False(eventFired);
        }

        #endregion

        #region BattleStateManager Tests

        [Fact]
        public void BattleStateManager_CheckBattleState_DetectsPlayerDefeat()
        {
            // Arrange
            var config = new MultiBattleConfig { AllowPlayerRevive = false };
            var stateManager = CreateTestStateManager(out var playerTeam, out var enemyTeam, config);
            
            // Kill all players by dealing full damage
            foreach (var member in playerTeam.Members)
            {
                member.TakeDamage(member.MaxHp);
            }
            
            bool stopRequested = false;
            stateManager.OnStopRequested += () => stopRequested = true;
            
            // Act
            stateManager.CheckBattleState(1000);
            
            // Assert
            Assert.Equal(MultiBattleState.Defeat, stateManager.State);
            Assert.True(stopRequested);
        }

        [Fact]
        public void BattleStateManager_CheckBattleState_DetectsVictory()
        {
            // Arrange
            var config = new MultiBattleConfig { AllowEnemyRespawn = false };
            var stateManager = CreateTestStateManager(out var playerTeam, out var enemyTeam, config);
            
            // Kill all enemies by dealing full damage
            foreach (var member in enemyTeam.Members)
            {
                member.TakeDamage(member.MaxHp);
            }
            
            bool stopRequested = false;
            stateManager.OnStopRequested += () => stopRequested = true;
            
            // Act
            stateManager.CheckBattleState(1000);
            
            // Assert
            Assert.Equal(MultiBattleState.Victory, stateManager.State);
            Assert.True(stopRequested);
        }

        [Fact]
        public void BattleStateManager_CheckBattleState_EntersPlayerCooldown()
        {
            // Arrange
            var config = new MultiBattleConfig { AllowPlayerRevive = true, PlayerReviveCooldownMs = 5000 };
            var stateManager = CreateTestStateManager(out var playerTeam, out var enemyTeam, config);
            
            // Kill all players by dealing full damage
            foreach (var member in playerTeam.Members)
            {
                member.TakeDamage(member.MaxHp);
            }
            
            // Act
            stateManager.CheckBattleState(1000);
            
            // Assert
            Assert.Equal(MultiBattleState.PlayerTeamDeadCooldown, stateManager.State);
            Assert.Equal(6000, stateManager.ResumeAtMs); // 1000 + 5000
        }

        [Fact]
        public void BattleStateManager_HandleCooldownEnd_RevivesPlayers()
        {
            // Arrange
            var config = new MultiBattleConfig { AllowPlayerRevive = true, ReviveWithFullHp = true };
            var stateManager = CreateTestStateManager(out var playerTeam, out var enemyTeam, config);
            
            // Kill all players by dealing full damage
            foreach (var member in playerTeam.Members)
            {
                member.TakeDamage(member.MaxHp);
            }
            
            stateManager.SetState(MultiBattleState.PlayerTeamDeadCooldown);
            
            // Act
            stateManager.HandleCooldownEnd(6000);
            
            // Assert
            Assert.Equal(MultiBattleState.Fighting, stateManager.State);
            foreach (var member in playerTeam.Members)
            {
                Assert.Equal(member.MaxHp, member.CurrentHp); // Check BattleMember HP, not Entity.Hp
            }
        }

        [Fact]
        public void BattleStateManager_IsCooldownEnded_ReturnsTrueWhenTimeReached()
        {
            // Arrange
            var stateManager = CreateTestStateManager(out _, out _);
            stateManager.SetState(MultiBattleState.PlayerTeamDeadCooldown);
            stateManager.SetResumeAtMs(5000);
            
            // Act & Assert
            Assert.False(stateManager.IsCooldownEnded(4000));
            Assert.True(stateManager.IsCooldownEnded(5000));
            Assert.True(stateManager.IsCooldownEnded(6000));
        }

        [Fact]
        public void BattleStateManager_FireTeamStatusEvent_SendsCorrectData()
        {
            // Arrange
            var stateManager = CreateTestStateManager(out var playerTeam, out var enemyTeam);
            
            TeamStatusEvent? receivedEvent = null;
            stateManager.OnTeamStatusChanged += evt => receivedEvent = evt;
            
            // Act
            stateManager.FireTeamStatusEvent();
            
            // Assert
            Assert.NotNull(receivedEvent);
            Assert.Equal(playerTeam.AliveCount, receivedEvent!.PlayerTeamStatus.AliveCount);
            Assert.Equal(enemyTeam.AliveCount, receivedEvent.EnemyTeamStatus.AliveCount);
        }

        [Fact]
        public void BattleStateManager_StateProperties_ReturnCorrectValues()
        {
            // Arrange
            var stateManager = CreateTestStateManager(out _, out _);
            
            // Act & Assert - Fighting
            stateManager.SetState(MultiBattleState.Fighting);
            Assert.True(stateManager.IsFighting);
            Assert.False(stateManager.IsEnded);
            Assert.False(stateManager.IsInCooldown);
            
            // Act & Assert - Victory
            stateManager.SetState(MultiBattleState.Victory);
            Assert.False(stateManager.IsFighting);
            Assert.True(stateManager.IsEnded);
            Assert.False(stateManager.IsInCooldown);
            
            // Act & Assert - Cooldown
            stateManager.SetState(MultiBattleState.PlayerTeamDeadCooldown);
            Assert.False(stateManager.IsFighting);
            Assert.False(stateManager.IsEnded);
            Assert.True(stateManager.IsInCooldown);
        }

        #endregion

        #region Helper Methods

        private BattleStateManager CreateTestStateManager(
            out BattleTeam<Character> playerTeam,
            out BattleTeam<Enemy> enemyTeam,
            MultiBattleConfig? config = null)
        {
            var clock = new TestClock();
            config ??= new MultiBattleConfig();
            
            var player = new Character { Hp = 100, MaxHp = 100, ActiveCombatProfessionId = "warrior" };
            playerTeam = new BattleTeam<Character>("players", "PlayerTeam", TeamType.Player);
            playerTeam.AddMember("player1", player, maxHp: 100, currentHp: 100);
            
            var enemy = new Enemy { Hp = 50, MaxHp = 50, BaseAttack = 10 };
            enemyTeam = new BattleTeam<Enemy>("enemies", "EnemyTeam", TeamType.Enemy);
            enemyTeam.AddMember("enemy1", enemy, maxHp: 50, currentHp: 50);
            
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>
            {
                ["player1"] = new CharacterBuffOwner(player, "player1", new ResourceBucketCollection())
            };
            
            var enemyBuffOwners = new Dictionary<string, EnemyBuffOwner>
            {
                ["enemy1"] = new EnemyBuffOwner(enemy, "enemy1")
            };
            
            var playerResources = new Dictionary<string, ResourceBucketCollection>
            {
                ["player1"] = new ResourceBucketCollection()
            };
            
            var characterTracks = new Dictionary<string, CharacterTracks>
            {
                ["player1"] = new CharacterTracks("player1", player)
            };
            
            var enemyTracks = new Dictionary<string, EnemyTrack>
            {
                ["enemy1"] = new EnemyTrack("enemy1", enemy)
            };
            
            return new BattleStateManager(
                clock,
                playerTeam,
                enemyTeam,
                config,
                playerBuffOwners,
                enemyBuffOwners,
                playerResources,
                null,
                characterTracks,
                enemyTracks
            );
        }

        private class TestClock : IGameClock
        {
            public int NowMs { get; set; } = 0;
            public double NowSec => NowMs / 1000.0;
            public void Reset() => NowMs = 0;
            public void AdvanceBy(int ms) => NowMs += ms;
        }

        #endregion
    }
}
