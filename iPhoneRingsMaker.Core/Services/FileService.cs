using System.Text.Json;
using iPhoneRingsMaker.Core.Contracts.Services;
using iPhoneRingsMaker.Core.Helpers;

namespace iPhoneRingsMaker.Core.Services;

public class FileService : IFileService
{
    public T Read<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            using var json = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(json, Json.Options)!;
        }

        return default;
    }

    public void Save<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        using var stream = File.Create(Path.Combine(folderPath, fileName));
        JsonSerializer.Serialize(stream, content, Json.Options);
    }

    public void Delete(string folderPath, string fileName)
    {
        if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
        {
            File.Delete(Path.Combine(folderPath, fileName));
        }
    }

    public async ValueTask<T> ReadAsync<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (File.Exists(path))
        {
            await using var json = File.OpenRead(path);
            return (await JsonSerializer.DeserializeAsync<T>(json, Json.Options))!;
        }

        return default;
    }
    public async Task SaveAsync<T>(string folderPath, string fileName, T content)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        await using var stream = File.Create(Path.Combine(folderPath, fileName));
        await JsonSerializer.SerializeAsync(stream, content, Json.Options);
    }
}
