using System;
using Elin_NiComment.Llm;
using HarmonyLib;
using UnityEngine;

namespace Elin_NiComment
{
    // ========== Tier S ==========

    [HarmonyPatch(typeof(Card), nameof(Card.Die))]
    static class CardDiePatch
    {
        static void Postfix(Card __instance, Card origin)
        {
            try
            {
                if (!NiCommentAPI.IsReady || CommentTrigger.Instance == null) return;

                if (__instance.IsPC)
                {
                    CommentTrigger.Instance.FireBarrage(
                        CommentTexts.PcDeath, new Color(1f, 0.27f, 0.27f));
                }
                else if (__instance.isChara && EClass.pc != null
                    && __instance.LV >= EClass.pc.LV
                    && origin != null && origin.IsPC)
                {
                    CommentTrigger.Instance.FireBarrage(
                        CommentTexts.StrongKill, new Color(1f, 0.84f, 0f));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NiComment] CardDiePatch: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Quest), nameof(Quest.OnComplete))]
    static class QuestCompletePatch
    {
        static void Postfix()
        {
            try
            {
                if (!NiCommentAPI.IsReady || CommentTrigger.Instance == null) return;
                CommentTrigger.Instance.FireBarrage(
                    CommentTexts.QuestComplete, new Color(1f, 0.84f, 0f));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NiComment] QuestCompletePatch: {ex.Message}");
            }
        }
    }

    // ========== SayRaw → LLM ==========

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
