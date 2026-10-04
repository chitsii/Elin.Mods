using System;
using System.Collections;
using System.Collections.Concurrent;

namespace Elin.RuntimeTestPipe
{
    public sealed class CommandPump<T>
    {
        readonly ConcurrentQueue<T> queue;
        readonly Func<bool> keepRunning;
        readonly Func<T, string> dispatch;
        readonly Action<T, string> reply;
        readonly Action<Exception> onError;
        public CommandPump(ConcurrentQueue<T> queue, Func<bool> keepRunning,
            Func<T, string> dispatch, Action<T, string> reply, Action<Exception> onError)
        { this.queue = queue; this.keepRunning = keepRunning; this.dispatch = dispatch; this.reply = reply; this.onError = onError; }
        public IEnumerator Run()
        {
            while (keepRunning())
            {
                if (queue.TryDequeue(out var item))
                {
                    string result;
                    try { result = dispatch(item); }
                    catch (Exception error)
                    { onError(error); result = "(err): " + error.GetType().Name + ": " + error.Message; }
                    try { reply(item, result); }
                    catch (Exception error) { onError(error); }
                }
                yield return null;
            }
        }
    }
    public static class TestSessionGuard
    {
        public static bool Allows(string saveId, string name, bool scriptingEnabled)
        { return scriptingEnabled && saveId == "world_11" && name != null && name.IndexOf("RUNTIME_TEST", StringComparison.Ordinal) >= 0; }
    }
}
