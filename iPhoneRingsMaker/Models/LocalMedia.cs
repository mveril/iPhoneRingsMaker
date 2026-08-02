using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Helpers;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace iPhoneRingsMaker.Models;

internal class LocalMedia : MediaBase<LocalMediaSource>
{
    public LocalMedia(LocalMediaSource mediaSource) : base(mediaSource)
    {

    }

    public async override Task<MediaSource> GetMediaSourceAsync()
    {
        return Windows.Media.Core.MediaSource.CreateFromStorageFile(await MediaSource.GetStorageFileAsync());
    }

    public async override Task<MusicMetadata> GetMusicMetadataAsync()
    {
        // await base.UpdateDisplayPropertiesAsync(properties);
        var file = await MediaSource.GetStorageFileAsync();
        var musicProps = await file.Properties.GetMusicPropertiesAsync();
        var mapper = new MusicMetadataMapper();
        return mapper.MapMusicProperties(musicProps);
    }

    public async override Task<RandomAccessStreamReference> GetStreamAsync()
    {
        return RandomAccessStreamReference.CreateFromFile(await MediaSource.GetStorageFileAsync());
    }

    public async override Task<RandomAccessStreamReference?> GetArtworkStreamAsync(uint? size = null)
    {
        var file = await MediaSource.GetStorageFileAsync();
        StorageItemThumbnail thumb;
        if (size.HasValue)
        {
            thumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView, size.Value);

        }
        else
        {
            thumb = await file.GetThumbnailAsync(ThumbnailMode.MusicView);
        }
        return thumb.Type switch
        {
            ThumbnailType.Image => RandomAccessStreamReference.CreateFromStream(thumb),
            ThumbnailType.Icon => null,
            _ => throw new UnreachableException($"Unknown value of {typeof(ThumbnailMode).FullName} {thumb.Type}."),
        };
    }
}
