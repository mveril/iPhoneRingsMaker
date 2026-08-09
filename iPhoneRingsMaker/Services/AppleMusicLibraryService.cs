using System.Collections.Concurrent;
using System.Diagnostics;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Core.Services;
using iPhoneRingsMaker.Models;

using Microsoft.Extensions.Logging;

using Netimobiledevice;

namespace iPhoneRingsMaker.Services;

public sealed class AppleMusicLibraryService : IAppleMusicLibraryService
{
    private const string CatalogPath = "/iTunes_Control/iTunes/MediaLibrary.sqlitedb";
    private readonly IAppleDeviceService _deviceService;
    private readonly IPhoneMusicCatalogParser _catalogParser;
    private readonly ConcurrentDictionary<string, Task<IReadOnlyList<IPhoneMusicTrack>>> _catalogLoads = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Task<string>> _trackCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, IReadOnlyList<IPhoneMusicTrack>> _catalogCache = new(StringComparer.Ordinal);
    private readonly string _sessionCachePath;
    private readonly ILogger<AppleMusicLibraryService> _logger;
    private readonly IAppleDeviceFileService _deviceFileService;
    private bool _disposed;

    public AppleMusicLibraryService(
        IAppleDeviceService deviceService,
        IPhoneMusicCatalogParser catalogParser,
        ILogger<AppleMusicLibraryService> logger,
        IAppleDeviceFileService deviceFileService)
    {
        _deviceService = deviceService;
        _catalogParser = catalogParser;
        _logger = logger;
        _deviceFileService = deviceFileService;
        _sessionCachePath = Path.Combine(
            Path.GetTempPath(),
            "iPhoneRingsMaker",
            "DeviceMedia",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sessionCachePath);
    }

    public async Task<IReadOnlyList<IPhoneMusicTrack>> GetTracksAsync(
        AppleDeviceInfo device,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(device);
        EnsureUsableDevice(device);

        if (_catalogCache.TryGetValue(device.Identifier, out var cachedTracks))
        {
            return cachedTracks;
        }

        var loadTask = _catalogLoads.GetOrAdd(
            device.Identifier,
            _ => LoadCatalogAsync(device, cancellationToken));
        try
        {
            return await loadTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            if (loadTask.IsCompleted)
            {
                _catalogLoads.TryRemove(new KeyValuePair<string, Task<IReadOnlyList<IPhoneMusicTrack>>>(
                    device.Identifier,
                    loadTask));
            }

            throw;
        }
    }

    public async Task<IIPhoneArtworkLoadingSession> CreateArtworkLoadingSessionAsync(
        AppleDeviceInfo device,
        IReadOnlyList<IPhoneMusicTrack> tracks,
        IProgress<IPhoneMusicTrack> progress,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(tracks);
        ArgumentNullException.ThrowIfNull(progress);
        EnsureUsableDevice(device);

        var lockdown = MobileDevice.CreateUsingUsbmux(device.Identifier);
        try
        {
            var syncSession = await AppleSyncSession.StartAsync(
                lockdown,
                _logger,
                cancellationToken).ConfigureAwait(false);
            return new IPhoneArtworkLoadingSession(
                lockdown,
                syncSession,
                tracks,
                progress,
                track => CacheArtwork(device.Identifier, track),
                _logger,
                cancellationToken);
        }
        catch
        {
            lockdown.Dispose();
            throw;
        }
    }

