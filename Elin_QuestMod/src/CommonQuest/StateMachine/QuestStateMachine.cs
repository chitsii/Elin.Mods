using System;
using System.Collections.Generic;

namespace Elin_QuestMod.CommonQuest
{
    public sealed class QuestTransitionRule<TState> where TState : struct, Enum
    {
        public QuestTransitionRule(
            TState from,
            TState to,
            Func<bool> canAdvance,
            Action onBlocked = null)
        {
            From = from;
            To = to;
            CanAdvance = canAdvance ?? throw new ArgumentNullException(nameof(canAdvance));
            OnBlocked = onBlocked;
        }

        public TState From { get; }
        public TState To { get; }
        public Func<bool> CanAdvance { get; }
        public Action OnBlocked { get; }
    }

    public sealed class QuestStateMachine<TState> where TState : struct, Enum
    {
        private readonly Func<TState> _getCurrentState;
        private readonly Action<TState> _advanceTo;
        private readonly Dictionary<TState, QuestTransitionRule<TState>> _rulesByFrom;

        public QuestStateMachine(
            Func<TState> getCurrentState,
            Action<TState> advanceTo,
            IEnumerable<QuestTransitionRule<TState>> rules)
        {
            _getCurrentState = getCurrentState ?? throw new ArgumentNullException(nameof(getCurrentState));
            _advanceTo = advanceTo ?? throw new ArgumentNullException(nameof(advanceTo));
            _rulesByFrom = new Dictionary<TState, QuestTransitionRule<TState>>();

            if (rules == null) throw new ArgumentNullException(nameof(rules));
            foreach (var rule in rules)
            {
                if (rule == null) continue;
                if (Convert.ToInt64(rule.To) <= Convert.ToInt64(rule.From))
                {
                    throw new ArgumentException(
                        $"Invalid transition rule detected: to({rule.To}) must be greater than from({rule.From})");
                }

                if (_rulesByFrom.ContainsKey(rule.From))
                {
                    throw new ArgumentException(
                        $"Duplicate transition rule detected for state={rule.From}");
                }

                _rulesByFrom[rule.From] = rule;
            }
        }

        public bool TryAdvanceFromCurrent()
        {
            var before = _getCurrentState();
            if (!_rulesByFrom.TryGetValue(before, out var rule))
                return false;

            if (!rule.CanAdvance())
            {
                rule.OnBlocked?.Invoke();
                return false;
            }

            _advanceTo(rule.To);
            var after = _getCurrentState();
            bool advanced = !EqualityComparer<TState>.Default.Equals(before, after);
            return advanced;
        }
    }
}
