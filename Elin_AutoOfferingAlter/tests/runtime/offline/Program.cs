using System;

internal static class Program
{
    private sealed class Child
    {
        internal readonly int Uid, Num;
        internal Child(int uid, int num) { Uid = uid; Num = num; }
    }
    private static void Assert(Child[] children)
    {
        // This is the exact helper invoked by AssertRecord before and after real Process.
        Pr8ReloadContents.Assert(children, child => child.Uid, child => child.Num, 101, 202, 37, 9);
    }
    private static void Reject(string label, Child[] children, string expectedMessage)
    {
        try { Assert(children); }
        catch (InvalidOperationException ex)
        {
            if (ex.Message != expectedMessage) throw new Exception(label + ": wrong failure: " + ex.Message);
            Console.WriteLine("PASS: " + label);
            return;
        }
        throw new Exception(label + ": counterexample falsely passed.");
    }
    private static void Main()
    {
        Assert(new[] { new Child(101, 37), new Child(202, 9) });
        Assert(new[] { new Child(202, 9), new Child(101, 37) });
        Console.WriteLine("PASS: intact children in either order");
        const string count = "Reload fixture must retain exactly two children.";
        Reject("water disappeared", new[] { new Child(202, 9) }, count);
        Reject("rejected item disappeared", new[] { new Child(101, 37) }, count);
        Reject("both children disappeared", new Child[0], count);
        Reject("extra child", new[] { new Child(101, 37), new Child(202, 9), new Child(303, 1) }, count);
        Reject("water replaced while count remains two", new[] { new Child(303, 37), new Child(202, 9) }, "Persisted water UID missing.");
        Reject("rejected item replaced while count remains two", new[] { new Child(101, 37), new Child(303, 9) }, "Persisted rejected UID missing.");
        Reject("duplicated water UID", new[] { new Child(101, 37), new Child(101, 9) }, "Persisted rejected UID missing.");
        Reject("water quantity changed", new[] { new Child(101, 36), new Child(202, 9) }, "Persisted water quantity mismatch.");
        Reject("rejected quantity changed", new[] { new Child(101, 37), new Child(202, 8) }, "Persisted rejected quantity mismatch.");
        Console.WriteLine("PASS: native-free reload conservation counterexamples (game/CWL not executed).");
    }
}
