using iPhoneRingsMaker.Core.Models;
using iPhoneRingsMaker.Core.Services;

using Microsoft.Data.Sqlite;

namespace iPhoneRingsMaker.Core.Tests;

public sealed class IPhoneMusicCatalogParserTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"iphone-music-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task ParseAsync_ClassifiesLocalProtectedAndCloudTracks()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        InitializeProvider();
        await using (var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE item_extra (
                    item_pid INTEGER NOT NULL,
                    title TEXT NOT NULL,
                    media_kind INTEGER NOT NULL,
                    artist TEXT,
                    album TEXT,
                    location TEXT,
                    file_format TEXT,
                    total_time_ms INTEGER,
                    file_size INTEGER,
                    is_protected INTEGER,
                    artwork_cache_id TEXT,
                    artwork_url TEXT,
                    artwork_data BLOB
                );
                INSERT INTO item_extra VALUES
                    (1, 'Local', 1, 'Artist', 'Album', 'F01/LOCAL.m4a', 'm4a', 12345, 1000, 0,
                        'artwork-1', 'https://example.test/cover.jpg', X'010203'),
                    (2, 'Protected', 1, NULL, NULL, 'F01/DRM.m4a', 'm4a', 20000, 2000, 1,
                        NULL, NULL, NULL),
                    (3, 'Cloud', 1, NULL, NULL, NULL, 'm4a', 30000, NULL, 0,
                        NULL, NULL, NULL),
                    (4, 'Podcast', 4, 'Host', NULL, 'F01/PODCAST.mp3', 'mp3', 40000, 4000, 0,
                        NULL, NULL, NULL);
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var tracks = await new IPhoneMusicCatalogParser().ParseAsync(_databasePath, cancellationToken);

        var local = Assert.Single(tracks, track => track.Title == "Local");
        Assert.Equal("/iTunes_Control/Music/F01/LOCAL.m4a", local.RemotePath);
        Assert.Equal(IPhoneMusicTrackAvailability.Available, local.Availability);
        Assert.Equal("artwork-1", local.ArtworkIdentifier);
        Assert.Equal("https://example.test/cover.jpg", local.ArtworkUrl);
        Assert.Equal([1, 2, 3], local.ArtworkData);
        Assert.DoesNotContain(tracks, track => track.Title == "Podcast");
        Assert.Equal(
            IPhoneMusicTrackAvailability.Protected,
            Assert.Single(tracks, track => track.Title == "Protected").Availability);
        Assert.Equal(
            IPhoneMusicTrackAvailability.CloudOnly,
            Assert.Single(tracks, track => track.Title == "Cloud").Availability);
    }

    [Fact]
    public async Task ParseAsync_ResolvesArtistFromItemArtistTable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        InitializeProvider();
        await using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE item_extra (item_pid INTEGER, title TEXT, media_kind INTEGER, location TEXT);
            CREATE TABLE item (item_pid INTEGER, item_artist_pid INTEGER);
            CREATE TABLE item_artist (item_artist_pid INTEGER, item_artist TEXT);
            INSERT INTO item_extra VALUES (1, 'Song', 1, 'F01/SONG.m4a');
            INSERT INTO item VALUES (1, 7);
            INSERT INTO item_artist VALUES (7, 'Real artist');
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        var track = Assert.Single(await new IPhoneMusicCatalogParser().ParseAsync(_databasePath, cancellationToken));
        Assert.Equal("Real artist", track.Artist);
    }

    [Fact]
    public async Task ParseAsync_CombinesBaseLocationWithFileName()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        InitializeProvider();
        await using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE item_extra (item_pid INTEGER, title TEXT, media_kind INTEGER, location TEXT);
            CREATE TABLE item (item_pid INTEGER, base_location_id INTEGER);
            CREATE TABLE base_location (base_location_id INTEGER, path TEXT);
            INSERT INTO item_extra VALUES (1, 'Song', 1, '-6176109735475747593.m4a');
            INSERT INTO item VALUES (1, 9);
            INSERT INTO base_location VALUES (9, 'Music/Downloads');
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        var track = Assert.Single(await new IPhoneMusicCatalogParser().ParseAsync(_databasePath, cancellationToken));
        Assert.Equal(
            "/Music/Downloads/-6176109735475747593.m4a",
            track.RemotePath);
    }

    [Fact]
    public async Task ParseAsync_PreservesITunesControlBaseLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        InitializeProvider();
        await using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE item_extra (item_pid INTEGER, title TEXT, media_kind INTEGER, location TEXT);
            CREATE TABLE item (item_pid INTEGER, base_location_id INTEGER);
            CREATE TABLE base_location (base_location_id INTEGER, path TEXT);
            INSERT INTO item_extra VALUES (1, 'Synced song', 1, 'TTYX.m4a');
            INSERT INTO item VALUES (1, 9);
            INSERT INTO base_location VALUES (9, 'iTunes_Control/Music/F35');
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        var track = Assert.Single(await new IPhoneMusicCatalogParser().ParseAsync(_databasePath, cancellationToken));
        Assert.Equal("/iTunes_Control/Music/F35/TTYX.m4a", track.RemotePath);
    }

    [Fact]
    public async Task ParseAsync_ResolvesNormalizedAlbumAndLocalArtwork()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        InitializeProvider();
        await using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE item_extra (item_pid INTEGER, title TEXT, media_kind INTEGER, location TEXT);
            CREATE TABLE item (item_pid INTEGER, album_pid INTEGER);
            CREATE TABLE album (album_pid INTEGER, album TEXT);
            CREATE TABLE best_artwork_token (
                entity_pid INTEGER,
                available_artwork_token TEXT,
                fetchable_artwork_token TEXT
            );
            CREATE TABLE artwork (artwork_token TEXT, relative_path TEXT);
            INSERT INTO item_extra VALUES (1, 'Song', 1, 'song.m4a');
            INSERT INTO item VALUES (1, 9);
            INSERT INTO album VALUES (9, 'Album title');
            INSERT INTO best_artwork_token VALUES (1, 'local-token', '');
            INSERT INTO best_artwork_token VALUES (9, 'album-token', 'https://example.test/artwork.jpg');
            INSERT INTO artwork VALUES ('local-token', '75/local-artwork');
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        var track = Assert.Single(await new IPhoneMusicCatalogParser().ParseAsync(_databasePath, cancellationToken));
        Assert.Equal("Album title", track.Album);
        Assert.Equal("local-token", track.ArtworkIdentifier);
        Assert.Equal("https://example.test/artwork.jpg", track.ArtworkUrl);
        Assert.Equal(
            "/iTunes_Control/iTunes/Artwork/Originals/75/local-artwork",
            track.ArtworkRemotePath);
    }

    [Fact]
    public async Task ParseAsync_PrefersPhysicalFileColumnOverLogicalLocation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        InitializeProvider();
        await using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE item_extra (
                item_pid INTEGER,
                title TEXT,
                media_kind INTEGER,
                location TEXT,
                file TEXT
            );
            INSERT INTO item_extra VALUES (
                1,
                'Song',
                1,
                'Downloads/-6176109735475747593.m4a',
                'F03/-6176109735475747593.m4a'
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        var track = Assert.Single(await new IPhoneMusicCatalogParser().ParseAsync(_databasePath, cancellationToken));
        Assert.Equal(
            "/iTunes_Control/Music/F03/-6176109735475747593.m4a",
            track.RemotePath);
    }

    [Fact]
    public async Task ParseAsync_StoreProtectedTrack_IsProtected()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        InitializeProvider();
        await using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE item_extra (
                item_pid INTEGER,
                title TEXT,
                media_kind INTEGER,
                location TEXT
            );
            CREATE TABLE item_store (
                item_pid INTEGER,
                is_protected INTEGER,
                is_subscription INTEGER
            );
            INSERT INTO item_extra VALUES (1, 'Subscription song', 1, 'song.m4a');
            INSERT INTO item_store VALUES (1, 1, 1);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        var track = Assert.Single(await new IPhoneMusicCatalogParser().ParseAsync(_databasePath, cancellationToken));
        Assert.Equal(IPhoneMusicTrackAvailability.Protected, track.Availability);
    }

    [Fact]
    public async Task ParseAsync_UnprotectedPurchasedSubscriptionTrack_IsAvailable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        InitializeProvider();
        await using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE item_extra (
                item_pid INTEGER,
                title TEXT,
                media_kind INTEGER,
                location TEXT
            );
            CREATE TABLE item_store (
                item_pid INTEGER,
                is_protected INTEGER,
                is_subscription INTEGER,
                is_ota_purchased INTEGER
            );
            INSERT INTO item_extra VALUES (1, 'Purchased song', 1, 'song.m4a');
            INSERT INTO item_store VALUES (1, 0, 1, 1);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        var track = Assert.Single(await new IPhoneMusicCatalogParser().ParseAsync(_databasePath, cancellationToken));
        Assert.Equal(IPhoneMusicTrackAvailability.Available, track.Availability);
    }

    [Fact]
    public async Task ParseAsync_UnsupportedSchema_ThrowsInvalidDataException()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        InitializeProvider();
        await using (var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await connection.OpenAsync(cancellationToken);
            var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE item_extra (unknown_column TEXT);";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await Assert.ThrowsAsync<InvalidDataException>(
            () => new IPhoneMusicCatalogParser().ParseAsync(_databasePath, cancellationToken));
    }

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private static void InitializeProvider()
    {
        try
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_winsqlite3());
        }
        catch (InvalidOperationException)
        {
            // The provider is process-global and may already be initialized by another test.
        }
    }
}
