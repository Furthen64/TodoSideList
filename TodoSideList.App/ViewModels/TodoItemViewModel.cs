using TodoSideList.Core.Models;
using Avalonia.Media;
using System.Windows.Input;

namespace TodoSideList.App.ViewModels;

public sealed class TodoItemViewModel : ViewModelBase
{
    private static readonly string[] PriorityCycle = ["pr3", "pr2", "pr1", "pr0"];
    private static readonly Dictionary<string, string> PriorityAccentColors = new()
    {
        ["pr3"] = "#6B8F71",
        ["pr2"] = "#6C8EA3",
        ["pr1"] = "#7D7F72",
        ["pr0"] = "#C9863A"
    };
    private readonly TodoItem _model;
    private string _title;
    private bool _isCompleted;

    public TodoItemViewModel(TodoItem model)
    {
        _model = model;
        _title = model.Title;
        _isCompleted = model.IsCompleted;
        RemoveCommand = new RelayCommand(() => { });
        CyclePriorityCommand = new RelayCommand(CyclePriority);
    }

    public TodoItem Model => _model;

    public string Id => _model.Id;

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
            {
                _model.Title = value;
                _model.Touch();
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (SetProperty(ref _isCompleted, value))
            {
                _model.SetCompleted(value);
            }
        }
    }

    public string PriorityLabel => NormalizePriority(_model.Priority);

    public DateTimeOffset CreatedAtUtc => _model.CreatedAtUtc;

    public string AccentColor => PriorityAccentColors[PriorityLabel];

    public IBrush AccentBrush => Brush.Parse(AccentColor);

    public ICommand RemoveCommand { get; set; }

    public ICommand CyclePriorityCommand { get; }

    public Action<TodoItemViewModel>? PriorityChanged { get; set; }

    private void CyclePriority()
    {
        var normalized = NormalizePriority(_model.Priority);
        var currentIndex = Array.IndexOf(PriorityCycle, normalized);
        var nextIndex = currentIndex >= 0 && currentIndex < PriorityCycle.Length - 1
            ? currentIndex + 1
            : 0;

        _model.Priority = PriorityCycle[nextIndex];
        _model.Touch();
        RaisePropertyChanged(nameof(PriorityLabel));
        RaisePropertyChanged(nameof(AccentColor));
        RaisePropertyChanged(nameof(AccentBrush));
        PriorityChanged?.Invoke(this);
    }

    private static string NormalizePriority(string? priority)
    {
        return priority?.Trim().ToLowerInvariant() switch
        {
            "pr3" => "pr3",
            "pr2" => "pr2",
            "pr1" => "pr1",
            "pr0" => "pr0",
            "normal" => "pr3",
            _ => "pr3"
        };
    }
}
