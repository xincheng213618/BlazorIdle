using BlazorIdle.Game;
using Xunit;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// 测试 RngContext 的确定性行为
    /// Stage 0.2: 确保RNG确定性
    /// </summary>
    public class RngContextDeterminismTests
    {
        #region 基础确定性测试

        [Fact(DisplayName = "相同种子产生相同序列")]
        public void SameSeed_ProducesSameSequence()
        {
            // Arrange
            const int seed = 12345;
            const int iterations = 100;

            var rng1 = new RngContext(seed);
            var rng2 = new RngContext(seed);

            // Act & Assert
            for (int i = 0; i < iterations; i++)
            {
                double val1 = rng1.NextDouble();
                double val2 = rng2.NextDouble();

                Assert.Equal(val1, val2);
                Assert.Equal(i + 1, rng1.Index);
                Assert.Equal(i + 1, rng2.Index);
            }
        }

        [Fact(DisplayName = "不同种子产生不同序列")]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            // Arrange
            var rng1 = new RngContext(12345);
            var rng2 = new RngContext(54321);

            // Act
            var val1 = rng1.NextDouble();
            var val2 = rng2.NextDouble();

            // Assert
            Assert.NotEqual(val1, val2);
        }

        [Fact(DisplayName = "种子为0时自动转换为1")]
        public void ZeroSeed_AutomaticallyConvertsToOne()
        {
            // Arrange & Act
            var rng = new RngContext(0);

            // Assert
            Assert.Equal(1, rng.Seed);
        }

        #endregion

        #region NextDouble 确定性测试

        [Theory(DisplayName = "NextDouble 多次调用返回确定值")]
        [InlineData(42)]
        [InlineData(12345)]
        [InlineData(999999)]
        [InlineData(-12345)]
        public void NextDouble_MultipleCallsWithSameSeed_ReturnsDeterministicValues(int seed)
        {
            // Arrange
            var rng1 = new RngContext(seed);
            var rng2 = new RngContext(seed);

            // Act
            var values1 = new double[50];
            var values2 = new double[50];

            for (int i = 0; i < 50; i++)
            {
                values1[i] = rng1.NextDouble();
                values2[i] = rng2.NextDouble();
            }

            // Assert
            for (int i = 0; i < 50; i++)
            {
                Assert.Equal(values1[i], values2[i]);
            }
        }

        [Fact(DisplayName = "NextDouble 返回值在 [0, 1) 范围内")]
        public void NextDouble_ReturnsValuesInRange()
        {
            // Arrange
            var rng = new RngContext(42);

            // Act & Assert
            for (int i = 0; i < 1000; i++)
            {
                double value = rng.NextDouble();
                Assert.InRange(value, 0.0, 1.0);
                Assert.True(value < 1.0, "Value should be strictly less than 1.0");
            }
        }

        #endregion

        #region NextRange 确定性测试

        [Theory(DisplayName = "NextRange 相同种子产生相同整数序列")]
        [InlineData(1, 100)]
        [InlineData(-50, 50)]
        [InlineData(0, 10)]
        public void NextRange_SameSeed_ProducesSameIntegerSequence(int min, int max)
        {
            // Arrange
            const int seed = 777;
            var rng1 = new RngContext(seed);
            var rng2 = new RngContext(seed);

            // Act & Assert
            for (int i = 0; i < 50; i++)
            {
                int val1 = rng1.NextRange(min, max);
                int val2 = rng2.NextRange(min, max);

                Assert.Equal(val1, val2);
                Assert.InRange(val1, min, max);
            }
        }

        [Fact(DisplayName = "NextRange 最小值大于最大值时自动交换")]
        public void NextRange_SwapsMinMaxIfReversed()
        {
            // Arrange
            var rng1 = new RngContext(42);
            var rng2 = new RngContext(42);

            // Act
            int val1 = rng1.NextRange(100, 1);  // 反向
            int val2 = rng2.NextRange(1, 100);  // 正向

            // Assert
            Assert.Equal(val1, val2);
            Assert.InRange(val1, 1, 100);
        }

        [Fact(DisplayName = "NextRange 单值范围总是返回该值")]
        public void NextRange_SingleValueRange_AlwaysReturnsThatValue()
        {
            // Arrange
            var rng = new RngContext(42);
            const int value = 77;

            // Act & Assert
            for (int i = 0; i < 20; i++)
            {
                Assert.Equal(value, rng.NextRange(value, value));
            }
        }

        #endregion

        #region Jitter 确定性测试

        [Fact(DisplayName = "Jitter 相同种子产生相同抖动值")]
        public void Jitter_SameSeed_ProducesSameJitteredValues()
        {
            // Arrange
            const int seed = 999;
            const double baseValue = 100.0;
            const double pct = 0.1; // ±10%

            var rng1 = new RngContext(seed);
            var rng2 = new RngContext(seed);

            // Act & Assert
            for (int i = 0; i < 30; i++)
            {
                double val1 = rng1.Jitter(baseValue, pct);
                double val2 = rng2.Jitter(baseValue, pct);

                Assert.Equal(val1, val2);
                
                // 验证抖动范围
                double minExpected = baseValue * (1 - pct);
                double maxExpected = baseValue * (1 + pct);
                Assert.InRange(val1, minExpected, maxExpected);
            }
        }

        [Theory(DisplayName = "Jitter 不同基础值产生正确抖动范围")]
        [InlineData(50.0, 0.05)]
        [InlineData(1000.0, 0.2)]
        [InlineData(10.5, 0.15)]
        public void Jitter_DifferentBaseValues_ProducesCorrectRange(double baseValue, double pct)
        {
            // Arrange
            var rng = new RngContext(42);
            double minExpected = baseValue * (1 - pct);
            double maxExpected = baseValue * (1 + pct);

            // Act & Assert
            for (int i = 0; i < 100; i++)
            {
                double jittered = rng.Jitter(baseValue, pct);
                Assert.InRange(jittered, minExpected, maxExpected);
            }
        }

        #endregion

        #region Index 跟踪测试

        [Fact(DisplayName = "Index 正确跟踪RNG调用次数")]
        public void Index_CorrectlyTracksNumberOfCalls()
        {
            // Arrange
            var rng = new RngContext(42);

            // Assert initial state
            Assert.Equal(0, rng.Index);

            // Act & Assert progressive calls
            rng.NextDouble();
            Assert.Equal(1, rng.Index);

            rng.NextDouble();
            Assert.Equal(2, rng.Index);

            rng.NextRange(1, 10);
            Assert.Equal(3, rng.Index);

            rng.Jitter(100, 0.1);
            Assert.Equal(4, rng.Index);
        }

        [Fact(DisplayName = "多个RNG实例的Index独立")]
        public void Index_IsIndependentAcrossInstances()
        {
            // Arrange
            var rng1 = new RngContext(42);
            var rng2 = new RngContext(42);

            // Act
            rng1.NextDouble();
            rng1.NextDouble();
            rng1.NextDouble();

            rng2.NextDouble();

            // Assert
            Assert.Equal(3, rng1.Index);
            Assert.Equal(1, rng2.Index);
        }

        #endregion

        #region 序列化支持测试（为持久化准备）

        [Fact(DisplayName = "Seed 属性可被读取用于序列化")]
        public void Seed_CanBeReadForSerialization()
        {
            // Arrange
            const int seed = 54321;
            var rng = new RngContext(seed);

            // Act
            int savedSeed = rng.Seed;

            // Assert
            Assert.Equal(seed, savedSeed);
        }

        [Fact(DisplayName = "通过保存的Seed可重建相同序列")]
        public void SavedSeed_CanReconstructSameSequence()
        {
            // Arrange
            const int originalSeed = 77777;
            var originalRng = new RngContext(originalSeed);

            // 生成一些值
            var originalValues = new double[20];
            for (int i = 0; i < originalValues.Length; i++)
            {
                originalValues[i] = originalRng.NextDouble();
            }

            // Act - 使用保存的种子重建
            int savedSeed = originalRng.Seed;
            var reconstructedRng = new RngContext(savedSeed);

            var reconstructedValues = new double[20];
            for (int i = 0; i < reconstructedValues.Length; i++)
            {
                reconstructedValues[i] = reconstructedRng.NextDouble();
            }

            // Assert
            for (int i = 0; i < originalValues.Length; i++)
            {
                Assert.Equal(originalValues[i], reconstructedValues[i]);
            }
        }

        #endregion

        #region 战斗模拟场景测试

        [Fact(DisplayName = "模拟战斗场景 - 相同种子产生相同战斗结果")]
        public void BattleSimulation_SameSeed_ProducesSameBattleResults()
        {
            // Arrange - 模拟战斗中的随机调用
            const int seed = 11111;

            // 第一次战斗模拟
            var battle1 = SimulateBattle(seed);

            // 第二次战斗模拟
            var battle2 = SimulateBattle(seed);

            // Assert - 所有结果应该完全相同
            Assert.Equal(battle1.criticalHits, battle2.criticalHits);
            Assert.Equal(battle1.totalDamage, battle2.totalDamage);
            Assert.Equal(battle1.dodges, battle2.dodges);
            Assert.Equal(battle1.turnCount, battle2.turnCount);
        }

        [Fact(DisplayName = "不同种子模拟产生不同但确定的结果")]
        public void BattleSimulation_DifferentSeeds_ProduceDifferentResults()
        {
            // Arrange & Act
            var battle1 = SimulateBattle(12345);
            var battle2 = SimulateBattle(54321);

            // Assert - 结果应该不同
            bool resultsDiffer = 
                battle1.criticalHits != battle2.criticalHits ||
                battle1.totalDamage != battle2.totalDamage ||
                battle1.dodges != battle2.dodges;

            Assert.True(resultsDiffer, "不同种子应该产生不同的战斗结果");
        }

        /// <summary>
        /// 模拟简化的战斗过程
        /// </summary>
        private (int criticalHits, double totalDamage, int dodges, int turnCount) SimulateBattle(int seed)
        {
            var rng = new RngContext(seed);
            int criticalHits = 0;
            double totalDamage = 0;
            int dodges = 0;
            int turnCount = 0;

            // 模拟10回合战斗
            for (int turn = 0; turn < 10; turn++)
            {
                turnCount++;

                // 命中检测
                if (rng.NextDouble() > 0.1) // 90% 命中率
                {
                    // 基础伤害
                    double baseDamage = rng.NextRange(10, 20);

                    // 暴击检测
                    if (rng.NextDouble() < 0.15) // 15% 暴击率
                    {
                        criticalHits++;
                        baseDamage *= 2.0;
                    }

                    // 伤害抖动 ±5%
                    double actualDamage = rng.Jitter(baseDamage, 0.05);
                    totalDamage += actualDamage;
                }
                else
                {
                    dodges++;
                }
            }

            return (criticalHits, totalDamage, dodges, turnCount);
        }

        #endregion

        #region 边界情况测试

        [Fact(DisplayName = "大种子值正常工作")]
        public void LargeSeedValue_WorksCorrectly()
        {
            // Arrange & Act
            var rng = new RngContext(int.MaxValue);

            // Assert
            double value = rng.NextDouble();
            Assert.InRange(value, 0.0, 1.0);
        }

        [Fact(DisplayName = "负种子值正常工作")]
        public void NegativeSeedValue_WorksCorrectly()
        {
            // Arrange & Act
            var rng = new RngContext(int.MinValue);

            // Assert
            double value = rng.NextDouble();
            Assert.InRange(value, 0.0, 1.0);
        }

        [Fact(DisplayName = "连续1000次调用保持确定性")]
        public void ThousandConsecutiveCalls_MaintainDeterminism()
        {
            // Arrange
            const int seed = 42;
            var rng1 = new RngContext(seed);
            var rng2 = new RngContext(seed);

            // Act & Assert
            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal(rng1.NextDouble(), rng2.NextDouble());
            }

            Assert.Equal(1000, rng1.Index);
            Assert.Equal(1000, rng2.Index);
        }

        #endregion
    }
}
