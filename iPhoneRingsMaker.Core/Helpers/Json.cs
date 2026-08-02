using System.Text.Json;

namespace iPhoneRingsMaker.Core.Helpers;

public static class Json
{
    public static JsonSerializerOptions Options
    {
        get;
    } = new()
    {
        AllowDuplicateProperties = false,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        WriteIndented = true,
    };

    public static async Task<T> ToObjectAsync<T>(string value)
    {
        return await Task.Run(() =>
        {
            return JsonSerializer.Deserialize<T>(value, Options)!;
        });
    }

    public static async ValueTask<T> ToObjectAsync<T>(Stream value)
    {
        return (await JsonSerializer.DeserializeAsync<T>(value, Options))!;
    }

    public static async Task<string> StringifyAsync(object? value)
    {
        return await Task.Run(() =>
        {
            return JsonSerializer.Serialize(value, Options);
        });
    }
}
