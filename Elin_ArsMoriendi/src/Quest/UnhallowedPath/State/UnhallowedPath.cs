using System;

namespace Elin_ArsMoriendi
{
    public enum UnhallowedStage
    {
        NotStarted = 0,
        Stage1 = 1,   // Blood-Stained Prologue
        Stage2 = 2,   // Indelible Ink
        Stage3 = 3,   // Ashen Sentinel
        Stage4 = 4,   // Ashen Records
        Stage5 = 5,   // Stigmata
        Stage6 = 6,   // The Seventh Sign
        Stage7 = 7,   // Karen's Shadow
        Stage8 = 8,   // Shadow of the Predecessor
        Stage9 = 9,   // Unhallowed Awakening
        Stage10 = 10, // Successor's Notes (complete)
    }

    /// <summary>
    /// Quest state machine for the Unhallowed Path (main quest line).
    /// Uses EClass.player.dialogFlags for per-save persistence.
    /// </summary>
    public class UnhallowedPath
    {
        // ── Quest ID (matches SourceQuest) ──
        private const string QuestId = "ars_moriendi";

        // ── dialogFlags keys ──
        private const string StageKey = "chitsii.ars.quest.stage";
        private const string KarenDefeatedKey = "chitsii.ars.quest.event.karen_defeated";
        private const string ErenosDefeatedKey = "chitsii.ars.quest.event.erenos_defeated";
        private const string KnightsSpawnedKey = "chitsii.ars.quest.event.knights_spawned";
        private const string ErenosSpawnedKey = "chitsii.ars.quest.event.erenos_spawned";
        private const string ScoutsDefeatedKey = "chitsii.ars.quest.event.scouts_defeated";
        private const string ApotheosisKey = "chitsii.ars.quest.state.apotheosis";
        private const string KarenJournalSpawnedKey = "chitsii.ars.quest.event.karen_journal_spawned";
        private const string S2BuffAppliedKey = "chitsii.ars.quest.state.s2_buff_applied";
        private const string AuthorChangedKey = "chitsii.ars.quest.state.author_changed";
        private const string LastAdvanceRawKey = "chitsii.ars.quest.last_advance_raw";
        private const string TriggerFirstSoulDrop = "trigger.first_soul_drop";
        private const string TriggerTomeOpen = "trigger.tome_open";
        private const string TriggerKarenEncounterHostile = "trigger.karen_encounter.hostile";
        private const string TriggerErenosBattleComplete = "trigger.enemy_defeated.erenos";
        private const string TriggerApotheosisApply = "trigger.apotheosis_apply";
        private readonly QuestTriggeredStateMachine<UnhallowedStage, string> _stateMachine;

        /// <summary>Fired when the quest stage changes.</summary>
        public event Action<UnhallowedStage>? OnStageChanged;

        public UnhallowedPath()
        {
            _stateMachine = new QuestTriggeredStateMachine<UnhallowedStage, string>(
                () => CurrentStage,
                stage => AdvanceTo(stage),
                BuildTransitionRules());
        }

        // ── dialogFlags helpers ──

        private static int GetFlag(string key)
        {
            return DialogFlagStore.GetInt(EClass.player?.dialogFlags, key);
        }

        private static void SetFlag(string key, int value)
        {
            DialogFlagStore.SetInt(EClass.player?.dialogFlags, key, value);
        }

        // ── Stage access ──

        public UnhallowedStage CurrentStage => (UnhallowedStage)GetFlag(StageKey);

        public bool IsStarted => CurrentStage > UnhallowedStage.NotStarted;
        public bool IsComplete => CurrentStage >= UnhallowedStage.Stage10;

        // ── Event flags ──

