using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

// Shared helpers for SukutsuArena critical runtime smoke cases.
public static class CriticalCaseHelpers
{
    public static MethodInfo RequireMethod(Type ownerType, string methodName, Type[] parameterTypes = null)
    {
        var method = AccessTools.Method(ownerType, methodName, parameterTypes);
        RuntimeAssertions.Require(method != null, "Method not found: " + ownerType.FullName + "." + methodName);
        return method;
    }

    public static ConstructorInfo RequireConstructor(Type ownerType, Type[] parameterTypes = null)
    {
        var ctor = AccessTools.Constructor(ownerType, parameterTypes ?? Type.EmptyTypes);
        RuntimeAssertions.Require(ctor != null, "Constructor not found: " + ownerType.FullName);
        return ctor;
    }

    public static MethodBase RequireZoneShouldAutoReviveGetter()
    {
        var getter = AccessTools.PropertyGetter(typeof(Zone), "ShouldAutoRevive");
        if (getter != null)
            return getter;

        getter = AccessTools.Method(typeof(Zone), "get_ShouldAutoRevive");
        RuntimeAssertions.Require(getter != null, "Zone.ShouldAutoRevive getter not found.");
        return getter;
    }

    public static void RequireOwnedPatch(object patchInfo, string owner, string reason)
    {
        RuntimeAssertions.Require(
            HasPatchOwner(patchInfo, owner),
            reason);
    }

    public static bool HasPatchOwner(object patchInfo, string owner)
    {
        if (patchInfo == null || string.IsNullOrEmpty(owner))
            return false;

        var ownersObj = ReflectionCompat.GetFieldOrPropertyValue(patchInfo, "Owners", "owners");
        var owners = ReflectionCompat.AsEnumerable(ownersObj);
        if (owners == null)
            return false;

        foreach (var item in owners)
        {
            var text = item as string;
            if (text == null)
                continue;
            if (string.Equals(text, owner, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static void RequirePatched(
        object patchInfo,
        string bucket,
        string declaringTypeFullName,
        string methodName,
        string reason)
    {
        RuntimeAssertions.Require(
            HarmonyCompatFacade.HasPatch(patchInfo, bucket, declaringTypeFullName, methodName),
            reason);
    }

    public static void RequireDictionaryHasKey(IDictionary dictionary, string key, string reason)
    {
        if (dictionary == null || string.IsNullOrEmpty(key))
        {
            RuntimeAssertions.Require(false, reason);
            return;
        }

        RuntimeAssertions.Require(dictionary.Contains(key), reason);
    }

    public static Elin_SukutsuArena.Quests.ArenaQuestDefinition RequireQuest(string questId)
    {
        var quest = Elin_SukutsuArena.Quests.ArenaQuestDatabase.GetQuest(questId);
        RuntimeAssertions.Require(quest != null, "Quest not found: " + questId);
        return quest;
    }

    public static void RequireQuestDependsOn(
        Elin_SukutsuArena.Quests.ArenaQuestDefinition quest,
        string requiredQuestId,
        string reason)
    {
        RuntimeAssertions.Require(quest != null, "Quest is null.");
        RuntimeAssertions.Require(quest.RequiredQuests != null, reason);

        bool found = false;
        for (int i = 0; i < quest.RequiredQuests.Count; i++)
        {
            if (string.Equals(quest.RequiredQuests[i], requiredQuestId, StringComparison.Ordinal))
            {
                found = true;
                break;
            }
        }

        RuntimeAssertions.Require(found, reason);
    }

    public static bool ListContains(IList<string> list, string value)
    {
        if (list == null || string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], value, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
