using System.Text.Json;
using ProjectCook;
using Xunit;

public class PredictionJsonTests
{
    private const string Known = "[{\"RecipeId\":7008,\"Name\":\"Stir-Fried Aged Mushrooms with Meat\",\"Tier\":3,\"Level\":2,\"IsExact\":false,\"Icon\":\"../../Res/Food/UI_Item_Icon_Food_laojunchaorou.png\"}]";
    private const string Unknown = "[{\"RecipeId\":4062,\"Name\":\"Unknown Dish\",\"Tier\":0,\"Level\":1,\"IsExact\":true,\"Icon\":\"\"}]";
    private const string Two = "[{\"RecipeId\":7008,\"Name\":\"A \\\"quoted\\\" dish\",\"Tier\":3,\"Level\":2,\"IsExact\":false,\"Icon\":\"a.png\"},{\"RecipeId\":5008,\"Name\":\"B\",\"Tier\":2,\"Level\":2,\"IsExact\":false,\"Icon\":\"b.png\"}]";

    [Fact]
    public void ReadEntries_KnownRecipe()
    {
        var e = Assert.Single(PreviewLogic.ReadEntries(Known));
        Assert.Equal(7008, e.RecipeId);
        Assert.Equal(3, e.Tier);
        Assert.Equal(2, e.Level);
        Assert.False(e.IsExact);
    }

    [Fact]
    public void ReadEntries_UnknownDish()
    {
        var e = Assert.Single(PreviewLogic.ReadEntries(Unknown));
        Assert.Equal(4062, e.RecipeId);
        Assert.Equal(0, e.Tier);
        Assert.Equal(1, e.Level);
        Assert.True(e.IsExact);
    }

    [Fact]
    public void ReadEntries_TwoEntriesWithEscapedQuote()
    {
        var list = PreviewLogic.ReadEntries(Two);
        Assert.Equal(new[] { 7008, 5008 }, list.Select(x => x.RecipeId));
        Assert.Equal(2, list[1].Tier);
    }

    // A game update can add fields, change their order, or add nested values.
    private const string Reordered = "[ {\"Icon\":\"a.png\", \"IsExact\":true, \"Extra\":{\"Tier\":9,\"List\":[1,{\"RecipeId\":1}]}, \"Level\":2,\n \"Name\":\"Tier \\\"3\\\" {dish}\", \"Tags\":[\"x\",\"]\"], \"Tier\":1, \"RecipeId\":7008, \"Price\":-1.5e2, \"None\":null} ]";

    [Fact]
    public void ReadEntries_AnyFieldOrderAndUnknownFields()
    {
        var e = Assert.Single(PreviewLogic.ReadEntries(Reordered));
        Assert.Equal(7008, e.RecipeId);
        Assert.Equal(1, e.Tier);
        Assert.Equal(2, e.Level);
        Assert.True(e.IsExact);
    }

