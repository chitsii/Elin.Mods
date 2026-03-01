using System.Linq;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Flags;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// カインをアリーナにスポーンさせ、バルガスも復活させるコマンド
    /// 使用: modInvoke spawn_cain()
    ///
    /// 復活イベントでカインとバルガスが同時に復活するシーン用
    /// - カイン: 新規作成してスポーン
    /// - バルガス: 復活フラグを設定（Zone更新時に表示される）
    /// </summary>
    public class SpawnCainCommand : IArenaCommand
    {
        public string Name => "spawn_cain";

        private const string CAIN_ID = "sukutsu_cain";
        private const string ARENA_ZONE_ID = "sukutsu_arena";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            // バルガス復活フラグを設定（カインと同時に復活）
            ctx.Storage.SetInt(ArenaFlagKeys.BalgasRevived, 1);
            ModLog.Log($"[SpawnCain] Set BalgasRevived flag");

            // Check if Cain already exists via multiple methods
            if (IsCainAlreadySpawned())
            {
                ModLog.Log($"[SpawnCain] Cain already exists in the world, skipping spawn");
                return;
            }

            // Get the arena zone
            var arenaZone = EClass.game.spatials.Find(ARENA_ZONE_ID);
            if (arenaZone == null)
            {
                Debug.LogError($"[SpawnCain] Could not find zone: {ARENA_ZONE_ID}");
                return;
            }

            // Create Cain character
            var cain = CharaGen.Create(CAIN_ID);
            if (cain == null)
            {
                Debug.LogError($"[SpawnCain] Failed to create character: {CAIN_ID}");
                return;
            }

            // Spawn at the current zone if it's the arena, otherwise move to arena
            if (EClass.game.activeZone?.id == ARENA_ZONE_ID)
            {
                // Find a spawn point near the player or at a default location
                var pos = EClass.pc?.pos ?? new Point(10, 10);
                EClass._zone.AddCard(cain, pos);
                ModLog.Log($"[SpawnCain] Spawned Cain in current zone at {pos}");
            }
            else
            {
                // Add to arena zone
                cain.MoveZone(arenaZone);
                ModLog.Log($"[SpawnCain] Spawned Cain in arena zone");
            }
        }

        /// <summary>
        /// カインが既にスポーンしているか複数の方法で確認
        /// </summary>
        private bool IsCainAlreadySpawned()
        {
            // Method 1: Check globalCharas (by ID)
            var globalCain = EClass.game.cards.globalCharas.Find(CAIN_ID);
            if (globalCain != null)
            {
                ModLog.Log($"[SpawnCain] Found Cain in globalCharas");
                return true;
            }

            // Method 2: Check globalCharas (iterate all values for safety)
            foreach (var c in EClass.game.cards.globalCharas.Values)
            {
                if (c?.id == CAIN_ID)
                {
                    ModLog.Log($"[SpawnCain] Found Cain in globalCharas.Values");
                    return true;
                }
            }

            // Method 3: Check current zone via FindChara
            var zoneCain = EClass._zone?.FindChara(CAIN_ID);
            if (zoneCain != null)
            {
                ModLog.Log($"[SpawnCain] Found Cain in current zone via FindChara");
                return true;
            }

            // Method 4: Check current map's charas
            var mapCain = EClass._map?.charas?.FirstOrDefault(c => c?.id == CAIN_ID);
            if (mapCain != null)
            {
                ModLog.Log($"[SpawnCain] Found Cain in current map's charas");
                return true;
            }

            return false;
        }
    }
}

