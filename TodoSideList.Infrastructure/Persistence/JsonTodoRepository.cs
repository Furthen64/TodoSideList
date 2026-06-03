using TodoSideList.Core.Models;
using TodoSideList.Core.Services;

namespace TodoSideList.Infrastructure.Persistence;

public sealed class JsonTodoRepository : ITodoRepository
{
    private readonly IAppPaths _appPaths;

    public JsonTodoRepository(IAppPaths appPaths)
    {
        _appPaths = appPaths;
    }

    public IReadOnlyList<TodoItem> Load()
    {
        return JsonFileStore.LoadOrDefault(_appPaths.TodoListPath, new List<TodoItem>());
    }

    public void Save(IReadOnlyCollection<TodoItem> items)
    {
        JsonFileStore.Save(_appPaths.TodoListPath, items);
    }
}
