using Xunit;

namespace Elin_JustDoomIt.Tests;

public sealed class DoomRewardRoundControllerTests
{
    [Fact]
    public void HandleMapStart_ResetsRoundState_WithoutOpeningRateSelection()
    {
        var controller = new DoomRewardRoundController();
        controller.TryBeginRound(DoomRewardRate.High, canAfford: true);

        var result = controller.HandleMapStart(4, "E2M3", "Refinery");

        Assert.False(result.OpenedRateSelection);
        Assert.False(controller.State.RoundActive);
        Assert.False(controller.State.RateSelectionOpen);
        Assert.Equal(DoomRewardRate.None, controller.State.CurrentRate);
        Assert.Equal("E2M3", controller.State.MapCode);
        Assert.Equal("Refinery", controller.State.MapTitle);
        Assert.Equal(4, controller.State.Skill);
    }

    [Fact]
    public void TryBeginRound_InsufficientFunds_LeavesRoundInactive()
    {
        var controller = new DoomRewardRoundController();
        controller.HandleMapStart(3, "E1M1", "Hangar");

        var result = controller.TryBeginRound(DoomRewardRate.Mid, canAfford: false);

        Assert.True(result.InsufficientFunds);
        Assert.False(controller.State.RoundActive);
        Assert.False(controller.State.RateSelectionOpen);
        Assert.Equal(DoomRewardRate.None, controller.State.CurrentRate);
    }

    [Fact]
    public void KillThenDamage_OnlyResetsMultiplier()
    {
        var controller = new DoomRewardRoundController();
        controller.HandleMapStart(3, "E1M1", "Hangar");
        controller.TryBeginRound(DoomRewardRate.Mid, canAfford: true);

        var kill = controller.HandleKill();
        var damage = controller.HandleDamage();

        Assert.Equal(70, kill.PoolDelta);
        Assert.Equal(0, damage.PoolDelta);
        Assert.True(damage.BonusReset);
        Assert.Equal(70, controller.State.CurrentPool);
        Assert.Equal(0, controller.State.MultiplierStage);
    }

    [Fact]
    public void Death_DoesNotCashOutAgain_AndResetsRound()
    {
        var controller = new DoomRewardRoundController();
        controller.HandleMapStart(5, "E1M8", "Phobos Anomaly");
        controller.TryBeginRound(DoomRewardRate.High, canAfford: true);
        controller.HandleKill();
        controller.HandleKill();

        var result = controller.HandleDeath();

        Assert.Equal(0, result.CashOutAmount);
        Assert.False(controller.State.RoundActive);
        Assert.False(controller.State.RateSelectionOpen);
        Assert.Equal(DoomRewardRate.None, controller.State.CurrentRate);
        Assert.Equal(0, controller.State.CurrentPool);
    }

    [Fact]
    public void Clear_OnlyAwardsFixedClearBonus()
    {
        var controller = new DoomRewardRoundController();
        controller.HandleMapStart(3, "E1M1", "Hangar");
        controller.TryBeginRound(DoomRewardRate.Mid, canAfford: true);
        controller.HandleKill();

        var result = controller.HandleClear(1000);

        Assert.Equal(1000, result.BonusAmount);
        Assert.Equal(0, result.CashOutAmount);
        Assert.False(controller.State.RoundActive);
    }

    [Fact]
    public void Clear_WithoutActiveRound_DoesNotAwardBonus()
    {
        var controller = new DoomRewardRoundController();
        controller.HandleMapStart(3, "E1M1", "Hangar");

        var result = controller.HandleClear(1000);

        Assert.Equal(0, result.BonusAmount);
        Assert.False(controller.State.RoundActive);
        Assert.Equal(DoomRewardRate.None, controller.State.CurrentRate);
    }

    [Fact]
    public void TryBeginRound_WithoutEntryCharge_StartsRoundForLaterMaps()
    {
        var controller = new DoomRewardRoundController();
        controller.HandleMapStart(4, "E1M2", "Nuclear Plant");

        var result = controller.TryBeginRound(DoomRewardRate.Mid, canAfford: true, chargeEntryCost: false);

        Assert.True(result.RoundStarted);
        Assert.Equal(0, result.EntryCost);
        Assert.True(controller.State.RoundActive);
        Assert.Equal(DoomRewardRate.Mid, controller.State.CurrentRate);
    }

    [Fact]
    public void Abort_DoesNotLoseAlreadyPaidRewards_AndResetsRound()
    {
        var controller = new DoomRewardRoundController();
        controller.HandleMapStart(2, "E3M1", "Slough of Despair");
        controller.TryBeginRound(DoomRewardRate.Low, canAfford: true);
        controller.HandleSecret(2);

        var result = controller.HandleAbort();

        Assert.Equal(0, result.LostPoolAmount);
        Assert.False(controller.State.RoundActive);
        Assert.Equal(0, controller.State.CurrentPool);
    }
}
