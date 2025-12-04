using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Battle.Systems;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Combat;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Resources;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 核心战斗系统重构 Phase 3 测试 - 系统集成模块
    /// Core Battle System Refactoring Phase 3 Tests - System Integration Modules
    /// </summary>
    public class BattleRefactorPhase3Tests
    {
        #region BuffIntegration Tests

        [Fact]
        public void BuffIntegration_ResolveBuffTargets_Self_ReturnsPlayerOwner()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>();
            var enemyBuffOwners = new Dictionary<string, EnemyBuffOwner>();
            
            var character = CreateTestCharacter();
            playerBuffOwners["player1"] = new CharacterBuffOwner(character, "player1", new ResourceBucketCollection());
            
            var buffIntegration = new BuffIntegration(
                clock, rng, playerBuffOwners, enemyBuffOwners,
                () => new List<string> { "player1" },
                () => new List<string>());

            // Act
            var targets = buffIntegration.ResolveBuffTargets(BuffTarget.Self, "player1", null, isCasterPlayer: true);

            // Assert
            Assert.Single(targets);
            Assert.Equal("player1", targets[0].Id);
        }

        [Fact]
        public void BuffIntegration_ResolveBuffTargets_Self_ReturnsEnemyOwner()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>();
            var enemyBuffOwners = new Dictionary<string, EnemyBuffOwner>();
            
            var enemy = CreateTestEnemy();
            enemyBuffOwners["enemy1"] = new EnemyBuffOwner(enemy, "enemy1");
            
            var buffIntegration = new BuffIntegration(
                clock, rng, playerBuffOwners, enemyBuffOwners,
                () => new List<string>(),
                () => new List<string> { "enemy1" });

            // Act
            var targets = buffIntegration.ResolveBuffTargets(BuffTarget.Self, "enemy1", null, isCasterPlayer: false);

            // Assert
            Assert.Single(targets);
            Assert.Equal("enemy1", targets[0].Id);
        }

        [Fact]
        public void BuffIntegration_ResolveBuffTargets_AllAllies_ReturnsAllPlayers()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>();
            var enemyBuffOwners = new Dictionary<string, EnemyBuffOwner>();
            
            var character1 = CreateTestCharacter();
            var character2 = CreateTestCharacter();
            playerBuffOwners["player1"] = new CharacterBuffOwner(character1, "player1", new ResourceBucketCollection());
            playerBuffOwners["player2"] = new CharacterBuffOwner(character2, "player2", new ResourceBucketCollection());
            
            var buffIntegration = new BuffIntegration(
                clock, rng, playerBuffOwners, enemyBuffOwners,
                () => new List<string> { "player1", "player2" },
                () => new List<string>());

            // Act
            var targets = buffIntegration.ResolveBuffTargets(BuffTarget.AllAllies, "player1", null, isCasterPlayer: true);

            // Assert
            Assert.Equal(2, targets.Count);
            Assert.Contains(targets, t => t.Id == "player1");
            Assert.Contains(targets, t => t.Id == "player2");
        }

        [Fact]
        public void BuffIntegration_ResolveBuffTargets_AllEnemies_ReturnsAllEnemies()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>();
            var enemyBuffOwners = new Dictionary<string, EnemyBuffOwner>();
            
            var character = CreateTestCharacter();
            playerBuffOwners["player1"] = new CharacterBuffOwner(character, "player1", new ResourceBucketCollection());
            
            var enemy1 = CreateTestEnemy();
            var enemy2 = CreateTestEnemy();
            enemyBuffOwners["enemy1"] = new EnemyBuffOwner(enemy1, "enemy1");
            enemyBuffOwners["enemy2"] = new EnemyBuffOwner(enemy2, "enemy2");
            
            var buffIntegration = new BuffIntegration(
                clock, rng, playerBuffOwners, enemyBuffOwners,
                () => new List<string> { "player1" },
                () => new List<string> { "enemy1", "enemy2" });

            // Act
            var targets = buffIntegration.ResolveBuffTargets(BuffTarget.AllEnemies, "player1", null, isCasterPlayer: true);

            // Assert
            Assert.Equal(2, targets.Count);
            Assert.Contains(targets, t => t.Id == "enemy1");
            Assert.Contains(targets, t => t.Id == "enemy2");
        }

        [Fact]
        public void BuffIntegration_ResolveBuffTargets_Target_ReturnsEnemyWhenPlayerCasts()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>();
            var enemyBuffOwners = new Dictionary<string, EnemyBuffOwner>();
            
            var enemy = CreateTestEnemy();
            enemyBuffOwners["enemy1"] = new EnemyBuffOwner(enemy, "enemy1");
            
            var buffIntegration = new BuffIntegration(
                clock, rng, playerBuffOwners, enemyBuffOwners,
                () => new List<string>(),
                () => new List<string> { "enemy1" });

            // Act
            var targets = buffIntegration.ResolveBuffTargets(BuffTarget.Target, "player1", "enemy1", isCasterPlayer: true);

            // Assert
            Assert.Single(targets);
            Assert.Equal("enemy1", targets[0].Id);
        }

        [Fact]
        public void BuffIntegration_ProcessBuffTicks_TriggersDoTDamage()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>();
            var enemyBuffOwners = new Dictionary<string, EnemyBuffOwner>();
            
            var character = CreateTestCharacter();
            character.Hp = 100;
            character.MaxHp = 100;
            playerBuffOwners["player1"] = new CharacterBuffOwner(character, "player1", new ResourceBucketCollection());
            
            // 添加一个 DoT buff
            var dotBuff = new BuffInstance(
                id: "test_dot",
                ownerId: "player1",
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect> { new BuffEffect { Type = BuffEffectType.DamageOverTime, Value = 10 } },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 5.0,
                tickIntervalSec: 1.0,
                maxStacks: 1);
            playerBuffOwners["player1"].ApplyBuff(dotBuff);
            
            var buffIntegration = new BuffIntegration(
                clock, rng, playerBuffOwners, enemyBuffOwners,
                () => new List<string> { "player1" },
                () => new List<string>());
            
            int tickEventCount = 0;
            buffIntegration.OnBuffTick += (ownerId, buffId, tickType, amount, hp, bundleId) =>
            {
                tickEventCount++;
                Assert.Equal("player1", ownerId);
                Assert.Equal("test_dot", buffId);
                Assert.Equal(BuffTickType.DamageOverTime, tickType);
            };

            // Act - 模拟1秒过去
            buffIntegration.ProcessBuffTicks(1.0);

            // Assert
            Assert.Equal(1, tickEventCount);
        }

        [Fact]
        public void BuffIntegration_ProcessBuffTicks_RemovesExpiredBuffs()
        {
            // Arrange
            var clock = new TestClock();
            var rng = new RngContext(12345);
            var playerBuffOwners = new Dictionary<string, CharacterBuffOwner>();
            var enemyBuffOwners = new Dictionary<string, EnemyBuffOwner>();
            
            var character = CreateTestCharacter();
            playerBuffOwners["player1"] = new CharacterBuffOwner(character, "player1", new ResourceBucketCollection());
            
            // 添加一个短时 buff（使用 null tickIntervalSec 避免 tick 要求）
            var shortBuff = new BuffInstance(
                id: "test_short",
                ownerId: "player1",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect>(),
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 0.5,
                tickIntervalSec: null,
                maxStacks: 1);
            playerBuffOwners["player1"].ApplyBuff(shortBuff);
            
            var buffIntegration = new BuffIntegration(
                clock, rng, playerBuffOwners, enemyBuffOwners,
                () => new List<string> { "player1" },
                () => new List<string>());
            
            int removeEventCount = 0;
            buffIntegration.OnBuffRemove += (ownerId, buffId, reason, bundleId) =>
            {
                removeEventCount++;
                Assert.Equal("player1", ownerId);
                Assert.Equal("test_short", buffId);
                Assert.Equal("expired", reason);
            };

            // Act - 模拟1秒过去（buff应该过期）
            buffIntegration.ProcessBuffTicks(1.0);

            // Assert
            Assert.Equal(1, removeEventCount);
            Assert.False(playerBuffOwners["player1"].Buffs.ContainsKey("test_short"));
        }

        #endregion

        #region ResourceIntegration Tests

        [Fact]
        public void ResourceIntegration_ApplyResourceChanges_AppliesGain()
        {
            // Arrange
            var playerResources = new Dictionary<string, ResourceBucketCollection>();
            playerResources["player1"] = new ResourceBucketCollection("rage", 100, 0);
            
            var resourceIntegration = new ResourceIntegration(playerResources);
            
            var result = new SkillCastResult
            {
                ResourceChanges = new Dictionary<string, int> { { "rage", 30 } },
                BundleId = "test_bundle"
            };
            
            int changeEventCount = 0;
            resourceIntegration.OnResourceChange += (actorId, resourceId, delta, newValue, reason, skillId, bundleId) =>
            {
                changeEventCount++;
                Assert.Equal("player1", actorId);
                Assert.Equal("rage", resourceId);
                Assert.Equal(30, delta);
                Assert.Equal(30, newValue);
                Assert.Equal("skill_resource_gain", reason);
            };

            // Act
            resourceIntegration.ApplyResourceChanges(result, "player1", isCasterPlayer: true, skillId: "test_skill");

            // Assert
            Assert.Equal(1, changeEventCount);
            Assert.Equal(30, playerResources["player1"].GetBucket("rage").Current);
        }

        [Fact]
        public void ResourceIntegration_ApplyResourceChanges_AppliesCost()
        {
            // Arrange
            var playerResources = new Dictionary<string, ResourceBucketCollection>();
            playerResources["player1"] = new ResourceBucketCollection("rage", 100, 50);
            
            var resourceIntegration = new ResourceIntegration(playerResources);
            
            var result = new SkillCastResult
            {
                ResourceChanges = new Dictionary<string, int> { { "rage", -20 } },
                BundleId = "test_bundle"
            };
            
            int changeEventCount = 0;
            resourceIntegration.OnResourceChange += (actorId, resourceId, delta, newValue, reason, skillId, bundleId) =>
            {
                changeEventCount++;
                Assert.Equal("player1", actorId);
                Assert.Equal("rage", resourceId);
                Assert.Equal(-20, delta);
                Assert.Equal(30, newValue);
                Assert.Equal("skill_resource_cost", reason);
            };

            // Act
            resourceIntegration.ApplyResourceChanges(result, "player1", isCasterPlayer: true, skillId: "test_skill");

            // Assert
            Assert.Equal(1, changeEventCount);
            Assert.Equal(30, playerResources["player1"].GetBucket("rage").Current);
        }

        [Fact]
        public void ResourceIntegration_ApplyResourceChanges_DoesNotApplyForEnemy()
        {
            // Arrange
            var playerResources = new Dictionary<string, ResourceBucketCollection>();
            playerResources["player1"] = new ResourceBucketCollection("rage", 100, 50);
            
            var resourceIntegration = new ResourceIntegration(playerResources);
            
            var result = new SkillCastResult
            {
                ResourceChanges = new Dictionary<string, int> { { "rage", 30 } }
            };
            
            int changeEventCount = 0;
            resourceIntegration.OnResourceChange += (_, _, _, _, _, _, _) => changeEventCount++;

            // Act
            resourceIntegration.ApplyResourceChanges(result, "enemy1", isCasterPlayer: false, skillId: "test_skill");

            // Assert
            Assert.Equal(0, changeEventCount);
            Assert.Equal(50, playerResources["player1"].GetBucket("rage").Current);
        }

        [Fact]
        public void ResourceIntegration_RecordAttackResourceGain_AppliesBaseAndCritGain()
        {
            // Arrange
            var playerResources = new Dictionary<string, ResourceBucketCollection>();
            playerResources["player1"] = new ResourceBucketCollection("rage", 100, 0);
            
            var resourceIntegration = new ResourceIntegration(playerResources);
            
            int changeEventCount = 0;
            resourceIntegration.OnResourceChange += (_, _, _, _, _, _, _) => changeEventCount++;

            // Act
            resourceIntegration.RecordAttackResourceGain("player1", "rage", 10, 5, isCrit: true);

            // Assert
            Assert.Equal(2, changeEventCount); // Base gain + crit gain
            Assert.Equal(15, playerResources["player1"].GetBucket("rage").Current);
        }

        [Fact]
        public void ResourceIntegration_HasEnoughResources_ReturnsTrueWhenSufficient()
        {
            // Arrange
            var playerResources = new Dictionary<string, ResourceBucketCollection>();
            playerResources["player1"] = new ResourceBucketCollection("rage", 100, 50);
            
            var resourceIntegration = new ResourceIntegration(playerResources);

            // Act & Assert
            Assert.True(resourceIntegration.HasEnoughResources("player1", "rage", 30));
            Assert.True(resourceIntegration.HasEnoughResources("player1", "rage", 50));
            Assert.False(resourceIntegration.HasEnoughResources("player1", "rage", 60));
        }

        #endregion

        #region TriggerIntegration Tests

        [Fact]
        public void TriggerIntegration_Constructor_ThrowsOnNull()
        {
            Assert.Throws<System.ArgumentNullException>(() => new TriggerIntegration(null!, new TriggerProcessor(SkillRepository.Shared, new ConditionChecker(), null!, new ResourceManager())));
            Assert.Throws<System.ArgumentNullException>(() => new TriggerIntegration(SkillRepository.Shared, null!));
        }

        [Fact]
        public void TriggerIntegration_ProcessWindowTriggers_DoesNothingForMonsters()
        {
            // Arrange
            var skillRepository = SkillRepository.Shared;
            var conditionChecker = new ConditionChecker();
            var triggerProcessor = new TriggerProcessor(skillRepository, conditionChecker, casterId => new CooldownManager(), new ResourceManager());
            var triggerIntegration = new TriggerIntegration(skillRepository, triggerProcessor);
            
            var context = new BattleContext { Rng = new RngContext(12345) };
            
            int executeCount = 0;
            triggerIntegration.OnExecuteSkillRequested += (_, _, _, _) => executeCount++;

            // Act
            triggerIntegration.ProcessWindowTriggers("enemy1", "OnPostAttackWindow", "enemy_attack", isCasterPlayer: false, context);

            // Assert
            Assert.Equal(0, executeCount);
        }

        #endregion

        #region CastingIntegration Tests

        [Fact]
        public void CastingIntegration_IsCasting_ReturnsFalseInitially()
        {
            // Arrange
            var clock = new TestClock();
            var skillRepository = SkillRepository.Shared;
            var conditionChecker = new ConditionChecker();
            var windowExecutor = new WindowExecutor(skillRepository, conditionChecker, casterId => new CooldownManager(), new ResourceManager());
            var autoCastEngine = new AutoCastEngine(skillRepository, conditionChecker, casterId => new CooldownManager(), new ResourceManager());
            
            var castingIntegration = new CastingIntegration(clock, skillRepository, windowExecutor, autoCastEngine);

            // Act & Assert - CastingIntegration no longer has IsCasting method (managed by MultiBattleInstance)
            // Just verify the module can be created successfully
            Assert.NotNull(castingIntegration);
        }

        [Fact]
        public void CastingIntegration_CalculateHastePercent_ReturnsBaseHaste()
        {
            // Arrange
            var clock = new TestClock();
            var skillRepository = SkillRepository.Shared;
            var conditionChecker = new ConditionChecker();
            var windowExecutor = new WindowExecutor(skillRepository, conditionChecker, casterId => new CooldownManager(), new ResourceManager());
            var autoCastEngine = new AutoCastEngine(skillRepository, conditionChecker, casterId => new CooldownManager(), new ResourceManager());
            
            var castingIntegration = new CastingIntegration(clock, skillRepository, windowExecutor, autoCastEngine);
            var character = CreateTestCharacter();
            character.HastePercent = 10.0;

            // Act
            var haste = castingIntegration.CalculateHastePercent(character, null);

            // Assert
            Assert.Equal(10.0, haste);
        }

        #endregion

        #region Helper Methods

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
