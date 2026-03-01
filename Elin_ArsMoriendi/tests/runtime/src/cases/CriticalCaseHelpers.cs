using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

// Shared helpers for critical runtime smoke cases.
public static class CriticalCaseHelpers
{
    public static MethodInfo RequireMethod(Type ownerType, string methodName, Type[] parameterTypes = null)
    {
        var method = AccessTools.Method(ownerType, methodName, parameterTypes);
        RuntimeAssertions.Require(method != null, $"Method not found: {ownerType.FullName}.{methodName}");
        return method;
    }

    public static MethodInfo RequireBestCharaDieMethod()
    {
        var methods = typeof(Chara).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo best = null;
        int bestParams = -1;

        for (int i = 0; i < methods.Length; i++)
        {
            var m = methods[i];
            if (!string.Equals(m.Name, nameof(Chara.Die), StringComparison.Ordinal))
                continue;
            int paramLength = m.GetParameters().Length;
            if (paramLength < 3)
                continue;

            if (paramLength > bestParams)
            {
                best = m;
                bestParams = paramLength;
            }
        }

        RuntimeAssertions.Require(best != null, "Chara.Die method not found.");
        return best;
    }

    public static Type RequireType(string fullName)
    {
        var type = AccessTools.TypeByName(fullName);
        RuntimeAssertions.Require(type != null, $"Type not found: {fullName}");
        return type;
    }

    public static Type RequireTypeAssignableTo(string fullName, Type baseType)
    {
        var type = RequireType(fullName);
        RuntimeAssertions.Require(
            baseType.IsAssignableFrom(type),
            $"Type {fullName} is not assignable to {baseType.FullName}.");
        return type;
    }

    public static object RequirePatchInfo(MethodInfo method, string reason)
    {
        var patchInfo = HarmonyCompatFacade.GetPatchInfo(method);
        RuntimeAssertions.Require(patchInfo != null, reason);
        return patchInfo;
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

    public static Elin_ArsMoriendi.SpellUnlock RequireSpellUnlock(string alias)
    {
        var unlocks = Elin_ArsMoriendi.NecromancyManager.SpellUnlocks;
        Elin_ArsMoriendi.SpellUnlock found = null;
        for (int i = 0; i < unlocks.Count; i++)
        {
            var item = unlocks[i];
            if (item == null)
                continue;
            if (string.Equals(item.Alias, alias, StringComparison.Ordinal))
            {
                found = item;
                break;
            }
        }

        RuntimeAssertions.Require(found != null, $"SpellUnlock not found: {alias}");
        return found;
    }

    public static IDictionary GetQuestTransitionRules()
    {
        var quest = Elin_ArsMoriendi.NecromancyManager.Instance.QuestPath;
        RuntimeAssertions.Require(quest != null, "QuestPath is null.");

        var stateMachine = ReflectionCompat.GetFieldOrPropertyValue(quest, "_stateMachine");
        RuntimeAssertions.Require(stateMachine != null, "UnhallowedPath._stateMachine not found.");

        return GetTriggeredTransitionRules(stateMachine);
    }

    public static IDictionary GetTriggeredTransitionRules(object stateMachine)
    {
        RuntimeAssertions.Require(stateMachine != null, "State machine instance is null.");

        var rulesObj = ReflectionCompat.GetFieldOrPropertyValue(stateMachine, "_rules");
        var rules = rulesObj as IDictionary;
        RuntimeAssertions.Require(rules != null, "Transition rules dictionary not found.");
        return rules;
    }

    public static void RequireQuestRule(
        IDictionary rules,
        Elin_ArsMoriendi.UnhallowedStage from,
        string trigger,
        Elin_ArsMoriendi.UnhallowedStage to)
    {
        RequireTriggeredRule(rules, (int)from, trigger, (int)to);
    }

