using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

[TestFixture]
[NonParallelizable]
public sealed class ArsPr4HandoffCodecTests
{
    private static ArsPr4Handoff Sample()
    {
        const string token = "f7135f869d50433797d647d58312b608";
        var roles = new[] { "skeleton", "undead", "ordinary", "enemy_summon", "enemy_owner", "permanent" };
        var names = Array.ConvertAll(roles, role => "RUNTIME_TEST_PR4_ARS_" + token + "_" + role);
        return new ArsPr4Handoff
        {
            ownershipVersion = ArsPr4HandoffCodec.CurrentVersion,
            token = token, pcName = "夢見る未来「RUNTIME_TEST_STABLE」", masterUid = 1, zoneUid = 2,
            gameIdentity = 1685319820,
            uids = new[] { 15689, 15693, 15697, 15699, 15698, 15700 }, names = names,
            durations = new[] { 333, 225, 1000, 1000, 0, 0 }, ownerUids = new[] { 1, 1, 1, 15698, 0, 1 },
            baseline = new ArsPr4Baseline
            {
                tracked = new[] { 21, 22 }, party = new[] { 1, 21 }, home = new[] { 21, 22, 23 },
                keys = new[] { "chitsii.ars.sv.21", "chitsii.ars.quest.sample", "chitsii.ars.enh.21.level" },
                values = new[] { 1, 7, 3 }
            }
        };
    }

    private static JObject Json() { return JObject.Parse(ArsPr4HandoffCodec.Serialize(Sample())); }

    [Test]
    public void PreviousVersionTwoOutputFailsExplicitlyForMissingBaseline()
    {
        var json = Json();
        json["ownershipVersion"] = 2;
        json.Remove("baseline");
        json.Remove("ownerUids");
        var error = Assert.Throws<InvalidOperationException>(() => ArsPr4HandoffCodec.Deserialize(json.ToString()));
        Assert.That(error.Message, Does.Contain("missing root.baseline"));
    }

