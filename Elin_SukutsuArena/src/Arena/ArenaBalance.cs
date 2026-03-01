using UnityEngine;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;

namespace Elin_SukutsuArena.Arena
{
    /// <summary>
    /// Arena balance utilities (mod settings driven)
    /// </summary>
    public static class ArenaBalance
    {
        private const int DmgDealtElementId = 94; // SKILL.dmgDealt
        private const int DefaultBossDamageRate = 100;
        private const int MinBossDamageRate = 1;
        private const int MaxBossDamageRate = 200;

        public static void ApplyBossDamageRate(Chara boss)
        {
            if (boss == null) return;

            int rate = ArenaContext.I.Storage.GetInt(ArenaFlagKeys.BossDamageRate, DefaultBossDamageRate);
            rate = Mathf.Clamp(rate, MinBossDamageRate, MaxBossDamageRate);
            int delta = rate - DefaultBossDamageRate;

            if (delta != 0)
            {
                boss.elements.ModBase(DmgDealtElementId, delta);
            }

            ModLog.Log($"[SukutsuArena] Boss damage rate: {rate}% (dmgDealt {delta:+#;-#;0}) for {boss.Name}");
        }
    }
}

