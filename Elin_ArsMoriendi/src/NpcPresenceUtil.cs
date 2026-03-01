namespace Elin_ArsMoriendi
{
    /// <summary>
    /// Common helper for global NPC lifecycle:
    /// ensure existence in "somewhere", show near player, hide to "somewhere".
    /// </summary>
    public static class NpcPresenceUtil
    {
        private const string SomewhereZoneId = "somewhere";

        public static Chara? EnsureInSomewhere(string npcId)
        {
            var npc = FindGlobalNpc(npcId);
            if (npc == null)
            {
                npc = CharaGen.Create(npcId);
                if (npc == null)
                {
                    ModLog.Warn($"NpcPresenceUtil: failed to create {npcId}");
                    return null;
                }
            }

            npc.SetGlobal();
            npc.MoveZone(SomewhereZoneId);
            return npc;
        }

        public static Chara? ShowNearPlayer(string npcId)
        {
            var pc = EClass.pc;
            if (pc == null || EClass._zone == null) return null;

            var npc = EnsureInSomewhere(npcId);
            if (npc == null) return null;

            if (npc.currentZone != EClass._zone)
            {
                if (npc.currentZone != null)
                    npc.currentZone.RemoveCard(npc);
                EClass._zone.AddCard(npc, pc.pos.GetNearestPointCompat(allowBlock: false, allowChara: false));
            }

            return npc;
        }

        public static void HideToSomewhere(string npcId)
        {
            EnsureInSomewhere(npcId);
        }

        public static Chara? FindGlobalNpc(string npcId)
        {
            var globalCharas = EClass.game?.cards?.globalCharas;
            if (globalCharas == null) return null;

            foreach (var c in globalCharas.Values)
            {
                if (c != null && !c.isDestroyed && c.id == npcId)
                    return c;
            }
            return null;
        }
    }
}
