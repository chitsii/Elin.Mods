using Xunit;

namespace Elin_JustDoomIt.Tests;

public sealed class DoomWeaponCyclePlannerTests
{
    [Fact]
    public void PlanCycleStep_UsesPredictedSlotUntilActualReadyCatchesUp()
    {
        var planner = new DoomWeaponCyclePlanner();
        planner.Reset(2);

        var first = planner.PlanCycleStep(+1);
        planner.SyncActualReady(2);
        var second = planner.PlanCycleStep(+1);

        Assert.Equal(3, first);
        Assert.Equal(4, second);
    }

    [Fact]
    public void SyncActualReady_ClearsPredictionWhenTargetReached()
    {
        var planner = new DoomWeaponCyclePlanner();
        planner.Reset(2);

        var first = planner.PlanCycleStep(+1);
        planner.SyncActualReady(3);
        var second = planner.PlanCycleStep(+1);

        Assert.Equal(3, first);
        Assert.Equal(4, second);
    }

    [Fact]
    public void PlanDirectSlot_DoesNotAffectCycleBaseUntilActualReadyChanges()
    {
        var planner = new DoomWeaponCyclePlanner();
        planner.Reset(2);

        var direct = planner.PlanDirectSlot(5);
        var cycled = planner.PlanCycleStep(-1);

        Assert.Equal(5, direct);
        Assert.Equal(1, cycled);
    }

    [Fact]
    public void Reset_UnknownReadySlotDoesNotAssumePistol()
    {
        var planner = new DoomWeaponCyclePlanner();
        planner.Reset(0);
        planner.SyncActualReady(6);

        var cycled = planner.PlanCycleStep(+1);

        Assert.Equal(7, cycled);
    }
}
