// Native operations are synchronous: no yielded child can escape finally/rollback.
public abstract class Pr1AutoEatCase : RuntimeCaseBase
{
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "pr1", "autoeat" };
    public override void Prepare(RuntimeTestContext ctx)
    {
        ctx.Set("pr1.fixture", Pr1AutoEatFixture.Create(ctx));
    }
    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(ctx.Get<bool>("pr1.asserted"), "Native assertions did not finish.");
    }
    public override void Cleanup(RuntimeTestContext ctx)
    {
        var f = ctx.GetOrDefault<Pr1AutoEatFixture>("pr1.fixture");
        if (f != null) f.Cleanup();
    }
    protected static void Done(RuntimeTestContext ctx) { ctx.Set("pr1.asserted", true); }
}

public sealed class Pr1AutoEatConfigCase : Pr1AutoEatCase
{
    public override string Id => "pr1.autoeat.config_default_preserved";
    public override void Execute(RuntimeTestContext ctx)
    {
        ctx.Get<Pr1AutoEatFixture>("pr1.fixture").RequireConfigBinding();
        Done(ctx);
    }
}

public sealed class Pr1AutoEatConsumptionCase : Pr1AutoEatCase
{
    public override string Id => "pr1.autoeat.reentry_consumption";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr1AutoEatFixture>("pr1.fixture");
        f.Run(() =>
        {
            for (int trigger = 1; trigger <= 2; trigger++)
            {
                f.Observer.Reset();
                f.Actor.hunger.Set(f.HungryValue - 1);
                int before = f.Food.Num;
                string nutrition = Pr1AutoEatFixture.ElementState(f.Actor);
                f.Actor.hunger.Mod(1);
                f.LogMeal("native-trigger-" + trigger, before);
                RuntimeAssertions.Require(f.Observer.InstantCalls == 1 && f.Observer.FoodCalls == 1,
                    "One native hunger trigger must reach InstantEat and FoodEffect exactly once.");
                RuntimeAssertions.Require(f.Observer.NestedPhases > 0,
                    "No native phase change inside FoodEffect: reentry coverage not established.");
                RuntimeAssertions.Require(f.Observer.MaxDepth == 1 && f.Food.Num == before - 1,
                    "Reentrant eating consumed more than one meal.");
                RuntimeAssertions.Require(f.Actor.hunger.value < f.HungryValue && f.Observer.NutritionCalls == 1,
                    "Native hunger reduction/nutrition missing.");
                RuntimeAssertions.Require(nutrition != Pr1AutoEatFixture.ElementState(f.Actor),
                    "Native nutrition did not change fixture element state.");
                RuntimeAssertions.Require(!f.Actor.isDead, "Safe cooked meal killed fixture.");
            }
        });
        Done(ctx);
    }
}

public sealed class Pr1AutoEatFiltersCase : Pr1AutoEatCase
{
    public override string Id => "pr1.autoeat.filters_threshold";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr1AutoEatFixture>("pr1.fixture");
        f.Run(() =>
        {
            f.Actor.hunger.Set(f.HungryValue - 1);
            f.ExpectNoMeal("below-threshold");
            f.Actor.hunger.Set(f.HungryValue);
            f.Set("AutoEatEnabled", false);
            f.ExpectNoMeal("disabled");
            f.Set("AutoEatEnabled", true);
            var oldAI = f.Actor.ai;
            try
            {
                f.Actor.SetAI(new AI_Eat());
                f.ExpectNoMeal("AI_Eat");
            }
            finally { f.Actor.SetAI(oldAI); }
            f.Food.c_isImportant = true;
            f.ExpectNoMeal("important");
            f.Food.c_isImportant = false;
            int decay = f.Food.decay;
            try
            {
                f.Food.decay = f.Food.MaxDecay + 1;
                RuntimeAssertions.Require(!f.Actor.CanEat(f.Food, true), "Rotten control native-edible.");
                f.ExpectNoMeal("native-reject-rotten");
            }
            finally { f.Food.decay = decay; }
            var box = f.CreateContainer();
            f.AddFixtureThing(box, f.Food);
            f.Set("UseContainerFilter", true);
            f.Set("ContainerId", "PR1_NO_MATCH_" + f.Token);
            f.ExpectNoMeal("container-excluded");
            f.Set("ContainerId", box.id);
            f.Observer.Reset();
            int before = f.Food.Num;
            f.CheckEat();
            f.LogMeal("container-included", before);
            RuntimeAssertions.Require(f.Food.Num == before - 1 && f.Observer.InstantCalls == 1,
                "Matching container did not provide exactly one meal.");
        });
        Done(ctx);
    }
}

public sealed class Pr1AutoEatReentryStressCase : Pr1AutoEatCase
{
    public override string Id => "pr1.autoeat.reentry_stress";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "pr1", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr1AutoEatFixture>("pr1.fixture");
        f.Run(() =>
        {
            f.Actor.hunger.Set(f.HungryValue);
            f.Observer.Reset();
            f.Observer.InjectReentry = true;
            int before = f.Food.Num;
            f.CheckEat();
            f.LogMeal("injected-CheckAutoEat-inside-FoodEffect", before);
            RuntimeAssertions.Require(f.Observer.ReentryInjected == 1 && f.Observer.InstantCalls == 1
                && f.Observer.MaxDepth == 1 && f.Food.Num == before - 1,
                "CheckAutoEat during native FoodEffect caused a second consumption.");
        });
        Done(ctx);
    }
}

