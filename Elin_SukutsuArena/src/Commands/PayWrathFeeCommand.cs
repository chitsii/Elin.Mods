using Elin_SukutsuArena.Core;
using UnityEngine;

namespace Elin_SukutsuArena.Commands
{
    /// <summary>
    /// 神罰治療費（300万gp）を支払うコマンド
    /// modInvoke: pay_wrath_fee()
    /// 結果は temp_paid_fee フラグにセット（branch_ifで使用）
    /// </summary>
    public class PayWrathFeeCommand : IArenaCommand
    {
        public string Name => "pay_wrath_fee";

        private const int WRATH_FEE = 3000000; // 300万gp
        private const string FLAG_PAID = "temp_paid_fee";

        public void Execute(ArenaContext ctx, DramaManager drama, string[] args)
        {
            var pc = EClass.pc;
            if (pc == null)
            {
                ctx.Storage.SetInt(FLAG_PAID, 0);
                return;
            }

            int currentMoney = pc.GetCurrency();

            if (currentMoney >= WRATH_FEE)
            {
                pc.ModCurrency(-WRATH_FEE);
                pc.Say($"{WRATH_FEE:#,0}gpを支払った。");
                ctx.Storage.SetInt(FLAG_PAID, 1);
                ModLog.Log($"[PayWrathFee] Paid {WRATH_FEE}gp, remaining: {pc.GetCurrency()}");
            }
            else
            {
                pc.Say($"お金が足りない…（必要: {WRATH_FEE:#,0}gp、所持: {currentMoney:#,0}gp）");
                ctx.Storage.SetInt(FLAG_PAID, 0);
                ModLog.Log($"[PayWrathFee] Not enough money: {currentMoney} < {WRATH_FEE}");
            }
        }
    }
}

