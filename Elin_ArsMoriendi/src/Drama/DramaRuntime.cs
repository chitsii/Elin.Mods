namespace Elin_CommonDrama
{
    /// <summary>
    /// Reusable runtime facade for drama-side eval calls.
    /// Scenario scripts should call this class via DramaBuilder helpers.
    /// </summary>
    public static class DramaRuntime
    {
        private static IDramaDependencyResolver _resolver = new NullDramaDependencyResolver();

        public static void ConfigureResolver(IDramaDependencyResolver resolver)
        {
            _resolver = resolver ?? new NullDramaDependencyResolver();
        }

        public static void ResolveFlag(string key, string targetFlagKey)
        {
            if (string.IsNullOrEmpty(targetFlagKey)) return;
            if (!_resolver.TryResolveBool(key, out bool value))
            {
                Elin_ArsMoriendi.ModLog.Warn($"DramaRuntime.ResolveFlag: unresolved key '{key}'");
                return;
            }
            if (EClass.player?.dialogFlags == null)
            {
                Elin_ArsMoriendi.ModLog.Log(
                    "DramaRuntime.ResolveFlag: skipped (dialogFlags unavailable), key={0}, target={1}, value={2}",
                    key, targetFlagKey, value ? 1 : 0);
                return;
            }
            EClass.player.dialogFlags[targetFlagKey] = value ? 1 : 0;
            Elin_ArsMoriendi.ModLog.Log(
                "DramaRuntime.ResolveFlag: key={0} -> {1}={2}",
                key, targetFlagKey, value ? 1 : 0);
        }

        public static void ResolveRun(string key)
        {
            if (!_resolver.TryExecute(key))
            {
                Elin_ArsMoriendi.ModLog.Warn($"DramaRuntime.ResolveRun: unresolved key '{key}'");
                return;
            }
            Elin_ArsMoriendi.ModLog.Log("DramaRuntime.ResolveRun: executed key={0}", key);
        }

        // Backward-compatible entry points
        public static void ResolveAndSetFlag(string depKey, string targetFlagKey)
        {
            ResolveFlag(depKey, targetFlagKey);
        }

        public static void RunDependencyCommand(string depKey)
        {
            ResolveRun(depKey);
        }
    }
}
