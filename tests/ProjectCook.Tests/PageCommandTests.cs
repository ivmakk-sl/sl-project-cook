using ProjectCook;
using Xunit;

public class PageCommandTests
{
    private const string Json = "{\"tips\":{},\"tiers\":{}}";
    private const string Script = "window.__projectCook = window.__projectCook || (function () { return {}; })();";

    [Fact]
    public void The_apply_command_runs_one_pass_or_gives_no_script()
    {
        Assert.Equal("window.__projectCook?window.__projectCook.apply():'no script'", PageJson.ApplyCommand);
        Assert.Equal("no script", PageJson.NoScript);
    }

    [Fact]
    public void The_setData_command_sends_only_the_data_when_the_page_has_the_script()
    {
        Assert.Equal("window.__projectCook?window.__projectCook.setData(" + Json + "):'no script'", PageJson.SetDataCommand(Json));
    }

    [Fact]
    public void The_full_command_is_the_script_then_the_data()
    {
        string js = PageJson.SetDataWithScriptCommand(Script, Json);

        Assert.StartsWith(Script, js);
        Assert.EndsWith(";window.__projectCook.setData(" + Json + ");", js);
    }

    [Fact]
    public void No_command_uses_the_root_globals_of_1_1_0()
    {
        Assert.DoesNotContain("__cooking", PageJson.ApplyCommand);
        Assert.DoesNotContain("__cooking", PageJson.SetDataCommand(Json));
        Assert.DoesNotContain("__cooking", PageJson.SetDataWithScriptCommand(Script, Json));
    }
}
