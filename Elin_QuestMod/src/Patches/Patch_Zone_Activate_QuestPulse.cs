using System;
using System.Reflection;
using Elin_QuestMod.Quest;
using HarmonyLib;

namespace Elin_QuestMod.Patches
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
                    ModLog.Error("Patch target not found: Zone.Activate");
                }

                return best;
            }
            catch (Exception ex)
            {
                ModLog.Error("Patch target resolution failed: " + ex.Message);
                return null;
            }
        }

        public static void Postfix()
        {
            try
            {
                QuestFlow.Pulse();
            }
            catch (Exception ex)
            {
                ModLog.Warn("Zone.Activate postfix failed: " + ex.Message);
            }
        }
    }
}
