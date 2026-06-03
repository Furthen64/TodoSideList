namespace TodoSideList.App.Services;

internal sealed class LaunchArguments
{
    private const string ReplaceExistingFlag = "--replace-existing";

    private LaunchArguments(bool replaceExistingInstance, string[] forwardedArgs)
    {
        ReplaceExistingInstance = replaceExistingInstance;
        ForwardedArgs = forwardedArgs;
    }

    public bool ReplaceExistingInstance { get; }

    public string[] ForwardedArgs { get; }

    public static LaunchArguments Parse(string[] args)
    {
        var replaceExistingInstance = false;
        var forwardedArgs = new List<string>(args.Length);

        foreach (var argument in args)
        {
            if (string.Equals(argument, ReplaceExistingFlag, StringComparison.Ordinal))
            {
                replaceExistingInstance = true;
                continue;
            }

            forwardedArgs.Add(argument);
        }

        return new LaunchArguments(replaceExistingInstance, forwardedArgs.ToArray());
    }

    public string[] BuildRestartArguments()
    {
        return [ReplaceExistingFlag, .. ForwardedArgs];
    }
}
