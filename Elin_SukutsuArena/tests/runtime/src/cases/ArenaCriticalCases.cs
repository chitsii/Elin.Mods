using System.Collections.Generic;
using System.Reflection;

// Critical arena battle pipeline contract checks for SukutsuArena.
public sealed class ArenaBattlePipelineContractCase : RuntimeCaseBase
{
    public override string Id => "arena.battle_pipeline_contract";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "arena" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        var startBattleByMaster = CriticalCaseHelpers.RequireMethod(
            typeof(Elin_SukutsuArena.ArenaManager),
            "StartBattleByStage",
            new[] { typeof(string), typeof(Chara) });
        var startBattleDirect = CriticalCaseHelpers.RequireMethod(
            typeof(Elin_SukutsuArena.ArenaManager),
            "StartBattleByStageWithoutMaster",
            new[] { typeof(string), typeof(string), typeof(string) });
        var preEnterExecute = CriticalCaseHelpers.RequireMethod(
            typeof(ZonePreEnterArenaBattle),
            nameof(ZonePreEnterArenaBattle.Execute),
            null);
        var arenaTick = CriticalCaseHelpers.RequireMethod(
            typeof(ZoneEventArenaBattle),
            nameof(ZoneEventArenaBattle.OnTick),
            null);
        var arenaOnCharaDie = CriticalCaseHelpers.RequireMethod(
            typeof(ZoneEventArenaBattle),
            nameof(ZoneEventArenaBattle.OnCharaDie),
            new[] { typeof(Chara) });
        var onLeaveZone = CriticalCaseHelpers.RequireMethod(
            typeof(ZoneInstanceArenaBattle),
            nameof(ZoneInstanceArenaBattle.OnLeaveZone),
            null);
        var leaveZone = CriticalCaseHelpers.RequireMethod(
            typeof(ZoneInstanceArenaBattle),
            nameof(ZoneInstanceArenaBattle.LeaveZone),
            null);

        ctx.Set("startBattleByMaster", startBattleByMaster);
        ctx.Set("startBattleDirect", startBattleDirect);
        ctx.Set("preEnterExecute", preEnterExecute);
        ctx.Set("arenaTick", arenaTick);
        ctx.Set("arenaOnCharaDie", arenaOnCharaDie);
        ctx.Set("onLeaveZone", onLeaveZone);
        ctx.Set("leaveZone", leaveZone);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(typeof(ZoneEvent).IsAssignableFrom(typeof(ZoneEventArenaBattle)), "ZoneEventArenaBattle must derive from ZoneEvent.");
        RuntimeAssertions.Require(typeof(ZoneInstance).IsAssignableFrom(typeof(ZoneInstanceArenaBattle)), "ZoneInstanceArenaBattle must derive from ZoneInstance.");
        RuntimeAssertions.Require(typeof(ZonePreEnterEvent).IsAssignableFrom(typeof(ZonePreEnterArenaBattle)), "ZonePreEnterArenaBattle must derive from ZonePreEnterEvent.");

        var pendingDirectDrama = ReflectionCompat.GetStaticFieldOrPropertyValue(
            typeof(ZoneInstanceArenaBattle),
            "PendingDirectDrama");
        RuntimeAssertions.Require(pendingDirectDrama != null, "ZoneInstanceArenaBattle.PendingDirectDrama is unavailable.");

        var ctor = CriticalCaseHelpers.RequireConstructor(typeof(ZoneEventArenaBattle), null);
        RuntimeAssertions.Require(ctor != null, "ZoneEventArenaBattle default constructor missing.");

        RuntimeAssertions.Require(ctx.Get<MethodInfo>("startBattleByMaster").IsStatic, "ArenaManager.StartBattleByStage(string,Chara) must be static.");
        RuntimeAssertions.Require(ctx.Get<MethodInfo>("startBattleDirect").IsStatic, "ArenaManager.StartBattleByStageWithoutMaster(string,string,string) must be static.");
        RuntimeAssertions.Require(!ctx.Get<MethodInfo>("preEnterExecute").IsStatic, "ZonePreEnterArenaBattle.Execute must be instance method.");
        RuntimeAssertions.Require(!ctx.Get<MethodInfo>("arenaTick").IsStatic, "ZoneEventArenaBattle.OnTick must be instance method.");
        RuntimeAssertions.Require(!ctx.Get<MethodInfo>("arenaOnCharaDie").IsStatic, "ZoneEventArenaBattle.OnCharaDie must be instance method.");
        RuntimeAssertions.Require(!ctx.Get<MethodInfo>("onLeaveZone").IsStatic, "ZoneInstanceArenaBattle.OnLeaveZone must be instance method.");
        RuntimeAssertions.Require(!ctx.Get<MethodInfo>("leaveZone").IsStatic, "ZoneInstanceArenaBattle.LeaveZone must be instance method.");

        RuntimeAssertions.Require(
            string.Equals(Elin_SukutsuArena.Arena.ArenaConfig.ZoneId, "sukutsu_arena", System.StringComparison.Ordinal),
            "ArenaConfig.ZoneId drift detected.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}