    public void InvalidateCatalog(string deviceIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceIdentifier);
        _catalogCache.TryRemove(deviceIdentifier, out _);
        _catalogLoads.TryRemove(deviceIdentifier, out _);
    }

    private async Task<IReadOnlyList<IPhoneMusicTrack>> LoadCatalogAsync(
        AppleDeviceInfo device,
        CancellationToken cancellationToken)
    {
        var catalogDirectory = Path.Combine(_sessionCachePath, $"catalog-{Guid.NewGuid():N}");
        Directory.CreateDirectory(catalogDirectory);
        var localCatalogPath = Path.Combine(catalogDirectory, "MediaLibrary.sqlitedb");

        try
        {
            using var lockdown = MobileDevice.CreateUsingUsbmux(device.Identifier);
            await using var session = await AppleSyncSession.StartAsync(
                lockdown,
                _logger,
                cancellationToken).ConfigureAwait(false);
            var afc = session.Afc;
            if (!await afc.Exists(CatalogPath, cancellationToken).ConfigureAwait(false))
            {
                throw new FileNotFoundException("The iPhone music catalog is not available.", CatalogPath);
            }

            await _deviceFileService.CopyAsync(afc, CatalogPath, localCatalogPath, cancellationToken).ConfigureAwait(false);
            await _deviceFileService.CopyOptionalSidecarAsync(afc, CatalogPath, localCatalogPath, "-wal", cancellationToken).ConfigureAwait(false);
            await _deviceFileService.CopyOptionalSidecarAsync(afc, CatalogPath, localCatalogPath, "-shm", cancellationToken).ConfigureAwait(false);

            var tracks = await _catalogParser.ParseAsync(localCatalogPath, cancellationToken).ConfigureAwait(false);
            _catalogCache[device.Identifier] = tracks;
            return tracks;
        }
        finally
        {
#if !DEBUG
            TryDeleteDirectory(catalogDirectory);
#endif
        }
    }

    public async Task<IPhoneMusicTrack> GetTrackAsync(
        IPhoneMediaSource source,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(source);
        ValidateSource(source);

        if (!_catalogCache.TryGetValue(source.DeviceIdentifier, out var tracks))
        {
            var device = (await _deviceService.GetDevicesAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(candidate => StringComparer.Ordinal.Equals(
                    candidate.Identifier,
                    source.DeviceIdentifier))
                ?? throw new InvalidOperationException($"Reconnect {source.DeviceDisplayName} to continue.");
            tracks = await GetTracksAsync(device, cancellationToken).ConfigureAwait(false);
        }

        return tracks.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(
            candidate.PersistentIdentifier,
            source.TrackPersistentIdentifier))
            ?? throw new FileNotFoundException(
                $"{source.TrackDisplayName} is no longer present on {source.DeviceDisplayName}.");
    }

    public async Task<string> GetCachedTrackPathAsync(
        IPhoneMediaSource source,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(source);
        ValidateSource(source);

        var key = $"{source.DeviceIdentifier}\0{source.TrackPersistentIdentifier}";
        var task = _trackCache.GetOrAdd(key, _ => DownloadTrackAsync(source, cancellationToken));
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch
        {
            _trackCache.TryRemove(new KeyValuePair<string, Task<string>>(key, task));
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _catalogCache.Clear();
        _catalogLoads.Clear();
        TryDeleteDirectory(_sessionCachePath);
    }

    private void CacheArtwork(string deviceIdentifier, IPhoneMusicTrack updatedTrack)
    {
        if (!_catalogCache.TryGetValue(deviceIdentifier, out var tracks))
        {
            return;
        }

        var updatedTracks = tracks.ToArray();
        for (var index = 0; index < updatedTracks.Length; index++)
        {
            if (StringComparer.Ordinal.Equals(
                updatedTracks[index].PersistentIdentifier,
                updatedTrack.PersistentIdentifier))
            {
                updatedTracks[index] = updatedTrack;
                _catalogCache.TryUpdate(deviceIdentifier, updatedTracks, tracks);
                return;
            }
        }
    }

    private async Task<string> DownloadTrackAsync(
        IPhoneMediaSource source,
        CancellationToken cancellationToken)
    {
        var device = (await _deviceService.GetDevicesAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(candidate => StringComparer.Ordinal.Equals(
                candidate.Identifier,
                source.DeviceIdentifier))
            ?? throw new InvalidOperationException($"Reconnect {source.DeviceDisplayName} to continue.");
        EnsureUsableDevice(device);

        var remotePath = source.RemotePathHint;
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            var track = await GetTrackAsync(source, cancellationToken).ConfigureAwait(false);
            if (!track.IsAvailable || string.IsNullOrWhiteSpace(track.RemotePath))
            {
                throw new InvalidOperationException($"{source.TrackDisplayName} cannot be read from {source.DeviceDisplayName}.");
            }

            remotePath = track.RemotePath;
        }

        var extension = Path.GetExtension(remotePath);
        var localPath = Path.Combine(
            _sessionCachePath,
            $"{SanitizeFileName(source.TrackPersistentIdentifier)}{extension}");
        using var lockdown = MobileDevice.CreateUsingUsbmux(device.Identifier);
        await using var session = await AppleSyncSession.StartAsync(
            lockdown,
            _logger,
            cancellationToken).ConfigureAwait(false);
        var resolvedRemotePath = await _deviceFileService.ResolveMusicPathAsync(
            session.Afc,
            remotePath,
            cancellationToken).ConfigureAwait(false);
        await _deviceFileService.CopyAsync(session.Afc, resolvedRemotePath, localPath, cancellationToken).ConfigureAwait(false);
        return localPath;
    }

    private static void EnsureUsableDevice(AppleDeviceInfo device)
    {
        if (!device.IsPaired)
        {
            throw new InvalidOperationException("The iPhone must be paired before its music library can be read.");
        }
    }

    private static void ValidateSource(IPhoneMediaSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source.DeviceIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.TrackPersistentIdentifier);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Where(character => !invalidCharacters.Contains(character)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception)
        {
            // Temporary data is also cleaned by the operating system.
        }
    }
}