        public bool KarenDefeated => GetFlag(KarenDefeatedKey) == 1;
        public bool ErenosDefeated => GetFlag(ErenosDefeatedKey) == 1;
        public bool KnightsSpawned => GetFlag(KnightsSpawnedKey) == 1;
        public bool ErenosSpawned => GetFlag(ErenosSpawnedKey) == 1;
        public bool ScoutsDefeated => GetFlag(ScoutsDefeatedKey) == 1;
        public bool ApotheosisApplied => GetFlag(ApotheosisKey) == 1;
        public bool S2BuffApplied => GetFlag(S2BuffAppliedKey) == 1;
        public bool KarenJournalSpawned => GetFlag(KarenJournalSpawnedKey) == 1;
        public bool AuthorChanged => GetFlag(AuthorChangedKey) == 1;

        public void MarkKarenDefeated() => SetFlag(KarenDefeatedKey, 1);
        public void MarkErenosDefeated() => SetFlag(ErenosDefeatedKey, 1);
        public void MarkKnightsSpawned() => SetFlag(KnightsSpawnedKey, 1);
        public void ResetKarenDefeated() => SetFlag(KarenDefeatedKey, 0);
        public void ResetKnightsSpawned() => SetFlag(KnightsSpawnedKey, 0);
        public void MarkErenosSpawned() => SetFlag(ErenosSpawnedKey, 1);
        public void ResetErenosSpawned() => SetFlag(ErenosSpawnedKey, 0);
        public void MarkScoutsDefeated() => SetFlag(ScoutsDefeatedKey, 1);
        public void MarkKarenJournalSpawned() => SetFlag(KarenJournalSpawnedKey, 1);

        // ── Debug ──

        [System.Diagnostics.Conditional("DEBUG")]
        public void DebugSetStage(UnhallowedStage stage)
        {
            SetFlag(StageKey, (int)stage);
            SetFlag(S2BuffAppliedKey, 0);
            ModLog.Log("DEBUG: stage forced to {0}, S2Buff reset", stage);
            Msg.Say($"DEBUG: Quest stage set to {stage}");
        }

        // ── Day cooldown ──

        /// <summary>
        /// Returns true if less than the configured cooldown days have passed
        /// since the last stage transition. Skipped in debug mode.
        /// </summary>
        public bool ShouldWaitBeforeAdvance(int cooldownDays = 1)
        {
            if (ModConfig.DebugMode.Value)
            {
                ModLog.Log("ShouldWait: skipped (DebugMode=true)");
                return false;
            }

            int lastRaw = GetFlag(LastAdvanceRawKey);
            int currentRaw = EClass.world.date.GetRaw();
            int elapsed = currentRaw - lastRaw;
            int threshold = QuestCooldownPolicy.DaysToRawMinutes(cooldownDays);

            ModLog.Log("ShouldWait: lastRaw={0}, currentRaw={1}, elapsed={2}, threshold={3}, days={4}",
                lastRaw, currentRaw, elapsed, threshold, cooldownDays);

            return QuestCooldownPolicy.ShouldWaitBeforeAdvanceDays(lastRaw, currentRaw, cooldownDays);
        }

        // ── Stage transitions ──

