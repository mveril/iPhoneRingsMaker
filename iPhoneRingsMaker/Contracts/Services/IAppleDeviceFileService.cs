using Netimobiledevice.Afc;

namespace iPhoneRingsMaker.Contracts.Services;

public interface IAppleDeviceFileService
{
    Task CopyAsync(
        AfcService afc,
        string remotePath,
        string localPath,
        CancellationToken cancellationToken);

    Task CopyOptionalSidecarAsync(
        AfcService afc,
        string remoteCatalogPath,
        string localCatalogPath,
        string suffix,
        CancellationToken cancellationToken);

    Task<string> ResolveMusicPathAsync(
        AfcService afc,
        string catalogPath,
        CancellationToken cancellationToken);
}
