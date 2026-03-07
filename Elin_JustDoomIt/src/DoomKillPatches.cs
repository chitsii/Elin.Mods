using DoomNetFrameworkEngine.DoomEntity.World;
using HarmonyLib;

namespace Elin_ModTemplate
{
    [HarmonyPatch(typeof(ThingInteraction), nameof(ThingInteraction.KillMobj))]
    public static class Patch_ThingInteraction_KillMobj
    {
        static void Prefix(Mobj source, Mobj target)
        {
            try
            {
                if (target == null)
                {
                    return;
                }

                if ((target.Flags & MobjFlags.CountKill) == 0)
                {
                    return;
                }

                var world = source?.World ?? target.World;
                if (world == null)
                {
                    return;
                }

                var countsAsConsolePlayerKill = false;
                if (source != null && source.Player != null)
                {
                    countsAsConsolePlayerKill = ReferenceEquals(source.Player, world.ConsolePlayer);
                }

                if (!countsAsConsolePlayerKill)
                {
                    return;
                }

                DoomKillFeed.EnqueueEnemy(target.Type);
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] KillMobj patch failed.", ex);
            }
        }
    }
}
