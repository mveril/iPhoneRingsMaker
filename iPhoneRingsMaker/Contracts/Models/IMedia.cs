using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iPhoneRingsMaker.Core.Models;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace iPhoneRingsMaker.Contracts.Models;

public interface IMedia
{
    Task<MediaPlaybackItem> GetMediaPlaybackItemAsync();
    Task<MediaPlaybackItem> GetMediaPlaybackItemAsync(TimeSpan startTime, TimeSpan duration);
    Task<MediaPlaybackItem> GetMediaPlaybackItemAsync(TimeSpan startTime);
    Task<MediaSource> GetMediaSourceAsync();
    Task<RandomAccessStreamReference> GetStreamAsync();
    Task<ImageSource?> GetArtworkAsync(uint? size = null);
    Task<RandomAccessStreamReference?> GetArtworkStreamAsync(uint? size = null);
    public Task<MusicMetadata> GetMusicMetadataAsync();
}
