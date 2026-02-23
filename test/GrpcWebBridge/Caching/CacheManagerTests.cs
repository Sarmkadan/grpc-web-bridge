using Xunit;
using FluentAssertions;
using GrpcWebBridge.Caching;

public class CacheManagerTests
{
    [Fact]
    public void SetExpiration_ValidKey_UpdatesExpiration()
    {
        // Arrange
        var cacheManager = new CacheManager(new NullLogger<CacheManager>(), new CacheManagerOptions());
        cacheManager.Set("key", "value");
        var expiration = TimeSpan.FromHours(1);

        // Act
        cacheManager.SetExpiration("key", expiration);

        // Assert
        cacheManager.GetTimeToLive("key").Should().Be(expiration);
    }

    [Fact]
    public void GetTimeToLive_ExpiredKey_ReturnsZero()
    {
        // Arrange
        var cacheManager = new CacheManager(new NullLogger<CacheManager>(), new CacheManagerOptions());
        cacheManager.Set("key", "value", TimeSpan.FromSeconds(-1));

        // Act
        var ttl = cacheManager.GetTimeToLive("key");

        // Assert
        ttl.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void EstimateAverageSizeInBytes_EmptyList_ReturnsZero()
    {
        // Arrange
        var cacheManager = new CacheManager(new NullLogger<CacheManager>(), new CacheManagerOptions());
        var entries = new List<CacheEntry>();

        // Act
        var averageSize = cacheManager.EstimateAverageSizeInBytes(entries);

        // Assert
        averageSize.Should().Be(0);
    }
}
