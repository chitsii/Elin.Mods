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
        return 0;
    }
}
