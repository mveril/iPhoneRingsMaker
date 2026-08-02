using iPhoneRingsMaker.Models;

namespace iPhoneRingsMaker.Contracts.Services;

public interface IAppleDeviceService : IDisposable
{
    event EventHandler? DevicesChanged;

    Task<IReadOnlyList<AppleDeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default);

    void StartWatching();

    void StopWatching();

    Task<AppleDeviceInfo> PairAsync(
        AppleDeviceInfo device,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    Task<bool> GetWifiConnectionsEnabledAsync(
        AppleDeviceInfo device,
        CancellationToken cancellationToken = default);

    Task SetWifiConnectionsEnabledAsync(
        AppleDeviceInfo device,
        bool enabled,
        CancellationToken cancellationToken = default);

    Task<RingtoneTransferResult> InstallRingtoneAsync(
        AppleDeviceInfo device,
        string ringtonePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