public sealed class Pr1AutoEatExceptionCase : Pr1AutoEatCase
{
    public override string Id => "pr1.autoeat.exception_guard_release";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "pr1", "fault_injection" };
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr1AutoEatFixture>("pr1.fixture");
        f.Run(() =>
        {
            f.Actor.hunger.Set(f.HungryValue);
            f.Observer.Reset();
            f.Observer.InjectThrow = true;
            int before = f.Food.Num;
            f.CheckEat();
            f.LogMeal("injected-FoodEffect-exception", before);
            RuntimeAssertions.Require(f.Observer.ThrowsInjected == 1 && f.Observer.InstantCalls == 1
                && f.Observer.InstantExceptions == 1 && f.Food.Num == before,
                "Fault did not escape native InstantEat before consumption.");
            f.Observer.Reset();
            f.CheckEat();
            f.LogMeal("normal-retry-after-exception", before);
            RuntimeAssertions.Require(f.Observer.InstantCalls == 1 && f.Observer.FoodCalls == 1
                && f.Food.Num == before - 1 && f.Observer.InstantExceptions == 0,
                "Guard remained locked after exception; subsequent meal failed.");
        });
        Done(ctx);
    }
}

public sealed class Pr1AutoEatDiseaseCase : Pr1AutoEatCase
{
    public override string Id => "pr1.autoeat.anorexia_native_effects";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr1AutoEatFixture>("pr1.fixture");
        long healthyNutrition = 0;
        var attribute = f.Actor.elements.GetElement(70);
        RuntimeAssertions.Require(attribute != null, "Fixture strength attribute absent.");
        f.Run(() =>
        {
            attribute.vBase = 10;
            attribute.vExp = 0;
            attribute.vPotential = 100;
            f.Actor.hunger.Set(f.HungryValue);
            f.CheckEat();
            healthyNutrition = (long)attribute.vBase * 1000 + attribute.vExp - 10000;
            RuntimeAssertions.Require(healthyNutrition > 0, "Healthy native meal produced no attribute nutrition.");
        });
        // NPC setup avoids the native PC AddCondition -> EndTurn branch.
        f.Actor.AddCondition<ConAnorexia>(1000, true);
        RuntimeAssertions.Require(f.Actor.HasCondition<ConAnorexia>(), "Native disease fixture rejected.");
        f.Run(() =>
        {
            attribute.vBase = 10;
            attribute.vExp = 0;
            attribute.vPotential = 100;
            f.Actor.hunger.Set(f.HungryValue);
            f.Observer.Reset();
            int before = f.Food.Num;
            int vomit = f.Actor.c_vomit;
            f.CheckEat();
            f.LogMeal("native-anorexia", before);
            RuntimeAssertions.Require(f.Observer.InstantCalls == 1 && f.Observer.NutritionCalls == 1
                && f.Food.Num == before - 1 && f.Observer.MaxDepth == 1,
                "Disease/nutrition feedback caused duplicate consumption.");
            RuntimeAssertions.Require(f.Actor.c_vomit == vomit + 1 && f.Actor.HasCondition<ConAnorexia>()
                && f.Observer.VomitCalls == 1 && f.Observer.NestedPhases > 0,
                "Native disease/Vomit/phase feedback missing.");
            RuntimeAssertions.Require(!f.Actor.isDead, "Disease fixture died; reload dedicated save.");
            long diseaseNutrition = (long)attribute.vBase * 1000 + attribute.vExp - 10000;
            ctx.Log("nutrition:healthyDelta=" + healthyNutrition + ":anorexiaDelta=" + diseaseNutrition);
            RuntimeAssertions.Require(diseaseNutrition < healthyNutrition,
                "Native anorexia/Vomit did not reduce nutrition relative to healthy control.");
        });
        Done(ctx);
    }
}

public sealed class Pr1AutoEatSleepCase : Pr1AutoEatCase
{
    public override string Id => "pr1.autoeat.sleep_native_resume";
    public override void Execute(RuntimeTestContext ctx)
    {
        var f = ctx.Get<Pr1AutoEatFixture>("pr1.fixture");
        var sleep = f.CreateSleep();
        RuntimeAssertions.Require(sleep != null && f.Actor.conSleep == sleep, "Native ConSleep setup failed.");
        sleep.slept = false;
        sleep.pcSleep = 0;
        var resume = new AI_Eat();
        f.Run(() =>
        {
            f.Actor.SetAI(resume);
            f.CaptureAI();
            f.Actor.SetAI(new AI_Idle());
            f.RemoveFixtureSleep(sleep);
            ctx.Log("native-sleep:uid=" + f.Actor.uid + ":removed=" + f.Observer.SleepRemovedCalls
                + ":ai=" + f.Actor.ai.GetType().Name + ":slept=false");
            RuntimeAssertions.Require(f.Observer.SleepRemovedCalls == 1 && f.Actor.conSleep == null
                && !f.Actor.HasCondition<ConSleep>(), "Condition.Kill did not remove ConSleep exactly once.");
            RuntimeAssertions.Require(object.ReferenceEquals(f.Actor.ai, resume) && f.SavedAI == null,
                "Native ConSleep hook did not resume captured AI and clear saved state.");
        });
        sleep = f.CreateSleep();
        RuntimeAssertions.Require(sleep != null, "Second native ConSleep setup failed.");
        f.Set("ResumeAiOnWake", false);
        f.Run(() =>
        {
            var idle = new AI_Idle();
            f.Actor.SetAI(idle);
            f.SavedAI = resume;
            f.Observer.Reset();
            f.RemoveFixtureSleep(sleep);
            RuntimeAssertions.Require(f.Observer.SleepRemovedCalls == 1 && object.ReferenceEquals(f.Actor.ai, idle),
                "Disabled resume changed AI on native ConSleep removal.");
        });
        Done(ctx);
    }
}
