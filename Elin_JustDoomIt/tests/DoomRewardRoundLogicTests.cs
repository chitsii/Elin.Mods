using Xunit;

namespace Elin_JustDoomIt.Tests;

public sealed class DoomRewardRoundLogicTests
{
    [Fact]
    public void GetConfig_AnyRate_UsesFixedValues()
    {
        var config = DoomRewardRoundLogic.GetConfig(DoomRewardRate.High);

        Assert.Equal("FIXED", config.Code);
        Assert.Equal(100, config.EntryCost);
        Assert.Equal(70, config.BaseReward);
        Assert.Equal(0, config.HitLossPercent);
    }

    [Fact]
    public void CalculateKillPoolGain_UsesKillStreakMultiplier()
    {
        var reward = DoomRewardRoundLogic.CalculateKillPoolGain(4, 2);

        Assert.Equal(140, reward);
    }

    [Fact]
    public void CalculateHitLoss_IsDisabled()
    {
        var loss = DoomRewardRoundLogic.CalculateHitLoss(DoomRewardRate.Mid, 1267);

        Assert.Equal(0, loss);
    }

    [Fact]
    public void CalculateRunNet_UsesFixedEntryCost()
    {
        var runNet = DoomRewardRoundLogic.CalculateRunNet(DoomRewardRoundLogic.GetFixedRate(), 740);

        Assert.Equal(640, runNet);
    }

    [Fact]
    public void GetSecretPoolGain_UsesFlatFiveHundred()
    {
        Assert.Equal(500, DoomRewardRoundLogic.GetSecretPoolGain());
    }

    [Fact]
    public void CalculateKillPoolGain_GrowsByFixedIncrementsAndCapsByDifficulty()
    {
        Assert.Equal(70, DoomRewardRoundLogic.CalculateKillPoolGain(1, 0));
        Assert.Equal(105, DoomRewardRoundLogic.CalculateKillPoolGain(1, 1));
        Assert.Equal(140, DoomRewardRoundLogic.CalculateKillPoolGain(1, 2));
        Assert.Equal(140, DoomRewardRoundLogic.CalculateKillPoolGain(1, 3));
        Assert.Equal(140, DoomRewardRoundLogic.CalculateKillPoolGain(1, 4));
    }

    [Fact]
    public void AdvanceMultiplierStage_ContinuesGrowing()
    {
        var stage = 0;
        for (var i = 0; i < 10; i++)
        {
            stage = DoomRewardRoundLogic.AdvanceMultiplierStage(stage);
        }

        Assert.Equal(10, stage);
        Assert.Equal("+280", DoomRewardRoundLogic.FormatKillBonus(5, stage));
    }

    [Fact]
    public void GetDisplayedKillBonusPercent_UsesDifficultyCaps()
    {
        Assert.Equal(100, DoomRewardRoundLogic.GetDisplayedKillBonusPercent(1, 10));
        Assert.Equal(200, DoomRewardRoundLogic.GetDisplayedKillBonusPercent(3, 10));
        Assert.Equal(400, DoomRewardRoundLogic.GetDisplayedKillBonusPercent(5, 10));
    }

    [Fact]
    public void HigherDifficultyCaps_StayAboveLowerDifficultyCaps()
    {
        const int stage = 25;

        var low = DoomRewardRoundLogic.CalculateKillPoolGain(1, stage);
        var mid = DoomRewardRoundLogic.CalculateKillPoolGain(3, stage);
        var high = DoomRewardRoundLogic.CalculateKillPoolGain(5, stage);

        Assert.True(low < mid);
        Assert.True(mid < high);
    }
}
