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
    private readonly ConcurrentDictionary<string, Task<string>> _trackCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, IReadOnlyList<IPhoneMusicTrack>> _catalogCache = new(StringComparer.Ordinal);
    private readonly string _sessionCachePath;
    private readonly ILogger<AppleMusicLibraryService> _logger;
    private bool _disposed;

    public AppleMusicLibraryService(
        IAppleDeviceService deviceService,
        IPhoneMusicCatalogParser catalogParser,
        ILogger<AppleMusicLibraryService> logger)
    {
        _deviceService = deviceService;
        _catalogParser = catalogParser;
        _logger = logger;
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

            await CopyRemoteFileAsync(afc, CatalogPath, localCatalogPath, cancellationToken).ConfigureAwait(false);
            await CopyOptionalSidecarAsync(afc, CatalogPath, localCatalogPath, "-wal", cancellationToken).ConfigureAwait(false);
            await CopyOptionalSidecarAsync(afc, CatalogPath, localCatalogPath, "-shm", cancellationToken).ConfigureAwait(false);

            var tracks = await _catalogParser.ParseAsync(localCatalogPath, cancellationToken).ConfigureAwait(false);
            var tracksWithArtwork = await LoadLocalArtworkAsync(session.Afc, tracks, cancellationToken).ConfigureAwait(false);
            _catalogCache[device.Identifier] = tracksWithArtwork;
            return tracksWithArtwork;
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

    private async Task<IReadOnlyList<IPhoneMusicTrack>> LoadLocalArtworkAsync(
        Netimobiledevice.Afc.AfcService afc,
        IReadOnlyList<IPhoneMusicTrack> tracks,
        CancellationToken cancellationToken)
    {
        var artworkCache = new Dictionary<string, byte[]?>(StringComparer.OrdinalIgnoreCase);
        var result = new IPhoneMusicTrack[tracks.Count];
        for (var index = 0; index < tracks.Count; index++)
        {
            var track = tracks[index];
            if (string.IsNullOrWhiteSpace(track.ArtworkRemotePath))
            {
                result[index] = track;
                continue;
            }

            if (!artworkCache.TryGetValue(track.ArtworkRemotePath, out var artworkData))
            {
                try
                {
                    artworkData = await afc.GetFileContents(
                        track.ArtworkRemotePath,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.LogDebug(
                        exception,
                        "Local artwork {ArtworkPath} could not be read; the remote URL will be used as fallback.",
                        track.ArtworkRemotePath);
                }

                artworkCache[track.ArtworkRemotePath] = artworkData;
            }

            result[index] = artworkData is null
                ? track
                : track with
                {
                    ArtworkData = artworkData
                };
        }

        return result;
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
        TryDeleteDirectory(_sessionCachePath);
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
        var resolvedRemotePath = await ResolveTrackRemotePathAsync(
            session.Afc,
            remotePath,
            cancellationToken).ConfigureAwait(false);
        await CopyRemoteFileAsync(session.Afc, resolvedRemotePath, localPath, cancellationToken).ConfigureAwait(false);
        return localPath;
    }

    private async Task<string> ResolveTrackRemotePathAsync(
        Netimobiledevice.Afc.AfcService afc,
        string catalogPath,
        CancellationToken cancellationToken)
    {
        if (await afc.Exists(catalogPath, cancellationToken).ConfigureAwait(false))
        {
            return catalogPath;
        }

        var fileName = Path.GetFileName(catalogPath.Replace('\\', '/'));
        string[] searchRoots =
        [
            "/Music",
            "/Downloads",
            "/Purchases",
            "/iTunes_Control/Music",
            "/iTunes_Control/Downloads",
        ];
        foreach (var searchRoot in searchRoots)
        {
            if (!await afc.Exists(searchRoot, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            await foreach (var candidate in afc.LsDirectory(
                searchRoot,
                cancellationToken,
                8).ConfigureAwait(false))
            {
                var normalizedCandidate = candidate.Replace('\\', '/');
                if (!StringComparer.OrdinalIgnoreCase.Equals(
                    Path.GetFileName(normalizedCandidate),
                    fileName))
                {
                    continue;
                }

                var normalizedRoot = searchRoot.TrimEnd('/');
                var resolvedPath = normalizedCandidate.StartsWith('/')
                    ? normalizedCandidate
                    : normalizedCandidate.StartsWith(
                        normalizedRoot.TrimStart('/') + '/',
                        StringComparison.OrdinalIgnoreCase)
                        ? $"/{normalizedCandidate}"
                        : $"{normalizedRoot}/{normalizedCandidate.TrimStart('/')}";
                _logger.LogDebug(
                    "Resolved catalog music path {CatalogPath} to AFC path {ResolvedPath}.",
                    catalogPath,
                    resolvedPath);
                return resolvedPath;
            }
        }

        throw new FileNotFoundException(
            $"The music file {fileName} was not found on the iPhone.",
            catalogPath);
    }

    private static async Task CopyRemoteFileAsync(
        Netimobiledevice.Afc.AfcService afc,
        string remotePath,
        string localPath,
        CancellationToken cancellationToken)
    {
        var contents = await afc.GetFileContents(remotePath, cancellationToken).ConfigureAwait(false)
            ?? throw new IOException($"The iPhone returned no data for {remotePath}.");
        await File.WriteAllBytesAsync(localPath, contents, cancellationToken).ConfigureAwait(false);
    }

    private static async Task CopyOptionalSidecarAsync(
        Netimobiledevice.Afc.AfcService afc,
        string remoteCatalogPath,
        string localCatalogPath,
        string suffix,
        CancellationToken cancellationToken)
    {
        var remotePath = remoteCatalogPath + suffix;
        if (await afc.Exists(remotePath, cancellationToken).ConfigureAwait(false))
        {
            await CopyRemoteFileAsync(afc, remotePath, localCatalogPath + suffix, cancellationToken)
                .ConfigureAwait(false);
        }
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
