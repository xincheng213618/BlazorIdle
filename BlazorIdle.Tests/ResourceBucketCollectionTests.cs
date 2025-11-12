using Xunit;
using BlazorIdle.Game.Resources;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// Phase 1 单元测试：ResourceBucketCollection
    /// Phase 1 unit tests: ResourceBucketCollection
    /// </summary>
    public class ResourceBucketCollectionTests
    {
        [Fact]
        public void ResourceBucketCollection_Constructor_CreatesDefaultRageBucket()
        {
            // Arrange & Act
            var collection = new ResourceBucketCollection();

            // Assert
            Assert.True(collection.HasBucket("rage"));
            var rage = collection.GetBucket("rage");
            Assert.NotNull(rage);
            Assert.Equal("rage", rage.Id);
            Assert.Equal(10, rage.Max);
            Assert.Equal(0, rage.Current);
        }

        [Fact]
        public void ResourceBucketCollection_GetBucket_ReturnsExistingBucket()
        {
            // Arrange
            var collection = new ResourceBucketCollection();

            // Act
            var rage = collection.GetBucket("rage");

            // Assert
            Assert.NotNull(rage);
            Assert.Equal("rage", rage.Id);
        }

        [Fact]
        public void ResourceBucketCollection_GetBucket_ThrowsWhenNotFound()
        {
            // Arrange
            var collection = new ResourceBucketCollection();

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => collection.GetBucket("nonexistent"));
        }

        [Fact]
        public void ResourceBucketCollection_HasBucket_ReturnsTrueForExisting()
        {
            // Arrange
            var collection = new ResourceBucketCollection();

            // Act & Assert
            Assert.True(collection.HasBucket("rage"));
        }

        [Fact]
        public void ResourceBucketCollection_HasBucket_ReturnsFalseForNonexistent()
        {
            // Arrange
            var collection = new ResourceBucketCollection();

            // Act & Assert
            Assert.False(collection.HasBucket("energy"));
        }

        [Fact]
        public void ResourceBucketCollection_AddBucket_CreatesNewBucket()
        {
            // Arrange
            var collection = new ResourceBucketCollection();

            // Act
            collection.AddBucket("energy", max: 100, initial: 50);

            // Assert
            Assert.True(collection.HasBucket("energy"));
            var energy = collection.GetBucket("energy");
            Assert.Equal("energy", energy.Id);
            Assert.Equal(100, energy.Max);
            Assert.Equal(50, energy.Current);
        }

        [Fact]
        public void ResourceBucketCollection_AddBucket_ThrowsWhenAlreadyExists()
        {
            // Arrange
            var collection = new ResourceBucketCollection();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                collection.AddBucket("rage", max: 10, initial: 0));
        }

        [Fact]
        public void ResourceBucketCollection_RemoveBucket_RemovesExistingBucket()
        {
            // Arrange
            var collection = new ResourceBucketCollection();
            collection.AddBucket("energy", max: 100, initial: 50);

            // Act
            bool removed = collection.RemoveBucket("energy");

            // Assert
            Assert.True(removed);
            Assert.False(collection.HasBucket("energy"));
        }

        [Fact]
        public void ResourceBucketCollection_RemoveBucket_ReturnsFalseWhenNotFound()
        {
            // Arrange
            var collection = new ResourceBucketCollection();

            // Act
            bool removed = collection.RemoveBucket("nonexistent");

            // Assert
            Assert.False(removed);
        }

        [Fact]
        public void ResourceBucketCollection_GetAll_ReturnsAllBuckets()
        {
            // Arrange
            var collection = new ResourceBucketCollection();
            collection.AddBucket("energy", max: 100, initial: 50);
            collection.AddBucket("mana", max: 50, initial: 25);

            // Act
            var all = collection.GetAll();

            // Assert
            Assert.Equal(3, all.Count); // rage (default) + energy + mana
            Assert.True(all.ContainsKey("rage"));
            Assert.True(all.ContainsKey("energy"));
            Assert.True(all.ContainsKey("mana"));
        }

        [Fact]
        public void ResourceBucketCollection_ModifyBucketThroughReference_AffectsCollection()
        {
            // Arrange
            var collection = new ResourceBucketCollection();
            var rage = collection.GetBucket("rage");

            // Act
            rage.Gain(5, "test");

            // Assert
            var rageAgain = collection.GetBucket("rage");
            Assert.Equal(5, rageAgain.Current); // 修改生效
        }

        [Fact]
        public void ResourceBucketCollection_MultipleOperations_WorkCorrectly()
        {
            // Arrange
            var collection = new ResourceBucketCollection();

            // Act & Assert
            // 添加多个资源桶
            collection.AddBucket("energy", max: 100, initial: 0);
            collection.AddBucket("mana", max: 50, initial: 25);

            // 验证数量
            Assert.Equal(3, collection.GetAll().Count);

            // 修改资源
            var rage = collection.GetBucket("rage");
            rage.Gain(10, "test");
            Assert.Equal(10, rage.Current);

            var energy = collection.GetBucket("energy");
            energy.Gain(50, "test");
            Assert.Equal(50, energy.Current);

            // 移除一个
            collection.RemoveBucket("mana");
            Assert.Equal(2, collection.GetAll().Count);
            Assert.False(collection.HasBucket("mana"));
        }
    }
}
