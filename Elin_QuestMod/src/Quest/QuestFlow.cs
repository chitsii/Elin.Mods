using System;
using Elin_QuestMod.CommonQuest;

namespace Elin_QuestMod.Quest
{
    public static class QuestFlow
    {
        public const string IntroQuestId = "quest_intro";
        public const string FollowupQuestId = "quest_followup";
        public const string DispatchQuestIdsCsv = IntroQuestId + "," + FollowupQuestId;

        private const int MaxAutoTransitionsPerPulse = 4;

        private enum QuestPhase
        {
            Bootstrap = 0,
            Intro = 1,
            Followup = 2,
            Completed = 3,
        }

        public static string BootstrapFlagKey => QuestStateService.GetBootstrapFlagKey();
        public static string DispatchFlagKey => QuestStateService.GetDispatchFlagKey();

        private static readonly QuestStateMachine<QuestPhase> PhaseMachine =
            new QuestStateMachine<QuestPhase>(
                GetCurrentPhaseState,
                AdvanceToPhase,
                new[]
                {
                    new QuestTransitionRule<QuestPhase>(
                        QuestPhase.Bootstrap,
                        QuestPhase.Intro,
                        () => QuestStateService.IsQuestActive(IntroQuestId)
                              || QuestStateService.IsQuestCompleted(IntroQuestId)),
                    new QuestTransitionRule<QuestPhase>(
                        QuestPhase.Intro,
                        QuestPhase.Followup,
                        () => QuestStateService.IsQuestCompleted(IntroQuestId)),
                    new QuestTransitionRule<QuestPhase>(
                        QuestPhase.Followup,
                        QuestPhase.Completed,
                        () => QuestStateService.IsQuestCompleted(FollowupQuestId)),
                });

        public static void Pulse()
        {
            try
            {
                EnsureBootstrap();
                AdvancePhaseMachine();
                EnsureQuestActivationForCurrentPhase();

                int selected = QuestStateService.CheckQuestsForDispatch(DispatchFlagKey, DispatchQuestIdsCsv);
                ModLog.Info("Quest pulse selected route: " + selected + ", phase=" + QuestStateService.GetCurrentPhase());
            }
            catch (Exception ex)
            {
                ModLog.Warn("Quest pulse failed: " + ex.Message);
            }
        }

        public static void EnsureBootstrap()
        {
            if (QuestStateService.GetFlagInt(BootstrapFlagKey, 0) == 1)
            {
                return;
            }

            QuestStateService.SetCurrentPhase((int)QuestPhase.Bootstrap);
            EnsureQuestStarted(IntroQuestId);

            QuestStateService.SetFlagInt(BootstrapFlagKey, 1);
        }

        private static void AdvancePhaseMachine()
        {
            for (int i = 0; i < MaxAutoTransitionsPerPulse; i++)
            {
                bool advanced = PhaseMachine.TryAdvanceFromCurrent();
                if (!advanced)
                {
                    break;
                }
            }
        }

        private static void EnsureQuestActivationForCurrentPhase()
        {
            QuestPhase phase = GetCurrentPhaseState();
            if (phase == QuestPhase.Intro)
            {
                EnsureQuestStarted(IntroQuestId);
            }
            else if (phase == QuestPhase.Followup)
            {
                EnsureQuestStarted(FollowupQuestId);
            }
        }

        private static void EnsureQuestStarted(string questId)
        {
            if (QuestStateService.IsQuestCompleted(questId))
            {
                return;
            }

            if (QuestStateService.IsQuestActive(questId))
            {
                return;
            }

            QuestStateService.StartQuest(questId);
        }

        private static QuestPhase GetCurrentPhaseState()
        {
            int raw = QuestStateService.GetCurrentPhase();
            if (raw < (int)QuestPhase.Bootstrap || raw > (int)QuestPhase.Completed)
            {
                return QuestPhase.Bootstrap;
            }

            return (QuestPhase)raw;
        }

        private static void AdvanceToPhase(QuestPhase phase)
        {
            QuestStateService.SetCurrentPhase((int)phase);
        }
    }
}
