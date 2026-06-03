using TodoSideList.Core.Models;
using TodoSideList.Core.Services;

namespace TodoSideList.Infrastructure.Persistence;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private readonly IAppPaths _appPaths;

    public JsonSettingsRepository(IAppPaths appPaths)
    {
        _appPaths = appPaths;
    }

    public TodoSettings Load()
    {
        return JsonFileStore.LoadOrDefault(_appPaths.SettingsPath, new TodoSettings());
    }

    public void Save(TodoSettings settings)
    {
        JsonFileStore.Save(_appPaths.SettingsPath, settings);
    }
}
