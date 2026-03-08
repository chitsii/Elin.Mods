using Xunit;

namespace Elin_JustDoomIt.Tests;

public sealed class DoomBindingParserTests
{
    [Fact]
    public void ParseCsv_ParsesKeysMouseAndWheelTokens()
    {
        var bindings = DoomBindingParser.ParseCsv("W, Mouse0, WheelUp, WheelDown");

        Assert.Equal(4, bindings.Count);
        Assert.Equal(DoomBindingKind.Key, bindings[0].Kind);
        Assert.Equal("W", bindings[0].KeyName);
        Assert.Equal(DoomBindingKind.MouseButton, bindings[1].Kind);
        Assert.Equal(0, bindings[1].MouseButton);
        Assert.Equal(DoomBindingKind.WheelUp, bindings[2].Kind);
        Assert.Equal(DoomBindingKind.WheelDown, bindings[3].Kind);
    }

    [Fact]
    public void ParseCsv_IgnoresEmptyTokens()
    {
        var bindings = DoomBindingParser.ParseCsv("W, , ,UpArrow");

        Assert.Equal(2, bindings.Count);
        Assert.Equal("W", bindings[0].KeyName);
        Assert.Equal("UpArrow", bindings[1].KeyName);
    }

    [Fact]
    public void ParseCsv_TreatsUnknownTokensAsKeys()
    {
        var bindings = DoomBindingParser.ParseCsv("JoystickButton0");

        Assert.Single(bindings);
        Assert.Equal(DoomBindingKind.Key, bindings[0].Kind);
        Assert.Equal("JoystickButton0", bindings[0].KeyName);
    }
}
