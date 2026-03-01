using System;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Manages the Erenos Shadow boss encounter (Stage 7 -> Stage 8).
    /// </summary>
    public static class ErenosBattle
    {
        /// <summary>
        /// Spawn the Erenos Shadow boss near the player.
        /// </summary>
        public static void SpawnBoss()
        {
            var quest = NecromancyManager.Instance.QuestPath;
            if (quest.CurrentStage != UnhallowedStage.Stage7)
            {
                ModLog.Log("ErenosBattle.SpawnBoss: wrong stage ({0}), ignoring", quest.CurrentStage);
                return;
            }

            if (quest.ErenosSpawned)
            {
                // Reset for re-spawn (e.g. player left zone mid-battle, then re-triggered from UI)
                quest.ResetErenosSpawned();
                ModLog.Log("ErenosBattle.SpawnBoss: resetting ErenosSpawned for re-spawn");
            }

            try
            {
                var boss = KnightEncounter.FindGlobalErenos();
                if (boss == null)
                {
                    // フォールバック: Stage4をスキップした等の異常系
                    boss = CharaGen.Create("ars_erenos_shadow");
                    boss?.SetGlobal();
                }
                if (boss == null)
                {
                    ModLog.Warn("ErenosBattle: failed to create ars_erenos_shadow");
                    return;
                }

                // Place near player
                EClass._zone.AddCard(boss, NecroSpellUtil.GetSpawnPos());
                boss.hostility = Hostility.Enemy;
                boss.c_originalHostility = Hostility.Enemy;

                NecroVFX.PlaySummon(boss);

                // Spawn Erenos's own servants
                const int GuardCount = 9;
                const int ShadeCount = 3;

                for (int i = 0; i < GuardCount; i++)
                {
                    var guard = CharaGen.Create("ars_erenos_guard");
                    if (guard == null) continue;
                    EClass._zone.AddCard(guard, NecroSpellUtil.GetSpawnPos());
                    guard.hostility = Hostility.Enemy;
                    guard.c_originalHostility = Hostility.Enemy;
                }

                for (int i = 0; i < ShadeCount; i++)
                {
                    var shade = CharaGen.Create("ars_erenos_shade");
                    if (shade == null) continue;
                    EClass._zone.AddCard(shade, NecroSpellUtil.GetSpawnPos());
                    shade.hostility = Hostility.Enemy;
                    shade.c_originalHostility = Hostility.Enemy;
                }

                quest.MarkErenosSpawned();
                ModLog.Log("ErenosBattle: spawned Erenos Shadow + 12 servants (uid={0}, Lv={1})", boss.uid, boss.LV);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"ErenosBattle.SpawnBoss error: {ex.Message}");
            }
        }
    }
}
