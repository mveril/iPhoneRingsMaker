using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Core.Contracts.Services;

public interface IM4RProjectFactory
{
    M4RProject Create(IMediaSource mediaSource);
}
