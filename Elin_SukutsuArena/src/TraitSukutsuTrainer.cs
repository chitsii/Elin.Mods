namespace Elin_SukutsuArena
{
    /// <summary>
    /// アリーナトレーナー用Trait
    /// TraitTrainerを継承し、追加機能（CanWhore, CanBout等）を有効化
    /// </summary>
    public class TraitSukutsuTrainer : TraitTrainer
    {
        /// <summary>
        /// ユニークNPCとして扱う
        /// 追放時にc_uniqueData.uidZone（闘技場）に戻る
        /// </summary>
        public override bool IsUnique => true;

        /// <summary>
        /// 夜のサービスを提供しない
        /// </summary>
        public override bool CanWhore => false;

        /// <summary>
        /// 勝負を挑める
        /// </summary>
        public override bool CanBout => true;

        /// <summary>
        /// パーティに参加可能（常時）
        /// </summary>
        public override bool CanJoinParty => true;

        /// <summary>
        /// 招待可能（常時）
        /// </summary>
        public override bool CanInvite => true;

        /// <summary>
        /// 拠点メンバーとしてパーティ参加可能
        /// </summary>
        public override bool CanJoinPartyResident => base.CanJoinPartyResident;
    }
}
