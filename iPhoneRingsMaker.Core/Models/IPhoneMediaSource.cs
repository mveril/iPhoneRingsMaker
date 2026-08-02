namespace iPhoneRingsMaker.Core.Models;

public sealed class IPhoneMediaSource : IMediaSource, IEquatable<IPhoneMediaSource>
{
    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public IPhoneMediaSource()
    {
        DeviceIdentifier = string.Empty;
        DeviceDisplayName = string.Empty;
        TrackPersistentIdentifier = string.Empty;
        TrackDisplayName = string.Empty;
    }

    [System.Text.Json.Serialization.JsonRequired]
    public required string DeviceIdentifier
    {
        get; init;
    }

    [System.Text.Json.Serialization.JsonRequired]
    public required string DeviceDisplayName
    {
        get; init;
    }

    [System.Text.Json.Serialization.JsonRequired]
    public required string TrackPersistentIdentifier
    {
        get; init;
    }

    [System.Text.Json.Serialization.JsonRequired]
    public required string TrackDisplayName
    {
        get; init;
    }

    public string? TrackTitle
    {
        get; init;
    }

    public string? RemotePathHint
    {
        get; init;
    }

    public bool Equals(IPhoneMediaSource? other)
    {
        return other is not null
            && StringComparer.Ordinal.Equals(DeviceIdentifier, other.DeviceIdentifier)
            && StringComparer.Ordinal.Equals(TrackPersistentIdentifier, other.TrackPersistentIdentifier);
    }

    public bool Equals(IMediaSource? other) => Equals(other as IPhoneMediaSource);

    public override bool Equals(object? obj) => Equals(obj as IPhoneMediaSource);

    public override int GetHashCode() => HashCode.Combine(
        StringComparer.Ordinal.GetHashCode(DeviceIdentifier),
        StringComparer.Ordinal.GetHashCode(TrackPersistentIdentifier));
}