        public void AdvanceTo(UnhallowedStage stage)
        {
            var current = CurrentStage;
            if (stage <= current) return;

            SetFlag(StageKey, (int)stage);
            SetFlag(LastAdvanceRawKey, EClass.world.date.GetRaw());
            KnightEncounter.SetPursuitPaused(false);
            ModLog.Log("UnhallowedPath: advanced {0} -> {1}", current, stage);

            SyncQuestPhase(stage);

            // Stage-specific side effects
            switch (stage)
            {
                case UnhallowedStage.Stage1:
                    QuestDrama.PlayDeferred("ars_first_soul", onComplete: () =>
                    {
                        TomePrompt.ShowDialog("questChapterFourAppeared");
                    });
                    break;

                case UnhallowedStage.Stage2:
                    KnightEncounter.EnsureKarenExists();
                    LangHelper.Say("questStage2");
                    ApplyStage2HiddenBuff();
                    QuestDrama.PlayDeferred("ars_tome_awakening", onComplete: () =>
                    {
                        TomePrompt.ShowDialog("questDramaComplete");
                    });
                    break;

                case UnhallowedStage.Stage4:
                    KnightEncounter.EnsureErenosExists();
                    LangHelper.Say("questStage4");
                    QuestDrama.PlayDeferred("ars_cinder_records", onComplete: () =>
                    {
                        TomePrompt.ShowDialog("questDramaComplete");
                        KnightEncounter.TrySpawnScouts();
                    });
                    break;

                case UnhallowedStage.Stage5:
                    LangHelper.Say("questStage5");
                    QuestDrama.PlayDeferred("ars_stigmata", onComplete: () =>
                    {
                        TomePrompt.ShowDialog("questDramaComplete");
                    });
                    break;

                case UnhallowedStage.Stage6:
                    QuestDrama.TryStartDeferred("ars_seventh_sign");
                    break;

                case UnhallowedStage.Stage7:
                    QuestDrama.TryStartDeferred("ars_karen_shadow");
                    break;

                case UnhallowedStage.Stage9:
                    // Drama is played from ApplyApotheosis()
                    break;

                case UnhallowedStage.Stage10:
                    break;
            }

            OnStageChanged?.Invoke(stage);
        }

        /// <summary>
        /// Called when a soul drops for the first time. Triggers Stage 1.
        /// </summary>
        public void TryAdvanceOnFirstSoulDrop()
        {
            bool advanced = _stateMachine.TryHandle(TriggerFirstSoulDrop);
            ModLog.Log("TryAdvanceOnFirstSoulDrop: advanced={0}, CurrentStage={1}", advanced, CurrentStage);
        }

        /// <summary>
        /// Called every time the tome is opened. Handles stage transitions via tome interaction.
        /// Returns true if a drama cutscene was triggered (caller should skip GUI).
        /// </summary>
        public bool TryAdvanceOnTomeOpen()
        {
            var before = CurrentStage;
            ModLog.Log("TryAdvanceOnTomeOpen: CurrentStage={0}", before);
            bool advanced = _stateMachine.TryHandle(TriggerTomeOpen);
            bool blocksGui = advanced && (
                before == UnhallowedStage.Stage1 ||
                before == UnhallowedStage.Stage3 ||
                before == UnhallowedStage.Stage4 ||
                before == UnhallowedStage.Stage5 ||
                before == UnhallowedStage.Stage6);
            ModLog.Log("TryAdvanceOnTomeOpen: advanced={0}, blocksGui={1}, CurrentStage={2}",
                advanced, blocksGui, CurrentStage);
            return blocksGui;
        }

