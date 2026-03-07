using UnityEngine;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Follows a Card's rendered position each frame.
    /// Used as a fallback when the card has no persistent actor transform
    /// (e.g. batch-rendered NPCs / servants).
    /// </summary>
    public class CardPositionTracker : MonoBehaviour
    {
        private Card? _card;
        private float _yOffset;
        private bool _headOffsetResolved;
        private bool _enableHeadOffsetRecovery;

        /// <summary>
        /// Retarget only. Keeps existing offset/recovery state unchanged.
        /// </summary>
        public void SetTarget(Card card)
        {
            _card = card;
        }

        /// <summary>
        /// Set or preserve offset.
        /// - yOffset = NaN: keep previous offset value.
        /// - enableHeadOffsetRecovery = true: if unresolved, try recomputing from sprite once actor appears.
        /// </summary>
        public void SetTarget(Card card, float yOffset, bool enableHeadOffsetRecovery)
        {
            _card = card;
            if (!float.IsNaN(yOffset))
            {
                _yOffset = yOffset;
                _headOffsetResolved = true;
            }
            _enableHeadOffsetRecovery = _enableHeadOffsetRecovery || enableHeadOffsetRecovery;
        }

        void LateUpdate()
        {
            if (_card == null)
            {
                Destroy(gameObject);
                return;
            }

            // Title / loading transitions can leave static game state temporarily null.
            // Avoid touching card zone-dependent properties in that window.
            if (EClass.game == null || EClass._zone == null || EClass._map == null)
            {
                Destroy(gameObject);
                return;
            }

            try
            {
                if (_card.isDestroyed || !_card.IsAliveInCurrentZone)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            catch
            {
                Destroy(gameObject);
                return;
            }

            var renderer = _card.renderer;
            if (renderer == null) return;

            if (_enableHeadOffsetRecovery && !_headOffsetResolved)
            {
                if (SpriteHeadAnchorUtil.TryGetHeadOffsetWorld(_card, out var computedOffset))
                {
                    _yOffset = computedOffset;
                    _headOffsetResolved = true;
                }
            }

            transform.position = renderer.PositionCenter() + new Vector3(0f, _yOffset, 0f);
        }
    }
}
