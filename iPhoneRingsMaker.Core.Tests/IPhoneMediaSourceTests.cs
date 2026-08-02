using System.Text.Json;

using iPhoneRingsMaker.Core.Helpers;
using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Core.Tests;

public class IPhoneMediaSourceTests
{
    [Fact]
    public void Serialize_IncludesIdentifiersAndHumanReadableNames()
    {
        IMediaSource source = CreateSource();

        var json = JsonSerializer.Serialize(source, Json.Options);

        Assert.Contains("\"$type\": \"iphone\"", json);
        Assert.Contains("\"DeviceIdentifier\": \"device-id\"", json);
        Assert.Contains("\"TrackPersistentIdentifier\": \"track-id\"", json);
        Assert.Contains("\"DeviceDisplayName\": \"Mika\\u0027s iPhone\"", json);
        Assert.Contains("\"TrackDisplayName\":", json);
        var restored = Assert.IsType<IPhoneMediaSource>(
            JsonSerializer.Deserialize<IMediaSource>(json, Json.Options));
        Assert.Equal("Artist — Title", restored.TrackDisplayName);
    }

    [Fact]
    public void Deserialize_MissingDeviceIdentifier_ThrowsJsonException()
    {
        const string json = """
            {
              "$type": "iphone",
              "DeviceDisplayName": "iPhone",
              "TrackPersistentIdentifier": "track-id",
              "TrackDisplayName": "Title"
            }
            """;

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IMediaSource>(json, Json.Options));
    }

    [Fact]
    public void Equals_IgnoresDisplayNamesAndRemotePathHint()
    {
        var first = CreateSource();
        var second = new IPhoneMediaSource
        {
            DeviceIdentifier = first.DeviceIdentifier,
            DeviceDisplayName = "Renamed iPhone",
            TrackPersistentIdentifier = first.TrackPersistentIdentifier,
            TrackDisplayName = "Renamed track",
            RemotePathHint = "/different/path.m4a",
        };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    private static IPhoneMediaSource CreateSource() => new()
    {
        DeviceIdentifier = "device-id",
        DeviceDisplayName = "Mika's iPhone",
        TrackPersistentIdentifier = "track-id",
        TrackDisplayName = "Artist — Title",
        RemotePathHint = "/iTunes_Control/Music/F01/ABC.m4a",
    };
}
