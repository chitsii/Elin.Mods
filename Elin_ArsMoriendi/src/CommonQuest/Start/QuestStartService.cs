using System;

namespace Elin_ArsMoriendi
{
    public interface IQuestStartStateStore
    {
        bool IsStarted(string startId);
        void MarkStarted(string startId);
    }

    public interface IQuestStartPolicy
    {
        bool CanStart(string startId);
    }

    public interface IQuestStartExecutor
    {
        bool TryStart(string startId);
    }

    /// <summary>
    /// Idempotent start gate for quest/drama start commands.
    /// </summary>
    public sealed class QuestStartService
    {
        private readonly IQuestStartStateStore _stateStore;
        private readonly IQuestStartPolicy _policy;
        private readonly IQuestStartExecutor _executor;

        public QuestStartService(
            IQuestStartStateStore stateStore,
            IQuestStartPolicy policy,
            IQuestStartExecutor executor)
        {
            _stateStore = stateStore;
            _policy = policy;
            _executor = executor;
        }

        public bool TryStart(string startId)
        {
            if (string.IsNullOrWhiteSpace(startId)) return false;
            if (_stateStore.IsStarted(startId)) return false;
            if (!_policy.CanStart(startId)) return false;

            try
            {
                bool started = _executor.TryStart(startId);
                if (!started) return false;
                _stateStore.MarkStarted(startId);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
