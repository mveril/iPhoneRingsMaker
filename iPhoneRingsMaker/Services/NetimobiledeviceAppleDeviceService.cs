using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Models;

using Microsoft.Extensions.Logging;

using Netimobiledevice;
using Netimobiledevice.Lockdown;
using Netimobiledevice.Lockdown.Pairing;
using Netimobiledevice.Plist;
using Netimobiledevice.Usbmuxd;

namespace iPhoneRingsMaker.Services;

public sealed class NetimobiledeviceAppleDeviceService : IAppleDeviceService
{
    private const string WirelessLockdownDomain = "com.apple.mobile.wireless_lockdown";
    private const string EnableWifiConnectionsKey = "EnableWifiConnections";
    private readonly IReadOnlyList<IRingtoneTransferAdapter> _transferAdapters;
    private readonly ILogger<NetimobiledeviceAppleDeviceService> _logger;
    private SynchronizationContext? _synchronizationContext;
    private bool _isWatching;

    public NetimobiledeviceAppleDeviceService(
        IEnumerable<IRingtoneTransferAdapter> transferAdapters,
        ILogger<NetimobiledeviceAppleDeviceService> logger)
    {
        _transferAdapters = transferAdapters.ToArray();
        _logger = logger;
    }

    public event EventHandler? DevicesChanged;

    public async Task<IReadOnlyList<AppleDeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run<IReadOnlyList<AppleDeviceInfo>>(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var devices = Usbmux.GetDeviceList();
                var result = new List<AppleDeviceInfo>(devices.Count);

                foreach (var device in devices
                    .GroupBy(device => device.Serial, StringComparer.Ordinal)
                    .Select(group => group
                        .OrderBy(device => GetConnectionPriority(device.ConnectionType))
                        .First()))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.Add(GetDeviceInfo(device));
                }

                return result;
            },
            cancellationToken).ConfigureAwait(false);
    }

    public void StartWatching()
    {
        if (_isWatching)
        {
            return;
        }

        _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
        Usbmux.Subscribe(OnDeviceChanged, OnWatcherError, _logger);
        _isWatching = true;
    }

    public void StopWatching()
    {
        if (!_isWatching)
        {
            return;
        }

        Usbmux.Unsubscribe();
        _isWatching = false;
    }

    public async Task<AppleDeviceInfo> PairAsync(
        AppleDeviceInfo device,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        progress?.Report("Waiting for the iPhone trust confirmation.");

        using var lockdown = MobileDevice.CreateUsingUsbmux(device.Identifier);
        var pairingProgress = new Progress<PairingState>(state => progress?.Report(state.ToString()));
        var paired = await lockdown.PairAsync(pairingProgress, cancellationToken);
        if (!paired)
        {
            throw new InvalidOperationException("The iPhone refused the pairing request.");
        }

        progress?.Report("The iPhone is paired.");
        return device with
        {
            Name = lockdown.DeviceName,
            IOSVersion = lockdown.OsVersion,
            DeviceClass = GetRawDeviceClass(lockdown),
            ProductType = lockdown.ProductType,
            IsPaired = true,
        };
    }

    public async Task<bool> GetWifiConnectionsEnabledAsync(
        AppleDeviceInfo device,
        CancellationToken cancellationToken = default)
    {
        ValidateWifiSettingsDevice(device);
        return await Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var lockdown = MobileDevice.CreateUsingUsbmux(device.Identifier);
                var value = lockdown.GetValue(WirelessLockdownDomain, EnableWifiConnectionsKey);
                return value is BooleanNode booleanNode && booleanNode.Value;
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SetWifiConnectionsEnabledAsync(
        AppleDeviceInfo device,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        ValidateWifiSettingsDevice(device);
        await Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var lockdown = MobileDevice.CreateUsingUsbmux(device.Identifier);
                lockdown.SetValue(
                    WirelessLockdownDomain,
                    EnableWifiConnectionsKey,
                    new BooleanNode(enabled));
            },
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<RingtoneTransferResult> InstallRingtoneAsync(
        AppleDeviceInfo device,
        string ringtonePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentException.ThrowIfNullOrWhiteSpace(ringtonePath);

        if (!File.Exists(ringtonePath))
        {
            throw new FileNotFoundException("The ringtone file no longer exists.", ringtonePath);
        }

        if (!string.Equals(System.IO.Path.GetExtension(ringtonePath), ".m4r", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only .m4r files can be transferred.", nameof(ringtonePath));
        }

        var adapter = _transferAdapters.FirstOrDefault(candidate => candidate.CanHandle(device));
        if (adapter is null)
        {
            return new RingtoneTransferResult(
                RingtoneTransferStatus.ManualFallbackRequired,
                "Direct ringtone synchronization is not available for this iOS version. The exported M4R file has been preserved.");
        }

        return await adapter.InstallAsync(device, ringtonePath, progress, cancellationToken);
    }

    private static AppleDeviceInfo GetDeviceInfo(UsbmuxdDevice device)
    {
        try
        {
            using var lockdown = MobileDevice.CreateUsingUsbmux(device.Serial);
            return new AppleDeviceInfo(
                device.Serial,
                lockdown.DeviceName,
                lockdown.OsVersion,
                GetRawDeviceClass(lockdown),
                lockdown.ProductType,
                MapConnectionKind(device.ConnectionType),
                lockdown.IsPaired);
        }
        catch
        {
            return new AppleDeviceInfo(
                device.Serial,
                device.Serial,
                null,
                null,
                null,
                MapConnectionKind(device.ConnectionType),
                false);
        }
    }

    private void OnDeviceChanged(UsbmuxdDevice device, UsbmuxdConnectionEventType eventType)
    {
        _logger.LogDebug("Apple device {DeviceIdentifier}: {DeviceEvent}.", device.Serial, eventType);
        _synchronizationContext?.Post(
            static state =>
            {
                var service = (NetimobiledeviceAppleDeviceService)state!;
                service.DevicesChanged?.Invoke(service, EventArgs.Empty);
            },
            this);
    }

    private static string? GetRawDeviceClass(LockdownClient lockdown)
    {
        return lockdown.GetValue(null, "DeviceClass")?.AsStringNode().Value;
    }

    private void OnWatcherError(Exception exception)
    {
        _logger.LogWarning(exception, "Apple device watcher failed.");
    }

    private static AppleDeviceConnectionKind MapConnectionKind(UsbmuxdConnectionType connectionType)
    {
        return connectionType.ToString() switch
        {
            "Usb" or "USB" => AppleDeviceConnectionKind.Usb,
            "Network" or "WiFi" => AppleDeviceConnectionKind.WiFi,
            _ => AppleDeviceConnectionKind.Unknown,
        };
    }

    private static int GetConnectionPriority(UsbmuxdConnectionType connectionType) =>
        MapConnectionKind(connectionType) switch
        {
            AppleDeviceConnectionKind.Usb => 0,
            AppleDeviceConnectionKind.WiFi => 1,
            _ => 2,
        };

    private static void ValidateWifiSettingsDevice(AppleDeviceInfo device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!device.IsPaired)
        {
            throw new InvalidOperationException("The iPhone must trust this computer first.");
        }

        if (device.ConnectionKind != AppleDeviceConnectionKind.Usb)
        {
            throw new InvalidOperationException("Connect the iPhone over USB to change Wi-Fi access.");
        }
    }

    public void Dispose()
    {
        StopWatching();
    }
}
