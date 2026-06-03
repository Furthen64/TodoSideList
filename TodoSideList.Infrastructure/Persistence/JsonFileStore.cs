using System.Text.Json;

namespace TodoSideList.Infrastructure.Persistence;

internal static class JsonFileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static T LoadOrDefault<T>(string path, T fallback) where T : class
    {
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, SerializerOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    public static void Save<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(value, SerializerOptions);
        var tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}
