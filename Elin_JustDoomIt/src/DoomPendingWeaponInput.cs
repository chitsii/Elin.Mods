namespace Elin_JustDoomIt
{
    public struct DoomPendingWeaponInput
    {
        public int PendingWeaponSlot { get; private set; }
        public int PendingWeaponCycleSteps { get; private set; }

        public void Capture(int weaponSlot, int weaponCycleSteps)
        {
            if (weaponSlot >= 1 && weaponSlot <= 7)
            {
                PendingWeaponSlot = weaponSlot;
                PendingWeaponCycleSteps = 0;
                return;
            }

            if (weaponCycleSteps != 0)
            {
                PendingWeaponSlot = 0;
                PendingWeaponCycleSteps += weaponCycleSteps;
                if (PendingWeaponCycleSteps > 7)
                {
                    PendingWeaponCycleSteps = 7;
                }
                else if (PendingWeaponCycleSteps < -7)
                {
                    PendingWeaponCycleSteps = -7;
                }
            }
        }

        public void Consume(out int weaponSlot, out int weaponCycleSteps)
        {
            weaponSlot = PendingWeaponSlot;
            weaponCycleSteps = PendingWeaponCycleSteps;
            PendingWeaponSlot = 0;
            PendingWeaponCycleSteps = 0;
        }

        public void ConsumeOneTick(out int weaponSlot, out int weaponCycleSteps)
        {
            weaponSlot = PendingWeaponSlot;
            if (PendingWeaponSlot != 0)
            {
                PendingWeaponSlot = 0;
                weaponCycleSteps = 0;
                return;
            }

            if (PendingWeaponCycleSteps > 0)
            {
                PendingWeaponCycleSteps--;
                weaponCycleSteps = 1;
                return;
            }

            if (PendingWeaponCycleSteps < 0)
            {
                PendingWeaponCycleSteps++;
                weaponCycleSteps = -1;
                return;
            }

            weaponCycleSteps = 0;
        }

        public void Clear()
        {
            PendingWeaponSlot = 0;
            PendingWeaponCycleSteps = 0;
        }
    }
}
