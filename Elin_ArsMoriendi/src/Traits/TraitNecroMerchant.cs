namespace Elin_ArsMoriendi
{
    public class TraitNecroMerchant : TraitMerchant
    {
        // SukutsuArenaのゼク方式と同様に、バニラのランダム在庫生成を抑止する。
        // 実在庫は addStock / stock_ars_hecatia.json 側のみで管理する。
        public override ShopType ShopType => ShopType.Specific;

        public override CurrencyType CurrencyType => CurrencyType.Money;

        public override bool CanInvest => false;

        public override bool IsCitizen => false;
    }
}
