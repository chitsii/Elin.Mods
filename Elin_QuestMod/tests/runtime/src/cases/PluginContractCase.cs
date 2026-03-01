using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// Verifies plugin/package contract for quest mod skeleton.
public sealed class PluginContractCase : RuntimeCaseBase
{
    private const string ExpectedModGuid = "yourname.elin_quest_mod";

    public override string Id => "questmod.plugin.contract.guid_and_package";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "compat" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        Type pluginType = ModRuntimeReflection.RequireType("Elin_QuestMod.Plugin");
        var modGuidField = pluginType.GetField("ModGuid", BindingFlags.Public | BindingFlags.Static);
        RuntimeAssertions.Require(modGuidField != null, "Plugin.ModGuid field not found.");

        string packagePath = Path.Combine(ctx.ModRoot ?? string.Empty, "package.xml");
        RuntimeAssertions.Require(File.Exists(packagePath), "package.xml not found: " + packagePath);

        ctx.Set("pluginType", pluginType);
        ctx.Set("modGuidField", modGuidField);
        ctx.Set("packagePath", packagePath);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        var modGuidField = ctx.Get<FieldInfo>("modGuidField");
        string modGuid = modGuidField.GetValue(null) as string;
        RuntimeAssertions.Require(!string.IsNullOrEmpty(modGuid), "Plugin.ModGuid is empty.");
        ctx.Set("modGuid", modGuid);

        string packagePath = ctx.Get<string>("packagePath");
        string xml = File.ReadAllText(packagePath);
        ctx.Set("packageXml", xml);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        string modGuid = ctx.Get<string>("modGuid");
        string packageXml = ctx.Get<string>("packageXml");

        RuntimeAssertions.Require(
            string.Equals(modGuid, ExpectedModGuid, StringComparison.Ordinal),
            "Plugin.ModGuid mismatch: " + modGuid);

        RuntimeAssertions.Require(
            packageXml.IndexOf("<id>" + ExpectedModGuid + "</id>", StringComparison.Ordinal) >= 0,
            "package.xml id mismatch.");

        ctx.Log("Plugin/package contract verified.");
    }
}
