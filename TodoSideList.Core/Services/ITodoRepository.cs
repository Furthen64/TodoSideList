using TodoSideList.Core.Models;

namespace TodoSideList.Core.Services;

public interface ITodoRepository
{
    IReadOnlyList<TodoItem> Load();

    void Save(IReadOnlyCollection<TodoItem> items);
}
