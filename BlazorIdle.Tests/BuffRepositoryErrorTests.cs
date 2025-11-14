using BlazorIdle.Game.Buffs;
using BlazorIdle.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Tests for BuffRepository error handling and edge cases.
    /// Part of P2 improvements.
    /// </summary>
    public class BuffRepositoryErrorTests
    {
        [Fact]
        public void BuffRepository_RegisterBuff_ThrowsOnNull()
        {
            var repo = BuffRepository.CreateNew();
            Assert.Throws<ArgumentNullException>(() => repo.RegisterBuff(null!));
        }

        [Fact]
        public void BuffRepository_RegisterBuff_ThrowsOnEmptyId()
        {
            var repo = BuffRepository.CreateNew();
            var config = new BuffConfig
            {
                Id = "",
                Name = "Test",
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            };
            Assert.Throws<ArgumentException>(() => repo.RegisterBuff(config));
        }

        [Fact]
        public void BuffRepository_RegisterBuff_ThrowsOnNullId()
        {
            var repo = BuffRepository.CreateNew();
            var config = new BuffConfig
            {
                Id = null!,
                Name = "Test",
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            };
            Assert.Throws<ArgumentException>(() => repo.RegisterBuff(config));
        }

        [Fact]
        public void BuffRepository_RegisterBuff_AllowsOverwrite()
        {
            var repo = BuffRepository.CreateNew();
            var config1 = new BuffConfig
            {
                Id = "test_buff",
                Name = "Original",
                Kind = BuffKind.Buff,
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            };
            var config2 = new BuffConfig
            {
                Id = "test_buff",
                Name = "Updated",
                Kind = BuffKind.Debuff,
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.2) }
            };

            repo.RegisterBuff(config1);
            repo.RegisterBuff(config2); // Should overwrite without throwing

            var retrieved = repo.GetBuffById("test_buff");
            Assert.NotNull(retrieved);
            Assert.Equal("Updated", retrieved.Name);
            Assert.Equal(BuffKind.Debuff, retrieved.Kind);
        }

        [Fact]
        public void BuffRepository_GetBuffById_ReturnsNullForNonexistent()
        {
            var repo = BuffRepository.CreateNew();
            var result = repo.GetBuffById("nonexistent_buff");
            Assert.Null(result);
        }

        [Fact]
        public void BuffRepository_GetBuffById_ReturnsNullForEmptyString()
        {
            var repo = BuffRepository.CreateNew();
            var result = repo.GetBuffById("");
            Assert.Null(result);
        }

        [Fact]
        public void BuffRepository_GetBuffById_ReturnsNullForNullString()
        {
            var repo = BuffRepository.CreateNew();
            var result = repo.GetBuffById(null!);
            Assert.Null(result);
        }

        [Fact]
        public void BuffRepository_HasBuff_ReturnsFalseForEmpty()
        {
            var repo = BuffRepository.CreateNew();
            Assert.False(repo.HasBuff(""));
        }

        [Fact]
        public void BuffRepository_HasBuff_ReturnsFalseForNull()
        {
            var repo = BuffRepository.CreateNew();
            Assert.False(repo.HasBuff(null!));
        }

        [Fact]
        public void BuffRepository_HasBuff_ReturnsTrueForExisting()
        {
            var repo = BuffRepository.CreateNew();
            var config = new BuffConfig
            {
                Id = "test_buff",
                Name = "Test",
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            };
            repo.RegisterBuff(config);
            Assert.True(repo.HasBuff("test_buff"));
        }

        [Fact]
        public void BuffRepository_LoadFromJson_HandlesEmptyArray()
        {
            var repo = BuffRepository.CreateNew();
            repo.Clear(); // Clear the auto-loaded buffs from JSON
            repo.LoadFromJson("[]");
            Assert.Equal(0, repo.Count);
        }

        [Fact]
        public void BuffRepository_LoadFromJson_IgnoresEmptyString()
        {
            var repo = BuffRepository.CreateNew();
            var initialCount = repo.Count; // Should have loaded from JSON
            repo.LoadFromJson("");
            Assert.Equal(initialCount, repo.Count); // Count unchanged
        }

        [Fact]
        public void BuffRepository_LoadFromJson_IgnoresNullString()
        {
            var repo = BuffRepository.CreateNew();
            var initialCount = repo.Count; // Should have loaded from JSON
            repo.LoadFromJson(null!);
            Assert.Equal(initialCount, repo.Count); // Count unchanged
        }

        [Fact]
        public void BuffRepository_LoadFromJson_ThrowsOnInvalidJson()
        {
            var repo = BuffRepository.CreateNew();
            Assert.Throws<System.Text.Json.JsonException>(() => 
                repo.LoadFromJson("{invalid json}"));
        }

        [Fact]
        public void BuffRepository_LoadFromJson_HandlesValidJson()
        {
            var repo = BuffRepository.CreateNew();
            repo.Clear(); // Clear auto-loaded buffs
            var json = @"[
                {
                    ""id"": ""test_buff"",
                    ""name"": ""Test Buff"",
                    ""kind"": ""Buff"",
                    ""effects"": [
                        {
                            ""type"": ""StatMultiplier"",
                            ""target"": ""DamagePerAttack"",
                            ""value"": 0.15
                        }
                    ]
                }
            ]";
            
            repo.LoadFromJson(json);
            Assert.Equal(1, repo.Count);
            Assert.True(repo.HasBuff("test_buff"));
        }

        [Fact]
        public void BuffConfig_ToBuffInstance_ClonesEffectsList()
        {
            var config = new BuffConfig
            {
                Id = "test",
                Effects = new List<BuffEffect>
                {
                    BuffEffect.StatMultiplier("DamagePerAttack", 0.1),
                    BuffEffect.StatAdditive("HastePercent", 10.0)
                }
            };

            var instance1 = config.ToBuffInstance("owner1");
            var instance2 = config.ToBuffInstance("owner2");

            // Modify instance1's effects
            instance1.Effects.Clear();

            // instance2 should be unaffected
            Assert.Equal(2, instance2.Effects.Count);
            Assert.Equal(2, config.Effects.Count); // Original unchanged
        }

        [Fact]
        public void BuffConfig_ToBuffInstance_SetsDifferentOwners()
        {
            var config = new BuffConfig
            {
                Id = "test",
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            };

            var instance1 = config.ToBuffInstance("player1");
            var instance2 = config.ToBuffInstance("player2");

            Assert.Equal("player1", instance1.OwnerId);
            Assert.Equal("player2", instance2.OwnerId);
        }

        [Fact]
        public void BuffConfig_ToBuffInstance_SetsSourceSkillId()
        {
            var config = new BuffConfig
            {
                Id = "test",
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            };

            var instance = config.ToBuffInstance("owner", "skill_123");

            Assert.Equal("skill_123", instance.SourceSkillId);
        }

        [Fact]
        public void BuffConfig_ToBuffInstance_HandlesNullSourceSkillId()
        {
            var config = new BuffConfig
            {
                Id = "test",
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            };

            var instance = config.ToBuffInstance("owner", null);

            Assert.Null(instance.SourceSkillId);
        }

        [Fact]
        public void BuffRepository_GetBuffsByKind_FiltersBuff()
        {
            var repo = BuffRepository.CreateNew();
            repo.Clear(); // Clear auto-loaded buffs
            repo.RegisterBuff(new BuffConfig
            {
                Id = "buff1",
                Name = "Buff",
                Kind = BuffKind.Buff,
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            });
            repo.RegisterBuff(new BuffConfig
            {
                Id = "debuff1",
                Name = "Debuff",
                Kind = BuffKind.Debuff,
                Effects = new List<BuffEffect> { BuffEffect.StatReduction("DamagePerAttack", 0.1) }
            });

            var buffs = repo.GetBuffsByKind(BuffKind.Buff);
            Assert.Single(buffs);
            Assert.Equal("buff1", buffs[0].Id);
        }

        [Fact]
        public void BuffRepository_GetBuffsByKind_FiltersDebuff()
        {
            var repo = BuffRepository.CreateNew();
            repo.Clear(); // Clear auto-loaded buffs
            repo.RegisterBuff(new BuffConfig
            {
                Id = "buff1",
                Name = "Buff",
                Kind = BuffKind.Buff,
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            });
            repo.RegisterBuff(new BuffConfig
            {
                Id = "debuff1",
                Name = "Debuff",
                Kind = BuffKind.Debuff,
                Effects = new List<BuffEffect> { BuffEffect.StatReduction("DamagePerAttack", 0.1) }
            });

            var debuffs = repo.GetBuffsByKind(BuffKind.Debuff);
            Assert.Single(debuffs);
            Assert.Equal("debuff1", debuffs[0].Id);
        }

        [Fact]
        public void BuffRepository_Clear_RemovesAllBuffs()
        {
            var repo = BuffRepository.CreateNew();
            repo.Clear(); // Clear auto-loaded buffs first
            repo.RegisterBuff(new BuffConfig
            {
                Id = "test1",
                Name = "Test",
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            });
            repo.RegisterBuff(new BuffConfig
            {
                Id = "test2",
                Name = "Test",
                Effects = new List<BuffEffect> { BuffEffect.StatMultiplier("DamagePerAttack", 0.1) }
            });

            Assert.Equal(2, repo.Count);

            repo.Clear();

            Assert.Equal(0, repo.Count);
            Assert.False(repo.HasBuff("test1"));
            Assert.False(repo.HasBuff("test2"));
        }
    }
}
