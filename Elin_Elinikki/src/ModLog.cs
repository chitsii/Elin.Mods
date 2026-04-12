namespace Elin_Elinikki
{
    internal static class ModLog
    {
        public static bool IsDeveloperEnabled => Plugin.Settings?.EnableDeveloperLogs?.Value == true;

        public static void Developer(string category, string message)
        {
            if (!IsDeveloperEnabled || Plugin.Log == null)
            {
                return;
            }

            Plugin.Log.LogInfo($"[DEV:{category}] {message}");
        }
    }
}
