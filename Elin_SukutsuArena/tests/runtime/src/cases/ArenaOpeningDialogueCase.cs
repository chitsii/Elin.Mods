using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

// Actual first NPC conversation. Uses native confirmation/choice callbacks;
// does not jump past events, substitute sprites, or suppress script exceptions.
public sealed class ArenaOpeningDialogueCase : RuntimeCaseBase, IRuntimeCoroutineCase
{
    public override string Id => "drama.arena.opening_native_dialogue";
    public override IReadOnlyList<string> Tags => new[] { "integration", "critical", "drama", "compat" };
    private readonly Dictionary<string, Chara> actors = new Dictionary<string, Chara>();
    private readonly Dictionary<string, Chara> expectedDialogueActors = new Dictionary<string, Chara>();
    private readonly HashSet<string> portraits = new HashSet<string>();
    private readonly List<string> errors = new List<string>();
    private readonly HashSet<int> protectedUids = new HashSet<int>();
    private static readonly string[] ActorIds = { "sukutsu_arena_master", "sukutsu_receptionist" };
    private static readonly System.Reflection.FieldInfo CurrentEvent = AccessTools.Field(typeof(DramaSequence), "currentEvent");
    private Game baseline;
    private string saveId, token;
    private bool oldAutosave, prepared, backgroundSeen, finished;
    private LayerDrama layer;
    private DramaSequence sequence;

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(RuntimeV2Config.CaseIdFilter == Id, "Run this mutating case alone by CaseId.");
        RuntimeAssertions.Require(EClass.game != null && !EClass.game.isLoading && EClass.pc != null
            && EClass.pc.Name.Contains("RUNTIME_TEST") && !string.IsNullOrEmpty(Game.id), "Dedicated save required.");
        RuntimeAssertions.Require(!LayerDrama.IsActive() && CurrentEvent != null, "Existing dialogue/current event unavailable.");
        RuntimeAssertions.Require(!EClass.player.dialogFlags.ContainsKey("sukutsu_opening_seen")
            || EClass.player.dialogFlags["sukutsu_opening_seen"] == 0, "Use a baseline before the first Arena conversation.");
        RuntimeAssertions.Require(!EClass._map.charas.Exists(c => Array.IndexOf(ActorIds, c.id) >= 0), "Existing Arena NPC on map; use an isolated baseline.");
        baseline = EClass.game;
        saveId = Game.id;
        oldAutosave = EClass.debug.ignoreAutoSave;
        token = "RUNTIME_TEST_OPENING_" + Guid.NewGuid().ToString("N");
        foreach (var c in EClass._map.charas) protectedUids.Add(c.uid);
        foreach (var c in EClass.game.cards.globalCharas.Values) protectedUids.Add(c.uid);
        prepared = true;
        EClass.debug.ignoreAutoSave = true;
        Application.logMessageReceived += CaptureError;
        foreach (var id in ActorIds)
        {
            var actor = CharaGen.Create(id, 1);
            RuntimeAssertions.Require(actor != null && actor.id == id && actor.uid > 0
                && !protectedUids.Contains(actor.uid), "Native generation ownership mismatch: " + id);
            actors.Add(id, actor);
            actor.c_altName = token + "_" + id;
            EClass._zone.AddCard(actor, EClass.pc.pos.GetNearestPoint(
                allowBlock: false, allowChara: false, allowInstalled: false));
            RuntimeAssertions.Require(actor.IsAliveInCurrentZone, "NPC not on map: " + id);
            ctx.Log("native_spawn=" + id + ";uid=" + actor.uid + ";material=" + actor.idMaterial);
            // Native DramaSequence.GetActor resolves global NPCs before map NPCs.
            RuntimeAssertions.Require(!EClass.sources.persons.map.ContainsKey(id), "Unexpected Person source: " + id);
            var resolved = EClass.game.cards.globalCharas.Find(id) ?? EClass._map.FindChara(id);
            RuntimeAssertions.Require(resolved != null && resolved.id == id, "Native actor resolution missing: " + id);
            expectedDialogueActors.Add(id, resolved);
            ctx.Log("native_dialogue_actor=" + id + ";uid=" + resolved.uid
                + ";fixture_uid=" + actor.uid + ";global=" + resolved.IsGlobal);
        }
    }

    public override void Execute(RuntimeTestContext ctx) { }
    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(errors.Count == 0, "Native dialogue errors: " + string.Join(" | ", errors));
        RuntimeAssertions.Require(finished && backgroundSeen && portraits.Count == ActorIds.Length,
            "Opening did not complete with its real background and both NPC portraits.");
        RuntimeAssertions.Require(EClass.player.dialogFlags.ContainsKey("sukutsu_opening_seen")
            && EClass.player.dialogFlags["sukutsu_opening_seen"] == 1, "Opening completion flag missing.");
        ctx.Log("native_opening_completed=true;background=true;portraits=2/2;forced_event_skips=0");
    }
    public override void Cleanup(RuntimeTestContext ctx) { }
    public IEnumerator PrepareAsync(RuntimeTestContext ctx) { Prepare(ctx); yield break; }
    public IEnumerator VerifyAsync(RuntimeTestContext ctx) { Verify(ctx); yield break; }

    public IEnumerator ExecuteAsync(RuntimeTestContext ctx)
    {
        actors["sukutsu_arena_master"].ShowDialog();
        // NPC pre-invoke starts the requested book asynchronously on a later frame.
        float activationStart = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - activationStart < 10f)
        {
            RuntimeAssertions.Require(object.ReferenceEquals(EClass.game, baseline), "Game changed while awaiting NPC dialogue.");
            var current = EClass.ui.GetLayer<LayerDrama>();
            if (current != null && current.drama.setup.book == "drama_sukutsu_opening")
            { layer = current; break; }
            yield return null;
        }
        RuntimeAssertions.Require(layer != null && layer.drama.setup.book == "drama_sukutsu_opening",
            "Native first NPC conversation did not select the opening book.");
        sequence = layer.drama.sequence;
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 90f)
        {
            RuntimeAssertions.Require(errors.Count == 0, "Drama execution error: " + string.Join(" | ", errors));
            if (sequence.isExited) { finished = true; break; }
            RuntimeAssertions.Require(object.ReferenceEquals(EClass.game, baseline)
                && EClass.ui.GetLayer<LayerDrama>() == layer, "Game/dialogue identity changed unexpectedly.");
            var talk = CurrentEvent.GetValue(sequence) as DramaEventTalk;
            if (talk == null || talk.progress == 0 || talk.timer < 0.15f)
            { yield return null; continue; }
            var dm = layer.drama;
            var bg = dm.imageBG;
            RuntimeAssertions.Require(bg != null && bg.enabled && bg.sprite != null && bg.sprite.texture != null
                && bg.sprite.name.Contains("arena_lobby"), "Opening background absent/wrong asset.");
            backgroundSeen = true;
            var actor = sequence.GetActor(talk.idActor);
            var id = actor?.owner?.chara?.id;
            if (id != null && actors.ContainsKey(id) && !portraits.Contains(id))
            {
                RuntimeAssertions.Require(object.ReferenceEquals(actor.owner.chara, expectedDialogueActors[id]), "Dialogue used a different NPC.");
                var portrait = dm.dialog.portrait;
                RuntimeAssertions.Require(portrait != null, "Native dialogue portrait component missing.");
                var images = new[] { portrait.portrait, portrait.imageFull };
                var visible = Array.Find(images, image => image != null && image.enabled
                    && image.gameObject.activeInHierarchy && image.sprite != null
                    && image.sprite != portrait.spriteNoPortrait);
                RuntimeAssertions.Require(visible != null, "No visible NPC portrait: " + id);
                yield return new WaitForSeconds(0.4f);
                var path = Path.Combine(Path.GetDirectoryName(RuntimeV2Config.ResultPath), "opening-" + id + ".png");
                ScreenCapture.CaptureScreenshot(path);
                yield return new WaitForEndOfFrame();
                portraits.Add(id);
                ctx.Log("portrait=" + id + ";sprite=" + visible.sprite.name + ";background=" + bg.sprite.name + ";screenshot=" + path);
            }
            var choice = talk.choices.Find(c => c.button != null && c.button.interactable
                && c.button.gameObject.activeInHierarchy && (c.activeCondition == null || c.activeCondition()));
            if (choice != null)
            {
                ctx.Log("native_choice=" + choice.idJump);
                choice.button.onClick.Invoke();
            }
            else if (talk.choices.Count == 0)
            {
                // Same input consumed by DramaEventTalk.Play; restored before yielding.
                var oldAction = EInput.action;
                try { EInput.action = EAction.Confirm; sequence.OnUpdate(); }
                finally { EInput.action = oldAction; }
            }
            yield return null;
        }
    }

    public IEnumerator CleanupAsync(RuntimeTestContext ctx)
    {
        Application.logMessageReceived -= CaptureError;
        if (!prepared) yield break;
        RuntimeAssertions.Require(Game.id == saveId && EClass.pc != null && EClass.pc.Name.Contains("RUNTIME_TEST"),
            "Cleanup dedicated save guard; stop and restore externally.");
        var current = EClass.ui.GetLayer<LayerDrama>();
        RuntimeAssertions.Require(current == null || current == layer, "Unexpected dialogue; hold cleanup.");
        if (current != null) current.Close();
        EClass.debug.ignoreAutoSave = true;
        Game.Load(saveId, baseline.isCloud);
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < 30f && (EClass.game.isLoading || object.ReferenceEquals(EClass.game, baseline)))
            yield return null;
        RuntimeAssertions.Require(!EClass.game.isLoading && !object.ReferenceEquals(EClass.game, baseline)
            && Game.id == saveId && EClass.pc.Name.Contains("RUNTIME_TEST"), "Baseline reload not confirmed; stop all tests.");
        RuntimeAssertions.Require(!EClass._map.charas.Exists(c => c.c_altName != null && c.c_altName.StartsWith(token))
            && !System.Linq.Enumerable.Any(EClass.game.cards.globalCharas.Values, c => c.c_altName != null && c.c_altName.StartsWith(token)),
            "Owned NPCs remained after baseline reload.");
        EClass.debug.ignoreAutoSave = oldAutosave;
        ctx.Log("cleanup:native_baseline_reload_confirmed;owned_npcs_absent;save_not_requested");
    }

    private void CaptureError(string message, string stack, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && errors.Count < 20)
            errors.Add(message + "\n" + stack);
    }
}
