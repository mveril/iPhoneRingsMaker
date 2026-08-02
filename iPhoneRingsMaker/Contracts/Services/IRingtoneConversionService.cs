using iPhoneRingsMaker.Contracts.Models;
using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Contracts.Services;

public interface IRingtoneConversionService
{
    Task ConvertAsync(
        IMedia media,
        M4RProject project,
        TimeSpan durationLimit,
        string outputPath,
        IProgress<double> progress,
        CancellationToken cancellationToken);

    void DeleteTemporaryOutput(string path);
}
