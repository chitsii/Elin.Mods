namespace Elin_ArsMoriendi
{
    public class TraitNecroSpellbook : TraitSpellbook
    {
        public override SourceElement.Row source =>
            EClass.sources.elements.map.TryGetValue(owner.refVal);

        public override void OnCreate(int lv)
        {
            base.OnCreate(lv);
            var alias = GetParam(1);
            if (EClass.sources.elements.alias.TryGetValue(alias, out var row))
            {
                TraitSpellbook.Create(owner, row.id);
            }
            var chargeParam = GetParam(2);
            if (!string.IsNullOrEmpty(chargeParam) && int.TryParse(chargeParam, out var charges))
            {
                owner.c_charges = charges;
            }
        }

        public override void OnRead(Chara c)
        {
            if (c.IsPC)
            {
                var spell = NecromancyManager.SpellUnlocks.Find(
                    s => s.ElementId == owner.refVal);
                if (spell != null && !NecromancyManager.Instance.IsUnlocked(spell.Alias))
                {
                    Msg.Say(Lang.langCode == "CN"
                        ? "无法理解这禁忌的知识……需要先解放知识。"
                        : Lang.isJP
                        ? "この禁断の知識はまだ理解できない…先に知識を解放する必要がある。"
                        : "You cannot comprehend this forbidden knowledge yet... You must unlock it first.");
                    return;
                }
            }
            base.OnRead(c);
        }
    }
}
