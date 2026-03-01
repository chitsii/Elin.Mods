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

        public void SetTarget(Card card) => _card = card;

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
            transform.position = renderer.PositionCenter();
        }
    }
}
