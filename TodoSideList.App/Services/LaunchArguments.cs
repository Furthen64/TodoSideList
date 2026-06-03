namespace TodoSideList.App.Services;

internal enum HotkeyCommand
{
    None,
    Toggle,
    Show,
    Hide
}

internal sealed class LaunchArguments
{
    private const string ReplaceExistingFlag = "--replace-existing";
    private const string HotkeyFlag = "--hotkey";

    private LaunchArguments(bool replaceExistingInstance, HotkeyCommand hotkeyCommand, string[] forwardedArgs)
    {
        ReplaceExistingInstance = replaceExistingInstance;
        HotkeyCommand = hotkeyCommand;
        ForwardedArgs = forwardedArgs;
    }

    public bool ReplaceExistingInstance { get; }

    public HotkeyCommand HotkeyCommand { get; }

    public string[] ForwardedArgs { get; }

    public static LaunchArguments Parse(string[] args)
    {
        var replaceExistingInstance = false;
        var hotkeyCommand = HotkeyCommand.None;
        var forwardedArgs = new List<string>(args.Length);

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (string.Equals(argument, ReplaceExistingFlag, StringComparison.Ordinal))
            {
                replaceExistingInstance = true;
                continue;
            }

            if (string.Equals(argument, HotkeyFlag, StringComparison.Ordinal) &&
                index + 1 < args.Length &&
                TryParseHotkeyCommand(args[index + 1], out var parsedCommand))
            {
                hotkeyCommand = parsedCommand;
                index++;
                continue;
            }

            forwardedArgs.Add(argument);
        }

        return new LaunchArguments(replaceExistingInstance, hotkeyCommand, forwardedArgs.ToArray());
    }

    public string[] BuildRestartArguments()
    {
        return [ReplaceExistingFlag, .. ForwardedArgs];
    }

    private static bool TryParseHotkeyCommand(string value, out HotkeyCommand command)
    {
        switch (value.ToLowerInvariant())
        {
            case "toggle":
                command = HotkeyCommand.Toggle;
                return true;
            case "show":
                command = HotkeyCommand.Show;
                return true;
            case "hide":
                command = HotkeyCommand.Hide;
                return true;
            default:
                command = HotkeyCommand.None;
                return false;
        }
    }
}
