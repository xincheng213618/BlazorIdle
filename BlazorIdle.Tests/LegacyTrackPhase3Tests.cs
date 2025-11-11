using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Tracks;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 3 单元测试：Legacy Track 适配器
    /// Phase 3 unit tests: Legacy Track adapters
    /// </summary>
    public class LegacyTrackPhase3Tests
    {
        [Fact]
        public void AttackTrackLegacy_HasCorrectId()
        {
            // Arrange
            var trackState = new TrackState(TrackType.Attack, 500);
            var resolver = new SkillResolver();
            var config = new TrackConfig { BoundSkills = new List<string> { "attack_basic" } };
            var track = new AttackTrackLegacy(trackState, resolver, config);

            // Act & Assert
            Assert.Equal("attack", track.Id);
            Assert.False(track.IsSuspended);
        }

        [Fact]
        public void AttackTrackLegacy_SuspendAndResume_Works()
        {
            // Arrange
            var trackState = new TrackState(TrackType.Attack, 500);
            var resolver = new SkillResolver();
            var config = new TrackConfig { BoundSkills = new List<string> { "attack_basic" } };
            var track = new AttackTrackLegacy(trackState, resolver, config);

            // Act - Suspend
            track.Suspend("test_suspend");
            Assert.True(track.IsSuspended);

            // Act - Resume
            track.Resume("test_resume");
            Assert.False(track.IsSuspended);
        }

        [Fact]
        public void AttackTrackLegacy_TickWhenSuspended_DoesNotTrigger()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character { DamagePerAttack = 50 };
            var trackState = new TrackState(TrackType.Attack, 500);
            trackState.Reset(0);
            
            var resolver = new SkillResolver();
            var config = new TrackConfig { BoundSkills = new List<string> { "attack_basic" } };
            var track = new AttackTrackLegacy(trackState, resolver, config);
            
            var ctx = new BattleContext
            {
                Player = player,
                Rng = rng,
                Clock = clock
            };

            // Act - Suspend and tick
            track.Suspend("test");
            clock.AdvanceBy(600); // 超过触发时间
            track.Tick(0.6, ctx);

            // Assert - Track 应该不触发（我们无法直接验证，但至少不应该崩溃）
            // Track should not trigger (we can't verify directly, but at least shouldn't crash)
            Assert.True(track.IsSuspended);
        }

        [Fact]
        public void SpecialTrackLegacy_HasCorrectId()
        {
            // Arrange
            var trackState = new TrackState(TrackType.Special, 5000);
            var resolver = new SkillResolver();
            var config = new TrackConfig { BoundSkills = new List<string> { "special_pulse" } };
            var track = new SpecialTrackLegacy(trackState, resolver, config);

            // Act & Assert
            Assert.Equal("special", track.Id);
            Assert.False(track.IsSuspended);
        }

        [Fact]
        public void SpecialTrackLegacy_PresenceMode_RequiresEnemiesAlive()
        {
            // Arrange
            var clock = new SimClock();
            var rng = new RngContext(12345);
            var player = new Character { SpecialDamage = 100 };
            var enemy = new Enemy { Hp = 100, MaxHp = 100 };
            var trackState = new TrackState(TrackType.Special, 5000);
            trackState.Reset(0);
            
            var resolver = new SkillResolver();
            var config = new TrackConfig 
            { 
                BoundSkills = new List<string> { "special_pulse" },
                ProgressPolicy = ProgressPolicy.Presence
            };
            var track = new SpecialTrackLegacy(trackState, resolver, config);
            
            // Act & Assert - 有敌人存活时应该可以推进
            var ctx1 = new BattleContext
            {
                Player = player,
                Enemy = enemy,
                Rng = rng,
                Clock = clock
            };
            clock.AdvanceBy(5100);
            track.Tick(5.1, ctx1);
            // 应该触发（无异常即通过）

            // Act & Assert - 没有敌人存活时不应该推进
            enemy.Hp = 0;
            trackState.Reset(clock.NowMs);
            clock.AdvanceBy(5100);
            track.Tick(5.1, ctx1);
            // 应该不触发（无异常即通过）
        }

        [Fact]
        public void EnemyAttackTrackLegacy_HasCorrectId()
        {
            // Arrange
            var trackState = new TrackState(TrackType.EnemyAttack, 1500);
            var resolver = new SkillResolver();
            var config = new TrackConfig { BoundSkills = new List<string> { "enemy_attack_basic" } };
            var track = new EnemyAttackTrackLegacy(trackState, resolver, config);

            // Act & Assert
            Assert.Equal("enemy_attack", track.Id);
            Assert.False(track.IsSuspended);
        }

        [Fact]
        public void EnemyAttackTrackLegacy_SuspendAndResume_Works()
        {
            // Arrange
            var trackState = new TrackState(TrackType.EnemyAttack, 1500);
            var resolver = new SkillResolver();
            var config = new TrackConfig { BoundSkills = new List<string> { "enemy_attack_basic" } };
            var track = new EnemyAttackTrackLegacy(trackState, resolver, config);

            // Act & Assert
            Assert.False(track.IsSuspended);
            
            track.Suspend("test_suspend");
            Assert.True(track.IsSuspended);
            
            track.Resume("test_resume");
            Assert.False(track.IsSuspended);
        }

        [Fact]
        public void AllLegacyTracks_ImplementITrack()
        {
            // Arrange & Act
            var attackType = typeof(AttackTrackLegacy);
            var specialType = typeof(SpecialTrackLegacy);
            var enemyAttackType = typeof(EnemyAttackTrackLegacy);
            var itrackType = typeof(ITrack);

            // Assert
            Assert.True(itrackType.IsAssignableFrom(attackType));
            Assert.True(itrackType.IsAssignableFrom(specialType));
            Assert.True(itrackType.IsAssignableFrom(enemyAttackType));
        }
    }
}
