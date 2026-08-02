using iPhoneRingsMaker.Models;

namespace iPhoneRingsMaker.Contracts.Services;

public interface IRingtoneTransferAdapter
{
    bool CanHandle(AppleDeviceInfo device);

    Task<RingtoneTransferResult> InstallAsync(
        AppleDeviceInfo device,
        string ringtonePath,
        IProgress<double>? progress,
        CancellationToken cancellationToken);
}
