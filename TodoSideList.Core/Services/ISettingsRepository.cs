using TodoSideList.Core.Models;

namespace TodoSideList.Core.Services;

public interface ISettingsRepository
{
    TodoSettings Load();

    void Save(TodoSettings settings);
}
