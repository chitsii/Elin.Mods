using System;

namespace Elin_JustDoomIt
{
    internal static class DoomBgmRuntime
    {
        public static SoundManager TryGetSoundManager()
        {
            return SoundManager.current;
        }

        public static Zone TryGetActiveZone()
        {
            return EClass.core?.game?.activeZone;
        }

        public static bool CanRefreshZoneBgm()
        {
            var game = EClass.core?.game;
            var player = game?.player;
            var pc = player?.chara;
            var zone = game?.activeZone;
            return zone != null && player != null && pc != null && pc.IsInActiveZone && !player.simulatingZone;
        }

        public static void TryStopCurrentBgm(string warningPrefix)
        {
            try
            {
                TryGetSoundManager()?.StopBGM();
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn(warningPrefix + ex.Message);
            }
        }

        public static void TryRefreshZoneBgm(string warningPrefix)
        {
            if (!CanRefreshZoneBgm())
            {
                return;
            }

            try
            {
                TryGetActiveZone()?.RefreshBGM();
            }
            catch (Exception ex)
            {
                DoomDiagnostics.Warn(warningPrefix + ex.Message);
            }
        }
    }
}
