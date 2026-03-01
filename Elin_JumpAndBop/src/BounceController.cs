using UnityEngine;

namespace Elin_JumpAndBop
{
    public static class BounceController
    {
        // Decay bounce (wait/stomp)
        private const float WaitFrequency = 4.0f;
        private const float WaitDecay = 0.4f;

        // Sustained bounce (AI_Fuck)
        private const float SexFrequency = 3.5f;
        private const float SexIrregularity = 0.3f;
        private const float SexAmplitudeMultiplier = 1.8f;

        // State
        private static float lastTriggerTime = -999f;
        public static bool sustainedMode;

        public static bool IsActive =>
            sustainedMode || (Time.time - lastTriggerTime < WaitDecay);

        /// <summary>
        /// Trigger a decaying bounce (e.g. wait/stomp action).
        /// Resets the decay timer; phase continues smoothly on rapid presses.
        /// </summary>
        public static void TriggerBounce()
        {
            lastTriggerTime = Time.time;
        }

        /// <summary>
        /// Enter sustained bounce mode (e.g. AI_Fuck).
        /// </summary>
        public static void StartSustained()
        {
            sustainedMode = true;
        }

        /// <summary>
        /// Exit sustained mode and transition to a decay tail.
        /// </summary>
        public static void StopSustained()
        {
            sustainedMode = false;
            lastTriggerTime = Time.time;
        }

        /// <summary>
        /// Calculate the current Y offset for the bounce animation.
        /// Uses |sin| waveform so the sprite only moves upward (never below ground).
        /// </summary>
        public static float GetCurrentOffset()
        {
            float amplitude = ModConfig.JumpHeight.Value;

            if (sustainedMode)
            {
                float t = Time.time;
                float main = Mathf.Abs(Mathf.Sin(t * SexFrequency * 2f * Mathf.PI));
                float sub = Mathf.Abs(Mathf.Sin(t * SexFrequency * 1.618f * 2f * Mathf.PI));
                float blend = main * (1f - SexIrregularity) + sub * SexIrregularity;
                return blend * amplitude * SexAmplitudeMultiplier;
            }

            float elapsed = Time.time - lastTriggerTime;
            if (elapsed >= WaitDecay)
                return 0f;

            float decay = Mathf.Clamp01(1f - elapsed / WaitDecay);
            float wave = Mathf.Abs(Mathf.Sin(Time.time * WaitFrequency * 2f * Mathf.PI));
            return wave * amplitude * decay;
        }
    }
}
