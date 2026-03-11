using Xunit;

namespace Elin_JustDoomIt.Tests;

public sealed class DoomLaunchPreferencesTests
{
    [Fact]
    public void ParseRewardRateSelectionMode_InvalidValue_FallsBackToAskEveryMap()
    {
        var mode = DoomLaunchPreferences.ParseRewardRateSelectionMode("unknown");

        Assert.Equal(DoomRewardRateSelectionMode.AskEveryMap, mode);
    }

    [Fact]
    public void ResolveConfiguredRate_FixedMid_ReturnsMidRate()
    {
        var rate = DoomLaunchPreferences.ResolveConfiguredRate(DoomRewardRateSelectionMode.FixedMid);

        Assert.Equal(DoomRewardRate.Mid, rate);
    }
}
