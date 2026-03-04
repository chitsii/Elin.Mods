using System.Collections.Generic;
using UnityEngine;

namespace Elin_NiComment
{
    public class LaneManager
    {
        private int _laneCount;
        private float _laneHeight;
        private float _topMargin;
        private readonly List<float> _laneTrailingEdge;

        public LaneManager()
        {
            _laneTrailingEdge = new List<float>();
            Recalculate();
        }

        public void Recalculate()
        {
            var fontSize = ModConfig.FontSize.Value;
            _laneHeight = fontSize + 4f; // small gap between lanes
            _topMargin = ModConfig.TopMargin.Value;

            // Reference height is 1080 (CanvasScaler)
            var availableHeight = 1080f - _topMargin;
            var maxFromScreen = Mathf.FloorToInt(availableHeight / _laneHeight);
            _laneCount = Mathf.Min(maxFromScreen, ModConfig.MaxLanes.Value);

            while (_laneTrailingEdge.Count < _laneCount)
                _laneTrailingEdge.Add(-9999f);
        }

        public int TryAssignLane(float commentWidth)
        {
            // Ensure lane count is up to date
            if (_laneCount != Mathf.Min(
                Mathf.FloorToInt((1080f - ModConfig.TopMargin.Value) / _laneHeight),
                ModConfig.MaxLanes.Value))
            {
                Recalculate();
            }

            // Minimum gap before a new comment can enter the same lane
            float gapThreshold = 1920f - 40f;

            for (int i = 0; i < _laneCount; i++)
            {
                if (_laneTrailingEdge[i] < gapThreshold)
                {
                    return i;
                }
            }

            return -1; // all lanes busy
        }

        public float GetLaneY(int laneIndex)
        {
            return _topMargin + laneIndex * _laneHeight;
        }

        public void UpdateLaneTrailingEdge(int laneIndex, float rightEdgeX)
        {
            if (laneIndex >= 0 && laneIndex < _laneTrailingEdge.Count)
                _laneTrailingEdge[laneIndex] = rightEdgeX;
        }

        public void RefreshTrailingEdges(IReadOnlyList<CommentElement> allElements)
        {
            // Reset all lanes
            for (int i = 0; i < _laneTrailingEdge.Count; i++)
                _laneTrailingEdge[i] = -9999f;

            // Update from active elements
            foreach (var elem in allElements)
            {
                if (!elem.gameObject.activeSelf) continue;
                var lane = elem.LaneIndex;
                if (lane >= 0 && lane < _laneTrailingEdge.Count)
                {
                    var rightEdge = elem.RightEdgeX();
                    if (rightEdge > _laneTrailingEdge[lane])
                        _laneTrailingEdge[lane] = rightEdge;
                }
            }
        }
    }
}
