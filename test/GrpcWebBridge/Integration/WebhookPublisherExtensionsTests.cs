using Xunit;
using FluentAssertions;
using GrpcWebBridge.Integration;

/// <summary>
/// Tests for the WebhookPublisherExtensions class.
/// </summary>
public class WebhookPublisherExtensionsTests
{
    /// <summary>
    /// Verifies that SubscribeWithFilter throws ArgumentNullException when the publisher is null.
    /// </summary>
    [Fact]
    public void SubscribeWithFilter_NullPublisher_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => WebhookPublisherExtensions.SubscribeWithFilter(null, "url", e => true));
    }

    /// <summary>
    /// Verifies that PublishEventAsync throws ArgumentNullException when the publisher is null.
    /// </summary>
    [Fact]
    public void PublishEventAsync_NullPublisher_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => WebhookPublisherExtensions.PublishEventAsync(null, new EventBase()));
    }
}
