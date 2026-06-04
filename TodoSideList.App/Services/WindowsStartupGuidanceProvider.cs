using TodoSideList.Infrastructure.Persistence;

namespace TodoSideList.App.Services;

public sealed class WindowsStartupGuidanceProvider : IStartupGuidanceProvider
{
    private readonly IAppPaths _appPaths;

    public WindowsStartupGuidanceProvider(IAppPaths appPaths)
    {
        _appPaths = appPaths;
    }

    public StartupGuidance GetGuidance()
    {
        if (File.Exists(_appPaths.SettingsPath))
        {
            return StartupGuidance.None;
        }

        return new StartupGuidance(
            shouldShow: true,
            title: "Welcome to TodoSideList",
            message:
            """
            No settings file was found yet, so TodoSideList started with defaults.

            Default behavior:
            - sidebar edge: right
            - idle auto-hide: 60 seconds

            To show or hide the window, click the TodoSideList icon in the system tray.
            Closing the window minimizes it to the system tray. To quit, use the tray icon menu.
            """);
    }
}
