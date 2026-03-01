using System;
using System.Collections.Generic;
using System.Reflection;

// Verifies quest service lifecycle operations with runtime-created quest IDs.
public sealed class QuestServiceLifecycleCase : RuntimeCaseBase
{
    public override string Id => "questmod.quest_state.lifecycle_roundtrip";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "quest", "critical" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.player != null, "EClass.player is null.");
        RuntimeAssertions.Require(EClass.player.dialogFlags != null, "dialogFlags is null.");

        Type serviceType = ModRuntimeReflection.RequireType(
            "Elin_QuestMod.Quest.QuestStateService");

        ctx.Set("serviceType", serviceType);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        Type serviceType = ctx.Get<Type>("serviceType");
        QuestRuntimeFlagReset.ClearAllFlagsForDefaultPrefix(serviceType);
        string questId = "questmod_runtime_lifecycle_" + DateTime.UtcNow.Ticks;
        string activeKey = InvokeString(serviceType, "GetQuestActiveKey", new object[] { questId });
        string doneKey = InvokeString(serviceType, "GetQuestDoneKey", new object[] { questId });

        InvokeSetFlagInt(serviceType, activeKey, 0);
        InvokeSetFlagInt(serviceType, doneKey, 0);

        InvokeStatic(serviceType, "StartQuest", new object[] { questId });

        bool activeAfterStart = InvokeBool(serviceType, "IsQuestActive", new object[] { questId });
        bool doneAfterStart = InvokeBool(serviceType, "IsQuestCompleted", new object[] { questId });

        InvokeStatic(serviceType, "CompleteQuest", new object[] { questId, 7 });

        bool activeAfterComplete = InvokeBool(serviceType, "IsQuestActive", new object[] { questId });
        bool doneAfterComplete = InvokeBool(serviceType, "IsQuestCompleted", new object[] { questId });
        int phaseAfterComplete = InvokeInt(serviceType, "GetCurrentPhase", new object[0]);

        ctx.Set("activeAfterStart", activeAfterStart);
        ctx.Set("doneAfterStart", doneAfterStart);
        ctx.Set("activeAfterComplete", activeAfterComplete);
        ctx.Set("doneAfterComplete", doneAfterComplete);
        ctx.Set("phaseAfterComplete", phaseAfterComplete);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(ctx.Get<bool>("activeAfterStart"), "Quest did not become active after StartQuest.");
        RuntimeAssertions.Require(!ctx.Get<bool>("doneAfterStart"), "Quest was completed immediately after StartQuest.");
        RuntimeAssertions.Require(!ctx.Get<bool>("activeAfterComplete"), "Quest remained active after CompleteQuest.");
        RuntimeAssertions.Require(ctx.Get<bool>("doneAfterComplete"), "Quest was not completed after CompleteQuest.");
        RuntimeAssertions.Require(
            ctx.Get<int>("phaseAfterComplete") == 7,
            "Current phase was not updated by CompleteQuest.");

        ctx.Log("Quest lifecycle roundtrip verified.");
    }

    private static void InvokeSetFlagInt(Type serviceType, string key, int value)
    {
        InvokeStatic(serviceType, "SetFlagInt", new object[] { key, value });
    }

    private static bool InvokeBool(Type type, string name, object[] args)
    {
        object raw = InvokeStatic(type, name, args);
        RuntimeAssertions.Require(raw is bool, "Expected bool return from " + name);
        return (bool)raw;
    }

    private static int InvokeInt(Type type, string name, object[] args)
    {
        object raw = InvokeStatic(type, name, args);
        RuntimeAssertions.Require(raw is int, "Expected int return from " + name);
        return (int)raw;
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
        var method = ModRuntimeReflection.RequireStaticMethod(type, methodName, argTypes);
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

// Verifies dispatch selection order and flag writeback.
public sealed class QuestServiceDispatchCase : RuntimeCaseBase
{
    public override string Id => "questmod.quest_state.dispatch_priority";

    public override IReadOnlyList<string> Tags => new[] { "smoke", "quest", "critical" };

    public override void Prepare(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(EClass.player != null, "EClass.player is null.");
        RuntimeAssertions.Require(EClass.player.dialogFlags != null, "dialogFlags is null.");

        Type serviceType = ModRuntimeReflection.RequireType(
            "Elin_QuestMod.Quest.QuestStateService");
        ctx.Set("serviceType", serviceType);
    }

    public override void Execute(RuntimeTestContext ctx)
    {
        Type serviceType = ctx.Get<Type>("serviceType");
        QuestRuntimeFlagReset.ClearAllFlagsForDefaultPrefix(serviceType);
        string suffix = DateTime.UtcNow.Ticks.ToString();
        string questA = "questmod_dispatch_a_" + suffix;
        string questB = "questmod_dispatch_b_" + suffix;
        string outputKey = InvokeString(serviceType, "BuildFlagKey", new object[] { "dispatch.result." + suffix });
        string csv = questA + "," + questB;

        SetQuestState(serviceType, questA, active: 1, done: 0);
        SetQuestState(serviceType, questB, active: 1, done: 0);
        int firstSelection = InvokeDispatch(serviceType, outputKey, csv);
        int firstFlag = InvokeGetFlagInt(serviceType, outputKey, -1);

        SetQuestState(serviceType, questA, active: 0, done: 1);
        SetQuestState(serviceType, questB, active: 1, done: 0);
        int secondSelection = InvokeDispatch(serviceType, outputKey, csv);
        int secondFlag = InvokeGetFlagInt(serviceType, outputKey, -1);

        SetQuestState(serviceType, questA, active: 0, done: 1);
        SetQuestState(serviceType, questB, active: 0, done: 1);
        int thirdSelection = InvokeDispatch(serviceType, outputKey, csv);
        int thirdFlag = InvokeGetFlagInt(serviceType, outputKey, -1);

        ctx.Set("firstSelection", firstSelection);
        ctx.Set("firstFlag", firstFlag);
        ctx.Set("secondSelection", secondSelection);
        ctx.Set("secondFlag", secondFlag);
        ctx.Set("thirdSelection", thirdSelection);
        ctx.Set("thirdFlag", thirdFlag);
    }

    public override void Verify(RuntimeTestContext ctx)
    {
        RuntimeAssertions.Require(ctx.Get<int>("firstSelection") == 1, "Dispatch did not pick first active quest.");
        RuntimeAssertions.Require(ctx.Get<int>("firstFlag") == 1, "Dispatch output flag mismatch for first selection.");
        RuntimeAssertions.Require(ctx.Get<int>("secondSelection") == 2, "Dispatch did not skip completed first quest.");
        RuntimeAssertions.Require(ctx.Get<int>("secondFlag") == 2, "Dispatch output flag mismatch for second selection.");
        RuntimeAssertions.Require(ctx.Get<int>("thirdSelection") == 0, "Dispatch should return fallback when no active quest.");
        RuntimeAssertions.Require(ctx.Get<int>("thirdFlag") == 0, "Dispatch output flag mismatch for fallback selection.");

        ctx.Log("Quest dispatch priority verified.");
    }

    private static void SetQuestState(Type serviceType, string questId, int active, int done)
    {
        string activeKey = InvokeString(serviceType, "GetQuestActiveKey", new object[] { questId });
        string doneKey = InvokeString(serviceType, "GetQuestDoneKey", new object[] { questId });
        InvokeSetFlagInt(serviceType, activeKey, active);
        InvokeSetFlagInt(serviceType, doneKey, done);
    }

    private static int InvokeDispatch(Type serviceType, string outputFlagKey, string questIdsCsv)
    {
        object raw = InvokeStatic(serviceType, "CheckQuestsForDispatch", new object[] { outputFlagKey, questIdsCsv });
        RuntimeAssertions.Require(raw is int, "CheckQuestsForDispatch returned non-int.");
        return (int)raw;
    }

    private static int InvokeGetFlagInt(Type serviceType, string key, int defaultValue)
    {
        object raw = InvokeStatic(serviceType, "GetFlagInt", new object[] { key, defaultValue });
        RuntimeAssertions.Require(raw is int, "GetFlagInt returned non-int.");
        return (int)raw;
    }

    private static void InvokeSetFlagInt(Type serviceType, string key, int value)
    {
        InvokeStatic(serviceType, "SetFlagInt", new object[] { key, value });
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
        var method = ModRuntimeReflection.RequireStaticMethod(type, methodName, argTypes);
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
