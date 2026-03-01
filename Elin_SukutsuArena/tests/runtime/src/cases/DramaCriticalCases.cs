using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// Critical drama routing contract checks for SukutsuArena.
public sealed class DramaModInvokeRegistryContractCase : RuntimeCaseBase
{
    public override string Id => "drama.modinvoke_registry_contract";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "critical", "quest", "drama" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        Elin_SukutsuArena.Commands.ArenaCommandRegistry.Initialize();

        var commandsObj = ReflectionCompat.GetStaticFieldOrPropertyValue(
            typeof(Elin_SukutsuArena.Commands.ArenaCommandRegistry),
            "_commands");
        var commandMap = commandsObj as IDictionary;
        RuntimeAssertions.Require(commandMap != null, "ArenaCommandRegistry command map not found.");

        var parseLine = CriticalCaseHelpers.RequireMethod(typeof(DramaManager), "ParseLine", null);

        ctx.Set("commandMap", commandMap);
        ctx.Set("parseLine", parseLine);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var parseLine = ctx.Get<MethodInfo>("parseLine");
        ctx.Set("parseLinePatchInfo", HarmonyCompatFacade.GetPatchInfo(parseLine));
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        var commandMap = ctx.Get<IDictionary>("commandMap");
        var parseLinePatchInfo = ctx.GetOrDefault<object>("parseLinePatchInfo");

        RuntimeAssertions.Require(commandMap.Count >= 20, "ArenaCommandRegistry registered too few commands: " + commandMap.Count);
        RuntimeAssertions.Require(parseLinePatchInfo != null, "No patch info on DramaManager.ParseLine.");

        CriticalCaseHelpers.RequireDictionaryHasKey(
            commandMap,
            "start_quest",
            "start_quest command is missing.");
        CriticalCaseHelpers.RequireDictionaryHasKey(
            commandMap,
            "complete_quest",
            "complete_quest command is missing.");
        CriticalCaseHelpers.RequireDictionaryHasKey(
            commandMap,
            "if_flag",
            "if_flag command is missing.");
        CriticalCaseHelpers.RequireDictionaryHasKey(
            commandMap,
            "switch_flag",
            "switch_flag command is missing.");
        CriticalCaseHelpers.RequireDictionaryHasKey(
            commandMap,
            "update_lily_shop_stock",
            "update_lily_shop_stock command is missing.");
        CriticalCaseHelpers.RequireDictionaryHasKey(
            commandMap,
            "hide_npc",
            "hide_npc command is missing.");

        CriticalCaseHelpers.RequireOwnedPatch(
            parseLinePatchInfo,
            Elin_SukutsuArena.Arena.ArenaConfig.ModGuid,
            "Sukutsu patch owner missing on DramaManager.ParseLine.");
        CriticalCaseHelpers.RequirePatched(
            parseLinePatchInfo,
            "postfix",
            "Elin_SukutsuArena.DramaManager_Patch",
            "ParseLine_Postfix",
            "ParseLine_Postfix is not patched on DramaManager.ParseLine.");
    }

    public override void Cleanup(RuntimeTestContext ctx)
    {
    }
}
