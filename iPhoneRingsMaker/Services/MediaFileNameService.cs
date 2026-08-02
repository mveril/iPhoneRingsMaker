using iPhoneRingsMaker.Contracts.Models;
using iPhoneRingsMaker.Contracts.Services;
using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Helpers;

namespace iPhoneRingsMaker.Services;

public sealed class MediaFileNameService : IMediaFileNameService
{
    public async Task<string> GetSuggestedNameAsync(
        IMediaSource source,
        IMedia? media = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        string? title = null;
        try
        {
            var metadata = await (media ?? source.GetMedia()).GetMusicMetadataAsync();
            title = metadata.Title;
        }
        catch (Exception)
        {
            // Metadata is only a naming hint; source-specific fallbacks remain valid.
        }

        var fallback = source switch
        {
            LocalMediaSource local => Path.GetFileNameWithoutExtension(local.Path),
            IPhoneMediaSource iPhone => iPhone.TrackDisplayName,
            _ => null,
        };

        var candidate = string.IsNullOrWhiteSpace(title) ? fallback : title;
        return Sanitize(candidate);
    }

    private static string Sanitize(string? value)
    {
        var name = value?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return "ringtone";
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(name
            .Where(character => !invalidCharacters.Contains(character))
            .ToArray())
            .Trim()
            .TrimEnd('.');

        return string.IsNullOrWhiteSpace(sanitized) ? "ringtone" : sanitized;
    }
}
