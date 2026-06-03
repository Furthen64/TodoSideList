using TodoSideList.Core.Models;

namespace TodoSideList.Tests;

public sealed class TodoItemTests
{
    [Fact]
    public void SetCompleted_True_SetsCompletionTimestamp()
    {
        var item = TodoItem.Create("Ship scaffolding", 1000);

        item.SetCompleted(true);

        Assert.True(item.IsCompleted);
        Assert.NotNull(item.CompletedAtUtc);
    }

    [Fact]
    public void SetCompleted_False_ClearsCompletionTimestamp()
    {
        var item = TodoItem.Create("Ship scaffolding", 1000);
        item.SetCompleted(true);

        item.SetCompleted(false);

        Assert.False(item.IsCompleted);
        Assert.Null(item.CompletedAtUtc);
    }

    [Fact]
    public void Create_DefaultsPriorityToNormal()
    {
        var item = TodoItem.Create("Ship scaffolding", 1000);

        Assert.Equal("normal", item.Priority);
    }

    [Fact]
    public void TodoSettings_DefaultsIdleAutoHideTimeoutToOneMinute()
    {
        var settings = new TodoSettings();

        Assert.Equal(60, settings.AutoHideAfterInactivitySeconds);
    }

    [Fact]
    public void TodoSettings_DefaultsStartupGuidanceToUnseen()
    {
        var settings = new TodoSettings();

        Assert.False(settings.HasSeenStartupGuidance);
    }
}
