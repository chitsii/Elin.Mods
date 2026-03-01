namespace Elin_ArsMoriendi
{
    public sealed class GameArsDramaRuntimeContext : IArsDramaRuntimeContext
    {
        private const string StartedDramaFlagPrefix = "chitsii.ars.drama.started.";
        private readonly QuestStartService _startService;

        public GameArsDramaRuntimeContext()
        {
            _startService = new QuestStartService(
                new DialogFlagStartStore(StartedDramaFlagPrefix),
                new RuntimeStartPolicy(this),
                new DramaStartExecutor());
        }

        public bool IsQuestComplete()
        {
            return NecromancyManager.Instance?.QuestPath?.IsComplete == true;
        }

        public bool IsDramaComplete(string dramaId)
        {
            if (string.IsNullOrEmpty(dramaId)) return false;
            var quest = NecromancyManager.Instance?.QuestPath;
            if (quest == null) return false;

            switch (dramaId)
            {
                case "ars_karen_ambush":
                    return quest.KarenJournalSpawned;
                case "ars_scout_ambush":
                    return quest.ScoutsDefeated;
                case "ars_erenos_ambush":
                    return quest.ErenosDefeated;
                default:
                    return false;
            }
        }

        public bool IsErenosBorrowed()
        {
            return NecromancyManager.Instance?.IsErenosBorrowed() == true;
        }

        public bool CanStartDrama(string dramaId)
        {
            if (string.IsNullOrEmpty(dramaId)) return false;
            if (EClass.player?.dialogFlags == null)
            {
                ModLog.Log("QuestBridge.CanStartDrama: false (dialogFlags unavailable), dramaId={0}", dramaId);
                return false;
            }
            string key = StartedDramaFlagPrefix + dramaId;
            bool found = EClass.player.dialogFlags.TryGetValue(key, out int started);
            bool canStart = !found || started != 1;
            ModLog.Log(
                "QuestBridge.CanStartDrama: dramaId={0}, startedFlag={1}, result={2}",
                dramaId,
                found ? started : -1,
                canStart);
            return canStart;
        }

        public bool TryStartDrama(string dramaId)
        {
            if (string.IsNullOrEmpty(dramaId)) return false;
            bool started = _startService.TryStart(dramaId);
            if (started)
                ModLog.Log("QuestBridge.TryStartDrama: started ({0})", dramaId);
            else
                ModLog.Log("QuestBridge.TryStartDrama: not started, will retry on next trigger ({0})", dramaId);
            return started;
        }

        public bool TryStartDramaRepeatable(string dramaId)
        {
            if (string.IsNullOrEmpty(dramaId)) return false;
            bool started = QuestDrama.TryPlay(dramaId);
            if (started)
                ModLog.Log("QuestBridge.TryStartDramaRepeatable: started ({0})", dramaId);
            else
                ModLog.Log("QuestBridge.TryStartDramaRepeatable: not started, will retry on next trigger ({0})", dramaId);
            return started;
        }

        public bool TryStartDramaUntilComplete(string dramaId)
        {
            if (string.IsNullOrEmpty(dramaId)) return false;
            if (IsDramaComplete(dramaId))
            {
                ModLog.Log("QuestBridge.TryStartDramaUntilComplete: skipped complete ({0})", dramaId);
                return false;
            }

            bool started = TryStartDramaRepeatable(dramaId);
            if (!started)
                ModLog.Log("QuestBridge.TryStartDramaUntilComplete: not started, will retry on next trigger ({0})", dramaId);
            return started;
        }

        public void EnsureErenosNearPlayerForDrama()
        {
            NecromancyManager.Instance?.EnsureErenosPetNearPlayerForDrama();
        }

        public void BorrowErenos()
        {
            NecromancyManager.Instance?.BorrowErenos();
        }

        public void StopBgmNow()
        {
            if (EClass.Sound == null) return;
            EClass.Sound.StopBGM();
            EClass.Sound.currentBGM = null;
        }

        public void PlayPcEffect(string effectId, string? soundId = null)
        {
            if (EClass.pc == null || string.IsNullOrEmpty(effectId)) return;
            EClass.pc.PlayEffect(effectId);
            if (!string.IsNullOrEmpty(soundId))
                EClass.pc.PlaySound(soundId);
        }

        public void PlayHecatiaPartyShow()
        {
            ModLog.Log("PlayHecatiaPartyShow invoked.");
            HecatiaPartyVFX.Play();
        }

        public void SetTalkPortrait(string portraitId)
        {
            if (string.IsNullOrEmpty(portraitId)) return;

            try
            {
                if (ELayer.ui?.TopLayer is not LayerDrama layer || layer.drama == null)
                    return;

                var dm = layer.drama;
                var person = dm.tgActor?.owner ?? dm.tg;
                if (person == null) return;

                person.idPortrait = portraitId;

                if (dm.dialog?.portrait != null)
                {
                    dm.dialog.portrait.SetPerson(person);
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn($"SetTalkPortrait failed: {ex.Message}");
            }
        }

        private sealed class DialogFlagStartStore : IQuestStartStateStore
        {
            private readonly string _prefix;

            public DialogFlagStartStore(string prefix)
            {
                _prefix = prefix;
            }

            public bool IsStarted(string startId)
            {
                if (EClass.player?.dialogFlags == null) return false;
                return EClass.player.dialogFlags.TryGetValue(_prefix + startId, out int started) && started == 1;
            }

            public void MarkStarted(string startId)
            {
                if (EClass.player?.dialogFlags == null) return;
                EClass.player.dialogFlags[_prefix + startId] = 1;
            }
        }

        private sealed class RuntimeStartPolicy : IQuestStartPolicy
        {
            private readonly GameArsDramaRuntimeContext _ctx;

            public RuntimeStartPolicy(GameArsDramaRuntimeContext ctx)
            {
                _ctx = ctx;
            }

            public bool CanStart(string startId)
            {
                return _ctx.CanStartDrama(startId);
            }
        }

        private sealed class DramaStartExecutor : IQuestStartExecutor
        {
            public bool TryStart(string startId)
            {
                return QuestDrama.TryPlay(startId);
            }
        }
    }
}
