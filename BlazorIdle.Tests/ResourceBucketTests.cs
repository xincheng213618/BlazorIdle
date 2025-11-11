using Xunit;
using BlazorIdle.Game.Resources;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 1 单元测试：ResourceBucket 资源系统基础
    /// Phase 1 unit tests: ResourceBucket resource system basics
    /// </summary>
    public class ResourceBucketTests
    {
        [Fact]
        public void ResourceBucket_Constructor_InitializesWithDefaults()
        {
            // Arrange & Act
            var bucket = new ResourceBucket("rage");

            // Assert
            Assert.Equal("rage", bucket.Id);
            Assert.Equal(10, bucket.Max);
            Assert.Equal(0, bucket.Current);
        }

        [Fact]
        public void ResourceBucket_Constructor_InitializesWithCustomValues()
        {
            // Arrange & Act
            var bucket = new ResourceBucket("energy", max: 100, initial: 50);

            // Assert
            Assert.Equal("energy", bucket.Id);
            Assert.Equal(100, bucket.Max);
            Assert.Equal(50, bucket.Current);
        }

        [Fact]
        public void ResourceBucket_Constructor_ClampsInitialValueToMax()
        {
            // Arrange & Act
            var bucket = new ResourceBucket("rage", max: 10, initial: 15);

            // Assert
            Assert.Equal(10, bucket.Current); // Clamped to max
        }

        [Fact]
        public void ResourceBucket_Gain_AddsResourceCorrectly()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 3);

            // Act
            int gained = bucket.Gain(4, "test_gain");

            // Assert
            Assert.Equal(4, gained);
            Assert.Equal(7, bucket.Current);
        }

        [Fact]
        public void ResourceBucket_Gain_ClampsToMax()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 8);

            // Act
            int gained = bucket.Gain(5, "test_gain");

            // Assert
            Assert.Equal(2, gained); // 只能获得 2（8 + 2 = 10）
            Assert.Equal(10, bucket.Current);
        }

        [Fact]
        public void ResourceBucket_Gain_WithZeroAmount_ReturnsZero()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 5);

            // Act
            int gained = bucket.Gain(0, "test_gain");

            // Assert
            Assert.Equal(0, gained);
            Assert.Equal(5, bucket.Current); // Unchanged
        }

        [Fact]
        public void ResourceBucket_Gain_WithNegativeAmount_ReturnsZero()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 5);

            // Act
            int gained = bucket.Gain(-3, "test_gain");

            // Assert
            Assert.Equal(0, gained);
            Assert.Equal(5, bucket.Current); // Unchanged
        }

        [Fact]
        public void ResourceBucket_TryConsume_SucceedsWhenEnough()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 5);

            // Act
            bool success = bucket.TryConsume(3, "test_consume");

            // Assert
            Assert.True(success);
            Assert.Equal(2, bucket.Current);
        }

        [Fact]
        public void ResourceBucket_TryConsume_FailsWhenInsufficient()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 2);

            // Act
            bool success = bucket.TryConsume(5, "test_consume");

            // Assert
            Assert.False(success);
            Assert.Equal(2, bucket.Current); // 未变化
        }

        [Fact]
        public void ResourceBucket_TryConsume_SucceedsWhenExactAmount()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 5);

            // Act
            bool success = bucket.TryConsume(5, "test_consume");

            // Assert
            Assert.True(success);
            Assert.Equal(0, bucket.Current);
        }

        [Fact]
        public void ResourceBucket_TryConsume_WithNegativeAmount_ReturnsFalse()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 5);

            // Act
            bool success = bucket.TryConsume(-3, "test_consume");

            // Assert
            Assert.False(success);
            Assert.Equal(5, bucket.Current); // Unchanged
        }

        [Fact]
        public void ResourceBucket_ForceConsume_ReducesResourceToZero()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 3);

            // Act
            bucket.ForceConsume(5, "test_force");

            // Assert
            Assert.Equal(0, bucket.Current); // Clamped to 0
        }

        [Fact]
        public void ResourceBucket_SetMax_ClampsCurrentValue()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 8);

            // Act
            bucket.SetMax(5);

            // Assert
            Assert.Equal(5, bucket.Max);
            Assert.Equal(5, bucket.Current); // 被 clamp 到新上限
        }

        [Fact]
        public void ResourceBucket_SetMax_DoesNotAffectCurrentIfBelowMax()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 3);

            // Act
            bucket.SetMax(15);

            // Assert
            Assert.Equal(15, bucket.Max);
            Assert.Equal(3, bucket.Current); // 未变化
        }

        [Fact]
        public void ResourceBucket_SetMax_WithNegativeValue_SetsToZero()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 5);

            // Act
            bucket.SetMax(-5);

            // Assert
            Assert.Equal(0, bucket.Max);
            Assert.Equal(0, bucket.Current);
        }

        [Fact]
        public void ResourceBucket_Reset_SetsCurrentToValue()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 5);

            // Act
            bucket.Reset(7);

            // Assert
            Assert.Equal(7, bucket.Current);
        }

        [Fact]
        public void ResourceBucket_Reset_DefaultsToZero()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 5);

            // Act
            bucket.Reset();

            // Assert
            Assert.Equal(0, bucket.Current);
        }

        [Fact]
        public void ResourceBucket_Reset_ClampsToMax()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 5);

            // Act
            bucket.Reset(15);

            // Assert
            Assert.Equal(10, bucket.Current); // Clamped to max
        }

        [Fact]
        public void ResourceBucket_MultipleOperations_WorkCorrectly()
        {
            // Arrange
            var bucket = new ResourceBucket("rage", max: 10, initial: 0);

            // Act & Assert - 多次增加
            Assert.Equal(3, bucket.Gain(3, "op1"));
            Assert.Equal(3, bucket.Current);

            Assert.Equal(5, bucket.Gain(5, "op2"));
            Assert.Equal(8, bucket.Current);

            // 尝试超过上限
            Assert.Equal(2, bucket.Gain(5, "op3"));
            Assert.Equal(10, bucket.Current);

            // 消耗一些
            Assert.True(bucket.TryConsume(4, "op4"));
            Assert.Equal(6, bucket.Current);

            // 再次增加
            Assert.Equal(4, bucket.Gain(4, "op5"));
            Assert.Equal(10, bucket.Current);
        }
    }
}
