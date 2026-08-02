using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Models;

namespace iPhoneRingsMaker.Contracts.Services;

public interface IAppleMusicLibraryService : IDisposable
{
    Task<IReadOnlyList<IPhoneMusicTrack>> GetTracksAsync(
        AppleDeviceInfo device,
        CancellationToken cancellationToken = default);

    Task<IPhoneMusicTrack> GetTrackAsync(
        IPhoneMediaSource source,
        CancellationToken cancellationToken = default);

    Task<string> GetCachedTrackPathAsync(
        IPhoneMediaSource source,
        CancellationToken cancellationToken = default);
}
