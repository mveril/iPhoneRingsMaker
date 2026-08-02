using iPhoneRingsMaker.Contracts.Models;
using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Contracts.Services;

public interface IMediaFileNameService
{
    Task<string> GetSuggestedNameAsync(
        IMediaSource source,
        IMedia? media = null,
        CancellationToken cancellationToken = default);
}
