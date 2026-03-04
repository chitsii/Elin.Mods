using System.Collections.Generic;
using UnityEngine;

namespace Elin_NiComment
{
    public class CommentThrottle
    {
        private readonly Dictionary<string, float> _lastFireTime = new Dictionary<string, float>();

        public bool TryFire(string category, float cooldownSeconds)
        {
            var now = Time.unscaledTime;

            if (_lastFireTime.TryGetValue(category, out var lastTime))
            {
                if (now - lastTime < cooldownSeconds) return false;
            }

            _lastFireTime[category] = now;
            return true;
        }
    }
}
