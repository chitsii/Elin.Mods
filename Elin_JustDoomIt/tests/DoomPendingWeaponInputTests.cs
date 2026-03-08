using Xunit;

namespace Elin_JustDoomIt.Tests;

public sealed class DoomPendingWeaponInputTests
{
    [Fact]
    public void Capture_DirectWeapon_StoresPendingSlot()
    {
        var pending = new DoomPendingWeaponInput();

        pending.Capture(3, 0);
        pending.Consume(out var slot, out var cycle);

        Assert.Equal(3, slot);
        Assert.Equal(0, cycle);
    }

    [Fact]
    public void Capture_MultipleDirectWeapons_LastPressWins()
    {
        var pending = new DoomPendingWeaponInput();

        pending.Capture(2, 0);
        pending.Capture(5, 0);
        pending.Consume(out var slot, out _);

        Assert.Equal(5, slot);
    }

    [Fact]
    public void Capture_WeaponCycle_AccumulatesUntilConsumed()
    {
        var pending = new DoomPendingWeaponInput();

        pending.Capture(0, 1);
        pending.Capture(0, 1);
        pending.Capture(0, -1);
        pending.Consume(out var slot, out var cycle);

        Assert.Equal(0, slot);
        Assert.Equal(1, cycle);
    }

    [Fact]
    public void Consume_ClearsPendingState()
    {
        var pending = new DoomPendingWeaponInput();

        pending.Capture(4, -1);
        pending.Consume(out _, out _);
        pending.Consume(out var slot, out var cycle);

        Assert.Equal(0, slot);
        Assert.Equal(0, cycle);
    }

    [Fact]
    public void ConsumeOneTick_ReplaysPositiveCycleOneStepAtATime()
    {
        var pending = new DoomPendingWeaponInput();

        pending.Capture(0, 3);

        pending.ConsumeOneTick(out var slot1, out var cycle1);
        pending.ConsumeOneTick(out var slot2, out var cycle2);
        pending.ConsumeOneTick(out var slot3, out var cycle3);
        pending.ConsumeOneTick(out var slot4, out var cycle4);

        Assert.Equal(0, slot1);
        Assert.Equal(1, cycle1);
        Assert.Equal(0, slot2);
        Assert.Equal(1, cycle2);
        Assert.Equal(0, slot3);
        Assert.Equal(1, cycle3);
        Assert.Equal(0, slot4);
        Assert.Equal(0, cycle4);
    }

    [Fact]
    public void ConsumeOneTick_ReplaysNegativeCycleOneStepAtATime()
    {
        var pending = new DoomPendingWeaponInput();

        pending.Capture(0, -2);

        pending.ConsumeOneTick(out _, out var cycle1);
        pending.ConsumeOneTick(out _, out var cycle2);
        pending.ConsumeOneTick(out _, out var cycle3);

        Assert.Equal(-1, cycle1);
        Assert.Equal(-1, cycle2);
        Assert.Equal(0, cycle3);
    }

    [Fact]
    public void ConsumeOneTick_PrioritizesDirectWeaponSelection()
    {
        var pending = new DoomPendingWeaponInput();

        pending.Capture(6, 2);

        pending.ConsumeOneTick(out var slot1, out var cycle1);
        pending.ConsumeOneTick(out var slot2, out var cycle2);

        Assert.Equal(6, slot1);
        Assert.Equal(0, cycle1);
        Assert.Equal(0, slot2);
        Assert.Equal(0, cycle2);
    }

    [Fact]
    public void Capture_DirectWeapon_ClearsPreviouslyQueuedCycle()
    {
        var pending = new DoomPendingWeaponInput();

        pending.Capture(0, 2);
        pending.Capture(4, 0);
        pending.ConsumeOneTick(out var slot1, out var cycle1);
        pending.ConsumeOneTick(out var slot2, out var cycle2);

        Assert.Equal(4, slot1);
        Assert.Equal(0, cycle1);
        Assert.Equal(0, slot2);
        Assert.Equal(0, cycle2);
    }

    [Fact]
    public void Capture_CycleAfterDirectWeapon_ClearsDirectSelection()
    {
        var pending = new DoomPendingWeaponInput();

        pending.Capture(5, 0);
        pending.Capture(0, -2);
        pending.ConsumeOneTick(out var slot1, out var cycle1);
        pending.ConsumeOneTick(out var slot2, out var cycle2);

        Assert.Equal(0, slot1);
        Assert.Equal(-1, cycle1);
        Assert.Equal(0, slot2);
        Assert.Equal(-1, cycle2);
    }
}
