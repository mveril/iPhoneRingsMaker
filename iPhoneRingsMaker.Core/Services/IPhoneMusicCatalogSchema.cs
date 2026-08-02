using Microsoft.Data.Sqlite;

namespace iPhoneRingsMaker.Core.Services;

internal static class IPhoneMusicCatalogSchema
{
    public static async Task<HashSet<string>> ReadColumnsAsync(
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

    public static async Task<HashSet<string>> ReadTablesAsync(
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

    public static string? PickColumn(HashSet<string> columns, params string[] candidates)
        => candidates.FirstOrDefault(candidate => columns.Contains(candidate));

    public static string SelectOrNull(string? column) => column is null ? "NULL" : $"\"{column}\"";

    public static string? SelectOrNull(string? column, string alias)
        => column is null ? null : $"{alias}.\"{column}\"";
}
