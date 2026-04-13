using System.Collections.Generic;
using Elin_Elinikki.Quest.Quest;
using UnityEngine;

namespace Elin_Elinikki.Quest.Placement
{
    /// <summary>
    /// Static placement data for the per-layer 3D trace objects that
    /// <see cref="ElinikkiZonePlacementManager"/> hands to
    /// <see cref="SharedWorldObjectManager"/>. Every definition id starts
    /// with <see cref="ElinikkiZonePlacementManager.PlacementPrefix"/>
    /// ("elinikki/") so the Task 3.3 Remove/Upsert pipeline can cleanly
    /// wipe the prior set with <c>RemoveDefinitionsByPrefix</c> on a
    /// zone change.
    ///
    /// <para>Coordinates here are a provisional layout picked to fit
    /// inside a ~40-tile map centered around (20.5, y, 20.5). Phase 5
    /// creates the real devmode maps; Task 3.6 will tune these values
    /// against that geometry (wall placements in particular need the
    /// actual wall directions and altitudes, not the flat-cave
    /// assumptions below). Every X/Z value ends in <c>.5</c> because
    /// <see cref="FpsIdealizedWorld.GetSurfaceHeightAt"/> subtracts
    /// <c>0.5</c> before sampling: the tile-space convention is
    /// <c>cellIndex + 0.5</c> for cell centers, and raw integer values
    /// land on cell corners and interpolate surface height from the
    /// wrong four cells. The values are deliberately kept in code
    /// rather than an XML/JSON blob so every change flows through the
    /// normal build verification and codex review loop.</para>
    ///
    /// <para>Every definition sets <c>Visibility = Both</c> per the
    /// AGENTS.md rule that a single definition drives both the normal
    /// top-down view (via the shared-world normal proxy) and the GPU
    /// FPS preview. Restricting Elinikki placements to <c>FpsView</c>
    /// would hide every quest landmark whenever the player is not
    /// holding the fullscreen overlay down, which breaks the default
    /// top-down play experience. Colors are placeholder solids; Task
    /// 3.5 replaces them with photorealistic 512x512 textures via
    /// <c>TextureOverride</c> and dedicated custom geometry for the
    /// stone cluster and campfire.</para>
    /// </summary>
    internal static class ElinikkiPlacementData
    {
        private const SharedWorldVisibility Visibility = SharedWorldVisibility.Both;

        // Color palette. One per trace category — intentionally low
        // saturation so the live maps still read as "cave" until the
        // Phase 7 textures land.
        private static readonly Color MarkColor = new Color(0.78f, 0.82f, 0.92f, 1f);
        private static readonly Color ChannelColor = new Color(0.36f, 0.54f, 0.72f, 1f);
        private static readonly Color StoneColor = new Color(0.58f, 0.56f, 0.52f, 1f);
        private static readonly Color JournalColor = new Color(0.72f, 0.60f, 0.40f, 1f);
        private static readonly Color EchoPointColor = new Color(0.50f, 0.48f, 0.60f, 1f);
        private static readonly Color MapColor = new Color(0.64f, 0.58f, 0.44f, 1f);
        private static readonly Color ShadowColor = new Color(0.12f, 0.10f, 0.14f, 1f);
        private static readonly Color FlowerColor = new Color(0.92f, 0.66f, 0.48f, 1f);
        private static readonly Color WeaveColor = new Color(0.70f, 0.60f, 0.38f, 1f);
        private static readonly Color CampfireColor = new Color(0.92f, 0.56f, 0.22f, 1f);

        /// <summary>
        /// Returns the trace/marker definitions that should be active
        /// while the player is in <paramref name="zoneId"/>. Returns an
        /// empty sequence for any zone the quest does not own (including
        /// the shared Nefia entrance map — chapters 0 and 5 use the
        /// same map but have no 3D trace placements).
        /// </summary>
        public static IEnumerable<SharedWorldPrimitiveDefinition> GetDefinitionsForZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                yield break;
            }