    [Fact]
    public void AddPreviews_AnyFieldOrderAndUnknownFields()
    {
        string result = PreviewLogic.AddPreviews(Reordered, new Dictionary<int, string> { [0] = "x", [1] = "nested must not match" });
        var entry = JsonDocument.Parse(result).RootElement[0];
        Assert.Equal("x", entry.GetProperty("Preview").GetString());
        Assert.Equal("Tier \"3\" {dish}", entry.GetProperty("Name").GetString());
        Assert.False(entry.GetProperty("Extra").GetProperty("List")[1].TryGetProperty("Preview", out _));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not json")]
    [InlineData("[{\"Name\":\"x\"}]")]
    [InlineData("[{\"RecipeId\":7008,\"Tier\":3,\"Level\":2,\"IsExact\":false}")]
    [InlineData("[{\"RecipeId\":\"7008\",\"Tier\":3,\"Level\":2,\"IsExact\":false}]")]
    [InlineData("{\"RecipeId\":7008,\"Tier\":3,\"Level\":2,\"IsExact\":false}")]
    public void ReadEntries_NothingToRead(string json)
    {
        Assert.Empty(PreviewLogic.ReadEntries(json));
    }

    [Fact]
    public void AddPreviews_OneEntry()
    {
        string result = PreviewLogic.AddPreviews(Known, new Dictionary<int, string> { [0] = "Perfect 72% Sat 21 x1\nGood 28% Sat 17 x1" });
        var entry = JsonDocument.Parse(result).RootElement[0];
        Assert.Equal("Perfect 72% Sat 21 x1\nGood 28% Sat 17 x1", entry.GetProperty("Preview").GetString());
        Assert.Equal("Stir-Fried Aged Mushrooms with Meat", entry.GetProperty("Name").GetString());
        Assert.Equal(7008, entry.GetProperty("RecipeId").GetInt32());
    }

    [Fact]
    public void AddPreviews_TwoEntriesOneMatch()
    {
        string result = PreviewLogic.AddPreviews(Two, new Dictionary<int, string> { [1] = "x" });
        var root = JsonDocument.Parse(result).RootElement;
        Assert.False(root[0].TryGetProperty("Preview", out _));
        Assert.Equal("x", root[1].GetProperty("Preview").GetString());
        Assert.Equal("A \"quoted\" dish", root[0].GetProperty("Name").GetString());
    }

    private const string SameRecipeTwice = "[{\"RecipeId\":4062,\"Name\":\"Pork Chops\",\"Tier\":0,\"Level\":2,\"IsExact\":true,\"Icon\":\"a.png\"},{\"RecipeId\":4062,\"Name\":\"Pork Chops\",\"Tier\":0,\"Level\":2,\"IsExact\":true,\"Icon\":\"a.png\"}]";

    [Fact]
    public void AddPreviews_TwoEntriesOfOneRecipe_EachGetsItsOwnPreview()
    {
        string result = PreviewLogic.AddPreviews(SameRecipeTwice, new Dictionary<int, string> { [0] = "top", [1] = "second" }, new Dictionary<int, string> { [0] = "tip top", [1] = "tip second" });
        var root = JsonDocument.Parse(result).RootElement;
        Assert.Equal("top", root[0].GetProperty("Preview").GetString());
        Assert.Equal("second", root[1].GetProperty("Preview").GetString());
        Assert.Equal("tip second", root[1].GetProperty("PreviewTip").GetString());
    }

    [Fact]
    public void AddPreviews_EscapesTheText()
    {
        string result = PreviewLogic.AddPreviews(Known, new Dictionary<int, string> { [0] = "a\"b\\c" });
        Assert.Equal("a\"b\\c", JsonDocument.Parse(result).RootElement[0].GetProperty("Preview").GetString());
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not json")]
    public void AddPreviews_ReturnsTheInputWhenNothingMatches(string json)
    {
        Assert.Equal(json, PreviewLogic.AddPreviews(json, new Dictionary<int, string> { [0] = "x" }));
    }

    [Fact]
    public void AddPreviews_ReturnsTheInputOnAnError()
    {
        Assert.Equal(Known, PreviewLogic.AddPreviews(Known, null));
    }

    [Fact]
    public void AddPreviews_WithTip_AddsBothFieldsEscaped()
    {
        string result = PreviewLogic.AddPreviews(Known, new Dictionary<int, string> { [0] = "prev" }, new Dictionary<int, string> { [0] = "tip \"text\"" });
        var entry = JsonDocument.Parse(result).RootElement[0];
        Assert.Equal("prev", entry.GetProperty("Preview").GetString());
        Assert.Equal("tip \"text\"", entry.GetProperty("PreviewTip").GetString());
    }

    [Fact]
    public void AddPreviews_WithPortionText_AddsThePreviewPortionField()
    {
        string result = PreviewLogic.AddPreviews(SameRecipeTwice, new Dictionary<int, string> { [0] = "whole", [1] = "whole 2" }, null, new Dictionary<int, string> { [0] = "portion", [1] = "portion 2" });
        var root = JsonDocument.Parse(result).RootElement;
        Assert.Equal("portion", root[0].GetProperty("PreviewPortion").GetString());
        Assert.Equal("portion 2", root[1].GetProperty("PreviewPortion").GetString());
        Assert.Equal("whole 2", root[1].GetProperty("Preview").GetString());
    }

    [Fact]
    public void AddPreviews_NoTipForTheEntry_OnlyThePreviewField()
    {
        string result = PreviewLogic.AddPreviews(Known, new Dictionary<int, string> { [0] = "prev" }, new Dictionary<int, string>());
        var entry = JsonDocument.Parse(result).RootElement[0];
        Assert.True(entry.TryGetProperty("Preview", out _));
        Assert.False(entry.TryGetProperty("PreviewTip", out _));
    }

    [Fact]
    public void AddPreviews_NullTipDictionary_SameOutputAsBefore()
    {
        var previews = new Dictionary<int, string> { [0] = "prev" };
        Assert.Equal(PreviewLogic.AddPreviews(Known, previews), PreviewLogic.AddPreviews(Known, previews, null));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not json")]
    public void AddPreviews_WithTip_ReturnsTheInputWhenNothingMatches(string json)
    {
        Assert.Equal(json, PreviewLogic.AddPreviews(json, new Dictionary<int, string> { [0] = "x" }, new Dictionary<int, string> { [0] = "y" }));
    }
    [Fact]
    public void PredictionJson_GameFieldsThatReadEntriesReadsBack()
    {
        string json = RowSplitLogic.PredictionJson(new List<RowSplitLogic.PredictionEntry>
        {
            new RowSplitLogic.PredictionEntry { RecipeId = 4062, Name = "Salt \"and\" Pepper", Tier = 0, Level = 2, IsExact = true, Icon = "../a.png" },
            new RowSplitLogic.PredictionEntry { RecipeId = 7008, Name = "Unknown Dish", Tier = 3, Level = 1, IsExact = false, Icon = "" },
        });
        var root = JsonDocument.Parse(json).RootElement;
        Assert.Equal("Salt \"and\" Pepper", root[0].GetProperty("Name").GetString());
        Assert.Equal("../a.png", root[0].GetProperty("Icon").GetString());
        Assert.Equal("", root[1].GetProperty("Icon").GetString());
        var entries = PreviewLogic.ReadEntries(json);
        Assert.Equal(new[] { 4062, 7008 }, entries.Select(e => e.RecipeId));
        Assert.True(entries[0].IsExact);
        Assert.Equal(2, entries[0].Level);
        Assert.Equal(3, entries[1].Tier);
        Assert.Equal(1, entries[1].Level);
        Assert.False(entries[1].IsExact);
    }

    [Fact]
    public void PredictionJson_NoEntryGivesAnEmptyList()
    {
        Assert.Equal("[]", RowSplitLogic.PredictionJson(new List<RowSplitLogic.PredictionEntry>()));
    }
}
