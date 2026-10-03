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
}
