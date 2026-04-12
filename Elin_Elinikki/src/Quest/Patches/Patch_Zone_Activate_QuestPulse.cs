using System;
using System.Reflection;
using Elin_Elinikki.Quest.Quest;
using HarmonyLib;

namespace Elin_Elinikki.Quest.Patches
{
    [HarmonyPatch]
    public static class Patch_Zone_Activate_QuestPulse
    {
        public static MethodBase TargetMethod()
        {
            try
            {
                var methods = typeof(Zone).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo best = null;
                int bestParamCount = int.MaxValue;

                for (int i = 0; i < methods.Length; i++)
                {
                    var method = methods[i];
                    if (method == null || !string.Equals(method.Name, "Activate", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int paramCount = method.GetParameters().Length;
                    if (best == null || paramCount < bestParamCount)
                    {
                        best = method;
                        bestParamCount = paramCount;
                    }
                }

                if (best == null)
                {
                    QuestModLog.Error("Patch target not found: Zone.Activate");
                }

                return best;
            }
            catch (Exception ex)
            {
                QuestModLog.Error("Patch target resolution failed: " + ex.Message);
                return null;
            }
        }

        public static void Postfix()
        {
            try
            {
                ElinikkiQuestFlow.Pulse();
            }
            catch (Exception ex)
            {
                QuestModLog.Warn("Zone.Activate postfix failed: " + ex.Message);
            }
        }
    }
}
