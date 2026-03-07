using Xunit;

namespace Elin_JustDoomIt.Tests;

public sealed class DoomVideoSettingsTests
{
    [Fact]
    public void GetClosestResolutionPresetIndex_ExactMatch_ReturnsMatchingPreset()
    {
        var index = DoomVideoSettings.GetClosestResolutionPresetIndex(640, 400);

        Assert.Equal(1, index);
    }

    [Fact]
    public void GetClosestResolutionPresetIndex_LegacyCustomValue_MapsToNearestPreset()
    {
        var index = DoomVideoSettings.GetClosestResolutionPresetIndex(800, 500);

        Assert.Equal(1, index);
    }

    [Fact]
    public void FormatResolutionSummary_UsesPresetLabelAndDimensions()
    {
        var summary = DoomVideoSettings.FormatResolutionSummary(960, 600);

        Assert.Equal("LARGE (960x600)", summary);
    }
}
