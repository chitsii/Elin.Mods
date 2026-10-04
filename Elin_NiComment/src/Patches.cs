using System;
using Elin_NiComment.Llm;
using HarmonyLib;
using UnityEngine;

namespace Elin_NiComment
{
    // ========== Tier S ==========

    [HarmonyPatch(typeof(Chara), nameof(Chara.Die))]
    static class CharaDiePatch
    {
        private static readonly LifecycleEventGate DeathEvents = new LifecycleEventGate();

        static void Prefix(Chara __instance, out LifecycleEventGate.State __state)
        {
            __state = DeathEvents.Begin(__instance, __instance == null || __instance.isDead);
        }

        static void Postfix(Chara __instance, Card origin, LifecycleEventGate.State __state)
        {
            try
            {
                if (!DeathEvents.Finish(__state, __instance != null && __instance.isDead)) return;
                if (!NiCommentAPI.IsReady || CommentTrigger.Instance == null) return;

                if (__instance.IsPC)
                {
                    CommentTrigger.Instance.FireBarrage(
                        CommentTexts.PcDeath, new Color(1f, 0.27f, 0.27f));
                }
                else if (EClass.pc != null
                    && __instance.LV >= EClass.pc.LV
                    && origin != null && origin.IsPC)
                {
                    CommentTrigger.Instance.FireBarrage(
                        CommentTexts.StrongKill, new Color(1f, 0.84f, 0f));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NiComment] CharaDiePatch: {ex.Message}");
            }
        }

        static Exception Finalizer(Exception __exception, LifecycleEventGate.State __state)
        {
            DeathEvents.Abort(__state);
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Quest), nameof(Quest.Complete))]
    static class QuestCompletePatch
    {
        private static readonly LifecycleEventGate CompleteEvents = new LifecycleEventGate();

        static void Prefix(Quest __instance, out LifecycleEventGate.State __state)
        {
            __state = CompleteEvents.Begin(__instance, __instance == null || __instance.isComplete);
        }

        static void Postfix(Quest __instance, LifecycleEventGate.State __state)
        {
            try
            {
                if (!CompleteEvents.Finish(__state, __instance != null && __instance.isComplete)) return;
                if (!NiCommentAPI.IsReady || CommentTrigger.Instance == null) return;

                CommentTrigger.Instance.FireBarrage(
                    CommentTexts.QuestComplete, new Color(1f, 0.84f, 0f));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NiComment] QuestCompletePatch: {ex.Message}");
            }
        }

        static Exception Finalizer(Exception __exception, LifecycleEventGate.State __state)
        {
            CompleteEvents.Abort(__state);
            return __exception;
        }
    }

    // ========== SayRaw -> LLM ==========

    [HarmonyPatch(typeof(Msg), nameof(Msg.SayRaw), new Type[] { typeof(string) })]
    static class MsgSayRawPatch
    {
        static void Postfix(string __result)
        {
            try
            {
                if (string.IsNullOrEmpty(__result)) return;
                if (LlmReactionService.Instance == null || !LlmReactionService.Instance.IsActive) return;
                LlmReactionService.Instance.EnqueueGameEvent(__result);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NiComment] MsgSayRawPatch: {ex.Message}");
            }
        }
    }

}
