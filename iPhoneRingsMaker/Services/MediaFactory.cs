using iPhoneRingsMaker.Contracts.Models;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Models;

namespace iPhoneRingsMaker.Services;

public sealed class MediaFactory : IMediaFactory
{
    private readonly IAppleMusicLibraryService _musicLibraryService;

    public MediaFactory(IAppleMusicLibraryService musicLibraryService)
    {
        _musicLibraryService = musicLibraryService;
    }

    public IMedia Create(Core.Models.IMediaSource mediaSource) => mediaSource switch
    {
        LocalMediaSource localMediaSource => new LocalMedia(localMediaSource),
        IPhoneMediaSource iPhoneMediaSource => new IPhoneMedia(iPhoneMediaSource, _musicLibraryService),
        _ => throw new NotSupportedException($"Unsupported media source type: {mediaSource.GetType().FullName}"),
    };
}
