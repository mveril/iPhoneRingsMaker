using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Core.Services;

namespace iPhoneRingsMaker.Core.Tests;

public sealed class IPhoneArtworkPriorityQueueTests
{
    [Fact]
    public void TryDequeue_VisibleTrackIsReturnedBeforeEarlierBackgroundTrack()
    {
        var queue = new IPhoneArtworkPriorityQueue(
        [
            CreateTrack("background-1"),
            CreateTrack("visible"),
            CreateTrack("background-2"),
        ]);
        queue.SetVisibleTracks(["visible"]);

        Assert.True(queue.TryDequeue(out var track));
        Assert.Equal("visible", track!.PersistentIdentifier);
    }

    [Fact]
    public void TryDequeue_NewlyVisibleTrackIsPromotedBeforeNextBackgroundTrack()
    {
        var queue = new IPhoneArtworkPriorityQueue(
        [
            CreateTrack("background-1"),
            CreateTrack("background-2"),
            CreateTrack("visible"),
        ]);

        Assert.True(queue.TryDequeue(out var first));
        queue.SetVisibleTracks(["visible"]);
        Assert.True(queue.TryDequeue(out var second));

        Assert.Equal("background-1", first!.PersistentIdentifier);
        Assert.Equal("visible", second!.PersistentIdentifier);
    }

    [Fact]
    public void TryDequeue_WithoutVisibleTracksPreservesCatalogOrder()
    {
        var queue = new IPhoneArtworkPriorityQueue(
        [
            CreateTrack("first"),
            CreateTrack("second"),
        ]);

        Assert.True(queue.TryDequeue(out var first));
        Assert.True(queue.TryDequeue(out var second));
        Assert.False(queue.TryDequeue(out var none));

        Assert.Equal("first", first!.PersistentIdentifier);
        Assert.Equal("second", second!.PersistentIdentifier);
        Assert.Null(none);
    }

    private static IPhoneMusicTrack CreateTrack(string identifier) => new(
        identifier,
        identifier,
        null,
        null,
        TimeSpan.Zero,
        null,
        null,
        null,
        IPhoneMusicTrackAvailability.Available);
}
