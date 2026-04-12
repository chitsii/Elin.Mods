namespace Elin_Elinikki.Quest.Quest
{
    /// <summary>
    /// Canonical zone content-id constants for the "帰らなかった遠足" quest.
    /// These must match the <c>id</c> field of the custom zones created in
    /// Elin devmode (see <c>story/worldbuilding/locations/*.md</c>).
    /// Phase 5 of the implementation plan is responsible for creating the
    /// actual devmode data and binding these ids to it.
    ///
    /// Centralising the ids here lets <see cref="ElinikkiQuestFlow"/> register
    /// zone-stage rules immediately, so the moment the devmode zones exist the
    /// flow becomes active without another code change.
    /// </summary>
    public static class ElinikkiZoneIds
    {
        /// <summary>Shared entrance map. Visited in chapter 0 and chapter 5.</summary>
        public const string NefiaEntrance = "elinikki_nefia_entrance";

        /// <summary>Chapter 1: water and moss cave with the etched wall, water channel, stones.</summary>
        public const string LayerWaterstone = "elinikki_layer_waterstone";

        /// <summary>Chapter 2: giant echoing cavern with the 4-stage echo experiment, map, shadow.</summary>
        public const string LayerEcho = "elinikki_layer_echo";

        /// <summary>Chapter 3: glowing flower field with the weave and bloom traces.</summary>
        public const string LayerBloom = "elinikki_layer_bloom";

        /// <summary>Chapter 4: Yuu's campsite at the deepest point. Reunion drama location.</summary>
        public const string YuuCamp = "elinikki_yuu_camp";
    }
}
