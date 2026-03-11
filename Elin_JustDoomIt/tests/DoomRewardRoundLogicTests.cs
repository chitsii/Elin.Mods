using Xunit;

namespace Elin_JustDoomIt.Tests;

public sealed class DoomRewardRoundLogicTests
{
    [Fact]
    public void GetConfig_HighRate_UsesPlanCValues()
    {
        var config = DoomRewardRoundLogic.GetConfig(DoomRewardRate.High);

        Assert.Equal("HIGH", config.Code);
        Assert.Equal(1000, config.EntryCost);
        Assert.Equal(110, config.BaseReward);
        Assert.Equal(30, config.HitLossPercent);
    }

    [Fact]
    public void CalculateKillPoolGain_UsesDifficultyAndMultiplier()
    {
        var reward = DoomRewardRoundLogic.CalculateKillPoolGain(DoomRewardRate.Mid, 4, 2);

        Assert.Equal(175, reward);
    }

    [Fact]
    public void CalculateHitLoss_RoundsAndClampsToPool()
    {
        var loss = DoomRewardRoundLogic.CalculateHitLoss(DoomRewardRate.Mid, 1267);

        Assert.Equal(304, loss);
    }

    [Fact]
    public void CalculateRunNet_IncludesEntryCost()
    {
        var runNet = DoomRewardRoundLogic.CalculateRunNet(DoomRewardRate.Low, 740);

        Assert.Equal(640, runNet);
    }

    [Fact]
    public void GetSecretPoolGain_UsesFlatFiveHundred()
    {
        Assert.Equal(500, DoomRewardRoundLogic.GetSecretPoolGain());
    }

    [Fact]
    public void CalculateKillPoolGain_HighRateOnSkillOne_UsesDreamLaneValues()
    {
        Assert.Equal(55, DoomRewardRoundLogic.CalculateKillPoolGain(DoomRewardRate.High, 1, 0));
        Assert.Equal(105, DoomRewardRoundLogic.CalculateKillPoolGain(DoomRewardRate.High, 1, 1));
        Assert.Equal(147, DoomRewardRoundLogic.CalculateKillPoolGain(DoomRewardRate.High, 1, 2));
        Assert.Equal(182, DoomRewardRoundLogic.CalculateKillPoolGain(DoomRewardRate.High, 1, 3));
        Assert.Equal(212, DoomRewardRoundLogic.CalculateKillPoolGain(DoomRewardRate.High, 1, 4));
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
        Assert.Equal("x4.0", DoomRewardRoundLogic.FormatMultiplier(DoomRewardRate.Mid, stage));
    }

    [Fact]
    public void GetDisplayedKillBonusPercent_UsesRateSpecificCaps()
    {
        Assert.Equal(200, DoomRewardRoundLogic.GetDisplayedKillBonusPercent(DoomRewardRate.Low, 10));
        Assert.Equal(300, DoomRewardRoundLogic.GetDisplayedKillBonusPercent(DoomRewardRate.Mid, 10));
        Assert.Equal(500, DoomRewardRoundLogic.GetDisplayedKillBonusPercent(DoomRewardRate.High, 10));
    }

    [Fact]
    public void HigherRates_StayAboveLowerRates_AtTheSameStage()
    {
        const int stage = 25;

        var low = DoomRewardRoundLogic.CalculateKillPoolGain(DoomRewardRate.Low, 3, stage);
        var mid = DoomRewardRoundLogic.CalculateKillPoolGain(DoomRewardRate.Mid, 3, stage);
        var high = DoomRewardRoundLogic.CalculateKillPoolGain(DoomRewardRate.High, 3, stage);

        Assert.True(low < mid);
        Assert.True(mid < high);
    }
}
