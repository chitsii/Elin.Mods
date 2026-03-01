/// <summary>
/// Mod-wide logging helpers.
/// Regular logs are compiled only in DEBUG builds.
/// </summary>
public static class ModLog
{
    [System.Diagnostics.Conditional("DEBUG")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    public static void LogFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(format, args);
    }
}
