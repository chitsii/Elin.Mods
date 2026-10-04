using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

// Offline graph fixtures use actual native types without constructors/Unity/game execution.
public static class NiPr7OwnershipChecks
{
    private static T Raw<T>(int uid) where T : Card
    {
        var card = (T)FormatterServices.GetUninitializedObject(typeof(T));
        card._ints = new int[30];
        card.uid = uid;
        card.things = new ThingContainer();
        return card;
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Console.WriteLine("PASS " + name);
    }
    public static int Main()
    {
        try { return Run(); }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL type=" + ex.GetType().FullName);
            try { Console.WriteLine("message=" + ex.Message); } catch { }
            if (ex.InnerException != null) Console.WriteLine("inner=" + ex.InnerException.GetType().FullName + ":" + ex.InnerException.Message);
            return 1;
        }
    }
    private static int Run()
    {
        var root = Raw<Chara>(1);
        var box = Raw<Thing>(2);
        var child = Raw<Thing>(3);
        var owned = new Dictionary<Thing, int> { { box, 2 }, { child, 3 } };
        root.things.Add(box); box.parent = root;
        box.things.Add(child); child.parent = box;
        Check(NiPr7Ownership.TreeIsOwned(root, owned) && NiPr7Ownership.RootIsOwned(child, root, null, owned), "known_descendants_allowed");
        var unknown = Raw<Thing>(4);
        box.things.Add(unknown); unknown.parent = box;
        Check(!NiPr7Ownership.TreeIsOwned(root, owned), "unknown_grandchild_blocks_recursive_destroy");
        box.things.Remove(unknown);
        var impostor = Raw<Thing>(3);
        box.things[0] = impostor; impostor.parent = box;
        Check(!NiPr7Ownership.TreeIsOwned(root, owned), "same_uid_different_reference_rejected");
        box.things[0] = child;
        child.uid = 99;
        Check(!NiPr7Ownership.TreeIsOwned(root, owned), "same_reference_changed_uid_rejected");
        child.uid = 3;
        child.parent = Raw<Chara>(5);
        Check(!NiPr7Ownership.RootIsOwned(child, root, null, owned), "existing_actor_owner_rejected");
        child.parent = unknown;
        Check(!NiPr7Ownership.RootIsOwned(child, root, null, owned), "unknown_container_owner_rejected");
        child.parent = box; box.parent = child;
        Check(!NiPr7Ownership.RootIsOwned(child, root, null, owned), "cyclic_owner_chain_rejected");
        CheckSharedState();
        return 0;
    }

    private static bool Rejects(Action action)
    {
        try { action(); return false; }
        catch (InvalidOperationException) { return true; }
    }

    // Real native Rand/Stats getters/Set; no game, Unity, SetLv or Chara constructors.
    private static void CheckSharedState()
    {
        var outer = new NiPr7SharedNativeState();
        try
        {
            var randomField = typeof(Rand).GetField("_random", System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var baseSeedField = typeof(Rand).GetField("baseSeed", System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var stats = new List<Stats>();
            foreach (var field in typeof(Stats).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                if (typeof(Stats).IsAssignableFrom(field.FieldType)) stats.Add((Stats)field.GetValue(null));
            for (int i = 0; i < stats.Count; i++)
            {
                stats[i].raw = i % 2 == 0 ? null : new[] { i, 7, 11 };
                stats[i].rawIndex = 50 + i;
            }
            var originalCc = Raw<Chara>(90);
            BaseStats.CC = originalCc; // deliberately independent of every Stats binding
            Rand.SetSeed(1234);
            var random = (Random)randomField.GetValue(null);
            var state = new NiPr7SharedNativeState();
            state.RequireUnchanged();
            Check(ReferenceEquals(BaseStats.CC, originalCc), "shared_capture_does_not_call_rebinding_getters");

            Rand.rnd(1000);
            Check(ReferenceEquals(randomField.GetValue(null), random) && Rejects(state.RequireUnchanged),
                "rng_reference_only_restore_is_insufficient");
            state.RestoreAndAssert();
            var expected = new Random(1234);
            bool sequence = true;
            for (int i = 0; i < 64; i++) sequence &= random.Next() == expected.Next();
            Check(sequence, "rng_cursors_and_array_restore_next_64_draws");
            state.RestoreAndAssert();

            System.Reflection.FieldInfo seedField = null;
            foreach (var field in typeof(Random).GetFields(System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                if (field.FieldType == typeof(int[])) seedField = field;
            var originalArray = (int[])seedField.GetValue(random);
            originalArray[9] ^= 1; // cursor/reference unchanged; state still differs
            Check(Rejects(state.RequireUnchanged), "rng_seed_contents_change_detected_without_cursor_change");
            state.RestoreAndAssert();
            seedField.SetValue(random, originalArray.Clone()); // same values, wrong array identity
            bool replaced = Rejects(state.RequireUnchanged);
            state.RestoreAndAssert();
            Check(replaced && ReferenceEquals(seedField.GetValue(random), originalArray),
                "rng_seed_array_identity_restored_after_replacement");

            Rand.SetSeed(9876);
            baseSeedField.SetValue(null, 9876);
            Rand.rnd(1000);
            state.RestoreAndAssert();
            Check(ReferenceEquals(randomField.GetValue(null), random), "native_setseed_replacement_and_baseseed_restored");

            var fixture = Raw<Chara>(91);
            fixture._cints = new int[30];
            var mana = fixture.mana;
            Check(ReferenceEquals(mana, Stats.Mana) && ReferenceEquals(mana.raw, fixture._cints)
                && ReferenceEquals(BaseStats.CC, fixture) && Rejects(state.RequireUnchanged),
                "native_mana_getter_shared_binding_contamination_detected");
            state.RestoreAndAssert();
            foreach (var stat in stats) stat.Set(fixture._cints, 3, fixture);
            state.RestoreAndAssert();
            Check(ReferenceEquals(BaseStats.CC, originalCc) && Stats.Hunger.raw == null
                && Stats.Hunger.rawIndex == 50, "all_nine_stats_restore_null_raw_and_independent_CC");

            try
            {
                Rand.rnd(1000); // advances the captured original, before replacement
                fixture.mana.Set(fixture._cints, 4, fixture);
                throw new NiPr7InjectedException();
            }
            catch (NiPr7InjectedException) { }
            finally { state.RestoreAndAssert(); }
            state.RequireUnchanged();
            Check(true, "exception_before_setseed_restores_original_internal_state");

            BaseStats.CC = null;
            var nullCc = new NiPr7SharedNativeState();
            try
            {
                Rand.SetSeed(777);
                fixture.stamina.Set(fixture._cints, 6, fixture);
                throw new NiPr7InjectedException();
            }
            catch (NiPr7InjectedException) { }
            finally { nullCc.RestoreAndAssert(); }
            Check(BaseStats.CC == null, "exception_after_setseed_restores_bindings_and_null_CC");

            var context = new RuntimeTestContext("offline.shared_state", "offline");
            var order = new List<string>();
            context.RegisterRollback("shared", () => { nullCc.RestoreAndAssert(); order.Add("shared"); });
            context.RegisterRollback("baseline", () => { order.Add("baseline"); fixture.mana.Set(fixture._cints, 7, fixture); throw new NiPr7InjectedException(); });
            context.RegisterRollback("fixtures", () => { order.Add("fixtures"); Rand.SetSeed(555); fixture.stamina.Set(fixture._cints, 8, fixture); });
            var errors = context.RunRollback(); // actual unchanged shared rollback runner
            nullCc.RequireUnchanged();
            nullCc.RestoreAndAssert(); // idempotent second rollback
            Check(errors.Count == 1 && string.Join(",", order) == "fixtures,baseline,shared",
                "shared_rollback_runs_last_despite_prior_cleanup_exception");
        }
        finally { outer.RestoreAndAssert(); }
    }
}
