using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iPhoneRingsMaker.Core.Models;
/// <summary>
/// Represents metadata for a music track, encapsulating all relevant information.
/// </summary>
public record MusicMetadata
{
    public string? Title
    {
        get; init;
    }
    public string? Artist
    {
        get; init;
    }
    public string? Album
    {
        get; init;
    }
    public string? AlbumArtist
    {
        get; init;
    }
    public uint? TrackNumber
    {
        get; init;
    }
    public uint? Year
    {
        get; init;
    }
    public IReadOnlyList<string> Genre { get; init; } = [];
    public required TimeSpan Duration
    {
        get; init;
    }
    public double? Bitrate
    {
        get; init;
    }
    public IReadOnlyList<string> Composers { get; init; } = [];
}

