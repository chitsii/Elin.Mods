using System;
using System.Reflection;

namespace Elin_ArsMoriendi
{
    internal static class SafeInvoke
    {
        [CompatibilityPatch("EA23.184+", "Chara.SetMainElement overload differences (1/2/3 args).")]
        public static bool TrySetMainElement(Chara servant, int mainElementId)
        {
            if (servant == null) return false;

            var resolved = MethodResolver.Resolve(CompatSymbol.CharaSetMainElement);
            if (!resolved.IsResolved || resolved.Method == null)
                return false;

            try
            {
                var p = resolved.Method.GetParameters().Length;
                if (p >= 3)
                    resolved.Method.Invoke(servant, new object[] { mainElementId, 0, true });
                else if (p == 2)
                    resolved.Method.Invoke(servant, new object[] { mainElementId, 0 });
                else if (p == 1)
                    resolved.Method.Invoke(servant, new object[] { mainElementId });
                else
                    resolved.Method.Invoke(servant, null);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"SafeInvoke SetMainElement failed: {ex.Message}");
                return false;
            }
        }

        [CompatibilityPatch("EA23.184+", "Chara.UnmakeMinion/ReleaseMinion rename compatibility.")]
        public static bool TryDetachMinion(Chara servant)
        {
            if (servant == null) return false;

            var resolved = MethodResolver.Resolve(CompatSymbol.CharaDetachMinion);
            if (!resolved.IsResolved || resolved.Method == null)
            {
                ModLog.Warn("SafeInvoke DetachMinion failed: target method unresolved.");
                return false;
            }

            try
            {
                resolved.Method.Invoke(servant, null);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"SafeInvoke DetachMinion failed: {ex.Message}");
                return false;
            }
        }

        [CompatibilityPatch("EA23.184+", "Chara.Die signature compatibility.")]
        public static bool TryDie(Chara chara, Card? origin = null, AttackSource attackSource = AttackSource.None, Chara? originalTarget = null)
        {
            if (chara == null) return false;

            var resolved = MethodResolver.Resolve(CompatSymbol.CharaDie);
            if (!resolved.IsResolved || resolved.Method == null)
            {
                ModLog.Warn("SafeInvoke Die failed: target method unresolved.");
                return false;
            }

            try
            {
                object?[] args = BuildDieArgs(resolved.Method.GetParameters(), origin, attackSource, originalTarget);
                resolved.Method.Invoke(chara, args);
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"SafeInvoke Die failed for {chara.Name} (uid={chara.uid}): {ex.Message}");
                return false;
            }
        }

        [CompatibilityPatch("EA23.184+", "Quest.Create/CreateQuest compatibility.")]
        public static Quest? TryCreateQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId))
                return null;

            var resolved = MethodResolver.Resolve(CompatSymbol.QuestCreate);
            if (!resolved.IsResolved || resolved.Method == null)
            {
                ModLog.Warn("SafeInvoke CreateQuest failed: target method unresolved.");
                return null;
            }

            try
            {
                object?[] args = BuildQuestCreateArgs(resolved.Method.GetParameters(), questId);
                var created = resolved.Method.Invoke(null, args);
                return created as Quest;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"SafeInvoke CreateQuest failed for {questId}: {ex.Message}");
                return null;
            }
        }

        [CompatibilityPatch("EA23.184", "Point.GetNearestPoint parameter change compatibility (4/5 args).")]
        public static Point GetNearestPoint(Point origin, bool allowBlock, bool allowChara, bool allowInstalled = true, bool ignoreCenter = false)
        {
            var resolved = MethodResolver.Resolve(CompatSymbol.PointGetNearestPoint);
            if (!resolved.IsResolved || resolved.Method == null)
            {
                ModLog.Error("SafeInvoke GetNearestPoint failed: target method unresolved.");
                return origin;
            }

            try
            {
                object?[] args = BuildNearestPointArgs(
                    resolved.Method.GetParameters(),
                    allowBlock,
                    allowChara,
                    allowInstalled,
                    ignoreCenter);
                var result = resolved.Method.Invoke(origin, args) as Point;
                return result ?? origin;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"SafeInvoke GetNearestPoint failed: {ex.Message}");
                return origin;
            }
        }

        private static object?[] BuildDieArgs(ParameterInfo[] parameters, Card? origin, AttackSource attackSource, Chara? originalTarget)
        {
            object?[] args = new object?[parameters.Length];
            if (parameters.Length > 0) args[0] = null;
            if (parameters.Length > 1) args[1] = origin;
            if (parameters.Length > 2) args[2] = attackSource;
            if (parameters.Length > 3) args[3] = originalTarget;
            for (int i = 4; i < parameters.Length; i++)
                args[i] = GetDefaultValue(parameters[i]);
            return args;
        }

        private static object?[] BuildQuestCreateArgs(ParameterInfo[] parameters, string questId)
        {
            object?[] args = new object?[parameters.Length];
            if (parameters.Length > 0) args[0] = questId;
            for (int i = 1; i < parameters.Length; i++)
                args[i] = GetDefaultValue(parameters[i]);
            return args;
        }

        private static object?[] BuildNearestPointArgs(
            ParameterInfo[] parameters,
            bool allowBlock,
            bool allowChara,
            bool allowInstalled,
            bool ignoreCenter)
        {
            object?[] args = new object?[parameters.Length];
            bool[] boolValues = { allowBlock, allowChara, allowInstalled, ignoreCenter };
            int boolIndex = 0;

            for (int i = 0; i < parameters.Length; i++)
            {
                var type = parameters[i].ParameterType;
                if (type == typeof(bool))
                {
                    args[i] = boolIndex < boolValues.Length ? boolValues[boolIndex] : false;
                    boolIndex++;
                    continue;
                }

                if (type == typeof(int))
                {
                    args[i] = 0;
                    continue;
                }

                args[i] = GetDefaultValue(parameters[i]);
            }

            return args;
        }

        private static object? GetDefaultValue(ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue)
                return parameter.DefaultValue;
            if (parameter.ParameterType.IsValueType)
                return Activator.CreateInstance(parameter.ParameterType);
            return null;
        }
    }
}
