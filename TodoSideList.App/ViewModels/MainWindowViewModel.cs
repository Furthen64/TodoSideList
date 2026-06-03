using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using TodoSideList.App.Services;
using TodoSideList.Core.Models;
using TodoSideList.Core.Services;
using TodoSideList.Infrastructure.Persistence;

namespace TodoSideList.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ITodoRepository _todoRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IHistoryReportService _historyReportService;
    private readonly IAppPaths _appPaths;
    private readonly IAppLifecycle _appLifecycle;
    private string _newTaskTitle = string.Empty;
    private string _statusMessage = "Ready.";
    private TodoItemViewModel? _selectedItem;
    private string _windowEdge = "right";
    private int _windowWidth = 460;
    private bool _autoHideOnFocusLoss = true;
    private int _autoHideAfterInactivitySeconds = 60;
    private TodoSettings _settings = new();
    private bool _showStartupGuidance;
    private string _startupGuidanceTitle = string.Empty;
    private string _startupGuidanceMessage = string.Empty;

    public MainWindowViewModel(
        ITodoRepository todoRepository,
        ISettingsRepository settingsRepository,
        IHistoryReportService historyReportService,
        IAppPaths appPaths,
        IAppLifecycle appLifecycle,
        IStartupGuidanceProvider startupGuidanceProvider)
    {
        _todoRepository = todoRepository;
        _settingsRepository = settingsRepository;
        _historyReportService = historyReportService;
        _appPaths = appPaths;
        _appLifecycle = appLifecycle;

        Items = new ObservableCollection<TodoItemViewModel>();
        AddTaskCommand = new RelayCommand(AddTask, CanAddTask);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, CanMutateSelected);
        RemoveItemCommand = new RelayCommand(RemoveItem, CanRemoveItem);
        ToggleSelectedCommand = new RelayCommand(ToggleSelected, CanMutateSelected);
        MoveSelectedUpCommand = new RelayCommand(() => MoveSelected(-1), CanMutateSelected);
        MoveSelectedDownCommand = new RelayCommand(() => MoveSelected(1), CanMutateSelected);
        SaveCommand = new RelayCommand(Save);
        GenerateHistoryReportCommand = new RelayCommand(GenerateHistoryReport);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
        CloseStartupGuidanceCommand = new RelayCommand(CloseStartupGuidance, CanCloseStartupGuidance);

        Load();
        ApplyStartupGuidance(startupGuidanceProvider.GetGuidance());
    }

    public ObservableCollection<TodoItemViewModel> Items { get; }

    public ICommand AddTaskCommand { get; }

    public ICommand RemoveSelectedCommand { get; }

    public ICommand RemoveItemCommand { get; }

    public ICommand ToggleSelectedCommand { get; }

    public ICommand MoveSelectedUpCommand { get; }

    public ICommand MoveSelectedDownCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand GenerateHistoryReportCommand { get; }

    public ICommand ResetDefaultsCommand { get; }

    public ICommand CloseStartupGuidanceCommand { get; }

    public string NewTaskTitle
    {
        get => _newTaskTitle;
        set
        {
            if (SetProperty(ref _newTaskTitle, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public TodoItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public int TotalCount => Items.Count;

    public int CompletedCount => Items.Count(item => item.IsCompleted);

    public string WindowEdge => _windowEdge;

    public int WindowWidth => _windowWidth;

    public bool AutoHideOnFocusLoss => _autoHideOnFocusLoss;

    public bool ShowStartupGuidance => _showStartupGuidance;

    public string StartupGuidanceTitle => _startupGuidanceTitle;

    public string StartupGuidanceMessage => _startupGuidanceMessage;

    public int AutoHideAfterInactivitySeconds
    {
        get => _autoHideAfterInactivitySeconds;
        set
        {
            var clampedValue = Math.Clamp(value, 0, 300);
            if (!SetProperty(ref _autoHideAfterInactivitySeconds, clampedValue))
            {
                return;
            }

            _settings.AutoHideAfterInactivitySeconds = clampedValue;
            _settingsRepository.Save(_settings);
            StatusMessage = clampedValue == 0
                ? "Idle auto-hide disabled."
                : $"Idle auto-hide set to {clampedValue} seconds.";
        }
    }

    private void Load()
    {
        var existing = _todoRepository.Load();
        foreach (var item in existing.OrderBy(item => item.SortOrder))
        {
            Items.Add(CreateItemViewModel(item));
        }

        _settings = _settingsRepository.Load();
        _windowEdge = string.IsNullOrWhiteSpace(_settings.WindowEdge) ? "right" : _settings.WindowEdge.Trim().ToLowerInvariant();
        _windowWidth = _settings.WindowWidth switch
        {
            360 => 460,
            > 0 => _settings.WindowWidth,
            _ => 460
        };
        _autoHideOnFocusLoss = _settings.AutoHideOnFocusLoss;
        _autoHideAfterInactivitySeconds = Math.Clamp(_settings.AutoHideAfterInactivitySeconds, 0, 300);
        StatusMessage = $"Loaded {Items.Count} tasks. Edge: {_settings.WindowEdge}.";
        RaiseCountsChanged();
    }

    private void ApplyStartupGuidance(StartupGuidance guidance)
    {
        _showStartupGuidance = guidance.ShouldShow && !_settings.HasSeenStartupGuidance;
        _startupGuidanceTitle = guidance.Title;
        _startupGuidanceMessage = guidance.Message;
        RaisePropertyChanged(nameof(ShowStartupGuidance));
        RaisePropertyChanged(nameof(StartupGuidanceTitle));
        RaisePropertyChanged(nameof(StartupGuidanceMessage));
        RaiseCanExecuteChanged();

        if (_showStartupGuidance)
        {
            _settings.HasSeenStartupGuidance = true;
            _settingsRepository.Save(_settings);
        }
    }

    private bool CanCloseStartupGuidance()
    {
        return _showStartupGuidance;
    }

    private void CloseStartupGuidance()
    {
        if (!_showStartupGuidance)
        {
            return;
        }

        _showStartupGuidance = false;
        RaisePropertyChanged(nameof(ShowStartupGuidance));
        RaiseCanExecuteChanged();
    }

    private bool CanAddTask()
    {
        return !string.IsNullOrWhiteSpace(NewTaskTitle);
    }

    private void AddTask()
    {
        var title = NewTaskTitle.Trim();
        if (title.Length == 0)
        {
            return;
        }

        var todo = TodoItem.Create(title, Items.Count == 0 ? 1000 : Items.Max(item => item.Model.SortOrder) + 1000);
        var itemViewModel = CreateItemViewModel(todo);
        Items.Add(itemViewModel);
        SelectedItem = itemViewModel;
        NewTaskTitle = string.Empty;
        Save();
        StatusMessage = $"Added \"{title}\".";
        RaiseCountsChanged();
    }

    private bool CanMutateSelected()
    {
        return SelectedItem is not null;
    }

    private void RemoveSelected()
    {
        if (SelectedItem is null)
        {
            return;
        }

        RemoveItem(SelectedItem);
    }

    private bool CanRemoveItem(object? parameter)
    {
        return parameter is TodoItemViewModel;
    }

    private TodoItemViewModel CreateItemViewModel(TodoItem todoItem)
    {
        var itemViewModel = new TodoItemViewModel(todoItem);
        itemViewModel.RemoveCommand = new RelayCommand(() => RemoveItem(itemViewModel));
        itemViewModel.PriorityChanged = OnItemPriorityChanged;
        return itemViewModel;
    }

    private void OnItemPriorityChanged(TodoItemViewModel item)
    {
        Save();
        StatusMessage = $"Priority set to {item.PriorityLabel} for \"{item.Title}\".";
    }

    private void RemoveItem(object? parameter)
    {
        if (parameter is not TodoItemViewModel removed)
        {
            return;
        }

        var index = Items.IndexOf(removed);
        if (index < 0)
        {
            return;
        }

        Items.RemoveAt(index);
        if (ReferenceEquals(SelectedItem, removed))
        {
            SelectedItem = index < Items.Count ? Items[index] : Items.LastOrDefault();
        }

        Save();
        StatusMessage = $"Removed \"{removed.Title}\".";
        RaiseCountsChanged();
    }

    private void ToggleSelected()
    {
        if (SelectedItem is null)
        {
            return;
        }

        SelectedItem.IsCompleted = !SelectedItem.IsCompleted;
        Save();
        StatusMessage = SelectedItem.IsCompleted
            ? $"Completed \"{SelectedItem.Title}\"."
            : $"Re-opened \"{SelectedItem.Title}\".";
        RaiseCountsChanged();
    }

    private void MoveSelected(int direction)
    {
        if (SelectedItem is null)
        {
            return;
        }

        var currentIndex = Items.IndexOf(SelectedItem);
        var newIndex = currentIndex + direction;
        if (currentIndex < 0 || newIndex < 0 || newIndex >= Items.Count)
        {
            return;
        }

        MoveItem(SelectedItem, newIndex);
    }

    public bool MoveItem(TodoItemViewModel item, int newIndex)
    {
        var currentIndex = Items.IndexOf(item);
        if (currentIndex < 0 || Items.Count == 0)
        {
            return false;
        }

        var boundedIndex = Math.Clamp(newIndex, 0, Items.Count - 1);
        if (boundedIndex == currentIndex)
        {
            return false;
        }

        Items.Move(currentIndex, boundedIndex);
        SelectedItem = item;
        RecalculateSortOrder();
        Save();
        StatusMessage = $"Moved \"{item.Title}\".";
        return true;
    }

    private void RecalculateSortOrder()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            Items[index].Model.SortOrder = (index + 1) * 1000;
            Items[index].Model.Touch();
        }
    }

    private void Save()
    {
        var models = Items.Select(item => item.Model).ToArray();
        _todoRepository.Save(models);
        RaiseCountsChanged();
        RaiseCanExecuteChanged();
    }

    private void GenerateHistoryReport()
    {
        var completedItems = Items
            .Where(item => item.IsCompleted)
            .Select(item => item.Model)
            .ToArray();

        var reportPath = _historyReportService.Generate(completedItems);
        StatusMessage = $"History report generated: {reportPath}";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = reportPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"History report generated, but auto-open failed: {ex.Message}";
        }
    }

    private void ResetDefaults()
    {
        try
        {
            if (File.Exists(_appPaths.SettingsPath))
            {
                File.Delete(_appPaths.SettingsPath);
            }

            _appLifecycle.Restart();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Reset failed: {ex.Message}";
        }
    }

    private void RaiseCountsChanged()
    {
        RaisePropertyChanged(nameof(TotalCount));
        RaisePropertyChanged(nameof(CompletedCount));
    }

    private void RaiseCanExecuteChanged()
    {
        foreach (var command in EnumerateCommands())
        {
            command.RaiseCanExecuteChanged();
        }
    }

    private IEnumerable<RelayCommand> EnumerateCommands()
    {
        yield return (RelayCommand)AddTaskCommand;
        yield return (RelayCommand)RemoveSelectedCommand;
        yield return (RelayCommand)RemoveItemCommand;
        yield return (RelayCommand)ToggleSelectedCommand;
        yield return (RelayCommand)MoveSelectedUpCommand;
        yield return (RelayCommand)MoveSelectedDownCommand;
        yield return (RelayCommand)SaveCommand;
        yield return (RelayCommand)GenerateHistoryReportCommand;
        yield return (RelayCommand)ResetDefaultsCommand;
        yield return (RelayCommand)CloseStartupGuidanceCommand;
    }
}
