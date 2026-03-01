using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// 鶏をパーティに追加するデバッグコマンド
    /// 使用: modInvoke add_chickens_to_party(count)
    /// </summary>
    public class AddChickensToPartyCommand : IArenaCommand
    {
        public string Name => "add_chickens_to_party";

        private const string CHICKEN_ID = "chicken";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            int count = 2; // デフォルト2匹
            if (args.Length > 0 && int.TryParse(args[0], out int parsed))
            {
                count = parsed;
            }

            var pc = EClass.pc;
            if (pc == null)
            {
                Debug.LogError("[AddChickensToParty] Player character not found");
                return;
            }

            int added = 0;
            for (int i = 0; i < count; i++)
            {
                var chicken = CharaGen.Create(CHICKEN_ID);
                if (chicken == null)
                {
                    Debug.LogError($"[AddChickensToParty] Failed to create chicken #{i + 1}");
                    continue;
                }

                // グローバル登録がないとセーブ時に壊れるため、現在ゾーンに配置してからグローバル化
                var zone = EClass._zone;
                if (zone != null)
                {
                    var point = pc.pos;
                    zone.AddCard(chicken, point);
                    chicken.SetGlobal();
                    chicken.homeZone = zone;
                }

                // パーティに追加
                pc.party.AddMemeber(chicken);
                added++;
                ModLog.Log($"[AddChickensToParty] Added chicken #{added} to party");
            }

            ModLog.Log($"[AddChickensToParty] Total added: {added}/{count} chickens");
        }
    }
}

