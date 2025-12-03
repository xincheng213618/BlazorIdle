using System;
using Xunit;
using BlazorIdle.Game.Config;

namespace BlazorIdle.Tests
{
    /// <summary>
    /// ConfigRepository 缓存机制测试
    /// ConfigRepository caching mechanism tests
    /// </summary>
    public class ConfigRepositoryCacheTests
    {
        [Fact]
        public void LoadItems_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange - 清除缓存确保测试隔离
            ConfigRepository.ClearCache();
            var initialMisses = ConfigRepository.CacheMisses;

            // Act - 第一次调用应从文件加载
            var items1 = ConfigRepository.LoadItems();
            var missesAfterFirst = ConfigRepository.CacheMisses;

            // Act - 第二次调用应从缓存获取
            var items2 = ConfigRepository.LoadItems();
            var missesAfterSecond = ConfigRepository.CacheMisses;

            // Assert
            Assert.NotNull(items1);
            Assert.NotNull(items2);
            Assert.Same(items1, items2); // 应该是同一个实例
            Assert.Equal(initialMisses + 1, missesAfterFirst); // 第一次应该是缓存未命中
            Assert.Equal(initialMisses + 1, missesAfterSecond); // 第二次应该仍是同一个miss count
        }

        [Fact]
        public void LoadMonsters_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var monsters1 = ConfigRepository.LoadMonsters();
            var hits1 = ConfigRepository.CacheHits;
            var monsters2 = ConfigRepository.LoadMonsters();
            var hits2 = ConfigRepository.CacheHits;

            // Assert
            Assert.NotNull(monsters1);
            Assert.NotNull(monsters2);
            Assert.Same(monsters1, monsters2);
            Assert.Equal(hits1 + 1, hits2); // 第二次调用应增加缓存命中
        }

        [Fact]
        public void LoadProfessions_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var profs1 = ConfigRepository.LoadProfessions();
            var profs2 = ConfigRepository.LoadProfessions();

            // Assert
            Assert.NotNull(profs1);
            Assert.Same(profs1, profs2);
        }

        [Fact]
        public void ClearCache_ResetsAllCachedData()
        {
            // Arrange - 先加载一些配置
            ConfigRepository.ClearCache();
            var items1 = ConfigRepository.LoadItems();
            var monsters1 = ConfigRepository.LoadMonsters();

            // Act
            ConfigRepository.ClearCache();

            // Assert - 统计应该被重置
            Assert.Equal(0, ConfigRepository.CacheHits);
            Assert.Equal(0, ConfigRepository.CacheMisses);

            // 重新加载应该创建新实例
            var items2 = ConfigRepository.LoadItems();
            Assert.NotSame(items1, items2); // 应该是新实例
        }

        [Fact]
        public void LoadUserConfig_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var config1 = ConfigRepository.LoadUserConfig();
            var config2 = ConfigRepository.LoadUserConfig();

            // Assert
            Assert.NotNull(config1);
            Assert.Same(config1, config2);
        }

        [Fact]
        public void LoadBattleConfigs_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var configs1 = ConfigRepository.LoadBattleConfigs();
            var configs2 = ConfigRepository.LoadBattleConfigs();

            // Assert
            Assert.NotNull(configs1);
            Assert.Same(configs1, configs2);
        }

        [Fact]
        public void LoadDungeons_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var dungeons1 = ConfigRepository.LoadDungeons();
            var dungeons2 = ConfigRepository.LoadDungeons();

            // Assert
            Assert.NotNull(dungeons1);
            Assert.Same(dungeons1, dungeons2);
        }

        [Fact]
        public void LoadExperienceCurve_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var curve1 = ConfigRepository.LoadExperienceCurve();
            var curve2 = ConfigRepository.LoadExperienceCurve();

            // Assert
            Assert.NotNull(curve1);
            Assert.Same(curve1, curve2);
        }

        // 注意：LoadProfessionAttributes 测试已移除，因为 attributes.json 已被删除

        [Fact]
        public void LoadProfessionLimits_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var limits1 = ConfigRepository.LoadProfessionLimits();
            var limits2 = ConfigRepository.LoadProfessionLimits();

            // Assert
            Assert.NotNull(limits1);
            Assert.Same(limits1, limits2);
        }

        [Fact]
        public void LoadConsumableShop_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var shop1 = ConfigRepository.LoadConsumableShop();
            var shop2 = ConfigRepository.LoadConsumableShop();

            // Assert
            Assert.NotNull(shop1);
            Assert.Same(shop1, shop2);
        }

        [Fact]
        public void LoadPotionShop_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var shop1 = ConfigRepository.LoadPotionShop();
            var shop2 = ConfigRepository.LoadPotionShop();

            // Assert
            Assert.NotNull(shop1);
            Assert.Same(shop1, shop2);
        }

        [Fact]
        public void LoadFoodShop_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var shop1 = ConfigRepository.LoadFoodShop();
            var shop2 = ConfigRepository.LoadFoodShop();

            // Assert
            Assert.NotNull(shop1);
            Assert.Same(shop1, shop2);
        }

        [Fact]
        public void LoadSystemShop_MultipleCalls_ReturnsSameInstance()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act
            var shop1 = ConfigRepository.LoadSystemShop();
            var shop2 = ConfigRepository.LoadSystemShop();

            // Assert
            Assert.NotNull(shop1);
            Assert.Same(shop1, shop2);
        }

        [Fact]
        public void Cache_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            ConfigRepository.ClearCache();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            // Act - 并发访问多个配置
            System.Threading.Tasks.Parallel.For(0, 100, i =>
            {
                try
                {
                    _ = ConfigRepository.LoadItems();
                    _ = ConfigRepository.LoadMonsters();
                    _ = ConfigRepository.LoadProfessions();
                    _ = ConfigRepository.LoadUserConfig();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.Empty(exceptions);
        }

        [Fact]
        public void CacheStatistics_TrackHitsAndMisses()
        {
            // Arrange
            ConfigRepository.ClearCache();

            // Act - 第一次加载应该是miss
            _ = ConfigRepository.LoadItems();
            var missesAfterFirst = ConfigRepository.CacheMisses;
            var hitsAfterFirst = ConfigRepository.CacheHits;

            // Act - 第二次加载应该是hit
            _ = ConfigRepository.LoadItems();
            var missesAfterSecond = ConfigRepository.CacheMisses;
            var hitsAfterSecond = ConfigRepository.CacheHits;

            // Assert
            Assert.Equal(1, missesAfterFirst);
            Assert.Equal(0, hitsAfterFirst);
            Assert.Equal(1, missesAfterSecond); // miss count 不变
            Assert.Equal(1, hitsAfterSecond); // hit count 增加
        }
    }
}
