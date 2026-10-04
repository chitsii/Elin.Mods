#if RUNTIME_TEST
using System;
using System.Collections.Generic;

public sealed class Pr8CardOwnership : IDisposable
{
    private static Pr8CardOwnership active;
    private readonly Pr8OwnedTree<Card> ledger;
    private readonly HarmonyLib.Harmony harmony;
    private readonly Action<Card> onAllocation;
    private bool installed;
    public Pr8CardOwnership(string token, Action<Card> onAllocation = null)
    {
        this.onAllocation = onAllocation;
        int floor = EClass.game.cards.uidNext;
        RuntimeAssertions.Require(floor > 0, "Native next UID unavailable.");
        ledger = new Pr8OwnedTree<Card>(floor, card => card.uid, Children);
        ledger.CaptureOriginal(EClass.pc);
        foreach (Chara card in EClass._map.charas) ledger.CaptureOriginal(card);
        foreach (Thing card in EClass._map.things) ledger.CaptureOriginal(card);
        var manager = EClass.game.cards;
        foreach (Chara card in manager.globalCharas.Values) ledger.CaptureOriginal(card);
        foreach (Chara card in manager.listAdv) ledger.CaptureOriginal(card);
        foreach (Thing card in manager.listPackage) ledger.CaptureOriginal(card);
        ledger.CaptureOriginal(manager.container_shipping);
        ledger.CaptureOriginal(manager.container_deliver);
        ledger.CaptureOriginal(manager.container_deposit);
        harmony = new HarmonyLib.Harmony("runtime_test.pr8.ownership." + token);
    }
    private static IEnumerable<Card> Children(Card card)
    {
        foreach (Thing child in card.things) yield return child;
        var actor = card as Chara;
        if (actor != null && actor.held != null) yield return actor.held;
    }
    public void Install()
    {
        RuntimeAssertions.Require(active == null, "Another PR8 ownership observer is active.");
        active = this; installed = true;
        Patch("Create", new[] { typeof(string), typeof(int), typeof(int) }, "BeforeCreate", null);
        Patch("Split", new[] { typeof(int) }, "BeforeSplit", "AfterSplit");
        Patch("Destroy", Type.EmptyTypes, "BeforeDestroy", null);
    }
    private void Patch(string method, Type[] args, string prefix, string postfix)
    {
        var target = HarmonyLib.AccessTools.DeclaredMethod(typeof(Card), method, args);
        RuntimeAssertions.Require(target != null, "Native ownership target missing: Card." + method);
        harmony.Patch(target,
            prefix == null ? null : new HarmonyLib.HarmonyMethod(typeof(Pr8CardOwnership), prefix) { priority = HarmonyLib.Priority.First },
            postfix == null ? null : new HarmonyLib.HarmonyMethod(typeof(Pr8CardOwnership), postfix) { priority = HarmonyLib.Priority.First });
    }
    private static void BeforeCreate(Card __instance)
    {
        if (active == null) return;
        active.ledger.ObserveAllocation(__instance);
        if (active.onAllocation != null) active.onAllocation(__instance);
    }
    private static void BeforeSplit(Card __instance) { if (active != null) active.ledger.GuardTree(__instance); }
    private static void AfterSplit(Card __instance, Thing __result)
    {
        if (active == null) return;
        active.ledger.ObserveSplit(__instance, __result);
        if (active.onAllocation != null) active.onAllocation(__result);
    }
    private static void BeforeDestroy(Card __instance)
    {
        // Guard every recursive native invocation too: callbacks may insert a foreign child after the outer precheck.
        if (active != null) active.ledger.GuardTree(__instance);
    }
    public void RequireOwned(Card card) { ledger.RequireOwned(card); }
    public void AuthorizePersisted(Card card) { ledger.AuthorizePersisted(card); }
    public void GuardTree(Card card) { ledger.GuardTree(card); }
    public void GuardAll()
    {
        foreach (Card card in ledger.Owned) if (!card.isDestroyed) ledger.GuardTree(card);
    }
    public void DestroyOwnedExcept(params Card[] retained)
    {
        // Complete the ownership precheck before making any deletion.
        GuardAll();
        var keep = new HashSet<Card>(retained, Pr8ReferenceComparer<Card>.Instance);
        var cards = new List<Card>(ledger.Owned);
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            Card card = cards[i];
            if (card.isDestroyed || keep.Contains(card)) continue;
            var actor = card as Chara;
            if (actor != null) { actor.isSummon = true; actor.isDead = false; if (actor.conSleep != null) actor.conSleep.slept = false; }
            card.Destroy();
            RuntimeAssertions.Require(card.isDestroyed, "Owned card cleanup failed: " + card.uid);
        }
    }
    public void RequireLiveOnly(params Card[] expected)
    {
        GuardAll();
        var keep = new HashSet<Card>(expected, Pr8ReferenceComparer<Card>.Instance);
        foreach (Card card in ledger.Owned)
            RuntimeAssertions.Require(card.isDestroyed || keep.Contains(card), "Unexpected live generated card in reload staging: " + card.uid);
    }
    public void Dispose()
    {
        if (!installed) return;
        try { harmony.UnpatchSelf(); }
        finally { if (active == this) active = null; installed = false; }
        foreach (var method in HarmonyLib.Harmony.GetAllPatchedMethods())
        {
            var info = HarmonyLib.Harmony.GetPatchInfo(method);
            if (info == null) continue;
            foreach (string owner in info.Owners)
                RuntimeAssertions.Require(owner != harmony.Id, "Ownership observer leaked: " + method.Name);
        }
    }
}
#endif
