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

    // The book data of the Recipe Book: the two codex counts and the recipe ids with no codex entry.
    [Fact]
    public void DataJson_writes_the_book_data()
    {
        var book = new PageJson.Book(false, 41, 88, new[] { 1203, 1301 });
        string json = PageJson.DataJson(new Dictionary<int, string>(), new Dictionary<int, int>(), new[] { "codexMarks" }, null, book);

        Assert.Equal("{\"tips\":{},\"tiers\":{},\"features\":[\"codexMarks\"],\"book\":{\"locked\":false,\"unlocked\":41,\"total\":88,\"ids\":[1203,1301]}}", json);
    }

    // The book data keeps the other fields.
    [Fact]
    public void DataJson_with_the_book_keeps_the_other_fields()
    {
        var book = new PageJson.Book(false, 0, 3, new int[0]);
        string json = PageJson.DataJson(FixtureTips(), FixtureTiers(), null, new[] { "Whole dish", "Per portion" }, book);

        var node = JsonNode.Parse(json);
        Assert.Equal(2, node["tips"].AsObject().Count);
        Assert.Equal(2, node["tiers"].AsObject().Count);
        Assert.Equal(2, node["portion"].AsArray().Count);
        Assert.Empty(node["book"]["ids"].AsArray());
    }

    // No book data: no book field.
    [Fact]
    public void DataJson_has_no_book_without_book_data()
    {
        string json = PageJson.DataJson(new Dictionary<int, string>(), new Dictionary<int, int>(), null, null, null);

        Assert.DoesNotContain("book", json);
    }

    // A locked codex: the lock alone, no counts and no ids.
    [Fact]
    public void DataJson_writes_a_locked_book_with_no_ids()
    {
        var book = new PageJson.Book(true, 5, 88, new[] { 1203 });
        string json = PageJson.DataJson(new Dictionary<int, string>(), new Dictionary<int, int>(), null, null, book);

        Assert.Equal("{\"tips\":{},\"tiers\":{},\"book\":{\"locked\":true}}", json);
    }

    // The words of the Codex filter go with the book data.
    [Fact]
    public void DataJson_writes_the_words_of_the_codex_filter()
    {
        var words = new PageJson.BookWords("Codex", "Dish Codex {0} / {1}", "{0} \"missing\"", "Only", "All", "None");
        var book = new PageJson.Book(false, 1, 2, new[] { 7 }, words);
        string json = PageJson.DataJson(new Dictionary<int, string>(), new Dictionary<int, int>(), null, null, book);

        var node = JsonNode.Parse(json)["book"]["words"];
        Assert.Equal("Codex", (string)node["codex"]);
        Assert.Equal("Dish Codex {0} / {1}", (string)node["title"]);
        Assert.Equal("{0} \"missing\"", (string)node["missing"]);
        Assert.Equal("Only", (string)node["showOnly"]);
        Assert.Equal("All", (string)node["showAll"]);
        Assert.Equal("None", (string)node["empty"]);
    }

    // The words of the Codex filter in English, and in Chinese with the game's text of Codex.
    [Fact]
    public void BookWords_come_from_the_mod_texts()
    {
        var en = PageJson.BookWords.From(TestTexts.EnglishSet());
        Assert.Equal("Codex", en.Codex);
        Assert.Equal("Dish Codex {0} / {1}", en.Title);
        Assert.Equal("{0} of the missing dishes are in your Recipe Book.", en.Missing);
        Assert.Equal("Click: show only these recipes.", en.ShowOnly);
        Assert.Equal("Click: show all recipes.", en.ShowAll);
        Assert.Equal("No recipe in your Recipe Book makes a new codex dish.", en.Empty);

        var texts = TestTexts.ChineseGameTexts();
        texts["SR_Web_OutSetting_Codex"] = "图鉴";
        var zh = PageJson.BookWords.From(TestTexts.Load().For(0, TestTexts.Game(texts)));
        Assert.Equal("图鉴", zh.Codex);
        Assert.Equal("菜肴图鉴 {0} / {1}", zh.Title);
        Assert.Equal("菜谱书中有 {0} 道未收录的菜肴。", zh.Missing);
        Assert.Equal("点击：只显示这些配方。", zh.ShowOnly);
        Assert.Equal("点击：显示全部配方。", zh.ShowAll);
        Assert.Equal("菜谱书中没有可收录新菜肴的配方。", zh.Empty);
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

