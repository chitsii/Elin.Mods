namespace Elin_SukutsuArena.Core
{
    /// <summary>
    /// EClass.player.dialogFlags を使用する本番用ストレージ実装
    /// </summary>
    public class DialogFlagsStorage : IFlagStorage
    {
        public int GetInt(string key, int defaultValue = 0)
        {
            if (EClass.player?.dialogFlags == null) return defaultValue;
            return EClass.player.dialogFlags.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public void SetInt(string key, int value)
        {
            if (EClass.player?.dialogFlags != null)
            {
                EClass.player.dialogFlags[key] = value;
            }
        }

        public bool HasKey(string key)
        {
            if (EClass.player?.dialogFlags == null) return false;
            return EClass.player.dialogFlags.ContainsKey(key);
        }
    }
}
