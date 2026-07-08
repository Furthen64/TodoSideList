using TodoSideList.Infrastructure.Persistence;

namespace TodoSideList.App.Services;

public sealed class LinuxStartupGuidanceProvider : IStartupGuidanceProvider
{
    private readonly IAppPaths _appPaths;

    public LinuxStartupGuidanceProvider(IAppPaths appPaths)
    {
        _appPaths = appPaths;
    }

    public StartupGuidance GetGuidance()
    {
        if (File.Exists(_appPaths.SettingsPath))
        {
            return StartupGuidance.None;
        }

        if (IsUbuntu26Gnome())
        {
            return new StartupGuidance(
                shouldShow: true,
                title: "Set up Super+T in GNOME",
                message: ShortcutSetupGuidance.BuildMessage());
        }

        return new StartupGuidance(
            shouldShow: true,
            title: "Welcome to TodoSideList",
            message:
            """
            No settings file was found yet, so TodoSideList started with defaults.

            Default behavior:
            - sidebar edge: right
            - shortcut: Super+T
            - idle auto-hide: 60 seconds

            If Super+T does not show the app after hiding it, use the ? button next to the shortcut label for desktop shortcut setup.
            """);
    }

    private static bool IsUbuntu26Gnome()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        var osReleasePath = "/etc/os-release";
        if (!File.Exists(osReleasePath))
        {
            return false;
        }

        var values = File.ReadLines(osReleasePath)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => parts[0],
                parts => parts[1].Trim().Trim('"'),
                StringComparer.OrdinalIgnoreCase);

        if (!values.TryGetValue("ID", out var id) ||
            !string.Equals(id, "ubuntu", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!values.TryGetValue("VERSION_ID", out var versionId) ||
            !versionId.StartsWith("26", StringComparison.Ordinal))
        {
            return false;
        }

        var currentDesktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
        var session = Environment.GetEnvironmentVariable("DESKTOP_SESSION");

        return ContainsGnome(currentDesktop) || ContainsGnome(session);
    }

    private static bool ContainsGnome(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains("gnome", StringComparison.OrdinalIgnoreCase);
    }
}
