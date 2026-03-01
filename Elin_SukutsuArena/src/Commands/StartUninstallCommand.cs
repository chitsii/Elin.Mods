using Elin_SukutsuArena.Arena;
using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Reset;
using UnityEngine;
using DG.Tweening;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// Modアンインストール処理を開始するコマンド
    /// テレポート後にデータクリア、セーブを行う
    /// 使用: modInvoke start_uninstall()
    /// </summary>
    public class StartUninstallCommand : IArenaCommand
    {
        public string Name => "start_uninstall";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            ModLog.Log("[StartUninstall] Starting mod uninstall process");

            // ダイアログ終了後に処理
            LayerDrama.Instance?.SetOnKill(() =>
            {
                // 1. まず安全な場所にテレポート
                TeleportToSafeZone();

                // 2. ゾーン移動完了を待ってからアンインストール処理
                DOVirtual.DelayedCall(1.0f, () =>
                {
                    // ArenaResetManagerでアンインストール実行
                    // （NPC解雇、フラグ削除、クエスト削除、フィート削除、ゾーン削除を含む）
                    var result = ArenaResetManager.Execute(ResetLevel.Uninstall);
                    ModLog.Log($"[StartUninstall] Uninstall complete: Flags={result.FlagsRemoved}, " +
                              $"Feats={result.FeatsRemoved}, Zone={result.ZoneRemoved}, " +
                              $"Pets={result.PetsRemoved}, Quests={result.QuestsRemoved}");

                    // ゲームをセーブして案内
                    SaveAndShowDialog();
                });
            });
        }

        /// <summary>
        /// PCを安全な場所（マイホームまたはダルフィ）にテレポート
        /// </summary>
        private void TeleportToSafeZone()
        {
            // 現在のゾーンが闘技場でなければテレポート不要
            if (EClass._zone?.id != ArenaConfig.ZoneId)
            {
                ModLog.Log("[StartUninstall] Not in arena, skipping teleport");
                return;
            }

            // マイホームに移動を試みる（アリーナ以外）
            var homeZone = EClass.pc.homeZone;
            if (homeZone != null && homeZone.id != ArenaConfig.ZoneId)
            {
                ModLog.Log($"[StartUninstall] Teleporting PC to home zone: {homeZone.id}");
                EClass.pc.MoveZone(homeZone, ZoneTransition.EnterState.Center);
                return;
            }

            // マイホームがない場合はダルフィに移動
            var derphy = EClass.game.spatials.Find("derphy");
            if (derphy != null)
            {
                ModLog.Log("[StartUninstall] Teleporting PC to Derphy");
                EClass.pc.MoveZone(derphy, ZoneTransition.EnterState.Center);
                return;
            }

            Debug.LogWarning("[StartUninstall] Could not find safe zone to teleport");
        }

        /// <summary>
        /// ゲームをセーブして案内ダイアログを表示
        /// </summary>
        private void SaveAndShowDialog()
        {
            ModLog.Log("[StartUninstall] Saving game...");
            try
            {
                GameIO.SaveGame();
                ModLog.Log("[StartUninstall] Game saved successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[StartUninstall] Failed to save: {ex.Message}");
            }

            // 案内ダイアログを表示
            var message = "アンインストール処理が完了しました。\n\n" +
                "以下の手順でModを解除してください：\n" +
                "1. Escキーでメニューを開き「タイトルに戻る」を選択\n" +
                "2. Modビューアーからこのmodを無効化、\n" +
                "   またはSteamワークショップでこのModの登録を解除";

            Dialog.Ok(message);
        }
    }
}

