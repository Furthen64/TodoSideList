using System.IO.Pipes;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace TodoSideList.App.Services;

internal enum InstanceCommand
{
    Toggle,
    Show,
    Hide
}

internal sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = "TodoSideList.SingleInstance";
    private const string PipeName = "TodoSideList.SingleInstance";
    private const int LinuxListenerBacklog = 8;
    private static readonly TimeSpan ReplaceExistingWaitTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReplaceExistingRetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly Mutex? _mutex;
    private readonly Socket? _linuxListener;
    private readonly string? _linuxSocketPath;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _listenerTask;

    private SingleInstanceManager(Mutex mutex)
    {
        _mutex = mutex;
        _listenerTask = Task.Run(ListenForPipeActivationAsync);
    }

    private SingleInstanceManager(Socket linuxListener, string linuxSocketPath)
    {
        _linuxListener = linuxListener;
        _linuxSocketPath = linuxSocketPath;
        _listenerTask = Task.Run(ListenForLinuxCommandsAsync);
    }

    public event Action<InstanceCommand>? CommandRequested;

    public static SingleInstanceManager? AcquireOrRedirect(LaunchArguments launchArguments)
    {
        return OperatingSystem.IsLinux()
            ? AcquireOrRedirectLinux(launchArguments)
            : AcquireOrRedirectWithPipe(launchArguments);
    }

    private static SingleInstanceManager? AcquireOrRedirectWithPipe(LaunchArguments launchArguments)
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

    private static SingleInstanceManager? AcquireOrRedirectLinux(LaunchArguments launchArguments)
    {
        var manager = TryAcquireLinuxPrimaryInstance();
        if (manager is not null)
        {
            return manager;
        }

        if (launchArguments.ReplaceExistingInstance)
        {
            return WaitForLinuxPrimaryInstance();
        }

        var command = launchArguments.HotkeyCommand switch
        {
            HotkeyCommand.Show => "show",
            HotkeyCommand.Hide => "hide",
            HotkeyCommand.Toggle => "toggle",
            _ => "toggle"
        };

        if (TrySendLinuxCommand(command))
        {
            return null;
        }

        if (launchArguments.HotkeyCommand is not HotkeyCommand.None)
        {
            // Wayland blocks app-registered global hotkeys in many environments, so desktop shortcuts launch
            // command mode and we start normally if no instance is currently running.
            return TryAcquireLinuxPrimaryInstance();
        }

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

    private async Task ListenForPipeActivationAsync()
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
                    CommandRequested?.Invoke(InstanceCommand.Toggle);
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

    private static SingleInstanceManager? TryAcquireLinuxPrimaryInstance()
    {
        var socketPath = GetLinuxSocketPath();
        var socketDirectory = Path.GetDirectoryName(socketPath);
        if (!string.IsNullOrWhiteSpace(socketDirectory))
        {
            Directory.CreateDirectory(socketDirectory);
        }

        if (File.Exists(socketPath) && !CanConnectToLinuxSocket())
        {
            TryDeleteSocketFile(socketPath);
        }

        try
        {
            var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            listener.Bind(new UnixDomainSocketEndPoint(socketPath));
            listener.Listen(backlog: LinuxListenerBacklog);
            return new SingleInstanceManager(listener, socketPath);
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private static SingleInstanceManager? WaitForLinuxPrimaryInstance()
    {
        var deadline = DateTime.UtcNow + ReplaceExistingWaitTimeout;

        while (DateTime.UtcNow < deadline)
        {
            var manager = TryAcquireLinuxPrimaryInstance();
            if (manager is not null)
            {
                return manager;
            }

            Thread.Sleep(ReplaceExistingRetryDelay);
        }

        return null;
    }

    private async Task ListenForLinuxCommandsAsync()
    {
        if (_linuxListener is null)
        {
            return;
        }

        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            Socket? client = null;
            try
            {
                client = await _linuxListener.AcceptAsync(_cancellationTokenSource.Token);
                await using var stream = new NetworkStream(client, ownsSocket: true);
                using var commandTimeout = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
                commandTimeout.CancelAfter(TimeSpan.FromSeconds(2));
                var text = await ReadLinuxCommandAsync(stream, commandTimeout.Token);
                client = null;

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (TryMapLinuxCommand(text, out var command))
                {
                    CommandRequested?.Invoke(command);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
            }
            finally
            {
                client?.Dispose();
            }
        }
    }

    private static bool TryMapLinuxCommand(string? text, out InstanceCommand command)
    {
        switch (text?.Trim().ToLowerInvariant())
        {
            case "show":
                command = InstanceCommand.Show;
                return true;
            case "hide":
                command = InstanceCommand.Hide;
                return true;
            case "toggle":
                command = InstanceCommand.Toggle;
                return true;
            default:
                command = default;
                return false;
        }
    }

    private static bool TrySendLinuxCommand(string command)
    {
        try
        {
            using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            client.Connect(new UnixDomainSocketEndPoint(GetLinuxSocketPath()));
            using var stream = new NetworkStream(client, ownsSocket: false);
            using var writer = new StreamWriter(stream) { AutoFlush = true };
            writer.WriteLine(command);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool CanConnectToLinuxSocket()
    {
        try
        {
            using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            client.Connect(new UnixDomainSocketEndPoint(GetLinuxSocketPath()));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static async Task<string?> ReadLinuxCommandAsync(Stream stream, CancellationToken cancellationToken)
    {
        // Keep command payload intentionally tiny to avoid unbounded memory growth from malformed clients.
        const int maxCommandLength = 64;
        var commandBuilder = new StringBuilder();
        var buffer = new byte[1];

        while (commandBuilder.Length < maxCommandLength)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            var nextChar = (char)buffer[0];
            if (nextChar == '\n')
            {
                break;
            }

            if (nextChar != '\r')
            {
                commandBuilder.Append(nextChar);
            }
        }

        return commandBuilder.Length == 0 ? null : commandBuilder.ToString();
    }

    private static string GetLinuxSocketPath()
    {
        var runtimeDirectory = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        var root = string.IsNullOrWhiteSpace(runtimeDirectory) ? "/tmp" : runtimeDirectory;
        var userScope = Environment.GetEnvironmentVariable("UID");
        if (string.IsNullOrWhiteSpace(userScope))
        {
            userScope = Environment.UserName;
        }

        if (string.IsNullOrWhiteSpace(userScope))
        {
            userScope = Environment.GetEnvironmentVariable("HOME");
        }

        if (string.IsNullOrWhiteSpace(userScope))
        {
            userScope = "unknown-user";
        }

        var userBytes = Encoding.UTF8.GetBytes(userScope);
        var hash = SHA256.HashData(userBytes);
        var userSuffix = Convert.ToHexString(hash.AsSpan(0, 6)).ToLowerInvariant();
        return Path.Combine(root, $"todosidelist-{userSuffix}.sock");
    }

    private static void TryDeleteSocketFile(string socketPath)
    {
        try
        {
            File.Delete(socketPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();

        if (_linuxListener is null)
        {
            TrySignalExistingInstance();
        }
        else
        {
            _linuxListener.Dispose();
        }

        try
        {
            _listenerTask.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
        }

        _cancellationTokenSource.Dispose();

        if (_mutex is not null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }

        if (!string.IsNullOrWhiteSpace(_linuxSocketPath))
        {
            TryDeleteSocketFile(_linuxSocketPath);
        }
    }
}
