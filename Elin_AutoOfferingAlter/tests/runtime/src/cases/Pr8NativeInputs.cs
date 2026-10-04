#if RUNTIME_TEST
using System;
using System.Collections.Generic;

public sealed class Pr8NativeInputs
{
    private readonly RuntimeTestContext ctx;
    private readonly string token;
    private readonly Action<Thing> requireOwned;
    private readonly List<object> events = new List<object>();
    private readonly HashSet<string> catalogs = new HashSet<string>();

    public Pr8NativeInputs(RuntimeTestContext context, string fixtureToken, Action<Thing> ownership)
    { ctx = context; token = fixtureToken; requireOwned = ownership; }

    public static bool IsConcrete(SourceThing.Row row)
    {
        SourceThing.Row thingRow;
        CardRow cardRow;
        return row != null && !row.isOrigin && !row.isChara && !string.IsNullOrEmpty(row.id)
            && EClass.sources.things.map.TryGetValue(row.id, out thingRow) && ReferenceEquals(thingRow, row)
            && EClass.sources.cards.map.TryGetValue(row.id, out cardRow) && ReferenceEquals(cardRow, row);
    }

    public static bool Matches(SourceThing.Row row, string category)
    { return row != null && (row._origin == category || row.Category.IsChildOf(category)); }

    public static IEnumerable<SourceThing.Row> ConcreteRows()
    {
        foreach (SourceThing.Row row in EClass.sources.things.rows)
            if (IsConcrete(row)) yield return row;
    }

    public void RecordCatalog(string category)
    {
        if (!catalogs.Add(category)) return;
        Record("loaded_source_candidates", () =>
        {
            var rows = new List<object>();
            foreach (SourceThing.Row row in EClass.sources.things.rows)
                if (Matches(row, category)) rows.Add(RowSnapshot(row));
            return new { requestedCategory = category, rows = rows };
        });
    }

    public Thing CreateExact(string id, int num)
    {
        SourceThing.Row requested;
        EClass.sources.things.map.TryGetValue(id, out requested);
        Record("create_requested", () => new { requestedId = id, requestedNum = num, source = RowSnapshot(requested) });
        RuntimeAssertions.Require(num > 0 && IsConcrete(requested), "Native input requires a registered concrete Thing row: " + id);
        Thing item;
        try { item = ThingGen.Create(id); }
        catch (Exception ex)
        {
            Record("create_failed", () => new { requestedId = id, requestedNum = num, source = RowSnapshot(requested), error = ex.ToString() });
            throw;
        }
        // Keep actual IDs even when identity/ownership checks fail. This record does not authorize any mutation.
        Record("create_returned", () => new { requestedId = id, requestedNum = num, source = RowSnapshot(requested), actual = ItemSnapshot(item) });
        RuntimeAssertions.Require(item != null, "Native ThingGen returned null: " + id);
        requireOwned(item);
        RuntimeAssertions.Require(item.id == id && ReferenceEquals(item.source, requested) && !item.isDestroyed && item.material != null,
            "Native concrete input identity/source/material mismatch: requested=" + id + ";actual=" + item.id);
        item.SetNum(num);
        Record("create_configured", () => new { requestedId = id, requestedNum = num, actual = ItemSnapshot(item) });
        RuntimeAssertions.Require(item.Num == num, "Native input quantity mismatch: " + id);
        return item;
    }

    public void Record(string stage, Func<object> readData)
    {
        Pr8Diagnostic.BestEffort(() =>
        {
            var record = new { stage = stage, token = token, data = readData() };
            events.Add(record);
            string path = System.IO.Path.Combine(ctx.ModRoot, "tests", "runtime", "_artifacts", "pr8-input", token + ".json");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(events, Newtonsoft.Json.Formatting.Indented));
            if (ctx.Logs.Count < 44)
                ctx.Log(stage == "loaded_source_candidates" ? "pr8_input_catalog:token=" + token + ";file=" + path
                    : "pr8_input_json:" + Newtonsoft.Json.JsonConvert.SerializeObject(record));
        });
    }

    public static object ItemSnapshot(Thing item)
    {
        if (item == null) return null;
        return new { uid = item.uid, actualId = item.id, num = item.Num, destroyed = item.isDestroyed,
            category = item.category == null ? null : item.category.id, source = RowSnapshot(item.source),
            trait = item.trait == null ? null : item.trait.GetType().FullName,
            materialId = item.material == null ? (int?)null : item.material.id,
            materialAlias = item.material == null ? null : item.material.alias };
    }

    public static object RowSnapshot(SourceThing.Row row)
    {
        if (row == null) return null;
        return new { id = row.id, category = row.category, origin = row._origin,
            originId = row.origin == null ? null : row.origin.id, isOrigin = row.isOrigin, isChara = row.isChara,
            concreteRegistered = IsConcrete(row) };
    }
}
#endif
