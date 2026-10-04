using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elin_NiComment
{
    internal sealed class LifecycleEventGate
    {
        private readonly HashSet<int> _activeKeys = new HashSet<int>();

        internal State Begin(object instance, bool alreadyFinalized)
        {
            if (instance == null || alreadyFinalized) return State.Suppressed;

            int key = RuntimeHelpers.GetHashCode(instance);
            return _activeKeys.Add(key) ? new State(key) : State.Suppressed;
        }

        internal bool Finish(State state, bool isFinalized)
        {
            if (!state.IsActive) return false;

            bool wasActive = _activeKeys.Remove(state.Key);
            return wasActive && isFinalized;
        }

        internal void Abort(State state)
        {
            if (state.IsActive) _activeKeys.Remove(state.Key);
        }

        internal struct State
        {
            internal static State Suppressed => default(State);

            internal State(int key)
            {
                Key = key;
                IsActive = true;
            }

            internal int Key { get; }
            internal bool IsActive { get; }
        }
    }
}
