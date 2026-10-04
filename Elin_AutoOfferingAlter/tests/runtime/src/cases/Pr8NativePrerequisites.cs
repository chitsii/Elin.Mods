#if RUNTIME_TEST
using System;
using System.Collections.Generic;

public static class Pr8NativePrerequisites
{
    public static Religion ResolveFaith<T>() where T : Religion
    {
        Religion result = null;
        foreach (SourceReligion.Row row in EClass.sources.religions.rows)
        {
            Religion loaded;
            if (!EClass.game.religions.dictAll.TryGetValue(row.id, out loaded) || !(loaded is T)) continue;
            RuntimeAssertions.Require(loaded.id == row.id && loaded.source == row, "Loaded faith/source mismatch: " + row.id);
            RuntimeAssertions.Require(result == null, "Ambiguous loaded native faith: " + typeof(T).Name);
            result = loaded;
        }
        RuntimeAssertions.Require(result != null, "Missing loaded faith/source: " + typeof(T).Name);
        return result;
    }
    public static void AuditAll(RuntimeTestContext ctx)
    {
        var errors = new List<string>();
        var inputs = new Pr8NativeInputs(ctx, "RUNTIME_TEST.PR8.PREFLIGHT." + Guid.NewGuid().ToString("N"), null);
        Check(ctx, errors, "loaded luck/Eyth source + dictAll", () =>
        {
            Religion luck = ResolveFaith<ReligionLuck>(), eyth = ResolveFaith<ReligionEyth>();
            ctx.Log("resolved_faith_ids: Luck=" + luck.id + ";Eyth=" + eyth.id + ";source/dict verified");
        });
        Check(ctx, errors, "all9 isolated cases: actor/item sources", () =>
        {
            RuntimeAssertions.Require(EClass.sources.charas.map.ContainsKey("putty"), "Native putty source missing.");
            foreach (string id in new[] { "water", "log", "altar", Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX })
                RuntimeAssertions.Require(EClass.sources.things.map.ContainsKey(id) && Pr8NativeInputs.IsConcrete(EClass.sources.things.map[id]),
                    "Native exact input source missing/origin/unregistered: " + id);
            foreach (int id in new[] { 85, 306, 764, 1228 })
                RuntimeAssertions.Require(EClass.sources.elements.map.ContainsKey(id), "Native element missing: " + id);
        });
        Check(ctx, errors, "consume/split/unsplit/actor-change/exception: fish + meat source candidates", () =>
        {
            foreach (string category in new[] { "fish", "meat" })
            {
                inputs.RecordCatalog(category);
                bool found = false;
                foreach (SourceThing.Row row in Pr8NativeInputs.ConcreteRows())
                    if (Pr8NativeInputs.Matches(row, category)) { found = true; break; }
                RuntimeAssertions.Require(found, "No native category candidate: " + category);
            }
        });
        Check(ctx, errors, "sleep_completion: native removal entrypoint", () =>
            RuntimeAssertions.Require(HarmonyLib.AccessTools.DeclaredMethod(typeof(ConSleep), "OnRemoved", Type.EmptyTypes) != null,
                "Native ConSleep.OnRemoved missing."));
        Check(ctx, errors, "source_craft_coldboot: postboot quick recipe", () =>
        {
            var recipe = RecipeManager.Get(Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX);
            RuntimeAssertions.Require(EClass.sources.initialized && recipe != null && recipe.IsQuickCraft,
                "Coldboot source/quick recipe unavailable; never reinitialize it from a test.");
        });
        // Neither phase is claimed ready solely because the save is open.
        ctx.Log("preflight: reload_prepare needs no manifest + preknown ingredient IDs; reload_verify needs a new Game + exact owned UID manifest");
        ctx.Log("preflight: native water/reject/value/batch/ingredients/body readiness is checked on owned fixtures before product calls");
        if (errors.Count != 0) throw new InvalidOperationException("PR8 aggregate preflight failed: " + string.Join(" | ", errors.ToArray()));
    }
    private static void Check(RuntimeTestContext ctx, List<string> errors, string name, Action check)
    {
        try { check(); ctx.Log("preflight:pass:" + name); }
        catch (Exception ex) { errors.Add(name + ":" + ex.Message); ctx.Log("preflight:failed:" + name + ":" + ex.Message); }
    }
    public static void RequirePreknownReloadIngredients()
    {
        // Native Card.AddThing records new ingredients on the selected Player. Do not repair that state afterwards.
        foreach (string id in new[] { Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX, "water", "log" })
        {
            SourceThing.Row row = EClass.sources.things.map[id];
            RuntimeAssertions.Require(EClass.player.recipes.knownIngredients.Contains(id)
                && (row.origin == null || EClass.player.recipes.knownIngredients.Contains(row.origin.id)),
                "Reload staging requires already-known ingredient/origin: " + id + "; prepare baseline through native play first.");
        }
    }
}
#endif
