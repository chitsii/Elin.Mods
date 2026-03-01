using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Elin_ArsMoriendi
{
    public static class CustomAssetFx
    {
        private const string BundleFileName = "ars_spell_particle";
        private const string LegacyLoopMigrationFlagKey = "chitsii.ars.vfx.owned_loop_migrated_v1";
        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, GameObject> ActiveAttachedFx = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, GameObject> LoopAttachedFx = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, int> LoopLeaseUntilTurn = new Dictionary<string, int>();
        private static readonly List<string> LoopKeyScratch = new List<string>();
        private static readonly string[] LegacyLoopPrefabIds =
        {
            "FxSoulBind",
            "FxPreserveCorpseAura",
            "FxServantShadowTentacleAura",
            "FxServantShadowTentacleAura_SmokeDrip",
            "FxServantShadowTentacleAura_SmokeDrip_v2",
        };
        private static AssetBundle? _bundle;
        private static bool _loadAttempted;
        private static bool _legacyLoopMigrationChecked;
        private static int _lastLoopSweepTurn = int.MinValue;

        private static Transform? GetActorTransform(Card card)
        {
            var renderer = card.renderer;
            if (renderer == null || !renderer.hasActor) return null;
            return renderer.actor != null ? renderer.actor.transform : null;
        }

        public static void PlayAtCard(
            string prefabName,
            Card card,
            float fallbackLifetime = 2f,
            Color? tint = null
        )
        {
            TryPlayAtCard(prefabName, card, fallbackLifetime, tint);
        }

        public static bool TryPlayAtCard(
            string prefabName,
            Card card,
            float fallbackLifetime = 2f,
            Color? tint = null
        )
        {
            if (card == null) return false;
            Vector3 pos = card.pos.PositionCenter();
            if (card.renderer != null)
            {
                pos = card.renderer.PositionCenter();
            }

            return TryPlayAt(prefabName, pos, fallbackLifetime, tint);
        }

        public static bool TryPlayAttachedToCard(
            string prefabName,
            Card card,
            float fallbackLifetime = 2f,
            bool replaceExisting = true,
            bool forceLoop = false,
            Color? tint = null,
            float scaleMultiplier = 1f,
            bool destroyOnZoneChange = false
        )
        {
            if (card == null) return false;

            Vector3 pos = card.pos.PositionCenter();
            Transform? attachParent = null;
            if (card.renderer != null)
            {
                pos = card.renderer.PositionCenter();
                attachParent = GetActorTransform(card);
            }

            string? attachKey = null;
            if (replaceExisting)
            {
                attachKey = $"{prefabName}:{card.uid}";
            }

            return TryPlayInternal(
                prefabName,
                pos,
                fallbackLifetime,
                attachParent,
                trackTarget: card,
                attachKey,
                replaceExisting,
                forceLoop,
                autoDestroy: true,
                ActiveAttachedFx,
                tint,
                scaleMultiplier,
                destroyOnZoneChange
            );
        }

        public static bool TryEnsureLoopAttachedToCard(string prefabName, Card card, Color? tint = null)
        {
            if (card == null || card.isDestroyed) return false;
            if (card is Chara chara && chara.isDead) return false;
            if (!card.IsAliveInCurrentZone) return false;

            var renderer = card.renderer;
            if (renderer == null) return false;

            var actorTransform = GetActorTransform(card);

            string attachKey = $"loop:{prefabName}:{card.uid}";
            if (LoopAttachedFx.TryGetValue(attachKey, out var existing))
            {
                if (existing != null)
                {
                    if (actorTransform != null)
                    {
                        existing.transform.SetParent(actorTransform, true);
                    }
                    else
                    {
                        var tracker = existing.GetComponent<CardPositionTracker>();
                        if (tracker != null) tracker.SetTarget(card);
                    }
                    return true;
                }
                LoopAttachedFx.Remove(attachKey);
            }

            Vector3 pos = renderer.PositionCenter();
            return TryPlayInternal(
                prefabName,
                pos,
                fallbackLifetime: 0f,
                actorTransform,
                trackTarget: card,
                attachKey,
                replaceExisting: true,
                forceLoop: true,
                autoDestroy: false,
                LoopAttachedFx,
                tint
            );
        }

        /// <summary>
        /// Ensure an owner-scoped loop FX is attached to the target card.
        /// The FX is leased and auto-destroyed if not renewed for leaseTurns.
        /// </summary>
        public static bool TryEnsureOwnedLoopAttachedToCard(
            string prefabName,
            Card card,
            string ownerKey,
            int casterUid = 0,
            int leaseTurns = 2,
            Color? tint = null)
        {
            if (card == null || card.isDestroyed) return false;
            if (card is Chara chara && chara.isDead) return false;
            if (!card.IsAliveInCurrentZone) return false;
            if (string.IsNullOrEmpty(ownerKey)) return false;

            var renderer = card.renderer;
            if (renderer == null) return false;

            var actorTransform = GetActorTransform(card);
            string attachKey = BuildOwnedLoopKey(prefabName, card.uid, ownerKey, casterUid);
            int leaseUntilTurn = GetCurrentGameTurn() + Math.Max(1, leaseTurns);

            if (LoopAttachedFx.TryGetValue(attachKey, out var existing))
            {
                if (existing != null)
                {
                    if (actorTransform != null)
                    {
                        existing.transform.SetParent(actorTransform, true);
                    }
                    else
                    {
                        var tracker = existing.GetComponent<CardPositionTracker>();
                        if (tracker != null) tracker.SetTarget(card);
                    }

                    LoopLeaseUntilTurn[attachKey] = leaseUntilTurn;
                    return true;
                }

                LoopAttachedFx.Remove(attachKey);
            }

            Vector3 pos = renderer.PositionCenter();
            bool created = TryPlayInternal(
                prefabName,
                pos,
                fallbackLifetime: 0f,
                actorTransform,
                trackTarget: card,
                attachKey,
                replaceExisting: true,
                forceLoop: true,
                autoDestroy: false,
                LoopAttachedFx,
                tint
            );

            if (created)
            {
                LoopLeaseUntilTurn[attachKey] = leaseUntilTurn;
            }

            return created;
        }

        public static void StopOwnedLoopFx(string prefabName, Card card, string ownerKey, int casterUid = 0)
        {
            if (card == null || string.IsNullOrEmpty(ownerKey)) return;
            string key = BuildOwnedLoopKey(prefabName, card.uid, ownerKey, casterUid);
            DestroyRegisteredFx(LoopAttachedFx, key);
            LoopLeaseUntilTurn.Remove(key);
        }

        public static void StopAttachedFx(string prefabName, Card card)
        {
            if (card == null) return;

            string oneShotKey = $"{prefabName}:{card.uid}";
            DestroyRegisteredFx(ActiveAttachedFx, oneShotKey);

            string loopKey = $"loop:{prefabName}:{card.uid}";
            DestroyRegisteredFx(LoopAttachedFx, loopKey);
            LoopLeaseUntilTurn.Remove(loopKey);

            DestroyOwnedLoopFxForPrefabAndTarget(prefabName, card.uid);
        }

        public static void StopAllAttachedFx(Card card)
        {
            if (card == null) return;

            string suffix = $":{card.uid}";
            DestroyAllWithSuffix(ActiveAttachedFx, suffix);
            DestroyAllWithSuffix(LoopAttachedFx, suffix);
            RemoveLeasesBySuffix(suffix);
            DestroyAllLoopFxForTarget(card.uid);
        }

        /// <summary>
        /// One-time migration cleanup for saves that already contain stale loop FX.
        /// Safe to call repeatedly; executes at most once per save and session.
        /// </summary>
        public static void EnsureLegacyLoopMigrationOnce()
        {
            if (_legacyLoopMigrationChecked) return;
            _legacyLoopMigrationChecked = true;

            var flags = EClass.player?.dialogFlags;
            if (DialogFlagStore.IsTrue(flags, LegacyLoopMigrationFlagKey))
                return;

            try
            {
                // Drop all tracked FX first to guarantee registry consistency.
                DestroyAllRegisteredFx(ActiveAttachedFx);
                DestroyAllRegisteredFx(LoopAttachedFx);
                LoopLeaseUntilTurn.Clear();

                // Then remove known loop prefab remnants that might have escaped registry tracking.
                DestroySceneFxByPrefabNameSet(LegacyLoopPrefabIds);

                DialogFlagStore.SetBool(flags, LegacyLoopMigrationFlagKey, true);
                ModLog.Log("CustomAssetFx: legacy loop migration cleanup applied.");
            }
            catch (Exception ex)
            {
                ModLog.Warn($"CustomAssetFx migration cleanup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Global lease maintenance entrypoint.
        /// Call from a deterministic player-turn-end hook
        /// (e.g., BaseGameScreen.OnEndPlayerTurn postfix).
        /// </summary>
        public static void RunLeaseMaintenance()
        {
            SweepExpiredLoopFxOncePerTurn();
        }

        public static void PlayAt(
            string prefabName,
            Vector3 worldPos,
            float fallbackLifetime = 2f,
            Color? tint = null
        )
        {
            TryPlayAt(prefabName, worldPos, fallbackLifetime, tint);
        }

        public static bool TryPlayAt(
            string prefabName,
            Vector3 worldPos,
            float fallbackLifetime = 2f,
            Color? tint = null
        )
        {
            return TryPlayInternal(
                prefabName,
                worldPos,
                fallbackLifetime,
                attachParent: null,
                trackTarget: null,
                attachKey: null,
                replaceExisting: false,
                forceLoop: false,
                autoDestroy: true,
                registry: null,
                tint
            );
        }

        private static bool TryEnsureLoaded()
        {
            if (_bundle != null) return true;
            if (_loadAttempted) return false;
            _loadAttempted = true;

            try
            {
                string asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
                string path = Path.Combine(asmDir, "Asset", BundleFileName);
                if (!File.Exists(path))
                {
                    ModLog.Warn($"CustomAssetFx bundle not found: {path}");
                    return false;
                }

                _bundle = AssetBundle.LoadFromFile(path);
                if (_bundle == null)
                {
                    ModLog.Warn($"CustomAssetFx failed to load bundle: {path}");
                    return false;
                }

                foreach (var assetName in _bundle.GetAllAssetNames())
                {
                    var prefab = _bundle.LoadAsset<GameObject>(assetName);
                    if (prefab == null) continue;
                    var key = prefab.name;
                    if (!Prefabs.ContainsKey(key))
                    {
                        Prefabs.Add(key, prefab);
                    }
                }

                ModLog.Log($"CustomAssetFx loaded {Prefabs.Count} prefab(s) from {BundleFileName}");
                return Prefabs.Count > 0;
            }
            catch (Exception ex)
            {
                ModLog.Warn($"CustomAssetFx load failed: {ex.Message}");
                return false;
            }
        }

        private static float EstimateLifetime(GameObject go, float fallbackLifetime)
        {
            float maxLifetime = 0f;
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                ps.Play(withChildren: true);
                var main = ps.main;
                float life = main.duration + main.startLifetime.constantMax;
                if (life > maxLifetime) maxLifetime = life;
            }

            return Math.Max(fallbackLifetime, maxLifetime + 0.5f);
        }

        private static bool TryPlayInternal(
            string prefabName,
            Vector3 worldPos,
            float fallbackLifetime,
            Transform? attachParent,
            Card? trackTarget,
            string? attachKey,
            bool replaceExisting,
            bool forceLoop,
            bool autoDestroy,
            Dictionary<string, GameObject>? registry,
            Color? tint,
            float scaleMultiplier = 1f,
            bool destroyOnZoneChange = false
        )
        {
            if (!TryEnsureLoaded()) return false;
            if (!Prefabs.TryGetValue(prefabName, out var prefab) || prefab == null)
            {
                ModLog.Warn($"CustomAssetFx prefab not found: {prefabName}");
                return false;
            }

            if (replaceExisting && attachKey != null && registry != null)
            {
                DestroyRegisteredFx(registry, attachKey);
            }

            var go = UnityEngine.Object.Instantiate(prefab, worldPos, Quaternion.identity);
            if (go == null) return false;

            if (!Mathf.Approximately(scaleMultiplier, 1f))
            {
                go.transform.localScale *= scaleMultiplier;
            }

            if (tint.HasValue)
            {
                bool replaceColor = string.Equals(prefabName, "FxGraveQuagmire", StringComparison.Ordinal);
                ApplyTint(go, tint.Value, replaceColor);
            }

            if (attachParent != null)
            {
                // Keep current world position, then follow the card transform.
                go.transform.SetParent(attachParent, true);
            }
            else if (trackTarget != null)
            {
                // Fallback: track card position via MonoBehaviour when actor is unavailable.
                go.AddComponent<CardPositionTracker>().SetTarget(trackTarget);
            }

            if (destroyOnZoneChange)
            {
                go.AddComponent<ZoneChangeDestroyer>().CaptureCurrentMap();
            }

            if (string.Equals(prefabName, "FxHecatiaPartyStage", StringComparison.Ordinal))
            {
                NormalizeVelocityCurveModes(go);
                ForceHierarchyScalingMode(go);
                ApplyParticleBudgetScale(go, 0.5f);
                go.AddComponent<AudioReactiveScale>().Initialize(
                    minScale: 0.70f, // temporary debug range: intentionally exaggerated
                    maxScale: 1.80f, // to verify visible reaction in-game
                    noiseFloor: 0.0015f,
                    gain: 80f,
                    smoothing: 14f,
                    lowSignalPulseBlend: 0.60f);
            }
            else if (IsServantAuraPrefab(prefabName))
            {
                // Make servant smoke aura drift much more slowly without rebaking Unity assets.
                // 0.25x => approximately 4x slower spread / longer visible playback.
                ApplySimulationSpeedScale(go, 0.25f);
            }

            if (forceLoop)
            {
                ForceLoop(go);
            }
            else
            {
                PlayAllParticleSystems(go);
            }

            if (autoDestroy)
            {
                float ttl = EstimateLifetime(go, fallbackLifetime);
                UnityEngine.Object.Destroy(go, ttl);
            }

            if (attachKey != null && registry != null)
            {
                registry[attachKey] = go;
            }

            return true;
        }

        private static void NormalizeVelocityCurveModes(GameObject go)
        {
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (ps == null) continue;

                var vol = ps.velocityOverLifetime;
                if (!vol.enabled) continue;

                if (!AreCurveModesEqual(vol.x.mode, vol.y.mode, vol.z.mode))
                {
                    vol.x = ToTwoConstants(vol.x);
                    vol.y = ToTwoConstants(vol.y);
                    vol.z = ToTwoConstants(vol.z);
                }

                if (!AreCurveModesEqual(vol.orbitalX.mode, vol.orbitalY.mode, vol.orbitalZ.mode, vol.radial.mode))
                {
                    vol.orbitalX = ToTwoConstants(vol.orbitalX);
                    vol.orbitalY = ToTwoConstants(vol.orbitalY);
                    vol.orbitalZ = ToTwoConstants(vol.orbitalZ);
                    vol.radial = ToTwoConstants(vol.radial);
                }
            }
        }

        private static bool AreCurveModesEqual(params ParticleSystemCurveMode[] modes)
        {
            if (modes == null || modes.Length <= 1) return true;
            var first = modes[0];
            for (int i = 1; i < modes.Length; i++)
            {
                if (modes[i] != first) return false;
            }
            return true;
        }

        private static ParticleSystem.MinMaxCurve ToTwoConstants(ParticleSystem.MinMaxCurve source)
        {
            switch (source.mode)
            {
                case ParticleSystemCurveMode.TwoConstants:
                    return source;
                case ParticleSystemCurveMode.Constant:
                    return new ParticleSystem.MinMaxCurve(source.constant, source.constant);
                default:
                    // Keep visual behavior approximately intact when normalizing curve mode.
                    float v1 = SafeEvaluate(source, 0.2f, 0.0f);
                    float v2 = SafeEvaluate(source, 0.8f, 1.0f);
                    float min = Mathf.Min(v1, v2);
                    float max = Mathf.Max(v1, v2);
                    return new ParticleSystem.MinMaxCurve(min, max);
            }
        }

        private static float SafeEvaluate(ParticleSystem.MinMaxCurve source, float time, float lerpFactor)
        {
            try
            {
                return source.Evaluate(time, lerpFactor);
            }
            catch
            {
                return source.constant;
            }
        }

        private static void ApplyTint(GameObject go, Color tint, bool replaceColor)
        {
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (ps == null) continue;

                var main = ps.main;
                main.startColor = replaceColor ? ReplaceColor(main.startColor, tint) : Tint(main.startColor, tint);

                var col = ps.colorOverLifetime;
                if (col.enabled)
                {
                    col.color = replaceColor ? ReplaceColor(col.color, tint) : Tint(col.color, tint);
                }

                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    ApplyMaterialTint(renderer, tint);
                }
            }
        }

        private static void ApplyMaterialTint(ParticleSystemRenderer renderer, Color tint)
        {
            var shared = renderer.sharedMaterial;
            if (shared == null) return;

            bool hasTint = shared.HasProperty("_TintColor");
            bool hasColor = shared.HasProperty("_Color");
            bool hasBaseColor = shared.HasProperty("_BaseColor");
            if (!hasTint && !hasColor && !hasBaseColor) return;

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            if (hasTint) block.SetColor("_TintColor", tint);
            if (hasColor) block.SetColor("_Color", tint);
            if (hasBaseColor) block.SetColor("_BaseColor", tint);
            renderer.SetPropertyBlock(block);
        }

        private static ParticleSystem.MinMaxGradient Tint(ParticleSystem.MinMaxGradient source, Color tint)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(Mul(source.color, tint));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(
                        Mul(source.colorMin, tint),
                        Mul(source.colorMax, tint)
                    );
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(TintGradient(source.gradient, tint));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(
                        TintGradient(source.gradientMin, tint),
                        TintGradient(source.gradientMax, tint)
                    );
                case ParticleSystemGradientMode.RandomColor:
                    return new ParticleSystem.MinMaxGradient(TintGradient(source.gradient, tint));
                default:
                    return source;
            }
        }

        private static ParticleSystem.MinMaxGradient ReplaceColor(ParticleSystem.MinMaxGradient source, Color tint)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(KeepAlpha(source.color, tint));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(
                        KeepAlpha(source.colorMin, tint),
                        KeepAlpha(source.colorMax, tint)
                    );
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(ReplaceGradientColor(source.gradient, tint));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(
                        ReplaceGradientColor(source.gradientMin, tint),
                        ReplaceGradientColor(source.gradientMax, tint)
                    );
                case ParticleSystemGradientMode.RandomColor:
                    return new ParticleSystem.MinMaxGradient(ReplaceGradientColor(source.gradient, tint));
                default:
                    return source;
            }
        }

        private static Gradient TintGradient(Gradient source, Color tint)
        {
            var src = source ?? new Gradient();
            var colorKeys = src.colorKeys;
            var alphaKeys = src.alphaKeys;

            var tintedColorKeys = new GradientColorKey[colorKeys.Length];
            for (int i = 0; i < colorKeys.Length; i++)
            {
                tintedColorKeys[i] = new GradientColorKey(Mul(colorKeys[i].color, tint), colorKeys[i].time);
            }

            var tintedAlphaKeys = new GradientAlphaKey[alphaKeys.Length];
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                tintedAlphaKeys[i] = new GradientAlphaKey(alphaKeys[i].alpha * tint.a, alphaKeys[i].time);
            }

            var g = new Gradient();
            g.SetKeys(tintedColorKeys, tintedAlphaKeys);
            return g;
        }

        private static Gradient ReplaceGradientColor(Gradient source, Color tint)
        {
            var src = source ?? new Gradient();
            var colorKeys = src.colorKeys;
            var alphaKeys = src.alphaKeys;

            var replacedColorKeys = new GradientColorKey[colorKeys.Length];
            for (int i = 0; i < colorKeys.Length; i++)
            {
                replacedColorKeys[i] = new GradientColorKey(new Color(tint.r, tint.g, tint.b, 1f), colorKeys[i].time);
            }

            var replacedAlphaKeys = new GradientAlphaKey[alphaKeys.Length];
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                replacedAlphaKeys[i] = new GradientAlphaKey(alphaKeys[i].alpha * tint.a, alphaKeys[i].time);
            }

            var g = new Gradient();
            g.SetKeys(replacedColorKeys, replacedAlphaKeys);
            return g;
        }

        private static Color Mul(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        }

        private static Color KeepAlpha(Color source, Color tint)
        {
            return new Color(tint.r, tint.g, tint.b, source.a * tint.a);
        }

        private static void PlayAllParticleSystems(GameObject go)
        {
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                ps.Play(withChildren: true);
            }
        }

        private static void ForceLoop(GameObject go)
        {
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                var main = ps.main;
                main.loop = true;
                ps.Play(withChildren: true);
            }
        }

        private static void ForceHierarchyScalingMode(GameObject go)
        {
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                var main = ps.main;
                // Ensure parent transform scale affects particle visuals.
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }

        private static bool IsServantAuraPrefab(string prefabName)
        {
            return
                string.Equals(prefabName, "FxServantShadowTentacleAura_SmokeDrip", StringComparison.Ordinal) ||
                string.Equals(prefabName, "FxServantShadowTentacleAura_SmokeDrip_v2", StringComparison.Ordinal) ||
                string.Equals(prefabName, "FxServantShadowTentacleAura", StringComparison.Ordinal);
        }

        private static void ApplySimulationSpeedScale(GameObject go, float scale01)
        {
            float scale = Mathf.Clamp(scale01, 0.05f, 4f);
            if (Mathf.Approximately(scale, 1f)) return;

            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                var main = ps.main;
                main.simulationSpeed *= scale;
            }
        }

        private static void ApplyParticleBudgetScale(GameObject go, float scale01)
        {
            float scale = Mathf.Clamp(scale01, 0.05f, 1f);
            if (scale >= 0.999f) return;

            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (ps == null) continue;

                var main = ps.main;
                main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(main.maxParticles * scale));

                var emission = ps.emission;
                emission.rateOverTimeMultiplier *= scale;
                emission.rateOverDistanceMultiplier *= scale;
            }
        }

        private static string BuildOwnedLoopKey(string prefabName, int targetUid, string ownerKey, int casterUid)
        {
            return $"loop:{prefabName}:{targetUid}:{ownerKey}:{casterUid}";
        }

        private static void SweepExpiredLoopFxOncePerTurn()
        {
            int currentTurn = GetCurrentGameTurn();
            if (currentTurn == _lastLoopSweepTurn) return;
            _lastLoopSweepTurn = currentTurn;
            if (LoopLeaseUntilTurn.Count == 0) return;

            LoopKeyScratch.Clear();
            foreach (var kv in LoopLeaseUntilTurn)
            {
                bool expired = currentTurn >= kv.Value;
                bool missingGo = !LoopAttachedFx.TryGetValue(kv.Key, out var go) || go == null;
                if (expired || missingGo)
                    LoopKeyScratch.Add(kv.Key);
            }

            foreach (var key in LoopKeyScratch)
            {
                DestroyRegisteredFx(LoopAttachedFx, key);
                LoopLeaseUntilTurn.Remove(key);
            }
            LoopKeyScratch.Clear();
        }

        private static int GetCurrentGameTurn()
        {
            if (EClass.pc != null)
                return EClass.pc.turn;

            // Keep leases stable until player state is available.
            return _lastLoopSweepTurn == int.MinValue ? 0 : _lastLoopSweepTurn;
        }

        private static void DestroyOwnedLoopFxForPrefabAndTarget(string prefabName, int targetUid)
        {
            string prefix = $"loop:{prefabName}:{targetUid}:";
            LoopKeyScratch.Clear();
            foreach (var kv in LoopAttachedFx)
            {
                if (kv.Key.StartsWith(prefix, StringComparison.Ordinal))
                    LoopKeyScratch.Add(kv.Key);
            }

            foreach (var key in LoopKeyScratch)
            {
                DestroyRegisteredFx(LoopAttachedFx, key);
                LoopLeaseUntilTurn.Remove(key);
            }
            LoopKeyScratch.Clear();
        }

        private static void DestroyAllLoopFxForTarget(int targetUid)
        {
            LoopKeyScratch.Clear();
            foreach (var kv in LoopAttachedFx)
            {
                if (IsLoopKeyForTargetUid(kv.Key, targetUid))
                    LoopKeyScratch.Add(kv.Key);
            }

            foreach (var key in LoopKeyScratch)
            {
                DestroyRegisteredFx(LoopAttachedFx, key);
                LoopLeaseUntilTurn.Remove(key);
            }
            LoopKeyScratch.Clear();
        }

        private static bool IsLoopKeyForTargetUid(string key, int targetUid)
        {
            if (!key.StartsWith("loop:", StringComparison.Ordinal))
                return false;

            int first = key.IndexOf(':');
            if (first < 0 || first + 1 >= key.Length) return false;
            int second = key.IndexOf(':', first + 1); // after prefab
            if (second < 0 || second + 1 >= key.Length) return false;
            int third = key.IndexOf(':', second + 1); // end of target uid

            string uidText = third < 0
                ? key.Substring(second + 1)
                : key.Substring(second + 1, third - second - 1);
            return int.TryParse(uidText, out int parsedUid) && parsedUid == targetUid;
        }

        private static void RemoveLeasesBySuffix(string suffix)
        {
            if (LoopLeaseUntilTurn.Count == 0) return;
            LoopKeyScratch.Clear();
            foreach (var kv in LoopLeaseUntilTurn)
            {
                if (kv.Key.EndsWith(suffix, StringComparison.Ordinal))
                    LoopKeyScratch.Add(kv.Key);
            }

            foreach (var key in LoopKeyScratch)
                LoopLeaseUntilTurn.Remove(key);
            LoopKeyScratch.Clear();
        }

        private static void DestroyAllRegisteredFx(Dictionary<string, GameObject> registry)
        {
            if (registry.Count == 0) return;
            LoopKeyScratch.Clear();
            foreach (var key in registry.Keys)
                LoopKeyScratch.Add(key);

            foreach (var key in LoopKeyScratch)
            {
                DestroyRegisteredFx(registry, key);
                LoopLeaseUntilTurn.Remove(key);
            }
            LoopKeyScratch.Clear();
        }

        private static void DestroySceneFxByPrefabNameSet(IEnumerable<string> prefabIds)
        {
            var ids = new HashSet<string>(prefabIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            if (ids.Count == 0) return;

            var visitedRootIds = new HashSet<int>();
            var systems = UnityEngine.Object.FindObjectsOfType<ParticleSystem>();
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                var root = ps.transform?.root;
                if (root == null) continue;
                var rootGo = root.gameObject;
                if (rootGo == null) continue;

                string rootName = rootGo.name;
                if (!TryMatchPrefabName(rootName, ids)) continue;

                int instanceId = rootGo.GetInstanceID();
                if (!visitedRootIds.Add(instanceId)) continue;
                UnityEngine.Object.Destroy(rootGo);
            }
        }

        private static bool TryMatchPrefabName(string objectName, HashSet<string> prefabIds)
        {
            if (string.IsNullOrEmpty(objectName)) return false;
            foreach (var prefabId in prefabIds)
            {
                if (objectName.Equals(prefabId, StringComparison.Ordinal))
                    return true;
                if (objectName.Equals(prefabId + "(Clone)", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static void DestroyRegisteredFx(Dictionary<string, GameObject> registry, string key)
        {
            if (registry.TryGetValue(key, out var existing) && existing != null)
            {
                UnityEngine.Object.Destroy(existing);
            }
            registry.Remove(key);
            LoopLeaseUntilTurn.Remove(key);
        }

        private static void DestroyAllWithSuffix(Dictionary<string, GameObject> registry, string suffix)
        {
            var keys = new List<string>();
            foreach (var kv in registry)
            {
                if (kv.Key.EndsWith(suffix, StringComparison.Ordinal))
                    keys.Add(kv.Key);
            }

            foreach (var key in keys)
            {
                DestroyRegisteredFx(registry, key);
            }
        }
    }
}
