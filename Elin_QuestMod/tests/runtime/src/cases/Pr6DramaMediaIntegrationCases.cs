// Test-only native media observers. No mod using directives: the csx builder strips them.
public abstract class Pr6QuestMediaCase : RuntimeCaseBase
{
    protected Pr6QuestMediaFixture Fixture;
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "pr6" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        Fixture = new Pr6QuestMediaFixture(ctx);
        ctx.RegisterRollback("pr6.media.cleanup", () => Fixture.Dispose());
        Fixture.Install();
    }

    public override void Verify(RuntimeTestContext ctx) { Fixture.AssertUnchanged(); }
    public override void Cleanup(RuntimeTestContext ctx) { if (Fixture != null) Fixture.Dispose(); }
}

public sealed class Pr6QuestFxOnlyCase : Pr6QuestMediaCase
{
    public override string Id => "pr6.quest.resolver_fx_only_dispatch";
    public override void Execute(RuntimeTestContext ctx)
    {
        Fixture.Begin(false, false);
        bool result;
        try { result = Fixture.Execute("fx.pc.teleport"); }
        finally { Fixture.End(); }
        Fixture.AssertDispatch(result, true, 1, 0);
        ctx.Log("coverage=dispatch_only; render/audio not asserted; human observation not collected");
    }
}

public sealed class Pr6QuestEmptyMediaCase : Pr6QuestMediaCase
{
    public override string Id => "pr6.quest.empty_media_rejected";
    public override void Execute(RuntimeTestContext ctx)
    {
        foreach (string key in new[] { "", "fx.pc.", "fx.pc.+sfx.pc.revive", "fx.pc.teleport+sfx.pc.", "sfx.pc.revive" })
        {
            Fixture.Begin(false, false);
            bool result;
            try { result = Fixture.Execute(key); }
            finally { Fixture.End(); }
            Fixture.AssertDispatch(result, false, 0, 0);
            ctx.Log("parser_reject: key=" + key + "; result=false; effect=0; sound=0");
        }
        Fixture.Begin(false, false);
        bool empty;
        try { empty = Fixture.PlayContext("", "revive"); }
        finally { Fixture.End(); }
        Fixture.AssertDispatch(empty, false, 0, 0);
        ctx.Log("context_empty_effect=false; native effect/sound=0; coverage=dispatch_only");
    }
}

public sealed class Pr6QuestEffectFailureCase : Pr6QuestMediaCase
{
    public override string Id => "pr6.quest.effect_failure_sound_attempted";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "pr6", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        Fixture.Begin(true, false);
        bool result;
        try { result = Fixture.Execute("fx.pc.teleport+sfx.pc.revive"); }
        finally { Fixture.End(); }
        Fixture.AssertDispatch(result, false, 1, 1);
        RuntimeAssertions.Require(Fixture.Injected == 1, "Expected exactly one fixture effect exception.");
        RuntimeAssertions.Require(Fixture.EffectResult == null, "Injected effect unexpectedly returned.");
        ctx.Log("coverage=fault_injection_dispatch; effect exception caught; sound independently attempted; result=false");
    }
}

public sealed class Pr6QuestSoundFailureCase : Pr6QuestMediaCase
{
    public override string Id => "pr6.quest.sound_failure_result_false";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "pr6", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        Fixture.Begin(false, true);
        bool result;
        try { result = Fixture.Execute("fx.pc.teleport+sfx.pc.revive"); }
        finally { Fixture.End(); }
        Fixture.AssertDispatch(result, false, 1, 1);
        RuntimeAssertions.Require(Fixture.Injected == 1, "Expected exactly one fixture sound exception.");
        RuntimeAssertions.Require(Fixture.EffectResult != null, "Effect did not reach native result before sound failure.");
        ctx.Log("coverage=fault_injection_dispatch; effect returned; sound exception caught; result=false");
    }
}

public sealed class Pr6QuestCleanupOwnershipCase : Pr6QuestMediaCase
{
    public override string Id => "pr6.quest.cleanup_unknown_fx_child_rejected";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "pr6", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        Fixture.Begin(false, false);
        bool result;
        try { result = Fixture.Execute("fx.pc.teleport"); }
        finally { Fixture.End(); }
        Fixture.AssertDispatch(result, true, 1, 0);
        var effect = Fixture.EffectResult;
        RuntimeAssertions.Require(effect != null && Effect.manager.list.Contains(effect), "Native effect fixture is unavailable.");
        var child = new UnityEngine.GameObject("RUNTIME_TEST_PR6_unknown_child");
        var childTransform = child.transform;
        ctx.RegisterRollback("pr6.unknown_child", () => RemoveControlChild(child, childTransform, effect));
        try
        {
            childTransform.SetParent(effect.transform, false);
            bool rejected = false;
            try { Fixture.CleanupOwnedEffect(effect); }
            catch (System.InvalidOperationException ex) { rejected = ex.Message.Contains("Unknown child"); }
            RuntimeAssertions.Require(rejected, "FX cleanup accepted an unrecorded child.");
            RuntimeAssertions.Require(effect != null && Effect.manager.list.Contains(effect) && child != null && childTransform.parent == effect.transform, "Rejected cleanup destroyed or detached native/control objects.");
            ctx.Log("ownership_counterexample: unknown child rejected; native FX and control intact; coverage=cleanup_guard_not_render_audio");
        }
        finally { RemoveControlChild(child, childTransform, effect); }
    }
    private static void RemoveControlChild(UnityEngine.GameObject child, UnityEngine.Transform recorded, Effect effect)
    {
        if (child == null) return;
        RuntimeAssertions.Require(object.ReferenceEquals(child.transform, recorded) && recorded.childCount == 0, "Unknown control descendants; refusing to destroy them.");
        RuntimeAssertions.Require(recorded.parent == null || (effect != null && recorded.parent == effect.transform), "Control owner changed; refusing to destroy it.");
        UnityEngine.Object.DestroyImmediate(child);
    }
}

