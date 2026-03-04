using UnityEngine;

namespace Elin_NiComment
{
    /// <summary>
    /// Public entry point for sending comments.
    /// Calls are queued and processed during Unity's Update cycle,
    /// making it safe to call from Harmony patches.
    /// </summary>
    public static class NiCommentAPI
    {
        private static CommentOrchestrator _orchestrator;

        /// <summary>True when the overlay is initialized and ready to display comments.</summary>
        public static bool IsReady => _orchestrator != null;

        internal static void Bind(CommentOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        internal static void Unbind()
        {
            _orchestrator = null;
        }

        public static void Send(string text)
        {
            if (_orchestrator == null) return;
            _orchestrator.Enqueue(new CommentRequest(text));
        }

        public static void Send(string text, Color color)
        {
            if (_orchestrator == null) return;
            _orchestrator.Enqueue(new CommentRequest(text, color));
        }

        public static void Send(CommentRequest request)
        {
            if (_orchestrator == null) return;
            _orchestrator.Enqueue(request);
        }
    }
}
