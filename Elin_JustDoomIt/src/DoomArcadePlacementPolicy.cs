namespace Elin_JustDoomIt
{
    internal enum DoomArcadePlacementDecision
    {
        IgnoreZone,
        SkipMissingSource,
        PlaceNewCabinet
    }

    internal static class DoomArcadePlacementPolicy
    {
        public const int TargetFloorLv = 1;

        public static DoomArcadePlacementDecision Decide(
            bool isCasinoZone,
            int zoneLv,
            bool hasExistingCabinet,
            bool sourceReady)
        {
            if (!isCasinoZone)
            {
                return DoomArcadePlacementDecision.IgnoreZone;
            }

            if (zoneLv != TargetFloorLv)
            {
                return DoomArcadePlacementDecision.IgnoreZone;
            }

            if (hasExistingCabinet)
            {
                return DoomArcadePlacementDecision.IgnoreZone;
            }

            return sourceReady
                ? DoomArcadePlacementDecision.PlaceNewCabinet
                : DoomArcadePlacementDecision.SkipMissingSource;
        }
    }
}
