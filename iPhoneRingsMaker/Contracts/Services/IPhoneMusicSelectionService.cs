using iPhoneRingsMaker.Core.Models;

namespace iPhoneRingsMaker.Contracts.Services;

public interface IPhoneMusicSelectionService
{
    Task<IPhoneMediaSource?> PickAsync();
}
