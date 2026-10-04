#if RUNTIME_TEST
using System;
using System.Collections.Generic;

// Reload phases deliberately retain/remove their three owned Things, but must not change Player data.
public sealed class Pr8ReloadPlayerPreservation
{
    private readonly RuntimeTestContext ctx;
    private readonly Player player;
    private readonly string before;
    private readonly string token = "reload." + Guid.NewGuid().ToString("N");
    public Pr8ReloadPlayerPreservation(RuntimeTestContext context)
    {
        ctx = context;
        player = EClass.player;
        before = Newtonsoft.Json.JsonConvert.SerializeObject(player, IO.dpFormat, IO.dpSetting);
    }
    public void AssertUnchanged(string stage)
    {
        RuntimeAssertions.Require(EClass.player == player, "Reload phase changed selected Player.");
        string after = Newtonsoft.Json.JsonConvert.SerializeObject(player, IO.dpFormat, IO.dpSetting);
        if (before != after)
        {
            string directory = System.IO.Path.Combine(ctx.ModRoot, "tests", "runtime", "_artifacts", "pr8-preservation", token);
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, stage + ".player.before.json"), before);
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, stage + ".player.after.json"), after);
            List<string> changes = Pr8StateDiff.Describe(before, after);
            System.IO.File.WriteAllLines(System.IO.Path.Combine(directory, stage + ".player.diff.txt"), changes);
            ctx.Log("preservation_failed: stage=" + stage + ";kind=reload_player;diff=" + directory + ";first=" + changes[0]);
        }
        RuntimeAssertions.Require(after == before, "Original serialized reload Player changed at " + stage + "; stop and reload baseline.");
    }
}
#endif
