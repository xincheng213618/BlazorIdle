using Xunit;
using BlazorIdle.Game.Skills;
using BlazorIdle.Game.Combat;
using System.Collections.Generic;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 核心战斗系统重构 Phase 1 测试 - 多段伤害数据结构
    /// Core Battle System Refactoring Phase 1 Tests - Multi-Hit Damage Data Structures
    /// </summary>
    public class BattleRefactorPhase1Tests
    {
        #region DamageHit Tests

        [Fact]
        public void DamageHit_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var hit = new DamageHit();

            // Assert
            Assert.Equal(0, hit.HitIndex);
            Assert.Equal(1.0, hit.CoefAtk);
            Assert.Equal(0, hit.Flat);
            Assert.Equal(0, hit.DelaySec);
            Assert.True(hit.IndependentCrit);
            Assert.Null(hit.TargetPolicy);
        }

        [Fact]
        public void DamageHit_Clone_CreatesExactCopy()
        {
            // Arrange
            var original = new DamageHit
            {
                HitIndex = 2,
                CoefAtk = 0.5,
                Flat = 10,
                DelaySec = 0.3,
                IndependentCrit = false,
                TargetPolicy = "enemies_all"
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotSame(original, clone);
            Assert.Equal(original.HitIndex, clone.HitIndex);
            Assert.Equal(original.CoefAtk, clone.CoefAtk);
            Assert.Equal(original.Flat, clone.Flat);
            Assert.Equal(original.DelaySec, clone.DelaySec);
            Assert.Equal(original.IndependentCrit, clone.IndependentCrit);
            Assert.Equal(original.TargetPolicy, clone.TargetPolicy);
        }

        [Fact]
        public void DamageHit_FromDamageDef_ConvertsCorrectly()
        {
            // Arrange
            var damageDef = new DamageDef
            {
                CoefAtk = 1.5,
                Flat = 25
            };

            // Act
            var hit = DamageHit.FromDamageDef(damageDef);

            // Assert
            Assert.Equal(0, hit.HitIndex);
            Assert.Equal(1.5, hit.CoefAtk);
            Assert.Equal(25, hit.Flat);
            Assert.Equal(0, hit.DelaySec);
            Assert.True(hit.IndependentCrit);
            Assert.Null(hit.TargetPolicy);
        }

        [Fact]
        public void DamageHit_FromDamageDef_HandlesNull()
        {
            // Act
            var hit = DamageHit.FromDamageDef(null);

            // Assert - Should return default values
            Assert.Equal(0, hit.HitIndex);
            Assert.Equal(1.0, hit.CoefAtk);
            Assert.Equal(0, hit.Flat);
        }

        #endregion

        #region DamageInstance Tests

        [Fact]
        public void DamageInstance_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var instance = new DamageInstance();

            // Assert
            Assert.Equal(0, instance.HitIndex);
            Assert.Equal(0, instance.Damage);
            Assert.False(instance.IsCrit);
            Assert.Equal("", instance.TargetId);
            Assert.Equal(0, instance.ApplyAtSec);
            Assert.Null(instance.DetailedResult);
            Assert.Equal("", instance.SkillId);
            Assert.Equal("", instance.CasterId);
            Assert.True(instance.IsCasterPlayer);
            Assert.Null(instance.BundleId);
            Assert.False(instance.HasElementAdvantage);
            Assert.Equal(1.0, instance.ElementMultiplier);
        }

        [Fact]
        public void DamageInstance_IsPending_ReturnsTrueWhenDelayed()
        {
            // Arrange
            var instance = new DamageInstance { ApplyAtSec = 0.5 };

            // Assert
            Assert.True(instance.IsPending);
            Assert.False(instance.IsImmediate);
        }

        [Fact]
        public void DamageInstance_IsImmediate_ReturnsTrueWhenNotDelayed()
        {
            // Arrange
            var instance = new DamageInstance { ApplyAtSec = 0 };

            // Assert
            Assert.True(instance.IsImmediate);
            Assert.False(instance.IsPending);
        }

        [Fact]
        public void DamageInstance_Clone_CreatesExactCopy()
        {
            // Arrange
            var original = new DamageInstance
            {
                HitIndex = 1,
                Damage = 100,
                IsCrit = true,
                TargetId = "enemy_1",
                ApplyAtSec = 0.5,
                SkillId = "triple_slash",
                CasterId = "player_1",
                IsCasterPlayer = true,
                BundleId = "bundle_123",
                HasElementAdvantage = true,
                ElementMultiplier = 1.5
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotSame(original, clone);
            Assert.Equal(original.HitIndex, clone.HitIndex);
            Assert.Equal(original.Damage, clone.Damage);
            Assert.Equal(original.IsCrit, clone.IsCrit);
            Assert.Equal(original.TargetId, clone.TargetId);
            Assert.Equal(original.ApplyAtSec, clone.ApplyAtSec);
            Assert.Equal(original.SkillId, clone.SkillId);
            Assert.Equal(original.CasterId, clone.CasterId);
            Assert.Equal(original.IsCasterPlayer, clone.IsCasterPlayer);
            Assert.Equal(original.BundleId, clone.BundleId);
            Assert.Equal(original.HasElementAdvantage, clone.HasElementAdvantage);
            Assert.Equal(original.ElementMultiplier, clone.ElementMultiplier);
        }

        #endregion

        #region SkillDef.Hits Tests

        [Fact]
        public void SkillDef_IsMultiHit_ReturnsFalseByDefault()
        {
            // Arrange
            var skill = new SkillDef();

            // Assert
            Assert.False(skill.IsMultiHit);
            Assert.Null(skill.Hits);
        }

        [Fact]
        public void SkillDef_IsMultiHit_ReturnsFalseWhenHitsEmpty()
        {
            // Arrange
            var skill = new SkillDef
            {
                Hits = new List<DamageHit>()
            };

            // Assert
            Assert.False(skill.IsMultiHit);
        }

        [Fact]
        public void SkillDef_IsMultiHit_ReturnsTrueWhenHitsHasElements()
        {
            // Arrange
            var skill = new SkillDef
            {
                Hits = new List<DamageHit>
                {
                    new DamageHit { HitIndex = 0, CoefAtk = 0.5 },
                    new DamageHit { HitIndex = 1, CoefAtk = 0.5 },
                    new DamageHit { HitIndex = 2, CoefAtk = 0.5 }
                }
            };

            // Assert
            Assert.True(skill.IsMultiHit);
            Assert.Equal(3, skill.Hits.Count);
        }

        [Fact]
        public void SkillDef_SingleDamage_StillWorks()
        {
            // Arrange - 向后兼容测试
            var skill = new SkillDef
            {
                Damage = new DamageDef { CoefAtk = 1.2, Flat = 10 }
            };

            // Assert
            Assert.False(skill.IsMultiHit);
            Assert.NotNull(skill.Damage);
            Assert.Equal(1.2, skill.Damage.CoefAtk);
            Assert.Equal(10, skill.Damage.Flat);
        }

        #endregion

        #region SkillCastResult Tests

        [Fact]
        public void SkillCastResult_HasMultiHit_ReturnsFalseByDefault()
        {
            // Arrange
            var result = new SkillCastResult();

            // Assert
            Assert.False(result.HasMultiHit);
            Assert.Null(result.DamageInstances);
        }

        [Fact]
        public void SkillCastResult_HasMultiHit_ReturnsFalseWhenEmpty()
        {
            // Arrange
            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>()
            };

            // Assert
            Assert.False(result.HasMultiHit);
        }

        [Fact]
        public void SkillCastResult_HasMultiHit_ReturnsTrueWhenHasInstances()
        {
            // Arrange
            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 50 },
                    new DamageInstance { Damage = 50 },
                    new DamageInstance { Damage = 50 }
                }
            };

            // Assert
            Assert.True(result.HasMultiHit);
        }

        [Fact]
        public void SkillCastResult_TotalDamage_SumsAllInstances()
        {
            // Arrange
            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 30 },
                    new DamageInstance { Damage = 40 },
                    new DamageInstance { Damage = 50 }
                }
            };

            // Assert
            Assert.Equal(120, result.TotalDamage);
        }

        [Fact]
        public void SkillCastResult_TotalDamage_ReturnsDamageDealtWhenNoInstances()
        {
            // Arrange - 向后兼容
            var result = new SkillCastResult
            {
                DamageDealt = 100
            };

            // Assert
            Assert.Equal(100, result.TotalDamage);
        }

        [Fact]
        public void SkillCastResult_HasPendingDamage_DetectsDelayedDamage()
        {
            // Arrange
            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 50, ApplyAtSec = 0 },
                    new DamageInstance { Damage = 50, ApplyAtSec = 0.5 } // Delayed
                }
            };

            // Assert
            Assert.True(result.HasPendingDamage);
        }

        [Fact]
        public void SkillCastResult_HasPendingDamage_ReturnsFalseWhenAllImmediate()
        {
            // Arrange
            var result = new SkillCastResult
            {
                DamageInstances = new List<DamageInstance>
                {
                    new DamageInstance { Damage = 50, ApplyAtSec = 0 },
                    new DamageInstance { Damage = 50, ApplyAtSec = 0 }
                }
            };

            // Assert
            Assert.False(result.HasPendingDamage);
        }

        [Fact]
        public void SkillCastResult_BackwardCompatibility_DamageDealtAndIsCritStillWork()
        {
            // Arrange
            var result = new SkillCastResult
            {
                DamageDealt = 150,
                IsCrit = true
            };

            // Assert
            Assert.Equal(150, result.DamageDealt);
            Assert.True(result.IsCrit);
            Assert.Equal(150, result.TotalDamage); // Falls back to DamageDealt
            Assert.False(result.HasMultiHit);
            Assert.False(result.HasPendingDamage);
        }

        #endregion

        #region Multi-Hit Skill Configuration Tests

        [Fact]
        public void TripleSlash_SkillConfiguration_IsValid()
        {
            // Arrange - 模拟三连斩配置
            var tripleSlash = new SkillDef
            {
                Id = "triple_slash",
                Name = "三连斩",
                ReleaseType = "instant",
                IsGcd = true,
                CooldownSec = 8.0,
                Hits = new List<DamageHit>
                {
                    new DamageHit { HitIndex = 0, CoefAtk = 0.5, DelaySec = 0 },
                    new DamageHit { HitIndex = 1, CoefAtk = 0.5, DelaySec = 0.2 },
                    new DamageHit { HitIndex = 2, CoefAtk = 0.5, DelaySec = 0.4 }
                }
            };

            // Assert
            Assert.True(tripleSlash.IsMultiHit);
            Assert.Equal(3, tripleSlash.Hits!.Count);
            
            // 第一段立即生效
            Assert.Equal(0, tripleSlash.Hits[0].DelaySec);
            
            // 后续段有延迟
            Assert.True(tripleSlash.Hits[1].DelaySec > 0);
            Assert.True(tripleSlash.Hits[2].DelaySec > 0);
            
            // 总系数应为 150%
            double totalCoef = 0;
            foreach (var hit in tripleSlash.Hits)
            {
                totalCoef += hit.CoefAtk;
            }
            Assert.Equal(1.5, totalCoef, 0.001);
        }

        [Fact]
        public void DelayedExplosion_SkillConfiguration_IsValid()
        {
            // Arrange - 模拟延迟爆炸配置
            var explosion = new SkillDef
            {
                Id = "delayed_explosion",
                Name = "延迟爆炸",
                ReleaseType = "cast",
                CastTimeSec = 0.5,
                Hits = new List<DamageHit>
                {
                    new DamageHit { HitIndex = 0, CoefAtk = 2.0, Flat = 50, DelaySec = 1.5 }
                }
            };

            // Assert
            Assert.True(explosion.IsMultiHit);
            Assert.Equal(1, explosion.Hits!.Count);
            
            // 只有一段但有延迟
            Assert.Equal(1.5, explosion.Hits[0].DelaySec);
            Assert.Equal(2.0, explosion.Hits[0].CoefAtk);
            Assert.Equal(50, explosion.Hits[0].Flat);
        }

        [Fact]
        public void Barrage_SkillConfiguration_IsValid()
        {
            // Arrange - 模拟弹幕配置
            var barrage = new SkillDef
            {
                Id = "barrage",
                Name = "弹幕",
                ReleaseType = "instant",
                TargetPolicy = "enemies_all",
                Hits = new List<DamageHit>
                {
                    new DamageHit { HitIndex = 0, CoefAtk = 0.3, DelaySec = 0 },
                    new DamageHit { HitIndex = 1, CoefAtk = 0.3, DelaySec = 0.1 },
                    new DamageHit { HitIndex = 2, CoefAtk = 0.3, DelaySec = 0.2 },
                    new DamageHit { HitIndex = 3, CoefAtk = 0.3, DelaySec = 0.3 },
                    new DamageHit { HitIndex = 4, CoefAtk = 0.3, DelaySec = 0.4 }
                }
            };

            // Assert
            Assert.True(barrage.IsMultiHit);
            Assert.Equal(5, barrage.Hits!.Count);
            Assert.Equal("enemies_all", barrage.TargetPolicy);
            
            // 总系数应为 150%
            double totalCoef = 0;
            foreach (var hit in barrage.Hits)
            {
                totalCoef += hit.CoefAtk;
            }
            Assert.Equal(1.5, totalCoef, 0.001);
            
            // 延迟递增
            for (int i = 1; i < barrage.Hits.Count; i++)
            {
                Assert.True(barrage.Hits[i].DelaySec > barrage.Hits[i - 1].DelaySec);
            }
        }

        #endregion
    }
}
