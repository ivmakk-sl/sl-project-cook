using ProjectCook;
using Xunit;

public class FlatJsonTests
{
    [Fact]
    public void ReadsStringsNumbersAndBooleans()
    {
        var o = Assert.Single(FlatJson.ReadArray("[{\"a\":\"x\\n\\u0041\\\"\",\"b\":-12,\"c\":1.5e1,\"d\":true,\"e\":false}]"));
        Assert.Equal("x\nA\"", o.Fields["a"]);
        Assert.Equal(-12.0, o.Fields["b"]);
        Assert.Equal(15.0, o.Fields["c"]);
        Assert.Equal(true, o.Fields["d"]);
        Assert.Equal(false, o.Fields["e"]);
    }

    [Fact]
    public void SkipsNullAndNestedValues()
    {
        var o = Assert.Single(FlatJson.ReadArray("[{\"n\":null,\"o\":{\"x\":[1,\"}\"]},\"l\":[{\"y\":2}],\"after\":7}]"));
        Assert.Null(o.Fields["n"]);
        Assert.Null(o.Fields["o"]);
        Assert.Null(o.Fields["l"]);
        Assert.Equal(7.0, o.Fields["after"]);
    }

    [Fact]
    public void GivesTheStartOfEachTopLevelObject()
    {
        string json = " [ {\"a\":1} , 5, \"s\", {\"b\":2} ] ";
        var list = FlatJson.ReadArray(json);
        Assert.Equal(2, list.Count);
        Assert.Equal(json.IndexOf('{'), list[0].Start);
        Assert.Equal(json.LastIndexOf('{'), list[1].Start);
    }

    [Fact]
    public void EmptyArrayAndEmptyObject()
    {
        Assert.Empty(FlatJson.ReadArray("[]"));
        Assert.Empty(Assert.Single(FlatJson.ReadArray("[{}]")).Fields);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"a\":1}")]
    [InlineData("[{\"a\":1}")]
    [InlineData("[{\"a\":1,}]")]
    [InlineData("[{\"a\" 1}]")]
    [InlineData("[{\"a\":\"open}]")]
    [InlineData("[{\"a\":1}] x")]
    public void NullWhenTheTextIsNotAJsonArray(string json)
    {
        Assert.Null(FlatJson.ReadArray(json));
    }
}
