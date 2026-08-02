using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace iPhoneRingsMaker.Models;

internal sealed class IPhoneMedia(
    IPhoneMediaSource mediaSource,
    IAppleMusicLibraryService musicLibraryService) : MediaBase<IPhoneMediaSource>(mediaSource)
{
    public async override Task<MediaSource> GetMediaSourceAsync()
        => Windows.Media.Core.MediaSource.CreateFromStorageFile(await GetStorageFileAsync());

    public async override Task<MusicMetadata> GetMusicMetadataAsync()
    {
        var file = await GetStorageFileAsync();
        var properties = await file.Properties.GetMusicPropertiesAsync();
        var metadata = new Helpers.MusicMetadataMapper().MapMusicProperties(properties);
        if (string.IsNullOrWhiteSpace(metadata.Title))
        {
            metadata = metadata with { Title = MediaSource.TrackTitle ?? MediaSource.TrackDisplayName };
        }

        return metadata;
    }

    public async override Task<RandomAccessStreamReference> GetStreamAsync()
        => RandomAccessStreamReference.CreateFromFile(await GetStorageFileAsync());

    public async override Task<RandomAccessStreamReference?> GetArtworkStreamAsync(uint? size = null)
        => await ArtworkSourceFactory.CreateStreamReferenceAsync(
            await musicLibraryService.GetTrackAsync(MediaSource));

    private async Task<StorageFile> GetStorageFileAsync()
    {
        var path = await musicLibraryService.GetCachedTrackPathAsync(MediaSource);
        return await StorageFile.GetFileFromPathAsync(path);
    }
}
