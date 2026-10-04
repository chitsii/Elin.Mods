#if RUNTIME_TEST
using System;
using System.Collections.Generic;

public static class Pr8StateDiff
{
    public static List<string> Describe(string before, string after)
    {
        var changes = new List<string>();
        Walk(Newtonsoft.Json.Linq.JToken.Parse(before), Newtonsoft.Json.Linq.JToken.Parse(after), "$", changes);
        if (changes.Count == 0 && before != after) changes.Add("$: serialized text changed (tokens equal); still a preservation failure");
        return changes;
    }
    private static void Walk(Newtonsoft.Json.Linq.JToken before, Newtonsoft.Json.Linq.JToken after, string path, List<string> changes)
    {
        if (Newtonsoft.Json.Linq.JToken.DeepEquals(before, after)) return;
        var a = before as Newtonsoft.Json.Linq.JObject; var b = after as Newtonsoft.Json.Linq.JObject;
        if (a != null && b != null)
        {
            var names = new HashSet<string>();
            foreach (Newtonsoft.Json.Linq.JProperty p in a.Properties()) names.Add(p.Name);
            foreach (Newtonsoft.Json.Linq.JProperty p in b.Properties()) names.Add(p.Name);
            foreach (string name in names) Walk(a[name], b[name], path + "." + name, changes);
            return;
        }
        var aa = before as Newtonsoft.Json.Linq.JArray; var bb = after as Newtonsoft.Json.Linq.JArray;
        if (aa != null && bb != null)
        {
            for (int i = 0; i < Math.Max(aa.Count, bb.Count); i++)
                Walk(i < aa.Count ? aa[i] : null, i < bb.Count ? bb[i] : null, path + "[" + i + "]", changes);
            return;
        }
        changes.Add(path + ": before=" + (before == null ? "<missing>" : before.ToString(Newtonsoft.Json.Formatting.None))
            + ";after=" + (after == null ? "<missing>" : after.ToString(Newtonsoft.Json.Formatting.None)));
    }
}
#endif
