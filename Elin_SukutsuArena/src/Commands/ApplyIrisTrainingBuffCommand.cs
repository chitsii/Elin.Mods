using Elin_SukutsuArena.Conditions;
using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// アイリスの訓練バフを付与するコマンド
    /// modInvoke: apply_iris_training_buff('sense'|'legs'|'peace')
    /// </summary>
    public class ApplyIrisTrainingBuffCommand : IArenaCommand
    {
        public string Name => "apply_iris_training_buff";

        // バフの持続ターン数
        private const int BUFF_DURATION = 1200;

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            if (args.Length < 1)
            {
                Debug.LogError("[ApplyIrisTrainingBuff] Missing buff type argument");
                return;
            }

            // 引用符を除去
            string buffType = args[0].Trim('\'', '"');
            var pc = EClass.pc;

            if (pc == null)
            {
                Debug.LogError("[ApplyIrisTrainingBuff] PC is null");
                return;
            }

            switch (buffType)
            {
                case "sense":
                    ApplySenseBuff(pc);
                    break;
                case "legs":
                    ApplyLegsBuff(pc);
                    break;
                case "peace":
                    ApplyPeaceBuff(pc);
                    break;
                default:
                    Debug.LogError($"[ApplyIrisTrainingBuff] Unknown buff type: {buffType}");
                    break;
            }
        }

        private void ApplySenseBuff(Chara pc)
        {
            // 既存のバフを削除
            pc.RemoveCondition<ConIrisSixthSense>();

            // 新しいバフを付与
            pc.AddCondition<ConIrisSixthSense>(BUFF_DURATION);
            ModLog.Log($"[ApplyIrisTrainingBuff] Applied ConIrisSixthSense to {pc.Name}, duration={BUFF_DURATION}");
        }

        private void ApplyLegsBuff(Chara pc)
        {
            // 既存のバフを削除
            pc.RemoveCondition<ConIrisIronLegs>();

            // 新しいバフを付与
            pc.AddCondition<ConIrisIronLegs>(BUFF_DURATION);
            ModLog.Log($"[ApplyIrisTrainingBuff] Applied ConIrisIronLegs to {pc.Name}, duration={BUFF_DURATION}");
        }

        private void ApplyPeaceBuff(Chara pc)
        {
            // 既存のバフを削除
            pc.RemoveCondition<ConIrisBoundaryPeace>();

            // 新しいバフを付与
            pc.AddCondition<ConIrisBoundaryPeace>(BUFF_DURATION);
            ModLog.Log($"[ApplyIrisTrainingBuff] Applied ConIrisBoundaryPeace to {pc.Name}, duration={BUFF_DURATION}");
        }
    }
}

