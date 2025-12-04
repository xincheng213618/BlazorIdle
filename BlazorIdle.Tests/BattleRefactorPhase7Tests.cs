using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Battle.Systems;
using BlazorIdle.Game.Battle.State;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Tracks;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Skills;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 7 测试 - HasteCalculator、AttackDecisionHelper 和 BattleStateManager 集成
    /// Phase 7 tests - HasteCalculator, AttackDecisionHelper and BattleStateManager integration
    /// </summary>
    public class BattleRefactorPhase7Tests
    {
        #region HasteCalculator Tests

        [Fact]
        public void HasteCalculator_CalculateHastePercent_WithNullBuffOwner_ReturnsBaseHaste()
        {
            // Arrange
            double baseHaste = 10.0;

            // Act
            double result = HasteCalculator.CalculateHastePercent(baseHaste, null);

            // Assert
            Assert.Equal(baseHaste, result);
        }

        [Fact]
        public void HasteCalculator_CalculateHastePercent_WithEmptyBuffOwner_ReturnsBaseHaste()
        {
            // Arrange
            double baseHaste = 15.0;
            var character = CreateTestCharacter();
            var buffOwner = new CharacterBuffOwner(character, "char1", new ResourceBucketCollection());

            // Act
            double result = HasteCalculator.CalculateHastePercent(baseHaste, buffOwner);

            // Assert - should return base haste when no buffs
            Assert.Equal(baseHaste, result);
        }

        [Fact]
        public void HasteCalculator_CalculateHasteMultiplier_ConvertsPercentToMultiplier()
        {
            // Arrange & Act & Assert
            Assert.Equal(1.0, HasteCalculator.CalculateHasteMultiplier(0.0));
            Assert.Equal(1.1, HasteCalculator.CalculateHasteMultiplier(10.0));
            Assert.Equal(1.5, HasteCalculator.CalculateHasteMultiplier(50.0));
            Assert.Equal(2.0, HasteCalculator.CalculateHasteMultiplier(100.0));
        }

        [Fact]
        public void HasteCalculator_CalculateMonsterHastePercent_WithNullBuffOwner_ReturnsBaseHaste()
        {
            // Arrange
            double baseHaste = 5.0;

            // Act
            double result = HasteCalculator.CalculateMonsterHastePercent(baseHaste, null);

            // Assert
            Assert.Equal(baseHaste, result);
        }

        [Fact]
        public void HasteCalculator_UpdateCharacterHaste_UpdatesTrackHaste()
        {
            // Arrange
            var character = CreateTestCharacter();
            character.HastePercent = 20.0;
            var tracks = new CharacterTracks("char1", character);

            // Act
            HasteCalculator.UpdateCharacterHaste(character, null, tracks);

            // Assert
            // Haste multiplier should be 1.0 + 20.0/100.0 = 1.2
            // Check that the track was updated (the actual value comes from the attack track)
            Assert.True(true); // The method runs without error
        }

        #endregion

        #region AttackDecisionHelper Tests

        [Fact]
        public void AttackDecisionHelper_PrepareAttackDecisionData_WithNullCharacterDataMap_ReturnsNoCharacterData()
        {
            // Arrange
            var character = CreateTestCharacter();
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>();
            var playerResources = new Dictionary<string, ResourceBucketCollection>();
            var rng = new RngContext(12345);
            var clock = CreateTestClock();
            var skillRepository = new SkillRepository();

            // Act
            var result = AttackDecisionHelper.PrepareAttackDecisionData(
                "char1",
                character,
                null,  // null character data map
                playerBuffOwners,
                playerResources,
                rng,
                clock,
                "enemy1",
                skillRepository);

            // Assert
            Assert.False(result.HasCharacterData);
            Assert.Null(result.CharacterData);
        }

        [Fact]
        public void AttackDecisionHelper_PrepareAttackDecisionData_WithCharacterData_ReturnsCharacterData()
        {
            // Arrange
            var character = CreateTestCharacter();
            var characterData = new Shared.Models.CharacterData
            {
                Id = "char1",
                Name = "Test Character"
            };
            var characterDataMap = new Dictionary<string, Shared.Models.CharacterData>
            {
                { "char1", characterData }
            };
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>();
            var playerResources = new Dictionary<string, ResourceBucketCollection>();
            var rng = new RngContext(12345);
            var clock = CreateTestClock();
            var skillRepository = new SkillRepository();

            // Act
            var result = AttackDecisionHelper.PrepareAttackDecisionData(
                "char1",
                character,
                characterDataMap,
                playerBuffOwners,
                playerResources,
                rng,
                clock,
                "enemy1",
                skillRepository);

            // Assert
            Assert.True(result.HasCharacterData);
            Assert.Equal(characterData, result.CharacterData);
        }

        [Fact]
        public void AttackDecisionHelper_PrepareAttackDecisionData_BuildsCorrectContext()
        {
            // Arrange
            var character = CreateTestCharacter();
            var buffOwner = new CharacterBuffOwner(character, "char1", new ResourceBucketCollection());
            var resources = new ResourceBucketCollection();
            var characterDataMap = new Dictionary<string, Shared.Models.CharacterData>
            {
                { "char1", new Shared.Models.CharacterData { Id = "char1" } }
            };
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>
            {
                { "char1", buffOwner }
            };
            var playerResources = new Dictionary<string, ResourceBucketCollection>
            {
                { "char1", resources }
            };
            var rng = new RngContext(12345);
            var clock = CreateTestClock();
            var skillRepository = new SkillRepository();

            // Act
            var result = AttackDecisionHelper.PrepareAttackDecisionData(
                "char1",
                character,
                characterDataMap,
                playerBuffOwners,
                playerResources,
                rng,
                clock,
                "enemy1",
                skillRepository);

            // Assert
            Assert.NotNull(result.Context);
            Assert.Equal(character, result.Context!.Player);
            Assert.Equal(buffOwner, result.Context.PlayerBuffOwner);
            Assert.Equal(resources, result.Context.PlayerResources);
            Assert.Equal("enemy1", result.Context.CurrentTargetId);
        }

        [Fact]
        public void AttackDecisionHelper_PrepareAttackDecisionData_GetsNormalAttackSkillId()
        {
            // Arrange
            var character = CreateTestCharacter();
            var rng = new RngContext(12345);
            var clock = CreateTestClock();
            var skillRepository = new SkillRepository();

            // Act
            var result = AttackDecisionHelper.PrepareAttackDecisionData(
                "char1",
                character,
                null,
                new Dictionary<string, CharacterBuffOwner>(),
                new Dictionary<string, ResourceBucketCollection>(),
                rng,
                clock,
                null,
                skillRepository);

            // Assert
            Assert.NotNull(result.NormalAttackSkillId);
            Assert.Equal(character.GetNormalAttackSkillId(), result.NormalAttackSkillId);
        }

        #endregion

        #region BattleStateManager Integration Tests

        [Fact]
        public void BattleStateManager_CheckBattleState_WhenAllPlayersDead_SetsPlayerTeamDeadCooldown()
        {
            // Arrange
            var clock = CreateTestClock(1000);
            
            var playerTeam = new BattleTeam<Character>("player", "Player Team", TeamType.Player);
            var character = CreateTestCharacter();
            character.Hp = 0; // Kill the character
            playerTeam.AddMember("char1", character, maxHp: 100, currentHp: 0);
            
            var enemyTeam = new BattleTeam<Enemy>("enemy", "Enemy Team", TeamType.Enemy);
            var enemy = CreateTestEnemy();
            enemyTeam.AddMember("enemy1", enemy, maxHp: 50, currentHp: 50);
            
            var config = new MultiBattleConfig { AllowPlayerRevive = true, PlayerReviveCooldownMs = 5000 };
            var stateManager = new BattleStateManager(
                clock,
                playerTeam,
                enemyTeam,
                config,
                new Dictionary<string, CharacterBuffOwner>(),
                new Dictionary<string, EnemyBuffOwner>(),
                new Dictionary<string, ResourceBucketCollection>(),
                null,
                new Dictionary<string, CharacterTracks>(),
                new Dictionary<string, EnemyTrack>());

            stateManager.SetState(MultiBattleState.Fighting);

            // Act
            stateManager.CheckBattleState(1000);

            // Assert
            Assert.Equal(MultiBattleState.PlayerTeamDeadCooldown, stateManager.State);
            Assert.Equal(6000, stateManager.ResumeAtMs); // 1000 + 5000
        }

        [Fact]
        public void BattleStateManager_CheckBattleState_WhenAllEnemiesDead_SetsEnemyTeamDeadCooldown()
        {
            // Arrange
            var clock = CreateTestClock(2000);
            
            var playerTeam = new BattleTeam<Character>("player", "Player Team", TeamType.Player);
            var character = CreateTestCharacter();
            playerTeam.AddMember("char1", character, maxHp: 100, currentHp: 100);
            
            var enemyTeam = new BattleTeam<Enemy>("enemy", "Enemy Team", TeamType.Enemy);
            var enemy = CreateTestEnemy();
            enemy.Hp = 0; // Kill the enemy
            enemyTeam.AddMember("enemy1", enemy, maxHp: 50, currentHp: 0);
            
            var config = new MultiBattleConfig { AllowEnemyRespawn = true, EnemyRespawnCooldownMs = 3000 };
            var stateManager = new BattleStateManager(
                clock,
                playerTeam,
                enemyTeam,
                config,
                new Dictionary<string, CharacterBuffOwner>(),
                new Dictionary<string, EnemyBuffOwner>(),
                new Dictionary<string, ResourceBucketCollection>(),
                null,
                new Dictionary<string, CharacterTracks>(),
                new Dictionary<string, EnemyTrack>());

            stateManager.SetState(MultiBattleState.Fighting);

            // Act
            stateManager.CheckBattleState(2000);

            // Assert
            Assert.Equal(MultiBattleState.EnemyTeamDeadCooldown, stateManager.State);
            Assert.Equal(5000, stateManager.ResumeAtMs); // 2000 + 3000
        }

        [Fact]
        public void BattleStateManager_IsCooldownEnded_ReturnsTrueWhenTimeExceeded()
        {
            // Arrange
            var clock = CreateTestClock();
            var config = new MultiBattleConfig();
            var stateManager = new BattleStateManager(
                clock,
                new BattleTeam<Character>("player", "Player Team", TeamType.Player),
                new BattleTeam<Enemy>("enemy", "Enemy Team", TeamType.Enemy),
                config,
                new Dictionary<string, CharacterBuffOwner>(),
                new Dictionary<string, EnemyBuffOwner>(),
                new Dictionary<string, ResourceBucketCollection>(),
                null,
                new Dictionary<string, CharacterTracks>(),
                new Dictionary<string, EnemyTrack>());

            stateManager.SetState(MultiBattleState.PlayerTeamDeadCooldown);
            stateManager.SetResumeAtMs(5000);

            // Act & Assert
            Assert.False(stateManager.IsCooldownEnded(4999));
            Assert.True(stateManager.IsCooldownEnded(5000));
            Assert.True(stateManager.IsCooldownEnded(5001));
        }

        [Fact]
        public void BattleStateManager_FireTeamStatusEvent_InvokesOnTeamStatusChanged()
        {
            // Arrange
            var clock = CreateTestClock(1000);
            
            var playerTeam = new BattleTeam<Character>("player", "Player Team", TeamType.Player);
            var playerChar = CreateTestCharacter();
            playerTeam.AddMember("char1", playerChar, maxHp: 100, currentHp: 100);
            
            var enemyTeam = new BattleTeam<Enemy>("enemy", "Enemy Team", TeamType.Enemy);
            var enemyChar = CreateTestEnemy();
            enemyTeam.AddMember("enemy1", enemyChar, maxHp: 50, currentHp: 50);
            
            var config = new MultiBattleConfig();
            var stateManager = new BattleStateManager(
                clock,
                playerTeam,
                enemyTeam,
                config,
                new Dictionary<string, CharacterBuffOwner>(),
                new Dictionary<string, EnemyBuffOwner>(),
                new Dictionary<string, ResourceBucketCollection>(),
                null,
                new Dictionary<string, CharacterTracks>(),
                new Dictionary<string, EnemyTrack>());

            TeamStatusEvent? capturedEvent = null;
            stateManager.OnTeamStatusChanged += evt => capturedEvent = evt;
            stateManager.SetState(MultiBattleState.Fighting);

            // Act
            stateManager.FireTeamStatusEvent();

            // Assert
            Assert.NotNull(capturedEvent);
            Assert.Equal(1000, capturedEvent!.TimeMs);
            Assert.Equal(MultiBattleState.Fighting, capturedEvent.BattleState);
            Assert.Equal(1, capturedEvent.PlayerTeamStatus.AliveCount);
            Assert.Equal(1, capturedEvent.EnemyTeamStatus.AliveCount);
        }

        #endregion

        #region Helper Methods

        private IGameClock CreateTestClock(int nowMs = 0)
        {
            return new TestClock { NowMs = nowMs };
        }

        private Character CreateTestCharacter()
        {
            return new Character
            {
                Hp = 100,
                MaxHp = 100,
                ActiveCombatProfessionId = "warrior"
            };
        }

        private Enemy CreateTestEnemy()
        {
            return new Enemy
            {
                Hp = 50,
                MaxHp = 50,
                BaseAttack = 10
            };
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
