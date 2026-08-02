using Microsoft.Extensions.Logging;

using Netimobiledevice.Afc;
using Netimobiledevice.Lockdown;
using Netimobiledevice.NotificationProxy;

namespace iPhoneRingsMaker.Services;

internal sealed class AppleSyncSession : IAsyncDisposable
{
    private const string SyncLockPath = "/com.apple.itunes.lock_sync";
    private readonly AfcService _afc;
    private readonly ILogger _logger;
    private readonly NotificationProxyService _notifications;
    private ulong? _lockHandle;
    private bool _lockAcquired;
    private bool _disposed;

    private AppleSyncSession(LockdownClient lockdown, ILogger logger)
    {
        _afc = new AfcService(lockdown);
        _logger = logger;
        _notifications = new NotificationProxyService(lockdown, false);
    }

    public AfcService Afc => _disposed
        ? throw new ObjectDisposedException(nameof(AppleSyncSession))
        : _afc;

    public static async Task<AppleSyncSession> StartAsync(
        LockdownClient lockdown,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var session = new AppleSyncSession(lockdown, logger);
        try
        {
            session._notifications.Post(SendableNotificaton.SyncWillStart);
            session._lockHandle = await session._afc.FileOpen(
                SyncLockPath,
                cancellationToken,
                AfcFileOpenMode.ReadWrite).ConfigureAwait(false);
            session._notifications.Post(SendableNotificaton.SyncLockRequest);
            await session.AcquireLockAsync(cancellationToken).ConfigureAwait(false);
            session._notifications.Post(SendableNotificaton.SyncDidStart);
            return session;
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Exception? cleanupError = null;
        if (_lockHandle is { } lockHandle)
        {
            if (_lockAcquired)
            {
                try
                {
                    await _afc.Lock(
                        lockHandle,
                        AfcLockModes.Unlock,
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    cleanupError = exception;
                }
            }

            try
            {
                await _afc.FileClose(lockHandle, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                cleanupError ??= exception;
            }
        }

        try
        {
            _notifications.Post(SendableNotificaton.SyncDidFinish);
        }
        catch (Exception exception)
        {
            cleanupError ??= exception;
        }
        finally
        {
            _notifications.Dispose();
            _afc.Dispose();
        }

        if (cleanupError is not null)
        {
            _logger.LogWarning(cleanupError, "The Apple synchronization session could not be closed cleanly.");
        }

    }

    private async Task AcquireLockAsync(CancellationToken cancellationToken)
    {
        if (_lockHandle is not { } lockHandle)
        {
            throw new InvalidOperationException("The synchronization lock file is not open.");
        }

        Exception? lastError = null;
        for (var attempt = 0; attempt < 50; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await _afc.Lock(
                    lockHandle,
                    AfcLockModes.ExclusiveLock,
                    cancellationToken).ConfigureAwait(false);
                _lockAcquired = true;
                return;
            }
            catch (Exception exception)
            {
                lastError = exception;
                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new IOException("The iPhone synchronization lock is busy.", lastError);
    }
}
