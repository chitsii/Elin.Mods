namespace Elin_ArsMoriendi
{
    public static class NecroSpellUtil
    {
        public static bool HasSoul(Chara c) => !c.IsMachine;

        /// <summary>
        /// PC周囲のランダムな座標を返す。見つからなければPCの座標にフォールバック。
        /// </summary>
        public static Point GetSpawnPos(int radius = 3)
        {
            return EClass.pc.pos.GetRandomPoint(radius) ?? EClass.pc.pos;
        }

        /// <summary>
        /// 対敵死霊術の共通ガード。Mod無効 or 魂なしターゲットならブロック。
        /// 戻り値: null=続行, false=Mod無効(Perform false), true=魂なし(Perform true)
        /// 使い方: if (NecroSpellUtil.CheckHostile(Act.TC, Act.CC) is bool r) return r;
        /// </summary>
        public static bool? CheckHostile(Card tc, Chara caster)
        {
            if (tc.isChara && !HasSoul(tc.Chara))
            {
                LangHelper.Say("spellNoSoul", caster, tc);
                return true;
            }
            return null;
        }
    }
}

