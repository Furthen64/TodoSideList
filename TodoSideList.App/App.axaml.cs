using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using TodoSideList.App.Services;
using TodoSideList.App.ViewModels;
using TodoSideList.App.Views;
using TodoSideList.Core.Services;
using TodoSideList.Infrastructure.Persistence;

namespace TodoSideList.App;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            IAppPaths appPaths = new DefaultAppPaths();
            ITodoRepository todoRepository = new JsonTodoRepository(appPaths);
            ISettingsRepository settingsRepository = new JsonSettingsRepository(appPaths);
            IHistoryReportService historyReportService = new StaticHistoryReportService(appPaths);
            var launchArguments = LaunchArguments.Parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
            IAppLifecycle appLifecycle = new DesktopAppLifecycle(desktop, launchArguments);
            IStartupGuidanceProvider startupGuidanceProvider = OperatingSystem.IsWindows()
                ? new WindowsStartupGuidanceProvider(appPaths)
                : new LinuxStartupGuidanceProvider(appPaths);

            var mainWindowViewModel = new MainWindowViewModel(
                todoRepository,
                settingsRepository,
                historyReportService,
                appPaths,
                appLifecycle,
                startupGuidanceProvider);
            var mainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
            _mainWindow = mainWindow;
            desktop.MainWindow = mainWindow;

            if (Program.InstanceManager is not null)
            {
                Program.InstanceManager.CommandRequested += command =>
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            switch (command)
                            {
                                case InstanceCommand.Show:
                                    mainWindow.ShowWindow();
                                    break;
                                case InstanceCommand.Hide:
                                    mainWindow.HideWindow();
                                    break;
                                default:
                                    mainWindow.ToggleVisibility();
                                    break;
                            }
                        },
                        DispatcherPriority.Input);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        ToggleMainWindowVisibility();
    }

    private void OnTrayQuitClicked(object? sender, EventArgs e)
    {
        if (_mainWindow is not null)
        {
            _mainWindow.ForceClose();
        }
        else if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void ToggleMainWindowVisibility()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.ToggleVisibility();
    }
}
