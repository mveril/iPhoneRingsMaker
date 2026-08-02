using Microsoft.Extensions.Logging;

using Netimobiledevice.Afc;

namespace iPhoneRingsMaker.Services;

internal sealed class RingtoneTransferTransaction(
    AfcService afc,
    string catalogPath,
    byte[]? originalCatalog,
    bool catalogExisted,
    ILogger logger) : IAsyncDisposable
{
    private string? _remoteRingtonePath;
    private bool _catalogWasWritten;
    private bool _committed;
    private bool _disposed;

    public void TrackRingtone(string remotePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _remoteRingtonePath = remotePath;
    }

    public void TrackCatalogWrite()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _catalogWasWritten = true;
    }

    public void Commit()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_committed)
        {
            return;
        }

        try
        {
            if (_catalogWasWritten)
            {
                if (catalogExisted && originalCatalog is not null)
                {
                    await afc.SetFileContents(
                        catalogPath,
                        originalCatalog,
                        CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    await afc.Rm(catalogPath, CancellationToken.None, force: true).ConfigureAwait(false);
                }
            }

            if (_remoteRingtonePath is not null
                && await afc.Exists(_remoteRingtonePath, CancellationToken.None).ConfigureAwait(false))
            {
                await afc.Rm(
                    _remoteRingtonePath,
                    CancellationToken.None,
                    force: true).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "The failed ringtone transfer could not be rolled back completely.");
        }
    }
}
