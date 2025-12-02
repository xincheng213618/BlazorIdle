using BlazorIdle.Game.Buffs;
using BlazorIdle.Game.Combat;
using System;
using System.Collections.Generic;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Buff 系统优化 Phase 1 测试 - BuffStatMapper 和 BuffStatApplier
    /// Buff system optimization Phase 1 tests - BuffStatMapper and BuffStatApplier
    /// </summary>
    public class BuffSystemOptimizationPhase1Tests
    {
        #region BuffStatMapper Tests

        [Theory]
        [InlineData("DamagePerAttack", "AttackFinal")]
        [InlineData("SpecialDamage", "AttackFinal")]
        [InlineData("DamageReduction", "DamageReductionPercent")]
        [InlineData("CritMultiplier", "CritDamageBonusPercent")]
        public void NormalizeStatName_LegacyNames_ReturnsNewName(string legacyName, string expectedNewName)
        {
            // Act
            var result = BuffStatMapper.NormalizeStatName(legacyName);

            // Assert
            Assert.Equal(expectedNewName, result);
        }

        [Theory]
        [InlineData("AttackFinal")]
        [InlineData("AttackPercent")]
        [InlineData("SpecialAttackPercent")]
        [InlineData("HPPercent")]
        [InlineData("HastePercent")]
        [InlineData("CritChancePercent")]
        [InlineData("CritDamageBonusPercent")]
        [InlineData("FortifyMaxPercent")]
        [InlineData("BackwaterMaxPercent")]
        [InlineData("ChasePercent")]
        [InlineData("KenChasePercent")]
        [InlineData("ChaseFlat")]
        [InlineData("DamageReductionPercent")]
        public void NormalizeStatName_NewNames_ReturnsSameName(string newName)
        {
            // Act
            var result = BuffStatMapper.NormalizeStatName(newName);

            // Assert
            Assert.Equal(newName, result);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void NormalizeStatName_NullOrEmpty_ReturnsEmpty(string? input, string expected)
        {
            // Act
            var result = BuffStatMapper.NormalizeStatName(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("DamagePerAttack", true)]
        [InlineData("AttackFinal", true)]
        [InlineData("InvalidStat", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        public void IsValidStatName_ReturnsCorrectResult(string? statName, bool expected)
        {
            // Act
            var result = BuffStatMapper.IsValidStatName(statName);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("DamagePerAttack", true)]
        [InlineData("CritMultiplier", true)]
        [InlineData("AttackFinal", false)]
        [InlineData("AttackPercent", false)]
        public void IsLegacyStatName_ReturnsCorrectResult(string? statName, bool expected)
        {
            // Act
            var result = BuffStatMapper.IsLegacyStatName(statName);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void NeedsValueConversion_CritMultiplier_ReturnsTrue()
        {
            // Act
            var result = BuffStatMapper.NeedsValueConversion("CritMultiplier", out var converter);

            // Assert
            Assert.True(result);
            Assert.NotNull(converter);
            
            // CritMultiplier 1.2 → 20%
            Assert.Equal(20.0, converter!(1.2), 0.001);
            // CritMultiplier 1.5 → 50%
            Assert.Equal(50.0, converter(1.5), 0.001);
            // CritMultiplier 2.0 → 100%
            Assert.Equal(100.0, converter(2.0), 0.001);
        }

        [Theory]
        [InlineData("AttackFinal")]
        [InlineData("AttackPercent")]
        [InlineData("DamagePerAttack")]
        public void NeedsValueConversion_OtherStats_ReturnsFalse(string statName)
        {
            // Act
            var result = BuffStatMapper.NeedsValueConversion(statName, out var converter);

            // Assert
            Assert.False(result);
            Assert.Null(converter);
        }

        [Fact]
        public void GetValidStatNames_Returns13Stats()
        {
            // Act
            var names = BuffStatMapper.GetValidStatNames();

            // Assert
            Assert.Equal(13, names.Count);
            Assert.Contains("AttackFinal", names);
            Assert.Contains("AttackPercent", names);
            Assert.Contains("DamageReductionPercent", names);
        }

        [Fact]
        public void GetLegacyMappings_Returns4Mappings()
        {
            // Act
            var mappings = BuffStatMapper.GetLegacyMappings();

            // Assert
            Assert.Equal(4, mappings.Count);
            Assert.Equal("AttackFinal", mappings["DamagePerAttack"]);
            Assert.Equal("CritDamageBonusPercent", mappings["CritMultiplier"]);
        }

        #endregion

        #region BuffStatApplier Tests

        [Fact]
        public void ApplyBuffsToCombatStats_NullBuffOwner_ReturnsCopy()
        {
            // Arrange
            var baseStats = CreateBaseStats();

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, null);

            // Assert
            Assert.Equal(100, result.AttackFinal);
            Assert.Equal(50.0, result.AttackPercent);
            Assert.NotSame(baseStats, result);
        }

        [Fact]
        public void ApplyBuffsToCombatStats_NoBuffs_ReturnsCopy()
        {
            // Arrange
            var baseStats = CreateBaseStats();
            var buffOwner = new MockBuffOwner();

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            Assert.Equal(100, result.AttackFinal);
            Assert.Equal(50.0, result.AttackPercent);
        }

        [Fact]
        public void ApplyBuffsToCombatStats_StatAdditive_AddsValue()
        {
            // Arrange
            var baseStats = CreateBaseStats();
            var buffOwner = new MockBuffOwner();
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "test_buff",
                OwnerId = "player",
                Stacks = 1,
                AppliedAtMs = 1000,
                Effects = new List<BuffEffect>
                {
                    new BuffEffect(BuffEffectType.StatAdditive, "AttackPercent", 20.0)
                }
            });

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            Assert.Equal(70.0, result.AttackPercent); // 50 + 20 = 70
        }

        [Fact]
        public void ApplyBuffsToCombatStats_StatMultiplier_MultipliesValue()
        {
            // Arrange
            var baseStats = CreateBaseStats();
            var buffOwner = new MockBuffOwner();
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "test_buff",
                OwnerId = "player",
                Stacks = 1,
                AppliedAtMs = 1000,
                Effects = new List<BuffEffect>
                {
                    new BuffEffect(BuffEffectType.StatMultiplier, "AttackFinal", 0.5) // +50%
                }
            });

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            Assert.Equal(150, result.AttackFinal); // 100 * 1.5 = 150
        }

        [Fact]
        public void ApplyBuffsToCombatStats_StatReduction_ReducesValue()
        {
            // Arrange
            var baseStats = CreateBaseStats();
            var buffOwner = new MockBuffOwner();
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "test_debuff",
                OwnerId = "player",
                Stacks = 1,
                AppliedAtMs = 1000,
                Effects = new List<BuffEffect>
                {
                    new BuffEffect(BuffEffectType.StatReduction, "AttackPercent", 0.2) // -20%
                }
            });

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            Assert.Equal(40.0, result.AttackPercent); // 50 * 0.8 = 40
        }

        [Fact]
        public void ApplyBuffsToCombatStats_Stacks_AppliesMultipleTimes()
        {
            // Arrange
            var baseStats = CreateBaseStats();
            var buffOwner = new MockBuffOwner();
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "test_buff",
                OwnerId = "player",
                Stacks = 3, // 3层
                AppliedAtMs = 1000,
                Effects = new List<BuffEffect>
                {
                    new BuffEffect(BuffEffectType.StatAdditive, "AttackPercent", 10.0)
                }
            });

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            Assert.Equal(80.0, result.AttackPercent); // 50 + 10*3 = 80
        }

        [Fact]
        public void ApplyBuffsToCombatStats_LegacyStatName_MapsCorrectly()
        {
            // Arrange
            var baseStats = CreateBaseStats();
            var buffOwner = new MockBuffOwner();
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "legacy_buff",
                OwnerId = "player",
                Stacks = 1,
                AppliedAtMs = 1000,
                Effects = new List<BuffEffect>
                {
                    // 使用旧属性名 DamagePerAttack
                    new BuffEffect(BuffEffectType.StatAdditive, "DamagePerAttack", 50)
                }
            });

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            Assert.Equal(150, result.AttackFinal); // 100 + 50 = 150
        }

        [Fact]
        public void ApplyBuffsToCombatStats_CritMultiplier_ConvertsValue()
        {
            // Arrange
            var baseStats = CreateBaseStats();
            baseStats.CritDamageBonusPercent = 0; // 起始0%
            var buffOwner = new MockBuffOwner();
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "crit_buff",
                OwnerId = "player",
                Stacks = 1,
                AppliedAtMs = 1000,
                Effects = new List<BuffEffect>
                {
                    // 使用旧的倍率格式 1.2 = +20%
                    new BuffEffect(BuffEffectType.StatAdditive, "CritMultiplier", 1.2)
                }
            });

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            Assert.Equal(20.0, result.CritDamageBonusPercent, 0.001); // (1.2-1)*100 = 20%
        }

        [Fact]
        public void ApplyBuffsToCombatStats_MultipleBuffs_AppliesInTimeOrder()
        {
            // Arrange
            var baseStats = CreateBaseStats();
            baseStats.AttackPercent = 0;
            var buffOwner = new MockBuffOwner();
            
            // 第二个应用的 buff - 先加 10
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "buff_1",
                OwnerId = "player",
                Stacks = 1,
                AppliedAtMs = 1000,
                Effects = new List<BuffEffect>
                {
                    new BuffEffect(BuffEffectType.StatAdditive, "AttackPercent", 10.0)
                }
            });
            
            // 第一个应用的 buff - 后乘 1.5
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "buff_2",
                OwnerId = "player",
                Stacks = 1,
                AppliedAtMs = 2000,
                Effects = new List<BuffEffect>
                {
                    new BuffEffect(BuffEffectType.StatMultiplier, "AttackPercent", 0.5)
                }
            });

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            // 按时间排序：先 1000ms 加 10 = 10，再 2000ms 乘 1.5 = 15
            Assert.Equal(15.0, result.AttackPercent);
        }

        [Fact]
        public void ApplyBuffsToCombatStats_SkipsNonStatEffects()
        {
            // Arrange
            var baseStats = CreateBaseStats();
            var buffOwner = new MockBuffOwner();
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "mixed_buff",
                OwnerId = "player",
                Stacks = 1,
                AppliedAtMs = 1000,
                Effects = new List<BuffEffect>
                {
                    new BuffEffect(BuffEffectType.ForceCrit),
                    new BuffEffect(BuffEffectType.DamageOverTime, amountPerTick: 10),
                    new BuffEffect(BuffEffectType.HealOverTime, amountPerTick: 5),
                    new BuffEffect(BuffEffectType.StatAdditive, "AttackPercent", 20.0)
                }
            });

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            Assert.Equal(70.0, result.AttackPercent); // 只应用了 StatAdditive
        }

        [Fact]
        public void ApplyBuffsToCombatStats_AllStats_AppliesCorrectly()
        {
            // Arrange
            var baseStats = CombatStats.CreateDefault();
            var buffOwner = new MockBuffOwner();
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "all_stats_buff",
                OwnerId = "player",
                Stacks = 1,
                AppliedAtMs = 1000,
                Effects = new List<BuffEffect>
                {
                    new BuffEffect(BuffEffectType.StatAdditive, "AttackFinal", 10),
                    new BuffEffect(BuffEffectType.StatAdditive, "AttackPercent", 5.0),
                    new BuffEffect(BuffEffectType.StatAdditive, "SpecialAttackPercent", 5.0),
                    new BuffEffect(BuffEffectType.StatAdditive, "HPPercent", 5.0),
                    new BuffEffect(BuffEffectType.StatAdditive, "HastePercent", 5.0),
                    new BuffEffect(BuffEffectType.StatAdditive, "CritChancePercent", 5.0),
                    new BuffEffect(BuffEffectType.StatAdditive, "CritDamageBonusPercent", 10.0),
                    new BuffEffect(BuffEffectType.StatAdditive, "FortifyMaxPercent", 5.0),
                    new BuffEffect(BuffEffectType.StatAdditive, "BackwaterMaxPercent", 5.0),
                    new BuffEffect(BuffEffectType.StatAdditive, "ChasePercent", 5.0),
                    new BuffEffect(BuffEffectType.StatAdditive, "KenChasePercent", 5.0),
                    new BuffEffect(BuffEffectType.StatAdditive, "ChaseFlat", 100),
                    new BuffEffect(BuffEffectType.StatAdditive, "DamageReductionPercent", 5.0),
                }
            });

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            Assert.Equal(110, result.AttackFinal);
            Assert.Equal(5.0, result.AttackPercent);
            Assert.Equal(5.0, result.SpecialAttackPercent);
            Assert.Equal(5.0, result.HPPercent);
            Assert.Equal(5.0, result.HastePercent);
            Assert.Equal(5.0, result.CritChancePercent);
            Assert.Equal(10.0, result.CritDamageBonusPercent);
            Assert.Equal(5.0, result.FortifyMaxPercent);
            Assert.Equal(5.0, result.BackwaterMaxPercent);
            Assert.Equal(5.0, result.ChasePercent);
            Assert.Equal(5.0, result.KenChasePercent);
            Assert.Equal(100, result.ChaseFlat);
            Assert.Equal(5.0, result.DamageReductionPercent);
        }

        [Fact]
        public void ApplyBuffsToCombatStats_ExceedsCap_BuffStillApplied()
        {
            // Arrange - 模拟装备已达到上限后再应用 Buff
            // Simulate equipment reached cap then apply buff
            var baseStats = CreateBaseStats();
            baseStats.AttackPercent = 100.0; // 已达到装备上限
            var buffOwner = new MockBuffOwner();
            buffOwner.AddBuff(new BuffInstance
            {
                Id = "buff_over_cap",
                OwnerId = "player",
                Stacks = 1,
                AppliedAtMs = 1000,
                Effects = new List<BuffEffect>
                {
                    new BuffEffect(BuffEffectType.StatAdditive, "AttackPercent", 20.0)
                }
            });

            // Act
            var result = BuffStatApplier.ApplyBuffsToCombatStats(baseStats, buffOwner);

            // Assert
            // Buff 不受上限影响，应该是 100 + 20 = 120
            Assert.Equal(120.0, result.AttackPercent);
        }

        [Fact]
        public void MergeStats_CombinesAdditive()
        {
            // Arrange
            var stats1 = new CombatStats
            {
                AttackFinal = 100,
                AttackPercent = 30.0,
                CritChancePercent = 10.0
            };
            var stats2 = new CombatStats
            {
                AttackFinal = 50,
                AttackPercent = 20.0,
                CritChancePercent = 5.0
            };

            // Act
            var result = BuffStatApplier.MergeStats(stats1, stats2);

            // Assert
            Assert.Equal(150, result.AttackFinal);
            Assert.Equal(50.0, result.AttackPercent);
            Assert.Equal(15.0, result.CritChancePercent);
        }

        #endregion

        #region Helper Methods

        private static CombatStats CreateBaseStats()
        {
            return new CombatStats
            {
                AttackFinal = 100,
                AttackPercent = 50.0,
                SpecialAttackPercent = 0,
                HPPercent = 0,
                HastePercent = 0,
                CritChancePercent = 10.0,
                CritDamageBonusPercent = 20.0,
                FortifyMaxPercent = 0,
                BackwaterMaxPercent = 0,
                ChasePercent = 0,
                KenChasePercent = 0,
                ChaseFlat = 0,
                DamageReductionPercent = 0
            };
        }

        #endregion

        #region Mock BuffOwner

        private class MockBuffOwner : IBuffOwner
        {
            private readonly Dictionary<string, BuffInstance> _buffs = new();

            public string Id => "mock_player";
            public bool IsPlayer => true;
            public int CurrentHp => 100;
            public int MaxHp => 100;
            public BlazorIdle.Game.Resources.ResourceBucketCollection? Buckets => null;
            public IReadOnlyDictionary<string, BuffInstance> Buffs => _buffs;

            public void AddBuff(BuffInstance buff)
            {
                _buffs[buff.Id] = buff;
            }

            public void ApplyBuff(BuffInstance buff)
            {
                _buffs[buff.Id] = buff;
            }

            public bool RemoveBuff(string buffId, string reason)
            {
                return _buffs.Remove(buffId);
            }

            public bool ReduceBuffStacks(string buffId, int stacksToRemove, string reason)
            {
                return false;
            }

            public void ReceiveDamage(int amount, DamageMeta meta) { }
            public void ReceiveHeal(int amount, HealMeta meta) { }
        }

        #endregion
    }
}