public abstract class Pr6QuestMediaCoroutineCase : Pr6QuestMediaCase, IRuntimeCoroutineCase
{
    public System.Collections.IEnumerator PrepareAsync(RuntimeTestContext ctx) { Prepare(ctx); yield break; }
    public System.Collections.IEnumerator VerifyAsync(RuntimeTestContext ctx) { Verify(ctx); yield break; }
    public System.Collections.IEnumerator CleanupAsync(RuntimeTestContext ctx) { Cleanup(ctx); yield break; }
    public abstract System.Collections.IEnumerator ExecuteAsync(RuntimeTestContext ctx);
    public override void Execute(RuntimeTestContext ctx)
    {
        throw new System.InvalidOperationException("This case requires the async runtime host.");
    }
}

public sealed class Pr6QuestMediaActualCase : Pr6QuestMediaCoroutineCase
{
    public override string Id => "pr6.quest.fx_sound_actual";
    public override System.Collections.IEnumerator ExecuteAsync(RuntimeTestContext ctx)
    {
        Fixture.Begin(false, false);
        bool result;
        try { result = Fixture.Execute("fx.pc.teleport+sfx.pc.revive"); }
        finally { Fixture.End(); }
        Fixture.AssertDispatch(result, true, 1, 1);
        Fixture.AssertAudio();
        bool rendered = false;
        // Stay in this IEnumerator so the host catches assertion errors and runs cleanup.
        for (int frame = 0; frame < 45; frame++)
        {
            rendered |= Fixture.ObserveRender();
            if (rendered) break;
            yield return null;
        }
        RuntimeAssertions.Require(rendered, "No enabled visible FX renderer/particles observed within 45 frames.");
        ctx.Log("coverage=native_render_state_and_audio_channel_clip; human saw/heard evidence=not_collected");
    }
}

public sealed class Pr6QuestShowcaseMediaCase : Pr6QuestMediaCoroutineCase
{
    public override string Id => "pr6.quest.showcase_cue_media_actual";
    public override System.Collections.IEnumerator ExecuteAsync(RuntimeTestContext ctx)
    {
        Fixture.OpenShowcase();
        Fixture.Begin(false, false);
        try
        {
            Fixture.PlayShowcaseMediaSegment();
        }
        finally
        {
            Fixture.End();
            Fixture.StopShowcase();
        }
        Fixture.AssertDispatch(Fixture.CommandResult, true, 1, 1);
        RuntimeAssertions.Require(Fixture.CueCalls == 1 && Fixture.CueResult, "Native showcase cue was not successfully dispatched once.");
        RuntimeAssertions.Require(Fixture.CommandCalls == 1, "Native showcase FX command was not dispatched exactly once.");
        Fixture.AssertAudio();
        bool rendered = false;
        for (int frame = 0; frame < 45; frame++)
        {
            rendered |= Fixture.ObserveRender();
            if (rendered) break;
            yield return null;
        }
        RuntimeAssertions.Require(rendered, "Showcase FX has no observed visible renderer/particles.");
        ctx.Log("coverage=loaded_xlsx_native_sequence_CWL_eval_cue_media; bounded segment ends before quest completion/reward; human observation=not_collected");
    }
}

