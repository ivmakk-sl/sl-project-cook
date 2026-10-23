using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using ProjectCook;
using Xunit;

public class PageJsonTests
{
    // The data of tests/fixtures/data.json: a High and a Low ingredient, and a tip text with a quote, a
    // backslash, and a line break.
    private static Dictionary<int, string> FixtureTips() => new Dictionary<int, string>
    {
        [555] = "T1|Tier|High-tier\nTrade value: 12",
        [556] = "T3|Tier|Low \"raw\" \\ tier\nTrade value: 2",
    };

    private static Dictionary<int, int> FixtureTiers() => new Dictionary<int, int> { [555] = 1, [556] = 3 };

    [Fact]
    public void DataJson_matches_the_fixture()
    {
        string json = PageJson.DataJson(FixtureTips(), FixtureTiers());

        var expected = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "data.json")));
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(json)), json);
    }

    // The page marks only the High and the Low tier.
    [Fact]
    public void DataJson_leaves_out_the_tiers_that_the_page_does_not_mark()
    {
        string json = PageJson.DataJson(new Dictionary<int, string>(), new Dictionary<int, int> { [1] = 1, [2] = 2, [3] = 3, [4] = 0 });

        Assert.Equal("{\"tips\":{},\"tiers\":{\"1\":1,\"3\":3}}", json);
    }

    // The labels of the portion switch.
    [Fact]
    public void DataJson_writes_the_portion_switch_labels()
    {
        string json = PageJson.DataJson(new Dictionary<int, string>(), new Dictionary<int, int>(), new[] { "portionSwitch" }, new[] { "Whole dish", "Per \"portion\"" });

        Assert.Equal("{\"tips\":{},\"tiers\":{},\"features\":[\"portionSwitch\"],\"portion\":[\"Whole dish\",\"Per \\\"portion\\\"\"]}", json);
    }

    // The page features that are on in the config.
    [Fact]
    public void DataJson_lists_the_page_features()
    {
        string json = PageJson.DataJson(new Dictionary<int, string>(), new Dictionary<int, int>(), new[] { "separatePieces", "dimUncookable" });

        Assert.Equal("{\"tips\":{},\"tiers\":{},\"features\":[\"separatePieces\",\"dimUncookable\"]}", json);
    }

    // The icon tab list of tests/fixtures/icon-tabs.json, which the page test reads too: the bag with no icon, a locked
    // fridge with its icon and a lock text with quotes, and a storage with no icon and its short text.
    [Fact]
    public void IconTabsJson_matches_the_fixture()
    {
        var entries = new[]
        {
            new IconTabsLogic.Entry(1, "Character Inventory", true, null).With(null, ""),
            new IconTabsLogic.Entry(2, "Fridge", false, "Unlocks at \"Cooking\" Lv.2").With("../../Res/Furniture/UI_Item_Icon_bingxiang.png", "Fridge"),
            new IconTabsLogic.Entry(3, "Large Storage Rack", false, null).With(null, "Large"),
        };
        string json = PageJson.IconTabsJson("BrewPanel", 7, entries);

        var expected = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "icon-tabs.json")));
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(json)), json);
    }

    [Fact]
    public void SetIconTabsCommand_gives_no_script_without_the_page_script()
    {
        Assert.Equal("window.__projectCook?window.__projectCook.setIconTabs({}):'no script'", PageJson.SetIconTabsCommand("{}"));
    }

    // A pass result names the pages whose tab row did not match the icon tab list (install.ts iconTabsResult).
    [Theory]
    [InlineData("installed; brew: applied; icontabs: mismatch BrewPanel", new[] { "BrewPanel" })]
    [InlineData("ratcage: applied; icontabs: mismatch RatCage,Compressor", new[] { "RatCage", "Compressor" })]
    [InlineData("icontabs: mismatch Cooking; errors: x", new[] { "Cooking" })]
    [InlineData("installed; brew: applied", new string[0])]
    [InlineData(null, new string[0])]
    public void IconTabMismatches_reads_the_pages_of_a_result(string result, string[] pages)
    {
        Assert.Equal(pages, PageJson.IconTabMismatches(result));
    }
}

