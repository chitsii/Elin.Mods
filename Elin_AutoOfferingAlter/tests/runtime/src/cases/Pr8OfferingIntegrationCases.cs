#if RUNTIME_TEST
using System;
using System.Collections.Generic;

public abstract class Pr8OfferingCase : RuntimeCaseBase
{
    public override IReadOnlyList<string> Tags => new[] { "integration", "pr8", "isolated_player" };
    public override void Prepare(RuntimeTestContext ctx) { Pr8OfferingFixture.Create(ctx); }
    public override void Verify(RuntimeTestContext ctx) { ctx.Get<Pr8OfferingFixture>("pr8.fixture").AssertMetadata(); }
}

public sealed class Pr8ConsumeWaterRejectCase : Pr8OfferingCase
{
    public override string Id => "pr8.sleep.consume_water_reject";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr8OfferingFixture>("pr8.fixture");
        f.Actor.SetFaith("ehekatl");
        f.Actor.elements.SetBase(306, 100);
        f.Actor.elements.SetBase(85, 1);
        Thing water = f.Item("water", 37);
        water.SetBlessedState(BlessedState.Normal);
        Thing fish = f.Offering("fish", 4);
        Thing meat = f.Offering("meat", 4);
        Thing rejected = f.Offering("meat", 9);
        rejected.elements.SetBase(764, 1);
        RuntimeAssertions.Require(f.Oracle.CanOffer(f.Actor, water), "Native water precondition failed.");
        RuntimeAssertions.Require(!f.Oracle.CanOffer(f.Actor, rejected), "Native rejection precondition failed.");
        int beforeExp = f.Actor.elements.GetOrCreateElement(85).vExp;
        f.LogState("before");
        Elin_AutoOfferingAlter.OfferLogic.Process(f.Box);
        f.LogState("after");
        f.AssertRetained(water, 37);
        RuntimeAssertions.Require(water.blessedState == BlessedState.Blessed, "Water was not natively blessed.");
        RuntimeAssertions.Require(f.Observer.CallsFor(water.uid) == 1 && f.Observer.SplitsFor(water.uid) == 0,
            "Water must reach OnOffer once without splitting.");
        RuntimeAssertions.Require(fish.isDestroyed && meat.isDestroyed, "Native fish/meat offerings were not consumed.");
        RuntimeAssertions.Require(f.Observer.NormalOfferingCalls >= 2, "Native _OnOffer was not reached for fish/meat.");
        RuntimeAssertions.Require(f.Actor.elements.GetOrCreateElement(85).vExp != beforeExp || f.Actor.Evalue(85) > 1,
            "Uncapped fixture piety did not change.");
        f.AssertRetained(rejected, 9);
        RuntimeAssertions.Require(f.Observer.CallsFor(rejected.uid) == 0, "Native rejected item reached OnOffer.");
        f.Observer.AssertActorAndDeity(f.Actor.uid, "ehekatl");
    }
}

public sealed class Pr8SplitNonconsumeStopCase : Pr8OfferingCase
{
    public override string Id => "pr8.sleep.split_nonconsume_stop";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr8OfferingFixture>("pr8.fixture");
        f.Actor.SetFaith("eyth");
        f.Actor.elements.SetBase(1228, 0);
        Thing item = f.Offering("meat", 1);
        int batch = f.SplitSize(item);
        item.SetNum(batch * 2 + 1);
        Thing sentinel = f.Offering("meat", 2);
        sentinel.SetBlessedState(BlessedState.Cursed);
        int total = item.Num;
        Thing oracleItem = f.Offering("meat", 1, attach: false);
        f.Oracle.OnOffer(f.Actor, oracleItem);
        RuntimeAssertions.Require(!oracleItem.isDestroyed && oracleItem.Num == 1,
            "Native Eyth OnOffer did not take the non-consuming branch.");
        f.Observer.Reset();
        f.LogState("before");
        Elin_AutoOfferingAlter.OfferLogic.Process(f.Box);
        f.LogState("after");
        RuntimeAssertions.Require(f.Observer.SplitsFor(item.uid) == 1, "Expected one native Split.");
        RuntimeAssertions.Require(f.Observer.OfferCalls.Count == 1 && f.Observer.CallsFor(sentinel.uid) == 0,
            "Non-consuming split must stop the batch and later items.");
        RuntimeAssertions.Require(f.SumNamed(item.c_altName) == total, "Split rejection lost or duplicated quantity.");
        f.AssertAllNamedInBox(item.c_altName);
        f.AssertRetained(sentinel, 2);
    }
}

