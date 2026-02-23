using Xunit;
using FluentAssertions;
using GrpcWebBridge.Integration;

public class WebhookPublisherExtensionsTests
{
    [Fact]
    public void SubscribeWithFilter_NullPublisher_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => WebhookPublisherExtensions.SubscribeWithFilter(null, "url", e => true));
    }

    [Fact]
    public void PublishEventAsync_NullPublisher_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => WebhookPublisherExtensions.PublishEventAsync(null, new EventBase()));
    }
}