        private QuestTriggeredTransitionRule<UnhallowedStage, string>[] BuildTransitionRules()
        {
            return new[]
            {
                new QuestTriggeredTransitionRule<UnhallowedStage, string>(
                    UnhallowedStage.NotStarted,
                    UnhallowedStage.Stage1,
                    TriggerFirstSoulDrop,
                    () => QuestProgressConditions.IsCurrentStage(this, UnhallowedStage.NotStarted)),
                new QuestTriggeredTransitionRule<UnhallowedStage, string>(
                    UnhallowedStage.Stage1,
                    UnhallowedStage.Stage2,
                    TriggerTomeOpen,
                    () => QuestProgressConditions.IsCooldownElapsed(this)),
                new QuestTriggeredTransitionRule<UnhallowedStage, string>(
                    UnhallowedStage.Stage2,
                    UnhallowedStage.Stage3,
                    TriggerKarenEncounterHostile,
                    QuestCondition.All(
                        () => QuestProgressConditions.IsCurrentStage(this, UnhallowedStage.Stage2),
                        () => QuestProgressConditions.HasKnightsSpawned(this))),
                new QuestTriggeredTransitionRule<UnhallowedStage, string>(
                    UnhallowedStage.Stage3,
                    UnhallowedStage.Stage4,
                    TriggerTomeOpen,
                    QuestCondition.All(
                        () => QuestProgressConditions.HasKarenJournal(this),
                        () => QuestProgressConditions.IsCooldownElapsed(this)),
                    onBlocked: NotifyMissingKarenJournalOnStage3TomeOpen),
                new QuestTriggeredTransitionRule<UnhallowedStage, string>(
                    UnhallowedStage.Stage4,
                    UnhallowedStage.Stage5,
                    TriggerTomeOpen,
                    QuestCondition.All(
                        () => QuestProgressConditions.HasMinimumServants(3),
                        () => QuestProgressConditions.IsCooldownElapsed(this)),
                    onBlocked: NotifyMissingServantsForStage5),
                new QuestTriggeredTransitionRule<UnhallowedStage, string>(
                    UnhallowedStage.Stage5,
                    UnhallowedStage.Stage6,
                    TriggerTomeOpen,
                    () => QuestProgressConditions.IsCooldownElapsed(this)),
                new QuestTriggeredTransitionRule<UnhallowedStage, string>(
                    UnhallowedStage.Stage6,
                    UnhallowedStage.Stage7,
                    TriggerTomeOpen,
                    QuestCondition.All(
                        () => QuestProgressConditions.HasMinimumServants(5),
                        () => QuestProgressConditions.IsCooldownElapsed(this))),
                new QuestTriggeredTransitionRule<UnhallowedStage, string>(
                    UnhallowedStage.Stage7,
                    UnhallowedStage.Stage8,
                    TriggerErenosBattleComplete,
                    () => QuestProgressConditions.HasErenosDefeated(this)),
                new QuestTriggeredTransitionRule<UnhallowedStage, string>(
                    UnhallowedStage.Stage8,
                    UnhallowedStage.Stage9,
                    TriggerApotheosisApply,
                    () => QuestProgressConditions.HasApotheosisApplied(this)),
                new QuestTriggeredTransitionRule<UnhallowedStage, string>(
                    UnhallowedStage.Stage9,
                    UnhallowedStage.Stage10,
                    TriggerTomeOpen,
                    () => QuestProgressConditions.IsCooldownElapsed(this)),
            };
        }

        private void NotifyMissingKarenJournalOnStage3TomeOpen()
        {
            if (!QuestProgressConditions.HasKarenJournal(this))
                LangHelper.Say("questNeedJournal");
        }

        private static void NotifyMissingServantsForStage5()
        {
            if (!QuestProgressConditions.HasMinimumServants(3))
                LangHelper.Say("questNeedServant");
        }

        public bool TryHandleErenosBattleComplete()
        {
            bool advanced = _stateMachine.TryHandle(TriggerErenosBattleComplete);
            ModLog.Log("TryHandleErenosBattleComplete: advanced={0}, CurrentStage={1}", advanced, CurrentStage);
            return advanced;
        }

        public bool TryHandleKarenEncounterHostile()
        {
            bool advanced = _stateMachine.TryHandle(TriggerKarenEncounterHostile);
            ModLog.Log("TryHandleKarenEncounterHostile: advanced={0}, CurrentStage={1}", advanced, CurrentStage);
            return advanced;
        }

        public bool TryHandleApotheosisApply()
        {
            bool advanced = _stateMachine.TryHandle(TriggerApotheosisApply);
            ModLog.Log("TryHandleApotheosisApply: advanced={0}, CurrentStage={1}", advanced, CurrentStage);
            return advanced;
        }

        // ── Stage 2: Hidden buff ──

        private void ApplyStage2HiddenBuff()
        {
            if (S2BuffApplied) return;

            var pc = EClass.pc;
            if (pc?.elements == null) return;

            pc.elements.ModBase(76, 5);  // MAG +5
            pc.elements.ModBase(75, 3);  // WIL +3
            SetFlag(S2BuffAppliedKey, 1);

            ModLog.Log("UnhallowedPath: Stage2 hidden buff applied (MAG+5, WIL+3)");
        }

