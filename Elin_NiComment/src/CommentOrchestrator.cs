using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Elin_NiComment
{
    /// <summary>
    /// Internal rendering engine. Do not call directly — use <see cref="NiCommentAPI"/>.
    /// Comments are queued via Enqueue() and spawned during Update().
    /// </summary>
    public class CommentOrchestrator : MonoBehaviour
    {
        private CommentPool _pool;
        private LaneManager _laneManager;
        private readonly Queue<CommentRequest> _queue = new Queue<CommentRequest>();

        private const int MaxSpawnsPerFrame = 3;

        public static CommentOrchestrator Create()
        {
            var go = new GameObject("NiCommentCanvas");
            go.hideFlags = HideFlags.DontSave;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = go.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            var orchestrator = go.AddComponent<CommentOrchestrator>();
            orchestrator.Init();
            return orchestrator;
        }

        private void Init()
        {
            var font = ResolveFont();
            _pool = new CommentPool(transform, ModConfig.PoolSize.Value, font);
            _laneManager = new LaneManager();
        }

        private Font ResolveFont()
        {
            try
            {
                var skinFont = SkinManager.Instance?.fontSet?.ui?.source?.font;
                if (skinFont != null) return skinFont;
            }
            catch (System.Exception)
            {
            }

            Debug.LogWarning("[NiComment] Could not resolve game font, using Arial fallback.");
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /// <summary>
        /// Thread-safe enqueue. Called from Harmony patches or any context.
        /// Actual UI work is deferred to Update().
        /// </summary>
        public void Enqueue(CommentRequest request)
        {
            _queue.Enqueue(request);
        }

        private void Update()
        {
            _laneManager.RefreshTrailingEdges(_pool.All);

            int spawned = 0;
            while (_queue.Count > 0 && spawned < MaxSpawnsPerFrame)
            {
                var request = _queue.Dequeue();
                SpawnComment(request);
                spawned++;
            }
        }

        private void SpawnComment(CommentRequest request)
        {
            var element = _pool.Get();
            if (element == null) return;

            var fontSize = request.EffectiveFontSize;
            var speed = ModConfig.ScrollSpeed.Value;

            // Temporarily activate to measure text width
            element.gameObject.SetActive(true);
            element.Initialize(request.Text, 0f, speed, request.Color, fontSize, _pool, -1);

            var width = element.Width;
            element.gameObject.SetActive(false);

            var laneIndex = _laneManager.TryAssignLane(width);
            if (laneIndex < 0)
            {
                _pool.Return(element);
                return;
            }

            var yPos = _laneManager.GetLaneY(laneIndex);
            element.Initialize(request.Text, yPos, speed, request.Color, fontSize, _pool, laneIndex);
            _laneManager.UpdateLaneTrailingEdge(laneIndex, element.RightEdgeX());
        }
    }
}
