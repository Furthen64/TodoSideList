using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;

namespace TodoSideList.App.Services;

internal sealed class DesktopAppLifecycle : IAppLifecycle
{
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly LaunchArguments _launchArguments;

    public DesktopAppLifecycle(IClassicDesktopStyleApplicationLifetime desktop, LaunchArguments launchArguments)
    {
        _desktop = desktop;
        _launchArguments = launchArguments;
    }

    public void Restart()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("Unable to resolve the current executable path.");
        }

        var arguments = _launchArguments.BuildRestartArguments();
        var startInfo = new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process.Start(startInfo);
        _desktop.Shutdown();
    }
}