        // ── Stage 9: Apotheosis ──

        /// <summary>
        /// Perform the apotheosis ritual. Requires 5 types of materials.
        /// Material validation and consumption are handled by the caller (GUI).
        /// </summary>
        public bool ApplyApotheosis()
        {
            if (ApotheosisApplied) return false;
            if (CurrentStage < UnhallowedStage.Stage8) return false;

            var pc = EClass.pc;
            var mgr = NecromancyManager.Instance;
            if (pc == null) return false;

            // Validate and consume all 5 ritual materials
            if (!mgr.HasAllRitualMaterials())
            {
                LangHelper.Say("apotheosisNoMaterials");
                return false;
            }
            mgr.ConsumeRitualMaterials();

            // Grant apotheosis feat (one-time, no re-grant logic)
            if (ApotheosisFeatBonus.TryGetSelectedFeatId(out int featId))
            {
                pc.SetFeat(featId, 1, msg: true);
            }
            else
            {
                ModLog.Error("UnhallowedPath: Failed to resolve selected apotheosis feat.");
            }

            // Mark apotheosis complete
            SetFlag(ApotheosisKey, 1);

            // Change tome author name
            if (!AuthorChanged)
            {
                SetFlag(AuthorChangedKey, 1);
            }

            // VFX is handled by drama script (ars_apotheosis) at appropriate phases

            // Advance to Unhallowed Awakening stage via state-machine trigger
            TryHandleApotheosisApply();

            // Play apotheosis drama after effects
            QuestDrama.PlayDeferred("ars_apotheosis", delay: 0.5f, onComplete: () =>
            {
                KnightEncounter.TrySetupKarenAsAdventurer();
            });

            ModLog.Log("UnhallowedPath: Apotheosis applied. Feat granted (stat bonuses are now feat-driven).");
            return true;
        }

        /// <summary>
        /// Get the display name for the tome author.
        /// After Erenos shadow battle (Stage8+), shows the player's name instead of Erenos.
        /// </summary>
        public string GetTomeAuthor()
        {
            if (CurrentStage >= UnhallowedStage.Stage8 && EClass.pc != null)
                return EClass.pc.Name;
            return Lang.langCode == "CN" ? "艾雷诺斯" : Lang.isJP ? "エレノス" : "Erenos";
        }

        // ── Journal Quest Sync ──

        private void SyncQuestPhase(UnhallowedStage stage)
        {
            int phase = (int)stage;
            var quests = EClass.game?.quests;
            if (quests == null) return;

            var existing = quests.list.Find(q => q.id == QuestId);
            if (existing != null)
            {
                existing.ChangePhase(phase);
                return;
            }

            if (phase >= 1)
            {
                var quest = SafeInvoke.TryCreateQuest(QuestId);
                if (quest == null)
                {
                    ModLog.Warn($"SyncQuestPhase: failed to create quest {QuestId}.");
                    return;
                }
                quest.phase = phase;
                quests.Start(quest);
            }
        }

        /// <summary>
        /// Ensure the journal quest exists for saves that predate the quest feature.
        /// Called from NecromancyManager.EnsureGameStateLoaded().
        /// </summary>
        public void EnsureQuestExists()
        {
            var stage = CurrentStage;
            if (stage <= UnhallowedStage.NotStarted) return;

            var quests = EClass.game?.quests;
            if (quests == null) return;

            var existing = quests.list.Find(q => q.id == QuestId);
            if (existing != null) return;

            var quest = SafeInvoke.TryCreateQuest(QuestId);
            if (quest == null)
            {
                ModLog.Warn($"EnsureQuestExists: failed to create quest {QuestId}.");
                return;
            }
            quest.phase = (int)stage;
            quests.Start(quest);
            ModLog.Log("EnsureQuestExists: created quest at phase {0}", (int)stage);
        }
    }
}
