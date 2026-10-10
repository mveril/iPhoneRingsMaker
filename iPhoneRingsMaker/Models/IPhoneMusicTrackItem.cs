using CommunityToolkit.Mvvm.ComponentModel;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Helpers;
using Microsoft.UI.Xaml.Media;

namespace iPhoneRingsMaker.Models;

public sealed partial class IPhoneMusicTrackItem : ObservableObject
{
    public IPhoneMusicTrackItem(IPhoneMusicTrack track)
    {
        Track = track;
        Artwork = ArtworkSourceFactory.CreateImageSource(track);
    }

    public IPhoneMusicTrack Track
    {
        get; private set;
    }

    public string Title => Track.Title;

    public string Artist => string.IsNullOrWhiteSpace(Track.Artist)
        ? "MusicLibrary_UnknownArtist".GetLocalized()
        : Track.Artist;

    public string Album => Track.Album ?? string.Empty;

    [ObservableProperty]
    public partial ImageSource? Artwork
    {
        get; private set;
    }

    public string Duration => Track.Duration.ToString(@"m\:ss");

    public bool IsAvailable => Track.IsAvailable;

    public double Opacity => IsAvailable ? 1 : 0.55;

    public string AvailabilityDescription => Track.Availability switch
    {
        IPhoneMusicTrackAvailability.Available => string.Empty,
        IPhoneMusicTrackAvailability.CloudOnly => "MusicLibrary_CloudOnly".GetLocalized(),
        IPhoneMusicTrackAvailability.Protected => "MusicLibrary_Protected".GetLocalized(),
        IPhoneMusicTrackAvailability.MissingFile => "MusicLibrary_Missing".GetLocalized(),
        IPhoneMusicTrackAvailability.UnsupportedFormat => "MusicLibrary_Unsupported".GetLocalized(),
        _ => "MusicLibrary_Unsupported".GetLocalized(),
    };

    public void UpdateTrack(IPhoneMusicTrack track)
    {
        if (!StringComparer.Ordinal.Equals(Track.PersistentIdentifier, track.PersistentIdentifier))
        {
            throw new ArgumentException("The updated track identifier must match the item.", nameof(track));
        }

        Track = track;
        Artwork = ArtworkSourceFactory.CreateImageSource(track);
    }

}
