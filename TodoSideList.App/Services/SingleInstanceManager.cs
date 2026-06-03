using System.IO.Pipes;
using System.Threading;

namespace TodoSideList.App.Services;

internal sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = "TodoSideList.SingleInstance";
    private const string PipeName = "TodoSideList.SingleInstance";
    private static readonly TimeSpan ReplaceExistingWaitTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReplaceExistingRetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _listenerTask;

    private SingleInstanceManager(Mutex mutex)
    {
        _mutex = mutex;
        _listenerTask = Task.Run(ListenForActivationAsync);
    }

    public event Action? ActivationRequested;

    public static SingleInstanceManager? AcquireOrRedirect(LaunchArguments launchArguments)
    {
        var manager = TryAcquirePrimaryInstance();
        if (manager is not null)
        {
            return manager;
        }

        if (launchArguments.ReplaceExistingInstance)
        {
            return WaitForPrimaryInstance();
        }

        TrySignalExistingInstance();
        return null;
    }

    private static SingleInstanceManager? TryAcquirePrimaryInstance()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (createdNew)
        {
            return new SingleInstanceManager(mutex);
        }

        mutex.Dispose();
        return null;
    }

    private static SingleInstanceManager? WaitForPrimaryInstance()
    {
        var deadline = DateTime.UtcNow + ReplaceExistingWaitTimeout;

        while (DateTime.UtcNow < deadline)
        {
            var manager = TryAcquirePrimaryInstance();
            if (manager is not null)
            {
                return manager;
            }

            Thread.Sleep(ReplaceExistingRetryDelay);
        }

        return null;
    }

    private async Task ListenForActivationAsync()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(_cancellationTokenSource.Token);
                _ = server.ReadByte();

                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    ActivationRequested?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
                // Ignore transient pipe failures and continue listening.
            }
        }
    }

    private static void TrySignalExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(timeout: 300);
            client.WriteByte(1);
            client.Flush();
        }
        catch (IOException)
        {
        }
        catch (TimeoutException)
        {
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        TrySignalExistingInstance();

        try
        {
            _listenerTask.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
        }

        _cancellationTokenSource.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
