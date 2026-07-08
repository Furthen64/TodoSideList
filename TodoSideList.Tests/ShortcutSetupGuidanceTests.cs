using TodoSideList.App.Services;

namespace TodoSideList.Tests;

public sealed class ShortcutSetupGuidanceTests
{
    [Fact]
    public void ResolveToggleCommand_WhenLaunchScriptExistsInAncestor_UsesLaunchScript()
    {
        var root = Path.Combine(Path.GetTempPath(), $"todosidelist-{Guid.NewGuid():N}");
        var baseDirectory = Path.Combine(root, "TodoSideList.App", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(baseDirectory);
        File.WriteAllText(Path.Combine(root, "launch.sh"), string.Empty);

        try
        {
            var command = ShortcutSetupGuidance.ResolveToggleCommand(baseDirectory);

            Assert.Equal(Path.Combine(root, "launch.sh") + " --hotkey toggle", command);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void BuildMessage_IncludesToggleCommandAndGnomeShortcut()
    {
        var message = ShortcutSetupGuidance.BuildMessage("/app/TodoSideList.App --hotkey toggle");

        Assert.Contains("/app/TodoSideList.App --hotkey toggle", message);
        Assert.Contains("Shortcut: Super+T", message);
        Assert.Contains("GNOME", message);
    }
}
