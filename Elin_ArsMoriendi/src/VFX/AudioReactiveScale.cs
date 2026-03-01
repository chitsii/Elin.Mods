using System;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Audio-reactive party controller.
    /// Uses relative loudness + spectral change + transient emphasis to avoid flat motion at loud sections.
    /// </summary>
    public class AudioReactiveScale : MonoBehaviour
    {
        private readonly float[] _samples = new float[128];
        private readonly float[] _spectrum = new float[128];

        private Vector3 _baseScale;
        private AudioSource? _source;
        private float _searchCooldown;
        private float _currentScale = 1f;
        private bool _loggedSource;

        private float _minScale = 0.93f;
        private float _maxScale = 1.18f;
        private float _noiseFloor = 0.004f;
        private float _gain = 24f;
        private float _smoothing = 10f;
        private bool _pulseFallback = true;
        private float _pulseHz = 2.2f;
        private float _searchInterval = 0.8f;
        private float _lowSignalPulseBlend = 0.45f;
        private const float AnalysisHz = 15f;
        private const float AnalysisInterval = 1f / AnalysisHz;
        private float _analysisTimer;
        private bool _cachedHasAudio;
        private float _cachedLevel01;
        private float _cachedLow01;
        private float _cachedMid01;
        private float _cachedHigh01;
        private float _cachedFlux01;

        private float _slowRms;
        private float _prevRms;
        private float _beatEnvelope;
        private float _rayYaw;

        private Transform? _confettiTransform;
        private Transform? _raysTransform;
        private Transform? _pulseTransform;
        private ParticleSystem? _confettiPs;
        private ParticleSystem? _raysPs;
        private ParticleSystem? _pulsePs;
        private float _confettiBaseRate = 120f;
        private Color _confettiBaseColor = Color.white;
        private Color _raysBaseColor = Color.white;
        private Color _pulseBaseColor = Color.white;

        public void Initialize(
            float minScale = 0.93f,
            float maxScale = 1.18f,
            float noiseFloor = 0.004f,
            float gain = 24f,
            float smoothing = 10f,
            float lowSignalPulseBlend = 0.45f
        )
        {
            _minScale = minScale;
            _maxScale = Mathf.Max(minScale, maxScale);
            _noiseFloor = Mathf.Max(0f, noiseFloor);
            _gain = Mathf.Max(0.1f, gain);
            _smoothing = Mathf.Max(1f, smoothing);
            _lowSignalPulseBlend = Mathf.Clamp01(lowSignalPulseBlend);
        }

        void Awake()
        {
            _baseScale = transform.localScale;
            CacheLayerRefs();
        }

        void OnEnable()
        {
            _baseScale = transform.localScale;
            _currentScale = 1f;
            _searchCooldown = 0f;
            _slowRms = 0f;
            _prevRms = 0f;
            _beatEnvelope = 0f;
            _rayYaw = 0f;
            _analysisTimer = 0f; // force immediate first analysis
            _cachedHasAudio = false;
            _cachedLevel01 = 0f;
            _cachedLow01 = 0f;
            _cachedMid01 = 0f;
            _cachedHigh01 = 0f;
            _cachedFlux01 = 0f;
            CacheLayerRefs();
        }

        void LateUpdate()
        {
            EnsureSource();

            _analysisTimer -= Time.unscaledDeltaTime;
            if (_analysisTimer <= 0f)
            {
                _analysisTimer += AnalysisInterval;
                if (_analysisTimer < 0f) _analysisTimer = AnalysisInterval;

                _cachedHasAudio = TryComputeFeatures(
                    out _cachedLevel01,
                    out _cachedLow01,
                    out _cachedMid01,
                    out _cachedHigh01,
                    out _cachedFlux01);
            }

            bool hasAudio = _cachedHasAudio;
            float level01 = _cachedLevel01;
            float low01 = _cachedLow01;
            float mid01 = _cachedMid01;
            float high01 = _cachedHigh01;
            float flux01 = _cachedFlux01;

            float target01;
            if (hasAudio)
            {
                target01 = ComposeTarget(level01, low01, mid01, high01, flux01);
            }
            else if (_pulseFallback)
            {
                float pulse = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * _pulseHz);
                target01 = 0.5f + 0.5f * pulse;
            }
            else
            {
                target01 = 0.5f;
            }

            float targetScale = Mathf.Lerp(_minScale, _maxScale, Mathf.Clamp01(target01));
            float t = Mathf.Clamp01(Time.unscaledDeltaTime * _smoothing);
            _currentScale = Mathf.Lerp(_currentScale, targetScale, t);
            transform.localScale = _baseScale * _currentScale;

            ApplyLayerMotion(hasAudio, low01, mid01, high01, flux01);
        }

        private void EnsureSource()
        {
            if (_source != null && _source.isActiveAndEnabled && _source.isPlaying)
                return;

            _searchCooldown -= Time.unscaledDeltaTime;
            if (_searchCooldown > 0f) return;
            _searchCooldown = _searchInterval;

            _source = FindLikelyMusicSource();
            if (_source != null && !_loggedSource)
            {
                _loggedSource = true;
                ModLog.Log(
                    "AudioReactiveScale source locked: {0}/{1}",
                    _source.gameObject.name,
                    _source.clip != null ? _source.clip.name : "(no clip)");
            }
        }

        private static AudioSource? FindLikelyMusicSource()
        {
            AudioSource[] sources;
            try
            {
                sources = FindObjectsOfType<AudioSource>();
            }
            catch
            {
                return null;
            }

            AudioSource? best = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < sources.Length; i++)
            {
                var src = sources[i];
                if (src == null || !src.isActiveAndEnabled || !src.isPlaying || src.mute)
                    continue;

                float score = src.volume;
                if (src.loop) score += 1.0f;
                if (src.clip != null)
                {
                    if (src.clip.length >= 20f) score += 1.5f;
                    if (src.clip.length >= 60f) score += 1.0f;

                    string clipName = src.clip.name.ToLowerInvariant();
                    if (clipName.Contains("bgm") || clipName.Contains("music") || clipName.Contains("rap"))
                        score += 1.2f;
                }

                string goName = src.gameObject.name.ToLowerInvariant();
                if (goName.Contains("bgm") || goName.Contains("music") || goName.Contains("sound"))
                    score += 0.8f;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = src;
                }
            }

            return best;
        }

        private bool TryComputeFeatures(
            out float level01,
            out float low01,
            out float mid01,
            out float high01,
            out float flux01)
        {
            level01 = 0f;
            low01 = 0f;
            mid01 = 0f;
            high01 = 0f;
            flux01 = 0f;

            if (_source == null || !_source.isActiveAndEnabled || !_source.isPlaying)
                return false;

            try
            {
                _source.GetOutputData(_samples, 0);
                _source.GetSpectrumData(_spectrum, 0, FFTWindow.BlackmanHarris);
            }
            catch
            {
                return false;
            }

            float sum = 0f;
            for (int i = 0; i < _samples.Length; i++)
            {
                float s = _samples[i];
                sum += s * s;
            }

            float rms = Mathf.Sqrt(sum / _samples.Length);
            if (rms < _noiseFloor * 0.5f)
            {
                _beatEnvelope = Mathf.MoveTowards(_beatEnvelope, 0f, Time.unscaledDeltaTime * 2.0f);
                return false;
            }

            if (_slowRms <= 0f) _slowRms = rms;
            float slowT = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 1.2f);
            _slowRms = Mathf.Lerp(_slowRms, rms, slowT);

            // Relative loudness against moving baseline keeps motion alive in sustained loud sections.
            float relative = rms / Mathf.Max(0.0001f, _slowRms);
            level01 = Mathf.Clamp01((relative - 0.80f) * 2.8f);

            float low = BandEnergy(1, 6);
            float mid = BandEnergy(7, 22);
            float high = BandEnergy(23, 60);

            low01 = CompressEnergy(low, 380f, 0.90f);
            mid01 = CompressEnergy(mid, 260f, 0.92f);
            high01 = CompressEnergy(high, 210f, 0.95f);

            float delta = Mathf.Max(0f, rms - _prevRms);
            _prevRms = rms;

            // Spectral flux proxy: emphasizes onsets and texture changes.
            float flux = Mathf.Abs(high01 - mid01) * 0.7f + Mathf.Abs(mid01 - low01) * 0.5f;
            float beatCandidate = delta * (_gain * 3.6f) + flux * 0.75f;
            float beatDecay = Mathf.Exp(-Time.unscaledDeltaTime * 5.8f);
            _beatEnvelope = Mathf.Max(_beatEnvelope * beatDecay, beatCandidate);
            flux01 = Mathf.Clamp01(_beatEnvelope);

            return true;
        }

        private float ComposeTarget(float level01, float low01, float mid01, float high01, float flux01)
        {
            float spectralSwing = Mathf.Clamp01(
                Mathf.Abs(low01 - mid01) * 0.8f +
                Mathf.Abs(mid01 - high01) * 0.8f);

            float target01 =
                level01 * 0.30f +
                flux01 * 0.44f +
                spectralSwing * 0.26f;

            // Keep a soft pulse in almost-flat passages.
            if (target01 < 0.09f)
            {
                float pulse01 = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * _pulseHz);
                target01 = Mathf.Lerp(target01, pulse01, _lowSignalPulseBlend * 0.4f);
            }

            return Mathf.Clamp01(target01);
        }

        private void ApplyLayerMotion(bool hasAudio, float low01, float mid01, float high01, float flux01)
        {
            float dt = Time.unscaledDeltaTime;

            if (_raysTransform != null)
            {
                float speed01 = hasAudio ? Mathf.Clamp01(mid01 * 0.65f + flux01 * 0.85f) : 0.25f;
                _rayYaw += dt * Mathf.Lerp(20f, 160f, speed01);
                _raysTransform.localRotation = Quaternion.Euler(0f, _rayYaw, 0f);

                float rayScale = 1f + mid01 * 0.25f + flux01 * 0.55f;
                _raysTransform.localScale = new Vector3(rayScale, rayScale, rayScale);
            }

            if (_pulseTransform != null)
            {
                float pulseScale = 1f + low01 * 0.20f + flux01 * 1.10f;
                _pulseTransform.localScale = new Vector3(pulseScale, pulseScale, pulseScale);
            }

            if (_confettiTransform != null)
            {
                float spread = 1f + high01 * 0.16f;
                _confettiTransform.localScale = new Vector3(spread, 1f, spread);
            }

            if (_confettiPs != null)
            {
                var emission = _confettiPs.emission;
                float intensity01 = hasAudio
                    ? Mathf.Clamp01(high01 * 0.58f + flux01 * 0.72f)
                    : 0.35f;
                float rate = _confettiBaseRate * Mathf.Lerp(0.60f, 2.15f, intensity01);
                emission.rateOverTimeMultiplier = rate;
            }

            ApplyLayerColor(hasAudio, low01, mid01, high01, flux01, dt);
        }

        private void ApplyLayerColor(bool hasAudio, float low01, float mid01, float high01, float flux01, float dt)
        {
            float time = Time.unscaledTime;
            float intensity = hasAudio ? Mathf.Clamp01(0.30f + flux01 * 0.70f) : 0.25f;

            // Rays: vivid hue rotation, driven by mid/high and tempo accents.
            float rayHue = Mathf.Repeat(0.08f + time * 0.03f + mid01 * 0.22f + high01 * 0.31f + flux01 * 0.12f, 1f);
            float raySat = Mathf.Lerp(0.45f, 0.95f, Mathf.Clamp01(high01 * 0.55f + flux01 * 0.70f));
            float rayVal = Mathf.Lerp(0.80f, 1.00f, Mathf.Clamp01(mid01 * 0.50f + flux01 * 0.80f));
            var rayColor = Color.HSVToRGB(rayHue, raySat, rayVal);
            rayColor.a = _raysBaseColor.a;
            SetParticleStartColor(_raysPs, BlendWithBase(_raysBaseColor, rayColor, intensity * 0.90f), dt * 10f);

            // Pulse: low band pushes warmer, highs pull to violet/blue.
            float pulseHue = Mathf.Repeat(0.66f - low01 * 0.35f + high01 * 0.18f + flux01 * 0.08f, 1f);
            float pulseSat = Mathf.Lerp(0.30f, 0.90f, Mathf.Clamp01(low01 * 0.60f + flux01 * 0.70f));
            float pulseVal = Mathf.Lerp(0.76f, 1.00f, Mathf.Clamp01(low01 * 0.45f + flux01 * 0.90f));
            var pulseColor = Color.HSVToRGB(pulseHue, pulseSat, pulseVal);
            pulseColor.a = _pulseBaseColor.a;
            SetParticleStartColor(_pulsePs, BlendWithBase(_pulseBaseColor, pulseColor, intensity), dt * 9f);

            // Confetti: moderate tint shift so original rainbow remains readable.
            float confHue = Mathf.Repeat(0.50f + time * 0.02f + high01 * 0.25f + flux01 * 0.10f, 1f);
            float confSat = Mathf.Lerp(0.20f, 0.70f, Mathf.Clamp01(high01 * 0.65f + flux01 * 0.45f));
            float confVal = Mathf.Lerp(0.88f, 1.00f, Mathf.Clamp01(high01 * 0.40f + flux01 * 0.50f));
            var confColor = Color.HSVToRGB(confHue, confSat, confVal);
            confColor.a = _confettiBaseColor.a;
            SetParticleStartColor(_confettiPs, BlendWithBase(_confettiBaseColor, confColor, intensity * 0.55f), dt * 7f);
        }

        private float BandEnergy(int start, int end)
        {
            int n = _spectrum.Length;
            if (n <= 0) return 0f;
            int s = Mathf.Clamp(start, 0, n - 1);
            int e = Mathf.Clamp(end, s, n - 1);

            float sum = 0f;
            for (int i = s; i <= e; i++)
            {
                sum += _spectrum[i];
            }
            return sum;
        }

        private static float CompressEnergy(float energy, float gain, float scale)
        {
            float x = Mathf.Max(0f, energy * gain);
            return Mathf.Clamp01(Mathf.Log10(1f + x) * scale);
        }

        private void CacheLayerRefs()
        {
            _confettiTransform = FindChildByName(transform, "ConfettiRain");
            _raysTransform = FindChildByName(transform, "DanceFloorRays");
            _pulseTransform = FindChildByName(transform, "BeatPulse");

            _confettiPs = _confettiTransform != null ? _confettiTransform.GetComponent<ParticleSystem>() : null;
            _raysPs = _raysTransform != null ? _raysTransform.GetComponent<ParticleSystem>() : null;
            _pulsePs = _pulseTransform != null ? _pulseTransform.GetComponent<ParticleSystem>() : null;

            if (_confettiPs != null)
            {
                _confettiBaseRate = Mathf.Max(1f, _confettiPs.emission.rateOverTimeMultiplier);
                _confettiBaseColor = ExtractMainColor(_confettiPs.main.startColor, Color.white);
            }

            if (_raysPs != null)
            {
                _raysBaseColor = ExtractMainColor(_raysPs.main.startColor, new Color(1f, 0.95f, 0.8f, 0.75f));
            }

            if (_pulsePs != null)
            {
                _pulseBaseColor = ExtractMainColor(_pulsePs.main.startColor, new Color(0.9f, 0.92f, 1f, 0.8f));
            }
        }

        private static void SetParticleStartColor(ParticleSystem? ps, Color target, float lerpSpeed)
        {
            if (ps == null) return;
            var main = ps.main;
            Color current = ExtractMainColor(main.startColor, target);
            float t = Mathf.Clamp01(Time.unscaledDeltaTime * Mathf.Max(0f, lerpSpeed));
            main.startColor = Color.Lerp(current, target, t);
        }

        private static Color BlendWithBase(Color baseColor, Color reactiveColor, float amount01)
        {
            float a = Mathf.Clamp01(amount01);
            Color blended = Color.Lerp(baseColor, reactiveColor, a);
            blended.a = baseColor.a;
            return blended;
        }

        private static Color ExtractMainColor(ParticleSystem.MinMaxGradient mmg, Color fallback)
        {
            switch (mmg.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return mmg.color;
                case ParticleSystemGradientMode.TwoColors:
                    return Color.Lerp(mmg.colorMin, mmg.colorMax, 0.5f);
                default:
                    return fallback;
            }
        }

        private static Transform? FindChildByName(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            int childCount = root.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = root.GetChild(i);
                var found = FindChildByName(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
