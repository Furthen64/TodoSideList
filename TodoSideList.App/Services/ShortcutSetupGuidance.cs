namespace TodoSideList.App.Services;

internal static class ShortcutSetupGuidance
{
    public static string BuildMessage()
    {
        return BuildMessage(ResolveToggleCommand(AppContext.BaseDirectory));
    }

    internal static string BuildMessage(string toggleCommand)
    {
        return
            $$"""
            Super+T must be registered in GNOME, not inside TodoSideList.

            Open Settings > Keyboard > View and Customize Shortcuts > Custom Shortcuts.

            Create a custom shortcut:
            Name: TodoSideList Toggle
            Command: {{toggleCommand}}
            Shortcut: Super+T

            Then click Hide and press Super+T. If nothing happens, check for an existing GNOME shortcut using Super+T or Win+T and remove that conflict.
            """;
    }

    internal static string ResolveToggleCommand(string baseDirectory)
    {
        var launchScript = FindLaunchScript(baseDirectory);
        if (launchScript is not null)
        {
            return $"{Quote(launchScript)} --hotkey toggle";
        }

        var executableName = OperatingSystem.IsWindows()
            ? "TodoSideList.App.exe"
            : "TodoSideList.App";
        var executablePath = Path.Combine(baseDirectory, executableName);
        if (File.Exists(executablePath))
        {
            return $"{Quote(executablePath)} --hotkey toggle";
        }

        var dllPath = Path.Combine(baseDirectory, "TodoSideList.App.dll");
        if (File.Exists(dllPath))
        {
            return $"dotnet {Quote(dllPath)} --hotkey toggle";
        }

        return "TodoSideList.App --hotkey toggle";
    }

    private static string? FindLaunchScript(string baseDirectory)
    {
        var directory = new DirectoryInfo(baseDirectory);
        while (directory is not null)
        {
            var launchScript = Path.Combine(directory.FullName, "launch.sh");
            if (File.Exists(launchScript))
            {
                return launchScript;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string Quote(string path)
    {
        return path.Contains(' ', StringComparison.Ordinal)
            ? $"\"{path.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : path;
    }
}
