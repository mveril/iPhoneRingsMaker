using iPhoneRingsMaker.Contracts.Models;

namespace iPhoneRingsMaker.Contracts.Services;

public interface IMediaFactory
{
    IMedia Create(Core.Models.IMediaSource mediaSource);
}
