#if RUNTIME_TEST
using System;
using System.Collections.Generic;

public sealed class Pr8ReloadRecord
{
    public string Token, GameId, Name, Deity;
    public int GameInstance, BoxUid, OwnerUid, WaterUid, RejectUid, InvX, InvY;
    public int WaterNum, RejectNum;
    public string WaterState;
    public static string PathFor(RuntimeTestContext ctx)
    {
        return System.IO.Path.Combine(ctx.ModRoot, "tests", "runtime", "_artifacts", "pr8_reload_fixture.json");
    }
}

// These phases deliberately do not save or reload: the sole runtime operator owns that operation.
public sealed class Pr8ReloadPrepareCase : RuntimeCaseBase
{
    public override string Id => "pr8.sleep.existing_box_reload_prepare";
    public override IReadOnlyList<string> Tags => new[] { "pr8", "save_reload_prepare", "destructive" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        Pr8OfferingFixture.Guard();
        RuntimeAssertions.Require(!System.IO.File.Exists(Pr8ReloadRecord.PathFor(ctx)), "An unresolved PR8 reload fixture already exists.");
        var states = new List<Pr8CardState>();
        foreach (Thing t in EClass.pc.things) states.Add(new Pr8CardState(t));
        var owned = new List<Thing>();
        ctx.Set("pr8.owned", owned);
        ctx.Set("pr8.original_items", states);
        ctx.Set("pr8.prepared", false);
        ctx.RegisterRollback("pr8.reload_prepare_cleanup", () =>
        {
            if (!ctx.Get<bool>("pr8.prepared"))
            {
                for (int i = owned.Count - 1; i >= 0; i--) if (!owned[i].isDestroyed) owned[i].Destroy();
                if (System.IO.File.Exists(Pr8ReloadRecord.PathFor(ctx))) System.IO.File.Delete(Pr8ReloadRecord.PathFor(ctx));
            }
            foreach (Pr8CardState state in states) state.AssertUnchanged();
            ctx.Log("prepare cleanup: originals unchanged; staged fixture retained only after successful prepare");
        });
    }
    public override void Execute(RuntimeTestContext ctx)
    {
        var owned = ctx.Get<List<Thing>>("pr8.owned");
        string token = "RUNTIME_TEST.PR8.RELOAD." + Guid.NewGuid().ToString("N");
        Thing box = ThingGen.Create(Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX);
        owned.Add(box);
        RuntimeAssertions.Require(box != null && box.id == Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX && box.IsContainer, "Native box generation failed.");
        box.c_altName = token;
        box.c_idDeity = Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX;
        EClass.pc.AddThing(box, tryStack: false);
        Thing water = ThingGen.Create("water");
        owned.Add(water);
        RuntimeAssertions.Require(water.id == "water", "Water fallback.");
        water.SetNum(37);
        water.SetBlessedState(BlessedState.Normal);
        water.c_altName = token + ".water";
        box.AddThing(water, tryStack: false);
        Thing rejected = ThingGen.Create("log");
        owned.Add(rejected);
        RuntimeAssertions.Require(rejected.id == "log", "Rejected control fallback.");
        rejected.SetNum(9);
        rejected.elements.SetBase(764, 1);
        rejected.c_altName = token + ".reject";
        box.AddThing(rejected, tryStack: false);
        Elin_AutoOfferingAlter.OfferLogic.Process(box);
        RuntimeAssertions.Require(box.c_altName == token && box.c_idDeity == Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX,
            "Old-box metadata was not restored before saving.");
        RuntimeAssertions.Require(water.Num == 37 && water.blessedState == BlessedState.Blessed && rejected.Num == 9,
            "Native water/rejection behavior failed before saving.");
        var record = new Pr8ReloadRecord
        {
            Token = token, GameId = Game.id,
            GameInstance = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(EClass.game),
            BoxUid = box.uid, OwnerUid = EClass.pc.uid, Name = box.c_altName, Deity = box.c_idDeity,
            WaterUid = water.uid, RejectUid = rejected.uid, InvX = box.invX, InvY = box.invY,
            WaterNum = water.Num, RejectNum = rejected.Num, WaterState = water.blessedState.ToString()
        };
        ctx.Set("pr8.record", record);
        string path = Pr8ReloadRecord.PathFor(ctx);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(record, Newtonsoft.Json.Formatting.Indented));
        ctx.Log("phase=prepared_only; fixture=" + token + ";box=" + box.uid + "; manifest=" + path);
    }
    public override void Verify(RuntimeTestContext ctx)
    {
        var record = ctx.Get<Pr8ReloadRecord>("pr8.record");
        RuntimeAssertions.Require(System.IO.File.Exists(Pr8ReloadRecord.PathFor(ctx)) && record.BoxUid != 0, "Reload manifest was not created.");
        foreach (Pr8CardState state in ctx.Get<List<Pr8CardState>>("pr8.original_items")) state.AssertUnchanged();
        ctx.Set("pr8.prepared", true);
        ctx.Log("Operator must save/reload this disposable save, then select existing_box_reload_verify; preparation is not reload coverage.");
    }
}

