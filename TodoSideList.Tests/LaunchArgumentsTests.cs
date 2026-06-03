using TodoSideList.App.Services;

namespace TodoSideList.Tests;

public sealed class LaunchArgumentsTests
{
    [Fact]
    public void Parse_RemovesReplaceExistingFlagFromForwardedArguments()
    {
        var launchArguments = LaunchArguments.Parse(["--replace-existing", "--debug"]);

        Assert.True(launchArguments.ReplaceExistingInstance);
        Assert.Equal(["--debug"], launchArguments.ForwardedArgs);
    }

    [Fact]
    public void BuildRestartArguments_PrependsReplaceExistingFlag()
    {
        var launchArguments = LaunchArguments.Parse(["--debug"]);

        Assert.Equal(["--replace-existing", "--debug"], launchArguments.BuildRestartArguments());
    }
}
