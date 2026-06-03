namespace TodoSideList.App.Services;

public sealed class StartupGuidance
{
    public static readonly StartupGuidance None = new(false, string.Empty, string.Empty);

    public StartupGuidance(bool shouldShow, string title, string message)
    {
        ShouldShow = shouldShow;
        Title = title;
        Message = message;
    }

    public bool ShouldShow { get; }

    public string Title { get; }

    public string Message { get; }
}
