using iPhoneRingsMaker.Contracts.Services;

using Microsoft.Extensions.Logging;

using Netimobiledevice.Afc;

namespace iPhoneRingsMaker.Services;

internal sealed class AppleDeviceFileService(ILogger<AppleDeviceFileService> logger) : IAppleDeviceFileService
{
    private static readonly string[] MusicSearchRoots =
    [
        "/Music",
        "/Downloads",
        "/Purchases",
        "/iTunes_Control/Music",
        "/iTunes_Control/Downloads",
    ];

    public async Task CopyAsync(
        AfcService afc,
        string remotePath,
        string localPath,
        CancellationToken cancellationToken)
    {
        var contents = await afc.GetFileContents(remotePath, cancellationToken).ConfigureAwait(false)
            ?? throw new IOException($"The iPhone returned no data for {remotePath}.");
        await File.WriteAllBytesAsync(localPath, contents, cancellationToken).ConfigureAwait(false);
    }

    public async Task CopyOptionalSidecarAsync(
        AfcService afc,
        string remoteCatalogPath,
        string localCatalogPath,
        string suffix,
        CancellationToken cancellationToken)
    {
        var remotePath = remoteCatalogPath + suffix;
        if (await afc.Exists(remotePath, cancellationToken).ConfigureAwait(false))
        {
            await CopyAsync(afc, remotePath, localCatalogPath + suffix, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string> ResolveMusicPathAsync(
        AfcService afc,
        string catalogPath,
        CancellationToken cancellationToken)
    {
        if (await afc.Exists(catalogPath, cancellationToken).ConfigureAwait(false))
        {
            return catalogPath;
        }

        var fileName = Path.GetFileName(catalogPath.Replace('\\', '/'));
        foreach (var searchRoot in MusicSearchRoots)
        {
            if (!await afc.Exists(searchRoot, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            await foreach (var candidate in afc.LsDirectory(searchRoot, cancellationToken, 8).ConfigureAwait(false))
            {
                var normalizedCandidate = candidate.Replace('\\', '/');
                if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(normalizedCandidate), fileName))
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
                logger.LogDebug(
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
}
