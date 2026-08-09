using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Core.Services;

using Microsoft.Extensions.Logging;

using Netimobiledevice.Lockdown;

namespace iPhoneRingsMaker.Services;

internal sealed class IPhoneArtworkLoadingSession : IIPhoneArtworkLoadingSession
{
    private static readonly TimeSpan InitialVisibilityDelay = TimeSpan.FromMilliseconds(100);
    private readonly CancellationTokenSource _cancellation;
    private readonly Action<IPhoneMusicTrack> _cacheArtwork;
    private readonly LockdownClient _lockdown;
    private readonly ILogger _logger;
    private readonly IPhoneArtworkPriorityQueue _priorityQueue;
    private readonly AppleSyncSession _syncSession;
    private readonly IProgress<IPhoneMusicTrack> _progress;
    private readonly object _stateLock = new();
    private bool _disposed;

    public IPhoneArtworkLoadingSession(
        LockdownClient lockdown,
        AppleSyncSession syncSession,
        IReadOnlyList<IPhoneMusicTrack> tracks,
        IProgress<IPhoneMusicTrack> progress,
        Action<IPhoneMusicTrack> cacheArtwork,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        _lockdown = lockdown;
        _syncSession = syncSession;
        _progress = progress;
        _cacheArtwork = cacheArtwork;
        _logger = logger;
        _priorityQueue = new IPhoneArtworkPriorityQueue(tracks
            .Where(track => track.ArtworkData is null && !string.IsNullOrWhiteSpace(track.ArtworkRemotePath))
            .ToList());
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Completion = LoadArtworkAsync(_cancellation.Token);
    }

    public Task Completion
    {
        get;
    }

    public void SetVisibleTracks(IReadOnlyCollection<string> trackIdentifiers)
    {
        ArgumentNullException.ThrowIfNull(trackIdentifiers);
        ObjectDisposedException.ThrowIf(_disposed, this);
        _priorityQueue.SetVisibleTracks(trackIdentifiers);
    }

    public async ValueTask DisposeAsync()
    {
        lock (_stateLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _cancellation.Cancel();
        try
        {
            await Completion.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _cancellation.Dispose();
        }
    }

    private async Task LoadArtworkAsync(CancellationToken cancellationToken)
    {
        var artworkCache = new Dictionary<string, byte[]?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await Task.Delay(InitialVisibilityDelay, cancellationToken).ConfigureAwait(false);
            while (_priorityQueue.TryDequeue(out var track))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var artworkPath = track!.ArtworkRemotePath!;
                if (!artworkCache.TryGetValue(artworkPath, out var artworkData))
                {
                    try
                    {
                        artworkData = await _syncSession.Afc.GetFileContents(
                            artworkPath,
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        _logger.LogDebug(
                            exception,
                            "Local artwork {ArtworkPath} could not be read; the remote URL will be used as fallback.",
                            artworkPath);
                    }

                    artworkCache[artworkPath] = artworkData;
                }

                if (artworkData is null)
                {
                    continue;
                }

                var updatedTrack = track with { ArtworkData = artworkData };
                _cacheArtwork(updatedTrack);
                _progress.Report(updatedTrack);
            }
        }
        finally
        {
            await _syncSession.DisposeAsync().ConfigureAwait(false);
            _lockdown.Dispose();
        }
    }

}