    [Test]
    public void ActualWriteReadPathPreservesEveryRequiredFieldAndBaseline()
    {
        string directory = Path.Combine(Path.GetTempPath(), "handoff-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "handoff.json");
        try
        {
            var original = Sample();
            ArsPr4HandoffCodec.WriteNew(path, original);
            var restored = ArsPr4HandoffCodec.Read(path);
            Assert.That(File.Exists(path + ".tmp"), Is.False);
            Assert.That(restored.ownershipVersion, Is.EqualTo(original.ownershipVersion));
            Assert.That(restored.token, Is.EqualTo(original.token));
            Assert.That(restored.pcName, Is.EqualTo(original.pcName));
            Assert.That(restored.masterUid, Is.EqualTo(original.masterUid));
            Assert.That(restored.zoneUid, Is.EqualTo(original.zoneUid));
            Assert.That(restored.gameIdentity, Is.EqualTo(original.gameIdentity));
            Assert.That(restored.uids, Is.EqualTo(original.uids));
            Assert.That(restored.names, Is.EqualTo(original.names));
            Assert.That(restored.durations, Is.EqualTo(original.durations));
            Assert.That(restored.ownerUids, Is.EqualTo(original.ownerUids));
            Assert.That(restored.baseline.tracked, Is.EqualTo(original.baseline.tracked));
            Assert.That(restored.baseline.party, Is.EqualTo(original.baseline.party));
            Assert.That(restored.baseline.home, Is.EqualTo(original.baseline.home));
            Assert.That(restored.baseline.keys, Is.EqualTo(original.baseline.keys));
            Assert.That(restored.baseline.values, Is.EqualTo(original.baseline.values));
            string saved = File.ReadAllText(path);
            Assert.Throws<InvalidOperationException>(() => ArsPr4HandoffCodec.WriteNew(path, Sample()));
            Assert.That(File.ReadAllText(path), Is.EqualTo(saved), "Previous evidence must not be replaced.");
        }
        finally { Directory.Delete(directory, true); }
    }

    [TestCase("ownershipVersion")]
    [TestCase("token")]
    [TestCase("pcName")]
    [TestCase("masterUid")]
    [TestCase("zoneUid")]
    [TestCase("gameIdentity")]
    [TestCase("uids")]
    [TestCase("names")]
    [TestCase("durations")]
    [TestCase("ownerUids")]
    [TestCase("baseline")]
    public void MissingOrNullRootEvidenceFailsWithoutInventingDefaults(string field)
    {
        var json = Json();
        json.Remove(field);
        Assert.Throws<InvalidOperationException>(() => ArsPr4HandoffCodec.Deserialize(json.ToString()));
        json[field] = JValue.CreateNull();
        Assert.Throws<InvalidOperationException>(() => ArsPr4HandoffCodec.Deserialize(json.ToString()));
    }

    [TestCase("tracked")]
    [TestCase("party")]
    [TestCase("home")]
    [TestCase("keys")]
    [TestCase("values")]
    public void MissingOrNullBaselineFieldFails(string field)
    {
        var json = Json();
        ((JObject)json["baseline"]).Remove(field);
        Assert.Throws<InvalidOperationException>(() => ArsPr4HandoffCodec.Deserialize(json.ToString()));
        json["baseline"][field] = JValue.CreateNull();
        Assert.Throws<InvalidOperationException>(() => ArsPr4HandoffCodec.Deserialize(json.ToString()));
    }

    [Test]
    public void EmptyButExplicitOriginalCollectionsRoundTrip()
    {
        var data = Sample();
        data.baseline = new ArsPr4Baseline
        {
            tracked = new int[0], party = new int[0], home = new int[0], keys = new string[0], values = new int[0]
        };
        var restored = ArsPr4HandoffCodec.Deserialize(ArsPr4HandoffCodec.Serialize(data));
        Assert.That(restored.baseline.tracked, Is.Empty);
        Assert.That(restored.baseline.party, Is.Empty);
        Assert.That(restored.baseline.home, Is.Empty);
        Assert.That(restored.baseline.keys, Is.Empty);
        Assert.That(restored.baseline.values, Is.Empty);
    }

    [TestCase("version")]
    [TestCase("token")]
    [TestCase("pc")]
    [TestCase("master")]
    [TestCase("zone")]
    [TestCase("game")]
    [TestCase("uid_zero")]
    [TestCase("uid_duplicate")]
    [TestCase("uid_pc")]
    [TestCase("names_short")]
    [TestCase("name_role")]
    [TestCase("name_token")]
    [TestCase("owners_short")]
    [TestCase("owner_pc")]
    [TestCase("owner_enemy")]
    [TestCase("duration_short")]
    [TestCase("duration_expired")]
    [TestCase("duration_permanent")]
    [TestCase("flags_short")]
    [TestCase("flags_duplicate")]
    [TestCase("baseline_fixture")]
    public void InvalidEvidenceFailsBothSerializationAndRead(string kind)
    {
        var data = Sample();
        switch (kind)
        {
            case "version": data.ownershipVersion = 2; break;
            case "token": data.token = ""; break;
            case "pc": data.pcName = "normal save"; break;
            case "master": data.masterUid = 0; break;
            case "zone": data.zoneUid = 0; break;
            case "game": data.gameIdentity = 0; break;
            case "uid_zero": data.uids[0] = 0; break;
            case "uid_duplicate": data.uids[0] = data.uids[1]; break;
            case "uid_pc": data.uids[0] = data.masterUid; break;
            case "names_short": data.names = new string[0]; break;
            case "name_role": data.names[0] = data.names[1]; break;
            case "name_token": data.names[0] = "RUNTIME_TEST_PR4_ARS_other_skeleton"; break;
            case "owners_short": data.ownerUids = new int[0]; break;
            case "owner_pc": data.ownerUids[0] = 999; break;
            case "owner_enemy": data.ownerUids[3] = data.masterUid; break;
            case "duration_short": data.durations = new int[0]; break;
            case "duration_expired": data.durations[1] = 0; break;
            case "duration_permanent": data.durations[5] = 1; break;
            case "flags_short": data.baseline.values = new int[0]; break;
            case "flags_duplicate": data.baseline.keys[1] = data.baseline.keys[0]; break;
            case "baseline_fixture": data.baseline.tracked[0] = data.uids[0]; break;
        }
        Assert.Throws<InvalidOperationException>(() => ArsPr4HandoffCodec.Serialize(data));
        Assert.Throws<InvalidOperationException>(() => ArsPr4HandoffCodec.Deserialize(JsonConvert.SerializeObject(data)));
    }

    [TestCase("scalar_string")]
    [TestCase("array_string")]
    [TestCase("unknown_root")]
    [TestCase("unknown_baseline")]
    [TestCase("duplicate_root")]
    [TestCase("duplicate_baseline")]
    [TestCase("trailing")]
    [TestCase("malformed")]
    public void NonStrictJsonFails(string kind)
    {
        var obj = Json();
        if (kind == "scalar_string") obj["masterUid"] = "1";
        if (kind == "array_string") obj["uids"][0] = "15689";
        if (kind == "unknown_root") obj["unexpected"] = 1;
        if (kind == "unknown_baseline") obj["baseline"]["unexpected"] = 1;
        string json = obj.ToString();
        if (kind == "duplicate_root") json = json.Replace("\"masterUid\": 1", "\"masterUid\": 999, \"masterUid\": 1");
        if (kind == "duplicate_baseline") json = json.Replace("\"tracked\":", "\"tracked\": [], \"tracked\":");
        if (kind == "trailing") json += " {}";
        if (kind == "malformed") json = "{";
        Assert.Throws<InvalidOperationException>(() => ArsPr4HandoffCodec.Deserialize(json));
    }

    [Test]
    public void AmbientJsonDefaultsCannotDropBaselineOrAddTypeMetadata()
    {
        var original = JsonConvert.DefaultSettings;
        try
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            string json = ArsPr4HandoffCodec.Serialize(Sample());
            Assert.That(json, Does.Not.Contain("$type"));
            Assert.That(ArsPr4HandoffCodec.Deserialize(json).baseline.keys, Is.EqualTo(Sample().baseline.keys));
        }
        finally { JsonConvert.DefaultSettings = original; }
    }
}
