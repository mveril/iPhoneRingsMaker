using System.Runtime.InteropServices.WindowsRuntime;

using iPhoneRingsMaker.Contracts.Models;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Models;

using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;

namespace iPhoneRingsMaker.Services;

internal sealed class RingtoneConversionService : IRingtoneConversionService
{
    public async Task ConvertAsync(
        IMedia media,
        M4RProject project,
        TimeSpan durationLimit,
        string outputPath,
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(media);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var endTime = project.EndTime ?? project.StartTime + durationLimit;
        RingtoneConstraints.ValidateRange(project.StartTime, endTime, endTime);

        var inputStreamReference = await media.GetStreamAsync();
        using var inputStream = await inputStreamReference.OpenReadAsync();
        await using var outputFileStream = new FileStream(
            outputPath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            4096,
            useAsync: true);
        using var outputStream = outputFileStream.AsRandomAccessStream();
        var transcoder = new MediaTranscoder { TrimStartTime = project.StartTime };
        if (project.EndTime is not null)
        {
            transcoder.TrimStopTime = project.EndTime.Value;
        }

        var result = await transcoder.PrepareStreamTranscodeAsync(
            inputStream,
            outputStream,
            MediaEncodingProfile.CreateM4a(AudioEncodingQuality.High));
        if (!result.CanTranscode)
        {
            throw new InvalidOperationException($"The media cannot be transcoded ({result.FailureReason}).");
        }

        var operation = result.TranscodeAsync();
        operation.Progress = (_, value) => progress.Report(value);
        using var cancellationRegistration = cancellationToken.Register(operation.Cancel);
        await operation.AsTask(cancellationToken);
        progress.Report(100);
    }

    public void DeleteTemporaryOutput(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
