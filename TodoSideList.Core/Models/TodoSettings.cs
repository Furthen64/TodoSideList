namespace TodoSideList.Core.Models;

public sealed class TodoSettings
{
    public string WindowEdge { get; set; } = "right";

    public int WindowWidth { get; set; } = 460;

    public bool AutoHideOnFocusLoss { get; set; } = true;

    public int AutoHideAfterInactivitySeconds { get; set; } = 60;

    public bool LaunchHidden { get; set; } = true;

    public string GlobalShortcut { get; set; } = "Super+T";

    public string Theme { get; set; } = "system";

    public bool ArchiveCompletedOnExit { get; set; }

    public bool HasSeenStartupGuidance { get; set; }
}
