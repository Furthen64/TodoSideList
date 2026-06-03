using TodoSideList.Core.Models;

namespace TodoSideList.Core.Services;

public interface IHistoryReportService
{
    string Generate(IReadOnlyCollection<TodoItem> completedItems);
}
