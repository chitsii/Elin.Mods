namespace Elin_JustDoomIt
{
    internal struct DoomWeaponCyclePlanner
    {
        private int _readyWeaponSlot;
        private int _predictedWeaponSlot;

        public void Reset(int readyWeaponSlot)
        {
            _readyWeaponSlot = NormalizeReadyWeaponSlot(readyWeaponSlot);
            _predictedWeaponSlot = 0;
        }

        public void SyncActualReady(int readyWeaponSlot)
        {
            _readyWeaponSlot = NormalizeReadyWeaponSlot(readyWeaponSlot);
            if (_predictedWeaponSlot == _readyWeaponSlot)
            {
                _predictedWeaponSlot = 0;
            }
        }

        public int PlanDirectSlot(int weaponSlot)
        {
            return WrapWeaponSlot(weaponSlot);
        }

        public int PlanCycleStep(int direction)
        {
            var baseSlot = _predictedWeaponSlot != 0
                ? _predictedWeaponSlot
                : (_readyWeaponSlot != 0 ? _readyWeaponSlot : 1);
            var slot = WrapWeaponSlot(baseSlot + (direction >= 0 ? 1 : -1));
            _predictedWeaponSlot = slot;
            return slot;
        }

        private static int NormalizeReadyWeaponSlot(int slot)
        {
            return slot > 0 ? WrapWeaponSlot(slot) : 0;
        }

        private static int WrapWeaponSlot(int slot)
        {
            while (slot < 1)
            {
                slot += 7;
            }

            while (slot > 7)
            {
                slot -= 7;
            }

            return slot;
        }
    }
}