public sealed class Pr8ReloadVerifyCase : RuntimeCaseBase
{
    public override string Id => "pr8.sleep.existing_box_reload_verify";
    public override IReadOnlyList<string> Tags => new[] { "integration", "pr8", "save_reload_verify", "destructive" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        Pr8OfferingFixture.Guard();
        string path = Pr8ReloadRecord.PathFor(ctx);
        RuntimeAssertions.Require(System.IO.File.Exists(path), "PR8 reload manifest missing; run prepare first.");
        var record = Newtonsoft.Json.JsonConvert.DeserializeObject<Pr8ReloadRecord>(System.IO.File.ReadAllText(path));
        RuntimeAssertions.Require(record != null && record.Token.StartsWith("RUNTIME_TEST.PR8.RELOAD.", StringComparison.Ordinal), "Manifest not owned by PR8 fixture.");
        RuntimeAssertions.Require(Game.id == record.GameId && EClass.pc.uid == record.OwnerUid, "Wrong disposable save/PC loaded.");
        RuntimeAssertions.Require(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(EClass.game) != record.GameInstance,
            "No new native Game instance observed; save/reload has not occurred.");
        Thing box = null;
        foreach (Thing item in EClass.pc.things) if (item.uid == record.BoxUid) box = item;
        RuntimeAssertions.Require(box != null && box.id == Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX && box.c_altName == record.Token,
            "Persisted fixture box UID/source/ownership missing.");
        RuntimeAssertions.Require(box.things.Count == 2, "Foreign content in fixture box; refusing cleanup.");
        foreach (Thing item in box.things)
            RuntimeAssertions.Require((item.uid == record.WaterUid || item.uid == record.RejectUid)
                && item.c_altName.StartsWith(record.Token, StringComparison.Ordinal), "Foreign item in fixture box; refusing cleanup.");
        var states = new List<Pr8CardState>();
        foreach (Thing item in EClass.pc.things) if (item != box) states.Add(new Pr8CardState(item));
        ctx.Set("pr8.record", record);
        ctx.Set("pr8.box", box);
        ctx.RegisterRollback("pr8.reload_verify_cleanup", () =>
        {
            var children = new List<Thing>(box.things);
            foreach (Thing item in children) if (!item.isDestroyed) item.Destroy();
            if (!box.isDestroyed) box.Destroy();
            foreach (Pr8CardState state in states) state.AssertUnchanged();
            RuntimeAssertions.Require(box.isDestroyed, "Persisted fixture box leaked.");
            System.IO.File.Delete(path);
            ctx.Log("reload cleanup: verified fixture box destroyed, manifest removed, originals unchanged; operator must reload baseline");
        });
    }
    public override void Execute(RuntimeTestContext ctx)
    {
        var record = ctx.Get<Pr8ReloadRecord>("pr8.record");
        Thing box = ctx.Get<Thing>("pr8.box");
        AssertRecord(box, record);
        Elin_AutoOfferingAlter.OfferLogic.Process(box);
        AssertRecord(box, record);
        Pr8SourceChecks.AssertPostBoot(ctx);
        ctx.Log("phase=reload_verified; fixture=" + record.Token + "; reacquired_uid=" + box.uid);
    }
    public override void Verify(RuntimeTestContext ctx) { AssertRecord(ctx.Get<Thing>("pr8.box"), ctx.Get<Pr8ReloadRecord>("pr8.record")); }
    private static void AssertRecord(Thing box, Pr8ReloadRecord record)
    {
        // Check existence and both quantities on every call, including after Process.
        // Iterating only surviving children could otherwise pass after either child vanished.
        Pr8ReloadContents.Assert(box.things, item => item.uid, item => item.Num,
            record.WaterUid, record.RejectUid, record.WaterNum, record.RejectNum);
        RuntimeAssertions.Require(box.c_altName == record.Name && box.c_idDeity == record.Deity
            && box.parent == EClass.pc && box.GetRootCard() == EClass.pc && box.invX == record.InvX && box.invY == record.InvY,
            "Reload changed old box metadata/owner/root/slot.");
        foreach (Thing item in box.things)
        {
            RuntimeAssertions.Require(item.parent == box && item.GetRootCard() == EClass.pc && !item.isDestroyed,
                "Reload fixture item parent/root/destroyed mismatch.");
            if (item.uid == record.WaterUid)
                RuntimeAssertions.Require(item.blessedState.ToString() == record.WaterState, "Persisted water blessing mismatch.");
        }
    }
}
#endif
