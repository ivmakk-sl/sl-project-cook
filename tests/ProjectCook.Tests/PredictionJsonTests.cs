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
        string result = PreviewLogic.AddPreviews(Reordered, new Dictionary<int, string> { [7008] = "x", [1] = "nested must not match" });
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
        string result = PreviewLogic.AddPreviews(Known, new Dictionary<int, string> { [7008] = "Perfect 72% Sat 21 x1\nGood 28% Sat 17 x1" });
        var entry = JsonDocument.Parse(result).RootElement[0];
        Assert.Equal("Perfect 72% Sat 21 x1\nGood 28% Sat 17 x1", entry.GetProperty("Preview").GetString());
        Assert.Equal("Stir-Fried Aged Mushrooms with Meat", entry.GetProperty("Name").GetString());
        Assert.Equal(7008, entry.GetProperty("RecipeId").GetInt32());
    }

    [Fact]
    public void AddPreviews_TwoEntriesOneMatch()
    {
        string result = PreviewLogic.AddPreviews(Two, new Dictionary<int, string> { [5008] = "x" });
        var root = JsonDocument.Parse(result).RootElement;
        Assert.False(root[0].TryGetProperty("Preview", out _));
        Assert.Equal("x", root[1].GetProperty("Preview").GetString());
        Assert.Equal("A \"quoted\" dish", root[0].GetProperty("Name").GetString());
    }

    [Fact]
    public void AddPreviews_EscapesTheText()
    {
        string result = PreviewLogic.AddPreviews(Known, new Dictionary<int, string> { [7008] = "a\"b\\c" });
        Assert.Equal("a\"b\\c", JsonDocument.Parse(result).RootElement[0].GetProperty("Preview").GetString());
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not json")]
    public void AddPreviews_ReturnsTheInputWhenNothingMatches(string json)
    {
        Assert.Equal(json, PreviewLogic.AddPreviews(json, new Dictionary<int, string> { [7008] = "x" }));
    }

    [Fact]
    public void AddPreviews_ReturnsTheInputOnAnError()
    {
        Assert.Equal(Known, PreviewLogic.AddPreviews(Known, null));
    }
}
