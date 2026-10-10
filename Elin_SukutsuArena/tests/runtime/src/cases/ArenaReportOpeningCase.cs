using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Before/after report regression. Opens the real packaged scene; never replaces
// its eval, background, script compiler, or exception behavior.
public sealed class ArenaReportOpeningCase : RuntimeCaseBase, IRuntimeCoroutineCase
{
    public override string Id => "report.arena.opening_background";
    public override IReadOnlyList<string> Tags => new[] { "integration", "report", "drama" };
    private Game baseline;
    private LayerDrama owned;
    private bool prepared, oldAutosave, background;
    private readonly List<string> errors = new List<string>();
    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(RuntimeV2Config.CaseIdFilter == Id && Game.id == "world_11"
            && EClass.pc.Name.Contains("RUNTIME_TEST") && !EClass.game.isLoading
            && !LayerDrama.IsActive(), "Dedicated, idle, single-case guard.");
        baseline = EClass.game;
        oldAutosave = EClass.debug.ignoreAutoSave;
        prepared = true;
        EClass.debug.ignoreAutoSave = true;
        Application.logMessageReceived += Capture;
        ctx.Log("product_dll=" + typeof(Elin_SukutsuArena.Plugin).Assembly.Location);
        ctx.Log("action=real_packaged_opening_scene;book=drama_sukutsu_opening;no_eval_substitution");
    }
    public override void Execute(RuntimeTestContext ctx) { }
    public override void Verify(RuntimeTestContext ctx)
    {
        foreach (var error in errors) ctx.Log("native_error=" + error);
        ctx.Log("background_visible=" + background);
        RuntimeAssertions.Require(errors.Count == 0, "Packaged opening error: " + string.Join(" | ", errors));
        RuntimeAssertions.Require(background, "Real opening background was not visible.");
    }
    public override void Cleanup(RuntimeTestContext ctx) { }
    public IEnumerator PrepareAsync(RuntimeTestContext ctx) { Prepare(ctx); yield break; }
    public IEnumerator VerifyAsync(RuntimeTestContext ctx) { Verify(ctx); yield break; }
    public IEnumerator ExecuteAsync(RuntimeTestContext ctx)
    {
        var master = EClass.game.cards.globalCharas.Find("sukutsu_arena_master");
        RuntimeAssertions.Require(master != null, "Existing dedicated-save Arena master unavailable; no synthetic NPC.");
        ctx.Log("existing_actor_uid=" + master.uid);
        try { owned = LayerDrama.Activate("drama_sukutsu_opening", "main", "main", master); }
        catch (Exception ex)
        {
            errors.Add(ex.ToString());
            owned = EClass.ui.GetLayer<LayerDrama>();
        }
        var start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 5f)
        {
            RuntimeAssertions.Require(object.ReferenceEquals(EClass.game, baseline), "Unexpected game replacement.");
            if (errors.Count != 0) break;
            RuntimeAssertions.Require(owned != null, "Packaged dialogue did not create a layer.");
            var bg = owned.drama.imageBG;
            if (bg != null && bg.enabled && bg.sprite != null && bg.sprite.name.Contains("arena_lobby"))
            { background = true; break; }
            yield return null;
        }
        yield return new WaitForSeconds(0.4f);
        var path = Path.Combine(Path.GetDirectoryName(RuntimeV2Config.ResultPath), "report-opening.png");
        ScreenCapture.CaptureScreenshot(path);
        yield return new WaitForEndOfFrame();
        ctx.Log("screenshot=" + path);
    }
    public IEnumerator CleanupAsync(RuntimeTestContext ctx)
    {
        Application.logMessageReceived -= Capture;
        if (!prepared) yield break;
        RuntimeAssertions.Require(Game.id == "world_11" && EClass.pc.Name.Contains("RUNTIME_TEST"), "Cleanup dedicated guard.");
        var layer = EClass.ui.GetLayer<LayerDrama>();
        RuntimeAssertions.Require(layer == null || layer == owned, "Unexpected dialogue owner; stop.");
        if (layer != null) layer.Close();
        Game.Load("world_11", baseline.isCloud);
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 30f && (EClass.game.isLoading || object.ReferenceEquals(EClass.game, baseline))) yield return null;
        RuntimeAssertions.Require(!EClass.game.isLoading && !object.ReferenceEquals(EClass.game, baseline)
            && Game.id == "world_11" && EClass.pc.Name.Contains("RUNTIME_TEST"), "Reload unconfirmed; stop.");
        EClass.debug.ignoreAutoSave = oldAutosave;
        ctx.Log("cleanup:dedicated_baseline_reload_confirmed;save_not_requested");
    }
    private void Capture(string message, string stack, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && errors.Count < 10) errors.Add(message + "\n" + stack);
    }
}
