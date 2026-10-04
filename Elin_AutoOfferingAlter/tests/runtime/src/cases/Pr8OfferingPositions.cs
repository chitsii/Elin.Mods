#if RUNTIME_TEST
using System;
using System.Reflection;

public sealed class Pr8OfferingPositionState
{
    private readonly Thing owner;
    private readonly Point point;
    private readonly int x, z, saveX, saveZ, uid, num;
    private readonly ICardParent parent;
    private readonly Card root;
    public Pr8OfferingPositionState(Thing owner)
    {
        this.owner = owner;
        SaveCallback(owner); // Owned Card data callback only; never save a Game or write a save file.
        point = owner.pos; x = point.x; z = point.z; saveX = owner._x; saveZ = owner._z;
        uid = owner.uid; num = owner.Num; parent = owner.parent; root = owner.GetRootCard();
    }
    public void AssertUnchanged(bool invokeSaveCallback = false)
    {
        RequireUnchanged();
        if (invokeSaveCallback) { SaveCallback(owner); RequireUnchanged(); }
    }
    private void RequireUnchanged()
    {
        RuntimeAssertions.Require(ReferenceEquals(owner.pos, point) && point.x == x && point.z == z
            && owner.invX == x && owner.invY == z && owner._x == saveX && owner._z == saveZ
            && owner.uid == uid && owner.Num == num && ReferenceEquals(owner.parent, parent) && owner.GetRootCard() == root,
            "Offering changed owner Point/inventory/save coordinates/identity/quantity.");
    }
    private static void SaveCallback(Card owner)
    {
        var method = typeof(Card).GetMethod("_OnSerializing", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        RuntimeAssertions.Require(method != null, "Native Card save coordinate callback missing.");
        method.Invoke(owner, new object[] { new System.Runtime.Serialization.StreamingContext() });
    }
    public static bool ScopeIsActive()
    {
        return ScopedOwnerPoint() != null;
    }
    public static Point ScopedOwnerPoint()
    {
        Type scope = typeof(Elin_AutoOfferingAlter.OfferLogic).Assembly
            .GetType("Elin_AutoOfferingAlter.OfferingEffectScope`1", throwOnError: true).MakeGenericType(typeof(Point));
        object value = scope.GetField("current", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        return value == null ? null : (Point)scope.GetField("ownerPoint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
    }
}

public sealed class Pr8OfferingFxState
{
    public Point Original;
    public bool OwnerPoint;
}
#endif
