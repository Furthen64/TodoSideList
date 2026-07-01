using TodoSideList.Core.Models;
using TodoSideList.App.ViewModels;

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
    public void Create_SetsCreationTimestamp()
    {
        var before = DateTimeOffset.UtcNow;

        var item = TodoItem.Create("Ship scaffolding", 1000);

        var after = DateTimeOffset.UtcNow;
        Assert.InRange(item.CreatedAtUtc, before, after);
        Assert.Equal(item.CreatedAtUtc, item.UpdatedAtUtc);
    }

    [Fact]
    public void FormatStartedText_ForToday_ShowsHoursAgo()
    {
        var now = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.FromHours(2));
        var created = now.AddHours(-5).ToUniversalTime();

        var text = TodoItemViewModel.FormatStartedText(created, now);

        Assert.Equal("Started: today 5 hours ago", text);
    }

    [Fact]
    public void FormatStartedText_ForOlderItem_ShowsDaysAgo()
    {
        var now = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.FromHours(2));
        var created = now.AddDays(-9).ToUniversalTime();

        var text = TodoItemViewModel.FormatStartedText(created, now);

        Assert.Equal("Started: 9 days ago", text);
    }

    [Fact]
    public void FormatStartedText_ForNewItem_ShowsJustNow()
    {
        var now = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.FromHours(2));

        var text = TodoItemViewModel.FormatStartedText(now.ToUniversalTime(), now);

        Assert.Equal("Started: today just now", text);
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
