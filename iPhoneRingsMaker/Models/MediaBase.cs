using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using iPhoneRingsMaker.Contracts.Models;
using iPhoneRingsMaker.Core.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using IMediaSource = iPhoneRingsMaker.Core.Models.IMediaSource;

namespace iPhoneRingsMaker.Models;

public abstract class MediaBase<TSource> : IMedia<TSource> where TSource : IMediaSource
{
    public TSource MediaSource
    {
        get; init;
    }

    protected MediaBase(TSource mediaSource)
    {
        MediaSource = mediaSource;
    }

    protected async Task<MediaPlaybackItem> GetMediaPlaybackItemAsyncInternal(TimeSpan? startTime = null, TimeSpan? duration = null)
    {
        var source = await GetMediaSourceAsync();
        if (startTime == TimeSpan.Zero)
        {
            startTime = null;
        }
        var playback = (startTime, duration) switch
        {
            (null, null) => new MediaPlaybackItem(source),
            (null, _) => new MediaPlaybackItem(source, TimeSpan.Zero, duration.Value),
            (_, null) => new MediaPlaybackItem(source, startTime.Value),
            (_, _) => new MediaPlaybackItem(source, startTime.Value, duration.Value),
        };
        var metadata = await GetMusicMetadataAsync();
        var artwork = await TryGetArtworkStreamAsync();
        var properties = playback.GetDisplayProperties();
        if (artwork is not null)
        {
            properties.Thumbnail = artwork;
        }
        properties.MusicProperties.AlbumTitle = metadata.Album;
        properties.MusicProperties.AlbumArtist = metadata.AlbumArtist;
        properties.MusicProperties.Artist = metadata.Artist;
        properties.MusicProperties.Title = metadata.Title;
        foreach (var genre in metadata.Genre)
        {
            properties.MusicProperties.Genres.Add(genre);
        }
        playback.ApplyDisplayProperties(properties);
        return playback;
    }

    public async Task<MediaPlaybackItem> GetMediaPlaybackItemAsync()
    {
        return await GetMediaPlaybackItemAsyncInternal();
    }

    public async Task<MediaPlaybackItem> GetMediaPlaybackItemAsync(TimeSpan startTime)
    {
        return await GetMediaPlaybackItemAsyncInternal(startTime);
    }

    public async Task<MediaPlaybackItem> GetMediaPlaybackItemAsync(TimeSpan startTime, TimeSpan duration)
    {
        return await GetMediaPlaybackItemAsyncInternal(startTime, duration);
    }

    public async virtual Task UpdateDisplayPropertiesAsync(MediaItemDisplayProperties properties)
    {
        properties.Thumbnail = await TryGetArtworkStreamAsync();
    }

    public abstract Task<MediaSource> GetMediaSourceAsync();
    public abstract Task<RandomAccessStreamReference> GetStreamAsync();
    public abstract Task<RandomAccessStreamReference?> GetArtworkStreamAsync(uint? size = null);
    public async virtual Task<ImageSource?> GetArtworkAsync(uint? size = null)
    {
        try
        {
            var streamReference = await TryGetArtworkStreamAsync(size);
            if (streamReference is null)
            {
                return null;
            }

            var image = new BitmapImage();
            using var stream = await streamReference.OpenReadAsync();
            await image.SetSourceAsync(stream);
            return image;
        }
        catch (Exception)
        {
            // Invalid or unavailable artwork must not prevent the media from loading.
            return null;
        }
    }

    private async Task<RandomAccessStreamReference?> TryGetArtworkStreamAsync(uint? size = null)
    {
        try
        {
            return await GetArtworkStreamAsync(size);
        }
        catch (Exception)
        {
            // Artwork is optional and must never prevent media playback or editing.
            return null;
        }
    }

    public abstract Task<MusicMetadata> GetMusicMetadataAsync();
}
