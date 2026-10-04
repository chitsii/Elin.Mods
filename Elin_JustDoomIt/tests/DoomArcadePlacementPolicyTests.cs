using Xunit;

namespace Elin_JustDoomIt.Tests;

public sealed class DoomArcadePlacementPolicyTests
{
    [Fact]
    public void Decide_NonTargetCasinoFloorWithExistingCabinet_PreservesIt()
    {
        var decision = DoomArcadePlacementPolicy.Decide(
            isCasinoZone: true,
            zoneLv: DoomArcadePlacementPolicy.TargetFloorLv - 1,
            hasExistingCabinet: true,
            sourceReady: true);

        Assert.Equal(DoomArcadePlacementDecision.IgnoreZone, decision);
    }

    [Fact]
    public void Decide_TargetFloorWithExistingCabinet_DoesNotMutateIt()
    {
        var decision = DoomArcadePlacementPolicy.Decide(
            isCasinoZone: true,
            zoneLv: DoomArcadePlacementPolicy.TargetFloorLv,
            hasExistingCabinet: true,
            sourceReady: true);

        Assert.Equal(DoomArcadePlacementDecision.IgnoreZone, decision);
    }

    [Fact]
    public void Decide_TargetFloorWithoutSource_SkipsPlacement()
    {
        var decision = DoomArcadePlacementPolicy.Decide(
            isCasinoZone: true,
            zoneLv: DoomArcadePlacementPolicy.TargetFloorLv,
            hasExistingCabinet: false,
            sourceReady: false);

        Assert.Equal(DoomArcadePlacementDecision.SkipMissingSource, decision);
    }

    [Fact]
    public void Decide_TargetFloorWithoutExistingCabinetAndValidSource_PlacesNewCabinet()
    {
        var decision = DoomArcadePlacementPolicy.Decide(
            isCasinoZone: true,
            zoneLv: DoomArcadePlacementPolicy.TargetFloorLv,
            hasExistingCabinet: false,
            sourceReady: true);

        Assert.Equal(DoomArcadePlacementDecision.PlaceNewCabinet, decision);
    }
}
