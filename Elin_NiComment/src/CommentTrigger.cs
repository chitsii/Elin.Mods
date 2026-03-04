using System.Collections;
using UnityEngine;

namespace Elin_NiComment
{
    public class CommentTrigger : MonoBehaviour
    {
        private static CommentTrigger _instance;
        private CommentThrottle _throttle;

        private const float CooldownTierA = 5f;
        private const float CooldownTierB = 3f;

        public static CommentTrigger Instance => _instance;

        public void Initialize()
        {
            _instance = this;
            _throttle = new CommentThrottle();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>Tier S: barrage of 3-5 comments with staggered timing.</summary>
        public void FireBarrage(string eventId, Color color)
        {
            if (!NiCommentAPI.IsReady) return;
            // Enqueue all barrage comments immediately — the orchestrator's
            // per-frame limit will naturally stagger them across frames.
            var count = Random.Range(3, 6);
            for (int i = 0; i < count; i++)
            {
                var text = CommentTexts.GetRandom(eventId);
                if (text != null) NiCommentAPI.Send(text, color);
            }
        }

        /// <summary>Tier A: single colored comment with cooldown.</summary>
        public void FireSingle(string eventId, Color color)
        {
            if (!NiCommentAPI.IsReady) return;
            if (!_throttle.TryFire(eventId, CooldownTierA)) return;

            var text = CommentTexts.GetRandom(eventId);
            if (text != null) NiCommentAPI.Send(text, color);
        }

        /// <summary>Tier B: ambient comment (white) with cooldown. Pass explicit text or use random from dictionary.</summary>
        public void FireAmbient(string eventId, string text = null)
        {
            if (!NiCommentAPI.IsReady) return;
            if (!_throttle.TryFire(eventId, CooldownTierB)) return;

            text = text ?? CommentTexts.GetRandom(eventId);
            if (text != null) NiCommentAPI.Send(text);
        }
    }
}