public sealed class Pr8UnsplitNonconsumeCase : Pr8OfferingCase
{
    public override string Id => "pr8.sleep.nonconsume_unsplit_stop";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr8OfferingFixture>("pr8.fixture");
        f.Actor.SetFaith("eyth");
        f.Actor.elements.SetBase(1228, 0);
        Thing item = f.Offering("meat", 1);
        Thing sentinel = f.Offering("meat", 3);
        sentinel.SetBlessedState(BlessedState.Cursed);
        Elin_AutoOfferingAlter.OfferLogic.Process(f.Box);
        f.AssertRetained(item, 1);
        f.AssertRetained(sentinel, 3);
        RuntimeAssertions.Require(f.Observer.OfferCalls.Count == 1 && f.Observer.SplitsFor(item.uid) == 0
            && f.Observer.CallsFor(sentinel.uid) == 0, "Unsplit native refusal did not stop later offerings.");
    }
}

public abstract class Pr8ActorChangeCase : Pr8OfferingCase
{
    protected abstract string Change { get; }
    public override IReadOnlyList<string> Tags => new[] { "integration", "pr8", "fault_injection", "isolated_player" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr8OfferingFixture>("pr8.fixture");
        f.Actor.SetFaith("ehekatl");
        Thing item = f.Offering("fish", 1);
        item.SetNum(f.SplitSize(item) * 2 + 1);
        Thing sentinel = f.Offering("meat", 3);
        int before = item.Num;
        f.Observer.AfterFirstOffer = () =>
        {
            if (Change == "dead") f.Actor.isDead = true;
            else if (Change == "pc") f.TestPlayer.chara = f.OtherActor;
            else f.Actor.SetFaith("eyth");
        };
        f.LogState("before");
        Elin_AutoOfferingAlter.OfferLogic.Process(f.Box);
        f.LogState("after");
        RuntimeAssertions.Require(f.Observer.OfferCalls.Count == 1, "Actor change did not stop further native OnOffer.");
        Pr8OfferCall call = f.Observer.OfferCalls[0];
        RuntimeAssertions.Require(call.DestroyedAfter, "First offering was not consumed before injection.");
        RuntimeAssertions.Require(f.SumNamed(item.c_altName) == before - call.Num, "Actor-change quantity accounting failed.");
        RuntimeAssertions.Require(f.Observer.CallsFor(sentinel.uid) == 0, "Actor change did not stop sentinel.");
        f.AssertAllNamedInBox(item.c_altName);
        f.AssertRetained(sentinel, 3);
    }
}
public sealed class Pr8StopDeadCase : Pr8ActorChangeCase
{
    public override string Id => "pr8.sleep.stop_on_actor_dead";
    protected override string Change => "dead";
}
public sealed class Pr8StopPcCase : Pr8ActorChangeCase
{
    public override string Id => "pr8.sleep.stop_on_pc_change";
    protected override string Change => "pc";
}
public sealed class Pr8StopFaithCase : Pr8ActorChangeCase
{
    public override string Id => "pr8.sleep.stop_on_faith_change";
    protected override string Change => "faith";
}

public sealed class Pr8ExceptionRecoveryCase : Pr8OfferingCase
{
    public override string Id => "pr8.sleep.exception_split_recovery";
    public override IReadOnlyList<string> Tags => new[] { "integration", "pr8", "fault_injection", "isolated_player" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr8OfferingFixture>("pr8.fixture");
        f.Actor.SetFaith("ehekatl");
        Thing item = f.Offering("fish", 1);
        item.SetNum(f.SplitSize(item) * 2 + 1);
        int before = item.Num;
        f.Observer.ThrowBeforeFirstOffer = true;
        bool caught = false;
        try { Elin_AutoOfferingAlter.OfferLogic.Process(f.Box); }
        catch (Pr8InjectedOfferingException) { caught = true; }
        RuntimeAssertions.Require(caught && f.Observer.SplitsFor(item.uid) == 1, "Exception did not occur after real Split.");
        RuntimeAssertions.Require(f.SumNamed(item.c_altName) == before, "Exception lost or duplicated split quantity.");
        f.AssertAllNamedInBox(item.c_altName);
        f.AssertMetadata();
        f.Observer.ThrowBeforeFirstOffer = false;
        f.Observer.Reset();
        Elin_AutoOfferingAlter.OfferLogic.Process(f.Box);
        RuntimeAssertions.Require(f.SumNamed(item.c_altName) == 0 && f.Observer.NormalOfferingCalls > 0,
            "Native retry failed after exception cleanup.");
    }
}

