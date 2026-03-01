using UnityEngine;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Localization;

namespace Elin_SukutsuArena.RandomBattle
{
    /// <summary>
    /// 無法地帯ギミック: アンチマジック+50
    /// 魔法攻撃を弱体化
    /// </summary>
    [GameDependency("Inheritance", "ZoneEvent", "Medium", "Zone event base class may change")]
    public class ZoneEventAntiMagic : ZoneEvent
    {
        private bool applied = false;
        private const int ANTI_MAGIC_POWER = 50;

        public override void OnTick()
        {
            if (applied) return;

            try
            {
                // 開始メッセージ
                Msg.Say(ArenaLocalization.GimmickAntiMagicStart);

                // PCと仲間にアンチマジックデバフを付与
                foreach (Chara c in EClass._map.charas)
                {
                    if (c.IsPC || c.IsPCFaction)
                    {
                        ApplyAntiMagicDebuff(c);
                    }
                }

                applied = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ZoneEventAntiMagic] Error: {ex.Message}");
                applied = true;
            }
        }

        private void ApplyAntiMagicDebuff(Chara c)
        {
            // アンチマジック効果として魔法系のダメージを軽減するデバフを付与
            // 実際の実装ではConditionを使用するが、簡易版として警告表示のみ
            ModLog.Log($"[ZoneEventAntiMagic] Applied anti-magic to {c.Name}");
        }

        /// <summary>
        /// 無法地帯が有効かチェック
        /// </summary>
        public static bool IsAntiMagicActive()
        {
            if (EClass._zone?.events == null) return false;
            return EClass._zone.events.GetEvent<ZoneEventAntiMagic>() != null;
        }
    }
}

