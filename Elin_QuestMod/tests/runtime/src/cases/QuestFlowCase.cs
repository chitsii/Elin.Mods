using System;
using System.Collections.Generic;
using System.Reflection;

// Verifies quest flow bootstrap defaults.
public sealed class QuestFlowBootstrapCase : RuntimeCaseBase
{
    public override string Id => "questmod.quest_flow.bootstrap_defaults";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "quest", "critical" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.player != null, "EClass.player is null.");
        RuntimeAssertions.Require(EClass.player.dialogFlags != null, "dialogFlags is null.");

        Type flowType = ModRuntimeReflection.RequireType("Elin_QuestMod.Quest.QuestFlow");
        Type serviceType = ModRuntimeReflection.RequireType("Elin_QuestMod.Quest.QuestStateService");
        Type pluginType = ModRuntimeReflection.RequireType("Elin_QuestMod.Plugin");

        ctx.Set("flowType", flowType);
        ctx.Set("serviceType", serviceType);
        ctx.Set("pluginType", pluginType);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        Type flowType = ctx.Get<Type>("flowType");
        Type serviceType = ctx.Get<Type>("serviceType");
        Type pluginType = ctx.Get<Type>("pluginType");

        int cleared = QuestRuntimeFlagReset.ClearAllFlagsForDefaultPrefix(serviceType);
        InvokeStatic(flowType, "EnsureBootstrap", new object[0]);

        string bootstrapKey = InvokeString(serviceType, "GetBootstrapFlagKey", new object[0]);
        string defaultPrefix = InvokeString(serviceType, "GetDefaultPrefix", new object[0]);
        string modGuid = (string)pluginType.GetField("ModGuid", BindingFlags.Public | BindingFlags.Static).GetValue(null);

        int bootstrap = InvokeGetFlagInt(serviceType, bootstrapKey, 0);
        int phase = InvokeInt(serviceType, "GetCurrentPhase", new object[0]);
        bool introActive = InvokeBool(serviceType, "IsQuestActive", new object[] { "quest_intro" });

        ctx.Set("bootstrapKey", bootstrapKey);
        ctx.Set("defaultPrefix", defaultPrefix);
        ctx.Set("modGuid", modGuid);
        ctx.Set("bootstrap", bootstrap);
        ctx.Set("phase", phase);
        ctx.Set("introActive", introActive);
        ctx.Set("cleared", cleared);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        string bootstrapKey = ctx.Get<string>("bootstrapKey");
        string defaultPrefix = ctx.Get<string>("defaultPrefix");
        string modGuid = ctx.Get<string>("modGuid");

        RuntimeAssertions.Require(ctx.Get<int>("bootstrap") == 1, "Bootstrap flag was not initialized.");
        RuntimeAssertions.Require(ctx.Get<int>("phase") == 0, "Initial phase is not 0.");
        RuntimeAssertions.Require(ctx.Get<bool>("introActive"), "Intro quest was not activated.");
        RuntimeAssertions.Require(
            string.Equals(defaultPrefix, modGuid.ToLowerInvariant(), StringComparison.Ordinal),
            "Default quest prefix is not derived from Plugin.ModGuid.");
        RuntimeAssertions.Require(
            bootstrapKey.StartsWith(defaultPrefix + ".", StringComparison.Ordinal),
            "Bootstrap key does not use default prefix.");

        ctx.Log("Quest flow bootstrap defaults verified. cleared=" + ctx.Get<int>("cleared"));
    }

    private static int InvokeGetFlagInt(Type serviceType, string key, int defaultValue)
    {
        object raw = InvokeStatic(serviceType, "GetFlagInt", new object[] { key, defaultValue });
        RuntimeAssertions.Require(raw is int, "GetFlagInt returned non-int.");
        return (int)raw;
    }

    private static int InvokeInt(Type type, string name, object[] args)
    {
        object raw = InvokeStatic(type, name, args);
        RuntimeAssertions.Require(raw is int, "Expected int return from " + name);
        return (int)raw;
    }

    private static bool InvokeBool(Type type, string name, object[] args)
    {
        object raw = InvokeStatic(type, name, args);
        RuntimeAssertions.Require(raw is bool, "Expected bool return from " + name);
        return (bool)raw;
    }

    private static string InvokeString(Type type, string name, object[] args)
    {
        object raw = InvokeStatic(type, name, args);
        RuntimeAssertions.Require(raw is string, "Expected string return from " + name);
        return (string)raw;
    }

    private static object InvokeStatic(Type type, string methodName, object[] args)
    {
        var argTypes = GetArgumentTypes(args);
        MethodInfo method = ModRuntimeReflection.RequireStaticMethod(type, methodName, argTypes);
        return ModRuntimeReflection.InvokeStatic(method, args);
    }

    private static Type[] GetArgumentTypes(object[] args)
    {
        if (args == null || args.Length == 0)
        {
            return Type.EmptyTypes;
        }

        var types = new Type[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            types[i] = args[i] != null ? args[i].GetType() : typeof(object);
        }
        return types;
    }
}
