namespace Elin_ArsMoriendi
{
    public class ConEmpowerUndead : Condition
    {
        public override bool UseElements => true;
        public override bool ShouldRefresh => true;

        public override void Tick()
        {
            Mod(-1);
        }
    }
}
