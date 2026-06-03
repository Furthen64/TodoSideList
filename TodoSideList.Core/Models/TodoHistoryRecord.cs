namespace TodoSideList.Core.Models;

public sealed class TodoHistoryRecord
{
    public string TaskId { get; set; } = string.Empty;

    public string TitleSnapshot { get; set; } = string.Empty;

    public DateTimeOffset CompletedAtUtc { get; set; }
}