            switch (zoneId)
            {
                case ElinikkiZoneIds.LayerWaterstone:
                    foreach (var def in BuildWaterstone()) yield return def;
                    yield break;
                case ElinikkiZoneIds.LayerEcho:
                    foreach (var def in BuildEcho()) yield return def;
                    yield break;
                case ElinikkiZoneIds.LayerBloom:
                    foreach (var def in BuildBloom()) yield return def;
                    yield break;
                case ElinikkiZoneIds.YuuCamp:
                    foreach (var def in BuildYuuCamp()) yield return def;
                    yield break;
                // NefiaEntrance and anything else: no placements.
            }
        }

        // ---------------------------------------------------------------
        // Chapter 1: 水石の層 — 3 traces + Yuu's journal.
        // ---------------------------------------------------------------
        private static IEnumerable<SharedWorldPrimitiveDefinition> BuildWaterstone()
        {
            // Wall markings — a vertical quad mounted at a nominal wall
            // position on the north edge. Task 3.6 will align this to
            // the actual wall once the devmode map exists.
            //
            // The north wall's inner surface faces south, so the quad
            // normal must point -Z. An unrotated Unity Quad already
            // points -Z, so yaw = 0° is the correct value.
            yield return MakeWallQuad(
                id: "elinikki/waterstone/trace_marks",
                tilePosition: new Vector3(20.5f, 1.3f, 28.5f),
                scale: new Vector3(1.8f, 1.4f, 0.08f),
                yawDegrees: 0f,
                color: MarkColor);

            // Water channel — a thin flat quad on the floor running
            // roughly north-south. Width 0.5 tiles, length 3.0 tiles.
            yield return MakeFloorQuad(
                id: "elinikki/waterstone/trace_channel",
                tilePosition: new Vector3(20.5f, 0.02f, 24.5f),
                footprintX: 0.5f,
                footprintZ: 3.0f,
                color: ChannelColor);

            // Sorted stones — a low cube standing in for the pile. Task
            // 3.5 will replace the cube with a custom cluster geometry.
            // TilePosition.y must equal Scale.y / 2 because Unity's
            // primitive cube is centered on its pivot: a half-height
            // lift is the only way the bottom of the cube rests on the
            // sampled surface instead of sinking into it.
            yield return MakeGroundCube(
                id: "elinikki/waterstone/trace_stones",
                tilePosition: new Vector3(16.5f, 0.175f, 20.5f),
                scale: new Vector3(0.9f, 0.35f, 0.9f),
                color: StoneColor);

            // Yuu's notebook — very small cube on the floor. No flag;
            // pure story beat.
            yield return MakeGroundCube(
                id: "elinikki/waterstone/trace_journal",
                tilePosition: new Vector3(24.5f, 0.06f, 16.5f),
                scale: new Vector3(0.35f, 0.12f, 0.50f),
                color: JournalColor);
        }

        // ---------------------------------------------------------------
        // Chapter 2: 反響の層 — 3 echo points + floor map + wall shadow.
        // ---------------------------------------------------------------
        private static IEnumerable<SharedWorldPrimitiveDefinition> BuildEcho()
        {
            // Echo experiment points. Spaced out so the player physically
            // walks between them (matches story outline "場所を変える").
            yield return MakeFloorQuad(
                id: "elinikki/echo/point_a",
                tilePosition: new Vector3(12.5f, 0.03f, 24.5f),
                footprintX: 1.4f,
                footprintZ: 1.4f,
                color: EchoPointColor);

            yield return MakeFloorQuad(
                id: "elinikki/echo/point_b",
                tilePosition: new Vector3(20.5f, 0.03f, 24.5f),
                footprintX: 1.4f,
                footprintZ: 1.4f,
                color: EchoPointColor);

            yield return MakeFloorQuad(
                id: "elinikki/echo/point_c",
                tilePosition: new Vector3(28.5f, 0.03f, 24.5f),
                footprintX: 1.4f,
                footprintZ: 1.4f,
                color: EchoPointColor);

            // Etched floor map — wider flat quad south of the echo line.
            yield return MakeFloorQuad(
                id: "elinikki/echo/trace_map",
                tilePosition: new Vector3(20.5f, 0.03f, 16.5f),
                footprintX: 2.2f,
                footprintZ: 2.2f,
                color: MapColor);

            // Wall shadow — vertical quad on a west-facing wall.
            // The wall sits at the east edge (x=30) so its inner
            // surface faces west (normal -X). Rotating Unity's default
            // -Z normal to -X is a yaw of 90° around world +Y.
            yield return MakeWallQuad(
                id: "elinikki/echo/trace_shadow",
                tilePosition: new Vector3(30.5f, 1.2f, 20.5f),
                scale: new Vector3(0.8f, 1.6f, 0.08f),
                yawDegrees: 90f,
                color: ShadowColor);
        }

        // ---------------------------------------------------------------
        // Chapter 3: 花の層 — flower center + woven cord.
        // ---------------------------------------------------------------
        private static IEnumerable<SharedWorldPrimitiveDefinition> BuildBloom()
        {
            // Flower root cluster at the heart of the bloom room.
            // TilePosition.y = Scale.y / 2 so the cube bottom rests on
            // the sampled surface.
            yield return MakeGroundCube(
                id: "elinikki/bloom/trace_flowers",
                tilePosition: new Vector3(20.5f, 0.375f, 20.5f),
                scale: new Vector3(1.4f, 0.75f, 1.4f),
                color: FlowerColor);

            // Abandoned woven cord near the east edge.
            yield return MakeGroundCube(
                id: "elinikki/bloom/trace_weave",
                tilePosition: new Vector3(26.5f, 0.05f, 20.5f),
                scale: new Vector3(0.45f, 0.10f, 0.25f),
                color: WeaveColor);
        }

        // ---------------------------------------------------------------
        // Chapter 4: ユウの野営地 — the campfire is the only landmark.
        // ---------------------------------------------------------------
        private static IEnumerable<SharedWorldPrimitiveDefinition> BuildYuuCamp()
        {
            // Campfire. Cylinder stands in for the fire ring. Task 3.5
            // will swap in a custom geometry with a flame texture.
            //
            // Unity's primitive Cylinder is 2 units tall by default, so
            // the world-space height is 2 * Scale.y. For a visible
            // height of 0.35 tiles we need Scale.y = 0.175. The pivot
            // is the cylinder's center, so TilePosition.y also = 0.175
            // to place the bottom on the sampled surface. HeightUnits
            // on the normal proxy stays 0.35 because that is the
            // semantic (billboard) height, not the raw scale.
            yield return new SharedWorldPrimitiveDefinition
            {
                Id = "elinikki/yuu_camp/campfire",
                Primitive = SharedWorldPrimitiveKind.Cylinder,
                Visibility = Visibility,
                TilePosition = new Vector3(20.5f, 0.175f, 20.5f),
                Scale = new Vector3(0.9f, 0.175f, 0.9f),
                EulerAngles = Vector3.zero,
                Color = CampfireColor,
                AttachToSurface = true,
                NormalProxy = new SharedWorldNormalProxyDefinition
                {
                    // Task 3.5: explicit placeholder texture for the
                    // campfire. Phase 7 will swap in a flame sprite.
                    Mode = SharedWorldNormalProxyMode.ExplicitTexture,
                    HeightMode = SharedWorldNormalProxyHeightMode.SemanticOnly,
                    FacingMode = SharedWorldNormalProxyFacingMode.DefinitionEuler,
                    Footprint = new Vector2(0.9f, 0.9f),
                    HeightUnits = 0.35f,
                    Pivot = new Vector2(0.5f, 0f),
                    SortPivotY = 0f,
                    TextureOverride = ElinikkiPlaceholderTextures.GetOrCreateNeutral(),
                },
            };
        }

        // ---------------------------------------------------------------
        // Factory helpers. Keep the per-trace records terse while still
        // exercising the full SharedWorldPrimitiveDefinition surface.
        // ---------------------------------------------------------------

        /// <summary>
        /// Flat floor quad. Used for channels, echo points, and the
        /// etched floor map. Rotated so the front face points +Y
        /// (visible from above).
        ///
        /// <para>Implementation note: Unity's built-in Quad is an XY
        /// plane with its normal along -Z (per the Unity manual) and
        /// no local Z thickness. Rotating by <c>(90°, 0°, 0°)</c> maps
        /// that -Z normal to world +Y, so the front face points up and
        /// the preview material's default back-face culling keeps the
        /// quad visible to a player looking down. <c>(-90°, 0°, 0°)</c>
        /// would point the normal at -Y instead and the marker would
        /// disappear under cull.</para>
        ///
        /// <para>After the rotation the quad's local +Y axis maps to
        /// world +Z, so the tile-space footprint depth must be supplied
        /// through <paramref name="footprintZ"/>, which is forwarded to
        /// <c>Scale.y</c>. <c>Scale.z</c> is ignored by the Quad
        /// primitive (quads have no local thickness) and is left at 1
        /// to stay out of the preview's per-axis scale clamp.</para>
        /// </summary>
        private static SharedWorldPrimitiveDefinition MakeFloorQuad(
            string id,
            Vector3 tilePosition,
            float footprintX,
            float footprintZ,
            Color color)
        {
            return new SharedWorldPrimitiveDefinition
            {
                Id = id,
                Primitive = SharedWorldPrimitiveKind.Quad,
                Visibility = Visibility,
                TilePosition = tilePosition,
                // See the method's implementation note: Scale.y carries
                // the world-Z footprint after the X=90 rotation.
                Scale = new Vector3(footprintX, footprintZ, 1f),
                EulerAngles = new Vector3(90f, 0f, 0f),
                Color = color,
                AttachToSurface = true,
                NormalProxy = new SharedWorldNormalProxyDefinition
                {
                    // Floor quads stay on AutoBake: the normal-view
                    // proxy system renders unrotated sprites, so the
                    // 90° tilt that projects these onto the ground
                    // plane is captured at bake time by sampling the
                    // tilted Unity primitive. Replacing that with an
                    // explicit flat texture would make floor traces
                    // render as screen-aligned rectangles in the
                    // top-down view. Task 7.1's final textures will
                    // need to encode the top-down projection
                    // themselves before this can switch to
                    // ExplicitTexture.
                    Mode = SharedWorldNormalProxyMode.AutoBake,
                    HeightMode = SharedWorldNormalProxyHeightMode.SemanticOnly,
                    FacingMode = SharedWorldNormalProxyFacingMode.DefinitionEuler,
                    Footprint = new Vector2(footprintX, footprintZ),
                    HeightUnits = 0.05f,
                    Pivot = new Vector2(0.5f, 0f),
                    SortPivotY = 0f,
                },
            };
        }

        /// <summary>
        /// Vertical wall quad. Used for wall markings and burn-in
        /// silhouettes. The unrotated Unity Quad's normal points -Z
        /// (per Unity's PrimitiveObjects manual page), and
        /// <paramref name="yawDegrees"/> rotates the quad around world
        /// +Y so the normal ends up facing the intended direction:
        ///   * 0° — normal -Z (north wall, visible from the south side)
        ///   * 90° — normal -X (east wall, visible from the west)
        ///   * 180° — normal +Z (south wall, visible from the north)
        ///   * 270° — normal +X (west wall, visible from the east)
        /// Hardcoding a single yaw here would make wall traces on
        /// anything other than the default-direction wall render
        /// back-face-culled in the GPU preview, so every wall trace
        /// must supply its own yaw.
        ///
        /// <para>The normal-view proxy samples footprint as
        /// <c>Vector2(eastWest, northSouth)</c>; for walls whose yaw
        /// swaps the visible width onto the Z axis (90°/270°) the
        /// helper rotates the footprint accordingly so the top-down
        /// proxy stays the right shape. Height becomes the footprint's
        /// "depth" axis because the wall has no world-Y extent in
        /// top-down projection.</para>
        ///
        /// <para>Stays surface-attached so the quad's y offset is
        /// applied relative to the underlying cell's surface height
        /// rather than an absolute world Y. Without this, a wall trace
        /// that lands on a raised floor, bridge, or any non-flat cell
        /// in the future devmode maps would float or clip while the
        /// rest of the trace set stayed grounded.</para>
        /// </summary>
        private static SharedWorldPrimitiveDefinition MakeWallQuad(
            string id,
            Vector3 tilePosition,
            Vector3 scale,
            float yawDegrees,
            Color color)
        {
            // Classify yaw modulo 360 into "north/south" (wall width
            // along world X) vs "east/west" (wall width along world Z)
            // so the top-down proxy footprint tracks the rotation
            // instead of always reading width off scale.x.
            float normalizedYaw = yawDegrees - 360f * Mathf.Floor(yawDegrees / 360f);
            bool runsAlongZ = Mathf.Approximately(normalizedYaw, 90f)
                              || Mathf.Approximately(normalizedYaw, 270f);
            Vector2 footprint = runsAlongZ
                ? new Vector2(0.1f, scale.x)
                : new Vector2(scale.x, 0.1f);

            return new SharedWorldPrimitiveDefinition
            {
                Id = id,
                Primitive = SharedWorldPrimitiveKind.Quad,
                Visibility = Visibility,
                TilePosition = tilePosition,
                Scale = scale,
                EulerAngles = new Vector3(0f, yawDegrees, 0f),
                Color = color,
                AttachToSurface = true,
                NormalProxy = new SharedWorldNormalProxyDefinition
                {
                    // Wall quads stay on AutoBake for the same
                    // reason floor quads do: the auto-bake step
                    // captures the yaw orientation into the
                    // top-down sprite, and the normal-view renderer
                    // never re-rotates the bound texture. Using
                    // ExplicitTexture here would render trace_marks
                    // and trace_shadow as identical upright
                    // rectangles regardless of their wall yaw.
                    Mode = SharedWorldNormalProxyMode.AutoBake,
                    HeightMode = SharedWorldNormalProxyHeightMode.MaxSemanticAndTextureAspect,
                    FacingMode = SharedWorldNormalProxyFacingMode.QuantizedElin,
                    Footprint = footprint,
                    HeightUnits = scale.y,
                    Pivot = new Vector2(0.5f, 0f),
                    SortPivotY = 0f,
                },
            };
        }

        /// <summary>
        /// Small standalone ground-hugging cube. Used for stone piles,
        /// the journal, flower root cluster, and woven cord — all
        /// placeholders for custom geometry added in Task 3.5/Phase 7.
        /// </summary>
        private static SharedWorldPrimitiveDefinition MakeGroundCube(
            string id,
            Vector3 tilePosition,
            Vector3 scale,
            Color color)
        {
            return new SharedWorldPrimitiveDefinition
            {
                Id = id,
                Primitive = SharedWorldPrimitiveKind.Cube,
                Visibility = Visibility,
                TilePosition = tilePosition,
                Scale = scale,
                EulerAngles = Vector3.zero,
                Color = color,
                AttachToSurface = true,
                NormalProxy = new SharedWorldNormalProxyDefinition
                {
                    // Task 3.5: explicit placeholder texture, see
                    // MakeFloorQuad for the rationale.
                    Mode = SharedWorldNormalProxyMode.ExplicitTexture,
                    // SemanticOnly while the placeholder is a square
                    // (see MakeWallQuad). Short ground cubes like
                    // trace_journal would otherwise blow up to the
                    // full footprint's square height in top-down view.
                    HeightMode = SharedWorldNormalProxyHeightMode.SemanticOnly,
                    FacingMode = SharedWorldNormalProxyFacingMode.QuantizedElin,
                    Footprint = new Vector2(scale.x, scale.z),
                    HeightUnits = scale.y,
                    Pivot = new Vector2(0.5f, 0f),
                    SortPivotY = 0f,
                    TextureOverride = ElinikkiPlaceholderTextures.GetOrCreateNeutral(),
                },
            };
        }
    }
}
