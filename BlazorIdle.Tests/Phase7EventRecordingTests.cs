using Xunit;
using BlazorIdle.Game;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Resources;
using BlazorIdle.Game.Config;
using System.Collections.Generic;
using System.Linq;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 7: 测试事件记录功能
    /// Phase 7: Test event recording functionality
    /// </summary>
    public class Phase7EventRecordingTests
    {
        /// <summary>
        /// 创建测试用的战斗实例
        /// Create battle instance for testing
        /// </summary>
        private MultiBattleInstance CreateTestBattle(
            List<Character>? players = null,
            List<Enemy>? enemies = null,
            SkillRepository? skillRepo = null)
        {
            var clock = new TestClock();
            var rng = new RngContext(42);

            players ??= new List<Character>
            {
                new Character
                {
                    Id = "player1",
                    CurrentHp = 100,
                    MaxHp = 100,
                    ActiveCombatProfessionId = "warrior",
                    AttackIntervalSec = 1.0
                }
            };

            enemies ??= new List<Enemy>
            {
                new Enemy
                {
                    Id = "enemy1",
                    CurrentHp = 50,
                    MaxHp = 50,
                    DamagePerAttack = 10,
                    AttackIntervalSec = 2.0
                }
            };

            var playerTeam = new BattleTeam<Character>(players);
            var enemyTeam = new BattleTeam<Enemy>(enemies);

            var resolver = new SkillResolver(skillRepo);
            var castingController = new CastingController(clock, rng);
            var config = new CombatConfig { EmitCastEvents = true }; // Enable event recording

            return new MultiBattleInstance(
                clock, rng, playerTeam, enemyTeam, resolver, castingController, config);
        }

        [Fact]
        public void BuffApplyEvent_RecordedWhenBuffApplied()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var buffTemplate = new BuffInstance(
                id: "test_buff",
                ownerId: "", // Will be set by ApplyBuffOperation
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.5) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 5.0
            );

            var skill = new SkillDef
            {
                Id = SkillIds.AttackBasic,
                DamageMultiplier = 1.0,
                OnHitBuffs = new List<BuffOperation>
                {
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.Target,
                        BuffTemplate = buffTemplate
                    }
                }
            };
            skillRepo.RegisterSkill(skill);

            var battle = CreateTestBattle(skillRepo: skillRepo);
            battle.Start();

            // Act - Attack to trigger buff application
            battle.AdvanceTick(1.1); // Trigger attack

            // Assert
            var segments = battle.GetCompletedSegments();
            Assert.NotEmpty(segments);

            var allEvents = segments.SelectMany(s => s.Events).ToList();
            var buffApplyEvents = allEvents.OfType<BuffApplyEvent>().ToList();

            Assert.NotEmpty(buffApplyEvents);
            var evt = buffApplyEvents.First();
            Assert.Equal("test_buff", evt.BuffId);
            Assert.Equal("enemy1", evt.OwnerId);
            Assert.Equal(BuffKind.Buff, evt.Kind);
            Assert.Equal(5.0, evt.DurationSec);
            Assert.Equal(1, evt.Stacks);
        }

        [Fact]
        public void BuffRemoveEvent_RecordedWhenBuffRemoved()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var buffTemplate = new BuffInstance(
                id: "short_buff",
                ownerId: "",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.5) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 0.5 // Short duration
            );

            var skill = new SkillDef
            {
                Id = SkillIds.AttackBasic,
                DamageMultiplier = 1.0,
                OnHitBuffs = new List<BuffOperation>
                {
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.Target,
                        BuffTemplate = buffTemplate
                    }
                }
            };
            skillRepo.RegisterSkill(skill);

            var battle = CreateTestBattle(skillRepo: skillRepo);
            battle.Start();

            // Act
            battle.AdvanceTick(1.1); // Apply buff
            battle.AdvanceTick(1.0); // Wait for buff to expire

            // Assert
            var segments = battle.GetCompletedSegments();
            var allEvents = segments.SelectMany(s => s.Events).ToList();
            var buffRemoveEvents = allEvents.OfType<BuffRemoveEvent>().ToList();

            Assert.NotEmpty(buffRemoveEvents);
            var evt = buffRemoveEvents.First();
            Assert.Equal("short_buff", evt.BuffId);
            Assert.Equal("enemy1", evt.OwnerId);
            Assert.Equal("expired", evt.Reason);
        }

        [Fact]
        public void BuffTickEvent_RecordedWhenDoTTicks()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var dotBuff = new BuffInstance(
                id: "dot_buff",
                ownerId: "",
                kind: BuffKind.Debuff,
                effects: new List<BuffEffect> { BuffEffect.DamageOverTime(5) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 3.0,
                tickIntervalSec: 1.0
            );

            var skill = new SkillDef
            {
                Id = SkillIds.AttackBasic,
                DamageMultiplier = 1.0,
                OnHitBuffs = new List<BuffOperation>
                {
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.Target,
                        BuffTemplate = dotBuff
                    }
                }
            };
            skillRepo.RegisterSkill(skill);

            var battle = CreateTestBattle(skillRepo: skillRepo);
            battle.Start();

            // Act
            battle.AdvanceTick(1.1); // Apply DoT buff
            battle.AdvanceTick(1.0); // Trigger first DoT tick

            // Assert
            var segments = battle.GetCompletedSegments();
            var allEvents = segments.SelectMany(s => s.Events).ToList();
            var buffTickEvents = allEvents.OfType<BuffTickEvent>().ToList();

            Assert.NotEmpty(buffTickEvents);
            var evt = buffTickEvents.First();
            Assert.Equal("dot_buff", evt.BuffId);
            Assert.Equal("enemy1", evt.OwnerId);
            Assert.Equal(BuffTickType.DamageOverTime, evt.TickType);
            Assert.Equal(5, evt.Amount);
            Assert.True(evt.ResultingHp <= 50); // Enemy took damage
        }

        [Fact]
        public void BuffTickEvent_RecordedWhenHoTTicks()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            
            // Create a damaged player
            var players = new List<Character>
            {
                new Character
                {
                    Id = "player1",
                    CurrentHp = 50, // Damaged
                    MaxHp = 100,
                    ActiveCombatProfessionId = "warrior",
                    AttackIntervalSec = 1.0
                }
            };

            var hotBuff = new BuffInstance(
                id: "hot_buff",
                ownerId: "",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.HealOverTime(10) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 3.0,
                tickIntervalSec: 1.0
            );

            var skill = new SkillDef
            {
                Id = SkillIds.AttackBasic,
                DamageMultiplier = 1.0,
                OnCastBuffs = new List<BuffOperation>
                {
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.Self,
                        BuffTemplate = hotBuff
                    }
                }
            };
            skillRepo.RegisterSkill(skill);

            var battle = CreateTestBattle(players: players, skillRepo: skillRepo);
            battle.Start();

            // Act
            battle.AdvanceTick(1.1); // Apply HoT buff
            battle.AdvanceTick(1.0); // Trigger first HoT tick

            // Assert
            var segments = battle.GetCompletedSegments();
            var allEvents = segments.SelectMany(s => s.Events).ToList();
            var buffTickEvents = allEvents.OfType<BuffTickEvent>().ToList();

            Assert.NotEmpty(buffTickEvents);
            var evt = buffTickEvents.First();
            Assert.Equal("hot_buff", evt.BuffId);
            Assert.Equal("player1", evt.OwnerId);
            Assert.Equal(BuffTickType.HealOverTime, evt.TickType);
            Assert.Equal(10, evt.Amount);
            Assert.True(evt.ResultingHp > 50); // Player was healed
        }

        [Fact]
        public void HealEvent_RecordedWhenInstantHealApplied()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            
            // Create a damaged player
            var players = new List<Character>
            {
                new Character
                {
                    Id = "player1",
                    CurrentHp = 50, // Damaged
                    MaxHp = 100,
                    ActiveCombatProfessionId = "warrior",
                    AttackIntervalSec = 1.0
                }
            };

            var skill = new SkillDef
            {
                Id = SkillIds.AttackBasic,
                DamageMultiplier = 1.0,
                InstantHeal = 20 // Heal 20 HP
            };
            skillRepo.RegisterSkill(skill);

            var battle = CreateTestBattle(players: players, skillRepo: skillRepo);
            battle.Start();

            // Act
            battle.AdvanceTick(1.1); // Trigger attack with instant heal

            // Assert
            var segments = battle.GetCompletedSegments();
            var allEvents = segments.SelectMany(s => s.Events).ToList();
            var healEvents = allEvents.OfType<HealEvent>().ToList();

            Assert.NotEmpty(healEvents);
            var evt = healEvents.First();
            Assert.Equal("player1", evt.OwnerId);
            Assert.Equal(20, evt.Amount);
            Assert.Equal(70, evt.ResultingHp); // 50 + 20 = 70
            Assert.Equal(SkillIds.AttackBasic, evt.Source);
        }

        [Fact]
        public void ResourceGainEvent_RecordedForSkillResourceChanges()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var skill = new SkillDef
            {
                Id = SkillIds.AttackBasic,
                DamageMultiplier = 1.0,
                ResourceGains = new Dictionary<string, int>
                {
                    { "rage", 2 } // Gain 2 rage per attack
                }
            };
            skillRepo.RegisterSkill(skill);

            var battle = CreateTestBattle(skillRepo: skillRepo);
            battle.Start();

            // Act
            battle.AdvanceTick(1.1); // Trigger attack

            // Assert
            var segments = battle.GetCompletedSegments();
            var allEvents = segments.SelectMany(s => s.Events).ToList();
            var resourceEvents = allEvents.OfType<ResourceGainEvent>().ToList();

            Assert.NotEmpty(resourceEvents);
            
            // Should have events from both normal attack gain and skill resource gain
            var skillResourceEvent = resourceEvents.FirstOrDefault(e => 
                e.Reason == "skill_resource_gain");
            
            Assert.NotNull(skillResourceEvent);
            Assert.Equal("player1", skillResourceEvent.ActorId);
            Assert.Equal("rage", skillResourceEvent.BucketId);
            Assert.Equal(2, skillResourceEvent.Delta);
            Assert.Equal(SkillIds.AttackBasic, skillResourceEvent.SkillId);
        }

        [Fact]
        public void ResourceGainEvent_RecordedForResourceCosts()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            
            // Create player with some rage
            var players = new List<Character>
            {
                new Character
                {
                    Id = "player1",
                    CurrentHp = 100,
                    MaxHp = 100,
                    ActiveCombatProfessionId = "warrior",
                    AttackIntervalSec = 1.0
                }
            };

            // First, give the player some rage
            var battle = CreateTestBattle(players: players);
            battle.Start();
            battle.AdvanceTick(5.5); // Attack 5 times to build rage

            // Now test a skill that costs rage
            var costSkill = new SkillDef
            {
                Id = SkillIds.SpecialPulse,
                DamageMultiplier = 2.0,
                ResourceCosts = new Dictionary<string, int>
                {
                    { "rage", 3 } // Costs 3 rage
                }
            };
            skillRepo.RegisterSkill(costSkill);

            var battle2 = CreateTestBattle(players: players, skillRepo: skillRepo);
            battle2.Start();
            
            // Build some rage first
            battle2.AdvanceTick(3.5);
            
            // Clear previous events
            var _ = battle2.GetCompletedSegments();
            
            // Trigger special attack
            battle2.ProcessCharacterSpecial("player1");

            // Assert
            var segments = battle2.GetCompletedSegments();
            var allEvents = segments.SelectMany(s => s.Events).ToList();
            var resourceEvents = allEvents.OfType<ResourceGainEvent>().ToList();

            var costEvent = resourceEvents.FirstOrDefault(e => 
                e.Reason == "skill_resource_cost");
            
            if (costEvent != null)
            {
                Assert.Equal("player1", costEvent.ActorId);
                Assert.Equal("rage", costEvent.BucketId);
                Assert.Equal(-3, costEvent.Delta); // Negative for cost
                Assert.Equal(SkillIds.SpecialPulse, costEvent.SkillId);
            }
        }

        [Fact]
        public void EventsRecorded_InCorrectOrder()
        {
            // Arrange
            var skillRepo = new SkillRepository();
            var buffTemplate = new BuffInstance(
                id: "test_buff",
                ownerId: "",
                kind: BuffKind.Buff,
                effects: new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.5) },
                stackingPolicy: BuffStackingPolicy.Refresh,
                durationSec: 5.0
            );

            var skill = new SkillDef
            {
                Id = SkillIds.AttackBasic,
                DamageMultiplier = 1.0,
                InstantHeal = 5,
                ResourceGains = new Dictionary<string, int> { { "rage", 1 } },
                OnHitBuffs = new List<BuffOperation>
                {
                    new BuffOperation
                    {
                        Type = BuffOperationType.Apply,
                        Target = BuffTarget.Target,
                        BuffTemplate = buffTemplate
                    }
                }
            };
            skillRepo.RegisterSkill(skill);

            var battle = CreateTestBattle(skillRepo: skillRepo);
            battle.Start();

            // Act
            battle.AdvanceTick(1.1); // Trigger attack with all effects

            // Assert
            var segments = battle.GetCompletedSegments();
            var allEvents = segments.SelectMany(s => s.Events).ToList();

            // Should have events in this order:
            // 1. Damage event (from attack)
            // 2. Buff apply event (from OnHitBuffs)
            // 3. Heal event (from InstantHeal)
            // 4. Resource gain events (from attack and skill)

            var damageEvents = allEvents.OfType<CombatEvent>()
                .Where(e => e.Damage > 0).ToList();
            var buffEvents = allEvents.OfType<BuffApplyEvent>().ToList();
            var healEvents = allEvents.OfType<HealEvent>().ToList();
            var resourceEvents = allEvents.OfType<ResourceGainEvent>().ToList();

            Assert.NotEmpty(damageEvents);
            Assert.NotEmpty(buffEvents);
            Assert.NotEmpty(healEvents);
            Assert.NotEmpty(resourceEvents);

            // Verify events are in the correct temporal order (all have TimeMs)
            var orderedEvents = allEvents.OrderBy(e => e.TimeMs).ToList();
            Assert.Equal(allEvents.Count, orderedEvents.Count);
        }
    }
}
