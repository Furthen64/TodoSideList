using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using TodoSideList.App.ViewModels;

namespace TodoSideList.App.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _inactivityTimer = new();
    private bool _hasRunInitialSlideIn;
    private Point? _dragStartPoint;
    private TodoItemViewModel? _pendingDragItem;
    private TodoItemViewModel? _activeDragItem;
    private bool _isDragInProgress;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Opened += OnWindowOpened;
        Closing += OnWindowClosing;
        Deactivated += OnWindowDeactivated;
        DataContextChanged += OnDataContextChanged;

        AddHandler(InputElement.PointerMovedEvent, OnPointerActivity, RoutingStrategies.Tunnel);
        AddHandler(InputElement.PointerPressedEvent, OnPointerActivity, RoutingStrategies.Tunnel);
        AddHandler(InputElement.KeyDownEvent, OnKeyboardActivity, RoutingStrategies.Tunnel);
        AddHandler(InputElement.TextInputEvent, OnTextInputActivity, RoutingStrategies.Tunnel);

        _inactivityTimer.Tick += OnInactivityTimerTick;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_hasRunInitialSlideIn || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        _hasRunInitialSlideIn = true;

        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var workArea = screen.WorkingArea;
        var width = Math.Max(viewModel.WindowWidth, 320);
        var height = workArea.Height;
        var top = workArea.Y;
        var finalLeft = ResolveLeft(viewModel.WindowEdge, workArea, width);
        var hiddenLeft = ResolveHiddenLeft(viewModel.WindowEdge, workArea, width);

        Width = width;
        Height = height;
        Position = new PixelPoint(hiddenLeft, top);

        await Dispatcher.UIThread.InvokeAsync(InvalidateMeasure, DispatcherPriority.Render);
        await Task.Delay(30);

        const int steps = 12;
        for (var step = 1; step <= steps; step++)
        {
            var progress = step / (double)steps;
            var currentLeft = hiddenLeft + (int)Math.Round((finalLeft - hiddenLeft) * progress);
            Position = new PixelPoint(currentLeft, top);
            await Task.Delay(12);
        }

        Position = new PixelPoint(finalLeft, top);
    }

    private static int ResolveLeft(string edge, PixelRect workArea, int width)
    {
        return edge == "left"
            ? workArea.X
            : workArea.Right - width;
    }

    private static int ResolveHiddenLeft(string edge, PixelRect workArea, int width)
    {
        return edge == "left"
            ? workArea.X - width
            : workArea.Right;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        ResetInactivityTimer();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel currentViewModel)
        {
            currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _inactivityTimer.Stop();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (sender is not MainWindow window)
        {
            return;
        }

        if (window.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            ResetInactivityTimer();
        }
    }

    private void OnPointerActivity(object? sender, PointerEventArgs e)
    {
        ResetInactivityTimer();
    }

    private void OnKeyboardActivity(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.T && e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            HideWindow();
            e.Handled = true;
            return;
        }

        ResetInactivityTimer();
    }

    private void OnTextInputActivity(object? sender, TextInputEventArgs e)
    {
        ResetInactivityTimer();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.AutoHideAfterInactivitySeconds))
        {
            ResetInactivityTimer();
        }
    }

    private void ResetInactivityTimer()
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            viewModel.AutoHideAfterInactivitySeconds <= 0)
        {
            _inactivityTimer.Stop();
            return;
        }

        _inactivityTimer.Stop();
        _inactivityTimer.Interval = TimeSpan.FromSeconds(viewModel.AutoHideAfterInactivitySeconds);
        _inactivityTimer.Start();
    }

    private void OnInactivityTimerTick(object? sender, EventArgs e)
    {
        _inactivityTimer.Stop();

        if (DataContext is not MainWindowViewModel viewModel ||
            viewModel.AutoHideAfterInactivitySeconds <= 0 ||
            !IsVisible)
        {
            return;
        }

        HideWindow();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            !viewModel.AutoHideOnFocusLoss ||
            !IsVisible)
        {
            return;
        }

        HideWindow();
    }

    private void HideWindow()
    {
        if (!IsVisible)
        {
            return;
        }

        Hide();
    }

    public void ToggleVisibility()
    {
        if (!IsVisible || WindowState == WindowState.Minimized)
        {
            ShowWindow();
            return;
        }

        HideWindow();
    }

    public void HideWindowFromCommand()
    {
        HideWindow();
    }

    public void ShowWindow()
    {
        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        Activate();
        ResetInactivityTimer();
    }

    private void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not TodoItemViewModel item ||
            !e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _pendingDragItem = item;
        _dragStartPoint = e.GetPosition(this);
    }

    private async void DragHandle_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragInProgress ||
            _pendingDragItem is null ||
            _dragStartPoint is null ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var currentPoint = e.GetPosition(this);
        if (Math.Abs(currentPoint.X - _dragStartPoint.Value.X) < 4 &&
            Math.Abs(currentPoint.Y - _dragStartPoint.Value.Y) < 4)
        {
            return;
        }

        _isDragInProgress = true;
        _activeDragItem = _pendingDragItem;

        try
        {
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(_activeDragItem.Id));
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        }
        finally
        {
            _isDragInProgress = false;
            _activeDragItem = null;
            _pendingDragItem = null;
            _dragStartPoint = null;
        }
    }

    private void DragHandle_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _pendingDragItem = null;
        _dragStartPoint = null;
    }

    private void TodoItem_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = ResolveDraggedItem() is null ? DragDropEffects.None : DragDropEffects.Move;
        e.Handled = true;
    }

    private void TodoItem_Drop(object? sender, DragEventArgs e)
    {
        if (sender is not Control control ||
            control.DataContext is not TodoItemViewModel targetItem ||
            DataContext is not MainWindowViewModel viewModel ||
            ResolveDraggedItem() is not { } draggedItem)
        {
            return;
        }

        var currentIndex = viewModel.Items.IndexOf(draggedItem);
        var targetIndex = viewModel.Items.IndexOf(targetItem);
        if (currentIndex < 0 || targetIndex < 0 || currentIndex == targetIndex)
        {
            e.Handled = true;
            return;
        }

        var insertAfter = e.GetPosition(control).Y >= control.Bounds.Height / 2;
        var destinationIndex = ResolveDestinationIndex(currentIndex, targetIndex, insertAfter);
        viewModel.MoveItem(draggedItem, destinationIndex);
        e.Handled = true;
    }

    private void TodoList_DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = ResolveDraggedItem() is null ? DragDropEffects.None : DragDropEffects.Move;
        e.Handled = true;
    }

    private void TodoList_Drop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel ||
            ResolveDraggedItem() is not { } draggedItem)
        {
            return;
        }

        viewModel.MoveItem(draggedItem, viewModel.Items.Count - 1);
        e.Handled = true;
    }

    private TodoItemViewModel? ResolveDraggedItem()
    {
        if (_activeDragItem is not null)
        {
            return _activeDragItem;
        }

        if (DataContext is not MainWindowViewModel viewModel)
        {
            return null;
        }

        return _pendingDragItem is not null
            ? viewModel.Items.FirstOrDefault(item => item.Id == _pendingDragItem.Id)
            : null;
    }

    private static int ResolveDestinationIndex(int currentIndex, int targetIndex, bool insertAfter)
    {
        if (insertAfter)
        {
            return currentIndex < targetIndex ? targetIndex : targetIndex + 1;
        }

        return currentIndex < targetIndex ? targetIndex - 1 : targetIndex;
    }
}
