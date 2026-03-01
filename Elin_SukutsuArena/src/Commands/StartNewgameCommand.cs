using Elin_SukutsuArena.Core;
using Elin_SukutsuArena.Localization;
using Elin_SukutsuArena.Reset;
using UnityEngine;
using DG.Tweening;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// 周回処理（ニューゲーム+）を開始するコマンド
    /// 進行状況をリセットしてオープニングから再開
    /// 使用: modInvoke start_newgame()
    /// </summary>
    public class StartNewgameCommand : IArenaCommand
    {
        public string Name => "start_newgame";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            ModLog.Log("[StartNewgame] Starting new game cycle");

            // ダイアログ終了後にリセット処理とオープニング再開
            LayerDrama.Instance?.SetOnKill(() =>
            {
                // 進行状況をリセット（ランクと貢献度は保持）
                var result = ArenaResetManager.Execute(ResetLevel.NewGamePlus);
                ModLog.Log($"[StartNewgame] Reset complete: Flags={result.FlagsRemoved}, Pets={result.PetsRemoved}");

                // 少し遅延してオープニングドラマを開始
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    try
                    {
                        // リリィを見つけてShowDialogで開始（Zone_SukutsuArenaと同じ方式）
                        var lily = EClass._zone.FindChara("sukutsu_receptionist");
                        if (lily != null)
                        {
                            lily.ShowDialog("drama_sukutsu_opening", "main");
                            ModLog.Log("[StartNewgame] Opening drama started via ShowDialog");
                        }
                        else
                        {
                            // リリィが見つからない場合は直接Activate
                            LayerDrama.Activate("drama_sukutsu_opening", null, null, EClass.pc, null, null);
                            ModLog.Log("[StartNewgame] Opening drama activated directly");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[StartNewgame] Failed to start opening drama: {ex}");
                        Msg.Say(ArenaLocalization.NewGameCycleStarted);
                    }
                });
            });
        }
    }
}