    public static void RequireTriggeredRule(
        IDictionary rules,
        int fromState,
        string trigger,
        int toState)
    {
        foreach (DictionaryEntry entry in rules)
        {
            var key = entry.Key;
            var rule = entry.Value;

            var fromObj = ReflectionCompat.GetFieldOrPropertyValue(key, "Item1", "state");
            var triggerObj = ReflectionCompat.GetFieldOrPropertyValue(key, "Item2", "trigger");
            var toObj = ReflectionCompat.GetFieldOrPropertyValue(rule, "To", "to");

            if (fromObj == null || triggerObj == null || toObj == null)
                continue;

            int fromValue;
            int toValue;
            try
            {
                fromValue = Convert.ToInt32(fromObj);
                toValue = Convert.ToInt32(toObj);
            }
            catch
            {
                continue;
            }

            var triggerValue = triggerObj as string;
            if (triggerValue == null)
                continue;

            if (fromValue == fromState
                && toValue == toState
                && string.Equals(triggerValue, trigger, StringComparison.Ordinal))
            {
                return;
            }
        }

        RuntimeAssertions.Require(
            false,
            $"Quest transition rule missing: {fromState} --({trigger})-> {toState}");
    }

    public static void RequireBranchTargets(
        IDictionary rules,
        int fromState,
        string trigger,
        params int[] expectedTargets)
    {
        RuntimeAssertions.Require(expectedTargets != null && expectedTargets.Length > 0, "expectedTargets is empty.");

        object matchedRule = null;
        foreach (DictionaryEntry entry in rules)
        {
            var key = entry.Key;
            var fromObj = ReflectionCompat.GetFieldOrPropertyValue(key, "Item1", "state");
            var triggerObj = ReflectionCompat.GetFieldOrPropertyValue(key, "Item2", "trigger");
            if (fromObj == null || triggerObj == null)
                continue;

            int fromValue;
            try
            {
                fromValue = Convert.ToInt32(fromObj);
            }
            catch
            {
                continue;
            }

            var triggerValue = triggerObj as string;
            if (triggerValue == null)
                continue;

            if (fromValue == fromState && string.Equals(triggerValue, trigger, StringComparison.Ordinal))
            {
                matchedRule = entry.Value;
                break;
            }
        }

        RuntimeAssertions.Require(
            matchedRule != null,
            $"Quest branch rule missing: state={fromState} trigger={trigger}");

        var allowedTargetsObj = ReflectionCompat.GetFieldOrPropertyValue(matchedRule, "AllowedTargets", "allowedTargets");
        var allowedTargets = ReflectionCompat.AsEnumerable(allowedTargetsObj);
        RuntimeAssertions.Require(allowedTargets != null, "AllowedTargets not found on matched rule.");

        var actualTargets = new HashSet<int>();
        foreach (var target in allowedTargets)
        {
            if (target == null)
                continue;
            try
            {
                actualTargets.Add(Convert.ToInt32(target));
            }
            catch
            {
            }
        }

        for (int i = 0; i < expectedTargets.Length; i++)
        {
            int expected = expectedTargets[i];
            RuntimeAssertions.Require(
                actualTargets.Contains(expected),
                $"Branch target missing: state={fromState} trigger={trigger} target={expected}");
        }
    }

    public static void CaptureDialogFlagWithRollback(RuntimeTestContext ctx, string key)
    {
        RuntimeAssertions.Require(ctx != null, "RuntimeTestContext is null.");
        RuntimeAssertions.Require(!string.IsNullOrEmpty(key), "Dialog flag key is empty.");
        RuntimeAssertions.Require(EClass.player != null, "EClass.player is null.");
        RuntimeAssertions.Require(EClass.player.dialogFlags != null, "dialogFlags is null.");

        var flags = EClass.player.dialogFlags;
        bool hadValue = flags.TryGetValue(key, out int originalValue);
        ctx.RegisterRollback("restore_dialog_flag:" + key, () =>
        {
            if (EClass.player == null || EClass.player.dialogFlags == null)
                return;

            if (hadValue)
                EClass.player.dialogFlags[key] = originalValue;
            else
                EClass.player.dialogFlags.Remove(key);
        });
    }

    public static int GetDialogFlag(string key)
    {
        return Elin_ArsMoriendi.DialogFlagStore.GetInt(EClass.player?.dialogFlags, key);
    }

    public static void SetDialogFlag(string key, int value)
    {
        Elin_ArsMoriendi.DialogFlagStore.SetInt(EClass.player?.dialogFlags, key, value);
    }
}
