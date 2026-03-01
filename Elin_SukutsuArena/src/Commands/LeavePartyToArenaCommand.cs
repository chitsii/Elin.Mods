using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// パーティからメンバーを外し、闘技場に戻すコマンド
    /// 使用: modInvoke leave_party_to_arena()
    /// </summary>
    public class LeavePartyToArenaCommand : IArenaCommand
    {
        public string Name => "leave_party_to_arena";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            // tg (会話対象) からCharaを取得
            var tg = drama.tg?.chara;
            if (tg == null)
            {
                Debug.LogError("[LeavePartyToArena] No target character (tg) found or has no Chara reference");
                return;
            }

            ModLog.Log($"[LeavePartyToArena] Removing {tg.Name} from party and sending to arena");

            // パーティから外す
            EClass.pc.party.RemoveMember(tg);

            // 現在地が闘技場でなければ移動させる
            if (EClass.game.activeZone.id != "sukutsu_arena")
            {
                var arenaZone = EClass.game.spatials.Find("sukutsu_arena");
                if (arenaZone != null)
                {
                    tg.MoveZone(arenaZone);
                    ModLog.Log($"[LeavePartyToArena] {tg.Name} moved to sukutsu_arena");
                }
                else
                {
                    Debug.LogError("[LeavePartyToArena] Could not find sukutsu_arena zone");
                }
            }
            else
            {
                ModLog.Log($"[LeavePartyToArena] {tg.Name} stays in current zone (already in arena)");
            }
        }
    }
}

