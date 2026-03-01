using UnityEngine;
using Elin_SukutsuArena.Attributes;
using Elin_SukutsuArena.Localization;

namespace Elin_SukutsuArena.RandomBattle
{
    /// <summary>
    /// 共感の場ギミック: 全員にテレパシー&透視付与
    /// </summary>
    [GameDependency("Inheritance", "ZoneEvent", "Medium", "Zone event base class may change")]
    public class ZoneEventEmpathetic : ZoneEvent
    {
        private bool applied = false;
        private float timer = 0f;

        public override void OnTick()
        {
            if (!applied)
            {
                Msg.Say(ArenaLocalization.GimmickEmpatheticStart);
                ApplyEmpathy();
                applied = true;
            }

            // 定期的にバフを再適用（新しく生成されたキャラ対応）
            timer += global::Core.delta;
            if (timer >= 10f)
            {
                timer = 0f;
                ApplyEmpathy();
            }
        }

        private void ApplyEmpathy()
        {
            try
            {
                foreach (Chara c in EClass._map.charas)
                {
                    if (c.isDead) continue;

                    // テレパシー付与
                    try
                    {
                        c.AddCondition<ConTelepathy>(500);
                    }
                    catch { }

                    // 透視付与
                    try
                    {
                        c.AddCondition<ConSeeInvisible>(500);
                    }
                    catch { }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ZoneEventEmpathetic] Error applying empathy: {ex.Message}");
            }
        }
    }
}
