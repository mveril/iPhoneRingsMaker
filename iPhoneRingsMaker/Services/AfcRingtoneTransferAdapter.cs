using System.Security.Cryptography;

using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Core.Services;
using iPhoneRingsMaker.Models;

using Microsoft.Extensions.Logging;

using Netimobiledevice;
using Netimobiledevice.Afc;

namespace iPhoneRingsMaker.Services;

public sealed class AfcRingtoneTransferAdapter(
    ILogger<AfcRingtoneTransferAdapter> logger) : IRingtoneTransferAdapter
{
    private const string RingtonesDirectory = "/iTunes_Control/Ringtones";
    private const string RingtonesPlistPath = "/iTunes_Control/iTunes/Ringtones.plist";

    public bool CanHandle(AppleDeviceInfo device)
    {
        Span<String> supportedDeviceType = ["IPhone", "IPod", "IPad"];
        return device.IsPaired
            && device.IOSVersion is { Major: >= 5 }
            && supportedDeviceType.Contains(device.DeviceClass, StringComparer.InvariantCultureIgnoreCase);

    }

    public async Task<RingtoneTransferResult> InstallAsync(
        AppleDeviceInfo device,
        string ringtonePath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            progress?.Report(0);
            var ringtoneContents = await File.ReadAllBytesAsync(ringtonePath, cancellationToken);
            var duration = M4RDurationReader.Read(ringtoneContents);
            using var lockdown = MobileDevice.CreateUsingUsbmux(device.Identifier);
            await using var syncSession = await AppleSyncSession.StartAsync(
                lockdown,
                logger,
                cancellationToken).ConfigureAwait(false);
            var afc = syncSession.Afc;

            var plistExisted = await afc.Exists(RingtonesPlistPath, cancellationToken).ConfigureAwait(false);
            var originalPlist = plistExisted
                ? await afc.GetFileContents(RingtonesPlistPath, cancellationToken).ConfigureAwait(false)
                : null;
            await using var transaction = new RingtoneTransferTransaction(
                afc,
                RingtonesPlistPath,
                originalPlist,
                plistExisted,
                logger);

            var ringtoneFileName = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}.m4r";
            var remoteRingtonePath = $"{RingtonesDirectory}/{ringtoneFileName}";
            var updatedPlist = RingtoneCatalog.AddRingtone(
                originalPlist,
                ringtoneFileName,
                Path.GetFileNameWithoutExtension(ringtonePath),
                duration);

            transaction.TrackRingtone(remoteRingtonePath);
            await WriteFileAsync(
                afc,
                remoteRingtonePath,
                ringtoneContents,
                progress,
                0.05,
                0.8,
                cancellationToken).ConfigureAwait(false);
            transaction.TrackCatalogWrite();
            await WriteFileAsync(
                afc,
                RingtonesPlistPath,
                updatedPlist,
                progress,
                0.8,
                0.95,
                cancellationToken).ConfigureAwait(false);

            var verifiedRingtone = await afc.GetFileContents(
                remoteRingtonePath,
                cancellationToken).ConfigureAwait(false);
            var verifiedPlist = await afc.GetFileContents(
                RingtonesPlistPath,
                cancellationToken).ConfigureAwait(false);
            if (!ringtoneContents.AsSpan().SequenceEqual(verifiedRingtone)
                || !updatedPlist.AsSpan().SequenceEqual(verifiedPlist))
            {
                throw new IOException("The ringtone could not be verified after transfer.");
            }

            transaction.Commit();
            progress?.Report(1);
            return new RingtoneTransferResult(
                RingtoneTransferStatus.Transferred,
                $"“{Path.GetFileNameWithoutExtension(ringtonePath)}” was transferred to {device.Name}.");
        }
        catch (OperationCanceledException)
        {
            return new RingtoneTransferResult(
                RingtoneTransferStatus.Cancelled,
                "The ringtone transfer was cancelled.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ringtone transfer to Apple device {DeviceIdentifier} failed.",
                device.Identifier);
            return new RingtoneTransferResult(
                RingtoneTransferStatus.Failed,
                $"The ringtone could not be transferred: {exception.Message}");
        }
    }

    private static async Task WriteFileAsync(
        AfcService afc,
        string path,
        byte[] contents,
        IProgress<double>? progress,
        double progressStart,
        double progressEnd,
        CancellationToken cancellationToken)
    {
        progress?.Report(progressStart);
        await afc.SetFileContents(path, contents, cancellationToken).ConfigureAwait(false);
        progress?.Report(progressEnd);
    }

}