public sealed class Pr8SleepCompletionCase : Pr8OfferingCase
{
    public override string Id => "pr8.sleep.sleep_completion";
    public override IReadOnlyList<string> Tags => new[] { "integration", "pr8", "sleep_removal", "isolated_player" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr8OfferingFixture>("pr8.fixture");
        f.Actor.SetFaith("ehekatl");
        Thing fish = f.Offering("fish", 5);
        f.RemoveSleep(f.Actor, completed: false, dead: false);
        f.AssertRetained(fish, 5);
        RuntimeAssertions.Require(f.Observer.OfferCalls.Count == 0, "Incomplete sleep offered items.");
        f.RemoveSleep(f.Actor, completed: true, dead: true);
        f.Actor.isDead = false;
        f.AssertRetained(fish, 5);
        RuntimeAssertions.Require(f.Observer.OfferCalls.Count == 0, "Dead sleep owner offered items.");
        f.RemoveSleep(f.OtherActor, completed: true, dead: false);
        f.AssertRetained(fish, 5);
        RuntimeAssertions.Require(f.Observer.OfferCalls.Count == 0, "NPC removal offered PC items.");
        f.RemoveSleep(f.Actor, completed: true, dead: false);
        RuntimeAssertions.Require(fish.isDestroyed && f.Observer.CallsFor(fish.uid) > 0,
            "Completed native ConSleep removal did not reach actual offering.");
        RuntimeAssertions.Require(f.Actor.conSleep == null, "Native removal did not clear conSleep.");
        ctx.Log("coverage=native_condition_removal; slept is fixture state; timed bed/UI sleep not executed");
    }
}

public sealed class Pr8SourceCraftCase : Pr8OfferingCase
{
    public override string Id => "pr8.sleep.source_craft_coldboot";
    public override IReadOnlyList<string> Tags => new[] { "integration", "pr8", "coldboot", "isolated_player" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr8OfferingFixture>("pr8.fixture");
        Pr8SourceChecks.AssertPostBoot(ctx);
        var src = RecipeManager.Get(Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX);
        RuntimeAssertions.Require(src != null && src.IsQuickCraft, "Offering box simple craft source missing.");
        Recipe recipe = Recipe.Create(src);
        RuntimeAssertions.Require(recipe != null, "Native Recipe.Create failed.");
        recipe.BuildIngredientList();
        var ingredients = new List<Thing>();
        foreach (Recipe.Ingredient ingredient in recipe.ingredients)
        {
            Thing material = f.ItemForIngredient(ingredient);
            ingredient.thing = material;
            ingredient.uid = material.uid;
            ingredients.Add(material);
        }
        Thing crafted = recipe.Craft(BlessedState.Normal, sound: false, ings: ingredients, crafter: Trait.SelfFactory);
        f.Track(crafted);
        RuntimeAssertions.Require(crafted != null && crafted.id == Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX && crafted.IsContainer,
            "Native Recipe.Craft did not produce actual offering box.");
        RuntimeAssertions.Require(crafted.isCrafted && crafted.c_idDeity != Elin_AutoOfferingAlter.Plugin.ID_OFFERING_BOX,
            "Native craft state missing or deity still used as identity.");
        ctx.Log("craft_runtime_type=" + recipe.GetType().FullName + "; altName=" + crafted.c_altName + "; deity=" + crafted.c_idDeity);
        RuntimeAssertions.Require(crafted.c_altName == "\u4fe1\u4ef0\u306e\u7bb1",
            "Craft metadata postfix did not execute on actual override. Do not manually call Postfix_Craft.");
        ctx.Log("coverage=postboot source/craft; boot ordering and true reload require separate restart protocol");
    }
}
#endif
