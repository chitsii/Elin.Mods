using UnityEngine;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Destroys the owning GameObject when the active map (zone) changes.
    /// </summary>
    public class ZoneChangeDestroyer : MonoBehaviour
    {
        private object? _mapAtSpawn;

        public void CaptureCurrentMap()
        {
            _mapAtSpawn = EClass._map;
        }

        void LateUpdate()
        {
            if (_mapAtSpawn == null) return;
            if (!ReferenceEquals(EClass._map, _mapAtSpawn))
            {
                Destroy(gameObject);
            }
        }
    }
}
