namespace Elin_ModTemplate
{
    // Dedicated trait for justdoomit_arcade to avoid patching vanilla SlotMachine OnUse.
    public class TraitJustDoomArcade : TraitSlotMachine
    {
        public override bool OnUse(Chara c)
        {
            try
            {
                if (owner == null)
                {
                    return base.OnUse(c);
                }

                if (DoomSessionManager.Instance == null)
                {
                    return base.OnUse(c);
                }

                bool handledResult = false;
                if (!DoomSessionManager.Instance.TryHandleMachineUse(owner, c, ref handledResult))
                {
                    // Fallback to vanilla behavior when no WAD is available, etc.
                    return base.OnUse(c);
                }

                return handledResult;
            }
            catch (System.Exception ex)
            {
                DoomDiagnostics.Error("[JustDoomIt] TraitJustDoomArcade.OnUse failed. Fallback to vanilla.", ex);
                return base.OnUse(c);
            }
        }
    }
}
