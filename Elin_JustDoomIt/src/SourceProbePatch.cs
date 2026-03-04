using HarmonyLib;

namespace Elin_ModTemplate
{
    [HarmonyPatch(typeof(SourceManager), nameof(SourceManager.Init))]
    public static class Patch_SourceManager_Init
    {
        private const string ProbeId = "justdoomit_arcade";

        private static void Postfix()
        {
            try
            {
                var hasThing = EClass.sources?.things?.map?.ContainsKey(ProbeId) ?? false;
                var hasCard = EClass.sources?.cards?.map?.ContainsKey(ProbeId) ?? false;
                DoomDiagnostics.Info("[JustDoomIt] Source probe: id=" + ProbeId +
                                     " things.map=" + hasThing +
                                     " cards.map=" + hasCard);
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] Source probe failed.", ex);
            }
        }
    }
}
