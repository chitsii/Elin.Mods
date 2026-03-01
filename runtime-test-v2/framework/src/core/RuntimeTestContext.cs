using System;
using System.Collections.Generic;

// Runtime Test V2 isolated context per case.
public sealed class RuntimeTestContext
{
    private sealed class RollbackEntry
    {
        public string Name;
        public Action Action;
    }

    private const int MaxLogs = 64;
    private readonly Dictionary<string, object> _bag = new Dictionary<string, object>(StringComparer.Ordinal);
    private readonly List<string> _logs = new List<string>();
    private readonly List<RollbackEntry> _rollback = new List<RollbackEntry>();

    public RuntimeTestContext(string caseId, string modRoot)
    {
        CaseId = caseId ?? string.Empty;
        ModRoot = modRoot ?? string.Empty;
    }

    public string CaseId { get; private set; }

    public string ModRoot { get; private set; }

    public IReadOnlyList<string> Logs => _logs;

    public void Set(string key, object value)
    {
        _bag[key] = value;
    }

    public T Get<T>(string key)
    {
        if (!_bag.TryGetValue(key, out var value))
            throw new InvalidOperationException("Missing context key: " + key);
        return (T)value;
    }

    public T GetOrDefault<T>(string key) where T : class
    {
        if (!_bag.TryGetValue(key, out var value))
            return null;
        return value as T;
    }

    public void Log(string message)
    {
        if (_logs.Count >= MaxLogs)
            return;
        _logs.Add(message ?? string.Empty);
    }

    /// <summary>
    /// Register a rollback action that is always executed in host finally, reverse order.
    /// Use this for any state mutation done in prepare/execute.
    /// </summary>
    public void RegisterRollback(string name, Action rollbackAction)
    {
        if (rollbackAction == null)
            throw new InvalidOperationException("rollbackAction is null.");

        _rollback.Add(new RollbackEntry
        {
            Name = name ?? "rollback",
            Action = rollbackAction
        });
    }

    public IReadOnlyList<string> RunRollback()
    {
        var errors = new List<string>();

        for (int i = _rollback.Count - 1; i >= 0; i--)
        {
            var entry = _rollback[i];
            try
            {
                entry.Action();
                Log("rollback:ok:" + entry.Name);
            }
            catch (Exception ex)
            {
                string message = "rollback:failed:" + entry.Name + ":" + ex.GetType().Name + ": " + ex.Message;
                Log(message);
                errors.Add(message);
            }
        }

        _rollback.Clear();
        return errors;
    }

    /// <summary>
    /// Set dialog flag and auto-restore previous value (or delete key if missing before).
    /// </summary>
    public void SetDialogFlagWithRollback(string key, int value)
    {
        var flags = EClass.player?.dialogFlags;
        if (flags == null)
            throw new InvalidOperationException("dialogFlags unavailable.");

        bool hadBefore = flags.TryGetValue(key, out int previous);

        RegisterRollback("dialogFlag:" + key, () =>
        {
            var current = EClass.player?.dialogFlags;
            if (current == null)
                return;

            if (hadBefore)
                current[key] = previous;
            else
                current.Remove(key);
        });

        flags[key] = value;
        Log("dialogFlag:set:" + key + "=" + value);
    }

    /// <summary>
    /// Spawn a temporary chara near PC and auto-destroy it on rollback.
    /// </summary>
    public Chara SpawnCharaWithRollback(string charaId, int level = -1)
    {
        if (string.IsNullOrEmpty(charaId))
            throw new InvalidOperationException("charaId is empty.");
        if (EClass.pc == null || EClass._zone == null)
            throw new InvalidOperationException("pc/zone unavailable.");
        if (EClass.pc.pos == null)
            throw new InvalidOperationException("pc position unavailable.");

        Chara chara = level > 0 ? CharaGen.Create(charaId, level) : CharaGen.Create(charaId);
        if (chara == null)
            throw new InvalidOperationException("CharaGen.Create returned null: " + charaId);

        var spawnPoint = EClass.pc.pos.GetNearestPoint(allowBlock: false, allowChara: false);
        EClass._zone.AddCard(chara, spawnPoint);

        RegisterRollback("spawnChara:" + charaId + ":" + chara.uid, () =>
        {
            if (!chara.isDestroyed)
                chara.Destroy();
        });

        Log("spawnChara:" + charaId + ":" + chara.uid);
        return chara;
    }
}
