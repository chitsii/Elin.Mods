using System.Linq;

namespace Elin_ArsMoriendi
{
    internal static class CompatBootstrap
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var results = MethodResolver.Warmup();
            int unresolved = results.Count(r => !r.IsResolved);
            int fallback = results.Count(r => r.ResolutionMode == CompatResolutionMode.Fallback);
            if (unresolved > 0)
            {
                ModLog.Warn($"Compat warmup completed with unresolved symbols: {unresolved}/{results.Count} (fallback={fallback})");
                return;
            }

            if (fallback > 0)
            {
                ModLog.Warn($"Compat warmup completed with fallback matches: {fallback}/{results.Count}");
                return;
            }

            ModLog.Log("Compat warmup completed: all symbols resolved in strict mode.");
        }
    }
}
