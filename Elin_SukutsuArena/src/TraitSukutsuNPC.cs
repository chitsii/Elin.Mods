using Elin_SukutsuArena.Attributes;

namespace Elin_SukutsuArena
{
    /// <summary>
    /// アリーナNPC用Trait（非商人）
    /// バニラの好感度判定に加え、メインクエスト完了後もペット化・招待可
    /// inject/Uniqueでバニラの_invite, _joinParty等が表示される
    /// </summary>
    [GameDependency("Inheritance", "TraitUniqueChara", "Medium", "Trait base class may change")]
    public class TraitSukutsuNPC : TraitUniqueChara
    {
        public override bool CanJoinParty =>
            base.CanJoinParty ||
            (ArenaQuestManager.Instance?.IsQuestCompleted("18_last_battle") ?? false);

        public override bool CanInvite =>
            base.CanInvite ||
            (ArenaQuestManager.Instance?.IsQuestCompleted("18_last_battle") ?? false);

        public override bool CanJoinPartyResident =>
            base.CanJoinPartyResident ||
            (ArenaQuestManager.Instance?.IsQuestCompleted("18_last_battle") ?? false);
    }

    /// <summary>
    /// アリーナ商人NPC用Trait（リリィ用）
    /// 商人機能を維持しつつ、バニラの好感度判定に加え、メインクエスト完了後もペット化・招待可
    /// inject/Uniqueでバニラの_invite, _joinParty等が表示される
    /// </summary>
    [GameDependency("Inheritance", "TraitMerchant", "Medium", "Trait base class may change")]
    public class TraitSukutsuMerchant : TraitMerchant
    {
        // 在庫キャパシティ: 10×10 = 100枠 (形式: 幅 * 100 + 高さ)
        private const int CONTAINER_SIZE = 1010;

        public override void OnCreate(int lv)
        {
            base.OnCreate(lv);
            owner.c_containerSize = CONTAINER_SIZE;
        }

        /// <summary>
        /// ユニークNPCとして扱う
        /// 追放時にc_uniqueData.uidZone（闘技場）に戻る
        /// </summary>
        public override bool IsUnique => true;

        /// <summary>
        /// ShopType.Specific を返すことでバニラ在庫の自動生成を抑止
        /// CWLのカスタム在庫のみが表示される
        /// </summary>
        public override ShopType ShopType => ShopType.Specific;

        // 治癒はアイリスが担当するため無効
        public override bool CanHeal => false;
        public override bool CanIdentify => true;

        public override bool CanJoinParty =>
            base.CanJoinParty ||
            (ArenaQuestManager.Instance?.IsQuestCompleted("18_last_battle") ?? false);

        public override bool CanInvite =>
            base.CanInvite ||
            (ArenaQuestManager.Instance?.IsQuestCompleted("18_last_battle") ?? false);

        public override bool CanJoinPartyResident =>
            base.CanJoinPartyResident ||
            (ArenaQuestManager.Instance?.IsQuestCompleted("18_last_battle") ?? false);
    }

    /// <summary>
    /// ゼク（怪しい商人）用Trait
    /// 商人機能のみ（治療・鑑定なし）
    /// </summary>
    [GameDependency("Inheritance", "TraitMerchant", "Medium", "Trait base class may change")]
    public class TraitSukutsuShadyMerchant : TraitMerchant
    {
        // 在庫キャパシティ: 10×10 = 100枠 (形式: 幅 * 100 + 高さ)
        private const int CONTAINER_SIZE = 1010;

        public override void OnCreate(int lv)
        {
            base.OnCreate(lv);
            owner.c_containerSize = CONTAINER_SIZE;
        }

        public override bool IsUnique => true;
        public override ShopType ShopType => ShopType.Specific;

        // 治療・鑑定機能は無効
        public override bool CanHeal => false;
        public override bool CanIdentify => false;

        public override bool CanJoinParty =>
            base.CanJoinParty ||
            (ArenaQuestManager.Instance?.IsQuestCompleted("18_last_battle") ?? false);

        public override bool CanInvite =>
            base.CanInvite ||
            (ArenaQuestManager.Instance?.IsQuestCompleted("18_last_battle") ?? false);

        public override bool CanJoinPartyResident =>
            base.CanJoinPartyResident ||
            (ArenaQuestManager.Instance?.IsQuestCompleted("18_last_battle") ?? false);
    }
}
