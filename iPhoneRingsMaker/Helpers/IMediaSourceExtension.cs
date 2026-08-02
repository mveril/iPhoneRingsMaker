using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iPhoneRingsMaker.Contracts.Models;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Models;
using Windows.Media.Core;

namespace iPhoneRingsMaker.Helpers;

internal static class IMediaSourceExtension
{
    public static IMedia GetMedia(this Core.Models.IMediaSource mediaSource)
    {
        return mediaSource switch
        {
            LocalMediaSource localMediaSource => new LocalMedia(localMediaSource),
            IPhoneMediaSource iPhoneMediaSource => new IPhoneMedia(
                iPhoneMediaSource,
                App.GetService<Contracts.Services.IAppleMusicLibraryService>()),
            _ => throw new InvalidOperationException(),
        };
    }
}
