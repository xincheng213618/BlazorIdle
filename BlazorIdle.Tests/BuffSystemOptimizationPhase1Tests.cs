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

        #region Monster BaseAttack Tests

        /// <summary>
        /// 验证怪物 BaseAttack 正确加载
        /// Verify monster BaseAttack loads correctly
        /// </summary>
        [Fact]
        public void MonsterDef_ShouldLoadCorrectBaseAttack()
        {
            // Arrange - 加载怪物配置
            var monsters = BlazorIdle.Game.Config.ConfigRepository.LoadMonsters();
            
            // 查找测试怪物
            var slime = monsters.Find(m => m.Id == "slime");
            var wolf = monsters.Find(m => m.Id == "wolf");
            var ogre = monsters.Find(m => m.Id == "ogre");
            
            // Assert - 怪物应该存在
            Assert.NotNull(slime);
            Assert.NotNull(wolf);
            Assert.NotNull(ogre);
            
            // Assert - 验证 BaseAttack 值与 monsters.json 匹配
            // slime: baseAttack=100, wolf: baseAttack=130, ogre: baseAttack=180
            Assert.Equal(100, slime.BaseAttack);
            Assert.Equal(130, wolf.BaseAttack);
            Assert.Equal(180, ogre.BaseAttack);
            
            // Assert - 三个怪物的攻击力应该不同
            Assert.NotEqual(slime.BaseAttack, wolf.BaseAttack);
            Assert.NotEqual(wolf.BaseAttack, ogre.BaseAttack);
        }

        /// <summary>
        /// 验证怪物技能的伤害计算使用正确的 BaseAttack
        /// Verify monster skill damage calculation uses correct BaseAttack
        /// </summary>
        [Fact]
        public void MonsterSkillDamage_ShouldUseCorrectBaseAttack()
        {
            // Arrange - 创建测试场景
            var monsters = BlazorIdle.Game.Config.ConfigRepository.LoadMonsters();
            var slimeDef = monsters.Find(m => m.Id == "slime");
            var wolfDef = monsters.Find(m => m.Id == "wolf");
            var ogreDef = monsters.Find(m => m.Id == "ogre");
            
            Assert.NotNull(slimeDef);
            Assert.NotNull(wolfDef);
            Assert.NotNull(ogreDef);
            
            // 创建 DamageCalculator
            var calculator = DamageCalculator.CreateDefault();
            
            // 测试 Slime (BaseAttack=100)
            var slimeStats = new CombatStats { AttackFinal = (int)slimeDef.BaseAttack };
            var slimeCtx = new DamageContext
            {
                AttackFinal = slimeStats.AttackFinal,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = slimeStats,
                AttackerHPRatio = 1.0,
                AttackerElement = "neutral",
                DefenderElement = "neutral",
                DefenderDRPct = 0,
                Rng = new Random(42)
            };
            var slimeResult = calculator.CalculateDeterministic(slimeCtx);
            
            // 测试 Wolf (BaseAttack=130)
            var wolfStats = new CombatStats { AttackFinal = (int)wolfDef.BaseAttack };
            var wolfCtx = new DamageContext
            {
                AttackFinal = wolfStats.AttackFinal,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = wolfStats,
                AttackerHPRatio = 1.0,
                AttackerElement = "neutral",
                DefenderElement = "neutral",
                DefenderDRPct = 0,
                Rng = new Random(42)
            };
            var wolfResult = calculator.CalculateDeterministic(wolfCtx);
            
            // 测试 Ogre (BaseAttack=180)
            var ogreStats = new CombatStats { AttackFinal = (int)ogreDef.BaseAttack };
            var ogreCtx = new DamageContext
            {
                AttackFinal = ogreStats.AttackFinal,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = ogreStats,
                AttackerHPRatio = 1.0,
                AttackerElement = "neutral",
                DefenderElement = "neutral",
                DefenderDRPct = 0,
                Rng = new Random(42)
            };
            var ogreResult = calculator.CalculateDeterministic(ogreCtx);
            
            // Assert - 伤害应该与 BaseAttack 成比例
            Assert.True(slimeResult.FinalDamage < wolfResult.FinalDamage, 
                $"Slime damage ({slimeResult.FinalDamage}) should be less than Wolf damage ({wolfResult.FinalDamage})");
            Assert.True(wolfResult.FinalDamage < ogreResult.FinalDamage,
                $"Wolf damage ({wolfResult.FinalDamage}) should be less than Ogre damage ({ogreResult.FinalDamage})");
            
            // 验证实际伤害值（中间值 variance = 1.0）
            Assert.Equal(100, slimeResult.FinalDamage);
            Assert.Equal(130, wolfResult.FinalDamage);
            Assert.Equal(180, ogreResult.FinalDamage);
        }

        /// <summary>
        /// 验证 Buff 的 AttackPercent 能够正确应用到伤害计算
        /// Verify Buff's AttackPercent is correctly applied to damage calculation
        /// </summary>
        [Fact]
        public void BuffAttackPercent_ShouldIncreaseMainLayerDamage()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            
            // 基础属性（无 Buff）
            var baseStats = new CombatStats 
            { 
                AttackFinal = 100,
                AttackPercent = 0  // 没有攻击力加成
            };
            
            // 应用了 50% 攻击力 Buff 的属性
            var buffedStats = new CombatStats 
            { 
                AttackFinal = 100,
                AttackPercent = 50  // 50% 攻击力加成
            };
            
            // 无 Buff 的伤害上下文
            var baseCtx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = baseStats,
                AttackerHPRatio = 1.0,
                AttackerElement = "neutral",
                DefenderElement = "neutral",
                DefenderDRPct = 0,
                Rng = new Random(42)
            };
            
            // 有 Buff 的伤害上下文
            var buffedCtx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = buffedStats,
                AttackerHPRatio = 1.0,
                AttackerElement = "neutral",
                DefenderElement = "neutral",
                DefenderDRPct = 0,
                Rng = new Random(42)
            };
            
            // Act
            var baseResult = calculator.CalculateDeterministic(baseCtx);
            var buffedResult = calculator.CalculateDeterministic(buffedCtx);
            
            // Assert
            // 基础伤害：100 × 1.0 × 1.0（无攻击%） = 100
            // Buff 伤害：100 × 1.0 × 1.5（50%攻击%） = 150
            Assert.Equal(100, baseResult.FinalDamage);
            Assert.Equal(150, buffedResult.FinalDamage);
            
            // 验证 Buff 效果正确应用（50% 增加）
            Assert.Equal(1.5, (double)buffedResult.FinalDamage / baseResult.FinalDamage, 1);
        }

        /// <summary>
        /// 验证 Buff 效果能够突破装备上限
        /// Verify Buff effects can exceed equipment caps
        /// </summary>
        [Fact]
        public void BuffAttackPercent_CanExceedEquipmentCaps()
        {
            // Arrange
            var calculator = DamageCalculator.CreateDefault();
            
            // 已达到装备上限（100%）+ Buff 增加 50%
            // 如果上限生效，应该只有 100%；如果 Buff 不受限制，应该是 150%
            var statsWithExceededCap = new CombatStats 
            { 
                AttackFinal = 100,
                AttackPercent = 150  // 装备100% + Buff 50% = 150%（超过上限）
            };
            
            var ctx = new DamageContext
            {
                AttackFinal = 100,
                SkillCoef = 1.0,
                SkillFlat = 0,
                AttackerStats = statsWithExceededCap,
                AttackerHPRatio = 1.0,
                AttackerElement = "neutral",
                DefenderElement = "neutral",
                DefenderDRPct = 0,
                Rng = new Random(42)
            };
            
            // Act
            var result = calculator.CalculateDeterministic(ctx);
            
            // Assert
            // 如果上限（100%）生效：100 × 2.0 = 200
            // 如果不裁剪（150%）：100 × 2.5 = 250
            Assert.Equal(250, result.FinalDamage);  // Buff 效果突破了上限
        }

        #endregion
    }
}
