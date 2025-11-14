using BlazorIdle.Game.Buffs;
using BlazorIdle.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Tests for BuffConfig and BuffRepository (Phase 1).
    /// </summary>
    public class BuffConfigTests
    {
        #region BuffConfig Tests

        [Fact]
        public void BuffConfig_DefaultConstructor_SetsDefaultValues()
        {
            var config = new BuffConfig();

            Assert.NotNull(config.Id);
            Assert.NotNull(config.Name);
            Assert.Equal(BuffKind.Buff, config.Kind);
            Assert.NotNull(config.Effects);
            Assert.Empty(config.Effects);
            Assert.Equal(BuffStackingPolicy.Refresh, config.StackingPolicy);
            Assert.Equal(0, config.MaxStacks);
            Assert.Equal(Game.Skills.BuffTarget.Self, config.DefaultTarget);
        }

        [Fact]
        public void BuffConfig_ToBuffInstance_CreatesValidInstance()
        {
            var config = new BuffConfig
            {
                Id = "test_buff",
                Kind = BuffKind.Buff,
                DurationSec = 10.0,
                TickIntervalSec = 2.0,
                StackingPolicy = BuffStackingPolicy.Stack,
                MaxStacks = 3,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.15)
                }
            };

            var instance = config.ToBuffInstance("player1", "skill1");

            Assert.Equal("test_buff", instance.Id);
            Assert.Equal("player1", instance.OwnerId);
            Assert.Equal(BuffKind.Buff, instance.Kind);
            Assert.Equal(10.0, instance.RemainingDurationSec);
            Assert.Equal(2.0, instance.TickIntervalSec);
            Assert.Equal(BuffStackingPolicy.Stack, instance.StackingPolicy);
            Assert.Equal(3, instance.MaxStacks);
            Assert.Equal(1, instance.Stacks);
            Assert.Equal("skill1", instance.SourceSkillId);
            Assert.Single(instance.Effects);
            Assert.Equal(0, instance.TickAccumulator);
        }

        [Fact]
        public void BuffConfig_ToBuffInstance_ClonesEffectsList()
        {
            var config = new BuffConfig
            {
                Id = "test_buff",
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.15),
                    BuffEffect.StatAdditive("HastePercent", 0.10)
                }
            };

            var instance1 = config.ToBuffInstance("player1");
            var instance2 = config.ToBuffInstance("player2");

            // Modifying one instance's effects should not affect the other
            instance1.Effects.Add(BuffEffect.ForceCrit());

            Assert.Equal(3, instance1.Effects.Count);
            Assert.Equal(2, instance2.Effects.Count);
            Assert.Equal(2, config.Effects.Count); // Original should be unchanged
        }

        [Fact]
        public void BuffConfig_Serialization_RoundTrip()
        {
            var config = new BuffConfig
            {
                Id = "test_buff",
                Name = "Test Buff",
                Description = "A test buff",
                Icon = "🔥",
                Kind = BuffKind.Debuff,
                DurationSec = 5.0,
                TickIntervalSec = 1.0,
                StackingPolicy = BuffStackingPolicy.Refresh,
                MaxStacks = 2,
                DefaultTarget = Game.Skills.BuffTarget.AllEnemies,
                Effects = new List<BuffEffect>
                {
                    BuffEffect.DamageOverTime(10),
                    BuffEffect.StatReduction("DamagePerAttack", 0.20)
                }
            };

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var json = JsonSerializer.Serialize(config, options);
            var deserialized = JsonSerializer.Deserialize<BuffConfig>(json, options);

            Assert.NotNull(deserialized);
            Assert.Equal(config.Id, deserialized.Id);
            Assert.Equal(config.Name, deserialized.Name);
            Assert.Equal(config.Description, deserialized.Description);
            Assert.Equal(config.Icon, deserialized.Icon);
            Assert.Equal(config.Kind, deserialized.Kind);
            Assert.Equal(config.DurationSec, deserialized.DurationSec);
            Assert.Equal(config.TickIntervalSec, deserialized.TickIntervalSec);
            Assert.Equal(config.StackingPolicy, deserialized.StackingPolicy);
            Assert.Equal(config.MaxStacks, deserialized.MaxStacks);
            Assert.Equal(config.DefaultTarget, deserialized.DefaultTarget);
            Assert.Equal(config.Effects.Count, deserialized.Effects.Count);
        }

        #endregion

        #region BuffRepository Tests

        [Fact]
        public void BuffRepository_Singleton_ReturnsSameInstance()
        {
            var instance1 = BuffRepository.Instance;
            var instance2 = BuffRepository.Instance;

            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void BuffRepository_CreateNew_ReturnsNewInstance()
        {
            var instance1 = BuffRepository.CreateNew();
            var instance2 = BuffRepository.CreateNew();

            Assert.NotSame(instance1, instance2);
        }

        [Fact]
        public void BuffRepository_InitializeDefaultBuffs_CreatesExpectedBuffs()
        {
            var repo = BuffRepository.CreateNew();

            Assert.True(repo.Count >= 10); // Should have at least 10 default buffs
            Assert.True(repo.HasBuff("warrior_rage_boost"));
            Assert.True(repo.HasBuff("mage_burn_dot"));
            Assert.True(repo.HasBuff("damage_boost"));
            Assert.True(repo.HasBuff("regeneration"));
            Assert.True(repo.HasBuff("weakness"));
            Assert.True(repo.HasBuff("shield"));
            Assert.True(repo.HasBuff("haste"));
            Assert.True(repo.HasBuff("poison"));
            Assert.True(repo.HasBuff("crit_boost"));
            Assert.True(repo.HasBuff("force_crit"));
        }

        [Fact]
        public void BuffRepository_RegisterBuff_AddsBuffSuccessfully()
        {
            var repo = BuffRepository.CreateNew();
            repo.Clear();

            var config = new BuffConfig
            {
                Id = "custom_buff",
                Name = "Custom Buff"
            };

            repo.RegisterBuff(config);

            Assert.Equal(1, repo.Count);
            Assert.True(repo.HasBuff("custom_buff"));
        }

        [Fact]
        public void BuffRepository_RegisterBuff_NullConfig_ThrowsException()
        {
            var repo = BuffRepository.CreateNew();

            Assert.Throws<ArgumentNullException>(() => repo.RegisterBuff(null!));
        }

        [Fact]
        public void BuffRepository_RegisterBuff_EmptyId_ThrowsException()
        {
            var repo = BuffRepository.CreateNew();

            var config = new BuffConfig { Id = "" };

            Assert.Throws<ArgumentException>(() => repo.RegisterBuff(config));
        }

        [Fact]
        public void BuffRepository_RegisterBuff_DuplicateId_Overwrites()
        {
            var repo = BuffRepository.CreateNew();
            repo.Clear();

            var config1 = new BuffConfig
            {
                Id = "test_buff",
                Name = "First"
            };
            var config2 = new BuffConfig
            {
                Id = "test_buff",
                Name = "Second"
            };

            repo.RegisterBuff(config1);
            repo.RegisterBuff(config2);

            Assert.Equal(1, repo.Count);
            var retrieved = repo.GetBuffById("test_buff");
            Assert.NotNull(retrieved);
            Assert.Equal("Second", retrieved.Name);
        }

        [Fact]
        public void BuffRepository_GetBuffById_ReturnsCorrectBuff()
        {
            var repo = BuffRepository.CreateNew();

            var buff = repo.GetBuffById("warrior_rage_boost");

            Assert.NotNull(buff);
            Assert.Equal("warrior_rage_boost", buff.Id);
            Assert.Equal("狂暴", buff.Name);
            Assert.Equal(BuffKind.Buff, buff.Kind);
        }

        [Fact]
        public void BuffRepository_GetBuffById_NonExistent_ReturnsNull()
        {
            var repo = BuffRepository.CreateNew();

            var buff = repo.GetBuffById("nonexistent_buff");

            Assert.Null(buff);
        }

        [Fact]
        public void BuffRepository_GetBuffById_NullOrEmpty_ReturnsNull()
        {
            var repo = BuffRepository.CreateNew();

            Assert.Null(repo.GetBuffById(null!));
            Assert.Null(repo.GetBuffById(""));
        }

        [Fact]
        public void BuffRepository_HasBuff_ReturnsCorrectValue()
        {
            var repo = BuffRepository.CreateNew();

            Assert.True(repo.HasBuff("warrior_rage_boost"));
            Assert.False(repo.HasBuff("nonexistent_buff"));
            Assert.False(repo.HasBuff(null!));
            Assert.False(repo.HasBuff(""));
        }

        [Fact]
        public void BuffRepository_GetAllBuffs_ReturnsAllRegisteredBuffs()
        {
            var repo = BuffRepository.CreateNew();
            repo.Clear();

            repo.RegisterBuff(new BuffConfig { Id = "buff1", Name = "Buff 1" });
            repo.RegisterBuff(new BuffConfig { Id = "buff2", Name = "Buff 2" });
            repo.RegisterBuff(new BuffConfig { Id = "buff3", Name = "Buff 3" });

            var allBuffs = repo.GetAllBuffs();

            Assert.Equal(3, allBuffs.Count);
            Assert.True(allBuffs.ContainsKey("buff1"));
            Assert.True(allBuffs.ContainsKey("buff2"));
            Assert.True(allBuffs.ContainsKey("buff3"));
        }

        [Fact]
        public void BuffRepository_GetBuffsByKind_FiltersCorrectly()
        {
            var repo = BuffRepository.CreateNew();
            repo.Clear();

            repo.RegisterBuff(new BuffConfig { Id = "buff1", Kind = BuffKind.Buff });
            repo.RegisterBuff(new BuffConfig { Id = "buff2", Kind = BuffKind.Buff });
            repo.RegisterBuff(new BuffConfig { Id = "debuff1", Kind = BuffKind.Debuff });

            var buffs = repo.GetBuffsByKind(BuffKind.Buff);
            var debuffs = repo.GetBuffsByKind(BuffKind.Debuff);

            Assert.Equal(2, buffs.Count);
            Assert.Single(debuffs);
        }

        [Fact]
        public void BuffRepository_Clear_RemovesAllBuffs()
        {
            var repo = BuffRepository.CreateNew();

            var initialCount = repo.Count;
            Assert.True(initialCount > 0);

            repo.Clear();

            Assert.Equal(0, repo.Count);
            Assert.False(repo.HasBuff("warrior_rage_boost"));
        }

        [Fact]
        public void BuffRepository_LoadFromJson_LoadsBuffsSuccessfully()
        {
            var repo = BuffRepository.CreateNew();
            repo.Clear();

            var json = @"[
                {
                    ""id"": ""test_buff1"",
                    ""name"": ""Test Buff 1"",
                    ""kind"": ""Buff"",
                    ""durationSec"": 10.0,
                    ""effects"": [
                        {
                            ""type"": ""StatMultiplier"",
                            ""target"": ""DamagePerAttack"",
                            ""value"": 0.15
                        }
                    ]
                },
                {
                    ""id"": ""test_buff2"",
                    ""name"": ""Test Buff 2"",
                    ""kind"": ""Debuff"",
                    ""effects"": []
                }
            ]";

            repo.LoadFromJson(json);

            Assert.Equal(2, repo.Count);
            Assert.True(repo.HasBuff("test_buff1"));
            Assert.True(repo.HasBuff("test_buff2"));

            var buff1 = repo.GetBuffById("test_buff1");
            Assert.NotNull(buff1);
            Assert.Equal("Test Buff 1", buff1.Name);
            Assert.Equal(BuffKind.Buff, buff1.Kind);
            Assert.Single(buff1.Effects);
        }

        [Fact]
        public void BuffRepository_LoadFromJson_EmptyString_DoesNothing()
        {
            var repo = BuffRepository.CreateNew();
            var initialCount = repo.Count;

            repo.LoadFromJson("");

            Assert.Equal(initialCount, repo.Count);
        }

        [Fact]
        public void BuffRepository_LoadFromJson_InvalidJson_ThrowsException()
        {
            var repo = BuffRepository.CreateNew();

            var invalidJson = "{ invalid json }";

            Assert.Throws<JsonException>(() => repo.LoadFromJson(invalidJson));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Integration_BuffConfig_ToBuffInstance_AndApply()
        {
            var repo = BuffRepository.CreateNew();
            var buffConfig = repo.GetBuffById("damage_boost");
            Assert.NotNull(buffConfig);

            var buffInstance = buffConfig.ToBuffInstance("player1", "attack_skill");

            Assert.Equal("damage_boost", buffInstance.Id);
            Assert.Equal("player1", buffInstance.OwnerId);
            Assert.Equal(BuffKind.Buff, buffInstance.Kind);
            Assert.Equal(1, buffInstance.Stacks);
            Assert.Equal(10.0, buffInstance.RemainingDurationSec);
            Assert.Equal(BuffStackingPolicy.Stack, buffInstance.StackingPolicy);
            Assert.Equal(3, buffInstance.MaxStacks);
        }

        [Fact]
        public void Integration_LoadFromJsonFile_Simulation()
        {
            // Simulates loading from the actual buffs.json file
            var repo = BuffRepository.CreateNew();
            repo.Clear();

            // This is the content structure from buffs.json
            var jsonContent = @"[
                {
                    ""id"": ""warrior_rage_boost"",
                    ""name"": ""狂暴"",
                    ""description"": ""增加攻击力、攻击速度和暴击率"",
                    ""icon"": ""⚔️"",
                    ""kind"": ""Buff"",
                    ""durationSec"": 6.0,
                    ""stackingPolicy"": ""Refresh"",
                    ""maxStacks"": 1,
                    ""defaultTarget"": ""Self"",
                    ""effects"": [
                        {
                            ""type"": ""StatMultiplier"",
                            ""target"": ""DamagePerAttack"",
                            ""value"": 0.15
                        },
                        {
                            ""type"": ""StatAdditive"",
                            ""target"": ""HastePercent"",
                            ""value"": 0.10
                        },
                        {
                            ""type"": ""StatAdditive"",
                            ""target"": ""CritChancePercent"",
                            ""value"": 0.05
                        }
                    ]
                }
            ]";

            repo.LoadFromJson(jsonContent);

            Assert.Equal(1, repo.Count);
            var buff = repo.GetBuffById("warrior_rage_boost");
            Assert.NotNull(buff);
            Assert.Equal("狂暴", buff.Name);
            Assert.Equal("⚔️", buff.Icon);
            Assert.Equal(3, buff.Effects.Count);
        }

        #endregion
    }
}
