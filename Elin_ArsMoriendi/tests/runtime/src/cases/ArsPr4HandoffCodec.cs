// Managed-only handoff data and serializer, shared verbatim with offline tests.
[System.Serializable]
public sealed partial class ArsPr4Baseline
{
    public int[] tracked;
    public int[] party;
    public int[] home;
    public string[] keys;
    public int[] values;
}

[System.Serializable]
public sealed partial class ArsPr4Handoff
{
    public int ownershipVersion;
    public string token;
    public string pcName;
    public int masterUid;
    public int zoneUid;
    public int gameIdentity;
    public int[] uids;
    public string[] names;
    public int[] durations;
    public int[] ownerUids;
    public ArsPr4Baseline baseline;
}

public static class ArsPr4HandoffCodec
{
    public const int CurrentVersion = 3;
    private const string Prefix = "RUNTIME_TEST_PR4_ARS_";

    private static System.InvalidOperationException Invalid(string detail)
    {
        return new System.InvalidOperationException("Invalid PR4 handoff: " + detail
            + ". No baseline reconstructed; archive evidence and rerun prepare on a restored dedicated save.");
    }

    private static Newtonsoft.Json.JsonSerializer Serializer()
    {
        // Create, not CreateDefault: other mods' JsonConvert.DefaultSettings cannot change this contract.
        return Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings
        {
            MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Error,
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None
        });
    }

    public static string Serialize(ArsPr4Handoff data)
    {
        Validate(data);
        using (var text = new System.IO.StringWriter(System.Globalization.CultureInfo.InvariantCulture))
        using (var writer = new Newtonsoft.Json.JsonTextWriter(text))
        {
            writer.Formatting = Newtonsoft.Json.Formatting.Indented;
            Serializer().Serialize(writer, data);
            return text.ToString();
        }
    }

    public static ArsPr4Handoff Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw Invalid("empty JSON");
        try
        {
            // Do not let duplicate properties silently replace ownership or baseline evidence.
            var objects = new System.Collections.Generic.Stack<System.Collections.Generic.HashSet<string>>();
            using (var reader = new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(json)))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == Newtonsoft.Json.JsonToken.Comment) throw Invalid("JSON comments forbidden");
                    if (reader.TokenType == Newtonsoft.Json.JsonToken.StartObject)
                        objects.Push(new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal));
                    else if (reader.TokenType == Newtonsoft.Json.JsonToken.EndObject) objects.Pop();
                    else if (reader.TokenType == Newtonsoft.Json.JsonToken.PropertyName
                        && !objects.Peek().Add((string)reader.Value)) throw Invalid("duplicate field " + reader.Value);
                }
            }
            var root = Newtonsoft.Json.Linq.JObject.Parse(json);
            RequireFields(root, new[] { "ownershipVersion", "token", "pcName", "masterUid", "zoneUid", "gameIdentity",
                "uids", "names", "durations", "baseline", "ownerUids" }, "root");
            foreach (var key in new[] { "ownershipVersion", "masterUid", "zoneUid", "gameIdentity" })
                RequireType(root[key], Newtonsoft.Json.Linq.JTokenType.Integer, key);
            foreach (var key in new[] { "token", "pcName" })
                RequireType(root[key], Newtonsoft.Json.Linq.JTokenType.String, key);
            foreach (var key in new[] { "uids", "durations", "ownerUids" })
                RequireArray(root[key], Newtonsoft.Json.Linq.JTokenType.Integer, key);
            RequireArray(root["names"], Newtonsoft.Json.Linq.JTokenType.String, "names");
            RequireType(root["baseline"], Newtonsoft.Json.Linq.JTokenType.Object, "baseline");
            var baseline = (Newtonsoft.Json.Linq.JObject)root["baseline"];
            RequireFields(baseline, new[] { "tracked", "party", "home", "keys", "values" }, "baseline");
            foreach (var key in new[] { "tracked", "party", "home", "values" })
                RequireArray(baseline[key], Newtonsoft.Json.Linq.JTokenType.Integer, "baseline." + key);
            RequireArray(baseline["keys"], Newtonsoft.Json.Linq.JTokenType.String, "baseline.keys");
            var data = root.ToObject<ArsPr4Handoff>(Serializer());
            Validate(data);
            return data;
        }
        catch (Newtonsoft.Json.JsonException ex) { throw Invalid("JSON schema/parse error: " + ex.Message); }
    }

    public static void WriteNew(string path, ArsPr4Handoff data)
    {
        if (System.IO.File.Exists(path)) throw Invalid("handoff already exists");
        string temp = path + ".tmp";
        if (System.IO.File.Exists(temp)) throw Invalid("temporary handoff already exists");
        string json = Serialize(data);
        // Detect serializer omissions through the exact read path before publishing a ready file.
        System.IO.File.WriteAllText(temp, json);
        try { Read(temp); System.IO.File.Move(temp, path); }
        catch { System.IO.File.Delete(temp); throw; }
    }

    public static ArsPr4Handoff Read(string path) { return Deserialize(System.IO.File.ReadAllText(path)); }

    private static void RequireFields(Newtonsoft.Json.Linq.JObject obj, string[] expected, string label)
    {
        foreach (string key in expected)
            if (obj.Property(key) == null) throw Invalid("missing " + label + "." + key);
        foreach (var property in obj.Properties())
            if (System.Array.IndexOf(expected, property.Name) < 0) throw Invalid("unknown " + label + "." + property.Name);
    }

    private static void RequireType(Newtonsoft.Json.Linq.JToken token, Newtonsoft.Json.Linq.JTokenType type, string label)
    {
        if (token == null || token.Type != type) throw Invalid("wrong/null type for " + label);
    }

    private static void RequireArray(Newtonsoft.Json.Linq.JToken token, Newtonsoft.Json.Linq.JTokenType type, string label)
    {
        RequireType(token, Newtonsoft.Json.Linq.JTokenType.Array, label);
        foreach (var item in token) RequireType(item, type, label + "[]");
    }

    public static void Validate(ArsPr4Handoff data)
    {
        if (data == null || data.ownershipVersion != CurrentVersion) throw Invalid("unsupported ownershipVersion");
        System.Guid token;
        if (!System.Guid.TryParseExact(data.token, "N", out token) || token.ToString("N") != data.token)
            throw Invalid("invalid token");
        if (string.IsNullOrEmpty(data.pcName) || data.pcName.IndexOf("RUNTIME_TEST", System.StringComparison.Ordinal) < 0
            || data.masterUid <= 0 || data.zoneUid <= 0 || data.gameIdentity == 0) throw Invalid("missing PC/zone/Game identity");
        if (data.uids == null || data.uids.Length != 6 || data.names == null || data.names.Length != 6
            || data.durations == null || data.durations.Length != 6 || data.ownerUids == null || data.ownerUids.Length != 6)
            throw Invalid("fixture array length");
        var roles = new[] { "skeleton", "undead", "ordinary", "enemy_summon", "enemy_owner", "permanent" };
        var ids = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < 6; i++)
        {
            if (data.uids[i] <= 0 || data.uids[i] == data.masterUid || !ids.Add(data.uids[i])) throw Invalid("invalid/duplicate fixture UID");
            if (data.names[i] != Prefix + data.token + "_" + roles[i]) throw Invalid("fixture name/token/role mismatch");
            if (i < 4 ? data.durations[i] <= 0 : data.durations[i] != 0) throw Invalid("fixture lifetime");
            int owner = i == 3 ? data.uids[4] : i == 4 ? 0 : data.masterUid;
            if (data.ownerUids[i] != owner) throw Invalid("fixture owner mismatch");
        }
        var baseline = data.baseline;
        if (baseline == null || baseline.tracked == null || baseline.party == null || baseline.home == null
            || baseline.keys == null || baseline.values == null) throw Invalid("missing baseline snapshot");
        foreach (var source in new[] { baseline.tracked, baseline.party, baseline.home })
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (int uid in source)
                if (uid <= 0 || ids.Contains(uid) || !seen.Add(uid)) throw Invalid("baseline UID invalid/duplicate/fixture");
        }
        if (baseline.keys.Length != baseline.values.Length) throw Invalid("baseline flag key/value length");
        var keys = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        foreach (string key in baseline.keys)
            if (string.IsNullOrEmpty(key) || !key.StartsWith("chitsii.ars.", System.StringComparison.Ordinal)
                || key.StartsWith("chitsii.ars.vfx.", System.StringComparison.Ordinal) || !keys.Add(key))
                throw Invalid("baseline flag key invalid/duplicate");
    }
}