public sealed class Pr6QuestMediaFixture : System.IDisposable
{
    private static Pr6QuestMediaFixture Active;
    private readonly RuntimeTestContext Context;
    private readonly Chara Pc;
    private readonly int X, Z, Hp;
    private readonly Layer Top;
    private readonly System.Collections.Generic.Dictionary<string, int> Flags;
    private readonly System.Collections.Generic.Dictionary<string, int> ScopedFlags;
    private readonly string FlagPrefix;
    private readonly System.Collections.Generic.HashSet<Effect> BaselineEffects;
    private readonly System.Collections.Generic.List<Effect> OwnedEffects = new System.Collections.Generic.List<Effect>();
    private readonly System.Collections.Generic.Dictionary<Effect, System.Collections.Generic.HashSet<UnityEngine.Transform>> EffectChildren = new System.Collections.Generic.Dictionary<Effect, System.Collections.Generic.HashSet<UnityEngine.Transform>>();
    private readonly System.Collections.Generic.Dictionary<Effect, UnityEngine.Vector3> EffectOrigins = new System.Collections.Generic.Dictionary<Effect, UnityEngine.Vector3>();
    private readonly System.Collections.Generic.Dictionary<Effect, UnityEngine.Transform> EffectParents = new System.Collections.Generic.Dictionary<Effect, UnityEngine.Transform>();
    private readonly System.Collections.Generic.Dictionary<SoundSource, UnityEngine.AudioClip> OwnedSounds = new System.Collections.Generic.Dictionary<SoundSource, UnityEngine.AudioClip>();
    private readonly System.Collections.Generic.HashSet<SoundSource> BaselineSounds;
    private readonly System.Collections.Generic.List<System.Action> UiRestores = new System.Collections.Generic.List<System.Action>();
    private readonly System.Collections.Generic.List<System.Action> AudioRestores = new System.Collections.Generic.List<System.Action>();
    private readonly System.Collections.Generic.HashSet<SoundSource> CapturedSources = new System.Collections.Generic.HashSet<SoundSource>();
    private readonly System.Collections.Generic.HashSet<SoundSource> ReusedSounds = new System.Collections.Generic.HashSet<SoundSource>();
    private readonly Pr6ActivationOwnership<Effect> EffectActivations = new Pr6ActivationOwnership<Effect>();
    private Effect ExpectedEffectActivation;
    private readonly System.Collections.Generic.Dictionary<SoundSource, UnityEngine.AudioSource> RecordedAudio = new System.Collections.Generic.Dictionary<SoundSource, UnityEngine.AudioSource>();
    private readonly System.Collections.Generic.Dictionary<SoundSource, UnityEngine.Transform> SoundParents = new System.Collections.Generic.Dictionary<SoundSource, UnityEngine.Transform>();
    private readonly SoundData ReviveData, ExtraData;
    private readonly object RuntimeContext, Resolver;
    private readonly System.Reflection.MethodInfo ExecuteMethod, ContextMethod, ResolverMethod;
    private readonly HarmonyLib.Harmony Observer;
    private bool Observing, ThrowEffect, ThrowSound, Disposed, ShowcaseOpened;
    private LayerDrama OwnedLayer;
    private bool CreatingLayer;
    private Layer LayerParent;
    private System.Collections.Generic.HashSet<UnityEngine.Transform> LayerChildren;
    private bool EffectDefaults, SoundDefaults, RootPosition;
    private int EffectCalls, SoundCalls;
    public int Injected, CueCalls, CommandCalls;
    public bool CueResult, CommandResult;
    public Effect EffectResult;
    public SoundSource SoundResult;
    private UnityEngine.AudioClip ObservedClip;

    public Pr6QuestMediaFixture(RuntimeTestContext ctx)
    {
        Context = ctx;
        RuntimeAssertions.Require(Active == null, "Another PR6 observer is active.");
        RuntimeAssertions.Require(EClass.pc != null && EClass.pc.Name.Contains("RUNTIME_TEST"), "Dedicated RUNTIME_TEST PC required.");
        RuntimeAssertions.Require(EClass.ui != null && EClass.player != null && EClass.player.dialogFlags != null, "UI/player/flags unavailable.");
        RuntimeAssertions.Require(LayerDrama.Instance == null && !(EClass.ui.TopLayer is LayerDrama), "Close existing drama before this case.");
        RuntimeAssertions.Require(Effect.manager != null && SoundManager.current != null, "Effect/audio manager unavailable.");
        RuntimeAssertions.Require(!SoundManager.ignoreSounds && UnityEngine.AudioListener.volume > 0f && !UnityEngine.AudioListener.pause, "Audio is muted/paused; prepare failed.");
        Pc = EClass.pc; X = Pc.pos.x; Z = Pc.pos.z; Hp = Pc.hp; Top = EClass.ui.TopLayer;
        Flags = EClass.player.dialogFlags;
        var service = ModRuntimeReflection.RequireType("Elin_QuestMod.Quest.QuestStateService");
        FlagPrefix = (string)service.GetMethod("GetDefaultPrefix").Invoke(null, null) + ".";
        ScopedFlags = SnapshotFlags();
        BaselineEffects = new System.Collections.Generic.HashSet<Effect>(Effect.manager.list);
        BaselineSounds = new System.Collections.Generic.HashSet<SoundSource>();
        foreach (var sound in SoundManager.current.listSfx) if (sound != null && sound.isPlaying) BaselineSounds.Add(sound);
        ReviveData = SoundManager.current.GetData("revive");
        ExtraData = SoundManager.current.GetData("base.ok");
        RuntimeAssertions.Require(ReviveData != null && ExtraData != null, "Known native sound data is missing.");
        var contextType = ModRuntimeReflection.RequireType("Elin_QuestMod.Drama.GameQuestDramaRuntimeContext");
        var resolverType = ModRuntimeReflection.RequireType("Elin_QuestMod.Drama.QuestDramaResolver");
        RuntimeContext = System.Activator.CreateInstance(contextType);
        Resolver = System.Activator.CreateInstance(resolverType, new[] { RuntimeContext });
        ExecuteMethod = RequireMethod(resolverType, "TryExecute", new[] { typeof(string) });
        ContextMethod = RequireMethod(contextType, "PlayPcEffect", new[] { typeof(string), typeof(string) });
        ResolverMethod = RequireMethod(resolverType, "TryExecute", new[] { typeof(string) });
        Observer = new HarmonyLib.Harmony("codex.runtime.pr6." + System.Guid.NewGuid().ToString("N"));
        ctx.Log("fixture.pc_uid=" + Pc.uid + "; root_uid=" + Pc.GetRootCard().uid + "; pos=" + X + "," + Z);
        ctx.Log("loaded_mod=" + contextType.Assembly.FullName + "; location=" + contextType.Assembly.Location);
    }

