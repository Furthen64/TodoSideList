using Avalonia;
using TodoSideList.App.Services;

namespace TodoSideList.App;

internal static class Program
{
    internal static SingleInstanceManager? InstanceManager { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var launchArguments = LaunchArguments.Parse(args);
        using var instanceManager = SingleInstanceManager.AcquireOrRedirect(launchArguments);
        if (instanceManager is null)
        {
            return;
        }

        InstanceManager = instanceManager;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(launchArguments.ForwardedArgs);
        InstanceManager = null;
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
