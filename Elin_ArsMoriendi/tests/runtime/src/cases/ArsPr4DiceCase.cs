public sealed class ArsPr4DiceDamageHealCase : RuntimeCaseBase, IRuntimeCoroutineCase
{
    public override string Id => "pr4.ars.dice_damage_heal";
    public override System.Collections.Generic.IReadOnlyList<string> Tags => new[] { "integration", "destructive", "pr4", "spell" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var scope = ArsPr4FixtureScope.Start(ctx);
        RuntimeAssertions.Require(Elin_ArsMoriendi.NecromancyManager.Instance.GetAllServants().Count == 0,
            "Corpse burst fixture requires no pre-existing servants (spell heals/protects the legion).");
        var caster = scope.Spawn("dice_caster", 5);
        caster.MakeMinion(EClass.pc);
        caster.SetSummon(1000);
        caster.hp = System.Math.Max(1, caster.MaxHP / 3);
        var enemy = scope.Spawn("dice_enemy", 30);
        enemy.hostility = Hostility.Enemy;
        enemy.c_originalHostility = Hostility.Enemy;
        enemy.hp = enemy.MaxHP;
        RuntimeAssertions.Require(enemy.IsHostile(caster) && enemy.IsAliveInCurrentZone, "Damage fixture not a live hostile target.");
        RuntimeAssertions.Require(caster.pos.Distance(enemy.pos) <= 6, "Damage fixture outside blast radius.");

        // The real spell consumes every visible corpse and mines nearby walls. Reject unsafe maps.
        foreach (var p in EClass._map.ListPointsInCircle(EClass.pc.pos, System.Math.Max(6, EClass.pc.GetSightRadius() + 1), false, false))
        {
            if (p.Things == null) continue;
            foreach (var thing in p.Things)
                RuntimeAssertions.Require(!(thing.trait is TraitFoodMeat) || string.IsNullOrEmpty(thing.c_idRefCard),
                    "Pre-existing corpse in spell scope; use an empty disposable zone.");
        }
        foreach (var p in EClass._map.ListPointsInCircle(caster.pos, 4, false, false))
            RuntimeAssertions.Require(!p.HasBlock && !p.HasObj, "Blast can mine a pre-existing block/object.");
        foreach (var card in EClass._map.ListCharasInCircle(caster.pos, 6, false))
            RuntimeAssertions.Require(card == enemy || !card.IsHostile(caster), "Pre-existing hostile in blast scope.");

        var corpse = ThingGen.Create("_meat");
        RuntimeAssertions.Require(corpse != null && corpse.trait is TraitFoodMeat, "Native meat fixture source unavailable.");
        ctx.RegisterRollback("pr4.corpse:" + corpse.uid, () =>
        {
            if (!corpse.isDestroyed) corpse.Destroy();
            RuntimeAssertions.Require(corpse.isDestroyed, "Corpse cleanup failed.");
        });
        corpse.MakeFoodFrom("putty");
        corpse.SetNum(1);
        corpse.c_altName = ArsPr4FixtureScope.NamePrefix + scope.Token + "_corpse";
        EClass._zone.AddCard(corpse, caster.pos);
        RuntimeAssertions.Require(EClass.pc.CanSee(corpse), "Native PC visibility rejects fixture corpse.");
        ctx.Set("pr4.caster", caster);
        ctx.Set("pr4.damageEnemy", enemy);
        ctx.Set("pr4.corpse", corpse);
        ctx.Set("pr4.casterHp", (long)caster.hp);
        ctx.Set("pr4.enemyHp", (long)enemy.hp);
        ctx.Log("corpse:uid=" + corpse.uid + ":num=1:ref=" + corpse.c_idRefCard);

        var observer = new HarmonyLib.Harmony("runtime.pr4.ars.dice." + scope.Token);
        ctx.RegisterRollback("pr4.dice_observer", () =>
        {
            observer.UnpatchSelf();
            ArsPr4DiceObserver.Caster = null;
            ArsPr4DiceObserver.Enemy = null;
        });
        ArsPr4DiceObserver.Reset(caster, enemy);
        System.Reflection.MethodInfo roll = null;
        foreach (var method in typeof(Dice).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
        {
            var parameters = method.GetParameters();
            if (method.Name == "Roll" && method.ReturnType == typeof(long) && parameters.Length == 4
                && parameters[0].ParameterType == typeof(int) && parameters[1].ParameterType == typeof(int)
                && parameters[2].ParameterType == typeof(int)) roll = method;
        }
        RuntimeAssertions.Require(roll != null, "Dice.Roll four-argument long signature absent.");
        observer.Patch(roll, postfix: new HarmonyLib.HarmonyMethod(typeof(ArsPr4DiceObserver), "AfterRoll"));
        var damage = HarmonyLib.AccessTools.Method(typeof(Card), "DamageHP",
            new[] { typeof(long), typeof(int), typeof(int), typeof(AttackSource), typeof(Card), typeof(bool), typeof(Thing), typeof(Chara), typeof(int) });
        RuntimeAssertions.Require(damage != null, "Native nine-argument DamageHP target absent.");
        observer.Patch(damage, prefix: new HarmonyLib.HarmonyMethod(typeof(ArsPr4DiceObserver), "BeforeDamage"));
        observer.Patch(HarmonyLib.AccessTools.Method(typeof(Card), "HealHP", new[] { typeof(long), typeof(HealSource) }),
            prefix: new HarmonyLib.HarmonyMethod(typeof(ArsPr4DiceObserver), "BeforeHeal"));
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        ArsPr4DiceObserver.InCast = true;
        try
        {
            ArsPr4FixtureScope.Cast(new Elin_ArsMoriendi.ActCorpseChainBurst(), "actCorpseChainBurst", ctx.Get<Chara>("pr4.caster"));
        }
        finally { ArsPr4DiceObserver.InCast = false; }
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var enemy = ctx.Get<Chara>("pr4.damageEnemy");
        var caster = ctx.Get<Chara>("pr4.caster");
        RuntimeAssertions.Require(ArsPr4DiceObserver.Rolls > 0 && ArsPr4DiceObserver.RollResult > 0,
            "Real spell never reached Dice.Roll(long).");
        RuntimeAssertions.Require(ArsPr4DiceObserver.DamageCalls > 0 && ArsPr4DiceObserver.DamageAmount > 0
            && ArsPr4DiceObserver.DamageAmount <= (long)System.Math.Floor(enemy.MaxHP * 0.60),
            "Native DamageHP long argument/corpse cap not observed.");
        RuntimeAssertions.Require(enemy.hp < ctx.Get<long>("pr4.enemyHp"), "Native corpse damage did not reduce fixture HP.");
        RuntimeAssertions.Require(ArsPr4DiceObserver.HealCalls > 0 && caster.hp > ctx.Get<long>("pr4.casterHp"),
            "Native HealHP(long) did not heal fixture caster.");
        RuntimeAssertions.Require(ctx.Get<Thing>("pr4.corpse").isDestroyed, "Real spell did not consume the fixture corpse.");
        ctx.Log("native:DiceRoll=" + ArsPr4DiceObserver.Rolls + ":lastLong=" + ArsPr4DiceObserver.RollResult
            + ":DamageHP=" + ArsPr4DiceObserver.DamageCalls + ":argLong=" + ArsPr4DiceObserver.DamageAmount
            + ":HealHP=" + ArsPr4DiceObserver.HealCalls + ":enemyHp=" + enemy.hp + ":casterHp=" + caster.hp);
    }

    public override void Cleanup(RuntimeTestContext ctx) { }
    public System.Collections.IEnumerator PrepareAsync(RuntimeTestContext ctx) { Prepare(ctx); yield break; }
    public System.Collections.IEnumerator ExecuteAsync(RuntimeTestContext ctx)
    {
        Execute(ctx);
        // One corpse schedules one delayed visual callback. Drain it before fixture destruction.
        float deadline = UnityEngine.Time.realtimeSinceStartup + 0.25f;
        int frames = 0;
        while (frames++ < 120 && UnityEngine.Time.realtimeSinceStartup < deadline) yield return null;
    }
    public System.Collections.IEnumerator VerifyAsync(RuntimeTestContext ctx) { Verify(ctx); yield break; }
    public System.Collections.IEnumerator CleanupAsync(RuntimeTestContext ctx) { Cleanup(ctx); yield break; }
}

// Pass-through observers never change arguments, results, or the original method.
public static class ArsPr4DiceObserver
{
    public static Chara Caster;
    public static Chara Enemy;
    public static bool InCast;
    public static int Rolls, DamageCalls, HealCalls;
    public static long RollResult, DamageAmount;
    public static void Reset(Chara caster, Chara enemy)
    {
        Caster = caster; Enemy = enemy; InCast = false;
        Rolls = DamageCalls = HealCalls = 0;
        RollResult = DamageAmount = 0;
    }
    public static void AfterRoll(object[] __args, long __result)
    {
        if (InCast && __args.Length == 4 && __args[3] == Caster) { Rolls++; RollResult = __result; }
    }
    public static void BeforeDamage(Card __instance, object[] __args)
    {
        if (!InCast || __instance != Enemy || __args[4] != Caster) return;
        DamageCalls++;
        DamageAmount += (long)__args[0];
    }
    public static void BeforeHeal(Card __instance, long __0)
    {
        if (InCast && __instance == Caster && __0 > 0) HealCalls++;
    }
}
