namespace iPhoneRingsMaker.Core.Models;

public enum IPhoneMusicTrackAvailability
{
    Available,
    CloudOnly,
    Protected,
    MissingFile,
    UnsupportedFormat,
}

public sealed record IPhoneMusicTrack(
    string PersistentIdentifier,
    string Title,
    string? Artist,
    string? Album,
    TimeSpan Duration,
    string? RemotePath,
    string? FileFormat,
    long? FileSize,
    IPhoneMusicTrackAvailability Availability,
    string? ArtworkIdentifier = null,
    string? ArtworkUrl = null,
    byte[]? ArtworkData = null,
    string? ArtworkRemotePath = null)
{
    public bool IsAvailable => Availability == IPhoneMusicTrackAvailability.Available;

    public string DisplayName => string.IsNullOrWhiteSpace(Artist)
        ? Title
        : $"{Artist} — {Title}";
}
