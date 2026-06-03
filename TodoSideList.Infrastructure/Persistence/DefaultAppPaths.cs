namespace TodoSideList.Infrastructure.Persistence;

public sealed class DefaultAppPaths : IAppPaths
{
    private readonly string _configDirectory;

    public DefaultAppPaths()
    {
        var root = OperatingSystem.IsWindows()
            ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        _configDirectory = Path.Combine(root, "TodoSideList");
    }

    public string ConfigDirectory => _configDirectory;

    public string TodoListPath => Path.Combine(ConfigDirectory, "mylist.json");

    public string SettingsPath => Path.Combine(ConfigDirectory, "settings.json");

    public string HistoryDirectory => Path.Combine(ConfigDirectory, "history");
}
