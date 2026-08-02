using System.Runtime.InteropServices.WindowsRuntime;

using iPhoneRingsMaker.Core.Models;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage.Streams;

namespace iPhoneRingsMaker.Models;

internal static class ArtworkSourceFactory
{
    public static ImageSource? CreateImageSource(IPhoneMusicTrack track)
    {
        if (track.ArtworkData is { Length: > 0 } artworkData)
        {
            var bitmap = new BitmapImage();
            _ = SetArtworkSourceAsync(bitmap, artworkData);
            return bitmap;
        }

        return Uri.TryCreate(track.ArtworkUrl, UriKind.Absolute, out var artworkUri)
            ? new BitmapImage(artworkUri)
            : null;
    }

    public static async Task<RandomAccessStreamReference?> CreateStreamReferenceAsync(
        IPhoneMusicTrack track)
    {
        if (track.ArtworkData is { Length: > 0 } artworkData)
        {
            var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(artworkData.AsBuffer());
            stream.Seek(0);
            return RandomAccessStreamReference.CreateFromStream(stream);
        }

        return Uri.TryCreate(track.ArtworkUrl, UriKind.Absolute, out var artworkUri)
            ? RandomAccessStreamReference.CreateFromUri(artworkUri)
            : null;
    }

    private static async Task SetArtworkSourceAsync(BitmapImage bitmap, byte[] artworkData)
    {
        try
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(artworkData.AsBuffer());
            stream.Seek(0);
            await bitmap.SetSourceAsync(stream);
        }
        catch (Exception)
        {
            // The track remains usable when its artwork cannot be decoded.
        }
    }
}
