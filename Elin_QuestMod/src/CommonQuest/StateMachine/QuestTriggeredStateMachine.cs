using System;
using System.Collections.Generic;

namespace Elin_QuestMod.CommonQuest
{
    public sealed class QuestTriggeredTransitionRule<TState, TTrigger>
        where TState : struct, Enum
        where TTrigger : notnull
    {
        public QuestTriggeredTransitionRule(
            TState from,
            TState to,
            TTrigger trigger,
            Func<bool> canAdvance,
            Action onBlocked = null)
        {
            From = from;
            To = to;
            Trigger = trigger;
            CanAdvance = canAdvance ?? throw new ArgumentNullException(nameof(canAdvance));
            OnBlocked = onBlocked;
            OnInvalidBranch = null;

            _allowedTargets = new HashSet<TState> { to };
            AllowedTargets = new[] { to };
        }

        public QuestTriggeredTransitionRule(
            TState from,
            TTrigger trigger,
            IEnumerable<TState> allowedTargets,
            Func<TState> resolveTo,
            Func<bool> canAdvance,
            Action onBlocked = null,
            Action onInvalidBranch = null)
        {
            if (allowedTargets == null) throw new ArgumentNullException(nameof(allowedTargets));

            var targets = new List<TState>();
            var targetSet = new HashSet<TState>();
            foreach (var target in allowedTargets)
            {
                if (!targetSet.Add(target))
                    throw new ArgumentException($"Duplicate allowed target detected: {target}", nameof(allowedTargets));
                targets.Add(target);
            }

            if (targets.Count == 0)
                throw new ArgumentException("At least one allowed target is required.", nameof(allowedTargets));

            From = from;
            To = targets[0];
            Trigger = trigger;
            ResolveTo = resolveTo ?? throw new ArgumentNullException(nameof(resolveTo));
            CanAdvance = canAdvance ?? throw new ArgumentNullException(nameof(canAdvance));
            OnBlocked = onBlocked;
            OnInvalidBranch = onInvalidBranch;

            _allowedTargets = targetSet;
            AllowedTargets = targets;
        }

        public TState From { get; }
        public TState To { get; }
        public TTrigger Trigger { get; }
        public IReadOnlyList<TState> AllowedTargets { get; }
        public Func<TState> ResolveTo { get; }
        public Func<bool> CanAdvance { get; }
        public Action OnBlocked { get; }
        public Action OnInvalidBranch { get; }

        private readonly HashSet<TState> _allowedTargets;

        internal TState ResolveTarget()
        {
            return ResolveTo != null ? ResolveTo() : To;
        }

        internal bool IsAllowedTarget(TState target)
        {
            return _allowedTargets.Contains(target);
        }
    }

    public sealed class QuestTriggeredStateMachine<TState, TTrigger>
        where TState : struct, Enum
        where TTrigger : notnull
    {
        private readonly Func<TState> _getCurrentState;
        private readonly Action<TState> _advanceTo;
        private readonly Dictionary<(TState state, TTrigger trigger), QuestTriggeredTransitionRule<TState, TTrigger>> _rules;

        public QuestTriggeredStateMachine(
            Func<TState> getCurrentState,
            Action<TState> advanceTo,
            IEnumerable<QuestTriggeredTransitionRule<TState, TTrigger>> rules)
        {
            _getCurrentState = getCurrentState ?? throw new ArgumentNullException(nameof(getCurrentState));
            _advanceTo = advanceTo ?? throw new ArgumentNullException(nameof(advanceTo));
            _rules = new Dictionary<(TState state, TTrigger trigger), QuestTriggeredTransitionRule<TState, TTrigger>>();

            if (rules == null) throw new ArgumentNullException(nameof(rules));
            foreach (var rule in rules)
            {
                if (rule == null) continue;
                ValidateForwardTargets(rule);
                var key = (rule.From, rule.Trigger);
                if (_rules.ContainsKey(key))
                    throw new ArgumentException($"Duplicate transition rule detected for state={rule.From}, trigger={rule.Trigger}");
                _rules[key] = rule;
            }
        }

        public bool TryHandle(TTrigger trigger)
        {
            var before = _getCurrentState();

            if (!_rules.TryGetValue((before, trigger), out var rule))
                return false;

            if (!rule.CanAdvance())
            {
                rule.OnBlocked?.Invoke();
                return false;
            }

            var target = rule.ResolveTarget();
            if (!rule.IsAllowedTarget(target))
            {
                rule.OnInvalidBranch?.Invoke();
                return false;
            }

            _advanceTo(target);
            var after = _getCurrentState();
            bool advanced = !EqualityComparer<TState>.Default.Equals(before, after);
            return advanced;
        }

        private static void ValidateForwardTargets(QuestTriggeredTransitionRule<TState, TTrigger> rule)
        {
            var targets = rule.AllowedTargets;
            if (targets == null || targets.Count == 0)
                throw new ArgumentException($"Invalid transition rule detected: no target for from({rule.From})");

            long fromValue = Convert.ToInt64(rule.From);
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (Convert.ToInt64(target) <= fromValue)
                {
                    throw new ArgumentException(
                        $"Invalid transition rule detected: to({target}) must be greater than from({rule.From})");
                }
            }
        }
    }
}