    public void Install()
    {
        Active = this;
        CaptureSoundData(ReviveData);
        CaptureSoundData(ExtraData);
        var effect = RequireMethod(typeof(Card), "PlayEffect", new[] { typeof(string), typeof(bool), typeof(float), typeof(UnityEngine.Vector3) });
        var sound = RequireMethod(typeof(Card), "PlaySound", new[] { typeof(string), typeof(float), typeof(bool) });
        var ep = effect.GetParameters(); var sp = sound.GetParameters();
        RuntimeAssertions.Require(ep[1].IsOptional && (bool)ep[1].DefaultValue && ep[2].IsOptional && (float)ep[2].DefaultValue == 0f && ep[3].IsOptional, "Card.PlayEffect optional defaults changed.");
        RuntimeAssertions.Require(sp[1].IsOptional && (float)sp[1].DefaultValue == 1f && sp[2].IsOptional && (bool)sp[2].DefaultValue, "Card.PlaySound optional defaults changed.");
        Patch(effect, "EffectPrefix", "EffectPostfix");
        Patch(sound, "SoundPrefix", "SoundPostfix");
        Patch(RequireMethod(typeof(SoundSource), "Play", new[] { typeof(SoundData), typeof(float), typeof(UnityEngine.Vector3) }), "NativeSoundPrefix", "NativeSoundPostfix");
        Patch(RequireMethod(typeof(Effect), "_Play", new[] { typeof(Point), typeof(UnityEngine.Vector3), typeof(float), typeof(Point), typeof(UnityEngine.Sprite) }), "NativeEffectPrefix", null);
        var activate = typeof(Effect).GetMethod("Activate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly, null, System.Type.EmptyTypes, null);
        RuntimeAssertions.Require(activate != null && activate.IsFamily, "Exact protected Effect.Activate() signature absent.");
        Patch(activate, "NativeEffectActivationPrefix", null);
        Patch(ResolverMethod, null, "ResolverPostfix");
        Patch(RequireMethod(typeof(LayerDrama), "OnInit", System.Type.EmptyTypes), null, "LayerInitPostfix");
        Patch(RequireMethod(ModRuntimeReflection.RequireType("Elin_QuestMod.Drama.GameQuestDramaRuntimeContext"), "RunCue", new[] { typeof(string) }), null, "CuePostfix");
    }

    private static System.Reflection.MethodInfo RequireMethod(System.Type type, string name, System.Type[] args)
    {
        var method = type.GetMethod(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, args, null);
        RuntimeAssertions.Require(method != null, "Exact native/runtime signature absent: " + type.FullName + "." + name);
        return method;
    }

    private void Patch(System.Reflection.MethodInfo method, string prefix, string postfix)
    {
        var binding = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public;
        Observer.Patch(method,
            prefix == null ? null : new HarmonyLib.HarmonyMethod(typeof(Pr6QuestMediaFixture).GetMethod(prefix, binding)),
            postfix == null ? null : new HarmonyLib.HarmonyMethod(typeof(Pr6QuestMediaFixture).GetMethod(postfix, binding)));
    }

    public void Begin(bool throwEffect, bool throwSound)
    {
        EffectCalls = SoundCalls = Injected = CueCalls = CommandCalls = 0;
        EffectDefaults = SoundDefaults = RootPosition = false;
        EffectResult = null; SoundResult = null; ObservedClip = null;
        CueResult = CommandResult = false;
        ThrowEffect = throwEffect; ThrowSound = throwSound; Observing = true;
    }
    public void End() { Observing = false; ThrowEffect = ThrowSound = false; ExpectedEffectActivation = null; }
    public bool Execute(string key) { return (bool)ExecuteMethod.Invoke(Resolver, new object[] { key }); }
    public bool PlayContext(string effect, string sound) { return (bool)ContextMethod.Invoke(RuntimeContext, new object[] { effect, sound }); }

    public static void EffectPrefix(Card __instance, string __0, bool __1, float __2, UnityEngine.Vector3 __3, out bool __state)
    {
        var f = Active;
        __state = f != null && f.Observing && object.ReferenceEquals(__instance, f.Pc);
        if (!__state) return;
        f.EffectCalls++;
        f.EffectDefaults = __0 == "teleport" && __1 && __2 == 0f && __3 == default(UnityEngine.Vector3);
        if (f.ThrowEffect) { f.Injected++; throw new System.InvalidOperationException("PR6 fixture-only effect failure"); }
    }
    public static void EffectPostfix(Effect __result, bool __state)
    {
        var f = Active;
        if (f == null || !__state) return;
        f.EffectResult = __result;
        if (__result != null && !f.BaselineEffects.Contains(__result))
        {
            if (!f.OwnedEffects.Contains(__result)) f.OwnedEffects.Add(__result);
            f.EffectOrigins[__result] = __result.fromV;
        }
    }
    public static void SoundPrefix(Card __instance, string __0, float __1, bool __2, out bool __state)
    {
        var f = Active;
        __state = f != null && f.Observing && object.ReferenceEquals(__instance, f.Pc);
        if (!__state) return;
        f.SoundCalls++;
        f.SoundDefaults = __0 == "revive" && __1 == 1f && __2;
        if (f.ThrowSound) { f.Injected++; throw new System.InvalidOperationException("PR6 fixture-only sound failure"); }
    }
    public static void SoundPostfix(SoundSource __result, bool __state)
    {
        var f = Active;
        if (f == null || !__state) return;
        f.SoundResult = __result;
        if (__result != null) f.ObservedClip = __result.source.clip;
        if (__result != null && f.CapturedSources.Contains(__result)) f.OwnedSounds[__result] = f.ObservedClip;
    }
    public static void NativeSoundPrefix(SoundSource __instance, SoundData __0, out bool __state)
    {
        var f = Active;
        __state = f != null && f.Observing && (__0 == f.ReviveData || __0 == f.ExtraData);
        if (f != null && !__state && f.CapturedSources.Contains(__instance)) f.ReusedSounds.Add(__instance);
        if (!__state) return;
        RuntimeAssertions.Require(!f.BaselineSounds.Contains(__instance), "Native audio attempted to reuse a pre-existing playing channel.");
        if (f.CapturedSources.Add(__instance))
        {
            var source = __instance.source;
            f.RecordedAudio[__instance] = source;
            f.SoundParents[__instance] = __instance.transform.parent;
            var clip = source.clip; float volume = source.volume, pitch = source.pitch, spatial = source.spatialBlend;
            bool loop = source.loop, active = __instance.gameObject.activeSelf;
            float time = source.time, min = source.minDistance, max = source.maxDistance, reverb = source.reverbZoneMix;
            var data = __instance.data; var mixer = source.outputAudioMixerGroup;
            f.AudioRestores.Add(() =>
            {
                RuntimeAssertions.Require(__instance != null && object.ReferenceEquals(__instance.source, source), "Fixture audio channel identity changed.");
                RuntimeAssertions.Require(!f.ReusedSounds.Contains(__instance) && object.ReferenceEquals(__instance.transform.parent, f.SoundParents[__instance]), "Fixture audio owner changed; refusing to restore it.");
                UnityEngine.AudioClip ownedClip;
                RuntimeAssertions.Require(!f.OwnedSounds.TryGetValue(__instance, out ownedClip) || source.clip == ownedClip, "Audio channel reused; refusing to restore unrelated channel.");
                source.clip = clip; source.volume = volume; source.pitch = pitch; source.spatialBlend = spatial;
                source.loop = loop; source.outputAudioMixerGroup = mixer; __instance.data = data;
                source.minDistance = min; source.maxDistance = max; source.reverbZoneMix = reverb;
                if (clip != null) source.time = time;
                __instance.gameObject.SetActive(active);
                RuntimeAssertions.Require(source.clip == clip && source.volume == volume && source.spatialBlend == spatial && __instance.gameObject.activeSelf == active, "Audio channel baseline restore failed.");
            });
        }
    }
    public static void NativeSoundPostfix(SoundSource __instance, bool __state)
    {
        var f = Active;
        if (f != null && __state && __instance != null && !f.BaselineSounds.Contains(__instance))
            f.OwnedSounds[__instance] = __instance.source.clip;
    }
    public static void NativeEffectPrefix(Effect __instance, Point __0, UnityEngine.Vector3 __1)
    {
        var f = Active;
        if (f == null || !f.Observing || f.EffectCalls != 1) return;
        Card root = f.Pc.GetRootCard();
        UnityEngine.Vector3 expected = f.Pc.isSynced ? root.renderer.position : root.pos.Position();
        f.RootPosition = object.ReferenceEquals(__0, root.pos) && UnityEngine.Vector3.Distance(__1, expected) < 0.001f;
        if (!f.BaselineEffects.Contains(__instance) && !f.OwnedEffects.Contains(__instance))
        {
            f.OwnedEffects.Add(__instance);
            f.EffectActivations.CaptureCreated(__instance);
            f.ExpectedEffectActivation = __instance;
            f.EffectChildren[__instance] = new System.Collections.Generic.HashSet<UnityEngine.Transform>(__instance.GetComponentsInChildren<UnityEngine.Transform>(true));
            f.EffectParents[__instance] = __instance.transform.parent;
        }
    }
    public static void NativeEffectActivationPrefix(Effect __instance)
    {
        var f = Active;
        if (f == null) return;
        bool expected = f.Observing && object.ReferenceEquals(f.ExpectedEffectActivation, __instance);
        if (object.ReferenceEquals(f.ExpectedEffectActivation, __instance)) f.ExpectedEffectActivation = null;
        f.EffectActivations.ObserveActivation(__instance, expected);
    }
    public static void ResolverPostfix(string __0, bool __result)
    {
        var f = Active;
        if (f == null || !f.Observing || __0 != "fx.pc.teleport+sfx.pc.revive") return;
        f.CommandCalls++; f.CommandResult = __result;
    }
    public static void CuePostfix(string __0, bool __result)
    {
        var f = Active;
        if (f == null || !f.Observing || __0 != "cue.questmod.feature_showcase_pulse") return;
        f.CueCalls++; f.CueResult = __result;
    }
    public static void LayerInitPostfix(LayerDrama __instance)
    {
        var f = Active;
        if (f == null || !f.CreatingLayer) return;
        RuntimeAssertions.Require(f.OwnedLayer == null, "Unexpected second drama layer creation.");
        f.OwnedLayer = __instance;
        f.LayerChildren = new System.Collections.Generic.HashSet<UnityEngine.Transform>(__instance.GetComponentsInChildren<UnityEngine.Transform>(true));
    }

    public void AssertDispatch(bool actual, bool expected, int effects, int sounds)
    {
        RuntimeAssertions.Require(actual == expected, "Resolver/context success result mismatch.");
        RuntimeAssertions.Require(EffectCalls == effects && SoundCalls == sounds, "Native attempts differ: effect=" + EffectCalls + "; sound=" + SoundCalls);
        RuntimeAssertions.Require(effects == 0 || EffectDefaults, "Wrong native PlayEffect defaults.");
        RuntimeAssertions.Require(sounds == 0 || SoundDefaults, "Wrong native PlaySound defaults.");
        if (effects > 0 && !ThrowEffect && EffectResult != null) RuntimeAssertions.Require(RootPosition, "Native effect did not use PC/root-card render position.");
        Context.Log("dispatch: result=" + actual + "; effect=" + EffectCalls + "; sound=" + SoundCalls + "; defaults=" + EffectDefaults + "/" + SoundDefaults + "; root_position=" + RootPosition);
    }

    public void AssertAudio()
    {
        RuntimeAssertions.Require(SoundResult != null && SoundResult.source != null && ObservedClip != null, "Native sound returned no audio channel/clip.");
        var source = SoundResult.source;
        RuntimeAssertions.Require(source.clip == ObservedClip && source.isPlaying && source.enabled && source.gameObject.activeInHierarchy && !source.mute && source.volume > 0f, "Native audio channel is not playing the observed audible-volume clip.");
        RuntimeAssertions.Require(source.spatialBlend == 0f, "Native root-PC audio must be non-spatial.");
        RuntimeAssertions.Require(ClipBelongsTo(SoundResult.data, ObservedClip), "Audio channel clip does not belong to requested native sound data.");
        Context.Log("audio_channel: instance=" + source.GetInstanceID() + "; clip=" + ObservedClip.name + "; samples=" + ObservedClip.samples + "; volume=" + source.volume + "; spatialBlend=" + source.spatialBlend + "; isPlaying=" + source.isPlaying + "; mixer=" + (source.outputAudioMixerGroup == null ? "none" : source.outputAudioMixerGroup.name));
    }
    private static bool ClipBelongsTo(SoundData data, UnityEngine.AudioClip clip)
    {
        if (data == null) return false;
        if (data.clip == clip) return true;
        if (data.variations != null) foreach (var v in data.variations) if (v != null && v.clip == clip) return true;
        return false;
    }
    private void CaptureSoundData(SoundData data)
    {
        float last = data.lastPlayed, alt = data.altLastPlayed;
        int index = data.variationIndex; var variation = data.lastVariation;
        AudioRestores.Add(() =>
        {
            data.lastPlayed = last; data.altLastPlayed = alt; data.variationIndex = index; data.lastVariation = variation;
            RuntimeAssertions.Require(data.lastPlayed == last && data.altLastPlayed == alt && data.variationIndex == index && data.lastVariation == variation, "Native sound data baseline restore failed.");
        });
    }

    public bool ObserveRender()
    {
        var fx = EffectResult;
        if (fx == null || !fx.gameObject.activeInHierarchy) return false;
        bool visible = false;
        foreach (var renderer in fx.GetComponentsInChildren<UnityEngine.Renderer>())
        {
            if (!renderer.enabled || !renderer.isVisible || !renderer.gameObject.activeInHierarchy) continue;
            var sprite = renderer as UnityEngine.SpriteRenderer;
            if (sprite != null && sprite.sprite != null && sprite.color.a > 0f) visible = true;
            var particles = renderer.GetComponent<UnityEngine.ParticleSystem>();
            if (particles != null && particles.particleCount > 0) visible = true;
        }
        if (visible) Context.Log("render_state: fx_instance=" + fx.GetInstanceID() + "; enabled_visible_renderer=true; active_sprite_or_particles=true; position=" + fx.transform.position);
        return visible;
    }

    public void OpenShowcase()
    {
        RuntimeAssertions.Require(System.IO.File.Exists(System.IO.Path.Combine(Context.ModRoot, "LangMod", "EN", "Dialog", "Drama", "drama_quest_drama_feature_showcase.xlsx")), "Showcase xlsx missing.");
        CaptureUiState();
        ShowcaseOpened = true;
        // Native end entry loads/compiles the real book without running its intro mutations.
        LayerDrama.forceJump = "end";
        CreatingLayer = true;
        try
        {
            var returned = LayerDrama.Activate("drama_quest_drama_feature_showcase", "quest_drama_feature_showcase", null, Pc, null, null);
            RuntimeAssertions.Require(object.ReferenceEquals(returned, OwnedLayer), "Layer creation observer did not capture the returned fixture.");
            LayerChildren = new System.Collections.Generic.HashSet<UnityEngine.Transform>(OwnedLayer.GetComponentsInChildren<UnityEngine.Transform>(true));
        }
        finally { CreatingLayer = false; if (OwnedLayer != null) LayerParent = OwnedLayer.parent; }
        RuntimeAssertions.Require(OwnedLayer != null && OwnedLayer.drama != null && OwnedLayer.drama.sequence != null, "Showcase activation failed.");
    }
    public void PlayShowcaseMediaSegment()
    {
        var sequence = OwnedLayer.drama.sequence;
        int cue = -1;
        const string cueScript = "Elin_QuestMod.Drama.DramaRuntime.ResolveRun(\"cue.questmod.feature_showcase_pulse\");";
        const string fxScript = "Elin_QuestMod.Drama.DramaRuntime.ResolveRun(\"fx.pc.teleport+sfx.pc.revive\");";
        for (int i = 0; i < sequence.events.Count; i++)
        {
            var method = sequence.events[i] as DramaEventMethod;
            if (method != null && HasScript(method.action == null ? null : method.action.Target, cueScript, 0))
            { RuntimeAssertions.Require(cue == -1, "Loaded showcase has duplicate cue scripts."); cue = i; }
        }
        RuntimeAssertions.Require(cue >= 0 && cue + 1 < sequence.events.Count, "Native CWL-compiled showcase cue absent.");
        var fx = sequence.events[cue + 1] as DramaEventMethod;
        RuntimeAssertions.Require(fx != null && HasScript(fx.action == null ? null : fx.action.Target, fxScript, 0), "Native showcase media command is not immediately after cue.");
        // Resume only this test-owned loaded sequence, then use the real sequence progression.
        // Retain original native events but bound the owned sequence before any reward/follow-up.
        sequence.events = sequence.events.GetRange(0, cue + 2);
        sequence.isExited = false;
        sequence.manager.SetActive(true);
        sequence.Play(cue);
        LayerChildren = new System.Collections.Generic.HashSet<UnityEngine.Transform>(OwnedLayer.GetComponentsInChildren<UnityEngine.Transform>(true));
        Context.Log("showcase: original CWL closure scripts matched; native sequence resumed at cue index=" + cue + "; test-owned event list ends immediately after media");
    }
    private static bool HasScript(object target, string script, int depth)
    {
        if (target == null || depth > 3) return false;
        var line = target as System.Collections.Generic.Dictionary<string, string>;
        string param;
        if (line != null) return line.TryGetValue("param", out param) && param == script;
        var type = target.GetType();
        if (type.IsPrimitive || target is string || target is System.Delegate || target is UnityEngine.Object) return false;
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            if (HasScript(field.GetValue(target), script, depth + 1)) return true;
        return false;
    }
    public void StopShowcase()
    {
        if (OwnedLayer != null && OwnedLayer.drama != null && OwnedLayer.drama.sequence != null) OwnedLayer.drama.sequence.Exit();
    }

    private void CaptureUiState()
    {
        CaptureField(typeof(EInput), null, "requireConfirmReset");
        CaptureField(typeof(Layer), null, "skipInput");
        CaptureField(typeof(LayerDrama), null, "forceJump");
        foreach (string name in new[] { "keepBGM", "haltPlaylist", "fromBook", "alwaysVisible", "maxBGMVolume" }) CaptureField(typeof(LayerDrama), null, name);
        CaptureField(typeof(SoundManager), null, "forceBGM");
        CaptureField(typeof(SoundManager), SoundManager.current, "haltUpdate");
        CaptureField(EClass.scene.screenElin.GetType(), EClass.scene.screenElin, "focusOption");
        float hintAlpha = EClass.ui.hud.hint.cg.alpha;
        UiRestores.Add(() => EClass.ui.hud.hint.cg.alpha = hintAlpha);
    }
    private void CaptureField(System.Type type, object target, string name)
    {
        var field = type.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy);
        RuntimeAssertions.Require(field != null, "UI state field missing: " + type.Name + "." + name);
        object value = field.GetValue(target);
        UiRestores.Add(() => { field.SetValue(target, value); RuntimeAssertions.Require(object.Equals(field.GetValue(target), value), "UI state restore failed: " + name); });
    }
    private System.Collections.Generic.Dictionary<string, int> SnapshotFlags()
    {
        var result = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
        foreach (var flag in Flags) if (flag.Key.StartsWith(FlagPrefix, System.StringComparison.Ordinal) || flag.Key.StartsWith("questmod.", System.StringComparison.Ordinal)) result.Add(flag.Key, flag.Value);
        return result;
    }
    private void RestoreFlags()
    {
        Pr6DialogFlagRestore.Restore(Flags, EClass.player == null ? null : EClass.player.dialogFlags, ScopedFlags, FlagPrefix);
    }
    public void AssertUnchanged()
    {
        RuntimeAssertions.Require(object.ReferenceEquals(Pc, EClass.pc) && Pc.pos.x == X && Pc.pos.z == Z && Pc.hp == Hp, "Fixture PC identity/position/HP changed.");
        if (ShowcaseOpened) return; // Scope flags are restored and asserted in cleanup for the cue case.
        AssertFlags();
    }
    private void AssertFlags()
    {
        Pr6DialogFlagRestore.RequireLiveDictionary(Flags, EClass.player == null ? null : EClass.player.dialogFlags);
        var current = SnapshotFlags();
        RuntimeAssertions.Require(current.Count == ScopedFlags.Count, "Quest flag count differs from baseline.");
        foreach (var flag in ScopedFlags) RuntimeAssertions.Require(current.ContainsKey(flag.Key) && current[flag.Key] == flag.Value, "Quest flag changed: " + flag.Key);
    }
    public void CleanupOwnedEffect(Effect effect)
    {
        if (effect == null) return;
        RuntimeAssertions.Require(OwnedEffects.Contains(effect), "FX reference was not recorded at creation.");
        RuntimeAssertions.Require(EffectActivations.TryCleanup(effect, owned =>
        {
            // A successfully played pooled effect can have naturally expired before cleanup.
            if (EffectOrigins.ContainsKey(owned) && !Effect.manager.list.Contains(owned)) return;
            System.Collections.Generic.HashSet<UnityEngine.Transform> children;
            RuntimeAssertions.Require(EffectChildren.TryGetValue(owned, out children) && children.SetEquals(owned.GetComponentsInChildren<UnityEngine.Transform>(true)), "Unknown child/changed FX hierarchy; refusing to kill fixture.");
            RuntimeAssertions.Require(object.ReferenceEquals(owned.transform.parent, EffectParents[owned]), "FX parent changed; refusing to kill it.");
            UnityEngine.Vector3 origin;
            RuntimeAssertions.Require(!EffectOrigins.TryGetValue(owned, out origin) || owned.fromV == origin, "FX reused or ownership changed; refusing to kill fixture.");
            owned.Kill();
        }), "FX activation generation changed; refusing to kill a reused effect.");
    }
    public void Dispose()
    {
        if (Disposed) return;
        End();
        var errors = new System.Collections.Generic.List<string>();
        System.Action<System.Action> attempt = action => { try { action(); } catch (System.Exception ex) { errors.Add(ex.ToString()); } };
        try
        {
            attempt(() =>
            {
                if (OwnedLayer != null)
                {
                    StopShowcase();
                    RuntimeAssertions.Require(OwnedLayer.parent != null && object.ReferenceEquals(OwnedLayer.parent, LayerParent), "Owned drama parent changed; refusing to remove it.");
                    RuntimeAssertions.Require(LayerChildren != null && LayerChildren.SetEquals(OwnedLayer.GetComponentsInChildren<UnityEngine.Transform>(true)), "Unknown drama child hierarchy; refusing to remove it.");
                    OwnedLayer.parent.RemoveLayer(OwnedLayer);
                }
            });
            foreach (var effect in OwnedEffects) attempt(() => CleanupOwnedEffect(effect));
            foreach (var pair in OwnedSounds) attempt(() =>
            {
                if (pair.Key == null) return;
                RuntimeAssertions.Require(!ReusedSounds.Contains(pair.Key) && object.ReferenceEquals(pair.Key.source, RecordedAudio[pair.Key]) && object.ReferenceEquals(pair.Key.transform.parent, SoundParents[pair.Key]) && pair.Key.source.clip == pair.Value, "Fixture audio identity/clip/owner changed; refusing to stop unrelated channel.");
                pair.Key.Stop();
            });
            // Each channel restore checks its own identity; unrelated cleanup still proceeds.
            for (int i = AudioRestores.Count - 1; i >= 0; i--) attempt(AudioRestores[i]);
            if (ShowcaseOpened)
            {
                attempt(RestoreFlags);
                for (int i = UiRestores.Count - 1; i >= 0; i--) attempt(UiRestores[i]);
            }
            attempt(AssertFlags);
            attempt(() => RuntimeAssertions.Require(object.ReferenceEquals(EClass.ui.TopLayer, Top), "Top UI layer was not restored."));
            attempt(() => RuntimeAssertions.Require(object.ReferenceEquals(Pc, EClass.pc) && Pc.pos.x == X && Pc.pos.z == Z && Pc.hp == Hp, "Cleanup PC baseline mismatch."));
            attempt(() => RuntimeAssertions.Require(!Effect.manager.list.Exists(e => OwnedEffects.Contains(e)), "Fixture effect still registered after cleanup."));
        }
        finally
        {
            attempt(() => Observer.UnpatchSelf());
            attempt(() =>
            {
                foreach (var method in HarmonyLib.Harmony.GetAllPatchedMethods())
                {
                    var patches = HarmonyLib.Harmony.GetPatchInfo(method);
                    RuntimeAssertions.Require(patches == null || !patches.Owners.Contains(Observer.Id), "Fixture Harmony owner still registered.");
                }
            });
            Active = null;
            Disposed = true;
        }
        Context.Log("cleanup: owned_fx=" + OwnedEffects.Count + "; owned_audio=" + OwnedSounds.Count + "; errors=" + errors.Count);
        if (errors.Count != 0) throw new System.InvalidOperationException("PR6 cleanup failed; stop and reload fixture save: " + string.Join(" | ", errors));
    }
}
