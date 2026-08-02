using iPhoneRingsMaker.Core.Contracts.Services;
using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Core.Services;

public sealed class M4RProjectFactory : IM4RProjectFactory
{
    public M4RProject Create(IMediaSource mediaSource)
    {
        ArgumentNullException.ThrowIfNull(mediaSource);
        return new M4RProject { MediaSource = mediaSource };
    }
}
