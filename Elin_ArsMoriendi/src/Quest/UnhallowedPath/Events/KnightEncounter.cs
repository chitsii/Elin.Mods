using System;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Manages the Temple Knight encounter (Stage 2 -> Stage 3).
    /// Spawns Karen + 3 knights on zone transition.
    /// Karen retreats at 30% HP, triggers Stage 3.
    /// </summary>
    public static class KnightEncounter
    {
        private const string KarenId = "ars_karen";
        private const string ErenosShadowId = "ars_erenos_shadow";
        private const string ErenosGuardId = "ars_erenos_guard";
        private const string ErenosShadeId = "ars_erenos_shade";
        private const string TempleKnightId = "ars_temple_knight";
        private const int KnightCount = 12;

        // Karen adventurer registration
        private const string KarenStayKey = "chitsii.ars.karen_stay";

        // Scout encounter
        private const string ScoutId = "ars_temple_scout";
        private const string ScoutsSpawnedKey = "chitsii.ars.quest.event.scouts_spawned";
        private const int ScoutCount = 3;
        private const int ScoutLevel = 65;

        // Pursuit pause
        private const string PursuitPausedKey = "chitsii.ars.pursuit_paused";

        internal static bool IsPursuitPaused()
        {
            return DialogFlagStore.IsTrue(EClass.player?.dialogFlags, PursuitPausedKey);
        }

        internal static void SetPursuitPaused(bool paused)
        {
            DialogFlagStore.SetBool(EClass.player?.dialogFlags, PursuitPausedKey, paused);
        }

        internal static bool HasPendingEncounter()
        {
            var quest = NecromancyManager.Instance.QuestPath;
            return quest.CurrentStage switch
            {
                UnhallowedStage.Stage2 => !quest.KarenDefeated,
                UnhallowedStage.Stage3 => !quest.KarenJournalSpawned,
                UnhallowedStage.Stage4 => !quest.ScoutsDefeated,
                UnhallowedStage.Stage7 => !quest.ErenosDefeated,
                _ => false,
            };
        }

        // ── NPC management (somewhere pattern) ──

        /// <summary>Find Karen in globalCharas (she should be in "somewhere").</summary>
        private static Chara? FindGlobalKaren()
        {
            if (EClass.game?.cards?.globalCharas == null) return null;
            foreach (var c in EClass.game.cards.globalCharas.Values)
                if (c.id == KarenId) return c;
            return null;
        }

        /// <summary>Find Erenos Shadow in globalCharas.</summary>
        internal static Chara? FindGlobalErenos()
        {
            if (EClass.game?.cards?.globalCharas == null) return null;
            foreach (var c in EClass.game.cards.globalCharas.Values)
                if (c.id == ErenosShadowId) return c;
            return null;
        }

        /// <summary>Create Karen and place in "somewhere" (first time only).</summary>
        public static void EnsureKarenExists()
        {
            if (FindGlobalKaren() != null) return;
            var karen = CharaGen.Create(KarenId);
            if (karen == null) return;
            karen.SetGlobal();
            karen.MoveZone("somewhere");
            ModLog.Log("KnightEncounter: Karen created and placed in somewhere");
        }

        /// <summary>Create Erenos Shadow and place in "somewhere" (first time only).</summary>
        public static void EnsureErenosExists()
        {
            if (FindGlobalErenos() != null) return;
            var erenos = CharaGen.Create(ErenosShadowId);
            if (erenos == null) return;
            erenos.SetGlobal();
            erenos.MoveZone("somewhere");
            ModLog.Log("KnightEncounter: Erenos Shadow created and placed in somewhere");
        }

        /// <summary>
        /// Called from Zone.Activate postfix.
        /// Stage2: first encounter (drama + hostile transition).
        /// Stage3: re-encounter if Karen was lost (immediate hostile spawn).
        /// Skips during vanilla encounter battles.
        /// </summary>
        public static void TrySpawnKnights()
        {
            var quest = NecromancyManager.Instance.QuestPath;
            if (quest.KarenDefeated)
            {
                if (!quest.KarenJournalSpawned)
                {
                    // 騎士を残して離脱 → フラグリセットで再エンカウント
                    quest.ResetKarenDefeated();
                    quest.ResetKnightsSpawned();
                    ModLog.Log("KnightEncounter: Knights not cleared, resetting for re-encounter");
                    // Fall through to spawn logic
                }
                else
                {
                    return;
                }
            }

            if (IsPursuitPaused()) return;

            var stage = quest.CurrentStage;
            if (stage != UnhallowedStage.Stage2 && stage != UnhallowedStage.Stage3) return;

            var pc = EClass.pc;
            if (pc == null) return;

            // Don't trigger on global map (Region) — only local zones
            if (EClass._zone.IsRegion) return;

            // Don't trigger during vanilla encounter battles
            if (pc.global?.transition?.state == ZoneTransition.EnterState.Encounter) return;

            ModLog.Log("KnightEncounter: TrySpawn check — stage={0}, KnightsSpawned={1}, zone={2}",
                stage, quest.KnightsSpawned, EClass._zone?.Name ?? "?");

            if (quest.KnightsSpawned)
            {
                // If Karen is no longer on this map, reset flag for next zone
                if (!IsKarenOnMap())
                {
                    quest.ResetKnightsSpawned();
                    ModLog.Log("KnightEncounter: Karen not on map, reset KnightsSpawned (stage={0})", stage);
                }
                return;
            }

            // ── Spawn Karen + knights ──

            if (stage == UnhallowedStage.Stage2)
            {
                SpawnFirstEncounter(pc, quest);
            }
            else // Stage3
            {
                SpawnReEncounter(pc, quest);
            }
        }

        private static void SpawnFirstEncounter(Chara pc, UnhallowedPath quest)
        {
            var karen = SpawnKaren(pc, Hostility.Neutral);
            if (karen == null) return;

            quest.MarkKnightsSpawned();

            // Play drama — Karen is on the map so GetActor("ars_karen") finds her
            QuestDrama.PlayDeferred("ars_karen_encounter", onComplete: () =>
            {
                MakeKnightsHostile(karen);
            });
        }

        private static void SpawnReEncounter(Chara pc, UnhallowedPath quest)
        {
            var karen = SpawnKaren(pc, Hostility.Enemy);
            if (karen == null) return;

            karen.c_originalHostility = Hostility.Enemy;

            int aliveKnights = CountAliveTempleKnights();
            if (aliveKnights == 0)
            {
                // Spawn temple knights immediately hostile
                for (int i = 0; i < KnightCount; i++)
                {
                    var knight = CharaGen.Create(TempleKnightId);
                    if (knight == null) continue;
                    EClass._zone.AddCard(knight, NecroSpellUtil.GetSpawnPos());
                    knight.hostility = Hostility.Enemy;
                    knight.c_originalHostility = Hostility.Enemy;
                }
            }
            else
            {
                ModLog.Log("KnightEncounter: Reusing existing knights (alive={0}), skipping respawn", aliveKnights);
            }

            quest.MarkKnightsSpawned();

            QuestDrama.TryStartUntilCompleteDeferred("ars_karen_ambush");
            ModLog.Log("KnightEncounter: Karen re-encounter (Stage3), hostile spawn");
        }

        private static Chara? SpawnKaren(Chara pc, Hostility hostility)
        {
            var karen = FindGlobalKaren();
            if (karen == null)
            {
                ModLog.Warn("KnightEncounter: Karen not found in globalCharas");
                return null;
            }

            // somewhere → current zone
            EClass._zone.AddCard(karen, NecroSpellUtil.GetSpawnPos());
            karen.hostility = hostility;

            // Equip Holy Lance Replica (if not already equipped)
            if (karen.things.List(t => t.id == "pole_holy", onlyAccessible: false).Count == 0)
            {
                var lance = ThingGen.Create("pole_holy");
                lance.SetReplica(on: true);
                lance.rarity = Rarity.Normal;
                karen.AddThing(lance);
                karen.body.Equip(lance);
            }

            pc.pos.PlayEffect("teleport");
            pc.pos.PlaySound("teleport");

            return karen;
        }

        private static bool IsKarenOnMap()
        {
            foreach (var c in EClass._map.charas)
            {
                if (c.id == KarenId && !c.isDead) return true;
            }
            return false;
        }

        private static void MakeKnightsHostile(Chara karen)
        {
            try
            {
                var pc = EClass.pc;
                if (pc == null) return;

                // Make Karen hostile
                karen.hostility = Hostility.Enemy;
                karen.c_originalHostility = Hostility.Enemy;

                // Spawn temple knights (already hostile)
                for (int i = 0; i < KnightCount; i++)
                {
                    var knight = CharaGen.Create(TempleKnightId);
                    if (knight == null) continue;
                    EClass._zone.AddCard(knight, NecroSpellUtil.GetSpawnPos());
                    knight.hostility = Hostility.Enemy;
                    knight.c_originalHostility = Hostility.Enemy;
                }

                // Advance to Stage 3 after spawn
                var quest = NecromancyManager.Instance.QuestPath;
                quest.TryHandleKarenEncounterHostile();

                ModLog.Log("KnightEncounter: Karen hostile + {0} knights spawned", KnightCount);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"KnightEncounter.MakeKnightsHostile error: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the first servant is created.
        /// Spawns 2 scout knights near PC as a first combat encounter with the servant.
        /// </summary>
        public static void TrySpawnScouts()
        {
            if (IsPursuitPaused()) return;

            var quest = NecromancyManager.Instance.QuestPath;
            if (quest.CurrentStage != UnhallowedStage.Stage4) return;
            if (NecromancyManager.Instance.ServantCount < 1) return;
            if (GetScoutsSpawned()) return;

            var pc = EClass.pc;
            if (pc == null) return;

            try
            {
                SetScoutsSpawned();

                for (int i = 0; i < ScoutCount; i++)
                {
                    var scout = CharaGen.Create(ScoutId, ScoutLevel);
                    if (scout == null) continue;
                    EClass._zone.AddCard(scout, NecroSpellUtil.GetSpawnPos());
                    scout.hostility = Hostility.Enemy;
                    scout.c_originalHostility = Hostility.Enemy;
                }

                pc.pos.PlayEffect("teleport");
                pc.pos.PlaySound("teleport");

                LangHelper.Say("scoutAppears");

                ModLog.Log("KnightEncounter: {0} scouts spawned after first servant creation", ScoutCount);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"KnightEncounter.TrySpawnScouts error: {ex.Message}");
            }
        }

        /// <summary>
        /// Called from Chara.Die postfix. Checks if all scouts are dead to drop Alvin's letter.
        /// </summary>
        public static void CheckScoutsDefeated(Chara chara)
        {
            if (chara.id != ScoutId) return;

            var quest = NecromancyManager.Instance.QuestPath;
            if (quest.ScoutsDefeated) return;
            if (!GetScoutsSpawned()) return;

            // Check if any scouts remain alive on the map
            foreach (var c in EClass._map.charas)
            {
                if (c.id == ScoutId && !c.isDead && c != chara) return;
            }

            // All scouts defeated — drama handles item drops via CWL add_item
            try
            {
                quest.MarkScoutsDefeated();
                QuestDrama.PlayDeferred("ars_scout_encounter", delay: 0.5f);
                ModLog.Log("KnightEncounter: All scouts defeated, drama triggered");
            }
            catch (Exception ex)
            {
                ModLog.Warn($"KnightEncounter.CheckScoutsDefeated error: {ex.Message}");
            }
        }

        /// <summary>
        /// Called from Zone.Activate postfix.
        /// Stage4: re-spawn scouts if player left mid-battle.
        /// </summary>
        public static void TryRespawnScouts()
        {
            if (IsPursuitPaused()) return;

            var quest = NecromancyManager.Instance.QuestPath;
            if (quest.ScoutsDefeated) return;
            if (quest.CurrentStage != UnhallowedStage.Stage4) return;
            if (!GetScoutsSpawned()) return;

            var pc = EClass.pc;
            if (pc == null) return;
            if (EClass._zone.IsRegion) return;
            if (pc.global?.transition?.state == ZoneTransition.EnterState.Encounter) return;

            // マップ上にスカウトが残っていれば再スポーン不要
            foreach (var c in EClass._map.charas)
            {
                if (c.id == ScoutId && !c.isDead) return;
            }

            // 再スポーン
            for (int i = 0; i < ScoutCount; i++)
            {
                var scout = CharaGen.Create(ScoutId, ScoutLevel);
                if (scout == null) continue;
                EClass._zone.AddCard(scout, NecroSpellUtil.GetSpawnPos());
                scout.hostility = Hostility.Enemy;
                scout.c_originalHostility = Hostility.Enemy;
            }

            pc.pos.PlayEffect("teleport");
            pc.pos.PlaySound("teleport");
            QuestDrama.TryStartUntilCompleteDeferred("ars_scout_ambush");
            ModLog.Log("KnightEncounter: Scouts re-encounter (Stage4), hostile re-spawn");
        }

        private static bool GetScoutsSpawned()
        {
            if (EClass.player?.dialogFlags == null) return false;
            return EClass.player.dialogFlags.TryGetValue(ScoutsSpawnedKey, out int val) && val == 1;
        }

        private static void SetScoutsSpawned()
        {
            if (EClass.player?.dialogFlags == null) return;
            EClass.player.dialogFlags[ScoutsSpawnedKey] = 1;
        }

        /// <summary>
        /// Called from Chara.Die postfix. Marks Karen as defeated and retreats her.
        /// Journal spawn and drama are deferred until all knights are cleared.
        /// </summary>
        public static void CheckKarenDefeated(Chara chara)
        {
            if (chara.id != KarenId) return;

            var quest = NecromancyManager.Instance.QuestPath;
            if (quest.KarenDefeated) return;

            try
            {
                quest.MarkKarenDefeated();

                // Prevent death: revive and move to somewhere
                chara.Revive();
                chara.pos.PlayEffect("teleport");
                chara.pos.PlaySound("teleport");
                chara.MoveZone("somewhere");

                ModLog.Log("KnightEncounter: Karen retreated to somewhere");

                // If all knights are already dead, trigger drama immediately
                if (AreKnightsCleared(exclude: null))
                    OnAllKnightsCleared(quest);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"KnightEncounter.CheckKarenDefeated error: {ex.Message}");
            }
        }

        /// <summary>
        /// Called from Chara.Die postfix. After Karen has retreated,
        /// checks if all temple knights are dead to trigger journal + drama.
        /// </summary>
        public static void CheckKnightsCleared(Chara chara)
        {
            if (chara.id != TempleKnightId) return;

            var quest = NecromancyManager.Instance.QuestPath;
            if (!quest.KarenDefeated) return;
            if (quest.KarenJournalSpawned) return;
            if (!AreKnightsCleared(exclude: chara)) return;

            OnAllKnightsCleared(quest);
        }

        private static bool AreKnightsCleared(Chara? exclude)
        {
            foreach (var c in EClass._map.charas)
            {
                if (c.id == TempleKnightId && !c.isDead && c != exclude) return false;
            }
            return true;
        }

        private static int CountAliveTempleKnights()
        {
            int count = 0;
            foreach (var c in EClass._map.charas)
            {
                if (c.id == TempleKnightId && !c.isDead) count++;
            }
            return count;
        }

        private static void OnAllKnightsCleared(UnhallowedPath quest)
        {
            var pc = EClass.pc;
            if (pc == null) return;

            quest.MarkKarenJournalSpawned();
            pc.pos.PlayEffect("teleport");
            pc.pos.PlaySound("book1");
            QuestDrama.PlayDeferred("ars_karen_retreat", delay: 0.5f);

            ModLog.Log("KnightEncounter: All knights cleared, drama triggered");
        }

        /// <summary>
        /// Called from Chara.Die postfix. Marks Erenos as defeated and retreats him.
        /// Drama is deferred until all guards/shades are cleared.
        /// </summary>
        public static void CheckErenosDefeated(Chara chara)
        {
            if (chara.id != ErenosShadowId) return;

            var quest = NecromancyManager.Instance.QuestPath;
            if (quest.ErenosDefeated) return;
            if (quest.CurrentStage != UnhallowedStage.Stage7) return;

            try
            {
                // Prevent death: revive and move to somewhere (flag/stage NOT set here)
                chara.Revive();
                chara.pos.PlayEffect("teleport");
                chara.pos.PlaySound("teleport");
                chara.MoveZone("somewhere");
                ModLog.Log("KnightEncounter: Erenos Shadow retreated to somewhere");

                // If all minions are already dead, complete the battle now
                if (AreErenosMinionsCleared(exclude: null))
                    OnAllErenosCleared(quest);
            }
            catch (Exception ex)
            {
                ModLog.Warn($"KnightEncounter.CheckErenosDefeated error: {ex.Message}");
            }
        }

        /// <summary>
        /// Called from Chara.Die postfix. After Erenos has retreated,
        /// checks if all guards/shades are dead to trigger drama.
        /// </summary>
        public static void CheckErenosMinionsCleared(Chara chara)
        {
            if (chara.id != ErenosGuardId && chara.id != ErenosShadeId) return;

            var quest = NecromancyManager.Instance.QuestPath;
            if (quest.ErenosDefeated) return;     // 完了済み
            if (!quest.ErenosSpawned) return;     // 戦闘未開始
            if (IsErenosOnMap()) return;          // エレノスまだ健在（退避してない）
            if (!AreErenosMinionsCleared(exclude: chara)) return;

            OnAllErenosCleared(quest);
        }

        private static bool AreErenosMinionsCleared(Chara? exclude)
        {
            foreach (var c in EClass._map.charas)
            {
                if ((c.id == ErenosGuardId || c.id == ErenosShadeId) && !c.isDead && c != exclude)
                    return false;
            }
            return true;
        }

        private static void OnAllErenosCleared(UnhallowedPath quest)
        {
            if (quest.ErenosDefeated) return;  // 二重発火防止
            quest.MarkErenosDefeated();
            quest.TryHandleErenosBattleComplete();

            var pc = EClass.pc;
            if (pc == null) return;

            pc.pos.PlayEffect("teleport");
            pc.pos.PlaySound("book1");
            QuestDrama.PlayDeferred("ars_erenos_defeat", delay: 0.5f);

            ModLog.Log("KnightEncounter: Erenos battle complete, drama triggered");
        }

        private static bool IsErenosOnMap()
        {
            foreach (var c in EClass._map.charas)
            {
                if (c.id == ErenosShadowId && !c.isDead) return true;
            }
            return false;
        }

        /// <summary>
        /// Called from Zone.Activate postfix.
        /// Stage7: re-spawn Erenos + minions if player left mid-battle.
        /// </summary>
        public static void TryRespawnErenos()
        {
            if (IsPursuitPaused()) return;

            var quest = NecromancyManager.Instance.QuestPath;
            if (quest.ErenosDefeated) return;
            if (quest.CurrentStage != UnhallowedStage.Stage7) return;
            if (!quest.ErenosSpawned) return;

            var pc = EClass.pc;
            if (pc == null) return;
            if (EClass._zone.IsRegion) return;
            if (pc.global?.transition?.state == ZoneTransition.EnterState.Encounter) return;

            if (IsErenosOnMap()) return;

            // ミニオンが残存していればスキップ（重複スポーン防止）
            if (!AreErenosMinionsCleared(exclude: null)) return;

            // 再エンカウント: フラグリセット → SpawnBoss で再設定される
            quest.ResetErenosSpawned();
            ErenosBattle.SpawnBoss();

            QuestDrama.TryStartUntilCompleteDeferred("ars_erenos_ambush");
            ModLog.Log("KnightEncounter: Erenos re-encounter (Stage7), hostile re-spawn");
        }

        /// <summary>
        /// Called after apotheosis drama completes.
        /// If the player invited Karen, register her as a North Tyris adventurer.
        /// </summary>
        public static void TrySetupKarenAsAdventurer()
        {
            if (EClass.player?.dialogFlags == null) return;
            if (!EClass.player.dialogFlags.TryGetValue(KarenStayKey, out int stay) || stay != 1)
                return;

            var karen = FindGlobalKaren();
            if (karen == null) return;

            // Already in listAdv → skip
            if (EClass.game.cards.listAdv.Contains(karen)) return;

            // Hostility: Neutral (interactable for talk/gift/recruit)
            karen.hostility = Hostility.Neutral;
            karen.c_originalHostility = Hostility.Neutral;

            // Place in a random town as adventurer
            var town = EClass.world.region.GetRandomTown();
            if (town == null)
            {
                ModLog.Warn("KnightEncounter: No town found for Karen adventurer setup");
                return;
            }

            karen.SetHomeZone(town);
            karen.MoveZone(town, ZoneTransition.EnterState.RandomVisit);
            EClass.game.cards.listAdv.Add(karen);

            ModLog.Log("KnightEncounter: Karen registered as adventurer in {0}", town.Name);
        }
    }
}
