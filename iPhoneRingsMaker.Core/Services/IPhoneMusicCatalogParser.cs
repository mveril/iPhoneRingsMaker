using iPhoneRingsMaker.Core.Models;

using Microsoft.Data.Sqlite;

namespace iPhoneRingsMaker.Core.Services;

public sealed class IPhoneMusicCatalogParser
{
    private const long MusicMediaKind = 1;

    private static readonly IReadOnlySet<string> SupportedFormats = new HashSet<string>(
        ["m4a", "mp3", "wav", "aac", "wma", "flac"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly object ProviderLock = new();
    private static bool _providerInitialized;

    public async Task<IReadOnlyList<IPhoneMusicTrack>> ParseAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        EnsureProviderInitialized();

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        };
        await using var connection = new SqliteConnection(builder.ToString());
        await connection.OpenAsync(cancellationToken);

        var columns = await ReadColumnsAsync(connection, "item_extra", cancellationToken);
        var tables = await ReadTablesAsync(connection, cancellationToken);
        if (!columns.Contains("item_pid") ||
            !columns.Contains("title") ||
            !columns.Contains("media_kind"))
        {
            throw new InvalidDataException("The iPhone music catalog schema is not supported.");
        }

        // The available column list comes from the connected MediaLibrary.sqlitedb itself and is
        // read at runtime with PRAGMA table_info(item_extra); no user database was used to hard-code
        // it here. Apple does not publish this private SQLite schema. The media_kind mapping is based
        // on the public DFRWS Apple Watch forensic study (1 = music), while the public MPMediaItem
        // documentation is used only as the semantic reference for music metadata:
        // https://dfrws.org/wp-content/uploads/2019/06/2019_EU-pres-apple_watch_forensics_is_it_ever_possible_and_what_is_the_profit.pdf
        // https://developer.apple.com/documentation/mediaplayer/mpmediaitem
        var locationColumn = PickColumn(columns, "file", "location", "file_path", "path");
        var formatColumn = PickColumn(columns, "file_format", "format");
        var durationColumn = PickColumn(columns, "total_time_ms", "total_time");
        var fileSizeColumn = PickColumn(columns, "file_size", "size");
        var protectedColumn = PickColumn(columns, "is_protected", "protected_content", "drm_version_number");
        var artistColumn = PickColumn(columns, "artist", "item_artist");
        var albumColumn = PickColumn(columns, "album", "album_name");
        var artworkIdentifierColumn = PickColumn(
            columns,
            "artwork_cache_id",
            "artwork_id",
            "artwork_token");
        var artworkUrlColumn = PickColumn(columns, "artwork_url", "artworkURL");
        var artworkDataColumn = PickColumn(columns, "artwork_data", "artwork");
        var itemArtistColumns = tables.Contains("item_artist")
            ? await ReadColumnsAsync(connection, "item_artist", cancellationToken)
            : [];
        var baseLocationColumns = tables.Contains("base_location")
            ? await ReadColumnsAsync(connection, "base_location", cancellationToken)
            : [];
        var itemStoreColumns = tables.Contains("item_store")
            ? await ReadColumnsAsync(connection, "item_store", cancellationToken)
            : [];
        var albumColumns = tables.Contains("album")
            ? await ReadColumnsAsync(connection, "album", cancellationToken)
            : [];
        var normalizedArtistColumn = PickColumn(itemArtistColumns, "artist", "item_artist", "name");
        var normalizedAlbumColumn = PickColumn(albumColumns, "album", "title", "name");
        var basePathColumn = PickColumn(baseLocationColumns, "path", "base_path", "location");
        var storeProtectedColumn = PickColumn(itemStoreColumns, "is_protected");
        var canJoinArtist = tables.Contains("item") &&
            tables.Contains("item_artist") &&
            normalizedArtistColumn is not null;
        var canJoinBaseLocation = !StringComparer.OrdinalIgnoreCase.Equals(locationColumn, "file") &&
            tables.Contains("item") &&
            tables.Contains("base_location") &&
            basePathColumn is not null;
        var canJoinAlbum = tables.Contains("item") &&
            tables.Contains("album") &&
            normalizedAlbumColumn is not null;
        var canResolveArtwork = tables.Contains("item") && tables.Contains("best_artwork_token");
        var canResolveLocalArtwork = canResolveArtwork && tables.Contains("artwork");
        var artistSelect = SelectOrNull(artistColumn, "e") ??
            (canJoinArtist
                ? $"ia.\"{normalizedArtistColumn}\""
                : "NULL");
        var albumSelect = SelectOrNull(albumColumn, "e") ??
            (canJoinAlbum
                ? $"a.\"{normalizedAlbumColumn}\""
                : "NULL");
        var itemJoin = canJoinArtist || canJoinBaseLocation || canJoinAlbum || canResolveArtwork
            ? "LEFT JOIN item i ON i.item_pid = e.item_pid"
            : string.Empty;
        var artistJoin = canJoinArtist
            ? "LEFT JOIN item_artist ia ON ia.item_artist_pid = i.item_artist_pid"
            : string.Empty;
        var baseLocationJoin = canJoinBaseLocation
            ? "LEFT JOIN base_location bl ON bl.base_location_id = i.base_location_id"
            : string.Empty;
        var albumJoin = canJoinAlbum
            ? "LEFT JOIN album a ON a.album_pid = i.album_pid"
            : string.Empty;
        var itemStoreJoin = tables.Contains("item_store")
            ? "LEFT JOIN item_store ist ON ist.item_pid = e.item_pid"
            : string.Empty;
        var protectedTerms = new List<string>();
        if (protectedColumn is not null)
        {
            protectedTerms.Add($"COALESCE(e.\"{protectedColumn}\", 0) != 0");
        }

        if (storeProtectedColumn is not null)
        {
            protectedTerms.Add($"COALESCE(ist.\"{storeProtectedColumn}\", 0) != 0");
        }

        var protectedSelect = protectedTerms.Count == 0
            ? "NULL"
            : $"CASE WHEN {string.Join(" OR ", protectedTerms)} THEN 1 ELSE 0 END";
        var normalizedArtworkIdentifierSelect = canResolveArtwork
            ? """
                COALESCE(
                    (SELECT NULLIF(bat.available_artwork_token, '')
                     FROM best_artwork_token bat
                     WHERE bat.entity_pid = e.item_pid
                     LIMIT 1),
                    (SELECT NULLIF(bat.available_artwork_token, '')
                     FROM best_artwork_token bat
                     WHERE bat.entity_pid = i.album_pid
                     LIMIT 1))
                """
            : "NULL";
        var normalizedArtworkUrlSelect = canResolveArtwork
            ? """
                COALESCE(
                    (SELECT NULLIF(bat.fetchable_artwork_token, '')
                     FROM best_artwork_token bat
                     WHERE bat.entity_pid = e.item_pid
                     LIMIT 1),
                    (SELECT NULLIF(bat.fetchable_artwork_token, '')
                     FROM best_artwork_token bat
                     WHERE bat.entity_pid = i.album_pid
                     LIMIT 1))
                """
            : "NULL";
        var artworkIdentifierSelect = artworkIdentifierColumn is null
            ? normalizedArtworkIdentifierSelect
            : $"COALESCE(NULLIF(e.\"{artworkIdentifierColumn}\", ''), {normalizedArtworkIdentifierSelect})";
        var artworkUrlSelect = artworkUrlColumn is null
            ? normalizedArtworkUrlSelect
            : $"COALESCE(NULLIF(e.\"{artworkUrlColumn}\", ''), {normalizedArtworkUrlSelect})";
        var artworkRemotePathSelect = canResolveLocalArtwork
            ? """
                COALESCE(
                    (SELECT '/iTunes_Control/iTunes/Artwork/Originals/' || ar.relative_path
                     FROM best_artwork_token bat
                     JOIN artwork ar ON ar.artwork_token = bat.available_artwork_token
                     WHERE bat.entity_pid = e.item_pid AND NULLIF(ar.relative_path, '') IS NOT NULL
                     LIMIT 1),
                    (SELECT '/iTunes_Control/iTunes/Artwork/Originals/' || ar.relative_path
                     FROM best_artwork_token bat
                     JOIN artwork ar ON ar.artwork_token = bat.available_artwork_token
                     WHERE bat.entity_pid = i.album_pid AND NULLIF(ar.relative_path, '') IS NOT NULL
                     LIMIT 1))
                """
            : "NULL";

        var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                e.item_pid,
                e.title,
                {SelectOrNull(locationColumn)} AS location,
                {SelectOrNull(formatColumn)} AS file_format,
                {SelectOrNull(durationColumn)} AS total_time,
                {SelectOrNull(fileSizeColumn)} AS file_size,
                {protectedSelect} AS protected_value,
                {artistSelect} AS artist,
                {albumSelect} AS album,
                {artworkIdentifierSelect} AS artwork_identifier,
                {artworkUrlSelect} AS artwork_url,
                {SelectOrNull(artworkDataColumn)} AS artwork_data,
                {(canJoinBaseLocation ? $"bl.\"{basePathColumn}\"" : "NULL")} AS base_path,
                {artworkRemotePathSelect} AS artwork_remote_path
            FROM item_extra e
            {itemJoin}
            {artistJoin}
            {baseLocationJoin}
            {albumJoin}
            {itemStoreJoin}
            WHERE e.title IS NOT NULL AND e.media_kind = $musicMediaKind
            ORDER BY e.title COLLATE NOCASE
            """;
        command.Parameters.AddWithValue("$musicMediaKind", MusicMediaKind);

        var tracks = new List<IPhoneMusicTrack>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var identifier = Convert.ToString(reader.GetValue(0), System.Globalization.CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(identifier))
            {
                continue;
            }

            var title = GetOptionalString(reader, 1) ?? identifier;
            var location = NormalizeRemotePath(
                GetOptionalString(reader, 2),
                GetOptionalString(reader, 12));
            var format = NormalizeFormat(GetOptionalString(reader, 3), location);
            var durationMilliseconds = GetOptionalInt64(reader, 4) ?? 0;
            var isProtected = IsProtected(reader, 6);
            var availability = GetAvailability(location, format, isProtected);
            tracks.Add(new IPhoneMusicTrack(
                identifier,
                title,
                GetOptionalString(reader, 7),
                GetOptionalString(reader, 8),
                TimeSpan.FromMilliseconds(Math.Max(0, durationMilliseconds)),
                location,
                format,
                GetOptionalInt64(reader, 5),
                availability,
                GetOptionalString(reader, 9),
                GetOptionalString(reader, 10),
                GetOptionalBytes(reader, 11),
                GetOptionalString(reader, 13)));
        }

        return tracks;
    }

    private static void EnsureProviderInitialized()
    {
        lock (ProviderLock)
        {
            if (_providerInitialized)
            {
                return;
            }

            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_winsqlite3());
            _providerInitialized = true;
        }
    }

    private static async Task<HashSet<string>> ReadColumnsAsync(
        SqliteConnection connection,
        string table,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({table})";
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }

    private static async Task<HashSet<string>> ReadTablesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static string? PickColumn(HashSet<string> columns, params string[] candidates)
        => candidates.FirstOrDefault(candidate => columns.Contains(candidate));

    private static string SelectOrNull(string? column) => column is null ? "NULL" : $"\"{column}\"";

    private static string? SelectOrNull(string? column, string alias)
        => column is null ? null : $"{alias}.\"{column}\"";

    private static string? GetOptionalString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal));

    private static long? GetOptionalInt64(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return Convert.ToInt64(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static byte[]? GetOptionalBytes(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return reader.GetValue(ordinal) as byte[];
    }

    private static bool IsProtected(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return false;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            bool boolean => boolean,
            string text => !string.IsNullOrWhiteSpace(text) && text != "0",
            _ => Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture) != 0,
        };
    }

    private static string? NormalizeRemotePath(string? location, string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return null;
        }

        var normalized = Uri.UnescapeDataString(location.Trim());
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            normalized = $"{basePath.TrimEnd('/', '\\')}/{normalized.TrimStart('/', '\\')}";
        }
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            normalized = uri.AbsolutePath;
        }

        normalized = normalized.Replace('\\', '/').TrimStart('/');
        if (normalized.StartsWith("iTunes_Control/", StringComparison.OrdinalIgnoreCase))
        {
            return $"/{normalized}";
        }

        if (normalized.StartsWith("Music/", StringComparison.OrdinalIgnoreCase))
        {
            return $"/{normalized}";
        }

        return $"/iTunes_Control/Music/{normalized}";
    }

    private static string? NormalizeFormat(string? format, string? location)
    {
        var value = string.IsNullOrWhiteSpace(format)
            ? Path.GetExtension(location)
            : format;
        return value?.Trim().TrimStart('.').ToLowerInvariant();
    }

    private static IPhoneMusicTrackAvailability GetAvailability(
        string? location,
        string? format,
        bool isProtected)
    {
        if (isProtected)
        {
            return IPhoneMusicTrackAvailability.Protected;
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            return IPhoneMusicTrackAvailability.CloudOnly;
        }

        if (string.IsNullOrWhiteSpace(format) || !SupportedFormats.Contains(format))
        {
            return IPhoneMusicTrackAvailability.UnsupportedFormat;
        }

        return IPhoneMusicTrackAvailability.Available;
    }
}
