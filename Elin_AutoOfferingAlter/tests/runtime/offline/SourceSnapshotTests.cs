using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

internal static class SourceSnapshotTests
{
    private sealed class Definition
    {
        public UnityEngine.Color Color;
        public UnityEngine.Color[] Colors;
        public int[,] Grid;
        public readonly Dictionary<string, int> Map = new Dictionary<string, int> { { "offer", 10 } };
        [NonSerialized] public int RuntimeFlag = 7;
        [Newtonsoft.Json.JsonIgnore] public int IgnoredBySave = 11;
        private int hidden = 13;
        public Definition Next;
        public int GetterCalls;
        public object DangerousGetter { get { GetterCalls++; throw new InvalidOperationException("Source property must never execute."); } }
        public void ChangeHidden() { hidden++; }
    }
    internal static void Run()
    {
        try { RunChecks(); }
        catch (Exception ex)
        {
            for (Exception error = ex; error != null; error = error.InnerException)
                Console.WriteLine("FAIL source snapshot: " + error.GetType().FullName + ": " + error.Message);
            Environment.Exit(1);
        }
    }
    private static void RunChecks()
    {
        // Actual native Color value struct only; no engine object constructors or native rendering calls.
        var color = new UnityEngine.Color(0.2f, 0.4f, 0.6f, 0.8f);
        var a = new[] { color }; var b = (UnityEngine.Color[])a.Clone();
        var snapshot = new Pr8SourceSnapshot();
        Require(snapshot.Values(a) == snapshot.Values(b), "Equal Color arrays falsely differ.");
        b[0].r = 0.9f;
        Require(snapshot.Values(a) != snapshot.Values(b), "Color component mutation was lost.");
        bool oldFailed = false;
        try { Newtonsoft.Json.JsonConvert.SerializeObject(color); }
        catch (Newtonsoft.Json.JsonSerializationException) { oldFailed = true; }
        Require(oldFailed, "Actual default Color serialization regression was not reproduced.");
        Console.WriteLine("PASS: real Color default serialization fails; field snapshots retain all RGBA values and detect mutation");

        foreach (string fault in new[] { "color r", "color g", "color b", "color a", "array", "array reference", "shape", "dictionary", "private field", "nonserialized field", "save-ignored field", "cycle reference" })
        {
            var d = new Definition { Color = color, Colors = new[] { color }, Grid = new[,] { { 1, 2 }, { 3, 4 } } };
            d.Next = d;
            var state = new Pr8SourceSnapshot();
            string before = state.State(d);
            string unchanged = state.State(d);
            Require(before == unchanged && d.GetterCalls == 0 && before.Contains("$ref"), "Snapshot evaluated getters/lost cycles or unstable unchanged data.");
            if (fault == "color r") d.Color.r++;
            if (fault == "color g") d.Color.g++;
            if (fault == "color b") d.Color.b++;
            if (fault == "color a") d.Color.a++;
            if (fault == "array") d.Colors[0].a++;
            if (fault == "array reference") d.Colors = (UnityEngine.Color[])d.Colors.Clone();
            if (fault == "shape") d.Grid = new[,] { { 1, 2, 3, 4 } };
            if (fault == "dictionary") d.Map["offer"]++;
            if (fault == "private field") d.ChangeHidden();
            if (fault == "nonserialized field") d.RuntimeFlag++;
            if (fault == "save-ignored field") d.IgnoredBySave++;
            if (fault == "cycle reference") d.Next = new Definition();
            Require(before != state.State(d) && d.GetterCalls == 0, "Source mutation was not detected: " + fault);
            Console.WriteLine("PASS: detects source " + fault + " without property recursion or loop omission");
        }
        // Actual SourceThing.Row field metadata, opaque allocation to avoid any native constructor/lazy property.
        var row = (SourceThing.Row)FormatterServices.GetUninitializedObject(typeof(SourceThing.Row));
        row.id = "snapshot.control"; row.name = "control"; row.factory = new[] { "self" }; row.recipeKey = new[] { "*" };
        row.origin = row;
        var native = new Pr8SourceSnapshot();
        string raw = native.State(row);
        Require(raw == native.State(row) && row._model == null, "Native SourceRow snapshot evaluated model or changed state.");
        row.name = "changed";
        Require(raw != native.State(row), "Actual SourceThing.Row scalar mutation was not detected.");
        Console.WriteLine("PASS: actual SourceThing.Row fields/cycle preserved; model getter uncalled; source-name mutation rejected");
        SpriteCaches();
    }
    private static void SpriteCaches()
    {
        var a = (SourceThing.Row)FormatterServices.GetUninitializedObject(typeof(SourceThing.Row));
        var b = (SourceThing.Row)FormatterServices.GetUninitializedObject(typeof(SourceThing.Row));
        a.id = "chest6"; b.id = "custom"; a._tiles = b._tiles = new[] { 1 };
        Pr8SourceSpriteCache.AssertIndependent(a, b);
        b.sprites = new UnityEngine.Sprite[1, 1];
        Pr8SourceSpriteCache.AssertIndependent(a, b);
        a.sprites = new UnityEngine.Sprite[1, 1];
        Pr8SourceSpriteCache.AssertIndependent(a, b);
        Console.WriteLine("PASS: native lazy sprites cache may be independently uninitialized/initialized without reading GetSprite");
        b.sprites = a.sprites;
        RejectSprites(a, b, "shared cache");
        b.sprites = new UnityEngine.Sprite[2, 1];
        RejectSprites(a, b, "skin shape");
        b.sprites = new UnityEngine.Sprite[1, 2];
        RejectSprites(a, b, "tile shape");
        b.sprites = new UnityEngine.Sprite[1, 1];
        var state = new Pr8SourceSnapshot();
        string before = state.State(b);
        b.sprites = new UnityEngine.Sprite[1, 1];
        Require(before != state.State(b), "Init no-op comparison lost runtime cache replacement.");
        var field = typeof(SourceThing.Row).GetField("sprites");
        Require(Pr8SourceSpriteCache.IsField(field) && !Pr8SourceSpriteCache.IsField(typeof(SourceThing.Row).GetField("tiles")),
            "Cache exception must target only native RenderRow.sprites metadata.");
        Console.WriteLine("PASS: Init no-op still detects cache replacement; tiles definition is not excluded");
    }
    private static void RejectSprites(SourceThing.Row a, SourceThing.Row b, string fault)
    {
        try { Pr8SourceSpriteCache.AssertIndependent(a, b); }
        catch (InvalidOperationException) { Console.WriteLine("PASS: rejects native sprite " + fault); return; }
        throw new InvalidOperationException("Sprite cache counterexample passed: " + fault);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
