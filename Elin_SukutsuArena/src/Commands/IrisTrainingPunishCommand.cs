using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// アイリスの訓練で不正解時の罰を適用するコマンド
    /// modInvoke: apply_iris_training_punish('pinch'|'flick'|'push'|'splash'|'poke')
    /// </summary>
    public class IrisTrainingPunishCommand : IArenaCommand
    {
        public string Name => "apply_iris_training_punish";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            if (args.Length < 1)
            {
                Debug.LogError("[IrisTrainingPunish] Missing punish type argument");
                return;
            }

            // 引用符を除去
            string punishType = args[0].Trim('\'', '"');
            var pc = EClass.pc;

            if (pc == null)
            {
                Debug.LogError("[IrisTrainingPunish] PC is null");
                return;
            }

            switch (punishType)
            {
                case "pinch":
                    // 頬をつねる（プチっと潰れる音）
                    pc.PlaySound("bubble");
                    pc.PlayAnime(AnimeID.Shiver);
                    pc.DamageHP(1 + EClass.rnd(3), AttackSource.Condition);
                    ModLog.Log("[IrisTrainingPunish] Applied pinch");
                    break;

                case "flick":
                    // 額を弾く/頭を叩く
                    pc.PlaySound("hit_blunt");
                    pc.PlayAnime(AnimeID.Shiver);
                    pc.DamageHP(2 + EClass.rnd(5), AttackSource.Condition);
                    ModLog.Log("[IrisTrainingPunish] Applied flick");
                    break;

                case "push":
                    // 背中を押す/足払い
                    pc.PlaySound("kick");
                    pc.PlayAnime(AnimeID.Shiver);
                    pc.DamageHP(3 + EClass.rnd(5), AttackSource.Condition);
                    ModLog.Log("[IrisTrainingPunish] Applied push");
                    break;

                case "splash":
                    // 水飛沫（ダメージなし）
                    pc.PlaySound("water");
                    ModLog.Log("[IrisTrainingPunish] Applied splash (no damage)");
                    break;

                case "poke":
                    // 頬を突く/肩を押す
                    pc.PlaySound("push");
                    pc.PlayAnime(AnimeID.Shiver);
                    pc.DamageHP(1, AttackSource.Condition);
                    ModLog.Log("[IrisTrainingPunish] Applied poke");
                    break;

                default:
                    Debug.LogError($"[IrisTrainingPunish] Unknown punish type: {punishType}");
                    break;
            }
        }
    }
}

