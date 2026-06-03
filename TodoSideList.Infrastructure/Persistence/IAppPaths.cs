namespace TodoSideList.Infrastructure.Persistence;

public interface IAppPaths
{
    string ConfigDirectory { get; }

    string TodoListPath { get; }

    string SettingsPath { get; }

    string HistoryDirectory { get; }
}
