namespace TodoSideList.Core.Models;

public sealed class TodoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public string Priority { get; set; } = "normal";

    public string AccentColor { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public static TodoItem Create(string title, int sortOrder)
    {
        var now = DateTimeOffset.UtcNow;
        return new TodoItem
        {
            Title = title,
            SortOrder = sortOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void SetCompleted(bool isCompleted)
    {
        IsCompleted = isCompleted;
        CompletedAtUtc = isCompleted ? DateTimeOffset.UtcNow : null;
        Touch();
    }

    public void Touch()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
